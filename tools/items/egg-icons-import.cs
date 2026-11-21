// Step three for the egg icons (after egg-renders.cs and egg-icons.py): the new "<egg name>.png" files in
// Assets/Resources/Items are imported as sprites, with the settings of an egg icon that is there already. The game
// loads item icons with Resources.Load<Sprite>("Items/<slug>"), and a png nobody imported as a sprite is no sprite.
//
//     unity command eval_file --file tools/items/egg-icons-import.cs      (edit mode)
//     (wait for %TEMP%/egg-icons-import.txt to end with "done")
//
// One icon per editor frame. Assets/Resources is not in git: the icons and their import settings live on this machine
// and the build machine, so this runs wherever egg-icons.py has written new ones.
var folder = "Assets/Resources/Items/";
var report = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "egg-icons-import.txt");
var todo = new System.Collections.Generic.List<string>();
foreach (var s in RavenNest.Combat.PetEggs.Species)
{
    var path = folder + s.EggName.Replace(" ", "-").Replace("'", "").ToLowerInvariant() + ".png";
    if (System.IO.File.Exists(path)) todo.Add(path);
}
var like = UnityEditor.AssetImporter.GetAtPath(folder + "wolf-pup-egg.png") as UnityEditor.TextureImporter;
var settings = new UnityEditor.TextureImporterSettings();
if (like) like.ReadTextureSettings(settings);
var log = new System.Text.StringBuilder();
var index = 0;
var imported = 0;
System.IO.File.WriteAllText(report, "started\n");
UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    if (index < todo.Count)
    {
        var path = todo[index++];
        try
        {
            if (!UnityEditor.AssetImporter.GetAtPath(path)) UnityEditor.AssetDatabase.ImportAsset(path);
            var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (!importer) { log.AppendLine("NOT IMPORTED " + path); return; }
            if (importer.textureType == UnityEditor.TextureImporterType.Sprite && UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path)) return; // one from before
            if (like)
            {
                importer.SetTextureSettings(settings);
                importer.SetPlatformTextureSettings(like.GetDefaultPlatformTextureSettings());
            }
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            imported++;
            log.AppendLine((UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path) ? "sprite " : "NO SPRITE ") + path);
        }
        catch (System.Exception e) { log.AppendLine("ERROR " + path + ": " + e.Message); }
        System.IO.File.WriteAllText(report, log.ToString());
        return;
    }
    UnityEditor.EditorApplication.update -= step;
    var missing = new System.Collections.Generic.List<string>();
    foreach (var s in RavenNest.Combat.PetEggs.Species)
        if (!UnityEngine.Resources.Load<UnityEngine.Sprite>("Items/" + s.EggName.Replace(" ", "-").Replace("'", "").ToLowerInvariant())) missing.Add(s.EggName);
    log.AppendLine(imported + " imported as sprites; eggs with no icon the game can load: " + (missing.Count == 0 ? "none" : string.Join(", ", missing)));
    log.AppendLine("done");
    System.IO.File.WriteAllText(report, log.ToString());
};
UnityEditor.EditorApplication.update += step;
return todo.Count + " egg icons to look at, report in " + report;
