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
// One OSRS tile is 2.5 m (a Synty house wall); a storey is 3.75 m. Rerun to rebuild; it opens the test scene first.
// Then run tools/ravebridge/lumbridge-paint.cs to paint the ground.
var sb = new System.Text.StringBuilder();
var stage = "start";
try
{

const string Json = @"G:\GitHub\rs-map-viewer\out\lumbridge.json";
const string ScenePath = "Assets/Scenes/LumbridgeTest.unity";
const string Folder = "Assets/Scenes/LumbridgeTest";
const float Tile = 2.5f, Unit = Tile / 128f;
// a storey: OSRS's is almost two tiles; 3.75 m is a Synty house's stone base (1.25) and wall (2.5) together
const float Floor = 3.75f;
const float Base = -6f, Span = 30f;          // the terrain's foot and height range
const float WaterDepth = 1.8f;
const float HillScale = 1.0f;            // OSRS's own proportions (heights in the tiles' units): at 0.6 the river
                                         // valley and the mine's valley north of Al Kharid were half as deep as the game's

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
// an asset saved over the one already at its path (same type), so materials and meshes keep their identity between
// builds: deleted and made anew, the editor kept drawing the old ones (with their materials gone, in magenta)
System.Func<UnityEngine.Object, string, UnityEngine.Object> keep = (o, path) =>
{
    var existing = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
    if (existing && existing.GetType() == o.GetType())
    {
        UnityEditor.EditorUtility.CopySerialized(o, existing);
        UnityEditor.EditorUtility.SetDirty(existing);
        return existing;
    }
    if (existing) UnityEditor.AssetDatabase.DeleteAsset(path);
    UnityEditor.AssetDatabase.CreateAsset(o, path);
    return o;
};
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
// each bridge tile's deck heights (m) at its corners, as the map arches it: (x,y) (x,y+1) (x+1,y+1) (x+1,y)
var deckAt = new System.Collections.Generic.Dictionary<long, float[]>();
foreach (var b in map["bridges"].ToObject<int[][]>())
{
    // the map's edge column has bridge flags with no bridge (the region beyond is not loaded)
    if (b[0] <= 0 || b[1] <= 0 || b[0] >= N - 1 || b[1] >= N - 1) continue;
    bridgeSet.Add(((long)b[0] << 20) | (uint)b[1]);
    if (b.Length >= 6) deckAt[((long)b[0] << 20) | (uint)b[1]] = new[] { b[2] * Unit * HillScale, b[3] * Unit * HillScale, b[4] * Unit * HillScale, b[5] * Unit * HillScale };
}
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
// the sea's level: the map's sea tiles (south of the swamp) as the water surface draws them
float seaLevel;
{
    var levels = new System.Collections.Generic.List<float>();
    for (var x = 0; x < N; x++) for (var y = 0; y < N; y++)
        if (overlayAt(x, y) == 442) levels.Add((corner(x, y) + corner(x + 1, y) + corner(x, y + 1) + corner(x + 1, y + 1)) * 0.25f - 0.45f);
    levels.Sort();
    seaLevel = levels.Count > 0 ? levels[levels.Count / 2] : -0.45f;
}
// the island's edges: v0.9.6 has only this part of the world, so where the map stops the land goes down into the sea
// along a ragged coast, not a cut (Edge tiles in, wavering by the noise)
const float Edge = 14f, EdgeWaver = 11f;
// buildings keep their ground: the coast bulges round anything with full walls or a roof
var built = new bool[N, N];
foreach (var l in locs)
{
    var t = (int)l["type"];
    var lk = l["look"];
    var full = (t == 0 || t == 2 || t == 9) && lk != null && lk["rgb"].Type != Newtonsoft.Json.Linq.JTokenType.Null && (int)lk["height"] >= 200;
    if (!full && !(t >= 12 && t <= 21)) continue;
    built[(int)l["x"], (int)l["y"]] = true;
}
// how far each tile is from the nearest building (two passes of a chamfer distance), so the coast rounds out round
// them instead of standing in a square cliff
var nearB = new float[N, N];
for (var x = 0; x < N; x++) for (var y = 0; y < N; y++) nearB[x, y] = built[x, y] ? 0f : 999f;
for (var pass = 0; pass < 2; pass++)
{
    for (var x = 0; x < N; x++) for (var y = 0; y < N; y++)
    {
        var v = nearB[x, y];
        if (x > 0) v = UnityEngine.Mathf.Min(v, nearB[x - 1, y] + 1f);
        if (y > 0) v = UnityEngine.Mathf.Min(v, nearB[x, y - 1] + 1f);
        if (x > 0 && y > 0) v = UnityEngine.Mathf.Min(v, nearB[x - 1, y - 1] + 1.414f);
        if (x > 0 && y < N - 1) v = UnityEngine.Mathf.Min(v, nearB[x - 1, y + 1] + 1.414f);
        nearB[x, y] = v;
    }
    for (var x = N - 1; x >= 0; x--) for (var y = N - 1; y >= 0; y--)
    {
        var v = nearB[x, y];
        if (x < N - 1) v = UnityEngine.Mathf.Min(v, nearB[x + 1, y] + 1f);
        if (y < N - 1) v = UnityEngine.Mathf.Min(v, nearB[x, y + 1] + 1f);
        if (x < N - 1 && y < N - 1) v = UnityEngine.Mathf.Min(v, nearB[x + 1, y + 1] + 1.414f);
        if (x < N - 1 && y > 0) v = UnityEngine.Mathf.Min(v, nearB[x + 1, y - 1] + 1.414f);
        nearB[x, y] = v;
    }
}
System.Func<float, float, float> inland = (tx, ty) =>
{
    int bx = UnityEngine.Mathf.Clamp((int)tx, 0, N - 1), by = UnityEngine.Mathf.Clamp((int)ty, 0, N - 1);
    // land for sure within three tiles of a building, easing off to the coast's own line by nine
    var keepLand = UnityEngine.Mathf.SmoothStep(1f, 0f, UnityEngine.Mathf.Clamp01((nearB[bx, by] - 3f) / 6f));
    var d = UnityEngine.Mathf.Min(UnityEngine.Mathf.Min(tx, ty), UnityEngine.Mathf.Min(N - tx, N - ty));
    var n = (UnityEngine.Mathf.PerlinNoise(tx * 0.028f + 11.3f, ty * 0.028f + 7.1f) - 0.5f) * 2f * EdgeWaver
          + (UnityEngine.Mathf.PerlinNoise(tx * 0.17f + 3.7f, ty * 0.17f + 1.9f) - 0.5f) * 2f;
    return UnityEngine.Mathf.Max(keepLand, UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.Clamp01((d + n - 3f) / Edge)));
};
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
    // the shore half way down: the water's edge falls on the bank's slope, not along the tiles' squares
    if (wet > 0) y -= wet == all ? WaterDepth : WaterDepth * 0.5f;
    // down to the sea floor towards the map's edges
    // (as deep as the terrain goes, so its square edge does not show through the water)
    y = UnityEngine.Mathf.Lerp(Base + 0.3f, y, inland(tx, ty));
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
        // half way to the average: the tiles' corners soften, valley walls stay steep
        if (!water(UnityEngine.Mathf.FloorToInt(stx + 0.4f), UnityEngine.Mathf.FloorToInt(sty + 0.4f)) && !water(UnityEngine.Mathf.FloorToInt(stx - 0.4f), UnityEngine.Mathf.FloorToInt(sty - 0.4f))) smooth[j, i] = (hs[j, i] + sum / 9f) * 0.5f;
    }
    hs = smooth;
}
// the roads ease the ground under them: across a road the ground is levelled to the road's own line (its middle's
// height, smoothed along it), fading out a couple of metres past its edges, so a road lies on a gentle bed instead
// of tilting with every bump; never more than half a metre
{
    var roadsFile = @"G:\GitHub\rs-map-viewer\out\roads.json";
    if (System.IO.File.Exists(roadsFile))
    {
        var roadsJ = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(roadsFile));
        float H(float wx, float wz)
        {
            float fi = wx / td.size.x * (res - 1), fj = wz / td.size.z * (res - 1);
            int i0 = UnityEngine.Mathf.Clamp((int)fi, 0, res - 2), j0 = UnityEngine.Mathf.Clamp((int)fj, 0, res - 2);
            float u = fi - i0, v = fj - j0;
            return UnityEngine.Mathf.Lerp(UnityEngine.Mathf.Lerp(hs[j0, i0], hs[j0, i0 + 1], u), UnityEngine.Mathf.Lerp(hs[j0 + 1, i0], hs[j0 + 1, i0 + 1], u), v);
        }
        var target = new float[res, res]; var weight = new float[res, res];
        const float Fade = 2.5f, MaxChange = 0.5f;
        foreach (var r in (Newtonsoft.Json.Linq.JArray)roadsJ["roads"])
        {
            var pts = r["points"].ToObject<float[][]>(); var ws = r["widths"].ToObject<float[]>();
            // samples every half metre along the line, with the middle's height smoothed over 4 m either way
            var samples = new System.Collections.Generic.List<(float X, float Z, float Half)>();
            for (var k = 0; k + 1 < pts.Length; k++)
            {
                var len = UnityEngine.Mathf.Sqrt((pts[k + 1][0] - pts[k][0]) * (pts[k + 1][0] - pts[k][0]) + (pts[k + 1][1] - pts[k][1]) * (pts[k + 1][1] - pts[k][1]));
                var n = UnityEngine.Mathf.Max(1, (int)(len / 0.5f));
                for (var q = 0; q < n; q++) { var t = q / (float)n; samples.Add((UnityEngine.Mathf.Lerp(pts[k][0], pts[k + 1][0], t), UnityEngine.Mathf.Lerp(pts[k][1], pts[k + 1][1], t), UnityEngine.Mathf.Lerp(ws[k], ws[k + 1], t) * 0.5f)); }
            }
            var mids = new float[samples.Count];
            for (var q = 0; q < samples.Count; q++) mids[q] = H(samples[q].X, samples[q].Z);
            for (var q = 0; q < samples.Count; q++)
            {
                float sum = 0; int c = 0;
                for (var o = -8; o <= 8; o++) { var qq = q + o; if (qq < 0 || qq >= samples.Count) continue; sum += mids[qq]; c++; }
                var level = sum / c;
                var (sx, sz, half) = samples[q];
                var reach = half + Fade;
                int i0 = UnityEngine.Mathf.Max(0, (int)((sx - reach) / td.size.x * (res - 1))), i1 = UnityEngine.Mathf.Min(res - 1, (int)((sx + reach) / td.size.x * (res - 1)) + 1);
                int j0 = UnityEngine.Mathf.Max(0, (int)((sz - reach) / td.size.z * (res - 1))), j1 = UnityEngine.Mathf.Min(res - 1, (int)((sz + reach) / td.size.z * (res - 1)) + 1);
                for (var jj = j0; jj <= j1; jj++)
                for (var ii = i0; ii <= i1; ii++)
                {
                    var wx = ii / (float)(res - 1) * td.size.x; var wz = jj / (float)(res - 1) * td.size.z;
                    var d = UnityEngine.Mathf.Sqrt((wx - sx) * (wx - sx) + (wz - sz) * (wz - sz));
                    var w = d <= half ? 1f : UnityEngine.Mathf.SmoothStep(1f, 0f, (d - half) / Fade);
                    if (w > weight[jj, ii]) { weight[jj, ii] = w; target[jj, ii] = level; }
                }
            }
        }
        var maxChange = MaxChange / Span;
        for (var jj = 0; jj < res; jj++)
        for (var ii = 0; ii < res; ii++)
        {
            if (weight[jj, ii] <= 0f) continue;
            var to = UnityEngine.Mathf.Clamp(target[jj, ii], hs[jj, ii] - maxChange, hs[jj, ii] + maxChange);
            hs[jj, ii] = UnityEngine.Mathf.Lerp(hs[jj, ii], to, weight[jj, ii]);
        }
    }
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
// the roads as centre lines for the road tool (tools/ravebridge/osrs/roads.py): under them the ground keeps its own paint,
// so the tiles' blocky outline does not show round the road
const string RoadsJson = @"G:\GitHub\rs-map-viewer\out\roads.json";
Newtonsoft.Json.Linq.JObject roadsMap = System.IO.File.Exists(RoadsJson) ? Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(RoadsJson)) : null;
var roadTiles = new System.Collections.Generic.HashSet<long>();
if (roadsMap != null) foreach (var t in roadsMap["covered"].ToObject<int[][]>()) roadTiles.Add(((long)t[0] << 20) | (uint)t[1]);
System.Func<int, int, int> paintOf = (x, y) =>
{
    var o = overlayAt(x, y);
    if (o > 0 && roadTiles.Contains(((long)x << 20) | (uint)y)) o = 0;
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
        case 63: case 96: case 67: case 64: case 65: return 4;                 // bare earth: dark brown in the game, the mud
                                                                               // layer (the footpath layer drew light tan)
        case 54: return 4;                                                     // the swamp
        case 50: case 53: case 56: return 3;                                   // dry grass
        default: return 2;                                                     // grass
    }
};
var aRes = 512;
td.alphamapResolution = aRes;
var alpha = new float[aRes, aRes, 8];
// each tile's grounds; bare earth (the game's dark brown patches) is earth by buildings, and only a trace in the open
// fields
var tileW = new float[N, N, 8];
for (var x = 0; x < N; x++)
for (var y = 0; y < N; y++)
{
    var layer = paintOf(x, y);
    if (layer == 4 && overlayAt(x, y) == 0 && underlayAt(x, y) != 54)
    {
        var share = UnityEngine.Mathf.Lerp(1f, 0.2f, UnityEngine.Mathf.InverseLerp(3f, 9f, nearB[x, y]));
        tileW[x, y, 4] += share;
        tileW[x, y, 2] += 1f - share;
    }
    else tileW[x, y, layer] = 1f;
}
// each texel blends the tiles round it, weighted by distance, then the blend is sharpened again: the edges between
// grounds stay crisp but run in curves instead of stepping along the tiles (a 3 x 3 sample wavered by a metre left
// the desert's edge in 2.5 m stairs); a slow and a quick waver make the curves wander
const float Sigma = 0.75f, Sharpen = 3f;
var wsum = new float[8];
for (var j = 0; j < aRes; j++)
for (var i = 0; i < aRes; i++)
{
    var tx = (i + 0.5f) / aRes * N; var ty = (j + 0.5f) / aRes * N;
    var wx = tx + (UnityEngine.Mathf.PerlinNoise(tx * 0.12f + 5.1f, ty * 0.12f + 2.3f) - 0.5f) * 1.6f + (UnityEngine.Mathf.PerlinNoise(tx * 0.5f + 1.3f, ty * 0.5f + 8.2f) - 0.5f) * 0.6f;
    var wy = ty + (UnityEngine.Mathf.PerlinNoise(tx * 0.12f + 9.7f, ty * 0.12f + 4.4f) - 0.5f) * 1.6f + (UnityEngine.Mathf.PerlinNoise(tx * 0.5f + 6.6f, ty * 0.5f + 3.1f) - 0.5f) * 0.6f;
    System.Array.Clear(wsum, 0, 8);
    int cx = UnityEngine.Mathf.FloorToInt(wx), cy = UnityEngine.Mathf.FloorToInt(wy);
    for (var x = cx - 2; x <= cx + 2; x++)
    for (var y = cy - 2; y <= cy + 2; y++)
    {
        var dx = x + 0.5f - wx; var dy = y + 0.5f - wy;
        var g = UnityEngine.Mathf.Exp(-(dx * dx + dy * dy) / (2f * Sigma * Sigma));
        int qx = System.Math.Clamp(x, 0, N - 1), qy = System.Math.Clamp(y, 0, N - 1);
        for (var l = 0; l < 8; l++) wsum[l] += tileW[qx, qy, l] * g;
    }
    var total = 0f;
    for (var l = 0; l < 8; l++) { wsum[l] = UnityEngine.Mathf.Pow(wsum[l], Sharpen); total += wsum[l]; }
    for (var l = 0; l < 8; l++) alpha[j, i, l] = total > 0f ? wsum[l] / total : (l == 2 ? 1f : 0f);
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
    var mat = new UnityEngine.Material(rbTerrainMat);
    var textures = td.alphamapTextures;
    mat.SetTexture("_RoadMask", textures[0]);
    if (textures.Length > 1) mat.SetTexture("_SnowMask", textures[1]);
    // its tilings are over the terrain: Ravebridge's are for 2560 m
    var k = N * Tile / 2560f;
    foreach (var p in new[] { "_LayerUVScale", "_MainUVScale", "_SecondaryUVScale" }) { var v = mat.GetVector(p); mat.SetVector(p, new UnityEngine.Vector4(v.x * k, v.y * k, 0, 0)); }
    // sand along the new coast, just above the sea, wet where the sea reaches
    mat.SetFloat("_SecondaryHeight", seaLevel + 0.9f);
    // the first layer is the desert here (Al Kharid's sand), so it draws the shader's own sand, not meadow dirt
    if (mat.HasProperty("_Dirt") && mat.HasProperty("_Main") && mat.GetTexture("_Main")) mat.SetTexture("_Dirt", mat.GetTexture("_Main"));
    mat.SetFloat("_SeaLevel", seaLevel);
    terrain.materialTemplate = (UnityEngine.Material)keep(mat, matPath);
}
terrain.heightmapPixelError = 3;
sb.Append("terrain " + res + "; ");

// ---- water -----------------------------------------------------------------------------------------------------
stage = "water";
{
    // one surface over a half tile grid near the water: each point at the level of the nearest water tile, reaching in
    // under the banks, so the edge is where the ground rises out of it and follows the bank's slope, not the tiles
    System.Func<int, int, float> levelOf = (x, y) => (corner(x, y) + corner(x + 1, y) + corner(x, y + 1) + corner(x + 1, y + 1)) * 0.25f - 0.45f;
    var gn = N * 2 + 1;
    var index = new int[gn, gn];
    var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
    for (var gy = 0; gy < gn; gy++)
    for (var gx = 0; gx < gn; gx++)
    {
        index[gx, gy] = -1;
        float tx = gx * 0.5f, ty = gy * 0.5f;
        var best = 9f; var level = 0f;
        for (var x = (int)tx - 2; x <= (int)tx + 2; x++)
        for (var y = (int)ty - 2; y <= (int)ty + 2; y++)
        {
            if (!water(x, y)) continue;
            var dx = UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Max(x - tx, tx - (x + 1)));
            var dy = UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Max(y - ty, ty - (y + 1)));
            var d = dx * dx + dy * dy;
            if (d < best) { best = d; level = levelOf(x, y); }
        }
        if (best > 1.6f) continue;   // more than a tile and a bit from any water
        index[gx, gy] = verts.Count;
        verts.Add(new UnityEngine.Vector3(tx * Tile, level, ty * Tile));
    }
    var tris = new System.Collections.Generic.List<int>();
    for (var gy = 0; gy < gn - 1; gy++)
    for (var gx = 0; gx < gn - 1; gx++)
    {
        int a0 = index[gx, gy], a1 = index[gx, gy + 1], a2 = index[gx + 1, gy + 1], a3 = index[gx + 1, gy];
        if (a0 >= 0 && a1 >= 0 && a2 >= 0) tris.AddRange(new[] { a0, a1, a2 });
        if (a0 >= 0 && a2 >= 0 && a3 >= 0) tris.AddRange(new[] { a0, a2, a3 });
    }
    var mesh = new UnityEngine.Mesh { name = "Lumbridge Water", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
    mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
    var up = new UnityEngine.Vector3[verts.Count]; for (var i = 0; i < up.Length; i++) up[i] = UnityEngine.Vector3.up;
    mesh.normals = up; mesh.RecalculateBounds();
    var meshPath = Folder + "/Lumbridge Water.asset";
    mesh = (UnityEngine.Mesh)keep(mesh, meshPath);
    var go = new UnityEngine.GameObject("Water");
    go.transform.SetParent(root, false);
    go.AddComponent<RiverWater>().Mesh = mesh;
    // the sea round the island in this test scene (in MainWorld the ocean is there already): a wide sheet at the sea's
    // level, under the land
    var sea = new UnityEngine.Mesh { name = "Lumbridge Sea" };
    const float far = 600f;
    float s0 = -far, s1 = N * Tile + far, sl = seaLevel - 0.02f;
    sea.vertices = new[] { new UnityEngine.Vector3(s0, sl, s0), new UnityEngine.Vector3(s0, sl, s1), new UnityEngine.Vector3(s1, sl, s1), new UnityEngine.Vector3(s1, sl, s0) };
    sea.triangles = new[] { 0, 1, 2, 0, 2, 3 };
    sea.normals = new[] { UnityEngine.Vector3.up, UnityEngine.Vector3.up, UnityEngine.Vector3.up, UnityEngine.Vector3.up };
    sea.RecalculateBounds();
    sea = (UnityEngine.Mesh)keep(sea, Folder + "/Lumbridge Sea.asset");
    var seaGo = new UnityEngine.GameObject("Sea (test only)");
    seaGo.transform.SetParent(root, false);
    seaGo.AddComponent<RiverWater>().Mesh = sea;
    sb.Append("water points " + verts.Count + "; ");
}

// from here on the ground is the terrain as built (smoothed, the banks dug): walls, floors, roofs and props on the
// map's raw heights floated over hollows the smoothing made
corner = (cx, cy) => terrain.SampleHeight(new UnityEngine.Vector3(cx * Tile, 0f, cy * Tile)) + Base;

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

// every roofed tile once the roofs are worked out (their pieces and the gaps between their sides): a low wall on an
// upper level under one is the attic's, not a battlement
var roofFootprint = new System.Collections.Generic.HashSet<long>();

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
    floorMat = (UnityEngine.Material)keep(floorMat, fmPath);
    var floorsJ = map["floors"].ToObject<int[][][]>();
    for (var level = 1; level < floorsJ.Length; level++)
    {
        var set = new System.Collections.Generic.HashSet<long>();
        // tiles with no colour are not drawn in the game (walkable tops of nothing; over Al Kharid's palace they made
        // a grey slab over its white roof terrace)
        // (nor upper floors over water that are not bridges: the map has 153 by the mill, a storey up they floated as
        // squares over the river)
        foreach (var f in floorsJ[level]) if ((f.Length < 3 || f[2] >= 0) && !(water(f[0], f[1]) && !isBridge(f[0], f[1]))) set.Add(((long)f[0] << 20) | (uint)f[1]);
        // patches of fewer than four tiles are ledges sticking out of walls (the gatehouse had two): left out
        {
            var seenF = new System.Collections.Generic.HashSet<long>();
            var drop = new System.Collections.Generic.List<long>();
            foreach (var k0 in set)
            {
                if (!seenF.Add(k0)) continue;
                var part = new System.Collections.Generic.List<long> { k0 };
                for (var i = 0; i < part.Count; i++)
                {
                    int px = (int)(part[i] >> 20), py = (int)(part[i] & 0xfffff);
                    foreach (var n in new[] { ((long)(px + 1) << 20) | (uint)py, ((long)(px - 1) << 20) | (uint)py, ((long)px << 20) | (uint)(py + 1), ((long)px << 20) | (uint)(py - 1) })
                        if (set.Contains(n) && seenF.Add(n)) part.Add(n);
                }
                if (part.Count < 4) drop.AddRange(part);
            }
            foreach (var k0 in drop) set.Remove(k0);
        }
        if (set.Count == 0) continue;
        // diagonal walls on this level or the one below, by tile
        var diagonals = new System.Collections.Generic.Dictionary<long, int>();
        foreach (var l in locs)
        {
            if ((int)l["type"] != 9) continue;
            var ll = (int)l["level"];
            if (ll != level && ll != level - 1) continue;
            var lk = l["look"];
            if (lk == null || lk["rgb"].Type == Newtonsoft.Json.Linq.JTokenType.Null) continue;
            diagonals[((long)(int)l["x"] << 20) | (uint)(int)l["y"]] = (int)l["rot"];
        }
        var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
        // one triangle list per floor colour (the map's: grey stone, planks, Al Kharid's white roof terraces)
        var trisByColour = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();
        var tris = new System.Collections.Generic.List<int>();
        System.Action<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3> quad = (a, b, c, d) =>
        {
            var v = verts.Count; verts.AddRange(new[] { a, b, c, d }); tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        };
        foreach (var f in floorsJ[level])
        {
            int x = f[0], y = f[1];
            if (!set.Contains(((long)x << 20) | (uint)y)) continue;
            // its colour, a little quantised so few materials are made
            var rgbF = f.Length > 2 && f[2] >= 0 ? f[2] : 0x777570;
            var ck = (((rgbF >> 16) & 255) / 16) * 256 + (((rgbF >> 8) & 255) / 16) * 16 + ((rgbF & 255) / 16);
            if (!trisByColour.TryGetValue(ck, out tris)) trisByColour[ck] = tris = new System.Collections.Generic.List<int>();
            if (deckAt.TryGetValue(((long)x << 20) | (uint)y, out var dk))
            {
                // a bridge's deck, arched as the map has it, a slab under it
                float bx0 = x * Tile, bx1 = (x + 1) * Tile, bz0 = y * Tile, bz1 = (y + 1) * Tile;
                var t0 = new UnityEngine.Vector3(bx0, dk[0], bz0); var t1 = new UnityEngine.Vector3(bx0, dk[1], bz1);
                var t2 = new UnityEngine.Vector3(bx1, dk[2], bz1); var t3 = new UnityEngine.Vector3(bx1, dk[3], bz0);
                var dn = new UnityEngine.Vector3(0, -0.6f, 0);
                quad(t0, t1, t2, t3);
                quad(t0 + dn, t3 + dn, t2 + dn, t1 + dn);
                if (!deckAt.ContainsKey(((long)(x - 1) << 20) | (uint)y)) quad(t0 + dn, t1 + dn, t1, t0);
                if (!deckAt.ContainsKey(((long)(x + 1) << 20) | (uint)y)) quad(t2 + dn, t3 + dn, t3, t2);
                if (!deckAt.ContainsKey(((long)x << 20) | (uint)(y - 1))) quad(t3 + dn, t0 + dn, t0, t3);
                if (!deckAt.ContainsKey(((long)x << 20) | (uint)(y + 1))) quad(t1 + dn, t2 + dn, t2, t1);
                continue;
            }
            var top = (corner(x, y) + corner(x + 1, y) + corner(x, y + 1) + corner(x + 1, y + 1)) * 0.25f + levelAt(x, y, level) * Floor + (isBridge(x, y) ? Deck : 0f);
            float x0 = x * Tile, x1 = (x + 1) * Tile, z0 = y * Tile, z1 = (y + 1) * Tile, lo = top - Thick;
            // a tile a diagonal wall crosses (the gatehouse towers' cut corners): only the half inside, the side its
            // floor neighbours are on; the whole tile stuck out past the wall as a shelf
            if (diagonals.TryGetValue(((long)x << 20) | (uint)y, out var drot))
            {
                System.Func<int, int, int> has = (ax, ay) => set.Contains(((long)(x + ax) << 20) | (uint)(y + ay)) ? 1 : 0;
                // corners: 0 (x0,z0) 1 (x0,z1) 2 (x1,z1) 3 (x1,z0)
                var cp = new[] { new UnityEngine.Vector3(x0, top, z0), new UnityEngine.Vector3(x0, top, z1), new UnityEngine.Vector3(x1, top, z1), new UnityEngine.Vector3(x1, top, z0) };
                int[] half;
                if (drot % 2 == 0) half = has(-1, 0) + has(0, 1) >= has(1, 0) + has(0, -1) ? new[] { 0, 1, 2 } : new[] { 2, 3, 0 };
                else half = has(-1, 0) + has(0, -1) >= has(1, 0) + has(0, 1) ? new[] { 3, 0, 1 } : new[] { 1, 2, 3 };
                var v0 = verts.Count;
                verts.AddRange(new[] { cp[half[0]], cp[half[1]], cp[half[2]] });
                tris.AddRange(new[] { v0, v0 + 1, v0 + 2 });
                var down = new UnityEngine.Vector3(0, -Thick, 0);
                v0 = verts.Count;
                verts.AddRange(new[] { cp[half[0]] + down, cp[half[2]] + down, cp[half[1]] + down });
                tris.AddRange(new[] { v0, v0 + 1, v0 + 2 });
                // the cut edge's face, under the wall
                quad(cp[half[2]] + down, cp[half[0]] + down, cp[half[0]], cp[half[2]]);
                continue;
            }
            quad(new UnityEngine.Vector3(x0, top, z0), new UnityEngine.Vector3(x0, top, z1), new UnityEngine.Vector3(x1, top, z1), new UnityEngine.Vector3(x1, top, z0));
            quad(new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x0, lo, z1));
            if (!set.Contains(((long)(x - 1) << 20) | (uint)y)) quad(new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x0, lo, z1), new UnityEngine.Vector3(x0, top, z1), new UnityEngine.Vector3(x0, top, z0));
            if (!set.Contains(((long)(x + 1) << 20) | (uint)y)) quad(new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x1, top, z0), new UnityEngine.Vector3(x1, top, z1));
            if (!set.Contains(((long)x << 20) | (uint)(y - 1))) quad(new UnityEngine.Vector3(x1, lo, z0), new UnityEngine.Vector3(x0, lo, z0), new UnityEngine.Vector3(x0, top, z0), new UnityEngine.Vector3(x1, top, z0));
            if (!set.Contains(((long)x << 20) | (uint)(y + 1))) quad(new UnityEngine.Vector3(x0, lo, z1), new UnityEngine.Vector3(x1, lo, z1), new UnityEngine.Vector3(x1, top, z1), new UnityEngine.Vector3(x0, top, z1));
        }
        var mesh = new UnityEngine.Mesh { name = "Floors " + level, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.subMeshCount = trisByColour.Count;
        var mats = new UnityEngine.Material[trisByColour.Count];
        var si = 0;
        foreach (var kv in trisByColour)
        {
            mesh.SetTriangles(kv.Value, si);
            var colour = new UnityEngine.Color((kv.Key / 256) / 15f, ((kv.Key / 16) % 16) / 15f, (kv.Key % 16) / 15f);
            var fm = new UnityEngine.Material(floorMat) { name = "Lumbridge Floor " + kv.Key };
            fm.SetColor("_BaseColor", colour * 0.95f);
            mats[si++] = (UnityEngine.Material)keep(fm, Folder + "/Lumbridge Floor " + kv.Key + ".mat");
        }
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new UnityEngine.GameObject("Floors " + level);
        go.transform.SetParent(group("Floors"), false);
        go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        go.AddComponent<UnityEngine.MeshRenderer>().sharedMaterials = mats;
        go.AddComponent<UnityEngine.MeshCollider>().sharedMesh = mesh;
    }
    sb.Append("floors; ");
}

// ---- roofs -------------------------------------------------------------------------------------------------------
stage = "roofs";
{
    // The roofs as the map has them, as meshes of our own: the export samples each roof piece's top surface every
    // quarter tile over its whole reach (slopes and overhangs run past their own tiles, and some roofs are plain
    // unnamed objects on upper levels), and here the highest surface at each point makes one height field over the
    // map, a floor up for each level. Each cell of it is two triangles in its roof's colour; where a roof's edge stands
    // well over the wall top under it (a gable end), a wall closes it down.
    const float VUnit = Floor / 240f;   // the game's units up: a storey is 240
    const int Q = 4;                    // samples a tile
    var lit = UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
    var gn = N * Q + 1;
    var hgt = new float[gn, gn];
    var bas = new float[gn, gn];
    var col = new int[gn, gn];
    for (var i = 0; i < gn; i++) for (var j = 0; j < gn; j++) hgt[i, j] = float.NaN;
    var palette = new System.Collections.Generic.List<UnityEngine.Color>();
    var paletteIndex = new System.Collections.Generic.Dictionary<int, int>();
    var contributors = 0;
    foreach (var l in locs)
    {
        var sf = l["surface"];
        if (sf == null || sf.Type == Newtonsoft.Json.Linq.JTokenType.Null) continue;
        int x = (int)l["x"], y = (int)l["y"];
        var level = levelAt(x, y, (int)l["level"]);
        var basis = UnityEngine.Mathf.Max(UnityEngine.Mathf.Max(corner(x, y), corner(x + 1, y)), UnityEngine.Mathf.Max(corner(x, y + 1), corner(x + 1, y + 1))) + level * Floor;
        // its colour: Lumbridge's roofs are red tiles; the map's averaged warm colours come out brown, so they get the
        // tile red (lighter or darker as the map's); slate and thatch keep their own
        // its look: the surface's own texture (6 red tiles, 44 slate, 18 thatch: our textures) or its colour
        var texId = sf["texture"] != null ? (int)sf["texture"] : -1;
        var rgbS = sf["rgb"] != null ? (int)sf["rgb"] : -1;
        var colour = rgbS >= 0 ? new UnityEngine.Color(((rgbS >> 16) & 255) / 255f, ((rgbS >> 8) & 255) / 255f, (rgbS & 255) / 255f) : new UnityEngine.Color(0.55f, 0.25f, 0.18f);
        var cq = texId == 6 || texId == 44 || texId == 18 ? -1000 - texId
            : UnityEngine.Mathf.RoundToInt(colour.r * 15) * 256 + UnityEngine.Mathf.RoundToInt(colour.g * 15) * 16 + UnityEngine.Mathf.RoundToInt(colour.b * 15);
        if (!paletteIndex.TryGetValue(cq, out var ci)) { ci = palette.Count; palette.Add(colour); paletteIndex[cq] = ci; }
        int x0 = (int)sf["x0"], z0 = (int)sf["z0"], nx = (int)sf["nx"], nz = (int)sf["nz"];
        var hs2 = sf["h"].ToObject<int[]>();
        for (var j = 0; j < nz; j++)
        for (var i = 0; i < nx; i++)
        {
            var h = hs2[j * nx + i];
            if (h < 0) continue;
            // the sample's place on the grid: the tile's middle is (x * 128 + 64) units in
            var gx = (x * 128 + 64 + x0 + i * 32) / 32;
            var gz = (y * 128 + 64 + z0 + j * 32) / 32;
            if (gx < 0 || gz < 0 || gx >= gn || gz >= gn) continue;
            // the game's roofs start 40 units over the wall top (its wall decorations hide the strip): down onto the walls
            var abs = basis + (h - 40) * VUnit;
            if (float.IsNaN(hgt[gx, gz]) || abs > hgt[gx, gz]) { hgt[gx, gz] = abs; bas[gx, gz] = basis; col[gx, gz] = ci; }
        }
        contributors++;
    }
    var byColour = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<UnityEngine.Vector3>>();
    var gables = new System.Collections.Generic.List<UnityEngine.Vector3>();
    const float S = Tile / Q;
    System.Func<int, int, bool> cell = (i, j) => i >= 0 && j >= 0 && i < gn - 1 && j < gn - 1 && !float.IsNaN(hgt[i, j]) && !float.IsNaN(hgt[i + 1, j]) && !float.IsNaN(hgt[i, j + 1]) && !float.IsNaN(hgt[i + 1, j + 1]);
    System.Func<int, int, UnityEngine.Vector3> at = (i, j) => new UnityEngine.Vector3(i * S, hgt[i, j], j * S);
    var cells = 0;
    for (var i = 0; i < gn - 1; i++)
    for (var j = 0; j < gn - 1; j++)
    {
        if (!cell(i, j)) continue;
        cells++;
        roofFootprint.Add(((long)(i / Q) << 20) | (uint)(j / Q));
        // the cell's colour: its highest corner's roof
        int ti = i, tj = j;
        foreach (var (a2, b2) in new[] { (i + 1, j), (i, j + 1), (i + 1, j + 1) }) if (hgt[a2, b2] > hgt[ti, tj]) { ti = a2; tj = b2; }
        if (!byColour.TryGetValue(col[ti, tj], out var verts)) byColour[col[ti, tj]] = verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var p0 = at(i, j); var p1 = at(i, j + 1); var p2 = at(i + 1, j + 1); var p3 = at(i + 1, j);
        if (UnityEngine.Mathf.Abs(p0.y - p2.y) <= UnityEngine.Mathf.Abs(p1.y - p3.y)) verts.AddRange(new[] { p0, p1, p2, p0, p2, p3 });
        else verts.AddRange(new[] { p0, p1, p3, p1, p2, p3 });
        // edges to no roof, standing over the wall top: a gable wall down to it
        System.Action<int, int, int, int> edge = (ai, aj, bi, bj) =>
        {
            var A = at(ai, aj); var B = at(bi, bj);
            var foot = UnityEngine.Mathf.Min(bas[ai, aj], bas[bi, bj]);
            if (A.y - foot < 0.5f && B.y - foot < 0.5f) return;
            var A0 = new UnityEngine.Vector3(A.x, foot - 0.2f, A.z); var B0 = new UnityEngine.Vector3(B.x, foot - 0.2f, B.z);
            gables.AddRange(new[] { A0, A, B, A0, B, B0 });
        };
        if (!cell(i - 1, j)) edge(i, j, i, j + 1);
        if (!cell(i + 1, j)) edge(i + 1, j, i + 1, j + 1);
        if (!cell(i, j - 1)) edge(i, j, i + 1, j);
        if (!cell(i, j + 1)) edge(i, j + 1, i + 1, j + 1);
    }
    System.Action<string, System.Collections.Generic.List<UnityEngine.Vector3>, UnityEngine.Color, string> build = (name, verts, colour, texture) =>
    {
        var tris = new int[verts.Count]; for (var i = 0; i < tris.Length; i++) tris[i] = i;
        // UVs a tile a texture, the rows along the ridge: across a slope facing east or west they turn, and the
        // distance up the slope is the true one (the projection shortens it)
        var uvs = new UnityEngine.Vector2[verts.Count];
        for (var i = 0; i + 2 < verts.Count; i += 3)
        {
            var n = UnityEngine.Vector3.Cross(verts[i + 1] - verts[i], verts[i + 2] - verts[i]).normalized;
            var ny = UnityEngine.Mathf.Max(0.3f, UnityEngine.Mathf.Abs(n.y));
            var eastWest = UnityEngine.Mathf.Abs(n.x) > UnityEngine.Mathf.Abs(n.z);
            for (var k = 0; k < 3; k++)
            {
                var v = verts[i + k];
                uvs[i + k] = eastWest ? new UnityEngine.Vector2(v.z / Tile, v.x / Tile / ny) : new UnityEngine.Vector2(v.x / Tile, v.z / Tile / ny);
            }
        }
        var mesh = new UnityEngine.Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new UnityEngine.GameObject(name);
        go.transform.SetParent(group("Roofs"), false);
        go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        var m = new UnityEngine.Material(lit) { name = name };
        var tex = texture != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(Folder + "/" + texture + ".png") : null;
        if (tex) { m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", UnityEngine.Color.white); }
        else m.SetColor("_BaseColor", colour);
        m.SetFloat("_Smoothness", 0.08f);
        m.SetFloat("_Cull", 0f);
        go.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = (UnityEngine.Material)keep(m, Folder + "/" + name + ".mat");
    };
    foreach (var kv in byColour)
    {
        var key = 0; foreach (var pk in paletteIndex) if (pk.Value == kv.Key) key = pk.Key;
        var texture = key == -1006 ? "Lumbridge Roof Tiles" : key == -1044 ? "Lumbridge Roof Slate" : key == -1018 ? "Lumbridge Roof Thatch" : null;
        build("Lumbridge Roof " + kv.Key, kv.Value, palette[kv.Key], texture);
    }
    build("Lumbridge Gables", gables, new UnityEngine.Color(0.5f, 0.5f, 0.51f), null);
    sb.Append("roofs: " + contributors + " pieces, " + cells + " cells, " + byColour.Count + " colours; ");
}

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
// each kind as layers of pieces from the foot up: (piece, from, to) in metres over the foot, to 0 for the piece's own
// height. Houses stand on Synty's stone base under the plastered wall; castle stone is one piece a storey high; the
// low walls of an upper level are the battlements on the walls below
const float Sink = 0.8f;   // full walls reach into the ground (or the floor below), so a slope never shows under them
var wallLayers = new System.Collections.Generic.Dictionary<string, (string Piece, float From, float To)[]>
{
    // a building's walls are all one shape (the castle wall's, plain, with a window or with a door), in its stuff:
    // grey stone, white stucco or planks (a hut was plaster and timber round its windows and door, planks between)
    { "stone", new[] { ("SM_Bld_Castle_Wall_01", -Sink, Floor) } },
    { "plaster", new[] { ("SM_Bld_Castle_Wall_01", -Sink, Floor) } },
    { "wood", new[] { ("SM_Bld_Castle_Wall_01", -Sink, Floor) } },
    { "stone window", new[] { ("SM_Bld_Castle_Wall_01", -Sink, 1.25f), ("SM_Bld_Castle_Wall_Window_01", 1.25f, Floor) } },
    { "plaster window", new[] { ("SM_Bld_Castle_Wall_01", -Sink, 1.25f), ("SM_Bld_Castle_Wall_Window_01", 1.25f, Floor) } },
    { "stone door", new[] { ("SM_Bld_Castle_Wall_Door_Big_01", -0.05f, Floor) } },
    { "plaster door", new[] { ("SM_Bld_Castle_Wall_Door_Big_01", -0.05f, Floor) } },
    { "wood door", new[] { ("SM_Bld_Castle_Wall_Door_Big_01", -0.05f, Floor) } },
    { "door", new[] { ("SM_Bld_Castle_Wall_Door_Big_01", -0.05f, Floor) } },
    { "fence", new[] { ("SM_Prop_Fence_03", -0.05f, 0f) } },
    { "gate", new[] { ("SM_Prop_Fence_Gate_01", -0.05f, 0f) } },
    { "low", new[] { ("SM_Env_StoneWall_01", -0.05f, 0f) } },
    { "field wall", new[] { ("SM_Env_StoneWall_01", -0.3f, 1.5f) } },
    { "battlement", new[] { ("SM_Bld_Castle_Battlements_01", -0.02f, 0f) } },
};
// windows: OSRS hangs a window on a wall as a decoration (an unnamed, wall high one on the wall's edge, inside and out)
var windows = new System.Collections.Generic.HashSet<string>();
// roofed tiles, any level: a low wall on an upper level under a roof is the attic's, not a battlement
var roofedAny = new System.Collections.Generic.HashSet<long>();
foreach (var l in locs)
{
    var t = (int)l["type"];
    if (t >= 12 && t <= 21) roofedAny.Add(((long)(int)l["x"] << 20) | (uint)(int)l["y"]);
    if (t != 4 && t != 5) continue;
    var n = (string)l["name"];
    var look = l["look"];
    if ((n != null && n != "null") || look == null || look["rgb"].Type == Newtonsoft.Json.Linq.JTokenType.Null || (int)look["height"] < 200) continue;
    windows.Add((int)l["x"] + "," + (int)l["y"] + "," + (int)l["level"] + "," + (int)l["rot"]);
}
System.Func<int, int, bool> underRoof = (x, y) => roofedAny.Contains(((long)x << 20) | (uint)y) || roofFootprint.Contains(((long)x << 20) | (uint)y);
var stoneGrey = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/Alts/PolygonFantasyKingdom_Mat_Castle_Wall_02.mat")
    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Wall_02.mat");
var stucco = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/Alts/PolygonFantasyKingdom_Mat_Castle_Wall_05.mat");
// plank walls: the castle wall's shape with planks of our own (Lumbridge Planks.png, from the image generator)
UnityEngine.Material planks = null;
{
    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(Folder + "/Lumbridge Planks.png");
    if (tex && stoneGrey)
    {
        var pm = new UnityEngine.Material(stoneGrey) { name = "Lumbridge Planks" };
        foreach (var prop in new[] { "_BaseMap", "_MainTex" }) if (pm.HasProperty(prop)) pm.SetTexture(prop, tex);
        foreach (var prop in new[] { "_BumpMap", "_NormalMap" }) if (pm.HasProperty(prop)) pm.SetTexture(prop, null);
        planks = (UnityEngine.Material)keep(pm, Folder + "/Lumbridge Planks.mat");
    }
}
// every wall's kind by place, for doors: a door in a stone wall is a stone doorway
var kindAt = new System.Collections.Generic.Dictionary<(int, int, int, int), string>();
foreach (var l in locs)
{
    var t = (int)l["type"];
    if (t != 0) continue;
    var k = wallKind(l);
    if (k != null) kindAt[((int)l["x"], (int)l["y"], (int)l["level"], (int)l["rot"])] = k;
}
// what stands over a tile: walls on an upper level, upper floors (a stone wall with nothing over it on either side is a
// field wall, waist high rubble in the game, not a building's)
var overTile = new System.Collections.Generic.HashSet<long>();
foreach (var l in locs)
{
    var t = (int)l["type"];
    if ((t == 0 || t == 2 || t == 9) && (int)l["level"] > 0) overTile.Add(((long)(int)l["x"] << 20) | (uint)(int)l["y"]);
}
foreach (var f in map["floors"].ToObject<int[][][]>()[1]) if (f.Length < 3 || f[2] >= 0) overTile.Add(((long)f[0] << 20) | (uint)f[1]);
var wallCount = 0; var skipped = 0;
var fenceEdges = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<((int, int), (int, int))>>();
var kinds = new System.Collections.Generic.Dictionary<string, int>();
foreach (var l in locs)
{
    var type = (int)l["type"];
    if (type != 0 && type != 2 && type != 9) continue;
    var kind = wallKind(l);
    if (kind == null) { skipped++; continue; }
    int x = (int)l["x"], y = (int)l["y"], rot = (int)l["rot"], level = (int)l["level"];
    var onBridge = level > 0 && isBridge(x, y);
    level = levelAt(x, y, level);
    var rawLevel = (int)l["level"];
    if (inland(x + 0.5f, y + 0.5f) < 0.55f) { skipped++; continue; }   // the coast took this tile
    if (kind == "low" && level > 0 && !onBridge)
    {
        // under a roof (this tile or the one across the wall) it is the attic's low wall, hidden by the roof
        var (nx, ny) = rot == 0 ? (x - 1, y) : rot == 1 ? (x, y + 1) : rot == 2 ? (x + 1, y) : (x, y - 1);
        if (underRoof(x, y) || underRoof(nx, ny)) { skipped++; continue; }
        kind = "battlement";
    }
    if (kind == "door" || kind == "gate")
    {
        // an open door is kept a quarter turn round onto the next edge of its tile: it goes back into the wall line,
        // the edge with walls in line with it either side
        System.Func<int, int> inLine = r =>
        {
            var al = r % 2 == 0 ? (0, 1) : (1, 0); var n = 0;
            for (var d = 1; d <= 2; d++)
                foreach (var sgn in new[] { -1, 1 })
                    if (kindAt.ContainsKey((x + al.Item1 * d * sgn, y + al.Item2 * d * sgn, rawLevel, r))) n++;
            return n;
        };
        if (type == 0 && inLine(rot) == 0)
            foreach (var r in new[] { (rot + 1) % 4, (rot + 3) % 4 })
                if (inLine(r) > 0) { rot = r; break; }
        // the walls in line with it, either side: stone ones make it a stone doorway
        var along = rot % 2 == 0 ? (0, 1) : (1, 0);
        var stone = 0; var other = 0;
        for (var d = 1; d <= 2; d++)
            foreach (var sgn in new[] { -1, 1 })
                if (kindAt.TryGetValue((x + along.Item1 * d * sgn, y + along.Item2 * d * sgn, rawLevel, rot), out var nk))
                {
                    if (nk == "stone") stone++; else if (nk == "plaster" || nk == "wood") other++;
                }
        if (kind == "door")
        {
            // the most common stuff of the walls in line with it
            var wood = 0; var plaster = 0;
            for (var d = 1; d <= 2; d++)
                foreach (var sgn in new[] { -1, 1 })
                    if (kindAt.TryGetValue((x + along.Item1 * d * sgn, y + along.Item2 * d * sgn, rawLevel, rot), out var nk2))
                    {
                        if (nk2 == "wood") wood++; else if (nk2 == "plaster") plaster++;
                    }
            kind = stone >= wood && stone >= plaster ? "stone door" : wood >= plaster ? "wood door" : "plaster door";
        }
    }
    // (brown ones too: the wheat field's rubble walls have a brownish texture)
    if ((kind == "stone" || kind == "wood") && level == 0)
    {
        var (fx, fy) = rot == 0 ? (x - 1, y) : rot == 1 ? (x, y + 1) : rot == 2 ? (x + 1, y) : (x, y - 1);
        var covered = underRoof(x, y) || underRoof(fx, fy) || overTile.Contains(((long)x << 20) | (uint)y) || overTile.Contains(((long)fx << 20) | (uint)fy);
        if (!covered) kind = "field wall";
    }
    // a window on this wall: the window pieces of its kind
    if (type == 0 && windows.Contains(x + "," + y + "," + rawLevel + "," + rot))
    {
        if (kind == "stone") kind = "stone window";
        else if (kind == "plaster") kind = "plaster window";
        // planks have no window piece: the hut's walls stay plain
    }
    var lift = level * Floor + (onBridge ? Deck : 0f);
    var parent = group("Walls " + kind);
    // the tile's corners, south west first, going round: 0 (x, y), 1 (x, y+1), 2 (x+1, y+1), 3 (x+1, y)
    deckAt.TryGetValue(((long)x << 20) | (uint)y, out var deck);
    System.Func<int, float, UnityEngine.Vector3> cornerAt = (c, up) =>
    {
        var cx = x + (c >= 2 ? 1 : 0); var cy = y + (c == 1 || c == 2 ? 1 : 0);
        // on a bridge, the deck's own arch
        if (onBridge && deck != null) return new UnityEngine.Vector3(cx * Tile, deck[c] + up, cy * Tile);
        return new UnityEngine.Vector3(cx * Tile, corner(cx, cy) + lift + up, cy * Tile);
    };
    // the edges this wall runs along, as pairs of corners
    var edges = new System.Collections.Generic.List<(int, int)>();
    if (type == 9) edges.Add(rot % 2 == 0 ? (0, 2) : (1, 3));   // across the tile
    else
    {
        // on the tile's west (rot 0), north (1), east (2) or south (3) edge; a corner on two of them
        edges.Add((rot, (rot + 1) % 4));
        if (type == 2) edges.Add(((rot + 1) % 4, (rot + 2) % 4));
    }
    // fences and low walls on the ground: gathered into lines, laid as WorldFences below
    if ((kind == "fence" || kind == "field wall" || kind == "low") && level == 0 && !onBridge)
    {
        if (!fenceEdges.TryGetValue(kind, out var list)) fenceEdges[kind] = list = new System.Collections.Generic.List<((int, int), (int, int))>();
        System.Func<int, (int, int)> cc = c => (x + (c >= 2 ? 1 : 0), y + (c == 1 || c == 2 ? 1 : 0));
        foreach (var (ca, cb) in edges) list.Add((cc(ca), cc(cb)));
        wallCount++;
        kinds[kind] = kinds.TryGetValue(kind, out var kc0) ? kc0 + 1 : 1;
        continue;
    }
    foreach (var (ca, cb) in edges)
    foreach (var layer in wallLayers[kind])
    {
        var pf = prefab(layer.Piece);
        if (!pf) { skipped++; continue; }
        // a wall's foot follows the higher end so its top is level (the sink keeps the low end in the ground); fences
        // and field walls follow the lower one, as they hug the ground
        var a = cornerAt(ca, 0f); var b = cornerAt(cb, 0f);
        var hugs = kind == "fence" || kind == "gate" || kind == "low";
        var top = hugs ? UnityEngine.Mathf.Min(a.y, b.y) : UnityEngine.Mathf.Max(a.y, b.y);
        if (kind == "fence")
        {
            // a fence piece has a post at each end, inset a little: reaching past both ends, a piece's posts sit on its
            // neighbours' instead of standing in pairs at every join
            var along2 = (b - a).normalized * 0.11f;
            a -= along2; b += along2;
        }
        a.y = b.y = top + layer.From;
        span(pf, parent, a, b, layer.To > 0 ? layer.To - layer.From : 0f);
        // Lumbridge's stone is grey: the pack's castle walls are beige brick, its dungeon stone grey
        // its stuff: the pieces' wall faces (Castle_Wall materials) get it, a door keeps its wood and iron
        var stuff = kind.StartsWith("stone") || kind == "battlement" ? stoneGrey : kind.StartsWith("plaster") ? stucco : kind.StartsWith("wood") ? planks : null;
        if (stuff)
            foreach (var r in parent.GetChild(parent.childCount - 1).GetComponentsInChildren<UnityEngine.Renderer>())
            {
                var ms = r.sharedMaterials;
                for (var mi = 0; mi < ms.Length; mi++) if (ms[mi] && ms[mi].name.Contains("Castle_Wall")) ms[mi] = stuff;
                r.sharedMaterials = ms;
            }
    }
    wallCount++;
    kinds[kind] = kinds.TryGetValue(kind, out var kc) ? kc + 1 : 1;
}
// ---- fence lines ----------------------------------------------------------------------------------------------
stage = "fences";
{
    // each kind's edges chained into lines through the corners that join exactly two edges, straight runs merged;
    // each line one WorldFence, its pieces bent to the ground as OSRS does
    var fenceCount = 0;
    foreach (var kv in fenceEdges)
    {
        var byCorner = new System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.List<int>>();
        var es = kv.Value;
        for (var i = 0; i < es.Count; i++)
            foreach (var c in new[] { es[i].Item1, es[i].Item2 })
            {
                if (!byCorner.TryGetValue(c, out var l2)) byCorner[c] = l2 = new System.Collections.Generic.List<int>();
                l2.Add(i);
            }
        var used = new bool[es.Count];
        System.Func<(int, int), bool> through = c => byCorner[c].Count == 2;
        for (var i = 0; i < es.Count; i++)
        {
            if (used[i]) continue;
            used[i] = true;
            var line = new System.Collections.Generic.List<(int, int)> { es[i].Item1, es[i].Item2 };
            // grow both ways
            for (var side = 0; side < 2; side++)
            {
                while (true)
                {
                    var end = line[line.Count - 1];
                    if (!through(end)) break;
                    var next = -1;
                    foreach (var j in byCorner[end]) if (!used[j]) { next = j; break; }
                    if (next < 0) break;
                    used[next] = true;
                    line.Add(es[next].Item1.Equals(end) ? es[next].Item2 : es[next].Item1);
                    if (line[line.Count - 1].Equals(line[0])) break;   // a closed pen
                }
                line.Reverse();
            }
            // straight runs merged
            var pts = new System.Collections.Generic.List<(int, int)> { line[0] };
            for (var k = 1; k + 1 < line.Count; k++)
            {
                var p0 = pts[pts.Count - 1]; var p1 = line[k]; var p2 = line[k + 1];
                var straight = (p1.Item1 - p0.Item1) * (p2.Item2 - p1.Item2) - (p1.Item2 - p0.Item2) * (p2.Item1 - p1.Item1) == 0;
                if (!straight) pts.Add(p1);
            }
            pts.Add(line[line.Count - 1]);
            var go = new UnityEngine.GameObject(kv.Key + " line");
            go.transform.SetParent(group("Walls " + kv.Key), false);
            go.transform.position = new UnityEngine.Vector3(pts[0].Item1 * Tile, 0f, pts[0].Item2 * Tile);
            var fence = go.AddComponent<WorldFence>();
            fence.Piece = prefab(kv.Key == "fence" ? "SM_Prop_Fence_03" : "SM_Env_StoneWall_01");
            if (kv.Key == "field wall") { fence.Height = 1.5f; fence.Sink = 0.3f; }
            else fence.Sink = kv.Key == "fence" ? 0.05f : 0.2f;
            foreach (var p in pts) fence.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(p.Item1 * Tile, 0f, p.Item2 * Tile)));
            fence.Rebuild();
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.BatchingStatic);
            fenceCount++;
        }
    }
    sb.Append("fence lines " + fenceCount + "; ");
}
sb.Append("walls " + wallCount + " (");
foreach (var kv in kinds) sb.Append(kv.Key + " " + kv.Value + " ");
sb.Append(") skipped " + skipped + "; ");

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
    { "Cabbage", new[] { "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Crop_Single_02.prefab" } },   // (no cabbage model in the project)
    { "Potato", new[] { "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Crop_Single_01.prefab", "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Crop_Single_02.prefab" } },   // (a 1.5 m bush before: giant onions)
    { "Onion", new[] { "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Crop_Single_01.prefab", "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Crop_Single_02.prefab" } },
    { "Rock", new[] { "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_01.prefab", "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_02.prefab", "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_03.prefab", "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_04.prefab" } },   // (by name, PolygonAdventure's: brown, one a 2 m column)
    { "Rocks", new[] { "SM_Env_Rock_Chunk_01", "SM_Env_Rock_Chunk_02" } },
    { "Boulder", new[] { "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Boulder_01.prefab" } },   // (by name alone SM_Env_Rock_04 came from PolygonAdventure: a brown block like a stump)
    { "Stones", new[] { "SM_Generic_Small_Rocks_01", "SM_Generic_Small_Rocks_02" } },
    { "Rockslide", new[] { "SM_Env_Rock_Chunk_03" } },
    { "Crate", new[] { "SM_Prop_Crate_01", "SM_Prop_Crate_02" } },
    { "Crates", new[] { "SM_Prop_Crate_Stack_01" } },
    { "Barrel", new[] { "SM_Prop_Barrel_01", "SM_Prop_Barrel_02" } },
    { "Table", new[] { "SM_Prop_Table_01" } },
    { "Shelves", new[] { "SM_Prop_Shelf_01", "SM_Prop_Shelf_02" } },
    { "Sack", new[] { "SM_Prop_Sack_01", "SM_Prop_Sack_02" } },
    { "Sacks", new[] { "SM_Prop_Sack_03", "SM_Prop_Sack_04" } },
    { "Bench", new[] { "Assets/PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Bench_Seat_03.prefab" } },   // (by name, the Dungeon pack's park bench)
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
    { "Cactus", new[] { "SM_Env_Bush_Small_04" } },   // (the only cactus in the project has a shader URP cannot draw)
    { "Potted fern", new[] { "SM_Prop_Pot_Plant_02" } },
    { "Standing torch", new[] { "SM_Prop_Torch_01" } },
    { "Cabinet", new[] { "SM_Prop_Cabinet_01" } },
    { "Axe cabinet", new[] { "SM_Prop_Cabinet_02" } },
    { "Flag", new[] { "SM_Prop_Banner_01_A" } },
    { "Water wheel", new[] { "SM_Bld_Waterwheel_01" } },   // the map has it in four parts: placed once (below)
    { "Yew tree", new[] { "SM_Env_Tree_Large_02" } },
    { "Pear tree", new[] { "SM_Env_Tree_Round_04" } },
    { "Workbench", new[] { "SM_Prop_Workbench_01" } },
    { "Cooking Pots", new[] { "SM_Prop_Cauldron_01" } },
    { "Cooking pot", new[] { "SM_Prop_Cauldron_02" } },
    { "Vat", new[] { "SM_Prop_Cauldron_01" } },
    { "Scarecrow", new[] { "SM_Prop_Scarecrow_01" } },
    { "Sink", new[] { "SM_Prop_Sink_01" } },
    { "Swampy sink", new[] { "SM_Prop_Sink_02" } },
    { "Boxes", new[] { "SM_Prop_Crate_Stack_01" } },
    { "Water Barrel", new[] { "SM_Prop_Barrel_01" } },
    { "Rubble", new[] { "SM_Prop_Marble_Rubble_01", "SM_Prop_Marble_Rubble_02" } },
    { "Fountain", new[] { "SM_Prop_Fountain_01" } },
    { "Plough", new[] { "SM_Prop_Plough_01" } },
    { "Wooden defence", new[] { "SM_Prop_Spike_Fortification_01" } },
    { "Charred bones", new[] { "SM_Prop_Bones_01" } },
    { "Furnace", new[] { "SM_Prop_Forge_01" } },
    { "Pillar", new[] { "SM_Bld_Base_Pillar_02" } },
    { "Warning sign", new[] { "SM_Prop_RoadSign_01" } },
    { "Warning Sign", new[] { "SM_Prop_RoadSign_01" } },
    { "Logs", new[] { "SM_Prop_Logs_01" } },
    { "Fallen tree", new[] { "SM_Prop_Ground_Logs_01" } },
    { "Fire", new[] { "SM_Env_CampFire_01" } },
    { "Dresser", new[] { "SM_Prop_Dresser_01" } },
    { "Tanning line", new[] { "SM_Prop_Camp_Tanning_Rack_01" } },
    { "Weather vane", new[] { "SM_Prop_Weather_Vane_01" } },
    { "Hay Bales", new[] { "SM_Prop_Hay_Bale_01" } },
    { "Open chest", new[] { "SM_Prop_Chest_01" } },
    { "Small table", new[] { "SM_Prop_Table_01" } },
    { "Rocking chair", new[] { "SM_Prop_Seat_Chair_03" } },
    { "Bank table", new[] { "SM_Prop_Table_01" } },
    { "Counter", new[] { "SM_Prop_Table_01" } },
    { "Cooking shelves", new[] { "SM_Prop_Shelf_02" } },
    { "Closed booth", new[] { "SM_Bld_Stall_01" } },
    { "Closed bank booth", new[] { "SM_Bld_Stall_01" } },
    { "Food trough", new[] { "SM_Prop_Hay_Pile_02" } },
    { "Trough", new[] { "SM_Prop_Hay_Pile_02" } },
    { "Chair", new[] { "SM_Prop_Seat_Chair_01", "SM_Prop_Seat_Chair_02" } },
    { "Stool", new[] { "SM_Prop_Seat_Stool_01" } },
    { "Barstool", new[] { "SM_Prop_Seat_Stool_02" } },
    { "Potted plant", new[] { "SM_Prop_Pot_Plant_01", "SM_Prop_Pot_Plant_02", "SM_Prop_Pot_Plant_03" } },
    { "Vase", new[] { "SM_Prop_Vase_01", "SM_Prop_Vase_02" } },
    { "Candles", new[] { "SM_Prop_Candle_Stand_01" } },
    { "Standard", new[] { "SM_Prop_Banner_01" } },
    { "Cannon", new[] { "SM_Wep_Cannon_01" } },
    { "Cannonballs", new[] { "SM_Prop_CannonBalls_01" } },
    { "Suit of armour", new[] { "SM_Prop_Knight_Armor_01" } },
    { "Mushroom", new[] { "SM_Env_Mushroom_01" } },
    { "Fungus", new[] { "SM_Env_Mushroom_01" } },
    { "Stacked barrels", new[] { "SM_Prop_Barrel_Stack_01", "SM_Prop_Barrel_Stack_02" } },
    { "Ruined Pillar", new[] { "SM_Bld_Base_Pillar_01" } },
    { "Church pew", new[] { "SM_Prop_Bench_Seat_Pew_01" } },
    { "Hollow log", new[] { "SM_Env_TreeLog_01" } },
    { "Log pile", new[] { "SM_Prop_Loghalf_01" } },
    { "Waterlily", new[] { "SM_Env_Lily_01", "SM_Env_Lily_02" } },
    { "Basket", new[] { "SM_Prop_Basket_01", "SM_Prop_Basket_02" } },
    { "Tropical tree", new[] { "SM_Env_Tree_Palm_01", "SM_Env_Tree_Palm_02" } },
    { "Tropical leaves", new[] { "SM_Env_Bush_02" } },
    { "Spiny bush", new[] { "SM_Env_Bush_Small_04" } },
    { "Hay bale", new[] { "SM_Prop_Hay_Bale_01" } },
    { "Closed chest", new[] { "SM_Prop_Chest_Wood_01" } },
    { "Bookcase", new[] { "SM_Prop_Shelf_03" } },
    { "Cupboard", new[] { "SM_Prop_Cupboard_01" } },
    { "Wardrobe", new[] { "SM_Prop_Wardrobe_01" } },
    { "Drawers", new[] { "SM_Prop_Dresser_01" } },
    { "Throne", new[] { "SM_Prop_Throne_01" } },
    { "Range", new[] { "SM_Prop_Stove_01" } },
    { "Cooking range", new[] { "SM_Prop_Stove_01" } },
    { "Fireplace", new[] { "SM_Prop_Camp_Fireplace_01" } },
    { "Loom", new[] { "SM_Prop_Loom_01" } },
    { "Bucket", new[] { "SM_Prop_Bucket_01" } },
    { "Wheelbarrow", new[] { "SM_Veh_Wheelbarrow_01" } },
    { "Weapon rack", new[] { "SM_Prop_Weapon_Rack_01" } },
    { "Bank booth", new[] { "SM_Bld_Stall_01" } },
    { "Market stall", new[] { "SM_Bld_Stall_02" } },
    { "Bed", new[] { "SM_Prop_Bed_01", "SM_Prop_Bed_02" } },
    { "Gem stall", new[] { "SM_Prop_Market_Stall_01" } },
    { "Silk stall", new[] { "SM_Prop_Market_Stall_02" } },
    { "Kebab", new[] { "SM_Prop_Market_Stall_03" } },
    { "Rusted anvil", new[] { "SM_Prop_Anvil_02" } },
    { "Barrels", new[] { "SM_Prop_Barrel_Stack_02" } },
    { "Sword cabinet", new[] { "SM_Prop_Weapon_Rack_01" } },
    { "Bow cabinet", new[] { "SM_Prop_Weapon_Rack_01" } },
    { "Big vase", new[] { "SM_Prop_Vase_03" } },
    { "Spit roast", new[] { "SM_Env_CampFire_01" } },
    { "Coop", new[] { "Medieval_Shelter_Small" } },                // a small roofed shelter, as wide as the map's 2 tiles
    { "Dairy cow", new[] { "Assets/Low Poly Animated Animals/- Prefabs/Animals/Cow.prefab" } },
    { "Strong Tree", new[] { "SM_Env_Tree_Large_01" } },
    { "Strong Yew", new[] { "SM_Env_Tree_Large_02" } },
    { "Small fishing net", new[] { "SM_Prop_Net_01" } },
    { "Rowboat", new[] { "Assets/PolygonFantasyKingdom/Prefabs/Bonus/SM_Prop_Boat_01.prefab" } },
    { "Dry stone wall", new[] { "SM_Env_StoneWall_01" } },
    { "Mysterious stone", new[] { "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Boulder_01.prefab" } },
    { "Scoreboard", new[] { "SM_Prop_Quest_Board_01" } },
};
var counts = new System.Collections.Generic.Dictionary<string, int>();
var missing = new System.Collections.Generic.Dictionary<string, int>();
foreach (var l in locs)
{
    var type = (int)l["type"];
    if (type != 10 && type != 11) continue;
    var name = (string)l["name"];
    if (name == null || name == "null") continue;
    var key = name.Replace("<col=ff9040>", "").Replace("</col>", "");
    if (key.EndsWith(" rocks") || key.EndsWith(" rock")) key = "Rocks";
    if (!things.TryGetValue(key, out var choices)) { missing[name] = missing.TryGetValue(name, out var mc) ? mc + 1 : 1; continue; }
    var pf = prefab(choices[rng.Next(choices.Length)]);
    if (!pf) { missing[name + " (no prefab)"] = 1; continue; }
    if (key == "Water wheel" && counts.ContainsKey(key)) continue;
    int x = (int)l["x"], y = (int)l["y"], rot = (int)l["rot"], level = (int)l["level"];
    level = levelAt(x, y, level);
    if (level > 0) continue;   // upstairs: not in this test
    if (inland(x + 0.5f, y + 0.5f) < 0.55f) continue;   // the coast took this tile
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
    if (key == "Water wheel") o.transform.localScale *= 0.5f;
    if (key == "Dairy cow") o.transform.localScale *= 0.8f;   // (3.2 m long as it comes)
    if (key == "Boulder" || key == "Mysterious stone") o.transform.localScale *= 0.6f;   // (2.7 m as it comes)
    if (key == "Rock") o.transform.localScale *= 2.5f;   // (PolygonNature's are pebbles of 0.3 to 0.6 m)
    counts[key] = counts.TryGetValue(key, out var cc) ? cc + 1 : 1;
}
var placedTotal = 0; foreach (var kv in counts) placedTotal += kv.Value;
sb.Append("objects " + placedTotal + "; not placed: ");
var miss = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(missing);
miss.Sort((a, b) => b.Value.CompareTo(a.Value));
for (var i = 0; i < miss.Count && i < 60; i++) sb.Append(miss[i].Key + " " + miss[i].Value + ", ");
sb.Append("; ");

// ---- roads ---------------------------------------------------------------------------------------------------------
stage = "roads";
if (roadsMap != null)
{
    var n = 0;
    foreach (var r in (Newtonsoft.Json.Linq.JArray)roadsMap["roads"])
    {
        var allPts = r["points"].ToObject<float[][]>();
        var allWs = r["widths"].ToObject<float[]>();
        // the part on land (a road running off the map's edge stops at the coast)
        var keepPts = new System.Collections.Generic.List<float[]>(); var keepWs = new System.Collections.Generic.List<float>();
        for (var i = 0; i < allPts.Length; i++)
        {
            if (inland(allPts[i][0] / Tile, allPts[i][1] / Tile) < 0.7f) { if (keepPts.Count > 0) break; continue; }
            keepPts.Add(allPts[i]); keepWs.Add(allWs[i]);
        }
        var pts = keepPts.ToArray(); var ws = keepWs.ToArray();
        if (pts.Length < 2) continue;
        var surface = (string)r["surface"] == "Cobbles" ? WorldRoad.Surface.Cobbles : (string)r["surface"] == "Gravel" ? WorldRoad.Surface.Gravel : WorldRoad.Surface.Dirt;
        var go = new UnityEngine.GameObject(surface + " road " + (++n));
        go.transform.SetParent(group("Roads"), false);
        go.transform.position = new UnityEngine.Vector3(pts[0][0], 0f, pts[0][1]);
        var road = go.AddComponent<WorldRoad>();
        road.RoadSurface = surface;
        var sorted = (float[])ws.Clone(); System.Array.Sort(sorted);
        var typical = sorted[sorted.Length / 2];
        road.Width = typical;
        for (var i = 0; i < pts.Length; i++)
            road.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(pts[i][0], 0f, pts[i][1])), ws[i] / typical);
        road.Rebuild();
    }
    sb.Append("roads " + n + "; ");
}

// ---- the game's look: MainWorld's light, sky and post processing, and its grass --------------------------------------
stage = "look";
{
    // MainWorld's render settings (read from the scene: trilight ambient, its sky, no fog)
    UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.48227125f, 0.50093204f, 0.5849056f);
    UnityEngine.RenderSettings.ambientEquatorColor = new UnityEngine.Color(0.4572802f, 0.57152236f, 0.6603774f);
    UnityEngine.RenderSettings.ambientGroundColor = new UnityEngine.Color(0.20507877f, 0.1878208f, 0.15292618f);
    UnityEngine.RenderSettings.ambientIntensity = 1f;
    UnityEngine.RenderSettings.fog = false;
    var sky = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/ASTROFISH_GAMES/MEDIEVAL_SERIES/POLYSTYLE_MEDIEVAL_VILLAGE/Astrofish_Common/C_Assets/C_Materials/Materials/Sky_Prefabs.mat");
    if (sky) UnityEngine.RenderSettings.skybox = sky;
    // its sun: warm white at 1.75 with soft shadows, from the south west in the afternoon
    UnityEngine.Light sun = null;
    foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>()) if (l.type == UnityEngine.LightType.Directional) sun = l;
    if (sun)
    {
        sun.color = new UnityEngine.Color(1f, 0.94f, 0.84f);
        sun.intensity = 1.75f;
        sun.shadows = UnityEngine.LightShadows.Soft;
        sun.transform.rotation = UnityEngine.Quaternion.Euler(48f, -35f, 0f);
        UnityEngine.RenderSettings.sun = sun;
    }
    // its post processing
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Scenes/MainWorld/Post Processing Volume Profile.asset");
    var volumeGo = UnityEngine.GameObject.Find("Post Processing");
    if (!volumeGo) volumeGo = new UnityEngine.GameObject("Post Processing");
    var volume = volumeGo.GetComponent<UnityEngine.Rendering.Volume>() ?? volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
    volume.isGlobal = true;
    volume.sharedProfile = profile;
    var cam0 = UnityEngine.Camera.main;
    if (cam0)
    {
        var data = cam0.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (data) data.renderPostProcessing = true;
    }
    // the grass: the game's profile, but no grass on the first layer (the desert here, meadow dirt elsewhere) and none
    // below the sea
    var baseProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<GrassProfile>("Assets/World/Grass/Grass Profile.asset");
    if (baseProfile)
    {
        var gp = UnityEngine.Object.Instantiate(baseProfile);
        gp.name = "Lumbridge Grass Profile";
        foreach (var layer in gp.layers)
            if (layer != null && layer.terrainLayer && layer.terrainLayer.name == "Terrain_Meadow_Dirt_01") layer.density = 0f;
        gp.waterLevel = seaLevel + 0.3f;
        gp = (GrassProfile)keep(gp, Folder + "/Lumbridge Grass Profile.asset");
        var field = terrainGo.GetComponent<GrassField>() ?? terrainGo.AddComponent<GrassField>();
        field.Profile = gp;
    }
    sb.Append("look; ");
}

// ---- light and a camera over the castle ------------------------------------------------------------------------
stage = "camera";
var cam = UnityEngine.Camera.main;
if (cam)
{
    cam.farClipPlane = 2000f;
    cam.transform.position = new UnityEngine.Vector3(80 * Tile, 90f, 40 * Tile);
    cam.transform.LookAt(new UnityEngine.Vector3(80 * Tile, 5f, 80 * Tile));
}

// the paint is applied by tools/ravebridge/lumbridge-paint.cs, run after this: set here, late in a long run, it never
// reached the saved terrain (it showed until the scene was reopened, then the ground was all one layer)
stage = "paint";
{
    var bytes = new byte[aRes * aRes * 8 * 4];
    System.Buffer.BlockCopy(alpha, 0, bytes, 0, bytes.Length);
    System.IO.File.WriteAllBytes("Temp/lumbridge-paint.bin", bytes);
    sb.Append("paint saved for lumbridge-paint.cs; ");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log("[lumbridge-test] " + (System.IO.File.Exists("Temp/lumbridge-run.txt") ? System.IO.File.ReadAllText("Temp/lumbridge-run.txt").Trim() + " " : "") + sb);
return sb.ToString();

}
catch (System.Exception ex)
{
    UnityEngine.Debug.Log("[lumbridge-test] " + (System.IO.File.Exists("Temp/lumbridge-run.txt") ? System.IO.File.ReadAllText("Temp/lumbridge-run.txt").Trim() + " " : "") + "failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
