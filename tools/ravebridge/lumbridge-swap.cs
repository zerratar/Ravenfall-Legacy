// Run with: unity command eval_file --file tools/ravebridge/lumbridge-swap.cs
//
// The Ravenfall version of the Lumbridge test (Assets/Scenes/LumbridgeRavenfall.unity, a copy of LumbridgeTest.unity,
// which stays as the OSRS-faithful reference): the generated buildings are swapped for Ravenfall's own pieces where
// those look better (the user, 2026-10-28: sizes need not match OSRS as long as the layout, fences and placement do).
// The buildings come from tools/ravebridge/osrs/buildings.py (out/buildings.json in the rs-map-viewer clone): for each
// one in the table below the tool
// - takes out the generated walls, doors, windows and the props inside its footprint, and cuts its footprint (and a
//   tile round it, the eaves) out of the roof, gable and floor meshes;
// - puts the stand-in in the footprint's middle, standing on its highest ground (the presets' stone bases reach down),
//   turned so its door faces the side the map's door is on, scaled to fill the footprint within limits;
// - gives FantasyKingdom presets the palette with the red tiles (Lumbridge's roofs), as Ravebridge did.
// Rerunnable: the stand-ins live under "Ravenfall Buildings" and are made again; what was taken out stays out.
var sb = new System.Text.StringBuilder();
var stage = "start";
try
{

const string ScenePath = "Assets/Scenes/LumbridgeRavenfall.unity";
const string BuildingsJson = @"G:\GitHub\rs-map-viewer\out\buildings.json";
const float Tile = 2.5f;
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath) return "open " + ScenePath + " first";
var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)");
if (!root) return "no Lumbridge (Ravenfall) root";
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
System.Func<float, float, float> groundAt = (x, z) => terrain.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + terrain.transform.position.y;

// building id -> (prefab, the yaw at which its door faces south (-z), the most it may be scaled up)
var table = new System.Collections.Generic.Dictionary<int, (string Prefab, float FrontYaw, float MaxScale)>
{
    { 1, ("SM_Bld_Preset_Stables_01_Optimized", 0f, 1.4f) },        // the barn by the windmill (hay bales)
    { 3, ("SM_Bld_Preset_Shelter_01_Optimized", 0f, 1.3f) },        // the small barn beside it
    { 4, ("SM_Bld_Preset_House_Windmill_01_Optimized", 0f, 1.2f) }, // the windmill (millstones, hopper)
    { 6, ("SM_Bld_Preset_House_03_Optimized", 0f, 1.5f) },          // Fred the farmer's house
    { 2, ("SM_Bld_Preset_Hut_01_Optimized", 0f, 1.4f) },            // Father Urhney's hut in the swamp
    { 7, ("SM_Bld_Preset_Hut_02_Optimized", 0f, 1.3f) },            // the swamp shack
    { 9, ("SM_Bld_Preset_House_01_A_Optimized", 0f, 1.5f) },        // the general store (shelves, counters)
    { 11, ("SM_Bld_Preset_Blacksmith_01_Optimized", 0f, 1.4f) },    // the furnace
    { 12, ("SM_Bld_Preset_House_05_Optimized", 0f, 1.4f) },
    { 14, ("SM_Bld_Preset_Tavern_01_Optimized", 0f, 1.3f) },
    { 15, ("SM_Bld_Preset_House_10_Optimized", 0f, 1.2f) },         // the big house south of the castle
    { 16, ("SM_Bld_Preset_Shelter_02_Optimized", 0f, 1.3f) },       // the chicken coops
    { 18, ("SM_Bld_Preset_Church_01_B_Optimized", 0f, 1.8f) },      // the church (altar, organ, pews)
    { 20, ("SM_Bld_Preset_House_02_A_Optimized", 0f, 1.4f) },
    { 21, ("SM_Bld_Preset_House_09_A_Optimized", 0f, 1.4f) },
    { 5, ("SM_Prop_Destroyed_House_Preset_01", 0f, 1.3f) },         // the ruin in the woods north west of the castle
};
var frontsFile = @"C:\Users\kaaru\AppData\Local\Temp\claude\G--Ravenfall-Projects-Ravenfall-Legacy\86b0e615-81c6-408c-acb9-282e465846ce\scratchpad\fronts.txt";
// the fronts, found by eye (each preset turned 0, 90, 180, 270 and looked at from the south): "prefab yaw" lines
var fronts = new System.Collections.Generic.Dictionary<string, float>();
foreach (var line in System.IO.File.Exists("tools/ravebridge/lumbridge-fronts.txt") ? System.IO.File.ReadAllLines("tools/ravebridge/lumbridge-fronts.txt") : new string[0])
{
    var parts = line.Split(' ');
    if (parts.Length == 2 && float.TryParse(parts[1], out var yaw)) fronts[parts[0]] = yaw;
}

var buildings = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(BuildingsJson));
var red = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_02_A.mat");

// ---- the footprints ---------------------------------------------------------------------------------------------
stage = "footprints";
var swapped = new System.Collections.Generic.List<(int Id, System.Collections.Generic.HashSet<long> Tiles, int[] Bounds, int[][] Doors, float[] Centre)>();
foreach (var b in buildings)
{
    var id = (int)b["id"];
    if (!table.ContainsKey(id)) continue;
    var tiles = new System.Collections.Generic.HashSet<long>();
    foreach (var t in b["tiles"].ToObject<int[][]>()) tiles.Add(((long)t[0] << 20) | (uint)t[1]);
    swapped.Add((id, tiles, b["bounds"].ToObject<int[]>(), b["doors"].ToObject<int[][]>(), b["centre"].ToObject<float[]>()));
}
// a tile is the building's when it or a neighbour (the walls on its edges, the eaves) is in the footprint
System.Func<float, float, int> ownerAt = (wx, wz) =>
{
    int tx = UnityEngine.Mathf.FloorToInt(wx / Tile), ty = UnityEngine.Mathf.FloorToInt(wz / Tile);
    foreach (var s in swapped)
        for (var dx = -1; dx <= 1; dx++) for (var dy = -1; dy <= 1; dy++)
            if (s.Tiles.Contains(((long)(tx + dx) << 20) | (uint)(ty + dy))) return s.Id;
    return 0;
};
System.Func<float, float, int> insideOf = (wx, wz) =>
{
    int tx = UnityEngine.Mathf.FloorToInt(wx / Tile), ty = UnityEngine.Mathf.FloorToInt(wz / Tile);
    foreach (var s in swapped) if (s.Tiles.Contains(((long)tx << 20) | (uint)ty)) return s.Id;
    return 0;
};

// ---- take out the generated building ------------------------------------------------------------------------------
stage = "clear";
var removed = 0;
foreach (UnityEngine.Transform group in root.transform)
{
    var isWalls = group.name.StartsWith("Walls ") && !group.name.Contains("fence") && !group.name.Contains("gate") && !group.name.Contains("field") && group.name != "Walls low";
    var isObjects = group.name.StartsWith("Objects");
    if (!isWalls && !isObjects) continue;
    var kill = new System.Collections.Generic.List<UnityEngine.GameObject>();
    foreach (UnityEngine.Transform c in group)
    {
        // a wall by the middle of its piece, a prop by where it stands
        var b = new UnityEngine.Bounds(c.position, UnityEngine.Vector3.zero);
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
        var p = isWalls ? b.center : c.position;
        if ((isWalls ? ownerAt(p.x, p.z) : insideOf(p.x, p.z)) != 0) kill.Add(c.gameObject);
    }
    foreach (var k in kill) UnityEngine.Object.DestroyImmediate(k);
    removed += kill.Count;
}
// roofs, gables and floors: drop the triangles over the footprints
var cut = 0;
foreach (var name in new[] { "Roofs", "Floors" })
{
    var g = root.transform.Find(name);
    if (!g) continue;
    foreach (var mf in g.GetComponentsInChildren<UnityEngine.MeshFilter>())
    {
        var mesh = mf.sharedMesh;
        if (!mesh) continue;
        var copy = UnityEngine.Object.Instantiate(mesh);
        copy.name = mesh.name;
        var verts = copy.vertices;
        var subs = new System.Collections.Generic.List<int[]>();
        for (var si = 0; si < copy.subMeshCount; si++)
        {
            var tris = copy.GetTriangles(si);
            var keep = new System.Collections.Generic.List<int>();
            for (var i = 0; i < tris.Length; i += 3)
            {
                var c = (mf.transform.TransformPoint(verts[tris[i]]) + mf.transform.TransformPoint(verts[tris[i + 1]]) + mf.transform.TransformPoint(verts[tris[i + 2]])) / 3f;
                if (ownerAt(c.x, c.z) != 0) { cut++; continue; }
                keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
            }
            subs.Add(keep.ToArray());
        }
        for (var si = 0; si < subs.Count; si++) copy.SetTriangles(subs[si], si);
        copy.RecalculateBounds();
        mf.sharedMesh = copy;
        var col = mf.GetComponent<UnityEngine.MeshCollider>();
        if (col) col.sharedMesh = copy;
    }
}
sb.Append("taken out " + removed + " pieces and props, " + cut + " roof and floor triangles; ");

// ---- the stand-ins ------------------------------------------------------------------------------------------------
stage = "place";
var old = root.transform.Find("Ravenfall Buildings");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var parent = new UnityEngine.GameObject("Ravenfall Buildings").transform;
parent.SetParent(root.transform, false);
foreach (var s in swapped)
{
    var (prefabName, _, maxScale) = table[s.Id];
    UnityEngine.GameObject pf = null;
    foreach (var gid in UnityEditor.AssetDatabase.FindAssets(prefabName + " t:prefab"))
    {
        var p = UnityEditor.AssetDatabase.GUIDToAssetPath(gid);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == prefabName) { pf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); break; }
    }
    if (!pf) { sb.Append("no " + prefabName + "; "); continue; }
    // which way it faces: out through the map's door (on the edge "rot" of its tile: 0 west, 1 north, 2 east, 3 south;
    // out of the footprint when the door's tile is inside it, into it when outside)
    var side = 3;
    if (s.Doors.Length > 0)
    {
        var d = s.Doors[0];
        var inside = s.Tiles.Contains(((long)d[0] << 20) | (uint)d[1]);
        side = inside ? d[2] : (d[2] + 2) % 4;
    }
    var sideYaw = side == 3 ? 0f : side == 0 ? 90f : side == 1 ? 180f : 270f;
    var frontYaw = fronts.TryGetValue(prefabName, out var fy) ? fy : table[s.Id].FrontYaw;
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, parent);
    o.name = "Building " + s.Id + " (" + prefabName.Replace("SM_Bld_Preset_", "").Replace("_Optimized", "") + ")";
    o.transform.rotation = UnityEngine.Quaternion.Euler(0f, frontYaw + sideYaw, 0f);
    // its footprint, turned, against the building's
    var bb = new UnityEngine.Bounds(); var first = true;
    foreach (var r in o.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); }
    float bw = (s.Bounds[2] - s.Bounds[0] + 1) * Tile, bd = (s.Bounds[3] - s.Bounds[1] + 1) * Tile;
    var scale = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.Min(bw / UnityEngine.Mathf.Max(1f, bb.size.x), bd / UnityEngine.Mathf.Max(1f, bb.size.z)) * 0.95f, 0.8f, maxScale);
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    // on the highest ground under the footprint, its middle on the footprint's middle
    var top = float.MinValue;
    foreach (var k in s.Tiles) { int tx = (int)(k >> 20), ty = (int)(k & 0xfffff); top = UnityEngine.Mathf.Max(top, groundAt((tx + 0.5f) * Tile, (ty + 0.5f) * Tile)); }
    bb = new UnityEngine.Bounds(); first = true;
    foreach (var r in o.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); }
    var target = new UnityEngine.Vector3(s.Centre[0] * Tile, top, s.Centre[1] * Tile);
    o.transform.position += new UnityEngine.Vector3(target.x - bb.center.x, top - o.transform.position.y, target.z - bb.center.z);
    // Lumbridge's red tiles (thatched ones keep their own palette: in the red one their thatch is mossy green)
    var thatched = prefabName.Contains("Hut_") || prefabName.Contains("Shelter_") || prefabName.Contains("House_04");
    if (red && prefabName.StartsWith("SM_Bld_Preset") && !thatched)
        foreach (var r in o.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
        {
            var ms = r.sharedMaterials; var changed = false;
            for (var i = 0; i < ms.Length; i++)
                if (ms[i] && ms[i].name.StartsWith("PolygonFantasyKingdom_Mat_0") && !ms[i].name.Contains("Glass")) { ms[i] = red; changed = true; }
            if (changed) r.sharedMaterials = ms;
        }
    UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, UnityEditor.StaticEditorFlags.BatchingStatic);
    sb.Append(s.Id + " " + o.name.Replace("Building " + s.Id + " ", "") + " x" + scale.ToString("F2") + " facing " + "WNES"[side] + "; ");
}

// ---- the ground under them -------------------------------------------------------------------------------------------
stage = "ground";
{
    // the old buildings' floors were painted as paving: each footprint tile takes the paint most common in the ring of
    // tiles round its building, so the new house stands on the ground round it (a road reaching the door stays a road)
    var td = terrain.terrainData;
    var res = td.alphamapResolution;
    var alpha = td.GetAlphamaps(0, 0, res, res);
    var layers = td.alphamapLayers;
    var size = td.size.x;
    System.Func<int, int, int> texel = (tx, ty) => 0;
    var painted = 0;
    foreach (var s in swapped)
    {
        // the ring: tiles next to the footprint, not in it
        var counts = new float[layers];
        foreach (var k in s.Tiles)
        {
            int tx = (int)(k >> 20), ty = (int)(k & 0xfffff);
            for (var dx = -2; dx <= 2; dx++) for (var dy = -2; dy <= 2; dy++)
            {
                var nk = ((long)(tx + dx) << 20) | (uint)(ty + dy);
                if (s.Tiles.Contains(nk)) continue;
                int ai = UnityEngine.Mathf.Clamp((int)(((tx + dx) + 0.5f) * Tile / size * res), 0, res - 1);
                int aj = UnityEngine.Mathf.Clamp((int)(((ty + dy) + 0.5f) * Tile / size * res), 0, res - 1);
                for (var l = 0; l < layers; l++) counts[l] += alpha[aj, ai, l];
            }
        }
        // roads are not the ground round a house (the footpath and cobble layers)
        counts[1] *= 0.3f; if (layers > 7) counts[7] *= 0.3f;
        var best = 0; for (var l = 1; l < layers; l++) if (counts[l] > counts[best]) best = l;
        foreach (var k in s.Tiles)
        {
            int tx = (int)(k >> 20), ty = (int)(k & 0xfffff);
            int i0 = (int)(tx * Tile / size * res), i1 = (int)((tx + 1) * Tile / size * res);
            int j0 = (int)(ty * Tile / size * res), j1 = (int)((ty + 1) * Tile / size * res);
            for (var j = UnityEngine.Mathf.Max(0, j0); j <= UnityEngine.Mathf.Min(res - 1, j1); j++)
            for (var i = UnityEngine.Mathf.Max(0, i0); i <= UnityEngine.Mathf.Min(res - 1, i1); i++)
            {
                for (var l = 0; l < layers; l++) alpha[j, i, l] = l == best ? 1f : 0f;
                painted++;
            }
        }
    }
    td.SetAlphamaps(0, 0, alpha);
    UnityEditor.EditorUtility.SetDirty(td);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
    sb.Append("repainted " + painted + " texels; ");
}

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEngine.Debug.Log("[lumbridge-swap] " + sb);
return sb.ToString();

}
catch (System.Exception ex)
{
    UnityEngine.Debug.Log("[lumbridge-swap] failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
