// Run with: unity command eval_file --file tools/ravebridge/castle-turret-roofs-experiment.cs (MainWorld open)
//
// An experiment, not applied (2026-10-31): Fantasy Village's conical slate roofs on the castle's four round turrets.
// Lumbridge's own turrets are flat and crenellated, so the cones looked more fairytale than Lumbridge; kept here so it
// can be tried again. It does not mark the scene dirty: discard or save it by hand.
var lum = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge/Lumbridge").transform;
var old = lum.Find("Castle Turret Roofs (experiment)"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Castle Turret Roofs (experiment)").transform; group.SetParent(lum, false);
var castle = lum.Find("Ravebridge Castle");
// each turret: the middle of its battlement ring and its top
var list = new System.Collections.Generic.List<UnityEngine.Bounds>();
foreach (var r in castle.GetComponentsInChildren<UnityEngine.MeshRenderer>())
{
    if (!r.name.StartsWith("SM_Bld_Castle_Wall_Corner_M_") && !r.name.StartsWith("SM_Bld_Castle_Battlements_M_Corner")) continue;
    var b = r.bounds; var merged = false;
    for (var i = 0; i < list.Count && !merged; i++)
    {
        var d = list[i].center - b.center; d.y = 0;
        if (d.magnitude < 5f) { var m = list[i]; m.Encapsulate(b); list[i] = m; merged = true; }
    }
    if (!merged) list.Add(b);
}
var rings = new System.Collections.Generic.Dictionary<int, UnityEngine.Bounds>();
for (var i = 0; i < list.Count; i++) rings[i] = list[i];
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Synty/PolygonFantasyVillage/Prefabs/Buildings/SM_Bld_Roof_S_Straight_Corner_Out_02.prefab");
var s = "";
foreach (var kv in rings)
{
    var b = kv.Value;
    var radius = UnityEngine.Mathf.Max(b.extents.x, b.extents.z);
    // the piece is a quarter of the cone (1.39 m out, 1.5 m high), four round its pivot make it, as the pack's tower does
    var k = radius * 0.95f / 1.39f;
    for (var q = 0; q < 4; q++)
    {
        var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
        o.transform.SetPositionAndRotation(new UnityEngine.Vector3(b.center.x, b.max.y - 0.3f, b.center.z), UnityEngine.Quaternion.Euler(0f, 45f + q * 90f, 0f));
        o.transform.localScale = new UnityEngine.Vector3(k, k * 1.3f, k);
    }
    s += "turret " + b.center.ToString("F1") + " r " + radius.ToString("F2") + " top " + b.max.y.ToString("F1") + "; ";
}
return s;
