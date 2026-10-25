// Run with: unity command eval_file --file tools/hero/hero-places.cs
//
// Hero mode's places on Ravebridge (docs/hero-mode-design.md, 7b), under "Hero mode" in WORLD/Islands/Ravebridge/
// Playable (run it again to rebuild them; tools/ravebridge/ravebridge-island.cs rebuilds Playable, so run this after):
// - Captain Hargrove (IslandNpc "captain") by the castle's east gate, who offers the wave quest "Hold the line";
// - the wave gates (WaveRun): where the waves come in, each snapped onto the island's navmesh. The stone bridge and
//   the wooden bridge to the east, the metal gate on the desert road, the swamp to the south and the mill road to
//   the north west. A run uses the two gates nearest the hero.
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

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playable.gameObject.scene);
return sb.ToString();
