// Run with: unity command eval_file --file tools/ravebridge/lumbridge-test.cs
//
// Builds Lumbridge as the OSRS map has it, out of Ravenfall's own pieces, in a test scene of its own
// (Assets/Scenes/LumbridgeTest.unity), to see how far Ravebridge could follow it before touching MainWorld. The map
// comes from the user's rs-map-viewer clone (G:\GitHub\rs-map-viewer, scripts/cache/export-region.ts): 192 x 192 tiles
// from OSRS 3136, 3136, with tile heights, the ground's types and every object's name, kind, place, turn and colour.
// Only the layout is followed; nothing of Jagex's models or textures comes in:
// - the terrain: heights from the tiles, the river, sea and swamp pools dug out under water, the ground painted from
//   the tile types (grass, dry grass, dirt paths, stone roads, the desert, the swamp's mud);
// - walls tile by tile, along the tile edges the map puts them on, from Synty pieces picked by the wall's colour and
//   height (castle stone, plaster, wood, fences, low field walls), doors and gates where the map has them;
// - roofs over each roofed footprint, a hipped roof in the roof's own colour;
// - trees, plants, rocks and props by name.
// One OSRS tile is 2.5 m (a Synty house wall); a floor is 2.5 m. Rerun to rebuild; it opens the test scene first.
var sb = new System.Text.StringBuilder();
var stage = "start";
try
{

const string Json = @"G:\GitHub\rs-map-viewer\out\lumbridge.json";
const string ScenePath = "Assets/Scenes/LumbridgeTest.unity";
const string Folder = "Assets/Scenes/LumbridgeTest";
const float Tile = 2.5f, Unit = Tile / 128f, Floor = 2.5f;
const float Base = -6f, Span = 30f;          // the terrain's foot and height range
const float WaterDepth = 1.8f;
const float HillScale = 0.6f;            // OSRS's heights read steep at 2.5 m a tile

if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
if (!System.IO.File.Exists(Json)) return "no " + Json + " (run scripts/cache/export-region.ts in rs-map-viewer)";

// ---- the test scene ------------------------------------------------------------------------------------------
stage = "scene";
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (active.path != ScenePath)
{
    if (active.isDirty) return "the open scene " + active.path + " has unsaved changes; save or discard them first";
    if (System.IO.File.Exists(ScenePath)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
    else
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
    }
}
if (!UnityEditor.AssetDatabase.IsValidFolder(Folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes", "LumbridgeTest");
var oldRoot = UnityEngine.GameObject.Find("Lumbridge");
if (oldRoot) UnityEngine.Object.DestroyImmediate(oldRoot);
var root = new UnityEngine.GameObject("Lumbridge").transform;

var map = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(Json));
int N = (int)map["sizeX"];
var heights = map["heights"].ToObject<int[][]>();
var underlay = map["underlay"].ToObject<int[][]>();
var overlay = map["overlay"].ToObject<int[][]>();
var locs = (Newtonsoft.Json.Linq.JArray)map["locs"];
// bridge tiles: their level 1 is drawn a level down, as the game does; the deck stands a little over the water
var bridgeSet = new System.Collections.Generic.HashSet<long>();
foreach (var b in map["bridges"].ToObject<int[][]>()) bridgeSet.Add(((long)b[0] << 20) | (uint)b[1]);
System.Func<int, int, bool> isBridge = (x, y) => bridgeSet.Contains(((long)x << 20) | (uint)y);
System.Func<int, int, int, int> levelAt = (x, y, level) => level > 0 && isBridge(x, y) ? level - 1 : level;
const float Deck = 0.7f;

// ground height (m) at a tile corner and anywhere between, before any digging
System.Func<int, int, float> corner = (cx, cy) =>
{
    cx = System.Math.Clamp(cx, 0, heights.Length - 1);
    var col = heights[cx];
    cy = System.Math.Clamp(cy, 0, col.Length - 1);
    return -col[cy] * Unit * HillScale;
};
System.Func<float, float, float> ground = (tx, ty) =>
{
    var x0 = UnityEngine.Mathf.FloorToInt(tx); var y0 = UnityEngine.Mathf.FloorToInt(ty);
    var fx = tx - x0; var fy = ty - y0;
    return UnityEngine.Mathf.Lerp(UnityEngine.Mathf.Lerp(corner(x0, y0), corner(x0 + 1, y0), fx), UnityEngine.Mathf.Lerp(corner(x0, y0 + 1), corner(x0 + 1, y0 + 1), fx), fy);
};
System.Func<int, int, int> overlayAt = (x, y) => x < 0 || y < 0 || x >= N || y >= N ? 0 : overlay[x][y];
System.Func<int, int, int> underlayAt = (x, y) => x < 0 || y < 0 || x >= N || y >= N ? 0 : underlay[x][y];
// the sea (442), the river (6) and the swamp's pools (7)
System.Func<int, int, bool> water = (x, y) => { var o = overlayAt(x, y); return o == 442 || o == 6 || o == 7; };

// ---- terrain ---------------------------------------------------------------------------------------------------
stage = "terrain";
// Unity only takes 2^n + 1 heights a side and rounds anything else up: at 385 (a height every half tile) it made 513
// and the map covered three quarters of the terrain, offset from everything built on it
var res = 513;
var td = new UnityEngine.TerrainData();
td.heightmapResolution = res;
td.size = new UnityEngine.Vector3(N * Tile, Span, N * Tile);
res = td.heightmapResolution;
var hs = new float[res, res];
for (var j = 0; j < res; j++)
for (var i = 0; i < res; i++)
{
    var tx = i * (float)N / (res - 1); var ty = j * (float)N / (res - 1);
    var y = ground(tx, ty);
    // dug out under water: fully where every tile at this point is water, part way on the shore
    int wet = 0, all = 0;
    for (var dx = -1; dx <= 1; dx += 2) for (var dy = -1; dy <= 1; dy += 2)
    {
        all++; if (water(UnityEngine.Mathf.FloorToInt(tx + dx * 0.3f), UnityEngine.Mathf.FloorToInt(ty + dy * 0.3f))) wet++;
    }
    if (wet > 0) y -= wet == all ? WaterDepth : WaterDepth * 0.35f * wet / all;
    hs[j, i] = UnityEngine.Mathf.Clamp01((y - Base) / Span);
}
{
    var smooth = (float[,])hs.Clone();
    for (var j = 1; j < res - 1; j++)
    for (var i = 1; i < res - 1; i++)
    {
        float sum = 0; for (var a = -1; a <= 1; a++) for (var b = -1; b <= 1; b++) sum += hs[j + a, i + b];
        // only where the ground is not dug for water (the banks keep their shape)
        var stx = i * (float)N / (res - 1); var sty = j * (float)N / (res - 1);
        if (!water(UnityEngine.Mathf.FloorToInt(stx + 0.4f), UnityEngine.Mathf.FloorToInt(sty + 0.4f)) && !water(UnityEngine.Mathf.FloorToInt(stx - 0.4f), UnityEngine.Mathf.FloorToInt(sty - 0.4f))) smooth[j, i] = sum / 9f;
    }
    hs = smooth;
}
td.SetHeights(0, 0, hs);

// the ground's paint, layers as Ravebridge's (0 dirt and desert, 1 footpath, 2 grass, 3 second grass, 4 mud, 5 snow,
// 6 rock, 7 cobblestones), from each tile's overlay, else its underlay
var rbTerrainMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Terrain/Ravebridge Terrain South.mat");
UnityEngine.TerrainLayer[] layers = null;
foreach (var t in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.TerrainData>())
    if (t.terrainLayers != null && t.terrainLayers.Length == 8 && t.terrainLayers[7] && t.terrainLayers[7].name == "Terrain_Meadow_Cobblestone_01") { layers = t.terrainLayers; break; }
if (layers == null)
{
    var paths = new[] { "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Dirt_01", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Footpath_Tile_01", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Grass_01", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Grass_02", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Mud_01", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Grass_02", "PNB_Tropical_Jungle/Terrain/Terrain_Rock_01", "PNB_Meadow_Forest/Terrain/Terrain_Meadow_Cobblestone_01" };
    layers = System.Array.ConvertAll(paths, p => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>("Assets/PolygonNatureBiomes/" + p + ".terrainlayer"));
}
td.terrainLayers = layers;
System.Func<int, int, int> paintOf = (x, y) =>
{
    var o = overlayAt(x, y);
    if (o > 0)
    {
        switch (o)
        {
            case 10: case 2: case 3: case 4: case 12: case 5: return 7;          // stone roads and floors
            case 14: case 22: case 101: case 16: case 89: case 108: case 102: return 1;  // dirt paths
            case 26: case 227: case 123: case 77: return 0;                    // the desert's roads and sand
            case 442: case 6: case 7: return 4;                                // under water: mud
            default: return 1;
        }
    }
    switch (underlayAt(x, y))
    {
        case 62: case 68: case 61: return 0;                                   // desert sand
        case 63: case 96: case 67: case 64: case 65: return 1;                 // bare earth
        case 54: return 4;                                                     // the swamp
        case 50: case 53: case 56: return 3;                                   // dry grass
        default: return 2;                                                     // grass
    }
};
var aRes = 512;
td.alphamapResolution = aRes;
var alpha = new float[aRes, aRes, 8];
for (var j = 0; j < aRes; j++)
for (var i = 0; i < aRes; i++)
{
    // a little blur: the tiles under a 3 x 3 spread of samples round this one
    for (var sx = -1; sx <= 1; sx++)
    for (var sy = -1; sy <= 1; sy++)
    {
        var tx = (i + 0.5f + sx * 0.45f) / aRes * N;
        var ty = (j + 0.5f + sy * 0.45f) / aRes * N;
        alpha[j, i, paintOf(UnityEngine.Mathf.FloorToInt(tx), UnityEngine.Mathf.FloorToInt(ty))] += 1f / 9f;
    }
}
var tdPath = Folder + "/Lumbridge TerrainData.asset";
UnityEditor.AssetDatabase.DeleteAsset(tdPath);
UnityEditor.AssetDatabase.CreateAsset(td, tdPath);
var terrainGo = UnityEngine.Terrain.CreateTerrainGameObject(td);
terrainGo.name = "Terrain";
terrainGo.transform.SetParent(root, false);
terrainGo.transform.position = new UnityEngine.Vector3(0, Base, 0);
var terrain = terrainGo.GetComponent<UnityEngine.Terrain>();

if (rbTerrainMat)
{
    var matPath = Folder + "/Lumbridge Terrain.mat";
    UnityEditor.AssetDatabase.DeleteAsset(matPath);
    var mat = new UnityEngine.Material(rbTerrainMat);
    var textures = td.alphamapTextures;
    mat.SetTexture("_RoadMask", textures[0]);
    if (textures.Length > 1) mat.SetTexture("_SnowMask", textures[1]);
    // its tilings are over the terrain: Ravebridge's are for 2560 m
    var k = N * Tile / 2560f;
    foreach (var p in new[] { "_LayerUVScale", "_MainUVScale", "_SecondaryUVScale" }) { var v = mat.GetVector(p); mat.SetVector(p, new UnityEngine.Vector4(v.x * k, v.y * k, 0, 0)); }
    mat.SetFloat("_SecondaryHeight", -40f);   // no beach: grass everywhere above the water
    mat.SetFloat("_SeaLevel", -40f);
    UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
    terrain.materialTemplate = mat;
}
terrain.heightmapPixelError = 3;
sb.Append("terrain " + res + "; ");

// ---- water -----------------------------------------------------------------------------------------------------
stage = "water";
{
    var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var tris = new System.Collections.Generic.List<int>();
    for (var x = 0; x < N; x++)
    for (var y = 0; y < N; y++)
    {
        if (!water(x, y)) continue;
        // a tile of water at the tile's own height, reaching a little past its edges under the banks
        var h = (corner(x, y) + corner(x + 1, y) + corner(x, y + 1) + corner(x + 1, y + 1)) * 0.25f - 0.25f;
        var v = verts.Count;
        const float pad = 0.35f;
        verts.Add(new UnityEngine.Vector3(x * Tile - pad, h, y * Tile - pad));
        verts.Add(new UnityEngine.Vector3(x * Tile - pad, h, (y + 1) * Tile + pad));
        verts.Add(new UnityEngine.Vector3((x + 1) * Tile + pad, h, (y + 1) * Tile + pad));
        verts.Add(new UnityEngine.Vector3((x + 1) * Tile + pad, h, y * Tile - pad));
        tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
    }
    var mesh = new UnityEngine.Mesh { name = "Lumbridge Water", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
    mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
    var meshPath = Folder + "/Lumbridge Water.asset";
    UnityEditor.AssetDatabase.DeleteAsset(meshPath);
    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
    var go = new UnityEngine.GameObject("Water");
    go.transform.SetParent(root, false);
    go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    var r = go.AddComponent<UnityEngine.MeshRenderer>();
    r.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/Misc/PolygonFantasyKingdom_Mat_Water.mat");
    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    sb.Append("water tiles " + verts.Count / 4 + "; ");
}

// ---- pieces ------------------------------------------------------------------------------------------------------
stage = "pieces";
var prefabCache = new System.Collections.Generic.Dictionary<string, UnityEngine.GameObject>();
System.Func<string, UnityEngine.GameObject> prefab = name =>
{
    if (prefabCache.TryGetValue(name, out var cached)) return cached;
    UnityEngine.GameObject found = null;
    if (name.StartsWith("Assets/")) found = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(name);
    else foreach (var g in UnityEditor.AssetDatabase.FindAssets(name + " t:prefab"))
    {
        var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == name) { found = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); break; }
    }
    prefabCache[name] = found;
    return found;
};
// a prefab's extent in its own space, from its meshes
var boundsCache = new System.Collections.Generic.Dictionary<UnityEngine.GameObject, UnityEngine.Bounds>();
System.Func<UnityEngine.GameObject, UnityEngine.Bounds> extent = go =>
{
    if (boundsCache.TryGetValue(go, out var bc)) return bc;
    var b = new UnityEngine.Bounds(); var first = true;
    foreach (var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        if (!mf.sharedMesh) continue;
        var mb = mf.sharedMesh.bounds;
        var m = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
        for (var c = 0; c < 8; c++)
        {
            var p = m.MultiplyPoint3x4(mb.center + UnityEngine.Vector3.Scale(mb.extents, new UnityEngine.Vector3((c & 1) * 2 - 1, (c & 2) - 1, (c & 4) / 2 - 1)));
            if (first) { b = new UnityEngine.Bounds(p, UnityEngine.Vector3.zero); first = false; } else b.Encapsulate(p);
        }
    }
    boundsCache[go] = b;
    return b;
};
var groups = new System.Collections.Generic.Dictionary<string, UnityEngine.Transform>();
System.Func<string, UnityEngine.Transform> group = name =>
{
    if (groups.TryGetValue(name, out var t)) return t;
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(root, false);
    groups[name] = go.transform;
    return go.transform;
};
System.Func<UnityEngine.GameObject, UnityEngine.Transform, UnityEngine.GameObject> place = (pf, parent) =>
{
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, parent);
    UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, UnityEditor.StaticEditorFlags.BatchingStatic | UnityEditor.StaticEditorFlags.NavigationStatic);
    return o;
};
// a wall piece stretched along a line from a to b (its own x along the line, its foot at the line's height), height
// scaled to h (0 keeps its own)
System.Action<UnityEngine.GameObject, UnityEngine.Transform, UnityEngine.Vector3, UnityEngine.Vector3, float> span = (pf, parent, a, b, h) =>
{
    var e = extent(pf);
    var len = UnityEngine.Vector3.Distance(new UnityEngine.Vector3(a.x, 0, a.z), new UnityEngine.Vector3(b.x, 0, b.z));
    var dir = (new UnityEngine.Vector3(b.x - a.x, 0, b.z - a.z)).normalized;
    var yaw = UnityEngine.Mathf.Atan2(-dir.z, dir.x) * UnityEngine.Mathf.Rad2Deg;
    var rot = UnityEngine.Quaternion.Euler(0, yaw, 0);
    var sx = len / UnityEngine.Mathf.Max(0.01f, e.size.x);
    var sy = h > 0 ? h / UnityEngine.Mathf.Max(0.01f, e.size.y) : 1f;
    var o = place(pf, parent);
    o.transform.localScale = new UnityEngine.Vector3(sx, sy, 1f);
    var foot = UnityEngine.Mathf.Min(a.y, b.y);
    o.transform.SetPositionAndRotation(new UnityEngine.Vector3(a.x, foot, a.z) - rot * new UnityEngine.Vector3(e.min.x * sx, e.min.y * sy, e.center.z), rot);
};

// ---- walls -------------------------------------------------------------------------------------------------------
stage = "walls";
// the wall's kind from its look: invisible blockers have no colour; low ones are field walls; brown and below a
// floor's height are fences; full height ones are stone (grey), plaster (light) or wood (brown)
System.Func<Newtonsoft.Json.Linq.JToken, string> wallKind = l =>
{
    var name = (string)l["name"] ?? "null";
    var look = l["look"];
    if (look == null || look["rgb"] == null || look["rgb"].Type == Newtonsoft.Json.Linq.JTokenType.Null) return null;
    var rgb = look["rgb"].ToObject<int[]>();
    var h = (int)look["height"];
    var brown = rgb[0] > rgb[2] * 1.5f && rgb[0] > 80;
    if (name.Contains("door") || name.Contains("Door")) return h < 200 ? "gate" : "door";
    if (name == "Gate") return "gate";
    if (h < 110) return "low";
    if (h < 200) return brown ? "fence" : "low";
    var light = rgb[0] + rgb[1] + rgb[2] > 480;
    if (light) return "plaster";
    if (brown) return "wood";
    return "stone";
};
var wallPieces = new System.Collections.Generic.Dictionary<string, string>
{
    { "stone", "SM_Bld_Castle_Wall_01" },
    { "plaster", "SM_Bld_House_Wall_01" },
    { "wood", "SM_Bld_House_Wall_01" },
    { "fence", "SM_Prop_Fence_03" },
    { "low", "SM_Env_StoneWall_01" },
    { "door", "SM_Bld_House_Wall_Door_01" },
    { "gate", "SM_Prop_Fence_Gate_01" },
};
var wallCount = 0; var skipped = 0;
var kinds = new System.Collections.Generic.Dictionary<string, int>();
foreach (var l in locs)
{
    var type = (int)l["type"];
    if (type != 0 && type != 2 && type != 9) continue;
    var kind = wallKind(l);
    if (kind == null) { skipped++; continue; }
    var pf = prefab(wallPieces[kind]);
    if (!pf) { skipped++; continue; }
    int x = (int)l["x"], y = (int)l["y"], rot = (int)l["rot"], level = (int)l["level"];
    var onBridge = level > 0 && isBridge(x, y);
    level = levelAt(x, y, level);
    var lift = level * Floor + (onBridge ? Deck : 0f);
    // full walls reach a little into the ground (or the floor below), so a slope never shows under them
    const float Sink = 0.8f;
    var full = !(kind == "low" || kind == "fence" || kind == "gate");
    var height = full ? Floor + Sink : 0f;
    lift -= full ? Sink : (onBridge ? 0f : 0.05f);
    var parent = group("Walls " + kind);
    // the tile's corners, south west first, going round: 0 (x, y), 1 (x, y+1), 2 (x+1, y+1), 3 (x+1, y)
    System.Func<int, UnityEngine.Vector3> cornerAt = c =>
    {
        var cx = x + (c >= 2 ? 1 : 0); var cy = y + (c == 1 || c == 2 ? 1 : 0);
        return new UnityEngine.Vector3(cx * Tile, corner(cx, cy) + lift, cy * Tile);
    };
    if (type == 9)
    {
        // across the tile: rot 0 and 2 from south west to north east, 1 and 3 the other way
        if (rot % 2 == 0) span(pf, parent, cornerAt(0), cornerAt(2), height); else span(pf, parent, cornerAt(1), cornerAt(3), height);
    }
    else
    {
        // a wall on the tile's west (rot 0), north (1), east (2) or south (3) edge; a corner on two of them
        System.Action<int> edge = side => span(pf, parent, cornerAt(side), cornerAt((side + 1) % 4), height);
        edge(rot);
        if (type == 2) edge((rot + 1) % 4);
    }
    wallCount++;
    kinds[kind] = kinds.TryGetValue(kind, out var kc) ? kc + 1 : 1;
}
sb.Append("walls " + wallCount + " (");
foreach (var kv in kinds) sb.Append(kv.Key + " " + kv.Value + " ");
sb.Append(") skipped " + skipped + "; ");

// ---- upper floors ------------------------------------------------------------------------------------------------
stage = "floors";
{
    // a slab for each level's floor tiles, a floor up from the tile's ground: the castle's storeys and its flat roof
    // with the battlements round it, the upper rooms of houses (under their roofs)
    const float Thick = 0.35f;
    var floorMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")) { name = "Lumbridge Floor" };
    floorMat.SetColor("_BaseColor", new UnityEngine.Color(0.47f, 0.45f, 0.42f));
    floorMat.SetFloat("_Smoothness", 0.05f);
    var fmPath = Folder + "/Lumbridge Floor.mat";
    UnityEditor.AssetDatabase.DeleteAsset(fmPath);
    UnityEditor.AssetDatabase.CreateAsset(floorMat, fmPath);
    var floorsJ = map["floors"].ToObject<int[][][]>();
    for (var level = 1; level < floorsJ.Length; level++)
    {
        var set = new System.Collections.Generic.HashSet<long>();
        foreach (var f in floorsJ[level]) set.Add(((long)f[0] << 20) | (uint)f[1]);
        if (set.Count == 0) continue;
        var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        System.Action<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3> quad = (a, b, c, d) =>
        {
            var v = verts.Count; verts.AddRange(new[] { a, b, c, d }); tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        };
        foreach (var f in floorsJ[level])
        {
            int x = f[0], y = f[1];
            var top = (corner(x, y) + corner(x + 1, y) + corner(x, y + 1) + corner(x + 1, y + 1)) * 0.25f + levelAt(x, y, level) * Floor + (isBridge(x, y) ? Deck : 0f);
            float x0 = x * Tile, x1 = (x + 1) * Tile, z0 = y * Tile, z1 = (y + 1) * Tile, lo = top - Thick;
            quad(new UnityEngine.Vector3(x0, top, z0), new UnityEngine.Vector3(x0, top, z1), new UnityEngine.Vector3(x1, top, z1), new UnityEngine.Vector3(x1, top, z0));
            quad(new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x0, lo, z1));
            if (!set.Contains(((long)(x - 1) << 20) | (uint)y)) quad(new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x0, lo, z1), new UnityEngine.Vector3(x0, top, z1), new UnityEngine.Vector3(x0, top, z0));
            if (!set.Contains(((long)(x + 1) << 20) | (uint)y)) quad(new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x1, top, z0), new UnityEngine.Vector3(x1, top, z1));
            if (!set.Contains(((long)x << 20) | (uint)(y - 1))) quad(new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x0, top, z0), new UnityEngine.Vector3(x1, top, z0));
            if (!set.Contains(((long)x << 20) | (uint)(y + 1))) quad(new UnityEngine.Vector3(x0, lo, z1), new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x1, top, z1), new UnityEngine.Vector3(x0, top, z1));
        }
        var mesh = new UnityEngine.Mesh { name = "Floors " + level, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new UnityEngine.GameObject("Floors " + level);
        go.transform.SetParent(group("Floors"), false);
        go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        go.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = floorMat;
        go.AddComponent<UnityEngine.MeshCollider>().sharedMesh = mesh;
    }
    sb.Append("floors; ");
}

// ---- roofs -------------------------------------------------------------------------------------------------------
stage = "roofs";
{
    // the roofed tiles and their colour; each roofed footprint gets a hipped roof: a corner rises with its distance in
    // from the footprint's edge, so a rectangle gets a ridge and an L its valleys. The map puts a roof's slopes and its
    // top on different levels; taken level by level they came out as thin slivers and holes, so the footprint is every
    // roof tile together, standing on the lowest level any of its tiles has
    const float Rise = 0.75f;   // metres up for each metre in
    var roofLevel = new System.Collections.Generic.Dictionary<long, int>();
    var byLevel = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<long, UnityEngine.Color>>();
    foreach (var l in locs)
    {
        var type = (int)l["type"];
        if (type < 12 || type > 21) continue;
        var level = levelAt((int)l["x"], (int)l["y"], (int)l["level"]);
        var tk = ((long)(int)l["x"] << 20) | (uint)(int)l["y"];
        roofLevel[tk] = roofLevel.TryGetValue(tk, out var was) ? System.Math.Min(was, level) : level;
        level = 0;
        var look = l["look"];
        var c = new UnityEngine.Color(0.55f, 0.25f, 0.18f);
        if (look != null && look["rgb"] != null && look["rgb"].Type != Newtonsoft.Json.Linq.JTokenType.Null)
        {
            var rgb = look["rgb"].ToObject<int[]>();
            c = new UnityEngine.Color(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f);
        }
        if (!byLevel.TryGetValue(level, out var tiles)) byLevel[level] = tiles = new System.Collections.Generic.Dictionary<long, UnityEngine.Color>();
        tiles[((long)(int)l["x"] << 20) | (uint)(int)l["y"]] = c;
    }
    var roofMats = new System.Collections.Generic.Dictionary<int, UnityEngine.Material>();
    var lit = UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
    var roofs = 0;
    foreach (var lv in byLevel)
    {
        var tiles = lv.Value;
        System.Func<int, int, bool> roofed = (x, y) => tiles.ContainsKey(((long)x << 20) | (uint)y);
        // a corner's height: its distance (in tiles, chessboard) to the nearest tile that is not roofed
        System.Func<int, int, float> rise = (cx, cy) =>
        {
            for (var d = 0; d < 12; d++)
            {
                for (var tx = cx - 1 - d; tx <= cx + d; tx++)
                for (var ty = cy - 1 - d; ty <= cy + d; ty++)
                {
                    var edge = tx == cx - 1 - d || tx == cx + d || ty == cy - 1 - d || ty == cy + d;
                    if (edge && !roofed(tx, ty)) return d;
                }
            }
            return 12;
        };
        // the footprints one by one, flood filled, each a mesh of its own in its main colour
        var seen = new System.Collections.Generic.HashSet<long>();
        foreach (var key in tiles.Keys)
        {
            if (seen.Contains(key)) continue;
            var part = new System.Collections.Generic.List<long>();
            var open = new System.Collections.Generic.Stack<long>();
            open.Push(key); seen.Add(key);
            while (open.Count > 0)
            {
                var k = open.Pop(); part.Add(k);
                int kx = (int)(k >> 20), ky = (int)(k & 0xfffff);
                foreach (var n in new[] { (kx + 1, ky), (kx - 1, ky), (kx, ky + 1), (kx, ky - 1) })
                {
                    var nk = ((long)n.Item1 << 20) | (uint)n.Item2;
                    if (tiles.ContainsKey(nk) && seen.Add(nk)) open.Push(nk);
                }
            }
            // the roof's foot: the highest ground under it, a floor up for each level below the roof's
            float foot = float.MinValue; float r = 0, g = 0, b = 0;
            var lowest = int.MaxValue;
            foreach (var k in part) lowest = System.Math.Min(lowest, roofLevel[k]);
            foreach (var k in part)
            {
                int kx = (int)(k >> 20), ky = (int)(k & 0xfffff);
                foot = UnityEngine.Mathf.Max(foot, UnityEngine.Mathf.Max(UnityEngine.Mathf.Max(corner(kx, ky), corner(kx + 1, ky)), UnityEngine.Mathf.Max(corner(kx, ky + 1), corner(kx + 1, ky + 1))));
                var c = tiles[k]; r += c.r; g += c.g; b += c.b;
            }
            foot += lowest * Floor;
            var colour = new UnityEngine.Color(r / part.Count, g / part.Count, b / part.Count);
            var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            const float eave = 0.3f;   // past the walls
            foreach (var k in part)
            {
                int kx = (int)(k >> 20), ky = (int)(k & 0xfffff);
                // corners of the tile: (x,y) (x,y+1) (x+1,y+1) (x+1,y), pushed out at the footprint's edge for the eaves
                var cs = new[] { (kx, ky), (kx, ky + 1), (kx + 1, ky + 1), (kx + 1, ky) };
                var p = new UnityEngine.Vector3[4];
                for (var c = 0; c < 4; c++)
                {
                    var (cx, cy) = cs[c];
                    var h = rise(cx, cy) * Tile * Rise;
                    var px = cx * Tile; var pz = cy * Tile;
                    if (h == 0)
                    {
                        // an edge corner: out over the wall, away from the roofed tiles round it, and a little down
                        var west = (roofed(cx - 1, cy - 1) ? 1 : 0) + (roofed(cx - 1, cy) ? 1 : 0);
                        var east = (roofed(cx, cy - 1) ? 1 : 0) + (roofed(cx, cy) ? 1 : 0);
                        var south = (roofed(cx - 1, cy - 1) ? 1 : 0) + (roofed(cx, cy - 1) ? 1 : 0);
                        var north = (roofed(cx - 1, cy) ? 1 : 0) + (roofed(cx, cy) ? 1 : 0);
                        var ox = System.Math.Sign(west - east) * eave;
                        var oz = System.Math.Sign(south - north) * eave;
                        px += ox; pz += oz; h = -eave * Rise;
                    }
                    p[c] = new UnityEngine.Vector3(px, foot + h, pz);
                }
                // two triangles, split along the diagonal whose ends are closer in height (keeps ridges straight)
                var v = verts.Count;
                if (UnityEngine.Mathf.Abs(p[0].y - p[2].y) <= UnityEngine.Mathf.Abs(p[1].y - p[3].y))
                {
                    verts.AddRange(new[] { p[0], p[1], p[2], p[0], p[2], p[3] });
                }
                else
                {
                    verts.AddRange(new[] { p[0], p[1], p[3], p[1], p[2], p[3] });
                }
                tris.AddRange(new[] { v, v + 1, v + 2, v + 3, v + 4, v + 5 });
            }
            var mesh = new UnityEngine.Mesh { name = "Roof" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new UnityEngine.GameObject("Roof " + lv.Key);
            go.transform.SetParent(group("Roofs"), false);
            go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<UnityEngine.MeshRenderer>();
            // one material per roof colour, a little darker and warmer than the map's as Synty's roofs are
            var q = UnityEngine.Mathf.RoundToInt(colour.r * 15) * 256 + UnityEngine.Mathf.RoundToInt(colour.g * 15) * 16 + UnityEngine.Mathf.RoundToInt(colour.b * 15);
            if (!roofMats.TryGetValue(q, out var m))
            {
                m = new UnityEngine.Material(lit) { name = "Lumbridge Roof " + q };
                m.SetColor("_BaseColor", colour);
                m.SetFloat("_Smoothness", 0.1f);
                m.SetFloat("_Cull", 0f);
                var mp = Folder + "/" + m.name + ".mat";
                UnityEditor.AssetDatabase.DeleteAsset(mp);
                UnityEditor.AssetDatabase.CreateAsset(m, mp);
                roofMats[q] = m;
            }
            mr.sharedMaterial = m;
            roofs++;
            // the meshes go into the scene: saved with it
        }
    }
    sb.Append("roofs " + roofs + " in " + roofMats.Count + " colours; ");
}

// ---- trees, plants, rocks and props ------------------------------------------------------------------------------
stage = "objects";
var rng = new System.Random(3222);
var things = new System.Collections.Generic.Dictionary<string, string[]>
{
    { "Tree", new[] { "SM_Env_Tree_Round_01", "SM_Env_Tree_Round_02", "SM_Env_Tree_Round_03", "SM_Env_Tree_Round_04" } },
    { "Oak tree", new[] { "SM_Env_Tree_Large_01", "SM_Env_Tree_Large_02" } },
    { "Oak", new[] { "SM_Env_Tree_Large_01", "SM_Env_Tree_Large_02" } },
    // PolygonNature's willows have a shader URP cannot draw
    { "Willow tree", new[] { "SM_Env_Tree_Thin_01", "SM_Env_Tree_Thin_03" } },
    { "Willow", new[] { "SM_Env_Tree_Thin_02" } },
    { "Dead tree", new[] { "SM_Env_Tree_Dead_01", "SM_Env_Tree_Dead_02", "SM_Env_Tree_Dead_03" } },
    { "Tree stump", new[] { "SM_Generic_TreeStump_01" } },
    { "Bush", new[] { "SM_Env_Bush_01", "SM_Env_Bush_02", "SM_Env_Bush_03" } },
    { "Plant", new[] { "SM_Env_Bush_Small_01", "SM_Env_Bush_Small_02", "SM_Env_Bush_Small_03" } },
    { "Fern", new[] { "SM_Plant_Fern_01", "SM_Plant_Fern_02" } },
    { "Small fern", new[] { "SM_Plant_Fern_03" } },
    { "Daisies", new[] { "SM_Env_Bush_Flowers_01", "SM_Env_Bush_Flowers_02" } },
    { "Flower", new[] { "SM_Env_Flower_01", "SM_Env_Flower_02" } },
    { "Flowers", new[] { "SM_Env_Bush_Flowers_03", "SM_Env_Bush_Flowers_04" } },
    { "Thistle", new[] { "SM_Env_Bush_Small_04" } },
    { "Roots", new[] { "SM_Env_Mangrove_Roots_01" } },
    { "Bullrushes", new[] { "SM_Env_Reeds_01", "SM_Env_Reeds_02" } },
    { "Wheat", new[] { "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_01.prefab", "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_02.prefab" } },
    { "Cabbage", new[] { "SM_Prop_Cabbage_01", "SM_Prop_Cabbage_02" } },
    { "Potato", new[] { "SM_Env_Bush_Small_05" } },
    { "Onion", new[] { "SM_Env_Bush_Small_05" } },
    { "Rock", new[] { "SM_Env_Rock_01", "SM_Env_Rock_02", "SM_Env_Rock_03" } },
    { "Rocks", new[] { "SM_Env_Rock_Chunk_01", "SM_Env_Rock_Chunk_02" } },
    { "Boulder", new[] { "SM_Env_Rock_04" } },
    { "Stones", new[] { "SM_Generic_Small_Rocks_01", "SM_Generic_Small_Rocks_02" } },
    { "Rockslide", new[] { "SM_Env_Rock_Chunk_03" } },
    { "Crate", new[] { "SM_Prop_Crate_01", "SM_Prop_Crate_02" } },
    { "Crates", new[] { "SM_Prop_Crate_Stack_01" } },
    { "Barrel", new[] { "SM_Prop_Barrel_01", "SM_Prop_Barrel_02" } },
    { "Table", new[] { "SM_Prop_Table_01" } },
    { "Shelves", new[] { "SM_Prop_Shelf_01", "SM_Prop_Shelf_02" } },
    { "Sack", new[] { "SM_Prop_Sack_01", "SM_Prop_Sack_02" } },
    { "Sacks", new[] { "SM_Prop_Sack_03", "SM_Prop_Sack_04" } },
    { "Bench", new[] { "SM_Prop_Bench_01" } },
    { "Statue", new[] { "SM_Prop_Statue_Knight_01" } },
    { "Hay bales", new[] { "SM_Prop_Hay_Bale_01" } },
    { "Haystack", new[] { "SM_Prop_Hay_Pile_01" } },
    { "Ladder", new[] { "SM_Prop_Ladder_01" } },
    { "Well", new[] { "SM_Prop_Well_01" } },
    { "Gravestone", new[] { "SM_Prop_Gravestone_01", "SM_Prop_Gravestone_02", "SM_Prop_Gravestone_03" } },
    { "Grave", new[] { "SM_Prop_Gravestone_04" } },
    { "Cart", new[] { "SM_Prop_Cart_01" } },
    { "Anvil", new[] { "SM_Prop_Anvil_01" } },
    { "Signpost", new[] { "SM_Prop_Direction_Sign_01" } },
    { "Chest", new[] { "SM_Prop_Chest_01" } },
    { "Cactus", new[] { "SM_Env_Cactus_01", "SM_Env_Cactus_02" } },
};
var counts = new System.Collections.Generic.Dictionary<string, int>();
var missing = new System.Collections.Generic.Dictionary<string, int>();
foreach (var l in locs)
{
    var type = (int)l["type"];
    if (type != 10 && type != 11) continue;
    var name = (string)l["name"];
    if (name == null || name == "null") continue;
    var key = name;
    if (key.EndsWith(" rocks") || key.EndsWith(" rock")) key = "Rocks";
    if (!things.TryGetValue(key, out var choices)) { missing[name] = missing.TryGetValue(name, out var mc) ? mc + 1 : 1; continue; }
    var pf = prefab(choices[rng.Next(choices.Length)]);
    if (!pf) { missing[name + " (no prefab)"] = 1; continue; }
    int x = (int)l["x"], y = (int)l["y"], rot = (int)l["rot"], level = (int)l["level"];
    level = levelAt(x, y, level);
    if (level > 0) continue;   // upstairs: not in this test
    int sxT = (int)l["sx"], syT = (int)l["sy"];
    if (rot % 2 == 1) { var t = sxT; sxT = syT; syT = t; }
    // on the middle of the tiles it covers
    var cxm = x + sxT * 0.5f; var cym = y + syT * 0.5f;
    var pos = new UnityEngine.Vector3(cxm * Tile, ground(cxm, cym) + level * Floor, cym * Tile);
    var o = place(pf, group("Objects " + (name.Contains("ree") ? "trees" : "other")));
    var natural = name.Contains("ree") || name.Contains("ush") || name.Contains("ock") || name.Contains("lant") || name.Contains("ern") || name == "Daisies" || name.Contains("lower") || name == "Wheat" || name == "Thistle" || name.Contains("Stone") || name == "Boulder";
    var yaw = natural ? (float)rng.NextDouble() * 360f : rot * 90f + 180f;
    o.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.Euler(0, yaw, 0));
    if (natural) o.transform.localScale *= 0.85f + (float)rng.NextDouble() * 0.3f;
    counts[key] = counts.TryGetValue(key, out var cc) ? cc + 1 : 1;
}
var placedTotal = 0; foreach (var kv in counts) placedTotal += kv.Value;
sb.Append("objects " + placedTotal + "; not placed: ");
var miss = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(missing);
miss.Sort((a, b) => b.Value.CompareTo(a.Value));
for (var i = 0; i < miss.Count && i < 30; i++) sb.Append(miss[i].Key + " " + miss[i].Value + ", ");

// ---- light and a camera over the castle ------------------------------------------------------------------------
stage = "camera";
var cam = UnityEngine.Camera.main;
if (cam)
{
    cam.farClipPlane = 2000f;
    cam.transform.position = new UnityEngine.Vector3(80 * Tile, 90f, 40 * Tile);
    cam.transform.LookAt(new UnityEngine.Vector3(80 * Tile, 5f, 80 * Tile));
}

// the paint last, once every other asset is made: painted earlier, the asset work after it lost the paint and every
// texel came out as the first layer
stage = "paint";
td.SetAlphamaps(0, 0, alpha);
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
{
    var check = td.GetAlphamaps(0, 0, aRes, aRes);
    var used = new int[8];
    for (var j = 0; j < aRes; j += 8) for (var i = 0; i < aRes; i += 8) { var best = 0; for (var l = 1; l < 8; l++) if (check[j, i, l] > check[j, i, best]) best = l; used[best]++; }
    sb.Append("paint " + string.Join("/", used) + "; ");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log("[lumbridge-test] " + sb);
return sb.ToString();

}
catch (System.Exception ex)
{
    UnityEngine.Debug.Log("[lumbridge-test] failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
