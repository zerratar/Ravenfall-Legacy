// Adds the UI Toolkit game menu to MainWorld: a "UI Toolkit Game Menu" object beside the other
// toolkit windows with a UIDocument (shared panel settings, GameMenuWindow.uxml, sorting order 29,
// just under the settings window) and the GameMenuWindow view, and points GameMenuHandler at it.
// Safe to run again. Edit mode only. Run with the Unity CLI eval_file.
var sb = new System.Text.StringBuilder("GAMEMENUUI|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("GAMEMENUUI|refused in play mode"); return null; }

UnityEngine.UIElements.UIDocument sibling = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (d.name == "UI Toolkit Settings") sibling = d;
if (sibling == null) { UnityEngine.Debug.Log("GAMEMENUUI|run settings_window_setup.cs first"); return null; }

var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Screens/GameMenuWindow.uxml");
if (uxml == null) { UnityEngine.Debug.Log("GAMEMENUUI|GameMenuWindow.uxml not imported yet"); return null; }

var parent = sibling.transform.parent;
UnityEngine.GameObject go = null;
foreach (var root in sibling.gameObject.scene.GetRootGameObjects())
    if (parent == null && root.name == "UI Toolkit Game Menu") go = root;
if (parent != null && parent.Find("UI Toolkit Game Menu")) go = parent.Find("UI Toolkit Game Menu").gameObject;
var created = go == null;
if (created)
{
    go = new UnityEngine.GameObject("UI Toolkit Game Menu");
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Game menu");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(sibling.transform.GetSiblingIndex());
}

var doc = go.GetComponent<UnityEngine.UIElements.UIDocument>() ?? go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = sibling.panelSettings;
doc.visualTreeAsset = uxml;
doc.sortingOrder = 29;

var view = go.GetComponent<Shinobytes.UI.GameMenuWindow>() ?? go.AddComponent<Shinobytes.UI.GameMenuWindow>();
var menu = UnityEngine.Object.FindFirstObjectByType<GameMenuHandler>(UnityEngine.FindObjectsInactive.Include);
var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>(UnityEngine.FindObjectsInactive.Include);
var so = new UnityEditor.SerializedObject(view);
so.FindProperty("handler").objectReferenceValue = menu;
so.FindProperty("gameManager").objectReferenceValue = gm;
so.ApplyModifiedPropertiesWithoutUndo();

var mso = new UnityEditor.SerializedObject(menu);
mso.FindProperty("toolkitMenu").objectReferenceValue = view;
mso.ApplyModifiedPropertiesWithoutUndo();

UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.EditorUtility.SetDirty(menu);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append(created ? "created " : "updated ").Append(go.name).Append(", order ").Append(doc.sortingOrder)
  .Append(", handler ").Append(menu ? "set" : "MISSING").Append(", game manager ").Append(gm ? "set" : "MISSING");
UnityEngine.Debug.Log(sb.ToString());
return null;
