// Puts the UI Toolkit login screen into MainWorld: a "UI Toolkit Login" object beside the other
// toolkit windows with a UIDocument (shared panel settings, LoginScreen.uxml, sorting order 28,
// under the game menu and settings) and the LoginScreenView, points LoginHandler at it, and
// removes the old uGUI LoginView prefab instance. The prefab asset itself is left in place.
// Safe to run again. Edit mode only. Run with the Unity CLI eval_file.
var sb = new System.Text.StringBuilder("LOGINUI|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("LOGINUI|refused in play mode"); return null; }

UnityEngine.UIElements.UIDocument sibling = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (d.name == "UI Toolkit Settings") sibling = d;
if (sibling == null) { UnityEngine.Debug.Log("LOGINUI|run settings_window_setup.cs first"); return null; }

var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Screens/LoginScreen.uxml");
if (uxml == null) { UnityEngine.Debug.Log("LOGINUI|LoginScreen.uxml not imported yet"); return null; }

var parent = sibling.transform.parent;
var existing = parent != null ? parent.Find("UI Toolkit Login") : null;
var go = existing != null ? existing.gameObject : null;
var created = go == null;
if (created)
{
    go = new UnityEngine.GameObject("UI Toolkit Login");
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Login screen");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(sibling.transform.GetSiblingIndex());
}

var doc = go.GetComponent<UnityEngine.UIElements.UIDocument>() ?? go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = sibling.panelSettings;
doc.visualTreeAsset = uxml;
doc.sortingOrder = 28;

var view = go.GetComponent<Shinobytes.UI.LoginScreenView>() ?? go.AddComponent<Shinobytes.UI.LoginScreenView>();
var handler = UnityEngine.Object.FindFirstObjectByType<LoginHandler>(UnityEngine.FindObjectsInactive.Include);
var so = new UnityEditor.SerializedObject(view);
so.FindProperty("handler").objectReferenceValue = handler;
so.ApplyModifiedPropertiesWithoutUndo();

if (handler != null)
{
    var hso = new UnityEditor.SerializedObject(handler);
    hso.FindProperty("screen").objectReferenceValue = view;
    hso.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(handler);
}

// the old uGUI login: every instance of the LoginView prefab in the scene
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/LoginView.prefab");
var removed = 0;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (t == null) continue;
    var o = t.gameObject;
    if (!UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(o)) continue;
    if (UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(o) != prefab) continue;
    UnityEditor.Undo.DestroyObjectImmediate(o);
    ++removed;
}

UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append(created ? "created " : "updated ").Append(go.name).Append(", order ").Append(doc.sortingOrder)
  .Append(", handler ").Append(handler ? "set" : "MISSING").Append(", old login views removed ").Append(removed);
UnityEngine.Debug.Log(sb.ToString());
return null;
