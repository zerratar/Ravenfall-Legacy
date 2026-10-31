// Run with: unity command eval_file --file tools/ravebridge/manor-count.cs (MainWorld open, edit mode), after
// tools/ravebridge/vampire-manor.cs
//
// The Count (Synty Horror Mansion's SM_Chr_Count_01, his hair, hat and cape on him): an island NPC by his coffin at the
// vampyre's manor, only after dark (NightOnly); by day he sleeps. He has nothing to give, only his lines (key "count"
// in StreamingAssets/Ravebridge/quests.json), denying everything about the Duke's wine. The pack's character prefab
// holds every one of its characters' bodies; all but his are turned off. Rerun to rebuild.
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var npcs = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge/Playable/NPCs");
var dressing = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge/Lumbridge/Manor Dressing");
if (!npcs || !dressing) return "Ravebridge's NPCs or the manor not found (run vampire-manor.cs first)";
var old = npcs.transform.Find("The Count (by night)");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var coffin = dressing.transform.Find("SM_Prop_Coffin_01");
if (!coffin) return "no coffin by the manor";

var night = new UnityEngine.GameObject("The Count (by night)");
night.transform.SetParent(npcs.transform, false);
night.AddComponent<NightOnly>();

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Synty/PolygonHorrorMansion/Prefabs/Characters/Characters/SM_Chr_Count_01.prefab");
var count = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, night.transform);
count.name = "count";
foreach (var body in count.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
    body.gameObject.SetActive(body.name == "SM_Chr_Count_01" || body.name == "SM_Chr_Count_Cape_01");
// beside the coffin's foot, on the porch side, looking down the path to the gate
var at = coffin.position + new UnityEngine.Vector3(1.9f, 0f, -1.1f);
at.y = WorldRoad.GroundHeight(at);
count.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0f, 205f, 0f));

var animator = count.GetComponent<UnityEngine.Animator>();
animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>("Assets/Animations/Barkeep.controller");
animator.applyRootMotion = false;
var capsule = count.AddComponent<UnityEngine.CapsuleCollider>();
capsule.isTrigger = true;
capsule.center = new UnityEngine.Vector3(0f, 1f, 0f);
capsule.height = 2f;
capsule.radius = 0.5f;
var npc = count.AddComponent<IslandNpc>();
npc.Key = "count";
// his hat stands tall
npc.HeadHeight = 2.4f;

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
return "the Count at " + at.ToString("F2");
