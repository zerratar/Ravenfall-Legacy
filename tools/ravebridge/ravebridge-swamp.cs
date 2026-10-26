// Ravebridge's swamp, as Lumbridge's: pools of still murky water among the mud (SwampPool). Picks the pools' places in
// the swamp away from the mine's rocks, Father Urhney's hut, the road and each other; digs a shallow bowl under each
// (from the ground saved before any pool, so a rerun gives the same pools); sets each pool's water just under
// the lowest ground round its shore, so no water stands above lower ground; marks the pools not walkable for the
// navmesh. Rerunnable (clears its group "Swamp Pools"). Run before ravebridge-dress.cs, then rebake the navmesh.
var island = UnityEngine.GameObject.Find("============ WORLD/Islands").transform.Find("Ravebridge");
var old = island.Find("Swamp Pools");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Swamp Pools").transform; group.SetParent(island, false);
UnityEngine.Terrain terrain = null;
foreach (var t in island.GetComponentsInChildren<UnityEngine.Terrain>(true)) if (t.transform.position.z < 1500) terrain = t;
var data = terrain.terrainData; var o = terrain.transform.position; var size = data.size; var res = data.heightmapResolution;
float Ground(float x, float z) => data.GetInterpolatedHeight((x - o.x) / size.x, (z - o.z) / size.z) + o.y;
// the swamp's ground as it was before any pool (tools/ravebridge/swamp-heights-before.bin, from the committed terrain):
// put back first, so every run picks and digs from the same ground and leaves no hollows from earlier runs
using (var br = new System.IO.BinaryReader(System.IO.File.OpenRead("tools/ravebridge/swamp-heights-before.bin")))
{
    int bi = br.ReadInt32(), bj = br.ReadInt32(), bw = br.ReadInt32(), bn = br.ReadInt32();
    var back = new float[bn, bw];
    for (var j = 0; j < bn; j++) for (var i = 0; i < bw; i++) back[j, i] = br.ReadSingle();
    UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Ravebridge swamp");
    data.SetHeights(bi, bj, back);
}
UnityEngine.Vector2 V(float x, float z) => new UnityEngine.Vector2(x, z);

// what the pools keep clear of
var keepOut = new System.Collections.Generic.List<(UnityEngine.Vector2 at, float r)>();
foreach (UnityEngine.Transform c in island.Find("Playable"))
    if (c.name.StartsWith("Mining") || c.name.StartsWith("Fishing"))
        foreach (var e in c.GetComponentsInChildren<UnityEngine.Transform>(true)) if (e != c && e.parent != c) keepOut.Add((V(e.position.x, e.position.z), 5f));
keepOut.Add((V(-1722, 1432), 12f));                 // Father Urhney's hut
// the swamp's mangrove roots and dead trees may stand in the water: that is the look
var road = new[] { V(-1626, 1450), V(-1642, 1444), V(-1666, 1446), V(-1688, 1452) };
float SegDist(UnityEngine.Vector2 p, UnityEngine.Vector2 a, UnityEngine.Vector2 b) { var ab = b - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (p - (a + ab * t)).magnitude; }
float RoadDist(UnityEngine.Vector2 p) { var d = float.MaxValue; for (var i = 0; i + 1 < road.Length; i++) d = UnityEngine.Mathf.Min(d, SegDist(p, road[i], road[i + 1])); return d; }

// pick the pools: the swamp is an ellipse round (-1695, 1445), 50 by 35 m; pools of 8 to 13 m across the long way
var rng = new System.Random(4);
float R() => (float)rng.NextDouble();
var pools = new System.Collections.Generic.List<(UnityEngine.Vector2 at, float rx, float rz, float yaw)>();
for (var tries = 0; tries < 400 && pools.Count < 8; tries++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var k = UnityEngine.Mathf.Sqrt(R()) * 0.85f;
    var p = V(-1695f + UnityEngine.Mathf.Cos(a) * 48f * k, 1445f + UnityEngine.Mathf.Sin(a) * 33f * k);
    var rx = UnityEngine.Mathf.Lerp(6.5f, 11f, R()); var rz = rx * UnityEngine.Mathf.Lerp(0.55f, 0.85f, R());
    var reach = rx * 1.3f;
    var ok = RoadDist(p) > reach + 3f;
    foreach (var (at, r) in keepOut) if ((p - at).magnitude < reach + r) ok = false;
    foreach (var q in pools) if ((p - q.at).magnitude < reach + q.rx * 1.3f + 3f) ok = false;
    if (Ground(p.x, p.y) < 7.5f) ok = false;
    if (ok) pools.Add((p, rx, rz, R() * 180f));
}

// dig and fill
var lilyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Water/Swamp Lilypads.mat");
var heights = data.GetHeights(0, 0, res, res);
var log = "";
foreach (var (at, rx, rz, yaw) in pools)
{
    var rad = yaw * UnityEngine.Mathf.Deg2Rad; var cos = UnityEngine.Mathf.Cos(rad); var sin = UnityEngine.Mathf.Sin(rad);
    // elliptic distance: 1 at the shore
    float E(float x, float z) { var dx = x - at.x; var dz = z - at.y; var u = dx * cos - dz * sin; var w = dx * sin + dz * cos; return UnityEngine.Mathf.Sqrt(u * u / (rx * rx) + w * w / (rz * rz)); }
    // the water: just under the lowest ground on the ring round the shore
    var level = float.MaxValue;
    for (var s = 0; s < 48; s++)
    {
        var ang = s / 48f * UnityEngine.Mathf.PI * 2f;
        foreach (var f in new[] { 1.15f, 1.25f, 1.35f })   // outside the dug bowl, so a rerun finds the same level
        {
            var u = UnityEngine.Mathf.Cos(ang) * rx * f; var w = UnityEngine.Mathf.Sin(ang) * rz * f;
            var x = at.x + u * cos + w * sin; var z = at.y - u * sin + w * cos;
            level = UnityEngine.Mathf.Min(level, Ground(x, z));
        }
    }
    level -= 0.15f;
    // the bowl: lowered toward level - 1.2 at the middle, rising to the level at the shore
    var span = UnityEngine.Mathf.Max(rx, rz) * 1.4f;
    int i0 = UnityEngine.Mathf.Clamp((int)((at.x - span - o.x) / size.x * (res - 1)), 0, res - 1), i1 = UnityEngine.Mathf.Clamp((int)((at.x + span - o.x) / size.x * (res - 1)) + 1, 0, res - 1);
    int j0 = UnityEngine.Mathf.Clamp((int)((at.y - span - o.z) / size.z * (res - 1)), 0, res - 1), j1 = UnityEngine.Mathf.Clamp((int)((at.y + span - o.z) / size.z * (res - 1)) + 1, 0, res - 1);
    var dug = 0;
    for (var j = j0; j <= j1; j++)
    for (var i = i0; i <= i1; i++)
    {
        var x = o.x + i / (float)(res - 1) * size.x; var z = o.z + j / (float)(res - 1) * size.z;
        var e = E(x, z);
        if (e >= 1.1f) continue;
        var k = UnityEngine.Mathf.Clamp01(1f - e / 1.1f);
        var target = level + 0.1f - 1.3f * k * k * (3f - 2f * k);
        var h = heights[j, i] * size.y + o.y;
        if (target < h) { heights[j, i] = (target - o.y) / size.y; dug++; }
    }
    var go = new UnityEngine.GameObject("Pool");
    go.transform.SetParent(group, false);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(at.x, level, at.y), UnityEngine.Quaternion.Euler(0, yaw, 0));
    var pool = go.AddComponent<SwampPool>();
    pool.RadiusX = rx * 1.3f; pool.RadiusZ = rz * 1.3f;
    var vol = go.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
    vol.size = new UnityEngine.Vector3(rx * 1.6f, 6f, rz * 1.6f);
    vol.center = new UnityEngine.Vector3(0f, 0f, 0f);
    vol.area = 1;   // not walkable
    // reeds round the shore and lily pads on the water break the shore's line (the heights are 5 m apart, so a dug shore
    // has corners) and are the swamp's look
    for (var s2 = 0; s2 < 26; s2++)
    {
        var ang = R() * UnityEngine.Mathf.PI * 2f; var f = UnityEngine.Mathf.Lerp(0.85f, 1.12f, R());
        var u = UnityEngine.Mathf.Cos(ang) * rx * f; var w = UnityEngine.Mathf.Sin(ang) * rz * f;
        var x = at.x + u * cos + w * sin; var z = at.y - u * sin + w * cos;
        var reed = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonAdventure/Prefabs/Environments/SM_Env_Reeds_0" + (1 + rng.Next(3)) + ".prefab");
        var rgo = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(reed, group);
        // a little under the water: in the shallows they stand in it, on the bank their feet sink into the mud
        rgo.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, level - 0.25f, z), UnityEngine.Quaternion.Euler(0, R() * 360f, 0));
        rgo.transform.localScale = UnityEngine.Vector3.one * UnityEngine.Mathf.Lerp(1.2f, 2f, R());
        rgo.isStatic = true;
    }
    for (var s2 = 0; s2 < 5; s2++)
    {
        var ang = R() * UnityEngine.Mathf.PI * 2f; var f = R() * 0.65f;
        var u = UnityEngine.Mathf.Cos(ang) * rx * f; var w = UnityEngine.Mathf.Sin(ang) * rz * f;
        var lily = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(R() < 0.5f ? "Assets/Fantasy Adventure Environment/Prefabs/Vegetation/Lilypads_A.prefab" : "Assets/Fantasy Adventure Environment/Prefabs/Vegetation/Lilypads_B.prefab");
        var lgo = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(lily, group);
        lgo.transform.SetPositionAndRotation(new UnityEngine.Vector3(at.x + u * cos + w * sin, level + 0.02f, at.y - u * sin + w * cos), UnityEngine.Quaternion.Euler(0, R() * 360f, 0));
        lgo.transform.localScale = UnityEngine.Vector3.one * UnityEngine.Mathf.Lerp(1.2f, 1.8f, R());
        // the pack's foliage shader is not URP: its own atlas on a URP material (Assets/World/Water)
        foreach (var lr in lgo.GetComponentsInChildren<UnityEngine.Renderer>()) { var lm = lr.sharedMaterials; for (var q = 0; q < lm.Length; q++) lm[q] = lilyMat; lr.sharedMaterials = lm; }
        lgo.isStatic = true;
    }
    log += "pool (" + at.x.ToString("F0") + ", " + at.y.ToString("F0") + ") " + rx.ToString("F1") + "x" + rz.ToString("F1") + " level " + level.ToString("F2") + " dug " + dug + "; ";
}
UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Ravebridge swamp");
data.SetHeights(0, 0, heights);
terrain.Flush();
UnityEditor.EditorUtility.SetDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return pools.Count + " pools: " + log;
