var sb = new System.Text.StringBuilder("FLT|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("FLT|refused"); return null; }
var mgr = UnityEngine.Object.FindAnyObjectByType<TownHallManager>(UnityEngine.FindObjectsInactive.Include);
var hall = new UnityEngine.Vector3(-18.64f, 0f, -69.72f); // old hall origin, fixed reference for the zone
var plaza = hall + new UnityEngine.Vector3(-5f, 0f, -40f);
UnityEngine.Terrain terrain = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsInactive.Include)) if (t.name == "Terrain") terrain = t;
var td = terrain.terrainData; var org = terrain.transform.position; var size = td.size; var res = td.heightmapResolution;
float step = size.x / (res - 1);
const float P = 8f, beach = -1.2f, seaCut = -3.0f;
// window: zone plus padding for the distance search
int x0 = UnityEngine.Mathf.FloorToInt((hall.x - 175 - org.x) / step), x1 = UnityEngine.Mathf.CeilToInt((hall.x + 165 - org.x) / step);
int z0 = UnityEngine.Mathf.FloorToInt((hall.z - 160 - org.z) / step), z1 = UnityEngine.Mathf.CeilToInt((hall.z + 130 - org.z) / step);
int w = x1 - x0 + 1, h = z1 - z0 + 1;
var hs = td.GetHeights(x0, z0, w, h);
var orig = (float[,])hs.Clone();
// distance to sea (meters), brute force within 60 m
var sea = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
for (var j = 0; j < h; ++j) for (var i = 0; i < w; ++i) if (orig[j, i] * size.y + org.y < seaCut) sea.Add(new UnityEngine.Vector2Int(i, j));
var dist = new float[h, w];
for (var j = 0; j < h; ++j) for (var i = 0; i < w; ++i) dist[j, i] = 999f;
foreach (var s in sea)
{
    for (var dj = -12; dj <= 12; ++dj) for (var di = -12; di <= 12; ++di)
    {
        int ii = s.x + di, jj = s.y + dj; if (ii < 0 || jj < 0 || ii >= w || jj >= h) continue;
        var d = UnityEngine.Mathf.Sqrt(di * di + dj * dj) * step;
        if (d < dist[jj, ii]) dist[jj, ii] = d;
    }
}
System.Func<float, float, float, float> smooth = (a, b, x) => { var t = UnityEngine.Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); };
var changed = 0;
for (var j = 0; j < h; ++j) for (var i = 0; i < w; ++i)
{
    var wx = org.x + (x0 + i) * step; var wz = org.z + (z0 + j) * step;
    var rx = wx - hall.x; var rz = wz - hall.z;
    if (rx < -150 || rx > 135 || rz < -130 || rz > 90) continue;
    var oy = orig[j, i] * size.y + org.y;
    if (oy < seaCut) continue;
    var F = UnityEngine.Mathf.Lerp(9f, 48f, smooth(5f, 45f, rz));
    var t = smooth(0f, F, dist[j, i]);
    var ny = UnityEngine.Mathf.Lerp(beach, P, t);
    var dp = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(wx, wz), new UnityEngine.Vector2(plaza.x, plaza.z));
    var noise = (UnityEngine.Mathf.PerlinNoise(wx * 0.025f + 31.7f, wz * 0.025f + 11.3f) - 0.5f) * 0.8f * smooth(30f, 55f, dp) * t;
    ny += noise;
    // blend back to the old ground at the northern edge and the zone border
    var edge = UnityEngine.Mathf.Min(smooth(-150f, -135f, rx), 1f - smooth(120f, 135f, rx), smooth(-130f, -115f, rz), 1f - smooth(62f, 88f, rz));
    ny = UnityEngine.Mathf.Lerp(oy, ny, edge);
    hs[j, i] = (ny - org.y) / size.y; changed++;
}
td.SetHeights(x0, z0, hs);
// paint: grass with patches, rock on steep ground, dirt on the shore, a cobbled plaza at the centre
int ar = td.alphamapResolution; float astep = size.x / ar;
int a0 = UnityEngine.Mathf.FloorToInt((hall.x - 150 - org.x) / astep), a1 = UnityEngine.Mathf.CeilToInt((hall.x + 135 - org.x) / astep);
int b0 = UnityEngine.Mathf.FloorToInt((hall.z - 130 - org.z) / astep), b1 = UnityEngine.Mathf.CeilToInt((hall.z + 70 - org.z) / astep);
int aw = a1 - a0, ah = b1 - b0; var layers = td.alphamapLayers;
var al = td.GetAlphamaps(a0, b0, aw, ah);
for (var j = 0; j < ah; ++j) for (var i = 0; i < aw; ++i)
{
    var wx = org.x + (a0 + i + 0.5f) * astep; var wz = org.z + (b0 + j + 0.5f) * astep;
    var y = terrain.SampleHeight(new UnityEngine.Vector3(wx, 0, wz)) + org.y;
    if (y < -2.5f) continue;
    var rz = wz - hall.z;
    var nrm = td.GetInterpolatedNormal((wx - org.x) / size.x, (wz - org.z) / size.z);
    var steep = 1f - nrm.y;
    var wgt = new float[layers];
    var g2 = UnityEngine.Mathf.PerlinNoise(wx * 0.04f + 5.1f, wz * 0.04f + 9.4f);
    wgt[2] = 1f - g2 * 0.6f; wgt[3] = g2 * 0.6f;
    var dp = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(wx, wz), new UnityEngine.Vector2(plaza.x, plaza.z));
    var ragged = (UnityEngine.Mathf.PerlinNoise(wx * 0.15f, wz * 0.15f) - 0.5f) * 4f;
    var plazaW = 0f;
    var shore = 1f - smooth(1.0f, 3.5f, y);
    var rock = smooth(0.25f, 0.45f, steep) * (1f - smooth(5.5f, 7.2f, y));
    for (var k = 0; k < layers; ++k) wgt[k] *= (1f - plazaW) * (1f - shore) * (1f - rock);
    wgt[1] += plazaW * (1f - rock); wgt[0] += shore * (1f - rock); wgt[6] += rock;
    var edge = UnityEngine.Mathf.Min(smooth(-150f, -140f, wx - hall.x), 1f - smooth(125f, 135f, wx - hall.x), smooth(-130f, -120f, rz), 1f - smooth(55f, 70f, rz));
    var sum = 0f; for (var k = 0; k < layers; ++k) sum += wgt[k];
    for (var k = 0; k < layers; ++k) al[j, i, k] = UnityEngine.Mathf.Lerp(al[j, i, k], wgt[k] / sum, edge);
}
td.SetAlphamaps(a0, b0, al);
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssets();
sb.Append("changed=").Append(changed).Append(" window=").Append(w).Append("x").Append(h).Append(" sea=").Append(sea.Count).Append(" plaza=").Append(plaza);
UnityEngine.Debug.Log(sb.ToString());
