// Run with: unity command eval_file --file tools/mangrove/navmesh-bake.cs (twice: bake, then check)
//
// Bakes the mangrove island's own navmesh (the NavMeshSurface on WORLD/Islands/Mangrove, area "Mangrove") into
// Assets/Scenes/MainWorld/NavMesh-Mangrove.asset and saves the scene; the Bake button on the surface does the same.
// The world's navmesh is left alone: a whole-world bake from today's scene does not match the one the game ships
// with, and rebuilding it from its own triangles broke about one short path in ten. Agents on the island keep to
// the island's area (NavMeshHelper.AreaMaskAt), so the old scraps of world navmesh there are never walked on.
// The second run checks the island: its spawn to its dock and to every training spot, in the island's area only.
var mangrove = UnityEngine.GameObject.Find("============ WORLD/Islands/Mangrove");
var surface = mangrove.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var island = mangrove.GetComponent<IslandController>();
var key = "mangrove-navmesh-bake";
var path = "Assets/Scenes/MainWorld/NavMesh-Mangrove.asset";
var stage = UnityEditor.SessionState.GetString(key, "");
if (stage == "")
{
    UnityEditor.SessionState.SetString(key, "baking");
    var data = new UnityEngine.AI.NavMeshData(surface.agentTypeID) { name = "NavMesh-Mangrove", position = surface.transform.position, rotation = surface.transform.rotation };
    var op = surface.UpdateNavMesh(data);
    op.completed += _ =>
    {
        var old = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
        if (old) UnityEditor.AssetDatabase.DeleteAsset(path);
        UnityEditor.AssetDatabase.CreateAsset(data, path);
        surface.enabled = false;
        surface.navMeshData = data;
        surface.enabled = true;
        UnityEditor.EditorUtility.SetDirty(surface);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(surface.gameObject.scene);
        UnityEditor.SessionState.SetString(key, "baked");
    };
    return "baking";
}
if (stage == "baking") return "still baking";

UnityEditor.SessionState.EraseString(key);
var area = 1 << island.NavMeshArea;
var sb = new System.Text.StringBuilder("asset " + UnityEditor.AssetDatabase.GetAssetPath(surface.navMeshData) + "; ");
var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
var size = 0f;
for (int t = 0; t < tri.indices.Length / 3; t++)
{
    if (tri.areas[t] != island.NavMeshArea) continue;
    var a = tri.vertices[tri.indices[t * 3]]; var b = tri.vertices[tri.indices[t * 3 + 1]]; var c = tri.vertices[tri.indices[t * 3 + 2]];
    size += UnityEngine.Vector3.Cross(b - a, c - a).magnitude * 0.5f;
}
sb.Append("island area " + size.ToString("0") + " m2\n");
UnityEngine.AI.NavMeshHit hs;
if (!UnityEngine.AI.NavMesh.SamplePosition(island.SpawnPositionTransform.position, out hs, 2f, area)) return sb + "the spawn is off the island's navmesh";
System.Func<UnityEngine.Vector3, string> reach = pos =>
{
    UnityEngine.AI.NavMeshHit h;
    if (!UnityEngine.AI.NavMesh.SamplePosition(pos, out h, 4f, area)) return "off";
    var p = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.CalculatePath(hs.position, h.position, area, p);
    if (p.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) return "ok";
    var end = p.corners.Length > 0 ? p.corners[p.corners.Length - 1] : hs.position;
    return "partial, " + UnityEngine.Vector3.Distance(end, pos).ToString("0.0") + " m short";
};
sb.Append("dock " + reach(island.DockingArea.DockPositionTransform.position) + "\n");
foreach (var chunk in mangrove.GetComponentsInChildren<Chunk>(true))
{
    sb.Append(chunk.name + ": ");
    foreach (UnityEngine.Transform group in chunk.transform)
    {
        if (group.childCount == 0) sb.Append(group.name + " " + reach(group.position) + "; ");
        foreach (UnityEngine.Transform spot in group) sb.Append(spot.name.Split(' ')[0] + " " + reach(spot.position) + "; ");
    }
    sb.Append("\n");
}
return sb.ToString();
