// dresses Ironhill's mountain with grey crags: steep faces and the ridge, facing downhill, half buried; rerunnable (clears its group)
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ironhill Island").transform;
var group = island.Find("Mountain");
if (group) UnityEngine.Object.DestroyImmediate(group.gameObject);
group = new UnityEngine.GameObject("Mountain").transform; group.SetParent(island, false);
UnityEditor.Undo.RegisterCreatedObjectUndo(group.gameObject, "Ironhill crags");
var stone = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Terrain/Ironhill Iron Stone.mat");
string K(string n) => "Assets/PolygonFantasyKingdom/Prefabs/Environments/" + n + ".prefab";
var tall = new[] { K("SM_Env_Rock_Cliff_01"), K("SM_Env_Rock_Cliff_02"), K("SM_Env_Rock_Cliff_03") };
var broad = new[] { K("SM_Env_Rock_Cliff_04"), "Assets/PolygonPirates/Prefabs/Environments/SM_Env_Rock_Huge_01.prefab" };
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
var d = t.terrainData; var o = t.transform.position;
float Smooth(float e0, float e1, float v) { var k = UnityEngine.Mathf.Clamp01((v - e0) / (e1 - e0)); return k * k * (3 - 2 * k); }
var rng = new System.Random(11);
float R() => (float)rng.NextDouble();
var placed = 0;
for (var gz = 400f; gz < 575f; gz += 10f)
for (var gx = -105f; gx < 115f; gx += 10f)
{
    var wx = gx + (R() - 0.5f) * 8f; var wz = gz + (R() - 0.5f) * 8f;
    var cave = UnityEngine.Mathf.Sqrt(((wx + 35) / 22f) * ((wx + 35) / 22f) + ((wz - 478) / 28f) * ((wz - 478) / 28f));
    if (cave < 1.25f) continue;
    // the dwarf hold's yard stays open, and so does the face over its gate
    if (wx > -20 && wx < 52 && wz > 440 && wz < 484) continue;
    if (wx > 4 && wx < 28 && wz < 498) continue;
    var nx = (wx - o.x) / d.size.x; var nz = (wz - o.z) / d.size.z;
    var y = o.y + d.GetInterpolatedHeight(nx, nz);
    var above = y + 3.2f;
    if (above < 6f || wz < 428f) continue;
    var steep = d.GetSteepness(nx, nz);
    var p = UnityEngine.Mathf.Clamp01(Smooth(16, 34, steep) * 0.7f + Smooth(32, 48, above) * 0.5f);
    if (R() > p) continue;
    var isTall = steep > 34f;
    var summit = above > 40f;
    var set = isTall ? tall : summit ? new[] { broad[1] } : broad;
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(set[rng.Next(set.Length)]);
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    var n = d.GetInterpolatedNormal(nx, nz);
    var yaw = UnityEngine.Mathf.Atan2(n.x, n.z) * UnityEngine.Mathf.Rad2Deg + (R() - 0.5f) * 60f;
    var lean = UnityEngine.Quaternion.FromToRotation(UnityEngine.Vector3.up, UnityEngine.Vector3.Lerp(UnityEngine.Vector3.up, n, 0.35f));
    go.transform.rotation = lean * UnityEngine.Quaternion.Euler(0, yaw, 0);
    // a few big ones for mass, many middling ones, wide rather than tall so they read as one rock face
    var big = R() < 0.18f ? 1.6f : 1f;
    var s = big * (isTall ? UnityEngine.Mathf.Lerp(1.0f, 1.6f, R()) : (prefab.name.Contains("Huge") ? UnityEngine.Mathf.Lerp(0.55f, 1.0f, R()) : UnityEngine.Mathf.Lerp(1.1f, 2.0f, R())));
    var wide = isTall ? UnityEngine.Mathf.Lerp(1.4f, 2.0f, R()) : 1f;
    go.transform.localScale = new UnityEngine.Vector3(s * wide * UnityEngine.Mathf.Lerp(0.85f, 1.15f, R()), s * UnityEngine.Mathf.Lerp(0.8f, 1.1f, R()), s * wide);
    var mf = go.GetComponentInChildren<UnityEngine.MeshFilter>();
    var mh = mf ? mf.sharedMesh.bounds.size.y * go.transform.localScale.y : 10f;
    go.transform.position = new UnityEngine.Vector3(wx, y - mh * (isTall ? 0.28f : 0.4f), wz);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        var ms = r.sharedMaterials; for (var i = 0; i < ms.Length; i++) ms[i] = stone; r.sharedMaterials = ms;
    }
    go.isStatic = true;
    placed++;
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return "placed " + placed;
