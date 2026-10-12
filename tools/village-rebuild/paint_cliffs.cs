// Paints the town's steep ground with splat layer 1, which the Triplanar Terrain shader uses as its
// darkening "road" mask: pale sand cliffs turn into darker earth. Flat ground goes back to grass.
var CLIFF = CLIFFWEIGHT;
var sb = new System.Text.StringBuilder("PNT|");
var hall = new UnityEngine.Vector3(-18.64f, 0f, -69.72f);
UnityEngine.Terrain terrain = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsInactive.Include)) if (t.name == "Terrain") terrain = t;
var td = terrain.terrainData; var org = terrain.transform.position; var size = td.size;
System.Func<float, float, float, float> smooth = (a, b, x) => { var u = UnityEngine.Mathf.Clamp01((x - a) / (b - a)); return u * u * (3 - 2 * u); };
int ar = td.alphamapResolution; float astep = size.x / ar;
int a0 = UnityEngine.Mathf.FloorToInt((hall.x - 150 - org.x) / astep), a1 = UnityEngine.Mathf.CeilToInt((hall.x + 135 - org.x) / astep);
int b0 = UnityEngine.Mathf.FloorToInt((hall.z - 130 - org.z) / astep), b1 = UnityEngine.Mathf.CeilToInt((hall.z + 70 - org.z) / astep);
int aw = a1 - a0, ah = b1 - b0; var layers = td.alphamapLayers;
var al = td.GetAlphamaps(a0, b0, aw, ah);
var painted = 0;
for (var j = 0; j < ah; ++j) for (var i = 0; i < aw; ++i)
{
    var wx = org.x + (a0 + i + 0.5f) * astep; var wz = org.z + (b0 + j + 0.5f) * astep;
    var y = terrain.SampleHeight(new UnityEngine.Vector3(wx, 0, wz)) + org.y;
    if (y < -3f) continue;
    var rx = wx - hall.x; var rz = wz - hall.z;
    var edge = UnityEngine.Mathf.Min(smooth(-150f, -140f, rx), 1f - smooth(125f, 135f, rx), smooth(-130f, -120f, rz), 1f - smooth(55f, 70f, rz));
    if (edge <= 0f) continue;
    var nrm = td.GetInterpolatedNormal((wx - org.x) / size.x, (wz - org.z) / size.z);
    // the shader turns to sand past ~16 degrees, so start the earth tone a little before that
    var cliff = smooth(0.05f, 0.16f, 1f - nrm.y) * CLIFF;
    var wgt = new float[layers];
    var g2 = UnityEngine.Mathf.PerlinNoise(wx * 0.04f + 5.1f, wz * 0.04f + 9.4f);
    wgt[2] = (1f - g2 * 0.6f) * (1f - cliff); wgt[3] = g2 * 0.6f * (1f - cliff); wgt[1] = cliff;
    for (var k = 0; k < layers; ++k) al[j, i, k] = UnityEngine.Mathf.Lerp(al[j, i, k], wgt[k], edge);
    if (cliff > 0.05f) painted++;
}
td.SetAlphamaps(a0, b0, al);
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssets();
sb.Append("painted=").Append(painted);
UnityEngine.Debug.Log(sb.ToString());
