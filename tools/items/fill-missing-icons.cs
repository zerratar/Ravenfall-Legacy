// Run with: unity command eval_file --file tools/items/fill-missing-icons.cs
//
// Gives every item in the catalog (Data/Repositories/items.json) an icon in Assets/Resources/Items: any item without
// one gets the website's picture (RavenNest Publish/wwwroot/imgs/items/<slug>.png), imported as a sprite like the
// rest. Run it before a release build; items the website has no picture for are listed (make those by hand). The
// game also fetches a missing icon from the website at runtime (EquipmentSlot.GetItemSprite), for items newer than
// the build. Assets/Resources is not versioned, so the icons live on the build machine.
var website = @"G:\Ravenfall\Projects\RavenNest\Publish\wwwroot\imgs\items\";
var folder = "Assets/Resources/Items/";
var items = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Data/Repositories/items.json"));
var copied = new System.Collections.Generic.List<string>();
var nowhere = new System.Collections.Generic.List<string>();
foreach (var item in items)
{
    var name = (string)item["name"];
    if (string.IsNullOrEmpty(name)) continue;
    var slug = name.Replace(" ", "-").Replace("'", "").ToLower();
    if (System.IO.File.Exists(folder + slug + ".png")) continue;
    var source = website + slug + ".png";
    if (!System.IO.File.Exists(source)) { nowhere.Add(name); continue; }
    System.IO.File.Copy(source, folder + slug + ".png");
    copied.Add(slug);
}
UnityEditor.AssetDatabase.Refresh();
// imported as sprites (a few at a time per run stays under the editor's time limit; run again if it says so)
var started = System.DateTime.UtcNow;
var left = 0;
foreach (var path in System.IO.Directory.GetFiles(folder, "*.png"))
{
    var importer = UnityEditor.AssetImporter.GetAtPath(path.Replace('\\', '/')) as UnityEditor.TextureImporter;
    if (importer == null || importer.textureType == UnityEditor.TextureImporterType.Sprite) continue;
    if ((System.DateTime.UtcNow - started).TotalSeconds > 3.5) { left++; continue; }
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.alphaIsTransparency = true;
    importer.mipmapEnabled = false;
    importer.SaveAndReimport();
}
return copied.Count + " icons copied from the website" + (left > 0 ? ", " + left + " still to import as sprites (run again)" : "") + (nowhere.Count > 0 ? "; no picture anywhere for: " + string.Join(", ", nowhere) : "");
