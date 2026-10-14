// Puts the UI Toolkit chat connection warning into MainWorld: a "UI Toolkit Connection" object
// beside the other toolkit windows with a UIDocument (shared panel settings, ConnectionStatus.uxml,
// sorting order 28.5, over the login screen and under the menus) and the ConnectionStatusView.
// Removes the old uGUI bot and Twitch icons: the RavenBotConnectionUI component and the two images
// it switched on and off, and the ConnectionChecker server banner. Safe to run again. Edit mode only. Run with the Unity CLI eval_file.
var sb = new System.Text.StringBuilder("CONNUI|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("CONNUI|refused in play mode"); return null; }

UnityEngine.UIElements.UIDocument sibling = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (d.name == "UI Toolkit Settings") sibling = d;
if (sibling == null) { UnityEngine.Debug.Log("CONNUI|run settings_window_setup.cs first"); return null; }

var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Screens/ConnectionStatus.uxml");
if (uxml == null) { UnityEngine.Debug.Log("CONNUI|ConnectionStatus.uxml not imported yet"); return null; }

var parent = sibling.transform.parent;
// the toolkit objects sit at the scene root, so look there too, and keep only the first
UnityEngine.GameObject go = null;
foreach (var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (d.name != "UI Toolkit Connection") continue;
    if (go == null) go = d.gameObject;
    else UnityEditor.Undo.DestroyObjectImmediate(d.gameObject);
}
var created = go == null;
if (created)
{
    go = new UnityEngine.GameObject("UI Toolkit Connection");
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Connection status");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(sibling.transform.GetSiblingIndex());
}

var doc = go.GetComponent<UnityEngine.UIElements.UIDocument>() ?? go.AddComponent<UnityEngine.UIElements.UIDocument>();
doc.panelSettings = sibling.panelSettings;
doc.visualTreeAsset = uxml;
doc.sortingOrder = 28.5f;
if (go.GetComponent<Shinobytes.UI.ConnectionStatusView>() == null) go.AddComponent<Shinobytes.UI.ConnectionStatusView>();

var removed = new System.Collections.Generic.List<string>();
foreach (var old in UnityEngine.Object.FindObjectsByType<RavenBotConnectionUI>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    var so = new UnityEditor.SerializedObject(old);
    foreach (var field in new[] { "statusImage", "twitchDisconnected" })
    {
        var image = so.FindProperty(field).objectReferenceValue as UnityEngine.Component;
        // only the image's own object, and only if it holds nothing else of note
        if (image != null && image.gameObject != old.gameObject && image.transform.childCount == 0)
        {
            removed.Add(image.gameObject.name);
            UnityEditor.Undo.DestroyObjectImmediate(image.gameObject);
        }
    }
    removed.Add(old.gameObject.name + "." + nameof(RavenBotConnectionUI));
    UnityEditor.Undo.DestroyObjectImmediate(old);
}

// the old server banner, a red band across the bottom edge
foreach (var old in UnityEngine.Object.FindObjectsByType<ConnectionChecker>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (old.Graphics != null && old.Graphics != old.gameObject)
    {
        removed.Add(old.Graphics.name);
        UnityEditor.Undo.DestroyObjectImmediate(old.Graphics);
    }
    removed.Add(old.gameObject.name + "." + nameof(ConnectionChecker));
    UnityEditor.Undo.DestroyObjectImmediate(old);
}

UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append(created ? "created " : "updated ").Append(go.name).Append(", order ").Append(doc.sortingOrder)
  .Append(", removed ").Append(removed.Count == 0 ? "nothing" : string.Join(", ", removed));
UnityEngine.Debug.Log(sb.ToString());
return null;
