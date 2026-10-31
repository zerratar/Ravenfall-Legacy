// Run with: unity command eval_file --file tools/ravebridge/vampire-manor-ground.cs (MainWorld open, edit mode), before
// tools/ravebridge/vampire-manor.cs (which sets the house on the ground as this leaves it)
//
// Levels Ravebridge's ground under the vampyre's manor: flat a little under the house's floor over its footprint and the
// front yard, easing back into the ground round it over Blend metres (the west side rises into a hill). Uses the same
// footprint as vampire-manor.cs. Run again and nothing changes: a footprint already flat is left at its height.
const float Scale = 0.85f, Blend = 9f, Sink = 0.12f, Margin = 2f, YardDepth = 11f;
var Centre = new UnityEngine.Vector2(-1727.5f, 1789f);
var DemoMin = new UnityEngine.Vector2(-26f, -54f);
var DemoMax = new UnityEngine.Vector2(22f, -5f);
var half = (DemoMax - DemoMin) * 0.5f * Scale;
// the flat: the footprint and the yard in front (the house faces -z), a margin round it
var minX = Centre.x - half.x - Margin; var maxX = Centre.x + half.x + Margin;
var minZ = Centre.y - half.y - YardDepth * Scale - Margin; var maxZ = Centre.y + half.y + Margin;

var terrain = TerrainGround.At(new UnityEngine.Vector3(Centre.x, 0f, Centre.y));
if (!terrain) return "no terrain under the manor";
var td = terrain.terrainData;
// the level: the mean over the footprint, as vampire-manor.cs takes it, before anything changes
float sum = 0f, lo = float.MaxValue, hi = float.MinValue; var n = 0;
for (var dz = -half.y; dz <= half.y; dz += 3f)
for (var dx = -half.x; dx <= half.x; dx += 3f)
    if (TerrainGround.TryHeight(new UnityEngine.Vector3(Centre.x + dx, 0f, Centre.y + dz), out var h)) { sum += h; n++; lo = UnityEngine.Mathf.Min(lo, h); hi = UnityEngine.Mathf.Max(hi, h); }
var level = sum / n;
if (hi - lo < 0.05f) return "already level at " + level.ToString("F2");

var res = td.heightmapResolution;
var size = td.size;
var origin = terrain.transform.position;
int I(float x) => UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt((x - origin.x) / size.x * (res - 1)), 0, res - 1);
int J(float z) => UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt((z - origin.z) / size.z * (res - 1)), 0, res - 1);
int i0 = I(minX - Blend), i1 = I(maxX + Blend), j0 = J(minZ - Blend), j1 = J(maxZ + Blend);
var heights = td.GetHeights(i0, j0, i1 - i0 + 1, j1 - j0 + 1);
var flat = (level - Sink - origin.y) / size.y;
var changed = 0;
for (var j = j0; j <= j1; j++)
for (var i = i0; i <= i1; i++)
{
    var x = origin.x + (float)i / (res - 1) * size.x;
    var z = origin.z + (float)j / (res - 1) * size.z;
    // the distance outside the flat (0 inside it)
    var dx = UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Max(minX - x, x - maxX));
    var dz = UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Max(minZ - z, z - maxZ));
    var d = UnityEngine.Mathf.Sqrt(dx * dx + dz * dz);
    if (d >= Blend) continue;
    var t = UnityEngine.Mathf.SmoothStep(0f, 1f, d / Blend);
    var was = heights[j - j0, i - i0];
    heights[j - j0, i - i0] = UnityEngine.Mathf.Lerp(flat, was, t);
    changed++;
}
td.SetHeights(i0, j0, heights);
terrain.Flush();
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "levelled " + changed + " heights to " + (level - Sink).ToString("F2") + " (the footprint's mean " + level.ToString("F2") + ")";
