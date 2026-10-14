using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The trodden ground round a village house: a patch laid over the terrain wherever the house stands, drawn
/// with the road shader and the island's own terrain textures, worn in the middle and fading raggedly into the
/// grass at its edge. It replaces the flat dirt pad the house models used to carry, which sat on the ground like
/// a plate. houses_prefabs.cs adds one for each YARD marker in a house model.
/// </summary>
/// <remarks>
/// The patch is built again whenever the house moves (build mode moves plots), so it always follows the ground
/// under it. As with <see cref="WorldRoad"/>, the mesh lives on a hidden child that is never saved. Yards draw
/// just before roads, so a road across a yard lies on top of it.
/// </remarks>
[ExecuteAlways]
public class HouseYard : MonoBehaviour
{
    private const int Segments = 36;
    private const int Rings = 7;
    private const float Lift = 0.05f;

    [Tooltip("Half the yard's size along x and along z, in metres, at its ragged edge (where it has faded out).")]
    [SerializeField] private Vector2 radius = new Vector2(4.6f, 4.4f);
    [Tooltip("How ragged the edge is, 0 for a smooth oval.")]
    [SerializeField, Range(0f, 0.4f)] private float jag = 0.12f;
    [Tooltip("Changes the shape of the ragged edge, so neighbouring yards differ.")]
    [SerializeField] private float seed = 1f;
    [Tooltip("The ground: dirt or gravel, the road surfaces.")]
    [SerializeField] private WorldRoad.Surface surface = WorldRoad.Surface.Dirt;

    private GameObject view;
    private Mesh mesh;
    private Vector3 builtAt = new Vector3(float.NaN, 0f, 0f);
    private Quaternion builtTurn;

    private static readonly Dictionary<int, Material> materials = new Dictionary<int, Material>();

    public void Configure(Vector2 halfSize, float raggedness, float shapeSeed, WorldRoad.Surface ground)
    {
        radius = halfSize;
        jag = raggedness;
        seed = shapeSeed;
        surface = ground;
        Rebuild();
    }

    private void OnEnable() => Rebuild();

    private void OnDisable()
    {
        if (view) DestroyNow(view);
        view = null;
        builtAt = new Vector3(float.NaN, 0f, 0f);
    }

    private void OnDestroy()
    {
        if (mesh) DestroyNow(mesh);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () => { if (this && isActiveAndEnabled) Rebuild(); };
    }
#endif

    private void LateUpdate()
    {
        // a plot moved in build mode: lay the yard on the ground where it is now
        if (transform.position != builtAt || transform.rotation != builtTurn) Rebuild();
    }

    public void Rebuild()
    {
        if (!isActiveAndEnabled) return;
        EnsureView();
        builtAt = transform.position;
        builtTurn = transform.rotation;

        // rings from the middle out to the ragged rim; the shader blends by the first coordinate, 0 in the
        // middle and 1 at the rim, as it does across a road
        var vertices = new List<Vector3>(1 + Segments * Rings);
        var uvs = new List<Vector2>(vertices.Capacity);
        var toLocal = view.transform.worldToLocalMatrix;
        vertices.Add(toLocal.MultiplyPoint3x4(Ground(transform.position)));
        uvs.Add(Vector2.zero);
        for (var ring = 1; ring <= Rings; ++ring)
        {
            var t = ring / (float)Rings;
            for (var i = 0; i < Segments; ++i)
            {
                var a = Mathf.PI * 2f * i / Segments;
                var k = 1f + (Mathf.Sin(a * 3f + seed) * 0.5f + Mathf.Sin(a * 7f + seed * 2f) * 0.5f) * jag;
                var local = new Vector3(Mathf.Cos(a) * radius.x * k * t, 0f, Mathf.Sin(a) * radius.y * k * t);
                var world = transform.TransformPoint(local);
                vertices.Add(toLocal.MultiplyPoint3x4(Ground(world)));
                uvs.Add(new Vector2(t, 0f));
            }
        }

        var triangles = new List<int>(Segments * 3 + (Rings - 1) * Segments * 6);
        for (var i = 0; i < Segments; ++i)
        {
            Face(triangles, vertices, 0, 1 + i, 1 + (i + 1) % Segments);
        }
        for (var ring = 1; ring < Rings; ++ring)
        {
            var inner = 1 + (ring - 1) * Segments;
            var outer = 1 + ring * Segments;
            for (var i = 0; i < Segments; ++i)
            {
                var j = (i + 1) % Segments;
                Face(triangles, vertices, inner + i, outer + i, outer + j);
                Face(triangles, vertices, inner + i, outer + j, inner + j);
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var renderer = view.GetComponent<MeshRenderer>();
        var material = MaterialFor(surface);
        renderer.sharedMaterials = material ? new[] { material } : new Material[0];
    }

    /// <summary>
    /// Adds a triangle that faces up, whichever way round its corners came: clockwise seen from above, which
    /// Unity draws as the front.
    /// </summary>
    private static void Face(List<int> triangles, List<Vector3> vertices, int a, int b, int c)
    {
        var clockwise = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).y > 0f;
        triangles.Add(a);
        if (clockwise) { triangles.Add(b); triangles.Add(c); }
        else { triangles.Add(c); triangles.Add(b); }
    }

    /// <summary>
    /// The ground under a point: the terrain where there is one, else the highest collider below that is not
    /// this house, a plot or a road, so the yard never lies over the house it belongs to.
    /// </summary>
    private Vector3 Ground(Vector3 point)
    {
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (!terrain || !terrain.terrainData) continue;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
            point.y = terrain.SampleHeight(point) + origin.y + Lift;
            return point;
        }

        var own = transform.root;
        var best = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(new Ray(new Vector3(point.x, point.y + 500f, point.z), Vector3.down), 2000f, ~0, QueryTriggerInteraction.Ignore))
        {
            var t = hit.collider.transform;
            if (t.root == own || hit.collider.GetComponentInParent<TownHouseSlot>() || hit.collider.GetComponentInParent<WorldRoad>()) continue;
            best = Mathf.Max(best, hit.point.y);
        }
        point.y = (float.IsNegativeInfinity(best) ? transform.position.y : best) + Lift;
        return point;
    }

    private void EnsureView()
    {
        if (!mesh) mesh = new Mesh { name = "House Yard", hideFlags = HideFlags.DontSave };
        if (view) return;
        view = new GameObject("Yard Surface") { hideFlags = HideFlags.HideAndDontSave };
        view.transform.SetParent(transform, false);
        view.layer = gameObject.layer;
        view.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = view.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>
    /// The road material for the surface, tuned for a wide patch: the worn surface over most of it, the shoulder
    /// texture towards the edge, and a long, uneven fade. Drawn just before the roads.
    /// </summary>
    private static Material MaterialFor(WorldRoad.Surface surface)
    {
        var key = (int)surface;
        if (materials.TryGetValue(key, out var made) && made) return made;
        var source = Resources.Load<Material>("Roads/Road_" + key);
        if (!source) return null;
        made = new Material(source) { name = source.name + " Yard", hideFlags = HideFlags.HideAndDontSave };
        if (made.HasProperty("_Shoulder"))
        {
            made.SetFloat("_Shoulder", 0.5f);
            made.SetFloat("_ShoulderSoftness", 0.22f);
            made.SetFloat("_Fade", 0.62f);
            made.SetFloat("_FadeWidth", 0.3f);
            made.SetFloat("_Ragged", 0.14f);
            made.SetFloat("_NoiseScale", 0.45f);
        }
        made.renderQueue = source.renderQueue - 2;
        materials[key] = made;
        return made;
    }

    private static void DestroyNow(Object target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
