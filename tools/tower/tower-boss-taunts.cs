// Makes Assets/Resources/Tower/Boss Taunts.asset (TowerBossTaunts): the clip each tower boss model plays in its intro.
// Run with `unity command eval_file` in edit mode. Rerun after adding a boss model; a model left out winds up its attack.
System.Func<string, string, UnityEngine.AnimationClip> Clip = (path, name) =>
    UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.AnimationClip>()
        .FirstOrDefault(c => !c.name.StartsWith("__preview") && (name == null || c.name == name));
var cheer1 = Clip("Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Cheer1.FBX", null);
var cheer2 = Clip("Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Cheer2.FBX", null);
var cheer3 = Clip("Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Cheer3.FBX", null);
var unka = "Assets/Malbers Animations/Dragons/4 - Unka the Dragon/Animations/Unka Attack.FBX";
var entries = new (string Model, UnityEngine.AnimationClip Clip, float Speed)[]
{
    // stands up tall and looks the players over
    ("home-bunny", Clip("Assets/Low Poly Animated Animals/- Meshes/Animals/Rabbit/Rabbit_LookOut.anim", null), 1f),
    ("ravebridge-goblin", cheer1, 1f),
    ("ravebridge-skeleton", cheer3, 1f),
    ("ravebridge-tormented-soul", cheer2, 0.8f),
    ("iceheim-viking", cheer1, 1f),
    ("iceheim-viking-chief", cheer3, 1f),
    ("kyojin-samurai", cheer2, 1f),
    // rears up and casts
    ("home-metalon-green", Clip("Assets/Polygonal Metalon/FBX/Polygonal Metalon@Cast Spell.FBX", null), 0.5f),
    ("home-metalon-purple", Clip("Assets/Polygonal Metalon/FBX/Polygonal Metalon@Cast Spell.FBX", null), 0.5f),
    ("home-metalon-red", Clip("Assets/Polygonal Metalon/FBX/Polygonal Metalon@Cast Spell.FBX", null), 0.5f),
    // the Moo-tant is not impressed: it goes on eating
    ("home-cow", Clip("Assets/Low Poly Animated Animals/- Meshes/Animals/Cow/Cow_Eating.anim", null), 1f),
    // a roar of fire
    ("ironhill-dragon-green", Clip(unka, "UAttack FireBreath L"), 1f),
    ("ironhill-dragon-white", Clip(unka, "UAttack FireBreath L"), 1f),
    // a big duck flaps its wings
    ("ironhill-lord-quackers", Clip(unka, "UAttack Wings R"), 1f),
    // the Storm Spire (band 3): the captain, the ninja, the champion and the warden cheer; the drakes beat their wings and roar
    ("away-pirate", cheer1, 1f),
    ("away-pirate-captain", cheer3, 1f),
    ("mangrove-pirate-captain", cheer3, 1f),
    ("kyojin-ninja", cheer2, 1.1f),
    ("atria-gladiator", cheer1, 1f),
    ("eldara-elven-soldier", cheer2, 0.9f),
    ("ironhill-dragon-purple", Clip(unka, "UAttack Wings R"), 1f),
    ("ironhill-dragon-blue", Clip(unka, "UAttack FireBreath L"), 1f),
};
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Tower")) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Tower");
var path = "Assets/Resources/Tower/Boss Taunts.asset";
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TowerBossTaunts>(path);
if (!asset)
{
    asset = UnityEngine.ScriptableObject.CreateInstance<TowerBossTaunts>();
    UnityEditor.AssetDatabase.CreateAsset(asset, path);
}
asset.Taunts = entries.Select(e => new TowerBossTaunts.Taunt { Model = e.Model, Clip = e.Clip, Speed = e.Speed }).ToArray();
UnityEditor.EditorUtility.SetDirty(asset);
UnityEditor.AssetDatabase.SaveAssets();
return string.Join(", ", entries.Select(e => e.Model + "=" + (e.Clip ? e.Clip.name + " " + e.Clip.length.ToString("0.0") + (e.Clip.isLooping ? " loop" : "") : "MISSING")));
