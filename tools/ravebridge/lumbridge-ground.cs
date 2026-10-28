// Run with: unity command eval_file --file tools/ravebridge/lumbridge-ground.cs (after the swap, castle and courtyard
// tools; play mode off)
//
// The Ravenfall Lumbridge's ground, softened (Assets/Scenes/LumbridgeRavenfall.unity; the reference scene keeps the
// map's look):
// - the map's two grasses alternate tile by tile in the fields, a checkerboard from above: the second grass is folded
//   into the first (the terrain shader varies the meadow by itself);
// - water was dug tile by tile, so pools and banks stood in steps: the heights round the water are blurred, which
//   rounds the shores (the water's surface stays, the ground meets it in curves).
// Rerunnable, but each run blurs the shores a little more.
var sb = new System.Text.StringBuilder();
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/LumbridgeRavenfall.unity") return "open LumbridgeRavenfall first";
if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
const float Tile = 2.5f;
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
var td = terrain.terrainData;

// ---- one grass ----------------------------------------------------------------------------------------------------
{
    var res = td.alphamapResolution;
    var alpha = td.GetAlphamaps(0, 0, res, res);
    var folded = 0;
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
        if (alpha[j, i, 3] > 0f) { alpha[j, i, 2] += alpha[j, i, 3]; alpha[j, i, 3] = 0f; folded++; }
    td.SetAlphamaps(0, 0, alpha);
    sb.Append("second grass folded on " + folded + " texels; ");
}

// ---- round shores -------------------------------------------------------------------------------------------------
{
    var map = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(@"G:\GitHub\rs-map-viewer\out\lumbridge.json"));
    var overlay = map["overlay"].ToObject<int[][]>();
    int N = (int)map["sizeX"];
    System.Func<int, int, bool> water = (x, y) => x >= 0 && y >= 0 && x < N && y < N && (overlay[x][y] == 442 || overlay[x][y] == 6 || overlay[x][y] == 7);
    var res = td.heightmapResolution;
    var hs = td.GetHeights(0, 0, res, res);
    // the samples within two tiles of a shore (water on one side, land within reach)
    var near = new bool[res, res];
    var count = 0;
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
    {
        var tx = i / (float)(res - 1) * td.size.x / Tile; var ty = j / (float)(res - 1) * td.size.z / Tile;
        bool wet = false, dry = false;
        for (var dx = -2; dx <= 2; dx++) for (var dy = -2; dy <= 2; dy++)
        {
            if (water(UnityEngine.Mathf.FloorToInt(tx + dx), UnityEngine.Mathf.FloorToInt(ty + dy))) wet = true; else dry = true;
        }
        if (wet && dry) { near[j, i] = true; count++; }
    }
    // a few passes of a 5 x 5 blur on those samples only
    for (var pass = 0; pass < 3; pass++)
    {
        var copy = (float[,])hs.Clone();
        for (var j = 2; j < res - 2; j++)
        for (var i = 2; i < res - 2; i++)
        {
            if (!near[j, i]) continue;
            float sum = 0; for (var a = -2; a <= 2; a++) for (var b = -2; b <= 2; b++) sum += copy[j + a, i + b];
            hs[j, i] = sum / 25f;
        }
    }
    td.SetHeights(0, 0, hs);
    sb.Append("shores blurred over " + count + " samples; ");
}

UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return sb.ToString();
