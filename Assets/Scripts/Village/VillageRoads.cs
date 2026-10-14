using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RavenNest.SDK.Endpoints;
using UnityEngine;

/// <summary>
/// The roads in the village: loaded from the server with the village, drawn for everyone to see,
/// and built, changed and removed through the server, which checks the type and charges for it.
/// Build mode draws and edits them with <see cref="VillageRoadTool"/>.
/// </summary>
public class VillageRoads : MonoBehaviour
{
    public sealed class Road
    {
        public Guid Id;
        public int Type;
        public List<Vector2> Points = new List<Vector2>();
        /// <summary>The width at each point, a multiple of the type's width (MinWidth to MaxWidth).</summary>
        public List<float> Widths = new List<float>();
        /// <summary>How round each end is: 1 a half circle, 0 a square end that stops at the point.</summary>
        public float StartRound = 1f;
        public float EndRound = 1f;
        public List<Vector2> Samples = new List<Vector2>();
        public GameObject Object;
        public MeshRenderer Renderer;
        public Mesh Mesh;
        /// <summary>Drawn red: the road being drawn when the village cannot pay for it.</summary>
        public bool Tinted;
    }

    /// <summary>The narrowest and widest a village road may be at a point, as the server allows.</summary>
    public const float MinWidth = 0.5f;
    public const float MaxWidth = 2f;

    /// <summary>
    /// A road's type, points, widths and ends, as a copy: what is saved, to compare with and go back to
    /// while it is being changed.
    /// </summary>
    public sealed class Shape
    {
        public int Type;
        public List<Vector2> Points;
        public List<float> Widths;
        public float StartRound;
        public float EndRound;

        public static Shape Of(Road road)
        {
            EnsureWidths(road);
            return new Shape { Type = road.Type, Points = new List<Vector2>(road.Points), Widths = new List<float>(road.Widths), StartRound = road.StartRound, EndRound = road.EndRound };
        }

        public void ApplyTo(Road road)
        {
            road.Type = Type;
            road.Points = new List<Vector2>(Points);
            road.Widths = new List<float>(Widths);
            road.StartRound = StartRound;
            road.EndRound = EndRound;
        }

        public bool Same(Road road)
        {
            if (road.Type != Type || Mathf.Abs(road.StartRound - StartRound) > 0.001f || Mathf.Abs(road.EndRound - EndRound) > 0.001f) return false;
            if (road.Points.Count != Points.Count || road.Widths.Count != Widths.Count) return false;
            for (var i = 0; i < Points.Count; ++i)
            {
                if ((road.Points[i] - Points[i]).sqrMagnitude > 0.0001f) return false;
            }
            for (var i = 0; i < Widths.Count; ++i)
            {
                if (Mathf.Abs(road.Widths[i] - Widths[i]) > 0.001f) return false;
            }
            return true;
        }
    }

    /// <summary>One width per point: 1 for any missing.</summary>
    public static void EnsureWidths(Road road)
    {
        while (road.Widths.Count < road.Points.Count) road.Widths.Add(1f);
        if (road.Widths.Count > road.Points.Count) road.Widths.RemoveRange(road.Points.Count, road.Widths.Count - road.Points.Count);
    }

    private readonly List<Road> roads = new List<Road>();
    private readonly Dictionary<int, Material> materials = new Dictionary<int, Material>();
    // the second pass of the road shader: only the middle, a queue later than every fringe
    private readonly Dictionary<int, Material[]> drawMaterials = new Dictionary<int, Material[]>();
    // made here rather than loaded, so ours to destroy, textures and all
    private readonly HashSet<Material> generated = new HashSet<Material>();
    private RoadType[] types = Array.Empty<RoadType>();
    private MaterialPropertyBlock block;
    private GameManager gameManager;
    private Transform root;

    public IReadOnlyList<Road> Roads => roads;
    public IReadOnlyList<RoadType> Types => types;
    public bool Loaded { get; private set; }

    /// <summary>Raised when the list of roads or of types changes.</summary>
    public event Action Changed;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        root = new GameObject("Village Roads").transform;
    }

    public RoadType TypeOf(int id)
    {
        foreach (var type in types) if (type.Id == id) return type;
        return null;
    }

    public float WidthOf(int type) => (float)(TypeOf(type)?.Width ?? 2.5);

    /// <summary>Fetches the road types and the village's roads, and draws them.</summary>
    public async Task LoadAsync()
    {
        var api = gameManager?.RavenNest?.Village;
        if (api == null) return;

        try
        {
            var loadedTypes = await api.GetRoadTypesAsync();
            if (loadedTypes != null) types = loadedTypes;

            var loaded = await api.GetRoadsAsync();
            if (loaded != null)
            {
                foreach (var road in roads) Destroy(road.Object);
                roads.Clear();
                foreach (var info in loaded)
                {
                    var road = FromInfo(info);
                    roads.Add(road);
                    Rebuild(road);
                }
            }

            Loaded = true;
            Changed?.Invoke();
        }
        catch (Exception exc)
        {
            // an older server without roads: there are simply none
            Shinobytes.Debug.LogWarning("Roads could not be loaded: " + exc.Message);
        }
    }

    // ---- Server ------------------------------------------------------------------------------

    /// <summary>Builds a new road. Null with the reason on failure.</summary>
    public async Task<(Road road, string error)> BuildAsync(int type, IReadOnlyList<Vector2> points)
    {
        var result = await gameManager.RavenNest.Village.BuildRoadAsync(type, ToArray(points));
        Apply(result);
        if (result == null || !result.Ok || result.Road == null) return (null, result?.Error ?? "unknown");

        var road = FromInfo(result.Road);
        roads.Add(road);
        Rebuild(road);
        Changed?.Invoke();
        return (road, null);
    }

    /// <summary>
    /// Saves a road as it is now: type, points, widths and ends. On failure it goes back to
    /// <paramref name="before"/>, what the server has.
    /// </summary>
    public async Task<string> ChangeAsync(Road road, Shape before)
    {
        EnsureWidths(road);
        var result = await gameManager.RavenNest.Village.ChangeRoadAsync(road.Id, road.Type, ToArray(road.Points), ToWidths(road.Widths), road.StartRound, road.EndRound);
        Apply(result);
        if (result == null || !result.Ok)
        {
            before.ApplyTo(road);
            Rebuild(road);
            Changed?.Invoke();
            return result?.Error ?? "unknown";
        }

        var saved = FromInfo(result.Road);
        road.Type = saved.Type;
        road.Points = saved.Points;
        road.Widths = saved.Widths;
        road.StartRound = saved.StartRound;
        road.EndRound = saved.EndRound;
        Rebuild(road);
        Changed?.Invoke();
        return null;
    }

    public async Task<string> RemoveAsync(Road road)
    {
        var result = await gameManager.RavenNest.Village.RemoveRoadAsync(road.Id);
        Apply(result);
        if (result == null || !result.Ok) return result?.Error ?? "unknown";

        roads.Remove(road);
        if (road.Object) Destroy(road.Object);
        Changed?.Invoke();
        return null;
    }

    /// <summary>The village bar shows the spend at once, rather than at the next update.</summary>
    private void Apply(RoadResult result)
    {
        if (result == null) return;
        var townHall = gameManager.Village ? gameManager.Village.TownHall : null;
        if (townHall) townHall.SetResources((long)result.Coins, (long)result.Ore, (long)result.Wood, (long)result.Fish, (long)result.Wheat);
    }

    /// <summary>What <paramref name="points"/> of road type <paramref name="type"/> costs, as the server counts it.</summary>
    public (long wood, long ore, long fish, long wheat, long coins) Cost(int type, float length)
    {
        var t = TypeOf(type);
        if (t == null) return default;
        long Of(double perMetre) => (long)Math.Ceiling(perMetre * length);
        return (Of(t.Wood), Of(t.Ore), Of(t.Fish), Of(t.Wheat), Of(t.Coins));
    }

    /// <summary>
    /// The length of the centre line as the server counts it: straight lines between the points. The
    /// drawn curve is a little longer round bends, but the price follows what is stored.
    /// </summary>
    public static float PaidLength(IReadOnlyList<Vector2> points)
    {
        var length = 0f;
        for (var i = 1; i < points.Count; ++i) length += Vector2.Distance(points[i - 1], points[i]);
        return length;
    }

    /// <summary>
    /// The length the server charges for once widths count: each stretch between two points times
    /// their average width, so a road twice as wide costs twice as much per metre.
    /// </summary>
    public static float PaidLength(IReadOnlyList<Vector2> points, IReadOnlyList<float> widths)
    {
        var length = 0f;
        for (var i = 1; i < points.Count; ++i)
        {
            var a = widths != null && i - 1 < widths.Count ? widths[i - 1] : 1f;
            var b = widths != null && i < widths.Count ? widths[i] : 1f;
            length += Vector2.Distance(points[i - 1], points[i]) * (a + b) / 2f;
        }
        return length;
    }

    // ---- Drawing -----------------------------------------------------------------------------

    /// <summary>Builds the mesh again after the points or the type changed.</summary>
    public void Rebuild(Road road)
    {
        if (!road.Object)
        {
            road.Object = new GameObject("Road " + road.Id);
            road.Object.transform.SetParent(root, false);
            var filter = road.Object.AddComponent<MeshFilter>();
            road.Renderer = road.Object.AddComponent<MeshRenderer>();
            road.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            road.Mesh = new Mesh { name = "Road" };
            filter.sharedMesh = road.Mesh;
        }

        road.Samples = RoadMesh.Sample(road.Points);
        EnsureWidths(road);
        // the better road a little higher, so where two meet the cobbles lie over the dirt rather
        // than flickering with it
        var sampleWidths = RoadMesh.SampleWidths(road.Points, road.Widths);
        RoadMesh.Build(road.Mesh, road.Samples, WidthOf(road.Type), sampleWidths, GroundHeight,
            RoadMesh.Lift + Mathf.Clamp(road.Type, 0, 4) * 0.012f, road.StartRound, road.EndRound);
        // no grass on the road; it grows back by itself once the road object is destroyed
        GrassRenderer.SetRoad(road.Object, road.Samples, WidthOf(road.Type), sampleWidths);
        road.Renderer.sharedMaterials = DrawMaterialsFor(road.Type);
        ApplyTint(road);
    }

    public void SetTinted(Road road, bool tinted)
    {
        if (road.Tinted == tinted) return;
        road.Tinted = tinted;
        ApplyTint(road);
    }

    private void ApplyTint(Road road)
    {
        if (!road.Renderer) return;
        if (block == null) block = new MaterialPropertyBlock();
        road.Renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", road.Tinted ? new Color(1f, 0.45f, 0.42f) : Color.white);
        road.Renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// What a road of the type draws with. With the road shader, its material and a copy that
    /// draws the middle only, later: a renderer with more materials than the mesh has parts draws
    /// the mesh once with each.
    /// </summary>
    private Material[] DrawMaterialsFor(int type)
    {
        if (drawMaterials.TryGetValue(type, out var existing) && existing[0]) return existing;
        var material = MaterialFor(type);
        Material[] made;
        if (material.HasProperty("_Core"))
        {
            var core = new Material(material) { name = material.name + " Middle" };
            core.SetFloat("_Core", 1f);
            core.renderQueue = material.renderQueue + 1;
            made = new[] { material, core };
        }
        else made = new[] { material };
        drawMaterials[type] = made;
        return made;
    }

    /// <summary>
    /// The material for a road type: Resources/Roads/Road_&lt;type&gt;, the road shader on the island's
    /// own terrain textures. Without one, a plain surface drawn here.
    /// </summary>
    public Material MaterialFor(int type)
    {
        if (materials.TryGetValue(type, out var existing) && existing) return existing;

        Material material;
        var asset = Resources.Load<Material>("Roads/Road_" + type);
        if (asset)
        {
            material = new Material(asset) { name = asset.name };
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = "Road " + type };
            var surface = RoadMesh.MakeSurface(type);
            material.SetTexture("_BaseMap", surface);
            material.mainTexture = surface;
            material.SetFloat("_Smoothness", 0.05f);
            // drawn after the ground, so the strip never flickers with the terrain under it
            material.renderQueue = 2010;
            generated.Add(material);
        }

        materials[type] = material;
        return material;
    }

    public float GroundHeight(Vector3 point)
    {
        var townHall = gameManager && gameManager.Village ? gameManager.Village.TownHall : null;
        return townHall ? townHall.GroundHeight(point) : PlacementUtility.FindGroundPoint(point).y;
    }

    private static Road FromInfo(RoadInfo info)
    {
        var road = new Road { Id = info.Id, Type = info.Type, Points = ToPoints(info.Points), StartRound = (float)info.StartRound, EndRound = (float)info.EndRound };
        if (info.Widths != null) foreach (var w in info.Widths) road.Widths.Add((float)w);
        EnsureWidths(road);
        return road;
    }

    /// <summary>The widths to send: none when they are all 1, as older roads have.</summary>
    private static double[] ToWidths(IReadOnlyList<float> widths)
    {
        var plain = true;
        foreach (var w in widths) if (Mathf.Abs(w - 1f) > 0.001f) plain = false;
        if (plain) return null;
        var flat = new double[widths.Count];
        for (var i = 0; i < widths.Count; ++i) flat[i] = Math.Round(widths[i], 2);
        return flat;
    }

    private static List<Vector2> ToPoints(double[] flat)
    {
        var points = new List<Vector2>();
        if (flat == null) return points;
        for (var i = 0; i + 1 < flat.Length; i += 2) points.Add(new Vector2((float)flat[i], (float)flat[i + 1]));
        return points;
    }

    private static double[] ToArray(IReadOnlyList<Vector2> points)
    {
        var flat = new double[points.Count * 2];
        for (var i = 0; i < points.Count; ++i)
        {
            flat[i * 2] = Math.Round(points[i].x, 2);
            flat[i * 2 + 1] = Math.Round(points[i].y, 2);
        }
        return flat;
    }

    private void OnDestroy()
    {
        foreach (var material in materials.Values)
        {
            if (!material) continue;
            // the textures of a loaded material are assets, not ours
            if (generated.Contains(material) && material.mainTexture) Destroy(material.mainTexture);
            Destroy(material);
        }
        foreach (var set in drawMaterials.Values)
        {
            for (var i = 1; i < set.Length; ++i) if (set[i]) Destroy(set[i]);
        }
        foreach (var road in roads)
        {
            if (road.Mesh) Destroy(road.Mesh);
        }
        if (root) Destroy(root.gameObject);
    }
}
