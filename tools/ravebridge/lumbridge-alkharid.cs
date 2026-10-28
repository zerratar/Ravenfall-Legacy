// Run with: unity command eval_file --file tools/ravebridge/lumbridge-alkharid.cs (after lumbridge-swap.cs)
//
// Al Kharid in the Ravenfall Lumbridge (Assets/Scenes/LumbridgeRavenfall.unity): there is no desert building kit in the
// project, but PolygonAncientEmpire's walls are white stucco, flat topped and 2.5 m wide, one map tile. So its
// buildings keep the map's outline and every generated wall piece in them is swapped for Ancient Empire's: a window
// for a window, a door for a door, a plain wall for a wall, a low parapet for a battlement on a flat roof, each
// turned so its outer face looks out of the building, and a cornice runs along the top of every wall.
// The buildings are tools/ravebridge/osrs/buildings.py's Al Kharid ones (those east of tile 120). Rerunnable: the
// generated pieces are switched off (not deleted) and the new ones live under "Al Kharid".
var sb = new System.Text.StringBuilder();
var stage = "start";
try
{

const float Tile = 2.5f, Storey = 3.75f, ParapetHeight = 1.1f;
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/LumbridgeRavenfall.unity") return "open LumbridgeRavenfall first";
var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)").transform;

// ---- the buildings ----------------------------------------------------------------------------------------------
stage = "buildings";
var buildings = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(@"G:\GitHub\rs-map-viewer\out\buildings.json"));
var footprint = new System.Collections.Generic.HashSet<long>();
var near = new System.Collections.Generic.HashSet<long>();
foreach (var b in buildings)
{
    var bounds = b["bounds"].ToObject<int[]>();
    if (bounds[0] < 120) continue;
    foreach (var t in b["tiles"].ToObject<int[][]>())
    {
        footprint.Add(((long)t[0] << 20) | (uint)t[1]);
        for (var dx = -1; dx <= 1; dx++) for (var dy = -1; dy <= 1; dy++) near.Add(((long)(t[0] + dx) << 20) | (uint)(t[1] + dy));
    }
}
System.Func<UnityEngine.Vector3, bool> inside = p => footprint.Contains(((long)UnityEngine.Mathf.FloorToInt(p.x / Tile) << 20) | (uint)UnityEngine.Mathf.FloorToInt(p.z / Tile));
System.Func<UnityEngine.Vector3, bool> nearAk = p => near.Contains(((long)UnityEngine.Mathf.FloorToInt(p.x / Tile) << 20) | (uint)UnityEngine.Mathf.FloorToInt(p.z / Tile));

// ---- the pieces ------------------------------------------------------------------------------------------------
var cache = new System.Collections.Generic.Dictionary<string, UnityEngine.GameObject>();
System.Func<string, UnityEngine.GameObject> prefab = n =>
{
    if (cache.TryGetValue(n, out var c)) return c;
    UnityEngine.GameObject found = null;
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:prefab", new[] { "Assets/PolygonAncientEmpire", "Assets/PolygonFantasyKingdom" }))
    {
        var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == n) { found = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); break; }
    }
    cache[n] = found;
    return found;
};
var extents = new System.Collections.Generic.Dictionary<UnityEngine.GameObject, UnityEngine.Bounds>();
System.Func<UnityEngine.GameObject, UnityEngine.Bounds> extent = go =>
{
    if (extents.TryGetValue(go, out var e)) return e;
    var b = new UnityEngine.Bounds(); var first = true;
    foreach (var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        if (!mf.sharedMesh) continue;
        var mb = mf.sharedMesh.bounds; var m = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
        for (var c = 0; c < 8; c++) { var q = m.MultiplyPoint3x4(mb.center + UnityEngine.Vector3.Scale(mb.extents, new UnityEngine.Vector3((c & 1) * 2 - 1, (c & 2) - 1, (c & 4) / 2 - 1))); if (first) { b = new UnityEngine.Bounds(q, UnityEngine.Vector3.zero); first = false; } else b.Encapsulate(q); }
    }
    extents[go] = b;
    return b;
};

var old = root.Find("Al Kharid"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Al Kharid").transform;
group.SetParent(root, false);
// a piece along a to b (the line its middle stands on), from y0 to y1 (0 keeps its own height), its outer (+z) face
// towards "outward"
System.Action<string, UnityEngine.Vector3, UnityEngine.Vector3, float, float, UnityEngine.Vector3> place = (name, a, b, y0, y1, outward) =>
{
    var pf = prefab(name);
    if (!pf) return;
    var e = extent(pf);
    var n = new UnityEngine.Vector3(outward.x, 0, outward.z).normalized;
    var rot = UnityEngine.Quaternion.LookRotation(n, UnityEngine.Vector3.up);
    var along = rot * UnityEngine.Vector3.right;
    if (UnityEngine.Vector3.Dot(b - a, along) < 0) { var t = a; a = b; b = t; }
    var len = UnityEngine.Vector3.Distance(new UnityEngine.Vector3(a.x, 0, a.z), new UnityEngine.Vector3(b.x, 0, b.z));
    var sx = len / UnityEngine.Mathf.Max(0.01f, e.size.x);
    var sy = y1 > y0 ? (y1 - y0) / UnityEngine.Mathf.Max(0.01f, e.size.y) : 1f;
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, group);
    o.transform.localScale = new UnityEngine.Vector3(sx, sy, 1f);
    o.transform.SetPositionAndRotation(new UnityEngine.Vector3(a.x, y0, a.z) - rot * new UnityEngine.Vector3(e.min.x * sx, e.min.y * sy, 0f), rot);
    UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, UnityEditor.StaticEditorFlags.BatchingStatic);
};

// ---- swap the walls ----------------------------------------------------------------------------------------------
stage = "walls";
var swapped = 0; var kinds = new System.Collections.Generic.Dictionary<string, int>();
foreach (UnityEngine.Transform g in root)
{
    if (!g.name.StartsWith("Walls ")) continue;
    var kind = g.name.Substring(6);
    if (kind == "fence" || kind == "gate" || kind == "field wall" || kind == "low") continue;
    foreach (UnityEngine.Transform c in g)
    {
        // (switched off ones too: a rerun builds them again)
        var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject);
        if (!src) continue;
        // the line and the height the generated piece spanned (as the builder's span() placed it)
        var e = extent(src);
        var s = c.localScale; var r = c.rotation;
        var a = c.position + r * new UnityEngine.Vector3(e.min.x * s.x, e.min.y * s.y, e.center.z);
        var b = a + r * new UnityEngine.Vector3(e.size.x * s.x, 0, 0);
        var mid = (a + b) * 0.5f;
        if (!nearAk(mid)) continue;
        var top = a.y + e.size.y * s.y;
        // out: the side with no building on it
        var n = r * UnityEngine.Vector3.forward; n.y = 0; n.Normalize();
        if (inside(mid + n * 1.0f) && !inside(mid - n * 1.0f)) n = -n;
        c.gameObject.SetActive(false);
        if (kind == "battlement")
        {
            place("SM_Bld_Wall_01", a, b, a.y - 0.05f, a.y + ParapetHeight, n);
            place("SM_Bld_Trim_02", a, b, a.y + ParapetHeight - 0.05f, 0f, n);
        }
        else
        {
            // a storey's wall from its top down (windows and doors at their own proportion), a plain base below
            var foot = top - Storey;
            var piece = kind.Contains("window") ? "SM_Bld_Wall_Window_01" : kind.Contains("door") ? "SM_Bld_Wall_Door_01" : "SM_Bld_Wall_01";
            if (kind.Contains("door")) foot = a.y + 0.05f;
            place(piece, a, b, foot, top, n);
            if (foot - a.y > 0.05f) place("SM_Bld_Wall_01", a, b, a.y, foot + 0.02f, n);
            place("SM_Bld_Trim_02", a, b, top - 0.33f, 0f, n);
        }
        swapped++;
        kinds[kind] = kinds.TryGetValue(kind, out var k) ? k + 1 : 1;
    }
}
// parapets with nothing under them (the map's lone low walls on rooftops) float: taken out
{
    var pieces = new System.Collections.Generic.List<(UnityEngine.GameObject Go, UnityEngine.Bounds B)>();
    foreach (UnityEngine.Transform c in group)
    {
        var b = new UnityEngine.Bounds(c.position, UnityEngine.Vector3.zero); var first = true;
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        pieces.Add((c.gameObject, b));
    }
    // (again until none is left: a cap held only by a parapet taken out floats in turn)
    var taken = 0;
    while (true)
    {
        var lone = new System.Collections.Generic.List<(UnityEngine.GameObject Go, UnityEngine.Bounds B)>();
        foreach (var (go, b) in pieces)
        {
            if (b.size.y > 1.6f) continue;   // parapets and trims only
            var held = false;
            foreach (var (go2, o) in pieces)
            {
                // walls hold parapets, parapets hold their caps
                if (go2 == go || o.size.y < 0.8f) continue;
                if (o.max.y < b.min.y - 0.6f || o.min.y > b.min.y) continue;
                if (o.max.x < b.min.x - 0.3f || o.min.x > b.max.x + 0.3f || o.max.z < b.min.z - 0.3f || o.min.z > b.max.z + 0.3f) continue;
                held = true; break;
            }
            if (!held) lone.Add((go, b));
        }
        if (lone.Count == 0) break;
        foreach (var l in lone) { pieces.Remove(l); UnityEngine.Object.DestroyImmediate(l.Go); }
        taken += lone.Count;
    }
    sb.Append("lone parapets taken out " + taken + "; ");
}
sb.Append("swapped " + swapped + " (");
foreach (var kv in kinds) sb.Append(kv.Key + " " + kv.Value + " ");
sb.Append("); ");

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEngine.Debug.Log("[lumbridge-alkharid] " + sb);
return sb.ToString();

}
catch (System.Exception ex)
{
    UnityEngine.Debug.Log("[lumbridge-alkharid] failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
