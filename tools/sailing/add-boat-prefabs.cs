// Run with: unity command eval_file --file tools/sailing/add-boat-prefabs.cs
//
// Puts OwnBoatPrefabs on the scene's GameManager object, pointing at the prefabs tools/sailing/make-boat-prefabs.cs
// made, and saves the scene. Run once, and again if the prefabs are renamed.
var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
if (!game) return "no GameManager in the open scene";
var holder = game.GetComponent<OwnBoatPrefabs>() ?? game.gameObject.AddComponent<OwnBoatPrefabs>();
var so = new UnityEditor.SerializedObject(holder);
foreach (var (field, name) in new[] { ("rowboat", "Rowboat"), ("fishingBoat", "FishingBoat"), ("sloop", "Sloop") })
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Sailing/Boats/" + name + ".prefab");
    if (!prefab) return "missing prefab " + name + ", run make-boat-prefabs.cs first";
    so.FindProperty(field).objectReferenceValue = prefab;
}
so.ApplyModifiedPropertiesWithoutUndo();
var scene = game.gameObject.scene;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "OwnBoatPrefabs on " + game.name + " in " + scene.path;
