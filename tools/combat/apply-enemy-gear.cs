// Run with: unity command eval_file --file tools/combat/apply-enemy-gear.cs   (edit mode)
//
// Combat v2 enemy gear (docs/combat-v2-design.md, Progress): sets Power, Accuracy and Armor on each enemy definition
// in Assets/World/Enemies from tools/combat/enemy-gear.json, which the combat simulator writes
// (RavenNest src/tools/RavenNest.CombatSimulator: "enemygear [fights] [armour share] [offence share]"; the values in
// use came from "enemygear 500 0.75 0.25": 75% of the armour and 25% of the power and accuracy of the standard melee
// kit at each enemy's own combat level). The result is also written to Temp/apply-enemy-gear.txt.
var root = System.IO.Directory.GetCurrentDirectory();
var log = new System.Text.StringBuilder();
try
{
    var entries = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "tools/combat/enemy-gear.json")));
    foreach (var e in entries)
    {
        var id = (string)e["Id"];
        var path = "Assets/World/Enemies/" + id + ".asset";
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(path);
        if (!asset || asset.Definition == null) { log.AppendLine("missing " + path); continue; }
        asset.Definition.Power = (int)e["Power"];
        asset.Definition.Accuracy = (int)e["Accuracy"];
        asset.Definition.Armor = (int)e["Armor"];
        UnityEditor.EditorUtility.SetDirty(asset);
        log.AppendLine(id + ": power " + asset.Definition.Power + ", accuracy " + asset.Definition.Accuracy + ", armour " + asset.Definition.Armor);
    }
    UnityEditor.AssetDatabase.SaveAssets();
}
catch (System.Exception ex)
{
    log.AppendLine("failed: " + ex);
}
System.IO.File.WriteAllText(System.IO.Path.Combine(root, "Temp/apply-enemy-gear.txt"), log.ToString());
return log.ToString();
