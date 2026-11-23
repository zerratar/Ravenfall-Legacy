// Run with: unity command eval_file --file tools/dark-fortress/dark-fortress-templates.cs (edit mode)
//
// Puts the Dark Fortress enemies (tools/dark-fortress/dark-fortress-enemies.cs) into the tower layouts' hidden
// templates, as prefab instances, so RavenNest.Combat.Tower's floors that name them ("df-undead", "df-behemoth")
// find their looks: the crypt (floors 4, 7, 9, 10) and the marsh (3, 8). Each scene is opened next to the open one,
// changed, saved and closed; the open scene is not touched. A template of the same name is replaced, so it can run
// again. The crypt and marsh builders add the same templates when they rebuild their scenes.
var log = new System.Text.StringBuilder();
var jobs = new[]
{
    ("Assets/Scenes/Tower/Tower_Crypt.unity", new[] { "df-undead", "df-undead-soldier", "df-corpse", "df-wraith", "df-grave-knight", "df-behemoth" }),
    ("Assets/Scenes/Tower/Tower_Marsh.unity", new[] { "df-plague" }),
};
foreach (var (scenePath, ids) in jobs)
{
    var already = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
    var wasOpen = already.IsValid() && already.isLoaded;
    var scene = wasOpen ? already : UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    TowerFloor floor = null;
    foreach (var root in scene.GetRootGameObjects())
    {
        floor = root.GetComponentInChildren<TowerFloor>(true);
        if (floor) break;
    }
    if (!floor || !floor.Templates) { log.AppendLine(scenePath + ": no TowerFloor templates"); if (!wasOpen) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); continue; }
    foreach (var id in ids)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Enemies/DarkFortress/" + id + ".prefab");
        if (!prefab) { log.AppendLine("no prefab " + id); continue; }
        for (var i = floor.Templates.childCount - 1; i >= 0; i--)
            if (floor.Templates.GetChild(i).name == id) UnityEngine.Object.DestroyImmediate(floor.Templates.GetChild(i).gameObject);
        var copy = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
        copy.name = id;
        copy.transform.SetParent(floor.Templates, false);
        copy.transform.localPosition = UnityEngine.Vector3.zero;
        copy.transform.localRotation = UnityEngine.Quaternion.identity;
        copy.SetActive(true);
        log.AppendLine(System.IO.Path.GetFileNameWithoutExtension(scenePath) + ": " + id);
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    if (!wasOpen) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
return log.ToString();
