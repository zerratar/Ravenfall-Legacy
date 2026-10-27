using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A glowing crack in the ground (Ironhill's dragon country, the dwarf hold): a jagged seam of lava with scorched edges
/// and a few branches, laid on the terrain under it and drawn with Ravenfall/Lava Crack. It runs from this object
/// along its forward; the same seed always gives the same crack.
/// </summary>
/// <remarks>
/// As <see cref="SwampPool"/>: the mesh lives on a hidden child made whenever the crack is enabled or changed and never
/// saved, so the scene keeps only the numbers. Placed by tools/ironhill/ih-cracks.cs.
/// </remarks>
[ExecuteAlways]
public class LavaCrack : MonoBehaviour
{
    private const string SurfaceName = "Lava Crack Surface";
    private const float Step = 0.35f;
    private const float Lift = 0.05f;
    private static readonly float[] Across = { -1f, -0.5f, 0f, 0.5f, 1f };

    [SerializeField] private Material material;
    [Tooltip("Which crack: the same seed always draws the same one.")]
    [SerializeField] private int seed = 1;
    [Tooltip("The main seam's length in metres.")]
    [SerializeField, Min(1f)] private float length = 8f;
    [Tooltip("The open seam's width at its widest, in metres.")]
    [SerializeField, Min(0.05f)] private float width = 0.35f;
    [Tooltip("How far the scorched ground reaches either side, in metres, at the widest.")]
    [SerializeField, Min(0.2f)] private float scorch = 1.4f;
    [Tooltip("Branches off the main seam (each may fork once more).")]
    [SerializeField, Range(0, 8)] private int branches = 3;
    [Tooltip("How hot it glows: 1 for a live seam, lower for one cooling down.")]
    [SerializeField, Range(0f, 2f)] private float heat = 1f;

    private GameObject surface;
    private Mesh mesh;
    private bool started;

    public void Set(Material material, int seed, float length, float width, float scorch, int branches, float heat)
    {
        this.material = material;
        this.seed = seed;
        this.length = Mathf.Max(1f, length);
        this.width = Mathf.Max(0.05f, width);
        this.scorch = Mathf.Max(0.2f, scorch);
        this.branches = Mathf.Clamp(branches, 0, 8);
        this.heat = Mathf.Clamp(heat, 0f, 2f);
        Build();
    }

    // in play the terrains may wake after this does, so the crack waits for Start to find the ground
    private void OnEnable()
    {
        if (!Application.isPlaying || started) Build();
    }

    private void Start()
    {
        started = true;
        if (Application.isPlaying) Build();
    }

    private void OnDisable()
    {
        if (surface) DestroyNow(surface);
        if (mesh) DestroyNow(mesh);
        surface = null;
        mesh = null;
    }

    private void OnValidate()
    {
        // OnValidate may not change objects in the scene; build on the next editor tick
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += () => { if (this && isActiveAndEnabled) Build(); };
#endif
    }

    public void Build()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name == SurfaceName && c.gameObject != surface) DestroyNow(c.gameObject);
        }
        if (!material) return;

        if (!surface)
        {
            surface = new GameObject(SurfaceName) { hideFlags = HideFlags.HideAndDontSave };
            surface.layer = gameObject.layer;
            surface.transform.SetParent(transform, false);
            surface.AddComponent<MeshFilter>();
            var r = surface.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = Vector3.one;
        surface.GetComponent<MeshRenderer>().sharedMaterial = material;

        if (!mesh) mesh = new Mesh { name = "Lava Crack", hideFlags = HideFlags.HideAndDontSave };
        Fill(mesh);
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // ---- The crack's shape -------------------------------------------------------------------------------

    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector3> uvs = new List<Vector3>();
    private readonly List<Vector2> uvs2 = new List<Vector2>();
    private readonly List<int> triangles = new List<int>();

    private void Fill(Mesh target)
    {
        vertices.Clear(); uvs.Clear(); uvs2.Clear(); triangles.Clear();
        var rng = new System.Random(seed);
        var start = new Vector2(transform.position.x, transform.position.z);
        var forward = new Vector2(transform.forward.x, transform.forward.z);
        if (forward.sqrMagnitude < 1e-4f) forward = Vector2.up;
        forward.Normalize();

        var main = Line(rng, start, forward, length);
        Strip(main, t => width * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.04f, 0.96f, t)), 0.6f), heat);

        for (var b = 0; b < branches; b++)
        {
            // spread along the seam and to alternate sides, so the branches never comb out one way
            var at = Mathf.Lerp(0.15f, 0.85f, (b + 0.2f + 0.6f * (float)rng.NextDouble()) / branches);
            var i = Mathf.Clamp(Mathf.RoundToInt(at * (main.Count - 1)), 1, main.Count - 2);
            var along = (main[i + 1] - main[i - 1]).normalized;
            var side = (b + seed) % 2 == 0 ? 1f : -1f;
            var dir = Rotate(along, side * Mathf.Lerp(35f, 75f, (float)rng.NextDouble()));
            var w0 = width * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.04f, 0.96f, at)), 0.6f) * 0.65f;
            var branch = Line(rng, main[i], dir, length * Mathf.Lerp(0.25f, 0.5f, (float)rng.NextDouble()));
            Strip(branch, t => w0 * Mathf.Pow(1f - t, 0.8f), heat * 0.85f);

            // and now and then a fork of the branch
            if (branch.Count > 6 && rng.NextDouble() < 0.5)
            {
                var j = branch.Count / 2;
                var bdir = Rotate((branch[j + 1] - branch[j - 1]).normalized, -side * Mathf.Lerp(25f, 55f, (float)rng.NextDouble()));
                var w1 = w0 * 0.5f * 0.6f;
                Strip(Line(rng, branch[j], bdir, length * 0.18f), t => w1 * (1f - t), heat * 0.7f);
            }
        }

        target.Clear();
        target.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        target.SetVertices(vertices);
        target.SetUVs(0, uvs);
        target.SetUVs(1, uvs2);
        target.SetTriangles(triangles, 0);
        target.RecalculateBounds();
    }

    /// <summary>A jagged line: straight runs between kinks, the way rock splits, wandering round the given direction.</summary>
    private static List<Vector2> Line(System.Random rng, Vector2 from, Vector2 dir, float len)
    {
        var points = new List<Vector2> { from };
        var heading = dir;
        var run = 0f;
        var p = from;
        for (var d = 0f; d < len; d += Step)
        {
            if (run <= 0f)
            {
                run = Mathf.Lerp(0.5f, 1.4f, (float)rng.NextDouble());
                heading = Rotate(dir, Mathf.Lerp(-26f, 26f, (float)rng.NextDouble()));
            }
            run -= Step;
            p += heading * Step;
            points.Add(p);
        }
        return points;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        var r = degrees * Mathf.Deg2Rad;
        var c = Mathf.Cos(r); var s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    /// <summary>The strip along a line: five vertices across, each set on the ground; the scorch tapers less than the seam.</summary>
    private void Strip(List<Vector2> line, System.Func<float, float> seamWidth, float hot)
    {
        if (line.Count < 2) return;
        var first = vertices.Count;
        var total = 0f;
        for (var i = 1; i < line.Count; i++) total += (line[i] - line[i - 1]).magnitude;
        var along = 0f;
        var scale = scorch / Mathf.Max(0.05f, width);
        for (var i = 0; i < line.Count; i++)
        {
            if (i > 0) along += (line[i] - line[i - 1]).magnitude;
            var t = i / (float)(line.Count - 1);
            var tangent = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized;
            var normal = new Vector2(-tangent.y, tangent.x);
            // the seam opens and pinches along its length, as split rock does
            var seam = Mathf.Max(0f, seamWidth(t)) * Mathf.Lerp(0.45f, 1.35f, Mathf.PerlinNoise(along * 0.55f + seed * 7.3f, first * 0.01f));
            // the scorch keeps some width to the very tip, so the crack ends in a burn, not a point
            var half = Mathf.Max(seam * scale, scorch * 0.5f) * 0.5f;
            var open = Mathf.Clamp01(seam * 0.5f / half);
            foreach (var a in Across)
            {
                var q = line[i] + normal * (a * half);
                vertices.Add(transform.InverseTransformPoint(new Vector3(q.x, Ground(q.x, q.y) + Lift, q.y)));
                // the third: how far from either end, in strip widths, so the scorch rounds off there instead of stopping square
                uvs.Add(new Vector3(along, a, Mathf.Min(along, total - along) / Mathf.Max(0.1f, half * 2f)));
                uvs2.Add(new Vector2(open, hot));
            }
        }

        var across = Across.Length;
        for (var i = 0; i + 1 < line.Count; i++)
        for (var k = 0; k + 1 < across; k++)
        {
            var a = first + i * across + k;
            var b = a + across;
            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
        }
    }

    private float Ground(float x, float z)
    {
        foreach (var t in Terrain.activeTerrains)
        {
            if (!t || !t.terrainData) continue;
            var o = t.transform.position;
            var size = t.terrainData.size;
            if (x < o.x || z < o.z || x > o.x + size.x || z > o.z + size.z) continue;
            return t.SampleHeight(new Vector3(x, 0f, z)) + o.y;
        }
        return transform.position.y;
    }

    private static void DestroyNow(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
