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
        public List<Vector2> Samples = new List<Vector2>();
        public GameObject Object;
        public MeshRenderer Renderer;
        public Mesh Mesh;
        /// <summary>Runs through a building or the town hall: outlined red in build mode, never moved.</summary>
        public bool Flagged;
        /// <summary>Drawn red: the road being drawn when the village cannot pay for it.</summary>
        public bool Tinted;
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
                    var road = new Road { Id = info.Id, Type = info.Type, Points = ToPoints(info.Points) };
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

        var road = new Road { Id = result.Road.Id, Type = result.Road.Type, Points = ToPoints(result.Road.Points) };
        roads.Add(road);
        Rebuild(road);
        Changed?.Invoke();
        return (road, null);
    }

    /// <summary>Saves a changed road. On failure the road goes back to what the server has.</summary>
    public async Task<string> ChangeAsync(Road road, int type, IReadOnlyList<Vector2> points, IReadOnlyList<Vector2> before, int typeBefore)
    {
        var result = await gameManager.RavenNest.Village.ChangeRoadAsync(road.Id, type, ToArray(points));
        Apply(result);
        if (result == null || !result.Ok)
        {
            road.Type = typeBefore;
            road.Points = new List<Vector2>(before);
            Rebuild(road);
            Changed?.Invoke();
            return result?.Error ?? "unknown";
        }

        road.Type = result.Road.Type;
        road.Points = ToPoints(result.Road.Points);
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
    /// The length the server charges for: straight lines between the points. The drawn curve is a
    /// little longer round bends, but the price follows what is stored.
    /// </summary>
    public static float PaidLength(IReadOnlyList<Vector2> points)
    {
        var length = 0f;
        for (var i = 1; i < points.Count; ++i) length += Vector2.Distance(points[i - 1], points[i]);
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
        // the better road a little higher, so where two meet the cobbles lie over the dirt rather
        // than flickering with it
        RoadMesh.Build(road.Mesh, road.Samples, WidthOf(road.Type), GroundHeight, RoadMesh.Lift + Mathf.Clamp(road.Type, 0, 4) * 0.012f);
        road.Renderer.sharedMaterials = DrawMaterialsFor(road.Type);
        ApplyTint(road);
    }

    /// <summary>Marks a road as running through a building. Build mode outlines that stretch.</summary>
    public void SetFlagged(Road road, bool flagged)
    {
        road.Flagged = flagged;
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
