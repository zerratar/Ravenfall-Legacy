using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

/// <summary>
/// Grows and draws the grass of a <see cref="GrassField"/>: the world in 8 m cells, every cell within the grass
/// distance of the camera grown once in a Burst job (from the terrain's heights and layers, the same cell always the
/// same grass), kept in a slot of one GPU buffer while it is in range, and drawn through one
/// <see cref="BatchRendererGroup"/> culled in a Burst job. Moving the camera only grows the cells at the edge.
/// </summary>
/// <remarks>
/// <see cref="Enabled"/>, <see cref="Distance"/> and <see cref="Density"/> are the settings: slower machines turn
/// grass down or off. Grass receives shadows and casts none; it thins out with distance and the shader shrinks it to
/// nothing at the end, so cells never pop.
/// </remarks>
public static unsafe class GrassRenderer
{
    private const float CellSize = 8f;
    private const int HeaderBytes = 64;
    private const int HeaderFloats = HeaderBytes / 4;

    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker(ProfilerCategory.Render, "GrassRenderer.Update");
    private static readonly ProfilerMarker CullMarker = new ProfilerMarker(ProfilerCategory.Render, "GrassRenderer.Cull");
    private static readonly int ObjectToWorldId = Shader.PropertyToID("unity_ObjectToWorld");
    private static readonly int WorldToObjectId = Shader.PropertyToID("unity_WorldToObject");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int WindId = Shader.PropertyToID("_GrassWind");
    private static readonly int FadeId = Shader.PropertyToID("_GrassFade");
    private static readonly int AmbientId = Shader.PropertyToID("_GrassSH");
    private static readonly Vector4[] ambient = new Vector4[7];

    private static bool enabled = true;
    private static float distance = 60f;
    private static float density = 1f;

    private static readonly List<GrassField> fields = new List<GrassField>();
    private static GrassField field;
    private static bool dirty = true;
    private static bool groundDirty = true;
    private static bool hooked;

    private static BatchRendererGroup group;
    private static GraphicsBuffer buffer;
    private static BatchID batch;
    private static JobHandle culling;
    // what the last camera drew: near clumps, far clumps, triangles
    private static NativeArray<int> drawn;
    private static int[] kindTriangles = Array.Empty<int>();

    // the kinds of grass, as drawn and as grown
    private struct KindInfo
    {
        public int candidates;   // points tried per cell at full density
        public int capacity;     // at most this many per cell
        public int offset;       // where they start in a cell's slot
        public float sizeMin;
        public float sizeMax;
        public float minDensity;
    }
    private static NativeArray<KindInfo> kinds;
    // two meshes per kind, near and far (the same mesh twice for a kind that has no far one)
    private static BatchMeshID[] kindMesh = Array.Empty<BatchMeshID>();
    private static BatchMaterialID[] kindMaterial = Array.Empty<BatchMaterialID>();
    private static readonly List<Mesh> madeMeshes = new List<Mesh>();
    private static readonly List<Material> madeMaterials = new List<Material>();
    private static int cellCapacity;
    private static float tallest;

    // the cells: which cell each slot holds, how many of each kind it grew, its height range and its points
    private static int slotCount;
    private static NativeArray<int2> slotCell;
    private static NativeArray<byte> slotUsed;
    private static NativeArray<int> slotCounts;
    private static NativeArray<float2> slotHeights;
    private static NativeArray<float4> points;        // position and rank, per instance
    private static int bufferFloats;
    private static readonly Dictionary<int2, int> cellSlot = new Dictionary<int2, int>();
    private static readonly Stack<int> freeSlots = new Stack<int>();

    // the ground: heights, and per texel the grass density (low byte) and colour
    private struct Ground
    {
        public float2 origin;
        public float2 size;
        public int heightResolution;
        public int mapResolution;
        public float waterLevel;
        public float minUp;
        public float3 tint;
        public float jitter;
        public float patches;
        public float maskMetres;     // metres per mask texel
        public int2 maskSize;        // texels across and down; 0 for no mask
    }
    private static Ground ground;
    private static NativeArray<float> heights;
    private static NativeArray<uint> groundMap;
    // the painted mask over the terrain, one byte per texel (128 the terrain's own grass)
    private static NativeArray<byte> maskMap;

    // roads keep grass off: each a run of capsules along its middle, as wide as its solid part and a little more
    private const float RoadMargin = 0.3f;
    private struct Segment { public float2 a; public float2 b; public float radius; }
    private sealed class Blocker { public Segment[] segments; public float4 bounds; }
    private static readonly Dictionary<object, Blocker> blockers = new Dictionary<object, Blocker>();
    // where grass has to grow again: a road came, went or changed there (min x, min z, max x, max z)
    private static readonly List<float4> regrow = new List<float4>();

    /// <summary>Grass on or off.</summary>
    public static bool Enabled
    {
        get => enabled;
        set { if (enabled != value) { enabled = value; dirty = true; } }
    }

    /// <summary>
    /// Not drawn and not grown for now (potato mode), keeping what is grown so it is back at once. Unlike
    /// <see cref="Enabled"/> this is not a setting of its own.
    /// </summary>
    public static bool Suppressed { get; set; }

    /// <summary>How far from the camera grass grows, in metres.</summary>
    public static float Distance
    {
        get => distance;
        set { value = Mathf.Clamp(value, 10f, 250f); if (!Mathf.Approximately(distance, value)) { distance = value; dirty = true; } }
    }

    /// <summary>How dense, from 0.1 to 1 of the profile's density.</summary>
    public static float Density
    {
        get => density;
        set { value = Mathf.Clamp(value, 0.1f, 1f); if (!Mathf.Approximately(density, value)) { density = value; dirty = true; } }
    }

    /// <summary>
    /// Grow grass round this instead of the game camera (or, in the editor, the Scene view's): a camera rendering
    /// into a texture, a cutscene. Null for the usual.
    /// </summary>
    public static Transform Focus { get; set; }

    /// <summary>How many grass instances are grown around the camera now.</summary>
    public static int InstanceCount { get; private set; }

    /// <summary>What the last camera drew: near and far instances, and their triangles.</summary>
    public static (int near, int far, long triangles) LastDrawn
    {
        get
        {
            if (!drawn.IsCreated) return default;
            culling.Complete();
            var triangles = 0L;
            for (var l = 0; l < kindTriangles.Length && l < drawn.Length; ++l) triangles += (long)drawn[l] * kindTriangles[l];
            int near = 0, far = 0;
            for (var l = 0; l < drawn.Length; ++l) { if (l % 2 == 0) near += drawn[l]; else far += drawn[l]; }
            return (near, far, triangles);
        }
    }

    /// <summary>How many cells are grown.</summary>
    public static int CellCount => cellSlot.Count;

    public static void Register(GrassField grassField)
    {
        if (!grassField || fields.Contains(grassField)) return;
        fields.Add(grassField);
        Hook();
        dirty = groundDirty = true;
    }

    public static void Unregister(GrassField grassField)
    {
        if (fields.Remove(grassField)) dirty = groundDirty = true;
    }

    /// <summary>
    /// Keeps grass off a road: <paramref name="samples"/> its middle in world x and z, <paramref name="width"/> the
    /// width of its solid part, <paramref name="scales"/> a multiple of it per sample (or null). Call it again when
    /// the road changes; the grass under the old and the new road grows again. A road that is a Unity object leaves
    /// by itself when it is destroyed, or call <see cref="Remove"/>.
    /// </summary>
    public static void SetRoad(object owner, IReadOnlyList<Vector2> samples, float width, IReadOnlyList<float> scales)
    {
        if (owner == null) return;
        if (samples == null || samples.Count == 0)
        {
            Remove(owner);
            return;
        }

        float Half(int i) => width * 0.5f * (scales != null && i < scales.Count ? scales[i] : 1f) + RoadMargin;
        var segments = new Segment[Mathf.Max(1, samples.Count - 1)];
        var bounds = new float4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        for (var i = 0; i < segments.Length; ++i)
        {
            var next = Mathf.Min(i + 1, samples.Count - 1);
            var a = (float2)samples[i];
            var b = (float2)samples[next];
            var radius = math.max(Half(i), Half(next));
            segments[i] = new Segment { a = a, b = b, radius = radius };
            bounds = new float4(math.min(bounds.xy, math.min(a, b) - radius), math.max(bounds.zw, math.max(a, b) + radius));
        }

        SetBlocker(owner, segments, bounds);
    }

    /// <summary>
    /// Keeps grass off a rectangle on the ground (a village plot and what is built on it): its middle, half its size
    /// along its own x and z, and its turn in degrees. Call it again when it moves or changes.
    /// </summary>
    public static void SetArea(object owner, Vector3 centre, Vector2 halfSize, float turn)
    {
        if (owner == null) return;
        // a capsule along the long side, as wide as the short side, nearly to the corners
        var rotation = Quaternion.Euler(0f, turn, 0f);
        var alongX = halfSize.x >= halfSize.y;
        var axis = rotation * (alongX ? Vector3.right : Vector3.forward);
        var radius = (alongX ? halfSize.y : halfSize.x) + RoadMargin;
        var length = Mathf.Max(0f, (alongX ? halfSize.x : halfSize.y) - radius * 0.3f);
        var c = new float2(centre.x, centre.z);
        var d = new float2(axis.x, axis.z) * length;
        var segment = new Segment { a = c - d, b = c + d, radius = radius };
        var bounds = new float4(math.min(segment.a, segment.b) - radius, math.max(segment.a, segment.b) + radius);
        SetBlocker(owner, new[] { segment }, bounds);
    }

    private static void SetBlocker(object owner, Segment[] segments, float4 bounds)
    {
        if (blockers.TryGetValue(owner, out var old)) regrow.Add(old.bounds);
        blockers[owner] = new Blocker { segments = segments, bounds = bounds };
        regrow.Add(bounds);
        Hook();
    }

    /// <summary>
    /// The field's mask was painted between these world corners (x and z): read that part again and grow the grass
    /// there again.
    /// </summary>
    public static void MaskChanged(GrassField grassField, Vector2 min, Vector2 max)
    {
        if (grassField != field || !maskMap.IsCreated || ground.maskSize.x == 0 || !grassField.Mask) return;
        var mask = grassField.Mask;
        var from = (int2)math.floor((new float2(min.x, min.y) - ground.origin) / ground.maskMetres);
        var to = (int2)math.ceil((new float2(max.x, max.y) - ground.origin) / ground.maskMetres);
        from = math.clamp(from, 0, ground.maskSize - 1);
        to = math.clamp(to, 0, ground.maskSize - 1);
        culling.Complete();
        for (var y = from.y; y <= to.y; ++y)
        {
            for (var x = from.x; x <= to.x; ++x) maskMap[y * ground.maskSize.x + x] = mask.Get(x, y);
        }
        regrow.Add(new float4(min.x, min.y, max.x, max.y));
    }

    /// <summary>The road or area is gone: grass grows where it was.</summary>
    public static void Remove(object owner)
    {
        if (owner == null || !blockers.TryGetValue(owner, out var old)) return;
        blockers.Remove(owner);
        regrow.Add(old.bounds);
    }

    /// <summary>Grows everything again, on the next frame (a profile or setting changed).</summary>
    public static void Regrow() => dirty = true;

    /// <summary>The terrain was reshaped or repainted: read it again and grow everything again.</summary>
    public static void GroundChanged(GrassField grassField) => dirty = groundDirty = true;

    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        RenderPipelineManager.beginContextRendering += BeforeRendering;
        GrassProfile.Changed += _ => dirty = true;
        Application.quitting += Dispose;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif
    }

    /// <summary>The scene's ambient light for the grass shader, which cannot rely on the instanced fallback for it.</summary>
    private static void SetAmbient()
    {
        var sh = new SHCoefficients(RenderSettings.ambientProbe);
        ambient[0] = sh.SHAr;
        ambient[1] = sh.SHAg;
        ambient[2] = sh.SHAb;
        ambient[3] = sh.SHBr;
        ambient[4] = sh.SHBg;
        ambient[5] = sh.SHBb;
        ambient[6] = sh.SHC;
        Shader.SetGlobalVectorArray(AmbientId, ambient);
    }

    private static void BeforeRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        culling.Complete();
        culling = default;
        using (UpdateMarker.Auto())
        {
            if (dirty) Rebuild();
            if (buffer == null || !field || Suppressed) return;

            Shader.SetGlobalVector(WindId, new Vector4(0.8f, 0.6f, 0.12f, 1.7f));
            Shader.SetGlobalVector(FadeId, new Vector4(distance * 0.7f, distance, 0f, 0f));
            SetAmbient();
            if (TryFocus(cameras, out var focus)) Stream(focus);
        }
    }

    /// <summary>Where grass grows around: the game camera while playing, the Scene view's in the editor.</summary>
    private static bool TryFocus(List<Camera> cameras, out Vector3 focus)
    {
        if (Focus)
        {
            focus = Focus.position;
            return true;
        }

        Camera camera = null;
        if (Application.isPlaying) camera = Camera.main;
#if UNITY_EDITOR
        else if (UnityEditor.SceneView.lastActiveSceneView) camera = UnityEditor.SceneView.lastActiveSceneView.camera;
#endif
        if (!camera && cameras.Count > 0) camera = cameras[0];
        focus = camera ? camera.transform.position : default;
        return camera;
    }

    /// <summary>Frees the cells that left the grass distance and grows the ones that came into it.</summary>
    private static void Stream(Vector3 focus)
    {
        var keep = distance + CellSize * 1.5f;
        var reach = distance + CellSize * 0.75f;
        var centre = new float2(focus.x, focus.z);

        // roads whose objects were destroyed are gone; cells under a road that came, went or changed grow again
        List<object> gone = null;
        foreach (var pair in blockers)
        {
            if (pair.Key is UnityEngine.Object unityObject && !unityObject) (gone ??= new List<object>()).Add(pair.Key);
        }
        if (gone != null) foreach (var owner in gone) Remove(owner);
        if (regrow.Count > 0)
        {
            for (var s = 0; s < slotCount; ++s)
            {
                if (slotUsed[s] == 0) continue;
                var min = (float2)slotCell[s] * CellSize;
                foreach (var area in regrow)
                {
                    if (min.x > area.z || min.y > area.w || min.x + CellSize < area.x || min.y + CellSize < area.y) continue;
                    slotUsed[s] = 0;
                    cellSlot.Remove(slotCell[s]);
                    freeSlots.Push(s);
                    break;
                }
            }
            regrow.Clear();
        }

        // cells out of range give their slots back
        for (var s = 0; s < slotCount; ++s)
        {
            if (slotUsed[s] == 0) continue;
            var cell = slotCell[s];
            if (math.distance(CellCentre(cell), centre) <= keep) continue;
            slotUsed[s] = 0;
            cellSlot.Remove(cell);
            freeSlots.Push(s);
        }

        // cells in range that are not grown yet, nearest first
        var wanted = new List<(int2 cell, float distance)>();
        var lo = (int2)math.floor((centre - reach) / CellSize);
        var hi = (int2)math.floor((centre + reach) / CellSize);
        for (var z = lo.y; z <= hi.y; ++z)
        {
            for (var x = lo.x; x <= hi.x; ++x)
            {
                var cell = new int2(x, z);
                if (cellSlot.ContainsKey(cell)) continue;
                var d = math.distance(CellCentre(cell), centre);
                if (d <= reach && OnTerrain(cell)) wanted.Add((cell, d));
            }
        }
        if (wanted.Count == 0) return;
        wanted.Sort((a, b) => a.distance.CompareTo(b.distance));

        var grow = new NativeList<int>(wanted.Count, Allocator.TempJob);
        foreach (var (cell, _) in wanted)
        {
            if (freeSlots.Count == 0) break;
            var s = freeSlots.Pop();
            slotCell[s] = cell;
            slotUsed[s] = 1;
            cellSlot[cell] = s;
            grow.Add(s);
        }

        // the road segments over each grown cell
        var segments = new NativeList<Segment>(64, Allocator.TempJob);
        var segmentRanges = new NativeArray<int2>(grow.Length, Allocator.TempJob);
        for (var g = 0; g < grow.Length; ++g)
        {
            var min = (float2)slotCell[grow[g]] * CellSize;
            var max = min + CellSize;
            var first = segments.Length;
            foreach (var blocker in blockers.Values)
            {
                var b = blocker.bounds;
                if (b.x > max.x || b.y > max.y || b.z < min.x || b.w < min.y) continue;
                foreach (var segment in blocker.segments)
                {
                    var from = math.min(segment.a, segment.b) - segment.radius;
                    var to = math.max(segment.a, segment.b) + segment.radius;
                    if (from.x > max.x || from.y > max.y || to.x < min.x || to.y < min.y) continue;
                    segments.Add(segment);
                }
            }
            segmentRanges[g] = new int2(first, segments.Length - first);
        }

        // the grown cells' transforms, inverses and colours, in three runs, on their way to the GPU
        var uploadInstances = grow.Length * cellCapacity;
        var upload = new NativeArray<float>(uploadInstances * 28, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        new GrowJob
        {
            slots = grow.AsArray(),
            slotCell = slotCell,
            slotCounts = slotCounts,
            slotHeights = slotHeights,
            points = points,
            upload = upload,
            uploadInstances = uploadInstances,
            segments = segments.AsArray(),
            segmentRanges = segmentRanges,
            kinds = kinds,
            heights = heights,
            groundMap = groundMap,
            maskMap = maskMap,
            ground = ground,
            density = density,
            cellCapacity = cellCapacity,
            tallest = tallest,
        }.Schedule(grow.Length, 4).Complete();

        // each grown cell into its slot of the GPU buffer
        var total = slotCount * cellCapacity;
        for (var g = 0; g < grow.Length; ++g)
        {
            var first = grow[g] * cellCapacity;
            var from = g * cellCapacity;
            buffer.SetData(upload, from * 12, HeaderFloats + first * 12, cellCapacity * 12);
            buffer.SetData(upload, uploadInstances * 12 + from * 12, HeaderFloats + (total + first) * 12, cellCapacity * 12);
            buffer.SetData(upload, uploadInstances * 24 + from * 4, HeaderFloats + total * 24 + first * 4, cellCapacity * 4);
        }
        upload.Dispose();
        segments.Dispose();
        segmentRanges.Dispose();
        grow.Dispose();

        var count = 0;
        for (var s = 0; s < slotCount; ++s)
        {
            if (slotUsed[s] == 0) continue;
            for (var k = 0; k < kinds.Length; ++k) count += slotCounts[s * kinds.Length + k];
        }
        InstanceCount = count;
    }

    private static float2 CellCentre(int2 cell) => ((float2)cell + 0.5f) * CellSize;

    private static bool OnTerrain(int2 cell)
    {
        var min = (float2)cell * CellSize;
        var max = min + CellSize;
        return max.x > ground.origin.x && max.y > ground.origin.y && min.x < ground.origin.x + ground.size.x && min.y < ground.origin.y + ground.size.y;
    }

    /// <summary>Everything again: the field, the kinds, the slots and the buffer, and the ground when it changed.</summary>
    private static void Rebuild()
    {
        dirty = false;
        culling.Complete();
        DisposeDrawing();

        fields.RemoveAll(f => !f);
        field = null;
        foreach (var f in fields) if (f.isActiveAndEnabled && f.Terrain && f.Terrain.terrainData && f.Profile) { field = f; break; }
        if (!enabled || !field) return;

        if (groundDirty || !heights.IsCreated) ReadGround();
        groundDirty = false;

        if (group == null)
        {
            group = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            group.SetGlobalBounds(new Bounds(Vector3.zero, Vector3.one * 100000f));
        }

        // the kinds: their meshes and materials, and how many of each a cell can hold
        var profile = field.Profile;
        var infos = new List<KindInfo>();
        var meshIds = new List<BatchMeshID>();
        var materialIds = new List<BatchMaterialID>();
        var triangles = new List<int>();
        var area = CellSize * CellSize;
        cellCapacity = 0;
        tallest = 0f;
        for (var k = 0; k < profile.kinds.Length; ++k)
        {
            var kind = profile.kinds[k];
            if (kind == null || !kind.enabled || kind.perSquareMetre <= 0f) continue;
            var mesh = kind.mesh;
            var farMesh = mesh;
            if (!mesh)
            {
                // half the blades, wider, in one segment: from a distance it covers as much for far fewer triangles
                mesh = GrassBlades.Build(kind.blades, kind.bladeWidth, 1000 + k);
                farMesh = GrassBlades.Build(Mathf.Max(2, kind.blades / 2), kind.bladeWidth * 1.6f, 2000 + k, 1);
                madeMeshes.Add(mesh);
                madeMeshes.Add(farMesh);
            }
            var material = kind.material;
            if (!material)
            {
                var shader = Shader.Find("Ravenfall/Grass");
                if (!shader) continue;
                material = new Material(shader) { name = "Grass " + kind.name, hideFlags = HideFlags.DontSave };
                madeMaterials.Add(material);
            }

            var candidates = Mathf.CeilToInt(kind.perSquareMetre * density * area);
            infos.Add(new KindInfo
            {
                candidates = candidates,
                capacity = candidates,
                offset = cellCapacity,
                sizeMin = Mathf.Min(kind.size.x, kind.size.y),
                sizeMax = Mathf.Max(kind.size.x, kind.size.y),
                minDensity = kind.minDensity,
            });
            cellCapacity += candidates;
            tallest = Mathf.Max(tallest, mesh.bounds.max.y * Mathf.Max(kind.size.x, kind.size.y));
            triangles.Add(mesh.triangles.Length / 3);
            triangles.Add(farMesh.triangles.Length / 3);
            var nearId = group.RegisterMesh(mesh);
            meshIds.Add(nearId);
            meshIds.Add(farMesh == mesh ? nearId : group.RegisterMesh(farMesh));
            materialIds.Add(group.RegisterMaterial(material));
        }
        if (infos.Count == 0 || cellCapacity == 0) return;

        kinds = new NativeArray<KindInfo>(infos.ToArray(), Allocator.Persistent);
        kindMesh = meshIds.ToArray();
        kindTriangles = triangles.ToArray();
        drawn = new NativeArray<int>(kindMesh.Length, Allocator.Persistent);
        kindMaterial = materialIds.ToArray();

        // enough slots for every cell that can be in range at once
        var across = Mathf.CeilToInt(2f * (distance + CellSize * 1.5f) / CellSize) + 1;
        slotCount = across * across;
        var total = slotCount * cellCapacity;
        slotCell = new NativeArray<int2>(slotCount, Allocator.Persistent);
        slotUsed = new NativeArray<byte>(slotCount, Allocator.Persistent);
        slotCounts = new NativeArray<int>(slotCount * kinds.Length, Allocator.Persistent);
        slotHeights = new NativeArray<float2>(slotCount, Allocator.Persistent);
        points = new NativeArray<float4>(total, Allocator.Persistent);
        bufferFloats = HeaderFloats + total * 28;
        cellSlot.Clear();
        freeSlots.Clear();
        for (var s = slotCount - 1; s >= 0; --s) freeSlots.Push(s);

        // the zero header BatchRendererGroup expects in front; the slots are written as cells grow
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferFloats, 4);
        buffer.SetData(new float[HeaderFloats]);
        var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = ObjectToWorldId, Value = 0x80000000u | HeaderBytes };
        metadata[1] = new MetadataValue { NameID = WorldToObjectId, Value = 0x80000000u | (uint)(HeaderBytes + total * 48) };
        metadata[2] = new MetadataValue { NameID = BaseColorId, Value = 0x80000000u | (uint)(HeaderBytes + total * 96) };
        batch = group.AddBatch(metadata, buffer.bufferHandle);
        metadata.Dispose();
        InstanceCount = 0;
    }

    /// <summary>
    /// The terrain's heights, and per texel of a map at half its layer resolution how much grass grows there and
    /// what colour the ground is, from the profile's layers.
    /// </summary>
    private static void ReadGround()
    {
        DisposeGround();
        var terrain = field.Terrain;
        var data = terrain.terrainData;
        var profile = field.Profile;
        var position = terrain.transform.position;

        var hr = data.heightmapResolution;
        var raw = data.GetHeights(0, 0, hr, hr);
        heights = new NativeArray<float>(hr * hr, Allocator.Persistent);
        for (var y = 0; y < hr; ++y) for (var x = 0; x < hr; ++x) heights[y * hr + x] = raw[y, x] * data.size.y + position.y;

        // per layer its colour (rgb) and how grassy it is (w)
        var layerCount = Mathf.Min(data.alphamapLayers, 8);
        if (data.alphamapLayers > 8) Debug.LogWarning("Grass reads the first 8 of " + data.alphamapLayers + " terrain layers.");
        var layers = new NativeArray<float4>(8, Allocator.TempJob);
        for (var l = 0; l < layerCount; ++l)
        {
            var settings = profile.LayerFor(data.terrainLayers[l]);
            // the shader works in linear colour, the profile's colours are as seen
            var colour = (settings != null ? settings.colour : Color.gray).linear;
            layers[l] = new float4(colour.r, colour.g, colour.b, settings != null ? settings.density : 0f);
        }

        // the layer weights are four to a texture; reading them is a copy, the blending a Burst job
        var ar = data.alphamapResolution;
        var mr = Mathf.Max(64, ar / 2);
        var empty = new NativeArray<Color32>(1, Allocator.TempJob);
        var splat0 = data.alphamapTextureCount > 0 ? new NativeArray<Color32>(data.GetAlphamapTexture(0).GetPixelData<Color32>(0), Allocator.TempJob) : empty;
        var splat1 = data.alphamapTextureCount > 1 ? new NativeArray<Color32>(data.GetAlphamapTexture(1).GetPixelData<Color32>(0), Allocator.TempJob) : empty;
        groundMap = new NativeArray<uint>(mr * mr, Allocator.Persistent);
        new GroundJob
        {
            splat0 = splat0,
            splat1 = splat1,
            layers = layers,
            layerCount = layerCount,
            alphaResolution = ar,
            mapResolution = mr,
            step = ar / mr,
            map = groundMap,
        }.Schedule(mr, 16).Complete();
        if (splat0 != empty) splat0.Dispose();
        if (splat1 != empty) splat1.Dispose();
        empty.Dispose();
        layers.Dispose();

        // the painted mask, neutral where nothing is painted
        var maskAsset = field.Mask;
        var maskSize = int2.zero;
        var maskMetres = 1f;
        if (maskAsset)
        {
            maskMetres = maskAsset.MetresPerTexel;
            maskSize = new int2(Mathf.CeilToInt(data.size.x / maskMetres), Mathf.CeilToInt(data.size.z / maskMetres));
            maskMap = new NativeArray<byte>(maskSize.x * maskSize.y, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < maskMap.Length; ++i) maskMap[i] = GrassMask.Neutral;
            foreach (var tile in maskAsset.Tiles)
            {
                for (var ty = 0; ty < GrassMask.TileSize; ++ty)
                {
                    var y = tile.y * GrassMask.TileSize + ty;
                    if (y < 0 || y >= maskSize.y) continue;
                    for (var tx = 0; tx < GrassMask.TileSize; ++tx)
                    {
                        var x = tile.x * GrassMask.TileSize + tx;
                        if (x >= 0 && x < maskSize.x) maskMap[y * maskSize.x + x] = tile.values[ty * GrassMask.TileSize + tx];
                    }
                }
            }
        }
        else
        {
            maskMap = new NativeArray<byte>(1, Allocator.Persistent);
        }

        ground = new Ground
        {
            origin = new float2(position.x, position.z),
            size = new float2(data.size.x, data.size.z),
            heightResolution = hr,
            mapResolution = mr,
            waterLevel = profile.waterLevel,
            minUp = math.cos(math.radians(profile.maxSlope)),
            tint = new float3(profile.tint.linear.r, profile.tint.linear.g, profile.tint.linear.b),
            jitter = profile.colourJitter,
            patches = profile.patches,
            maskMetres = maskMetres,
            maskSize = maskSize,
        };
    }

    /// <summary>
    /// One row of the ground map: per texel the layers under it blended into how grassy the ground is (low byte)
    /// and its colour (the other three), from a step by step block of the terrain's layer weights.
    /// </summary>
    [BurstCompile]
    private struct GroundJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> splat0;
        [ReadOnly] public NativeArray<Color32> splat1;
        [ReadOnly] public NativeArray<float4> layers;
        public int layerCount;
        public int alphaResolution;
        public int mapResolution;
        public int step;
        [NativeDisableParallelForRestriction] public NativeArray<uint> map;

        public void Execute(int my)
        {
            for (var mx = 0; mx < mapResolution; ++mx)
            {
                var sum = float4.zero;   // colour times weight, and grass times weight
                var weight = 0f;
                for (var dy = 0; dy < step; ++dy)
                {
                    for (var dx = 0; dx < step; ++dx)
                    {
                        var at = (my * step + dy) * alphaResolution + mx * step + dx;
                        for (var l = 0; l < layerCount; ++l)
                        {
                            var w = Weight(at, l);
                            if (w <= 0f) continue;
                            sum += layers[l] * w;
                            weight += w;
                        }
                    }
                }
                if (weight > 0f) sum /= weight;
                map[my * mapResolution + mx] = Byte(sum.w) | (Byte(sum.x) << 8) | (Byte(sum.y) << 16) | (Byte(sum.z) << 24);
            }
        }

        private float Weight(int at, int layer)
        {
            var texel = layer < 4 ? splat0[math.min(at, splat0.Length - 1)] : splat1[math.min(at, splat1.Length - 1)];
            var channel = layer & 3;
            var value = channel == 0 ? texel.r : channel == 1 ? texel.g : channel == 2 ? texel.b : texel.a;
            return value / 255f;
        }

        private static uint Byte(float v) => (uint)math.clamp((int)math.round(v * 255f), 0, 255);
    }

    /// <summary>
    /// Grows a cell: for each kind a jittered grid of points over the cell, each kept as often as the ground is
    /// grassy there, above the water and not too steep, with a random turn, size and rank, and the ground's colour.
    /// </summary>
    [BurstCompile]
    private struct GrowJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> slots;
        [ReadOnly] public NativeArray<int2> slotCell;
        [ReadOnly] public NativeArray<KindInfo> kinds;
        [ReadOnly] public NativeArray<float> heights;
        [ReadOnly] public NativeArray<uint> groundMap;
        [ReadOnly] public NativeArray<byte> maskMap;
        [ReadOnly] public NativeArray<Segment> segments;
        [ReadOnly] public NativeArray<int2> segmentRanges;
        [NativeDisableParallelForRestriction] public NativeArray<int> slotCounts;
        [NativeDisableParallelForRestriction] public NativeArray<float2> slotHeights;
        [NativeDisableParallelForRestriction] public NativeArray<float4> points;
        [NativeDisableParallelForRestriction] public NativeArray<float> upload;
        public int uploadInstances;
        public Ground ground;
        public float density;
        public int cellCapacity;
        public float tallest;

        public void Execute(int index)
        {
            var slot = slots[index];
            var cell = slotCell[slot];
            var roads = segmentRanges[index];
            var corner = (float2)cell * CellSize;
            var low = float.MaxValue;
            var high = float.MinValue;

            for (var k = 0; k < kinds.Length; ++k)
            {
                var kind = kinds[k];
                // the same cell and kind always grow the same grass
                var random = new Random(math.hash(new int3(cell, k)) | 1u);
                var side = (int)math.ceil(math.sqrt(kind.candidates));
                var spacing = CellSize / side;
                var chance = kind.candidates / (float)(side * side);
                var written = 0;
                for (var gz = 0; gz < side && written < kind.capacity; ++gz)
                {
                    for (var gx = 0; gx < side && written < kind.capacity; ++gx)
                    {
                        var at = corner + new float2(gx + random.NextFloat(), gz + random.NextFloat()) * spacing;
                        var keep = random.NextFloat();
                        var turn = random.NextFloat(0f, 2f * math.PI);
                        var size = math.lerp(kind.sizeMin, kind.sizeMax, random.NextFloat());
                        var rank = random.NextFloat();
                        var shade = 1f + (random.NextFloat() * 2f - 1f) * ground.jitter;

                        var groundAt = SampleGround(at);
                        groundAt.x = Painted(groundAt.x, SampleMask(at));
                        if (groundAt.x < kind.minDensity || keep > groundAt.x * chance) continue;
                        var height = SampleHeight(at, out var up);
                        if (height < ground.waterLevel || up < ground.minUp) continue;
                        if (OnRoad(at, roads)) continue;

                        var position = new float3(at.x, height - 0.03f * size, at.y);
                        points[slot * cellCapacity + kind.offset + written] = new float4(position, rank);
                        var u = index * cellCapacity + kind.offset + written;
                        WriteMatrices(u, position, turn, size);
                        // broad patches, some lighter and some drier, so a meadow is not one flat colour
                        var patch = Patches(at);
                        var colour = groundAt.yzw * ground.tint * shade * (1f + (patch.x - 0.5f) * ground.patches);
                        colour = math.lerp(colour, colour * new float3(1.12f, 1.04f, 0.7f), patch.y * ground.patches);
                        var c = uploadInstances * 24 + u * 4;
                        upload[c] = colour.x; upload[c + 1] = colour.y; upload[c + 2] = colour.z; upload[c + 3] = 1f;
                        low = math.min(low, height);
                        high = math.max(high, height);
                        written++;
                    }
                }
                slotCounts[slot * kinds.Length + k] = written;
            }
            slotHeights[slot] = low <= high ? new float2(low, high + tallest) : new float2(0f, 0f);
        }

        // turned about up and sized: the transform and its inverse, packed 3x4 as the instancing shaders read them
        private void WriteMatrices(int i, float3 p, float turn, float s)
        {
            math.sincos(turn, out var sin, out var cos);
            var o = i * 12;
            upload[o + 0] = cos * s; upload[o + 1] = 0f; upload[o + 2] = -sin * s;
            upload[o + 3] = 0f; upload[o + 4] = s; upload[o + 5] = 0f;
            upload[o + 6] = sin * s; upload[o + 7] = 0f; upload[o + 8] = cos * s;
            upload[o + 9] = p.x; upload[o + 10] = p.y; upload[o + 11] = p.z;

            var inv = 1f / s;
            var w = (uploadInstances + i) * 12;
            upload[w + 0] = cos * inv; upload[w + 1] = 0f; upload[w + 2] = sin * inv;
            upload[w + 3] = 0f; upload[w + 4] = inv; upload[w + 5] = 0f;
            upload[w + 6] = -sin * inv; upload[w + 7] = 0f; upload[w + 8] = cos * inv;
            upload[w + 9] = -(cos * p.x - sin * p.z) * inv;
            upload[w + 10] = -p.y * inv;
            upload[w + 11] = -(sin * p.x + cos * p.z) * inv;
        }

        // the mask over the terrain's own grass: below half it thins it out to none, above half it fills it in
        private static float Painted(float grass, float mask)
        {
            return mask < 0.5f ? grass * mask * 2f : math.lerp(grass, 1f, (mask - 0.5f) * 2f);
        }

        private float SampleMask(float2 at)
        {
            if (ground.maskSize.x == 0) return GrassMask.Neutral / 255f;
            var uv = (at - ground.origin) / ground.maskMetres - 0.5f;
            var i = (int2)math.floor(uv);
            var f = uv - i;
            var a = Mask(i);
            var b = Mask(i + new int2(1, 0));
            var c = Mask(i + new int2(0, 1));
            var d = Mask(i + new int2(1, 1));
            return math.lerp(math.lerp(a, b, f.x), math.lerp(c, d, f.x), f.y) / 255f;
        }

        private float Mask(int2 i)
        {
            i = math.clamp(i, 0, ground.maskSize - 1);
            return maskMap[i.y * ground.maskSize.x + i.x];
        }

        // two smooth noises over the ground, one patch every 12 m or so: brightness and dryness
        private static float2 Patches(float2 at)
        {
            return new float2(noise.snoise(at / 12f) * 0.5f + 0.5f, math.saturate(noise.snoise(at / 19f + 37f) * 0.7f + 0.2f));
        }

        private bool OnRoad(float2 at, int2 range)
        {
            for (var r = range.x; r < range.x + range.y; ++r)
            {
                var segment = segments[r];
                var along = segment.b - segment.a;
                var t = math.saturate(math.dot(at - segment.a, along) / math.max(math.dot(along, along), 1e-6f));
                if (math.distancesq(at, segment.a + along * t) < segment.radius * segment.radius) return true;
            }
            return false;
        }

        // x the grass density, yzw the ground colour, blended between the four texels round the point
        private float4 SampleGround(float2 at)
        {
            var r = ground.mapResolution;
            var uv = (at - ground.origin) / ground.size * r - 0.5f;
            var i = (int2)math.floor(uv);
            var f = uv - i;
            var a = Unpack(Texel(i, r));
            var b = Unpack(Texel(i + new int2(1, 0), r));
            var c = Unpack(Texel(i + new int2(0, 1), r));
            var d = Unpack(Texel(i + new int2(1, 1), r));
            return math.lerp(math.lerp(a, b, f.x), math.lerp(c, d, f.x), f.y);
        }

        private uint Texel(int2 i, int r)
        {
            i = math.clamp(i, 0, r - 1);
            return groundMap[i.y * r + i.x];
        }

        private static float4 Unpack(uint v) => new float4(v & 255u, (v >> 8) & 255u, (v >> 16) & 255u, (v >> 24) & 255u) / 255f;

        // the terrain height, blended, and how much the ground faces up
        private float SampleHeight(float2 at, out float up)
        {
            var r = ground.heightResolution;
            var cell = ground.size / (r - 1);
            var uv = (at - ground.origin) / cell;
            var i = (int2)math.floor(uv);
            var f = uv - i;
            var h00 = Height(i, r);
            var h10 = Height(i + new int2(1, 0), r);
            var h01 = Height(i + new int2(0, 1), r);
            var h11 = Height(i + new int2(1, 1), r);
            var dx = math.lerp(h10 - h00, h11 - h01, f.y) / cell.x;
            var dz = math.lerp(h01 - h00, h11 - h10, f.x) / cell.y;
            up = 1f / math.sqrt(dx * dx + dz * dz + 1f);
            return math.lerp(math.lerp(h00, h10, f.x), math.lerp(h01, h11, f.x), f.y);
        }

        private float Height(int2 i, int r)
        {
            i = math.clamp(i, 0, r - 1);
            return heights[i.y * r + i.x];
        }
    }

    private static JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext context, BatchCullingOutput output, IntPtr userContext)
    {
        if (buffer == null || cellSlot.Count == 0 || Suppressed) return default;
        // no shadows from grass, and nothing to pick in the Scene view
        if (context.viewType != BatchCullingViewType.Camera) return default;

        using (CullMarker.Auto())
        {
            var planes = new NativeArray<float4>(context.cullingPlanes.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (var p = 0; p < planes.Length; ++p)
            {
                var plane = context.cullingPlanes[p];
                planes[p] = new float4(plane.normal, plane.distance);
            }

            var meshes = new NativeArray<BatchMeshID>(kindMesh, Allocator.TempJob);
            var materials = new NativeArray<BatchMaterialID>(kindMaterial, Allocator.TempJob);
            var job = new CullJob
            {
                slotCell = slotCell,
                slotUsed = slotUsed,
                slotCounts = slotCounts,
                slotHeights = slotHeights,
                points = points,
                kinds = kinds,
                meshes = meshes,
                materials = materials,
                planes = planes,
                camera = context.lodParameters.cameraPosition,
                distance = distance,
                farFrom = distance * 0.5f,
                cellCapacity = cellCapacity,
                batch = batch,
                layer = (byte)(field ? field.gameObject.layer : 0),
                drawn = drawn,
                output = output.drawCommands,
            };
            // after the previous camera's: they share the drawn counts
            var handle = job.Schedule(culling);
            planes.Dispose(handle);
            meshes.Dispose(handle);
            materials.Dispose(handle);
            culling = JobHandle.CombineDependencies(culling, handle);
            return handle;
        }
    }

    /// <summary>
    /// Cells against the view, then per instance a thinning by distance (a high rank drops out sooner, so the
    /// grass gets sparser smoothly towards the end), and one draw command per kind.
    /// </summary>
    [BurstCompile]
    private struct CullJob : IJob
    {
        [ReadOnly] public NativeArray<int2> slotCell;
        [ReadOnly] public NativeArray<byte> slotUsed;
        [ReadOnly] public NativeArray<int> slotCounts;
        [ReadOnly] public NativeArray<float2> slotHeights;
        [ReadOnly] public NativeArray<float4> points;
        [ReadOnly] public NativeArray<KindInfo> kinds;
        [ReadOnly] public NativeArray<BatchMeshID> meshes;
        [ReadOnly] public NativeArray<BatchMaterialID> materials;
        [ReadOnly] public NativeArray<float4> planes;
        public float3 camera;
        public float distance;
        public float farFrom;
        public int cellCapacity;
        public BatchID batch;
        public byte layer;
        public NativeArray<int> drawn;
        public NativeArray<BatchCullingOutputDrawCommands> output;

        public void Execute()
        {
            var kindCount = kinds.Length;
            // per kind, the near ones and the far ones
            var lists = new NativeArray<UnsafeList<int>>(kindCount * 2, Allocator.Temp);
            for (var l = 0; l < lists.Length; ++l) lists[l] = new UnsafeList<int>(1024, Allocator.Temp);
            var farFrom2 = farFrom * farFrom;
            var near = distance * 0.4f;
            var far2 = distance * distance;

            for (var s = 0; s < slotUsed.Length; ++s)
            {
                if (slotUsed[s] == 0) continue;
                var heightsOf = slotHeights[s];
                var min = new float3(slotCell[s].x * CellSize, heightsOf.x, slotCell[s].y * CellSize);
                var max = new float3(min.x + CellSize, heightsOf.y, min.z + CellSize);
                if (!Visible(min, max)) continue;

                for (var k = 0; k < kindCount; ++k)
                {
                    var count = slotCounts[s * kindCount + k];
                    var first = s * cellCapacity + kinds[k].offset;
                    var nearList = lists[k * 2];
                    var farList = lists[k * 2 + 1];
                    for (var n = 0; n < count; ++n)
                    {
                        var point = points[first + n];
                        var offset = point.xyz - camera;
                        var d2 = math.dot(offset, offset);
                        if (d2 > far2) continue;
                        var d = math.sqrt(d2);
                        var keep = d <= near ? 1f : math.lerp(1f, 0.45f, (d - near) / (distance - near));
                        if (point.w > keep) continue;
                        if (d2 < farFrom2) nearList.Add(first + n);
                        else farList.Add(first + n);
                    }
                    lists[k * 2] = nearList;
                    lists[k * 2 + 1] = farList;
                }
            }

            var visible = 0;
            var commandCount = 0;
            for (var l = 0; l < lists.Length; ++l)
            {
                drawn[l] = lists[l].Length;
                visible += lists[l].Length;
                if (lists[l].Length > 0) commandCount++;
            }
            if (visible == 0) return;

            var commands = new BatchCullingOutputDrawCommands
            {
                drawCommandCount = commandCount,
                drawCommands = Malloc<BatchDrawCommand>(commandCount),
                drawRangeCount = commandCount,
                drawRanges = Malloc<BatchDrawRange>(commandCount),
                visibleInstanceCount = visible,
                visibleInstances = Malloc<int>(visible),
                drawCommandPickingEntityIds = null,
                instanceSortingPositions = null,
                instanceSortingPositionFloatCount = 0,
            };

            var offsetAt = 0;
            var command = 0;
            for (var l = 0; l < lists.Length; ++l)
            {
                var list = lists[l];
                if (list.Length == 0) continue;
                UnsafeUtility.MemCpy(commands.visibleInstances + offsetAt, list.Ptr, list.Length * sizeof(int));
                commands.drawCommands[command] = new BatchDrawCommand
                {
                    visibleOffset = (uint)offsetAt,
                    visibleCount = (uint)list.Length,
                    batchID = batch,
                    materialID = materials[l / 2],
                    meshID = meshes[l],
                    submeshIndex = 0,
                    splitVisibilityMask = 0xff,
                    flags = BatchDrawCommandFlags.None,
                    sortingPosition = 0,
                };
                commands.drawRanges[command] = new BatchDrawRange
                {
                    drawCommandsType = BatchDrawCommandType.Direct,
                    drawCommandsBegin = (uint)command,
                    drawCommandsCount = 1,
                    filterSettings = new BatchFilterSettings
                    {
                        renderingLayerMask = 0xffffffff,
                        layer = layer,
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = true,
                        staticShadowCaster = false,
                        allDepthSorted = false,
                    },
                };
                offsetAt += list.Length;
                command++;
            }
            output[0] = commands;
        }

        // the box against every plane: out only when it is wholly behind one
        private bool Visible(float3 min, float3 max)
        {
            for (var p = 0; p < planes.Length; ++p)
            {
                var plane = planes[p];
                var far = math.select(min, max, plane.xyz >= 0f);
                if (math.dot(plane.xyz, far) + plane.w < 0f) return false;
            }
            return true;
        }

        // freed by the render pipeline once it has drawn
        private static T* Malloc<T>(int n) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * math.max(1, n), UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
        }
    }

    private static void DisposeDrawing()
    {
        if (group != null)
        {
            if (buffer != null) group.RemoveBatch(batch);
            foreach (var id in new HashSet<BatchMeshID>(kindMesh)) group.UnregisterMesh(id);
            foreach (var id in kindMaterial) group.UnregisterMaterial(id);
        }
        kindMesh = Array.Empty<BatchMeshID>();
        kindMaterial = Array.Empty<BatchMaterialID>();
        buffer?.Dispose();
        buffer = null;
        foreach (var mesh in madeMeshes) if (mesh) UnityEngine.Object.DestroyImmediate(mesh);
        foreach (var material in madeMaterials) if (material) UnityEngine.Object.DestroyImmediate(material);
        madeMeshes.Clear();
        madeMaterials.Clear();
        if (kinds.IsCreated) kinds.Dispose();
        if (drawn.IsCreated) drawn.Dispose();
        kindTriangles = Array.Empty<int>();
        if (slotCell.IsCreated) slotCell.Dispose();
        if (slotUsed.IsCreated) slotUsed.Dispose();
        if (slotCounts.IsCreated) slotCounts.Dispose();
        if (slotHeights.IsCreated) slotHeights.Dispose();
        if (points.IsCreated) points.Dispose();
        cellSlot.Clear();
        freeSlots.Clear();
        slotCount = 0;
        InstanceCount = 0;
    }

    private static void DisposeGround()
    {
        if (heights.IsCreated) heights.Dispose();
        if (groundMap.IsCreated) groundMap.Dispose();
        if (maskMap.IsCreated) maskMap.Dispose();
    }

    private static void Dispose()
    {
        culling.Complete();
        culling = default;
        DisposeDrawing();
        DisposeGround();
        group?.Dispose();
        group = null;
        dirty = groundDirty = true;
    }
}
