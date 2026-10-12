var sb = new System.Text.StringBuilder("LND|");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("LND|refused"); return null; }
var hall = new UnityEngine.Vector3(-18.64f, 0f, -69.72f);
var plaza = hall + new UnityEngine.Vector3(-5f, 0f, -40f);
UnityEngine.Terrain terrain = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsInactive.Include)) if (t.name == "Terrain") terrain = t;
System.Func<float, float, float> H = (x, z) => terrain.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + terrain.transform.position.y;
var rng = new System.Random(1337);
System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);
UnityEngine.Transform env = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include))
    if (t.name == "Environment" && t.parent != null && t.parent.name == "Main Island") env = t;
var old = env.Find("Town Landscape"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new UnityEngine.GameObject("Town Landscape").transform; root.SetParent(env, false);
root.position = UnityEngine.Vector3.zero; root.rotation = UnityEngine.Quaternion.identity; root.localScale = UnityEngine.Vector3.one;

// re-ground gameplay points
var grounded = 0;
var plots = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (var s in UnityEngine.Object.FindObjectsByType<TownHouseSlot>(UnityEngine.FindObjectsInactive.Include))
{ var p = s.transform.position; p.y = H(p.x, p.z); s.transform.position = p; plots.Add(p); grounded++; }
foreach (var s in UnityEngine.Object.FindObjectsByType<TreasureBox>(UnityEngine.FindObjectsInactive.Include))
{ var p = s.transform.position; var rel = p - hall; if (rel.x < -150 || rel.x > 135 || rel.z < -130 || rel.z > 70) continue; p.y = H(p.x, p.z); s.transform.position = p; grounded++; }
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include))
{
    if (t.parent == null || t.parent.name != "ItemDropSpawnPoints") continue;
    var p = t.position; var rel = p - hall; if (rel.x < -150 || rel.x > 135 || rel.z < -130 || rel.z > 70) continue;
    p.y = H(p.x, p.z) + 0.5f; t.position = p; grounded++;
}

System.Func<string, UnityEngine.GameObject> load = path => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
System.Func<UnityEngine.GameObject, UnityEngine.Transform, UnityEngine.GameObject> place = (prefab, parent) => (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
System.Func<UnityEngine.GameObject, UnityEngine.Bounds> boundsOf = go => { var rs = go.GetComponentsInChildren<UnityEngine.Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; };

// coast: march out from the plaza to the cliff edge
var cliffs = new UnityEngine.GameObject("Coast Cliffs").transform; cliffs.SetParent(root, false);
var cl1 = load("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Cliff_03.prefab");
var cl2 = load("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Cliff_02.prefab");
var boulder = load("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Cliff_01.prefab");
var boulders = 0;
var edge = new System.Collections.Generic.List<UnityEngine.Vector3>();
var lastPlaced = new UnityEngine.Vector3(9999, 0, 9999);
var rocks = 0;
for (var a = 0f; a < 360f; a += 2f)
{
    var dir = UnityEngine.Quaternion.Euler(0, a, 0) * UnityEngine.Vector3.forward;
    var e = plaza; var found = false;
    for (var r = 20f; r < 220f; r += 1f) { var p = plaza + dir * r; if (H(p.x, p.z) < 3.5f) { e = p; found = true; break; } }
    if (!found) continue;
    var rel = e - hall;
    // the northern shore is a gentle beach, leave it open
    var north = rel.z > 28f;
    var inner = e - dir * 6f;
    var drop = H(inner.x, inner.z) - H(e.x + dir.x * 4f, e.z + dir.z * 4f);
    edge.Add(new UnityEngine.Vector3(e.x, a, e.z));
    if (north || drop < 5f) continue;
    if (UnityEngine.Vector3.Distance(new UnityEngine.Vector3(e.x, 0, e.z), lastPlaced) < R(9f, 12f)) continue;
    var big = rng.NextDouble() < 0.8;
    var go = place(big ? cl1 : cl2, cliffs);
    // long side along the shore, face flipped at random so the wall does not repeat
    go.transform.rotation = UnityEngine.Quaternion.LookRotation(dir) * UnityEngine.Quaternion.Euler(R(-4, 4), (rng.NextDouble() < 0.5 ? 0 : 180) + R(-12, 12), R(-4, 4));
    var b0 = boundsOf(go);
    var targetH = R(10f, 12f);
    var sc = targetH / UnityEngine.Mathf.Max(0.1f, b0.size.y);
    go.transform.localScale = UnityEngine.Vector3.one * sc;
    go.transform.position = new UnityEngine.Vector3(e.x, 0, e.z) + dir * R(-1f, 1.5f);
    var b1 = boundsOf(go);
    go.transform.position += UnityEngine.Vector3.up * (R(7.9f, 8.7f) - b1.max.y);
    lastPlaced = new UnityEngine.Vector3(e.x, 0, e.z); rocks++;
    // mossy boulders at the waterline now and then
    if (rng.NextDouble() < 0.2)
    {
        var bo = place(boulder, cliffs);
        bo.transform.rotation = UnityEngine.Quaternion.Euler(R(-18, 18), R(0, 360), R(-18, 18));
        bo.transform.localScale = new UnityEngine.Vector3(R(0.6f, 1.1f), R(0.35f, 0.7f), R(0.6f, 1.1f));
        bo.transform.position = new UnityEngine.Vector3(e.x, R(-5f, -3.5f), e.z) + dir * R(3.5f, 6.5f) + UnityEngine.Vector3.Cross(dir, UnityEngine.Vector3.up) * R(-5f, 5f);
        boulders++;
    }
}

// trees and bushes in a belt inside the edge
var trees = new UnityEngine.GameObject("Tree Belt").transform; trees.SetParent(root, false);
var treePaths = new[] { "SM_Env_Tree_Large_01", "SM_Env_Tree_Large_02", "SM_Env_Tree_Large_03", "SM_Env_Tree_02", "SM_Env_Tree_03", "SM_Env_Tree_04", "SM_Env_Tree_Thin_01", "SM_Env_Tree_Thin_02" };
var bushPaths = new[] { "SM_Env_Bush_Cluster_01", "SM_Env_Bush_Cluster_03", "SM_Env_Bush_Cluster_04", "SM_Env_Tree_Bush_01", "SM_Env_Tree_Bush_02" };
var placedPts = new System.Collections.Generic.List<UnityEngine.Vector3>();
var treeCount = 0;
System.Func<UnityEngine.Vector3, float, bool> clear = (p, minD) =>
{
    foreach (var q in plots) if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p.x, p.z), new UnityEngine.Vector2(q.x, q.z)) < 10f) return false;
    if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p.x, p.z), new UnityEngine.Vector2(plaza.x, plaza.z)) < 38f) return false;
    foreach (var q in placedPts) if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p.x, p.z), new UnityEngine.Vector2(q.x, q.z)) < minD) return false;
    return true;
};
System.Action<UnityEngine.Vector3, bool> plant = (p, isTree) =>
{
    var name = isTree ? treePaths[rng.Next(treePaths.Length)] : bushPaths[rng.Next(bushPaths.Length)];
    var go = place(load("Assets/PolygonAncientEmpire/Prefabs/Environment/" + name + ".prefab"), trees);
    go.transform.position = p + UnityEngine.Vector3.down * 0.2f;
    go.transform.rotation = UnityEngine.Quaternion.Euler(0, R(0, 360), 0);
    go.transform.localScale *= R(0.85f, 1.2f);
    placedPts.Add(p); treeCount++;
};
foreach (var e in edge)
{
    var dir = UnityEngine.Quaternion.Euler(0, e.y, 0) * UnityEngine.Vector3.forward;
    var rel = new UnityEngine.Vector3(e.x, 0, e.z) - hall;
    var tries = rel.z > 28f ? 1 : 3;
    for (var k = 0; k < tries; ++k)
    {
        var inward = R(5f, rel.z > 28f ? 30f : 20f);
        var side = UnityEngine.Vector3.Cross(dir, UnityEngine.Vector3.up) * R(-4f, 4f);
        var p = new UnityEngine.Vector3(e.x, 0, e.z) - dir * inward + side;
        p.y = H(p.x, p.z);
        if (p.y < 1.5f) continue;
        var isTree = rng.NextDouble() < 0.62;
        if (!clear(p, isTree ? 7f : 4.5f)) continue;
        plant(p, isTree);
    }
}
// a few groves on the plateau so it is not a bare lawn
var groveCenters = new[] { hall + new UnityEngine.Vector3(-80, 0, -60), hall + new UnityEngine.Vector3(70, 0, -95), hall + new UnityEngine.Vector3(-5, 0, 40), hall + new UnityEngine.Vector3(90, 0, 30) };
foreach (var c in groveCenters)
    for (var k = 0; k < 9; ++k)
    {
        var p = c + new UnityEngine.Vector3(R(-12, 12), 0, R(-12, 12)); p.y = H(p.x, p.z);
        if (p.y < 1.5f) continue;
        var isTree = rng.NextDouble() < 0.55;
        if (!clear(p, isTree ? 6.5f : 4f)) continue;
        plant(p, isTree);
    }

// flowers, grass tufts and the odd boulder, never on plots or the plaza
var flora = new UnityEngine.GameObject("Flowers And Stones").transform; flora.SetParent(root, false);
var floraPaths = new[] { "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Flowers_01.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Flowers_02.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Flowers_04.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Flowers_07.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Grass_01.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_Grass_03.prefab", "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Grass_01.prefab", "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Grass_02.prefab" };
var stonePaths = new[] { "Assets/PolygonElvenRealm/Prefabs/Environment/SM_Env_Rock_01.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/SM_Env_Rock_02.prefab", "Assets/PolygonElvenRealm/Prefabs/Environment/SM_Env_Rock_03.prefab" };
var floraCount = 0;
for (var k = 0; k < 900 && floraCount < 260; ++k)
{
    var p = hall + new UnityEngine.Vector3(R(-110, 100), 0, R(-115, 65)); p.y = H(p.x, p.z);
    if (p.y < 1f) continue;
    var near = false;
    foreach (var q in plots) if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p.x, p.z), new UnityEngine.Vector2(q.x, q.z)) < 6f) near = true;
    if (near || UnityEngine.Vector2.Distance(new UnityEngine.Vector2(p.x, p.z), new UnityEngine.Vector2(plaza.x, plaza.z)) < 26f) continue;
    var stone = false;
    var go = place(load(stone ? stonePaths[rng.Next(stonePaths.Length)] : floraPaths[rng.Next(floraPaths.Length)]), flora);
    go.transform.position = p + UnityEngine.Vector3.down * (stone ? 0.4f : 0.05f);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0, R(0, 360), 0);
    go.transform.localScale *= stone ? R(0.5f, 1.1f) : R(0.8f, 1.3f);
    floraCount++;
}

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
sb.Append("grounded=").Append(grounded).Append(" cliffs=").Append(rocks).Append(" boulders=").Append(boulders).Append(" trees=").Append(treeCount).Append(" flora=").Append(floraCount).Append(" edgePts=").Append(edge.Count);
UnityEngine.Debug.Log(sb.ToString());
