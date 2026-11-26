// Opens a tower scene (path in %TEMP%/tower-audit-scene.txt) additively in edit mode and checks that everything in it can
// be walked to from where players arrive: monster spots, the boss spot (and its room), people, rest spots, strolls,
// work spots and every road point. Writes %TEMP%/tower-audit.txt; closes the scene after (unless it was open already).
// The checks themselves are TowerAudit (Assets/Scripts/Editor/TowerPlaces/TowerAudit.cs), which Ravenfall > Tower
// Places' Check runs too.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
var path = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower-audit-scene.txt")).Trim();
try
{
    var open = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    var wasOpen = open.IsValid() && open.isLoaded;
    var scene = wasOpen ? open : UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    log.Append(TowerAudit.Run(scene).Report());
    if (!wasOpen) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-audit.txt"), log.ToString());
return null;
