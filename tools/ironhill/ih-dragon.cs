// the sleeping dragon on Ironhill's summit: Unka (poly art) with one looping sleep state, no colliders or demo scripts
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ironhill Island").transform;
var old = island.Find("Sleeping Dragon");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var dir = "Assets/World/Ironhill";
if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) UnityEditor.AssetDatabase.CreateFolder("Assets/World", "Ironhill");
var ctrlPath = dir + "/Sleeping Dragon.controller";
var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ctrlPath);
if (!ctrl)
{
    ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
    UnityEngine.AnimationClip sleep = null;
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Malbers Animations/Dragons/4 - Unka the Dragon/Animations/Unka Sleep.FBX"))
        if (o is UnityEngine.AnimationClip c && c.name == "USleep Idle") sleep = c;
    var state = ctrl.layers[0].stateMachine.AddState("Sleep");
    state.motion = sleep;
}
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Malbers Animations/Dragons/4 - Unka the Dragon/Model/Unka Poly Art.prefab");
var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, island);
UnityEditor.PrefabUtility.UnpackPrefabInstance(go, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
go.name = "Sleeping Dragon";
foreach (var c in go.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
foreach (var mb in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(mb);
foreach (var a in go.GetComponentsInChildren<UnityEngine.Animator>(true)) { a.runtimeAnimatorController = ctrl; a.applyRootMotion = false; a.cullingMode = UnityEngine.AnimatorCullingMode.CullCompletely; }
// red, so it stands out against the grey iron stone from the island below
var red = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Malbers Animations/Dragons/4 - Unka the Dragon/Materials/Poly Art/Unka Poly Art Red.mat");
foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true)) { var ms = r.sharedMaterials; for (var i = 0; i < ms.Length; i++) ms[i] = red; r.sharedMaterials = ms; }
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
float x0 = DRAGON_X, z0 = DRAGON_Z;
var y0 = t.SampleHeight(new UnityEngine.Vector3(x0, 0, z0)) + t.transform.position.y;
go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x0, y0 - 0.3f, z0), UnityEngine.Quaternion.Euler(0, DRAGON_YAW, 0));
go.transform.localScale = UnityEngine.Vector3.one * DRAGON_SCALE;
// the crags under its body go, so it lies on the summit and not inside the rock
var crags = island.Find("Mountain"); var cleared = 0;
for (var i = crags.childCount - 1; i >= 0; i--)
{
    var c = crags.GetChild(i);
    if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(c.position.x, c.position.z), new UnityEngine.Vector2(x0, z0)) < DRAGON_CLEAR) { UnityEngine.Object.DestroyImmediate(c.gameObject); cleared++; }
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return "dragon at " + go.transform.position.ToString("F1") + ", cleared " + cleared;
