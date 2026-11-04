// Run with: unity command eval_file --file tools/ravebridge/ravebridge-skeletons.cs
//
// Skeletons in Ravebridge's swamp, as Father Aereck says ("the skeletons that rise from the swamp", the bones quest):
// they only came in hero mode's waves, so a streamer looking for their bones found none (the user, 2026-11-04). A
// fighting group of its own, "Fighting Lv. 10 (skeletons)", copied from the island's goblins (whose rig holds every
// Synty character, skeletons too, as the waves use it) with the skeleton models shown, named "Skeleton" (the loot
// table "skeleton": bones and skulls) and stats between the goblins' (level 5) and the Tormented Soul's (15). They
// stand on walkable ground among the pools, apart from each other, Father Urhney's hut and the Tormented Soul.
// Rerunnable (replaces the group). Saves the scene. The waves keep their skeletons.
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge");
var islandController = island.GetComponent<IslandController>();
var playable = island.transform.Find("Playable");
Chunk goblins = null;
foreach (var c in island.GetComponentsInChildren<Chunk>(true)) if (c.name == "Fighting Lv. 5") goblins = c;
if (!goblins) return "no goblin group";

var oldGroup = playable.Find("Fighting Lv. 10 (skeletons)");
if (oldGroup) UnityEngine.Object.DestroyImmediate(oldGroup.gameObject);

var group = UnityEngine.Object.Instantiate(goblins.gameObject, playable);
group.name = "Fighting Lv. 10 (skeletons)";
var chunk = group.GetComponent<Chunk>();
chunk.RequiredCombatLevel = 10;
chunk.RequiredSkilllevel = 10;
var enemies = group.GetComponentsInChildren<EnemyController>(true);
const int Count = 6;
for (var i = Count; i < enemies.Length; i++) UnityEngine.Object.DestroyImmediate(enemies[i].gameObject);

// walkable spots in the swamp, spread out
var area = 1 << islandController.NavMeshArea;
var keepOut = new System.Collections.Generic.List<(UnityEngine.Vector3 at, float r)>
{
    (new UnityEngine.Vector3(-1722, 0, 1432), 12f),     // Father Urhney's hut
    (new UnityEngine.Vector3(-1776.75f, 0, 1462.63f), 14f), // the Tormented Soul
};
var rng = new System.Random(10);
var spots = new System.Collections.Generic.List<UnityEngine.Vector3>();
for (var attempt = 0; attempt < 4000 && spots.Count < Count; attempt++)
{
    var x = -1815f + (float)rng.NextDouble() * 180f;
    var z = 1440f + (float)rng.NextDouble() * 100f;
    if (!UnityEngine.AI.NavMesh.SamplePosition(new UnityEngine.Vector3(x, 0, z), out var hit, 1.5f, area)) continue;
    var p = hit.position;
    var ok = true;
    foreach (var k in keepOut) if (new UnityEngine.Vector2(p.x - k.at.x, p.z - k.at.z).magnitude < k.r) ok = false;
    foreach (var s in spots) if ((p - s).magnitude < 16f) ok = false;
    if (ok) spots.Add(p);
}
if (spots.Count < Count) return "only " + spots.Count + " spots found";

var looks = new[] { "Character_Skeleton_Soldier_01", "Character_Skeleton_Soldier_02", "Character_Skeleton_Slave_01" };
var kept = group.GetComponentsInChildren<EnemyController>(true);
var sb = new System.Text.StringBuilder();
for (var i = 0; i < kept.Length; i++)
{
    var enemy = kept[i];
    var look = looks[i % looks.Length];
    foreach (UnityEngine.Transform part in enemy.transform)
        if (part.name.StartsWith("Character_")) part.gameObject.SetActive(part.name == look);
    enemy.name = "Skeleton";
    enemy.transform.position = spots[i];
    enemy.transform.rotation = UnityEngine.Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
    enemy.Stats = goblins.GetComponentInChildren<EnemyController>(true).Stats * 1.8f;
    UnityEditor.EditorUtility.SetDirty(enemy);
    sb.Append(look + " at " + spots[i].ToString("0") + "; ");
}

UnityEditor.EditorUtility.SetDirty(chunk);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(island.scene);
return "skeletons: " + sb + " hp " + kept[0].Stats.Health.MaxLevel + " atk " + kept[0].Stats.Attack.MaxLevel;
