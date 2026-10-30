using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A road placed by hand in the editor, anywhere in the world: the same curve, mesh and road
/// shader as the roads streamers draw in their village, but saved with the scene rather than on
/// the server. Draw and edit it in the Scene view (see WorldRoadEditor): Shift click to add points,
/// drag them, Ctrl click to insert or remove. GameObject > Ravenfall > Road makes a new one.
/// Each point has its own width (a multiple of the road's), so a road can widen or narrow, and each end
/// can be anything from round to square: a square end stops at its last point without fading, so the
/// road meets a bridge or a doorstep instead of spilling over it.
/// </summary>
/// <remarks>
/// Only the points, their widths, the ends, the surface and the width are saved. The mesh is built again whenever the road
/// is enabled or changed, on a hidden child that is never saved, so the scene stays small, nothing
/// generated ends up in it, and a change to the road shader or its materials shows everywhere.
/// The points are kept relative to this object, so moving it moves the road; their heights come
/// from the ground under them.
/// </remarks>
[ExecuteAlways]
public class WorldRoad : MonoBehaviour
{
    public enum Surface { Dirt = 0, Gravel = 1, Cobbles = 2 }

    [Tooltip("The road surface: the same materials as village roads (Resources/Roads).")]
    [SerializeField] private Surface surface = Surface.Dirt;

    [Tooltip("Width of the solid part of the road, in metres. Village roads are 2.5 (dirt), 3 (gravel) and 3.5 (cobbles).")]
    [SerializeField, Min(0.5f)] private float width = 2.5f;

    [Tooltip("How round the start is: 1 a half circle, 0 a square end that stops at the first point without fading.")]
    [SerializeField, Range(0f, 1f)] private float startRound = 1f;

    [Tooltip("How round the end is: 1 a half circle, 0 a square end that stops at the last point without fading.")]
    [SerializeField, Range(0f, 1f)] private float endRound = 1f;

    [Tooltip("The points the road curves through, relative to this object. Heights are ignored: the road follows the ground.")]
    [SerializeField] private List<Vector3> points = new List<Vector3>();

    [Tooltip("The width at each point, as a multiple of Width. The road eases from one to the next.")]
    [SerializeField] private List<float> widths = new List<float>();

    private GameObject view;
    private Mesh mesh;
    private List<Vector2> samples = new List<Vector2>();
    private Vector3[] gizmoLine = new Vector3[0];

    // the road shader draws each road twice; the second, middle only copy is made here once per material
    private static readonly Dictionary<Material, Material> middles = new Dictionary<Material, Material>();

    public Surface RoadSurface { get => surface; set { surface = value; Rebuild(); } }
    public float Width { get => width; set { width = Mathf.Max(0.5f, value); Rebuild(); } }

    public float StartRound { get => startRound; set { startRound = Mathf.Clamp01(value); Rebuild(); } }
    public float EndRound { get => endRound; set { endRound = Mathf.Clamp01(value); Rebuild(); } }

    /// <summary>
    /// The points, relative to this object. Change them through <see cref="AddPoint"/>,
    /// <see cref="InsertPoint"/>, <see cref="RemovePoint"/> and <see cref="ReversePoints"/>, which keep each
    /// point's width with it; moving one in place is fine. Call <see cref="Rebuild"/> after.
    /// </summary>
    public List<Vector3> Points => points;

    /// <summary>The width at a point, as a multiple of <see cref="Width"/>.</summary>
    public float PointWidth(int index) => index >= 0 && index < widths.Count ? widths[index] : 1f;

    public void SetPointWidth(int index, float scale)
    {
        if (index < 0 || index >= points.Count) return;
        SyncWidths();
        widths[index] = Mathf.Clamp(scale, 0.1f, 5f);
    }

    public void AddPoint(Vector3 local, float scale = -1f)
    {
        SyncWidths();
        points.Add(local);
        widths.Add(scale > 0f ? scale : (widths.Count > 0 ? widths[widths.Count - 1] : 1f));
    }

    public void InsertPoint(int index, Vector3 local, float scale = -1f)
    {
        SyncWidths();
        index = Mathf.Clamp(index, 0, points.Count);
        // a new point between two takes the width between them
        var guess = index > 0 && index < points.Count ? (widths[index - 1] + widths[index]) / 2f : (points.Count > 0 ? widths[Mathf.Min(index, widths.Count - 1)] : 1f);
        points.Insert(index, local);
        widths.Insert(index, scale > 0f ? scale : guess);
    }

    public void RemovePoint(int index)
    {
        if (index < 0 || index >= points.Count) return;
        SyncWidths();
        points.RemoveAt(index);
        widths.RemoveAt(index);
    }

    public void ReversePoints()
    {
        SyncWidths();
        points.Reverse();
        widths.Reverse();
        (startRound, endRound) = (endRound, startRound);
    }

    public void ClearPoints()
    {
        points.Clear();
        widths.Clear();
    }

    /// <summary>One width per point: roads saved before widths existed get 1 for each.</summary>
    private void SyncWidths()
    {
        while (widths.Count < points.Count) widths.Add(1f);
        if (widths.Count > points.Count) widths.RemoveRange(points.Count, widths.Count - points.Count);
    }

    /// <summary>The curve the mesh follows, in world x and z.</summary>
    public IReadOnlyList<Vector2> Samples => samples;

    public static float DefaultWidth(Surface surface) => surface == Surface.Cobbles ? 3.5f : surface == Surface.Gravel ? 3f : 2.5f;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        GrassRenderer.Remove(this);
        if (view) DestroyNow(view);
        view = null;
    }

    private void OnDestroy()
    {
        if (mesh) DestroyNow(mesh);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // meshes and objects cannot be made inside OnValidate; a moment later they can
        UnityEditor.EditorApplication.delayCall += () => { if (this && isActiveAndEnabled) Rebuild(); };
    }
#endif

    /// <summary>A point in the world, on the ground.</summary>
    public Vector3 WorldPoint(int index)
    {
        var p = transform.TransformPoint(points[index]);
        p.y = GroundHeight(p);
        return p;
    }

    /// <summary>Builds the mesh again from the points, the surface and the width.</summary>
    public void Rebuild()
    {
        if (!isActiveAndEnabled) return;
        EnsureView();

        var flat = new List<Vector2>(points.Count);
        foreach (var local in points)
        {
            var world = transform.TransformPoint(local);
            flat.Add(new Vector2(world.x, world.z));
        }

        samples = RoadMesh.Sample(flat);
        gizmoLine = new Vector3[samples.Count];
        for (var i = 0; i < samples.Count; ++i)
        {
            var at = new Vector3(samples[i].x, 0f, samples[i].y);
            gizmoLine[i] = new Vector3(at.x, GroundHeight(at) + 0.2f, at.z);
        }
        // the better surface a little higher, as in the village, so crossings do not flicker
        SyncWidths();
        var sampleWidths = RoadMesh.SampleWidths(flat, widths);
        RoadMesh.Build(mesh, samples, width, sampleWidths, GroundHeight, RoadMesh.Lift + (int)surface * 0.012f, startRound, endRound);
        // no grass on the road
        GrassRenderer.SetRoad(this, samples, width, sampleWidths);

        // the mesh is made in world space; the view sits on this object, so bring it into its space
        if (mesh.vertexCount > 0)
        {
            var vertices = mesh.vertices;
            var toLocal = view.transform.worldToLocalMatrix;
            for (var i = 0; i < vertices.Length; ++i) vertices[i] = toLocal.MultiplyPoint3x4(vertices[i]);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        var renderer = view.GetComponent<MeshRenderer>();
        var material = Resources.Load<Material>("Roads/Road_" + (int)surface);
        renderer.sharedMaterials = material ? new[] { material, MiddleOf(material) } : new Material[0];
    }

    private void EnsureView()
    {
        if (!mesh)
        {
            mesh = new Mesh { name = "World Road", hideFlags = HideFlags.DontSave };
        }

        if (view) return;
        view = new GameObject("Road Surface") { hideFlags = HideFlags.HideAndDontSave };
        view.transform.SetParent(transform, false);
        view.layer = gameObject.layer;
        view.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = view.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static Material MiddleOf(Material material)
    {
        if (!material.HasProperty("_Core")) return material;
        if (middles.TryGetValue(material, out var made) && made) return made;
        made = new Material(material) { name = material.name + " Middle", hideFlags = HideFlags.HideAndDontSave };
        made.SetFloat("_Core", 1f);
        made.renderQueue = material.renderQueue + 1;
        middles[material] = made;
        return made;
    }

    /// <summary>
    /// The ground under a point: the terrain where there is one, else the first collider below
    /// that is not a road.
    /// </summary>
    public static float GroundHeight(Vector3 point)
    {
        if (TerrainGround.TryHeight(point, out var ground)) return ground;

        var hits = Physics.RaycastAll(new Ray(new Vector3(point.x, point.y + 500f, point.z), Vector3.down), 2000f, ~0, QueryTriggerInteraction.Ignore);
        var best = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<WorldRoad>()) continue;
            best = Mathf.Max(best, hit.point.y);
        }
        return float.IsNegativeInfinity(best) ? point.y : best;
    }

    private static void DestroyNow(Object target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    private void OnDrawGizmos()
    {
        // a thin line down the middle: the road's own mesh is hidden, so this is what a click in the
        // Scene view picks
        if (gizmoLine == null || gizmoLine.Length < 2) return;
        Gizmos.color = new Color(1f, 0.82f, 0.38f, 0.04f);
        for (var i = 1; i < gizmoLine.Length; ++i) Gizmos.DrawLine(gizmoLine[i - 1], gizmoLine[i]);
    }
}
