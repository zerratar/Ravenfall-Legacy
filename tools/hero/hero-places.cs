// Run with: unity command eval_file --file tools/hero/hero-places.cs
//
// Hero mode's places on Ravebridge (docs/hero-mode-design.md, 7b), under "Hero mode" in WORLD/Islands/Ravebridge/
// Playable (run it again to rebuild them; tools/ravebridge/ravebridge-island.cs rebuilds Playable, so run this after):
// - Captain Hargrove (IslandNpc "captain") by the castle's east gate, who offers the wave quest "Hold the line";
// - the wave gates (WaveRun): where the waves come in, each snapped onto the island's navmesh. The stone bridge and
//   the wooden bridge to the east, the metal gate on the desert road, the swamp to the south and the mill road to
//   the north west. A run uses the two gates nearest the hero;
// - the look (7e): the courtyard dressed as the stand (banners at the gate, braziers, the captain's arrows), and
//   each gate with goblin spikes on both sides of the way in and a fire.
var sb = new System.Text.StringBuilder();
var islands = UnityEngine.GameObject.Find("============ WORLD/Islands");
var playable = islands ? islands.transform.Find("Ravebridge/Playable") : null;
if (!playable) return "no Ravebridge/Playable";

var old = playable.Find("Hero mode");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Hero mode").transform;
group.SetParent(playable, false);

System.Func<float, float, UnityEngine.Vector3?> snap = (x, z) =>
{
    var probe = new UnityEngine.Vector3(x, 60f, z);
    if (UnityEngine.Physics.Raycast(probe, UnityEngine.Vector3.down, out var hit, 120f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) probe = hit.point;
    else probe.y = 8f;
    var areas = NavMeshHelper.AreaMaskAt(probe);
    if (UnityEngine.AI.NavMesh.SamplePosition(probe, out var nav, 6f, areas)) return nav.position;
    return null;
};

// the captain, as the other people of Ravebridge are made (tools/ravebridge/ravebridge-island.cs)
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Soldier_Male_01.prefab");
var at = snap(-1621f, 1549f);
if (prefab && at.HasValue)
{
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    go.name = "captain";
    go.transform.position = at.Value;
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, 90f, 0f);
    var animator = go.GetComponentInChildren<UnityEngine.Animator>(true);
    if (!animator) animator = go.AddComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>("Assets/Animations/Barkeep.controller");
    var capsule = go.AddComponent<UnityEngine.CapsuleCollider>();
    capsule.isTrigger = true;
    capsule.center = new UnityEngine.Vector3(0f, 1f, 0f);
    capsule.height = 2f;
    capsule.radius = 0.5f;
    go.AddComponent<IslandNpc>().Key = "captain";
    sb.Append("captain at " + at.Value.ToString("0") + "; ");
}
else sb.Append("no captain (" + (prefab ? "no navmesh" : "no prefab") + "); ");

var gates = new UnityEngine.GameObject("Wave gates").transform;
gates.SetParent(group, false);
foreach (var (name, x, z) in new[]
{
    ("Stone bridge", -1545f, 1577f),
    ("Wooden bridge", -1584f, 1671f),
    ("Desert gate", -1490f, 1590f),
    ("Swamp", -1695f, 1450f),
    ("Mill road", -1755f, 1700f),
})
{
    var p = snap(x, z);
    if (!p.HasValue) { sb.Append(name + " missed; "); continue; }
    var g = new UnityEngine.GameObject(name).transform;
    g.SetParent(gates, false);
    g.position = p.Value;
    sb.Append(name + " " + p.Value.ToString("0") + "; ");
}

// ---- the look (7e): the courtyard as the stand, and the gates the waves come through ----------------------
System.Func<string, UnityEngine.Transform, float, float, float, float, UnityEngine.GameObject> prop = (path, parent, x, z, yaw, scale) =>
{
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!asset) { sb.Append("no " + path + "; "); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset, parent);
    var probe = new UnityEngine.Vector3(x, 60f, z);
    var y = UnityEngine.Physics.Raycast(probe, UnityEngine.Vector3.down, out var ground, 120f, ~0, UnityEngine.QueryTriggerInteraction.Ignore) ? ground.point.y : 8f;
    go.transform.position = new UnityEngine.Vector3(x, y, z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    return go;
};
const string Dungeon = "Assets/PolygonDungeon/Prefabs/";
const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";
const string Empire = "Assets/PolygonAncientEmpire/Prefabs/";
System.Action<UnityEngine.GameObject, float> fire = (brazier, height) =>
{
    if (!brazier) return;
    // Synty's meadow campfire (URP particles; PolygonDungeon's FX_Fire has no material and renders magenta)
    var fx = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/FX/FX_Prefabs/FX_Fire_01.prefab");
    if (!fx) return;
    var f = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fx, brazier.transform);
    f.transform.localPosition = new UnityEngine.Vector3(0f, height, 0f);
};

// the courtyard: banners at the gate, braziers, the captain's arrows and crates
var stand = new UnityEngine.GameObject("Courtyard").transform;
stand.SetParent(group, false);
prop(Kingdom + "Props/Banners/SM_Prop_Battle_Banner_01.prefab", stand, -1623.2f, 1558.6f, 90f, 1.4f);
prop(Kingdom + "Props/Banners/SM_Prop_Battle_Banner_01.prefab", stand, -1623.2f, 1551.4f, 90f, 1.4f);
fire(prop(Empire + "Props/SM_Prop_Brazier_02.prefab", stand, -1627.5f, 1560.5f, 0f, 0.8f), 0.8f);
fire(prop(Empire + "Props/SM_Prop_Brazier_02.prefab", stand, -1627.5f, 1549.5f, 0f, 0.8f), 0.8f);
prop(Kingdom + "Props/SM_Prop_Barrel_Arrows_01.prefab", stand, -1623.4f, 1546.6f, 20f, 1f);
prop(Kingdom + "Props/SM_Prop_Barrel_Arrows_01.prefab", stand, -1624.4f, 1545.9f, 75f, 1f);
prop(Kingdom + "Props/SM_Prop_Crate_Stack_01.prefab", stand, -1625.6f, 1545.2f, 0f, 1f);

// each gate: goblin spikes on both sides of the way in, facing out, and a fire
var courtyard = new UnityEngine.Vector3(-1630f, 0f, 1555f);
foreach (UnityEngine.Transform gate in gates)
{
    // the way in is the path the waves walk: from the gate toward the courtyard, its first few metres
    var outward = gate.position - courtyard; outward.y = 0f; outward.Normalize();
    var path = new UnityEngine.AI.NavMeshPath();
    var inside = UnityEngine.AI.NavMesh.SamplePosition(courtyard + UnityEngine.Vector3.up * 8f, out var court, 6f, UnityEngine.AI.NavMesh.AllAreas) ? court.position : courtyard;
    if (UnityEngine.AI.NavMesh.CalculatePath(gate.position, inside, NavMeshHelper.AreaMaskAt(gate.position), path) && path.corners.Length > 1)
    {
        foreach (var corner in path.corners)
        {
            var d = corner - gate.position; d.y = 0f;
            if (d.magnitude < 4f) continue;
            outward = -d.normalized;
            break;
        }
    }
    var side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, outward);
    var yaw = UnityEngine.Quaternion.LookRotation(outward).eulerAngles.y;
    foreach (var s2 in new[] { -1f, 1f })
    {
        var spot = gate.position + outward * 2f + side * (s2 * 5f);
        prop(Dungeon + "Props/SM_Prop_Goblin_Spikes_01.prefab", gate, spot.x, spot.z, yaw, 0.9f);
    }
    // the fire beside a barricade, on whichever side there is ground to stand on (not the water by a bridge)
    foreach (var s3 in new[] { 1f, -1f })
    {
        var b = gate.position + outward * 2f + side * (s3 * 8f);
        if (!UnityEngine.AI.NavMesh.SamplePosition(b, out var onGround, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) continue;
        fire(prop(Empire + "Props/SM_Prop_Brazier_02.prefab", gate, onGround.position.x, onGround.position.z, 0f, 0.7f), 0.8f);
        break;
    }
}
sb.Append("dressed " + stand.childCount + " courtyard props and " + gates.childCount + " gates; ");

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playable.gameObject.scene);
return sb.ToString();
