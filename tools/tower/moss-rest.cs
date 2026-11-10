// Mossgate's resting place (!tower rest): a campfire beside the inn with cushions to meditate on and rugs to sleep on.
// Adds it to Tower_Mossgate (again: an old one is replaced) and sets TowerTown.RestSpots.
var path = "Assets/Scenes/Tower/Tower_Mossgate.unity";
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
var origin = new UnityEngine.Vector3(600f, 0f, -6000f);
var log = new System.Text.StringBuilder();
void Mark(string m) { log.AppendLine(m); System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "moss-rest.txt"), log.ToString()); }
var rootGo = scene.GetRootGameObjects().First(g => g.GetComponent<TowerTown>());
var town = rootGo.GetComponent<TowerTown>();
var terrain = rootGo.GetComponentInChildren<UnityEngine.Terrain>();
float H(float x, float z) { var w = origin + new UnityEngine.Vector3(x, 0, z); return terrain ? terrain.SampleHeight(w) + terrain.transform.position.y : 0f; }
UnityEngine.Vector3 At(float x, float z) => origin + new UnityEngine.Vector3(x, H(x, z), z);

Mark("start " + (terrain ? terrain.name : "no terrain"));
// a clear spot on open ground south of the square (the camera looks from the north: nothing tall may stand north of it)
var buildingsGroup = rootGo.transform.Find("Buildings");
var bounds = new System.Collections.Generic.List<UnityEngine.Bounds>();
foreach (var r in buildingsGroup.GetComponentsInChildren<UnityEngine.MeshRenderer>()) bounds.Add(r.bounds);
float Clearance(UnityEngine.Vector2 c)
{
    var w = origin + new UnityEngine.Vector3(c.x, 0, c.y); var best = float.MaxValue;
    foreach (var bb in bounds) { var dx = UnityEngine.Mathf.Max(bb.min.x - w.x, 0, w.x - bb.max.x); var dz = UnityEngine.Mathf.Max(bb.min.z - w.z, 0, w.z - bb.max.z); best = UnityEngine.Mathf.Min(best, UnityEngine.Mathf.Sqrt(dx * dx + dz * dz)); }
    return best;
}
var candidates = new[] { new UnityEngine.Vector2(-12f, -11f), new UnityEngine.Vector2(-14f, -8f), new UnityEngine.Vector2(-30f, 0f), new UnityEngine.Vector2(30f, 2f), new UnityEngine.Vector2(-8f, -14f), new UnityEngine.Vector2(26f, -22f) };
var center = candidates[0]; var bestClear = -1f;
foreach (var c in candidates) { var cl = Clearance(c); Mark("candidate " + c + " clear " + cl.ToString("F1")); if (cl >= 7f) { center = c; bestClear = cl; break; } if (cl > bestClear) { bestClear = cl; center = c; } }
Mark("picked " + center + " clear " + bestClear.ToString("F1"));
var old = rootGo.transform.Find("Resting Place");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Resting Place").transform; group.SetParent(rootGo.transform, false);
var spots = new UnityEngine.GameObject("Rest Spots").transform; spots.SetParent(group, false);

// clear foliage round it
var foliage = rootGo.transform.Find("Foliage");
var removed = 0;
if (foliage)
    for (var i = foliage.childCount - 1; i >= 0; i--)
    {
        var c = foliage.GetChild(i); var p = c.position - origin;
        if (new UnityEngine.Vector2(p.x - center.x, p.z - center.y).magnitude < 7.5f) { UnityEngine.Object.DestroyImmediate(c.gameObject); removed++; }
    }
Mark("foliage removed " + removed);

UnityEngine.GameObject Place(string prefab, UnityEngine.Vector3 pos, float yaw, float scale = 1f)
{
    var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefab);
    if (!pf) { log.AppendLine("missing " + prefab); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, scene);
    go.transform.SetParent(group, true);
    go.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var col in go.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(col);
    return go;
}
float Yaw(UnityEngine.Vector2 from, UnityEngine.Vector2 to) { var d = to - from; return UnityEngine.Mathf.Atan2(d.x, d.y) * UnityEngine.Mathf.Rad2Deg; }

var viking = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
var fvp = "Assets/Synty/PolygonFantasyVillage/Prefabs/Props/";
Mark("fire");
var c3 = At(center.x, center.y);
var pit = Place(viking + "SM_Prop_Fire_Pit_01.prefab", c3, 0f);
var pitTop = c3.y + 0.3f;
if (pit) { var prs = pit.GetComponentsInChildren<UnityEngine.MeshRenderer>(); if (prs.Length > 0) { var pb = prs[0].bounds; foreach (var pr in prs) pb.Encapsulate(pr.bounds); pitTop = pb.max.y - 0.15f; } }
var fire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", new UnityEngine.Vector3(c3.x, pitTop, c3.z), 0f, 1.2f);
var lightGo = new UnityEngine.GameObject("Campfire Light"); lightGo.transform.SetParent(group, false); lightGo.transform.position = c3 + UnityEngine.Vector3.up * 1.2f;
var light = lightGo.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = new UnityEngine.Color(1f, 0.62f, 0.3f); light.range = 9f; light.intensity = 2.2f; light.shadows = UnityEngine.LightShadows.None;

Mark("cushions");
// cushions in a ring to meditate on, facing the fire
var ring = 2.5f;
for (var i = 0; i < 6; i++)
{
    var a = (i / 6f) * UnityEngine.Mathf.PI * 2f + 0.25f;
    var p2 = center + new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * ring;
    var yaw = Yaw(p2, center);
    var pos = At(p2.x, p2.y);
    Place(fvp + "SM_Prop_Pillow_0" + (1 + i % 4) + ".prefab", pos, yaw + 90f, 1.6f);
    var s = new UnityEngine.GameObject("Meditate " + (i + 1)).transform; s.SetParent(spots, false);
    s.SetPositionAndRotation(pos + UnityEngine.Vector3.up * 0.08f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
}
Mark("rugs");
// rugs further out to sleep on, heads toward the fire
var outer = 5f;
for (var i = 0; i < 5; i++)
{
    var a = (i / 5f) * UnityEngine.Mathf.PI * 2f + 0.9f;
    var p2 = center + new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * outer;
    var yaw = Yaw(p2, center);
    var pos = At(p2.x, p2.y);
    Place(fvp + "SM_Prop_Picnic_Rug_01.prefab", pos, yaw);
    var s = new UnityEngine.GameObject("Sleep " + (i + 1)).transform; s.SetParent(spots, false);
    s.SetPositionAndRotation(pos + UnityEngine.Vector3.up * 0.05f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
}
// logs to lean on, firewood, lanterns
for (var i = 0; i < 3; i++)
{
    var a = (i / 3f) * UnityEngine.Mathf.PI * 2f + 0.6f;
    var p2 = center + new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * 3.7f;
    Place(viking + "SM_Prop_Log_0" + (1 + i % 2) + ".prefab", At(p2.x, p2.y), Yaw(p2, center) + 90f);
}
Place(fvp + "SM_Prop_Firewood_Rack_01.prefab", At(center.x - 6.5f, center.y + 1.5f), 80f);
Place(fvp + "SM_Prop_Lantern_01.prefab", At(center.x + 6.2f, center.y - 1f), 0f, 1.1f);
Place(fvp + "SM_Prop_Lantern_02.prefab", At(center.x - 3f, center.y - 6.3f), 0f, 1.1f);
Place(fvp + "SM_Prop_Sign_01.prefab", At(center.x + 5f, center.y - 4.5f), Yaw(new UnityEngine.Vector2(center.x + 5f, center.y - 4.5f), center) + 180f, 1.1f);

Mark("saving");
town.RestSpots = spots;
UnityEditor.EditorUtility.SetDirty(town);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);

System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "moss-rest.txt"), log.ToString());
return "ok";
