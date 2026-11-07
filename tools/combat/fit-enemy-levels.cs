// Run with: unity command eval_file --file tools/combat/fit-enemy-levels.cs   (edit mode)
//
// Combat v2: enemies whose combat level sat above their fighting area's requirement, so a player at the requirement
// lost most fights there (the simulator, 2026-11-07). Each kind's skills are scaled so its combat level lands on the
// target below (the area's level, a little above it for the tougher ones). Run apply-enemy-gear.cs after the
// simulator's enemygear for the new levels. Every island from Kyojin up already keeps its enemies at or below the
// requirement; Ravebridge takes any level of character, and maxed ones beat these whatever their level.
var targets = new System.Collections.Generic.Dictionary<string, int>
{
    ["ravebridge-cow"] = 2,              // Lv. 1 (cows)
    ["ravebridge-goblin"] = 5,           // Lv. 5
    ["ravebridge-skeleton"] = 10,        // Lv. 10, "the level 10 group" Father Aereck speaks of
    ["ravebridge-tormented-soul"] = 15,  // Lv. 15
    ["mangrove-pirate"] = 3,             // Lv. 1
    ["mangrove-pirate-captain"] = 6,     // Lv. 1, the tougher one
    ["home-metalon-purple"] = 40,        // Lv. 40 (red stays 32)
    ["home-metalon-green"] = 45,
    ["ironhill-dragon"] = 100,           // Lv. 100
    ["ironhill-dragon-blue"] = 100,
    ["ironhill-dragon-green"] = 100,
    ["ironhill-dragon-purple"] = 100,
    ["ironhill-dragon-red"] = 100,
    ["ironhill-dragon-white"] = 100,
    ["ironhill-lord-quackers"] = 115,    // the tougher one
};
var log = new System.Text.StringBuilder();
foreach (var (id, target) in targets)
{
    var path = "Assets/World/Enemies/" + id + ".asset";
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(path);
    if (!asset || asset.Definition == null) { log.AppendLine("missing " + path); continue; }
    var d = asset.Definition;
    var before = d.CombatLevel;
    // scale toward the target, then nudge Health (it counts a quarter) until the level lands
    var f = (double)target / System.Math.Max(1, before);
    int S(int v) => System.Math.Max(1, (int)System.Math.Round(v * f));
    d.Attack = S(d.Attack); d.Strength = S(d.Strength); d.Defense = S(d.Defense); d.Health = System.Math.Max(10, S(d.Health));
    d.Ranged = S(d.Ranged); d.Magic = S(d.Magic);
    for (var i = 0; i < 400 && d.CombatLevel < target; ++i) d.Health++;
    for (var i = 0; i < 400 && d.CombatLevel > target && d.Health > 10; ++i) d.Health--;
    for (var i = 0; i < 400 && d.CombatLevel > target && (d.Attack > 1 || d.Strength > 1 || d.Defense > 1); ++i) { d.Attack = System.Math.Max(1, d.Attack - 1); d.Strength = System.Math.Max(1, d.Strength - 1); d.Defense = System.Math.Max(1, d.Defense - 1); }
    UnityEditor.EditorUtility.SetDirty(asset);
    log.AppendLine(id + ": combat level " + before + " -> " + d.CombatLevel + " (A/S/D/H " + d.Attack + "/" + d.Strength + "/" + d.Defense + "/" + d.Health + ")");
}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText("Temp/fit-enemy-levels.txt", log.ToString());
return log.ToString();
