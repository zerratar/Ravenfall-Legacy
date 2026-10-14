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
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws every <see cref="ResourceField"/> in one <see cref="BatchRendererGroup"/>: one GPU buffer with a
/// transform per instance, and per camera (and per shadow pass) a pass over the instances that culls them to
/// the view, picks each one's level of detail by distance and its look by state (standing, or its stump), and
/// draws the lot instanced, grouped by mesh and material. No GameObject per tree, and it runs in the Scene view
/// without the game running.
/// </summary>
/// <remarks>
/// The buffer is built again only when fields or types change (painting in the editor, a field loading); at
/// runtime only the standing/used-up state changes, one byte per instance kept here. The culling is a Burst job
/// the render pipeline waits on only when it draws, so it runs off the main thread.
/// </remarks>
public static unsafe class ResourceRenderer
{
    private const int HeaderBytes = 64;          // a zero matrix in front, as BatchRendererGroup expects
    private const int MatrixBytes = 48;          // a packed 3x4 matrix
    private const float ReferenceTanHalfFov = 0.57735f;   // tan(30 degrees): LOD distances are at a 60 degree view

    // in the Profiler as their own entries, under Render; the culling itself shows as CullJob on a worker
    private static readonly ProfilerMarker RebuildMarker = new ProfilerMarker(ProfilerCategory.Render, "ResourceRenderer.Rebuild");
    private static readonly ProfilerMarker CullMarker = new ProfilerMarker(ProfilerCategory.Render, "ResourceRenderer.Cull");

    private static readonly int ObjectToWorldId = Shader.PropertyToID("unity_ObjectToWorld");
    private static readonly int WorldToObjectId = Shader.PropertyToID("unity_WorldToObject");

    private static BatchRendererGroup group;
    private static GraphicsBuffer buffer;
    private static BatchID batch;
    private static bool dirty = true;
    private static bool hooked;
    // the culling jobs in flight, finished before anything they read changes
    private static JobHandle culling;

    private static readonly List<ResourceField> fields = new List<ResourceField>();
    private static readonly Dictionary<ResourceField, int> fieldStart = new Dictionary<ResourceField, int>();
    private static readonly Dictionary<Mesh, BatchMeshID> meshIds = new Dictionary<Mesh, BatchMeshID>();
    private static readonly Dictionary<Material, BatchMaterialID> materialIds = new Dictionary<Material, BatchMaterialID>();

    // what can be drawn: one key per mesh, submesh, material, shadow setting and layer
    private struct DrawKey
    {
        public BatchMeshID mesh;
        public BatchMaterialID material;
        public ushort submesh;
        public ShadowCastingMode shadows;
        public byte layer;
    }
    private static readonly List<DrawKey> keys = new List<DrawKey>();
    private static readonly Dictionary<(int, int, int, int, int), int> keyLookup = new Dictionary<(int, int, int, int, int), int>();

    // a look's levels of detail; each level has its draw keys (one per submesh) in lodKeys
    private struct LodInfo { public int firstKey; public int keyCount; public float maxDistance; }
    private static readonly List<LodInfo> lods = new List<LodInfo>();
    private static readonly List<int> lodKeys = new List<int>();
    private struct LookInfo { public int firstLod; public int lodCount; }
    private static readonly List<LookInfo> looks = new List<LookInfo>();
    private static readonly Dictionary<(ResourceType, int, byte), int> lookLookup = new Dictionary<(ResourceType, int, byte), int>();

    // what the culling job reads, in buffer order, made when the buffer is
    private static int count;
    private static NativeArray<float3> centres;
    private static NativeArray<float> radii;
    private static NativeArray<int> standingLook;
    private static NativeArray<int> depletedLook;
    private static NativeArray<LookInfo> nativeLooks;
    private static NativeArray<LodInfo> nativeLods;
    private static NativeArray<int> nativeLodKeys;
    private static NativeArray<DrawKey> nativeKeys;
    // whether each instance is used up, written by the game; each cull takes a copy
    private static byte[] depleted = Array.Empty<byte>();

    /// <summary>How many instances are drawn, for the field inspectors and the profiler numbers.</summary>
    public static int InstanceCount => count;

    public static void Register(ResourceField field)
    {
        if (!field || fields.Contains(field)) return;
        fields.Add(field);
        Hook();
        dirty = true;
    }

    public static void Unregister(ResourceField field)
    {
        if (fields.Remove(field)) dirty = true;
    }

    public static void MarkDirty() => dirty = true;

    /// <summary>Shows an instance standing or used up (its stump, or nothing) from the next frame on.</summary>
    public static void SetDepleted(ResourceField field, int index, bool value)
    {
        if (dirty || !fieldStart.TryGetValue(field, out var start)) return;   // the rebuild reads it from the field
        var at = start + index;
        if (at >= 0 && at < count) depleted[at] = value ? (byte)1 : (byte)0;
    }

    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        RenderPipelineManager.beginContextRendering += BeforeRendering;
        ResourceType.Changed += _ => dirty = true;
        Application.quitting += Dispose;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        // an undone or redone brush stroke
        UnityEditor.Undo.undoRedoPerformed += MarkDirty;
#endif
    }

    private static void BeforeRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        // last frame's culling is long done; start the new frame's list
        culling.Complete();
        culling = default;
        if (!dirty) return;
        dirty = false;
        using (RebuildMarker.Auto()) Rebuild();
    }

    /// <summary>Everything again: the meshes and materials, the draw keys, the looks and the instance buffer.</summary>
    private static void Rebuild()
    {
        culling.Complete();
        fields.RemoveAll(f => !f);
        if (group == null)
        {
            group = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            // the fields cover the world; culling is done per instance in the callback
            group.SetGlobalBounds(new Bounds(Vector3.zero, Vector3.one * 100000f));
        }

        if (buffer != null)
        {
            group.RemoveBatch(batch);
            buffer.Dispose();
            buffer = null;
        }
        foreach (var id in meshIds.Values) group.UnregisterMesh(id);
        foreach (var id in materialIds.Values) group.UnregisterMaterial(id);
        meshIds.Clear();
        materialIds.Clear();
        keys.Clear();
        keyLookup.Clear();
        lods.Clear();
        lodKeys.Clear();
        looks.Clear();
        lookLookup.Clear();
        fieldStart.Clear();
        DisposeNative();

        count = 0;
        foreach (var field in fields) if (field.isActiveAndEnabled) count += field.Count;
        if (count == 0) return;

        centres = new NativeArray<float3>(count, Allocator.Persistent);
        radii = new NativeArray<float>(count, Allocator.Persistent);
        standingLook = new NativeArray<int>(count, Allocator.Persistent);
        depletedLook = new NativeArray<int>(count, Allocator.Persistent);
        depleted = new byte[count];

        var matrices = new float[(HeaderBytes + count * MatrixBytes * 2) / 4];
        var n = 0;
        foreach (var field in fields)
        {
            if (!field.isActiveAndEnabled) continue;
            fieldStart[field] = n;
            var layer = (byte)field.gameObject.layer;
            for (var i = 0; i < field.Count; ++i)
            {
                var instance = field.Instances[i];
                var type = field.TypeOf(i);
                var matrix = instance.Matrix;
                var variant = type != null && type.variants.Length > 0 ? Mathf.Clamp(instance.variant, 0, type.variants.Length - 1) : -1;
                standingLook[n] = type != null && variant >= 0 ? LookIndex(type, variant, layer) : -1;
                depletedLook[n] = type != null ? LookIndex(type, -1, layer) : -1;
                depleted[n] = field.IsDepleted(i) ? (byte)1 : (byte)0;

                // a sphere round the standing look, for culling; the stump fits inside it
                var bounds = type != null && variant >= 0 && !type.variants[variant].IsEmpty ? type.variants[variant].lods[0].mesh.bounds : new Bounds(Vector3.zero, Vector3.one);
                centres[n] = matrix.MultiplyPoint3x4(bounds.center);
                radii[n] = bounds.extents.magnitude * (instance.scale > 0f ? instance.scale : 1f);

                WritePacked(matrices, (HeaderBytes + n * MatrixBytes) / 4, matrix);
                WritePacked(matrices, (HeaderBytes + (count + n) * MatrixBytes) / 4, matrix.inverse);
                n++;
            }
        }

        nativeLooks = ToNative(looks);
        nativeLods = ToNative(lods);
        nativeLodKeys = ToNative(lodKeys);
        nativeKeys = ToNative(keys);

        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, matrices.Length, 4);
        buffer.SetData(matrices);
        var metadata = new NativeArray<MetadataValue>(2, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = ObjectToWorldId, Value = 0x80000000u | HeaderBytes };
        metadata[1] = new MetadataValue { NameID = WorldToObjectId, Value = 0x80000000u | (uint)(HeaderBytes + count * MatrixBytes) };
        batch = group.AddBatch(metadata, buffer.bufferHandle);
        metadata.Dispose();
    }

    /// <summary>The look of a type (a variant, or -1 for its depleted look) with its levels of detail and draw keys, or -1 when it draws nothing.
    /// Per layer, since the draw keys carry the layer of the field.</summary>
    private static int LookIndex(ResourceType type, int variant, byte layer)
    {
        if (lookLookup.TryGetValue((type, variant, layer), out var known)) return known;

        var look = variant < 0 ? type.depleted : type.variants[variant];
        if (look == null || look.IsEmpty)
        {
            lookLookup[(type, variant, layer)] = -1;
            return -1;
        }

        var info = new LookInfo { firstLod = lods.Count };
        foreach (var lod in look.lods)
        {
            if (lod == null || !lod.mesh) continue;
            var lodInfo = new LodInfo { firstKey = lodKeys.Count, maxDistance = lod.maxDistance };
            for (var s = 0; s < lod.mesh.subMeshCount; ++s)
            {
                var material = lod.materials != null && lod.materials.Length > 0 ? lod.materials[Mathf.Min(s, lod.materials.Length - 1)] : null;
                if (!material) continue;
                lodKeys.Add(KeyIndex(lod.mesh, material, s, type.castShadows, layer));
                lodInfo.keyCount++;
            }
            lods.Add(lodInfo);
            info.lodCount++;
        }

        looks.Add(info);
        lookLookup[(type, variant, layer)] = looks.Count - 1;
        return looks.Count - 1;
    }

    private static int KeyIndex(Mesh mesh, Material material, int submesh, ShadowCastingMode shadows, byte layer)
    {
        if (!meshIds.TryGetValue(mesh, out var meshId)) meshIds[mesh] = meshId = group.RegisterMesh(mesh);
        if (!materialIds.TryGetValue(material, out var materialId)) materialIds[material] = materialId = group.RegisterMaterial(material);
        var lookup = ((int)meshId.value, (int)materialId.value, submesh, (int)shadows, (int)layer);
        if (keyLookup.TryGetValue(lookup, out var index)) return index;
        keys.Add(new DrawKey { mesh = meshId, material = materialId, submesh = (ushort)submesh, shadows = shadows, layer = layer });
        keyLookup[lookup] = keys.Count - 1;
        return keys.Count - 1;
    }

    private static JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext context, BatchCullingOutput output, IntPtr userContext)
    {
        if (count == 0 || buffer == null) return default;
        if (context.viewType == BatchCullingViewType.Picking || context.viewType == BatchCullingViewType.SelectionOutline) return default;

        using (CullMarker.Auto())
        {
            var shadowPass = context.viewType == BatchCullingViewType.Light;
            var lod = context.lodParameters;

            // the view's planes, and this frame's standing state, for the job to own
            var planes = new NativeArray<float4>(shadowPass ? 0 : context.cullingPlanes.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (var p = 0; p < planes.Length; ++p)
            {
                var plane = context.cullingPlanes[p];
                planes[p] = new float4(plane.normal, plane.distance);
            }
            var state = new NativeArray<byte>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<byte>.Copy(depleted, state, count);

            var job = new CullJob
            {
                centres = centres,
                radii = radii,
                standingLook = standingLook,
                depletedLook = depletedLook,
                depleted = state,
                planes = planes,
                looks = nativeLooks,
                lods = nativeLods,
                lodKeys = nativeLodKeys,
                keys = nativeKeys,
                batch = batch,
                shadowPass = shadowPass,
                camera = lod.cameraPosition,
                // distances as if seen at 60 degrees, so a zoomed in camera keeps detail further out
                lodScale = lod.isOrthographic ? 1f : Mathf.Tan(lod.fieldOfView * 0.5f * Mathf.Deg2Rad) / ReferenceTanHalfFov,
                shadowDistance = ShadowDistance(),
                output = output.drawCommands,
            };
            var handle = job.Schedule();
            planes.Dispose(handle);
            state.Dispose(handle);
            culling = JobHandle.CombineDependencies(culling, handle);
            return handle;
        }
    }

    /// <summary>
    /// Culls every instance to the view (or, for shadows, to the shadow distance), picks each one's level of
    /// detail, and writes one draw command per mesh, submesh and material that has any instances to draw.
    /// </summary>
    [BurstCompile]
    private struct CullJob : IJob
    {
        [ReadOnly] public NativeArray<float3> centres;
        [ReadOnly] public NativeArray<float> radii;
        [ReadOnly] public NativeArray<int> standingLook;
        [ReadOnly] public NativeArray<int> depletedLook;
        [ReadOnly] public NativeArray<byte> depleted;
        [ReadOnly] public NativeArray<float4> planes;
        [ReadOnly] public NativeArray<LookInfo> looks;
        [ReadOnly] public NativeArray<LodInfo> lods;
        [ReadOnly] public NativeArray<int> lodKeys;
        [ReadOnly] public NativeArray<DrawKey> keys;
        public BatchID batch;
        public bool shadowPass;
        public float3 camera;
        public float lodScale;
        public float shadowDistance;
        public NativeArray<BatchCullingOutputDrawCommands> output;

        public void Execute()
        {
            var n = centres.Length;
            var chosenLod = new NativeArray<int>(n, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var keyCounts = new NativeArray<int>(keys.Length, Allocator.Temp);
            var visible = 0;
            for (var i = 0; i < n; ++i)
            {
                chosenLod[i] = -1;
                var look = depleted[i] != 0 ? depletedLook[i] : standingLook[i];
                if (look < 0) continue;

                var centre = centres[i];
                var radius = radii[i];
                var distance = math.distance(camera, centre);
                if (shadowPass)
                {
                    // shadow cascades come as one set of planes per split; the caster distance is enough to keep
                    // far trees out of the shadow maps
                    if (distance - radius > shadowDistance) continue;
                }
                else
                {
                    var inside = true;
                    for (var p = 0; p < planes.Length && inside; ++p)
                    {
                        var plane = planes[p];
                        inside = math.dot(plane.xyz, centre) + plane.w >= -radius;
                    }
                    if (!inside) continue;
                }

                var info = looks[look];
                var seen = distance * lodScale;
                var chosen = -1;
                for (var l = 0; l < info.lodCount; ++l)
                {
                    var d = lods[info.firstLod + l].maxDistance;
                    if (d <= 0f || seen <= d)
                    {
                        chosen = info.firstLod + l;
                        break;
                    }
                }
                if (chosen < 0) continue;

                chosenLod[i] = chosen;
                var lodInfo = lods[chosen];
                for (var k = 0; k < lodInfo.keyCount; ++k) keyCounts[lodKeys[lodInfo.firstKey + k]]++;
                visible += lodInfo.keyCount;
            }

            if (visible == 0) return;

            var commandCount = 0;
            for (var k = 0; k < keys.Length; ++k) if (keyCounts[k] > 0) commandCount++;

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

            // where each key's instances start in the visible list
            var keyFill = new NativeArray<int>(keys.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var offset = 0;
            var command = 0;
            for (var k = 0; k < keys.Length; ++k)
            {
                keyFill[k] = offset;
                if (keyCounts[k] == 0) continue;
                var key = keys[k];
                commands.drawCommands[command] = new BatchDrawCommand
                {
                    visibleOffset = (uint)offset,
                    visibleCount = (uint)keyCounts[k],
                    batchID = batch,
                    materialID = key.material,
                    meshID = key.mesh,
                    submeshIndex = key.submesh,
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
                        layer = key.layer,
                        shadowCastingMode = key.shadows,
                        receiveShadows = true,
                        staticShadowCaster = false,
                        allDepthSorted = false,
                    },
                };
                offset += keyCounts[k];
                command++;
            }

            for (var i = 0; i < n; ++i)
            {
                var chosen = chosenLod[i];
                if (chosen < 0) continue;
                var lodInfo = lods[chosen];
                for (var k = 0; k < lodInfo.keyCount; ++k)
                {
                    var key = lodKeys[lodInfo.firstKey + k];
                    commands.visibleInstances[keyFill[key]] = i;
                    keyFill[key] = keyFill[key] + 1;
                }
            }

            output[0] = commands;
        }

        // freed by the render pipeline once it has drawn
        private static T* Malloc<T>(int n) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * math.max(1, n), UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
        }
    }

    private static float ShadowDistance()
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        return asset ? asset.shadowDistance : QualitySettings.shadowDistance;
    }

    // the packed 3x4 layout the instancing shaders read: three rows of each column, column by column
    private static void WritePacked(float[] into, int at, Matrix4x4 m)
    {
        into[at + 0] = m.m00; into[at + 1] = m.m10; into[at + 2] = m.m20;
        into[at + 3] = m.m01; into[at + 4] = m.m11; into[at + 5] = m.m21;
        into[at + 6] = m.m02; into[at + 7] = m.m12; into[at + 8] = m.m22;
        into[at + 9] = m.m03; into[at + 10] = m.m13; into[at + 11] = m.m23;
    }

    private static NativeArray<T> ToNative<T>(List<T> list) where T : unmanaged
    {
        var array = new NativeArray<T>(Mathf.Max(1, list.Count), Allocator.Persistent);
        for (var i = 0; i < list.Count; ++i) array[i] = list[i];
        return array;
    }

    private static void DisposeNative()
    {
        if (centres.IsCreated) centres.Dispose();
        if (radii.IsCreated) radii.Dispose();
        if (standingLook.IsCreated) standingLook.Dispose();
        if (depletedLook.IsCreated) depletedLook.Dispose();
        if (nativeLooks.IsCreated) nativeLooks.Dispose();
        if (nativeLods.IsCreated) nativeLods.Dispose();
        if (nativeLodKeys.IsCreated) nativeLodKeys.Dispose();
        if (nativeKeys.IsCreated) nativeKeys.Dispose();
    }

    private static void Dispose()
    {
        culling.Complete();
        culling = default;
        if (group != null)
        {
            if (buffer != null) group.RemoveBatch(batch);
            foreach (var id in meshIds.Values) group.UnregisterMesh(id);
            foreach (var id in materialIds.Values) group.UnregisterMaterial(id);
            group.Dispose();
            group = null;
        }
        buffer?.Dispose();
        buffer = null;
        DisposeNative();
        meshIds.Clear();
        materialIds.Clear();
        fieldStart.Clear();
        count = 0;
        dirty = true;
    }
}
