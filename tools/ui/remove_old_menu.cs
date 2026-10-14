// Deletes the old uGUI game menu and settings hierarchy: every child of the "Game Menu" object
// (Panel, Main, Players, Settings, RaidAndExit). The Game Menu object itself stays, switched off, as
// the home of GameMenuHandler. An offline scan of the scene file found nothing outside the hierarchy
// pointing into it apart from GameMenuHandler and GameManager, whose fields went with the old code.
// Edit mode only. Run with the Unity CLI eval_file, then save the scene.
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("OLDMENU|refused in play mode"); return null; }
var menu = UnityEngine.Object.FindFirstObjectByType<GameMenuHandler>(UnityEngine.FindObjectsInactive.Include);
if (!menu) { UnityEngine.Debug.Log("OLDMENU|no GameMenuHandler"); return null; }
var names = new System.Collections.Generic.List<string>();
var count = 0;
for (var i = menu.transform.childCount - 1; i >= 0; --i)
{
    var child = menu.transform.GetChild(i).gameObject;
    names.Add(child.name);
    count += child.GetComponentsInChildren<UnityEngine.Transform>(true).Length;
    UnityEditor.Undo.DestroyObjectImmediate(child);
}
// components left on the Game Menu object besides its transform and the handler
var extra = new System.Collections.Generic.List<string>();
foreach (var c in menu.GetComponents<UnityEngine.Component>())
    if (!(c is UnityEngine.Transform) && !(c is GameMenuHandler)) extra.Add(c ? c.GetType().Name : "missing script");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
UnityEngine.Debug.Log("OLDMENU|deleted " + string.Join(", ", names) + " (" + count + " objects); left on Game Menu: " + (extra.Count > 0 ? string.Join(", ", extra) : "nothing else"));
return null;
