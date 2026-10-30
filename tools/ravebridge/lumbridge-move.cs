// Run with: unity command eval_file --file tools/ravebridge/lumbridge-move.cs (play mode off; takes a minute or two)
//
// Moves the Ravenfall Lumbridge (Assets/Scenes/LumbridgeRavenfall.unity) into MainWorld in place of Ravebridge's old
// ground and buildings (docs/lumbridge-move-plan.md). Ravebridge's island root, its gameplay (Playable) and its two big
// terrains stay; the gameplay is put back on the new layout afterwards (lumbridge-places.cs).
// - Lumbridge's own pieces (roof and floor materials, their textures, the river's mesh, the grass profile) move to
//   Assets/World/Lumbridge, where git keeps them; both Lumbridge scenes keep using them.
// - The island gets a terrain of its own, Assets/TerrainData_Lumbridge.asset: 640 m square at 0.63 m, Lumbridge's
//   480 m in the middle and 80 m round it where the land runs on from the old edge and falls into the sea (buildings,
//   roads and Al Kharid stand right up to Lumbridge's edge in places). Its material is a copy of Lumbridge's in
//   Assets/World/Terrain.
// - The middle of it sits on the old island's middle (-1640, 1610), 2.84 m down so its sea meets the ocean (-3.31).
// - The old terrains' land goes down to their deep sea floor (-30), under the new terrain's.
// - The paint is applied afterwards by lumbridge-move-paint.cs (Temp/lumbridge-move-paint.bin): set in this long run
//   it is lost when the asset is saved.
// - The island's sphere grows to reach all of the land, its navmesh box to the new terrain.
// Not rerunnable on its own result: restore MainWorld (git tag before-lumbridge-move, or
// G:\Ravenfall\Backups\before-lumbridge-move) and run it again.
var sb = new System.Text.StringBuilder();
var stage = "start";
var logPath = System.IO.Path.GetFullPath("Temp/lumbridge-move.txt");
if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
try
{
const float SourceSize = 480f, Margin = 80f, Size = SourceSize + 2f * Margin;
const float SourceBase = -6f, SourceSpan = 30f;
// (the margin goes down to just above the old terrains' deep floor at -30: a shallower one showed as a pale square
// through the water)
const float Base = -32f, Span = 60f, SeaFloor = -29.5f, Fall = 45f;
var offset = new UnityEngine.Vector3(-1880f, -2.84f, 1370f);   // where Lumbridge's (0, 0, 0) goes
var centre = new UnityEngine.Vector2(offset.x + SourceSize * 0.5f, offset.z + SourceSize * 0.5f);

// ---- the source -------------------------------------------------------------------------------------------------
stage = "open Lumbridge";
var source = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/LumbridgeRavenfall.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)");
if (!root) throw new System.Exception("no Lumbridge (Ravenfall) root");
var srcTerrain = root.GetComponentInChildren<UnityEngine.Terrain>();
var srcTd = srcTerrain.terrainData;
var srcMat = srcTerrain.materialTemplate;

// ---- Lumbridge's pieces to Assets/World/Lumbridge -----------------------------------------------------------------
stage = "assets";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/World/Lumbridge")) UnityEditor.AssetDatabase.CreateFolder("Assets/World", "Lumbridge");
var moved = 0;
foreach (var dep in UnityEditor.EditorUtility.CollectDependencies(new UnityEngine.Object[] { root }))
{
    var path = UnityEditor.AssetDatabase.GetAssetPath(dep);
    if (string.IsNullOrEmpty(path) || !(path.StartsWith("Assets/Scenes/LumbridgeTest/") || path.StartsWith("Assets/Scenes/LumbridgeRavenfall/"))) continue;
    if (dep == srcTd || dep == srcMat || dep is UnityEngine.TerrainData) continue;
    if (!UnityEditor.AssetDatabase.IsMainAsset(dep)) continue;
    var to = "Assets/World/Lumbridge/" + System.IO.Path.GetFileName(path);
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(to)) to = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(to);
    var err = UnityEditor.AssetDatabase.MoveAsset(path, to);
    if (!string.IsNullOrEmpty(err)) sb.Append("could not move " + path + ": " + err + "; "); else moved++;
}
sb.Append("moved " + moved + " assets to Assets/World/Lumbridge; ");

// ---- the island's terrain -----------------------------------------------------------------------------------------
stage = "terrain";
const string TdPath = "Assets/TerrainData_Lumbridge.asset";
UnityEditor.AssetDatabase.DeleteAsset(TdPath);
var td = new UnityEngine.TerrainData();
td.heightmapResolution = 1025;
td.alphamapResolution = 1024;
td.baseMapResolution = srcTd.baseMapResolution;
td.size = new UnityEngine.Vector3(Size, Span, Size);
td.terrainLayers = srcTd.terrainLayers;
var srcRes = srcTd.heightmapResolution;
var srcH = srcTd.GetHeights(0, 0, srcRes, srcRes);
// the source's height (world, after the drop) at a point in its own space, clamped to its edge
float SourceHeight(float x, float z)
{
    var fx = UnityEngine.Mathf.Clamp(x, 0f, SourceSize) / SourceSize * (srcRes - 1);
    var fz = UnityEngine.Mathf.Clamp(z, 0f, SourceSize) / SourceSize * (srcRes - 1);
    int i0 = UnityEngine.Mathf.Min((int)fx, srcRes - 2), j0 = UnityEngine.Mathf.Min((int)fz, srcRes - 2);
    float tx = fx - i0, tz = fz - j0;
    var h = UnityEngine.Mathf.Lerp(UnityEngine.Mathf.Lerp(srcH[j0, i0], srcH[j0, i0 + 1], tx), UnityEngine.Mathf.Lerp(srcH[j0 + 1, i0], srcH[j0 + 1, i0 + 1], tx), tz);
    return SourceBase + h * SourceSpan + offset.y;
}
var res = td.heightmapResolution;
var heights = new float[res, res];
for (var j = 0; j < res; j++)
for (var i = 0; i < res; i++)
{
    // in the source's own space
    var x = i / (float)(res - 1) * Size - Margin; var z = j / (float)(res - 1) * Size - Margin;
    var edge = SourceHeight(x, z);
    var outside = UnityEngine.Mathf.Sqrt(UnityEngine.Mathf.Pow(UnityEngine.Mathf.Max(0f, -x, x - SourceSize), 2f) + UnityEngine.Mathf.Pow(UnityEngine.Mathf.Max(0f, -z, z - SourceSize), 2f));
    var y = edge;
    if (outside > 0f)
    {
        // the land runs on and falls into the sea, its shoreline wandering
        var wander = (UnityEngine.Mathf.PerlinNoise((x + offset.x) * 0.02f + 3.1f, (z + offset.z) * 0.02f + 7.7f) - 0.5f) * 30f;
        var t = UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.Clamp01((outside + wander * 0.5f) / (Fall + wander)));
        y = UnityEngine.Mathf.Lerp(edge, UnityEngine.Mathf.Min(edge, SeaFloor), t);
        // and the last metres down to the sea floor all round, so the old terrains meet it under water
        y = UnityEngine.Mathf.Lerp(y, SeaFloor, UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.InverseLerp(Margin - 15f, Margin, outside)));
    }
    heights[j, i] = UnityEngine.Mathf.Clamp01((y - Base) / Span);
}
// the sea deepens away from the shore (Lumbridge's own sea floor lay flat 5 m down to its edge, a shallow square
// round the island): by the distance to the nearest land, rivers and ponds (within 8 m of land) untouched
{
    const float Sea = -3.31f;
    var cell = Size / (res - 1);
    var dist = new float[res, res];
    var queue = new System.Collections.Generic.Queue<(int, int)>();
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
    {
        if (Base + heights[j, i] * Span >= Sea) { dist[j, i] = 0f; queue.Enqueue((j, i)); }
        else dist[j, i] = float.MaxValue;
    }
    // a chamfer distance, spread out from the land
    while (queue.Count > 0)
    {
        var (j, i) = queue.Dequeue();
        for (var dj = -1; dj <= 1; dj++)
        for (var di = -1; di <= 1; di++)
        {
            if (dj == 0 && di == 0) continue;
            int nj = j + dj, ni = i + di;
            if (nj < 0 || ni < 0 || nj >= res || ni >= res) continue;
            var d = dist[j, i] + cell * (dj != 0 && di != 0 ? 1.4142f : 1f);
            if (d < dist[nj, ni]) { dist[nj, ni] = d; queue.Enqueue((nj, ni)); }
        }
    }
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
    {
        var d = dist[j, i];
        if (d <= 8f || d == float.MaxValue) continue;
        var y = Base + heights[j, i] * Span;
        // eased in from 8 to 24 m out (a step where it began showed as a line along every shore)
        var deep = UnityEngine.Mathf.Max(SeaFloor, Sea - 1f - (d - 8f) * 0.6f);
        var w = UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.InverseLerp(8f, 24f, d));
        if (deep < y) heights[j, i] = UnityEngine.Mathf.Clamp01((UnityEngine.Mathf.Lerp(y, deep, w) - Base) / Span);
    }
    // smoothed, out at sea only: the distances from two shores meet in straight creases (a V off the river's mouth,
    // lit through the water)
    const int R = 8;
    for (var pass = 0; pass < 3; pass++)
    {
        var copy = (float[,])heights.Clone();
        var row = new float[res];
        for (var j = 0; j < res; j++)
        {
            float sum = 0; int n = 0;
            for (var i = -R; i < res + R; i++)
            {
                if (i + R < res) { sum += copy[j, i + R]; n++; }
                if (i - R - 1 >= 0) { sum -= copy[j, i - R - 1]; n--; }
                if (i >= 0 && i < res) row[i] = sum / n;
            }
            for (var i = 0; i < res; i++) copy[j, i] = row[i];
        }
        var column = new float[res];
        for (var i = 0; i < res; i++)
        {
            float sum = 0; int n = 0;
            for (var j = -R; j < res + R; j++)
            {
                if (j + R < res) { sum += copy[j + R, i]; n++; }
                if (j - R - 1 >= 0) { sum -= copy[j - R - 1, i]; n--; }
                if (j >= 0 && j < res) column[j] = sum / n;
            }
            for (var j = 0; j < res; j++)
                if (dist[j, i] > 20f && dist[j, i] != float.MaxValue) heights[j, i] = UnityEngine.Mathf.Min(heights[j, i], column[j]);
        }
    }
}
td.SetHeights(0, 0, heights);
// the paint: the source's, its edge texels carried out over the margin (the shader draws the beach by height); saved
// for lumbridge-move-paint.cs
var srcARes = srcTd.alphamapResolution; var layers = srcTd.alphamapLayers;
var srcA = srcTd.GetAlphamaps(0, 0, srcARes, srcARes);
var ares = td.alphamapResolution;
var alpha = new float[ares, ares, layers];
for (var j = 0; j < ares; j++)
for (var i = 0; i < ares; i++)
{
    var x = (i + 0.5f) / ares * Size - Margin; var z = (j + 0.5f) / ares * Size - Margin;
    var si = UnityEngine.Mathf.Clamp((int)(x / SourceSize * srcARes), 0, srcARes - 1);
    var sj = UnityEngine.Mathf.Clamp((int)(z / SourceSize * srcARes), 0, srcARes - 1);
    for (var l = 0; l < layers; l++) alpha[j, i, l] = srcA[sj, si, l];
}
var paintBytes = new byte[ares * ares * layers * 4];
System.Buffer.BlockCopy(alpha, 0, paintBytes, 0, paintBytes.Length);
System.IO.File.WriteAllBytes("Temp/lumbridge-move-paint.bin", paintBytes);
UnityEditor.AssetDatabase.CreateAsset(td, TdPath);
UnityEditor.AssetDatabase.SaveAssets();
// its material: Lumbridge's, its masks from this terrain's paint, its tilings for 640 m, its sea and beach 2.84 m down
const string MatPath = "Assets/World/Terrain/Ravebridge Terrain Lumbridge.mat";
UnityEditor.AssetDatabase.DeleteAsset(MatPath);
var mat = new UnityEngine.Material(srcMat) { name = "Ravebridge Terrain Lumbridge" };
var masks = td.alphamapTextures;
mat.SetTexture("_RoadMask", masks[0]);
if (masks.Length > 1) mat.SetTexture("_SnowMask", masks[1]);
foreach (var p in new[] { "_LayerUVScale", "_MainUVScale", "_SecondaryUVScale" })
    if (mat.HasProperty(p)) { var v = mat.GetVector(p); mat.SetVector(p, new UnityEngine.Vector4(v.x * Size / SourceSize, v.y * Size / SourceSize, v.z, v.w)); }
foreach (var p in new[] { "_SeaLevel", "_SecondaryHeight" }) if (mat.HasProperty(p)) mat.SetFloat(p, mat.GetFloat(p) + offset.y);
UnityEditor.AssetDatabase.CreateAsset(mat, MatPath);
sb.Append("terrain " + Size + " m at " + res + ", sea " + mat.GetFloat("_SeaLevel").ToString("F2") + "; ");
// the grass stops at the sea: 2.84 m down too
var grass = srcTerrain.GetComponent<GrassField>();
if (grass && grass.Profile) { grass.Profile.waterLevel = -0.15f + offset.y; UnityEditor.EditorUtility.SetDirty(grass.Profile); }

// ---- MainWorld: the old Ravebridge out ------------------------------------------------------------------------------
stage = "open MainWorld";
var main = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainWorld.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEngine.Transform island = null;
foreach (var r in main.GetRootGameObjects()) if (r.name == "============ WORLD") island = r.transform.Find("Islands/Ravebridge");
if (!island) throw new System.Exception("no Ravebridge island in MainWorld");
if (island.Find("Lumbridge")) throw new System.Exception("Lumbridge is in already: restore MainWorld first");
stage = "clear";
var cleared = 0;
foreach (var name in new[] { "Environment", "Castle", "Small Goblin House", "Small House", "Farmer House", "Roads", "Swamp Pools", "Dressing", "Playable/Places" })
{
    var t = island.Find(name);
    if (t) { UnityEngine.Object.DestroyImmediate(t.gameObject); cleared++; }
}
sb.Append("old groups taken out " + cleared + "; ");

// ---- Lumbridge in ---------------------------------------------------------------------------------------------------
stage = "bring in";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, main);
root.name = "Lumbridge";
root.transform.SetParent(island, true);
root.transform.position = offset;
var sea = root.transform.Find("Sea (test only)"); if (sea) UnityEngine.Object.DestroyImmediate(sea.gameObject);
// what only the build tools kept: the generated pieces the swaps switched off
var off = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (UnityEngine.Transform g in root.transform)
    if (g.name.StartsWith("Walls ")) foreach (UnityEngine.Transform t in g) if (!t.gameObject.activeSelf) off.Add(t.gameObject);
foreach (var g in off) if (g) UnityEngine.Object.DestroyImmediate(g);
sb.Append("switched off pieces dropped " + off.Count + "; ");
var terrainGo = srcTerrain.gameObject;
srcTerrain.terrainData = td;
var col = terrainGo.GetComponent<UnityEngine.TerrainCollider>(); if (col) col.terrainData = td;
srcTerrain.materialTemplate = mat;
terrainGo.transform.position = new UnityEngine.Vector3(offset.x - Margin, Base, offset.z - Margin);

// ---- the river's surface stops at the sea --------------------------------------------------------------------------
// (it ran on over Lumbridge's sea, a paler wedge over the ocean off the river's mouth): a copy without the triangles
// at sea level, where the ocean shows instead
stage = "river";
foreach (var river in root.GetComponentsInChildren<RiverWater>(true))
{
    var from = river.Mesh; if (!from) continue;
    var verts = from.vertices; var tris = from.triangles; var keepTris = new System.Collections.Generic.List<int>();
    var m = river.transform.localToWorldMatrix;
    for (var k = 0; k < tris.Length; k += 3)
    {
        // (any corner at sea level: long slivers from the river's mouth out to sea made a pale V over the ocean)
        var low = UnityEngine.Mathf.Min(m.MultiplyPoint3x4(verts[tris[k]]).y, UnityEngine.Mathf.Min(m.MultiplyPoint3x4(verts[tris[k + 1]]).y, m.MultiplyPoint3x4(verts[tris[k + 2]]).y));
        if (low < -3.2f) continue;
        keepTris.Add(tris[k]); keepTris.Add(tris[k + 1]); keepTris.Add(tris[k + 2]);
    }
    var copy = UnityEngine.Object.Instantiate(from); copy.name = "Ravebridge River";
    copy.SetTriangles(keepTris, 0); copy.RecalculateBounds();
    const string RiverPath = "Assets/World/Lumbridge/Ravebridge River.asset";
    UnityEditor.AssetDatabase.DeleteAsset(RiverPath);
    UnityEditor.AssetDatabase.CreateAsset(copy, RiverPath);
    river.Mesh = copy;
    sb.Append("river kept " + keepTris.Count / 3 + " of " + tris.Length / 3 + " triangles; ");
}

// ---- the old terrains under the sea -------------------------------------------------------------------------------------
stage = "old terrains";
var lowered = 0;
var footMin = new UnityEngine.Vector2(offset.x - Margin, offset.z - Margin);
var footMax = new UnityEngine.Vector2(offset.x - Margin + Size, offset.z - Margin + Size);
foreach (var t in island.GetComponentsInChildren<UnityEngine.Terrain>(true))
{
    if (t == srcTerrain) continue;
    var otd = t.terrainData; var ores = otd.heightmapResolution;
    var oh = otd.GetHeights(0, 0, ores, ores);
    for (var j = 0; j < ores; j++)
    for (var i = 0; i < ores; i++)
    {
        var wx = t.transform.position.x + i / (float)(ores - 1) * otd.size.x;
        var wz = t.transform.position.z + j / (float)(ores - 1) * otd.size.z;
        if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(wx, wz), centre) > 520f) continue;
        var dx = UnityEngine.Mathf.Max(0f, footMin.x - wx, wx - footMax.x); var dz = UnityEngine.Mathf.Max(0f, footMin.y - wz, wz - footMax.y);
        var outside = UnityEngine.Mathf.Sqrt(dx * dx + dz * dz);
        // all of the old land down to the deep floor round the island (under the new terrain and past it)
        var target = -30f;
        var y = t.transform.position.y + oh[j, i] * otd.size.y;
        if (y <= target) continue;
        oh[j, i] = UnityEngine.Mathf.Clamp01((target - t.transform.position.y) / otd.size.y);
        lowered++;
    }
    otd.SetHeights(0, 0, oh);
    UnityEditor.EditorUtility.SetDirty(otd);
    var field = t.GetComponent<GrassField>(); if (field) UnityEngine.Object.DestroyImmediate(field);
}
UnityEditor.AssetDatabase.SaveAssets();
sb.Append("old terrain samples lowered " + lowered + "; ");

// ---- the island's reach -----------------------------------------------------------------------------------------------
stage = "island";
var sphere = island.GetComponent<UnityEngine.SphereCollider>();
var sphereCentre = island.TransformPoint(sphere.center);
var reach = 0f;
for (var j = 0; j < res; j += 4)
for (var i = 0; i < res; i += 4)
{
    var y = Base + heights[j, i] * Span;
    if (y < -3.31f) continue;
    var wx = terrainGo.transform.position.x + i / (float)(res - 1) * Size; var wz = terrainGo.transform.position.z + j / (float)(res - 1) * Size;
    reach = UnityEngine.Mathf.Max(reach, UnityEngine.Vector2.Distance(new UnityEngine.Vector2(wx, wz), new UnityEngine.Vector2(sphereCentre.x, sphereCentre.z)));
}
sphere.radius = UnityEngine.Mathf.Ceil(reach + 10f);
var surface = island.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if (surface) surface.size = new UnityEngine.Vector3(Size, surface.size.y, Size);
var seaFloorVolume = island.Find("Playable/Sea Floor Not Walkable");
if (seaFloorVolume)
{
    var v = seaFloorVolume.GetComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
    if (v) v.size = new UnityEngine.Vector3(Size + 40f, v.size.y, Size + 40f);
}
sb.Append("sphere radius " + sphere.radius + " (land reaches " + reach.ToString("F0") + " m), navmesh box " + Size + "; ");

// ---- fences on the new ground, with their not walkable boxes ------------------------------------------------------------
stage = "fences";
var fences = 0;
foreach (var f in root.GetComponentsInChildren<WorldFence>(true)) { f.Rebuild(); fences++; }
sb.Append("fences rebuilt " + fences + "; ");

// ---- save -----------------------------------------------------------------------------------------------------------------
stage = "save";
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(main);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(main);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(source, true);
sb.Append("saved MainWorld; ");
System.IO.File.WriteAllText(logPath, "[lumbridge-move] " + sb);
UnityEngine.Debug.Log("[lumbridge-move] " + sb);
return sb.ToString();
}
catch (System.Exception ex)
{
    System.IO.File.WriteAllText(logPath, "[lumbridge-move] failed at " + stage + ": " + ex + "\n" + sb);
    UnityEngine.Debug.Log("[lumbridge-move] failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
