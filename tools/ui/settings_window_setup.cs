// Adds the UI Toolkit settings window to MainWorld: a "UI Toolkit Settings" object beside the other
// toolkit windows with a UIDocument (shared panel settings, SettingsWindow.uxml, sorting order 30)
// and the SettingsWindow view, and points GameMenuHandler at it. Safe to run again; it reuses the
// object if it is already there. Edit mode only. Run with the Unity CLI eval_file.
var sb = new System.Text.StringBuilder("SETTINGSUI|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("SETTINGSUI|refused in play mode"); return null; }

UnityEngine.UIElements.UIDocument sibling = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (d.name == "UI Toolkit Player Details") sibling = d;
if (sibling == null) { UnityEngine.Debug.Log("SETTINGSUI|no UI Toolkit Player Details object to sit beside"); return null; }

var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Screens/SettingsWindow.uxml");
if (uxml == null) { UnityEngine.Debug.Log("SETTINGSUI|SettingsWindow.uxml not imported yet"); return null; }

var parent = sibling.transform.parent;
var existing = parent != null ? parent.Find("UI Toolkit Settings") : null;
var go = existing ? existing.gameObject : new UnityEngine.GameObject("UI Toolkit Settings");
if (!existing)
{
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Settings window");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(sibling.transform.GetSiblingIndex() + 1);
}

var doc = go.GetComponent<UnityEngine.UIElements.UIDocument>() ?? go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = sibling.panelSettings;
doc.visualTreeAsset = uxml;
doc.sortingOrder = 30;

var window = go.GetComponent<Shinobytes.UI.SettingsWindow>() ?? go.AddComponent<Shinobytes.UI.SettingsWindow>();
var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>(UnityEngine.FindObjectsInactive.Include);
var so = new UnityEditor.SerializedObject(window);
so.FindProperty("gameManager").objectReferenceValue = gm;
so.ApplyModifiedPropertiesWithoutUndo();

var menu = UnityEngine.Object.FindFirstObjectByType<GameMenuHandler>(UnityEngine.FindObjectsInactive.Include);
var mso = new UnityEditor.SerializedObject(menu);
mso.FindProperty("toolkitSettings").objectReferenceValue = window;
mso.ApplyModifiedPropertiesWithoutUndo();

UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.EditorUtility.SetDirty(menu);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append(existing ? "updated " : "created ").Append(go.name).Append(" under ").Append(parent ? parent.name : "scene root")
  .Append(", panel ").Append(doc.panelSettings ? doc.panelSettings.name : "none").Append(", order ").Append(doc.sortingOrder)
  .Append(", game manager ").Append(gm ? "set" : "MISSING").Append(", menu ").Append(menu ? menu.name : "MISSING");
UnityEngine.Debug.Log(sb.ToString());
return null;
