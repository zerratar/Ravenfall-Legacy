// Puts the UI Toolkit "getting your game ready" card into MainWorld: a "UI Toolkit Loading" object
// beside the other toolkit windows with a UIDocument (shared panel settings, GameLoading.uxml,
// sorting order 27, under the login screen) and the GameLoadingView. Safe to run again. Edit mode
// only. Run with the Unity CLI eval_file.
var sb = new System.Text.StringBuilder("LOADUI|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("LOADUI|refused in play mode"); return null; }

UnityEngine.UIElements.UIDocument sibling = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (d.name == "UI Toolkit Settings") sibling = d;
if (sibling == null) { UnityEngine.Debug.Log("LOADUI|run settings_window_setup.cs first"); return null; }

var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Screens/GameLoading.uxml");
if (uxml == null) { UnityEngine.Debug.Log("LOADUI|GameLoading.uxml not imported yet"); return null; }

var parent = sibling.transform.parent;
// the toolkit objects sit at the scene root, so look there too, and keep only the first
UnityEngine.GameObject go = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (d.name != "UI Toolkit Loading") continue;
    if (go == null) go = d.gameObject;
    else UnityEditor.Undo.DestroyObjectImmediate(d.gameObject);
}
var created = go == null;
if (created)
{
    go = new UnityEngine.GameObject("UI Toolkit Loading");
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Loading");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(sibling.transform.GetSiblingIndex());
}

var doc = go.GetComponent<UnityEngine.UIElements.UIDocument>() ?? go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = sibling.panelSettings;
doc.visualTreeAsset = uxml;
doc.sortingOrder = 27f;
if (go.GetComponent<Shinobytes.UI.GameLoadingView>() == null) go.AddComponent<Shinobytes.UI.GameLoadingView>();

UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append(created ? "created " : "updated ").Append(go.name).Append(", order ").Append(doc.sortingOrder);
UnityEngine.Debug.Log(sb.ToString());
return null;
