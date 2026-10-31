// Run with: unity command eval_file --file tools/quests/quest-ui-install.cs, after tools/quests/quest-ui-pictures.cs
//
// Shrinks the quest window's pictures from Temp/ImageGen/questui into Assets/World/Quests/Resources/QuestUI (QuestArt
// loads them from there): the card scenes, stories and the empty board at 512, the village at 256, the small icons at
// 128. Imported for UI: no mipmaps, clamped, alpha as transparency.
const string From = "Temp/ImageGen/questui";
const string To = "Assets/World/Quests/Resources/QuestUI";
if (!System.IO.Directory.Exists(From)) return "nothing in " + From + ": run quest-ui-pictures.cs first";
System.IO.Directory.CreateDirectory(To);
var done = new System.Collections.Generic.List<string>();
foreach (var file in System.IO.Directory.GetFiles(From, "*.png"))
{
    var name = System.IO.Path.GetFileNameWithoutExtension(file);
    var size = name.StartsWith("icon-") ? 128 : name == "village-houses" ? 256 : 512;
    System.IO.File.WriteAllBytes(To + "/" + name + ".png", IconForge.Shrink(System.IO.File.ReadAllBytes(file), size));
    done.Add(name);
}
UnityEditor.AssetDatabase.Refresh();
foreach (var name in done)
{
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(To + "/" + name + ".png");
    if (importer == null) continue;
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    importer.SaveAndReimport();
}
return "installed " + done.Count + ": " + string.Join(", ", done);
