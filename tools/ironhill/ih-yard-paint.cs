// the hold's yard: packed dark ground (mud) across it, a paved way (footpath tiles) from the entrance to the gate
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
var d = t.terrainData; var res = d.alphamapResolution; var o = t.transform.position; var size = d.size;
int ax0 = (int)((-20 - o.x) / size.x * res), ax1 = (int)((52 - o.x) / size.x * res);
int az0 = (int)((425 - o.z) / size.z * res), az1 = (int)((486 - o.z) / size.z * res);
var w = ax1 - ax0; var hh = az1 - az0;
var maps = d.GetAlphamaps(ax0, az0, w, hh);
float Smooth(float e0, float e1, float v) { var k = UnityEngine.Mathf.Clamp01((v - e0) / (e1 - e0)); return k * k * (3 - 2 * k); }
var rng = new System.Random(5);
for (var j = 0; j < hh; j++)
for (var i = 0; i < w; i++)
{
    var wx = o.x + (ax0 + i + 0.5f) / res * size.x; var wz = o.z + (az0 + j + 0.5f) / res * size.z;
    var qx = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(wx - 16) - 28, 0); var qz = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(wz - 463.5f) - 18, 0);
    var yard = 1 - Smooth(0, 5, UnityEngine.Mathf.Sqrt(qx * qx + qz * qz) + (float)rng.NextDouble() * 1.5f);
    // the way: 5 m wide down the middle, from the gate out past the totem and on toward the island
    var way = (1 - Smooth(2.2f, 3.4f, UnityEngine.Mathf.Abs(wx - 16))) * (1 - Smooth(482, 484, wz)) * Smooth(428, 436, wz);
    // the way is paved with tiles now; under them and round them the same worked ground
    var mud = UnityEngine.Mathf.Max(yard * 0.9f, way); var path = 0f;
    var keep = 1 - mud - path;
    if (keep >= 0.999f) continue;
    for (var l = 0; l < maps.GetLength(2); l++) maps[j, i, l] *= keep;
    maps[j, i, 4] += mud; maps[j, i, 1] += path;
}
UnityEditor.Undo.RegisterCompleteObjectUndo(d, "Ironhill yard");
d.SetAlphamaps(ax0, az0, maps);
return "yard painted";
