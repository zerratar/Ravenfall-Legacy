// paints Ironhill's mountain: rock on steep faces and high up, dark gravel (mud) on the shoulders; the cave untouched
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
var d = t.terrainData; var res = d.alphamapResolution; var o = t.transform.position; var size = d.size;
int ax0 = (int)((-110 - o.x) / size.x * res), ax1 = (int)((120 - o.x) / size.x * res);
int az0 = (int)((395 - o.z) / size.z * res), az1 = (int)((575 - o.z) / size.z * res);
var w = ax1 - ax0; var hgt = az1 - az0;
var maps = d.GetAlphamaps(ax0, az0, w, hgt);
var layers = d.alphamapLayers;
float Smooth(float e0, float e1, float v) { var k = UnityEngine.Mathf.Clamp01((v - e0) / (e1 - e0)); return k * k * (3 - 2 * k); }
var painted = 0;
for (var j = 0; j < hgt; j++)
for (var i = 0; i < w; i++)
{
    var nx = (ax0 + i + 0.5f) / res; var nz = (az0 + j + 0.5f) / res;
    var wx = o.x + nx * size.x; var wz = o.z + nz * size.z;
    var cave = UnityEngine.Mathf.Sqrt(((wx + 35) / 22f) * ((wx + 35) / 22f) + ((wz - 478) / 28f) * ((wz - 478) / 28f));
    if (cave < 1.0f) continue;
    var y = o.y + d.GetInterpolatedHeight(nx, nz) + 3.2f; // above the sea
    if (y < 1.5f) continue;
    var steep = d.GetSteepness(nx, nz);
    var rock = UnityEngine.Mathf.Clamp01(Smooth(24, 36, steep) + Smooth(30, 46, y) * 0.6f);
    var gravel = (1 - rock) * Smooth(9, 22, steep) * Smooth(7, 14, y) * 0.85f;
    var keep = 1 - rock - gravel;
    for (var l = 0; l < layers; l++) maps[j, i, l] *= keep;
    maps[j, i, 6] += rock; maps[j, i, 4] += gravel;
    painted++;
}
UnityEditor.Undo.RegisterCompleteObjectUndo(d, "Ironhill paint");
d.SetAlphamaps(ax0, az0, maps);
return "painted " + painted;
