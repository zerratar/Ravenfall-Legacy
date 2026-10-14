using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor tools for the language files, see Languages.cs.</summary>
public static class LocalizationMenu
{
    [MenuItem("Ravenfall/Localization/Export template")]
    private static void ExportTemplate()
    {
        Directory.CreateDirectory(Languages.Folder);
        var path = Path.Combine(Languages.Folder, "_template.json");
        File.WriteAllText(path, Languages.ExportTemplate());
        AssetDatabase.Refresh();
        Debug.Log("Language template written to " + path);
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Ravenfall/Localization/Reload language files")]
    private static void Reload()
    {
        Languages.Rescan();
        Languages.Apply(PlayerSettings.Instance.Language ?? Languages.English);
        Debug.Log("Languages: " + string.Join(", ", System.Linq.Enumerable.Select(Languages.Available, a => a.Code)) + "; now using " + Languages.Current);
    }
}
