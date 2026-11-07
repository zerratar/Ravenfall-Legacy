using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEngine;

/// <summary>
///     Writes every enemy definition (Assets/World/Enemies) to the combat simulator in the RavenNest repo
///     (src/tools/RavenNest.CombatSimulator/data/enemies.json), so it fights the same enemies as the game. Run it after
///     changing a definition, then run the simulator's "enemies" command.
/// </summary>
public static class EnemyDefinitionExport
{
    private const string SimulatorData = "../RavenNest/src/tools/RavenNest.CombatSimulator/data/enemies.json";

    [MenuItem("Ravenfall/Combat/Export Enemy Definitions To Simulator")]
    public static void Export()
    {
        var definitions = AssetDatabase.FindAssets("t:EnemyDefinitionAsset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset && asset.Definition != null)
            .Select(asset => asset.Definition)
            .OrderBy(d => d.CombatLevel).ThenBy(d => d.Id)
            .ToList();

        var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), SimulatorData));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var json = JsonConvert.SerializeObject(definitions, Formatting.Indented, new StringEnumConverter());
        File.WriteAllText(path, json);
        Debug.Log($"Exported {definitions.Count} enemy definitions to {path}");
    }
}
