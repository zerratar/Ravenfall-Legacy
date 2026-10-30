using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fence or a low wall placed in the editor along a line of points, laid on the ground as OSRS lays its fences: the
/// piece (a Synty fence panel or dry stone wall) repeats along each straight run, stretched a little so whole pieces
/// fill it, and every vertex keeps its height above the ground under it, so the posts stay upright while the rails and
/// stones follow the slope and runs meet without gaps. Draw and edit it in the Scene view (WorldFenceEditor); the
/// Lumbridge tools make them from the map's fences.
/// </summary>
/// <remarks>
/// The bent mesh is built in the editor whenever the fence changes and saved with the scene on this object's own
/// MeshFilter, so nothing is built in the game. Rebuild it after the ground under it changes (the Lumbridge ground tool
/// does). The points are kept relative to this object; their heights do not matter, the ground's do.
/// A fence that blocks walking gets a not walkable navmesh box along each run (children named "Not Walkable"), which
/// the island's navmesh bake honours; it has no collider, so clicks and ground probes pass through it. Leave a gap in
/// the points where a gate stands open.
/// </remarks>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WorldFence : MonoBehaviour
{
    [Tooltip("The piece repeated along the fence: a fence panel or a length of wall, lying along its own x.")]
    [SerializeField] private GameObject piece;

    [Tooltip("The fence's height in metres; 0 keeps the piece's own.")]
    [SerializeField, Min(0f)] private float height;

    [Tooltip("How far it reaches into the ground, so no gap shows under it on uneven ground.")]
    [SerializeField] private float sink = 0.05f;

    [Tooltip("Whether bots and players walk round it: a not walkable navmesh box along each run (rebake the island's navmesh after).")]
    [SerializeField] private bool blocksWalking = true;

    [SerializeField] private List<Vector3> points = new List<Vector3>();

    public GameObject Piece { get => piece; set => piece = value; }
    public float Height { get => height; set => height = Mathf.Max(0f, value); }
    public float Sink { get => sink; set => sink = value; }
    public bool BlocksWalking { get => blocksWalking; set => blocksWalking = value; }
    public List<Vector3> Points => points;

    public void AddPoint(Vector3 local) => points.Add(local);

    /// <summary>Builds the bent mesh again from the points, the piece and the ground (in the editor).</summary>
    public void Rebuild()
    {
#if UNITY_EDITOR
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();
        if (!piece || points.Count < 2)
        {
            filter.sharedMesh = null;
            return;
        }

        // the piece's meshes in its own space, by material
        var parts = new List<(Mesh Mesh, int Sub, Matrix4x4 ToPiece, Material Material)>();
        var bounds = new Bounds(); var first = true;
        foreach (var mf in piece.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mf.sharedMesh || !mr) continue;
            var toPiece = piece.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            for (var s = 0; s < mf.sharedMesh.subMeshCount; s++)
                parts.Add((mf.sharedMesh, s, toPiece, s < mr.sharedMaterials.Length ? mr.sharedMaterials[s] : mr.sharedMaterial));
            foreach (var v in mf.sharedMesh.vertices)
            {
                var p = toPiece.MultiplyPoint3x4(v);
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
            }
        }
        if (parts.Count == 0) return;
        var pieceLength = Mathf.Max(0.1f, bounds.size.x);
        var sy = height > 0f ? height / Mathf.Max(0.01f, bounds.size.y) : 1f;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var materials = new List<Material>();
        var triangles = new List<List<int>>();
        var origin = transform.position;
        for (var run = 0; run + 1 < points.Count; run++)
        {
            var a = transform.TransformPoint(points[run]); var b = transform.TransformPoint(points[run + 1]);
            a.y = 0; b.y = 0;
            var length = Vector3.Distance(a, b);
            if (length < 0.01f) continue;
            var along = (b - a) / length;
            var across = Vector3.Cross(along, Vector3.up);
            var count = Mathf.Max(1, Mathf.RoundToInt(length / pieceLength));
            var sx = length / count / pieceLength;
            for (var i = 0; i < count; i++)
            {
                var start = a + along * (i * pieceLength * sx);
                foreach (var part in parts)
                {
                    var slot = materials.IndexOf(part.Material);
                    if (slot < 0) { slot = materials.Count; materials.Add(part.Material); triangles.Add(new List<int>()); }
                    var baseIndex = vertices.Count;
                    var mv = part.Mesh.vertices; var mn = part.Mesh.normals; var mu = part.Mesh.uv;
                    for (var k = 0; k < mv.Length; k++)
                    {
                        var p = part.ToPiece.MultiplyPoint3x4(mv[k]);
                        var x = (p.x - bounds.min.x) * sx;
                        // (across is the piece's own z turned with it: no mirroring, which would turn its faces inside out)
                        var flat = start + along * x + across * (p.z - bounds.center.z);
                        // its height above the ground under it, as the piece stood on flat ground
                        var y = WorldRoad.GroundHeight(new Vector3(flat.x, 1000f, flat.z)) + (p.y - bounds.min.y) * sy - sink;
                        vertices.Add(new Vector3(flat.x, y, flat.z) - origin);
                        var n = k < mn.Length ? part.ToPiece.MultiplyVector(mn[k]) : Vector3.up;
                        normals.Add((along * n.x + Vector3.up * n.y + across * n.z).normalized);
                        uvs.Add(k < mu.Length ? mu[k] : Vector2.zero);
                    }
                    foreach (var t in part.Mesh.GetTriangles(part.Sub)) triangles[slot].Add(baseIndex + t);
                }
            }
        }

        var mesh = filter.sharedMesh;
        if (!mesh || mesh.name != "World Fence") mesh = new Mesh { name = "World Fence" };
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Count;
        for (var s = 0; s < triangles.Count; s++) mesh.SetTriangles(triangles[s], s);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        renderer.sharedMaterials = materials.ToArray();
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        // (after the reset above: the boxes are placed in the world)
        RebuildBlocks(pieceLength, bounds.size.y * sy);
#endif
    }

#if UNITY_EDITOR
    // one not walkable box per run, from the lowest ground along it to the fence's top over the highest
    private void RebuildBlocks(float pieceLength, float fenceHeight)
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name == "Not Walkable") DestroyImmediate(transform.GetChild(i).gameObject);
        if (!blocksWalking) return;
        for (var run = 0; run + 1 < points.Count; run++)
        {
            var a = transform.TransformPoint(points[run]); var b = transform.TransformPoint(points[run + 1]);
            a.y = 0; b.y = 0;
            var length = Vector3.Distance(a, b);
            if (length < 0.01f) continue;
            float low = float.MaxValue, high = float.MinValue;
            var steps = Mathf.Max(2, Mathf.CeilToInt(length / 1f));
            for (var k = 0; k <= steps; k++)
            {
                var p = Vector3.Lerp(a, b, k / (float)steps);
                var y = WorldRoad.GroundHeight(new Vector3(p.x, 1000f, p.z));
                low = Mathf.Min(low, y); high = Mathf.Max(high, y);
            }
            var block = new GameObject("Not Walkable");
            block.transform.SetParent(transform, false);
            var bottom = low - 1f; var top = high + Mathf.Max(1f, fenceHeight);
            block.transform.SetPositionAndRotation(new Vector3((a.x + b.x) * 0.5f, (bottom + top) * 0.5f, (a.z + b.z) * 0.5f), Quaternion.LookRotation(b - a, Vector3.up));
            var volume = block.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
            // across, a little wider than the fence, so no agent squeezes through between the posts
            volume.size = new Vector3(0.6f, top - bottom, length + 0.2f);
            volume.center = Vector3.zero;
            volume.area = 1;   // Not Walkable
        }
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.4f);
        for (var i = 0; i + 1 < points.Count; i++)
        {
            var a = transform.TransformPoint(points[i]); var b = transform.TransformPoint(points[i + 1]);
            a.y = WorldRoad.GroundHeight(a) + 0.3f; b.y = WorldRoad.GroundHeight(b) + 0.3f;
            Gizmos.DrawLine(a, b);
        }
    }
}
