// Run with: unity command eval_file --file tools/sailing/bake-sea-chart.cs
//
// Bakes the sea chart own boats sail by (docs/own-boats.md, phase 2): a grid over the sailable islands where each
// cell is water a boat may cross (deep enough, with nothing standing in it), and each sailable island's dock point
// (where the ferry docks: the end of its spline into that island, which is where an own boat comes alongside). Writes
// Assets/StreamingAssets/Sailing/sea-chart.json, read by SeaChart in the game, and a picture of it to
// Temp/sea-chart.png to look over. Rerun after islands, docks or the ferry's splines move.
const float Cell = 10f;          // metres a cell
const float Margin = 250f;       // open sea round the islands
const float Depth = 0.8f;        // the ground must lie this far under the sea for a boat
const float Clearance = 3.5f;    // and nothing may stand within this of the cell's centre
var ocean = UnityEngine.Object.FindAnyObjectByType<Ocean>();
var sea = ocean ? ocean.transform.position.y : -3.31f;
var islands = new System.Collections.Generic.List<IslandController>();
foreach (var i in UnityEngine.Object.FindObjectsByType<IslandController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (i.DockingArea != null) islands.Add(i); // Sailable less the lock: a locked island (Mangrove) still gets its dock point

// where each dock is: what DockController.Start works out, which has not run outside play
UnityEngine.Vector3 DockOf(IslandController i)
{
    var d = i.DockingArea;
    if (d.DockPositionTransform) return d.DockPositionTransform.position;
    var box = d.GetComponent<UnityEngine.BoxCollider>();
    return box ? box.center + d.transform.position : d.transform.position;
}

// the area: every dock and island centre, with open sea round them
float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
foreach (var i in islands)
{
    foreach (var p in new[] { i.transform.position, DockOf(i) })
    {
        minX = System.Math.Min(minX, p.x); maxX = System.Math.Max(maxX, p.x);
        minZ = System.Math.Min(minZ, p.z); maxZ = System.Math.Max(maxZ, p.z);
    }
}
minX -= Margin; minZ -= Margin; maxX += Margin; maxZ += Margin;
var width = UnityEngine.Mathf.CeilToInt((maxX - minX) / Cell);
var height = UnityEngine.Mathf.CeilToInt((maxZ - minZ) / Cell);

var terrains = UnityEngine.Terrain.activeTerrains;
float Ground(float x, float z)
{
    foreach (var t in terrains)
    {
        var tp = t.transform.position; var size = t.terrainData.size;
        if (x < tp.x || z < tp.z || x > tp.x + size.x || z > tp.z + size.z) continue;
        return t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + tp.y;
    }
    return sea - 100f; // past every terrain: open sea
}

UnityEngine.Physics.SyncTransforms();
var hits = new UnityEngine.Collider[16];
bool Blocked(float x, float z)
{
    var n = UnityEngine.Physics.OverlapCapsuleNonAlloc(new UnityEngine.Vector3(x, sea - 1f, z), new UnityEngine.Vector3(x, sea + 10f, z), Clearance, hits, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
    for (var k = 0; k < n; k++)
    {
        var c = hits[k];
        if (c.GetComponentInParent<Ocean>() || c.GetComponentInParent<FerryController>() || c.GetComponentInParent<PlayerController>()) continue;
        if (c is UnityEngine.TerrainCollider) continue; // the ground is judged by its height
        if (c.name.Contains("Do Not Walk") || c.name.Contains("Not Walkable")) continue; // the box keeping walkers off the sea
        return true;
    }
    return false;
}

var bits = new byte[(width * height + 7) / 8];
var water = 0;
for (var z = 0; z < height; z++)
{
    for (var x = 0; x < width; x++)
    {
        var wx = minX + (x + 0.5f) * Cell; var wz = minZ + (z + 0.5f) * Cell;
        if (Ground(wx, wz) > sea - Depth || Blocked(wx, wz)) continue;
        var index = z * width + x;
        bits[index >> 3] |= (byte)(1 << (index & 7));
        water++;
    }
}

// the dock points: the ferry's spline ends, the one nearest each island's dock
var ps = UnityEngine.Object.FindAnyObjectByType<PathSelector>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var splines = new System.Collections.Generic.List<UnityEngine.Splines.SplineContainer>((UnityEngine.Splines.SplineContainer[])typeof(PathSelector).GetField("splines", flags).GetValue(ps));
foreach (var name in new[] { "toMangrove", "fromMangrove" })
    if (typeof(PathSelector).GetField(name, flags)?.GetValue(ps) is UnityEngine.Splines.SplineContainer c && c) splines.Add(c);
var docks = new Newtonsoft.Json.Linq.JArray();
var log = new System.Text.StringBuilder();
foreach (var i in islands)
{
    var dock = DockOf(i);
    var best = UnityEngine.Vector3.zero; var heading = UnityEngine.Vector3.forward; var bestDistance = float.MaxValue;
    foreach (var c in splines)
    {
        foreach (var t in new[] { 0f, 1f })
        {
            UnityEngine.Vector3 p = c.EvaluatePosition(t);
            var d = new UnityEngine.Vector2(p.x - dock.x, p.z - dock.z).magnitude;
            if (d >= bestDistance) continue;
            bestDistance = d; best = p;
            UnityEngine.Vector3 tangent = c.EvaluateTangent(t);
            heading = t == 0f ? -tangent : tangent; // the way a boat faces coming in
        }
    }
    var deg = UnityEngine.Mathf.Atan2(heading.x, heading.z) * UnityEngine.Mathf.Rad2Deg;
    docks.Add(new Newtonsoft.Json.Linq.JObject { ["island"] = i.Identifier, ["x"] = System.Math.Round(best.x, 2), ["z"] = System.Math.Round(best.z, 2), ["heading"] = System.Math.Round(deg, 1) });
    log.Append(i.Identifier + " " + bestDistance.ToString("0") + " m off the dock; ");
}

var json = new Newtonsoft.Json.Linq.JObject
{
    ["comment"] = "Baked by tools/sailing/bake-sea-chart.cs: which cells own boats may cross (bits, row by row from originX/originZ, " + Cell + " m cells) and where they come alongside each island. Rerun the tool, never edit by hand.",
    ["cell"] = Cell,
    ["originX"] = System.Math.Round(minX, 2),
    ["originZ"] = System.Math.Round(minZ, 2),
    ["width"] = width,
    ["height"] = height,
    ["sea"] = sea,
    ["water"] = System.Convert.ToBase64String(bits),
    ["docks"] = docks,
};
System.IO.Directory.CreateDirectory("Assets/StreamingAssets/Sailing");
System.IO.File.WriteAllText("Assets/StreamingAssets/Sailing/sea-chart.json", json.ToString(Newtonsoft.Json.Formatting.Indented));

// a picture to look over: water blue, land sand, docks red
var tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
for (var z = 0; z < height; z++)
    for (var x = 0; x < width; x++)
    {
        var index = z * width + x;
        var w = (bits[index >> 3] & (1 << (index & 7))) != 0;
        tex.SetPixel(x, z, w ? new UnityEngine.Color(0.2f, 0.45f, 0.8f) : new UnityEngine.Color(0.85f, 0.8f, 0.6f));
    }
foreach (var d in docks)
{
    var cx = (int)(((float)d["x"] - minX) / Cell); var cz = (int)(((float)d["z"] - minZ) / Cell);
    for (var dx = -2; dx <= 2; dx++) for (var dz = -2; dz <= 2; dz++) tex.SetPixel(cx + dx, cz + dz, UnityEngine.Color.red);
}
tex.Apply();
System.IO.File.WriteAllBytes("Temp/sea-chart.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
UnityEngine.Object.DestroyImmediate(tex);
UnityEditor.AssetDatabase.Refresh();
return width + " x " + height + " cells, " + water + " water; " + log;
