// Run with: unity command eval_file --file tools/ravebridge/intro-pictures.cs
//
// Draws the Ravebridge introduction's pictures (RavebridgeIntro, one per page) with the Image Generator (Ravenfall >
// Image Generator, the user's key) into Temp/ImageGen/rbintro, in the quest window's style (tools/quests/quest-ui-pictures.cs).
// They replace the first pass, which were screenshots (the user, 2026-11-03: "looks quite bad"). Pictures already there
// are skipped; delete one to draw it again. Copy them as JPGs into Assets/World/Hero/Resources/HeroUI/intro-N.jpg after.
// Set Only to a few names to draw just those.
var Only = new string[0];
const string Style = "Stylized low-poly 3D game art in the style of a cozy fantasy village game, soft warm lighting, chunky simple shapes, " +
    "clean flat-shaded polygons, gentle ambient occlusion, rich but not garish colours, no text, no letters, no numbers, no user interface, no border, no frame. ";
const string Wide = "Wide cinematic composition, the subject in the middle third so the edges can be cropped, a clear readable silhouette. ";
var pictures = new System.Collections.Generic.List<(string Name, string Prompt)>
{
    ("intro-1", "A lone adventurer hero in simple armour with a sword and a round shield, seen from behind and a little to the side, standing on a grassy hilltop looking out over a small fantasy island: a stone castle with walls and towers, red-roofed cottages, a turquoise river with a stone bridge, a windmill, the sea all round, golden morning light."),
    ("intro-2", "An adventurer hero diving into a forward roll on a cobblestone courtyard, a dust trail behind them, just as a green goblin's crude club smashes the stones where the hero stood a moment ago, a second goblin running in, a castle wall with blue banners behind."),
    ("intro-3", "An adventurer hero swinging a sword in a wide spinning strike that leaves a glowing golden arc of light, three green goblins knocked back round them, a small red health potion flask glowing on the hero's belt and faint blue magic sparks round their free hand, a castle courtyard at dusk."),
    ("intro-4", "An adventurer hero talking to a friendly castle guard captain with a steel helmet and a big moustache in front of a castle gate, a large glowing golden exclamation mark floating above the captain's head, banners and a lantern, warm afternoon light."),
    ("intro-5", "An old parchment map of a small island spread on a wooden table, a dotted path and a red X on it, a brass compass, a lit candle, and an open leather adventurer's bag spilling loot beside it: gold coins, a sword, a small shield, a ring and a red potion. The map has drawings only, no writing."),
    ("intro-6", "An adventurer hero leading a cheering crowd of many small colourful adventurers in different outfits charging across a stone castle bridge at a huge horned monster, one of them healing the hero with a soft green glow, another holding up a war banner, weapons raised, dramatic dusk sky."),
};
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
System.IO.Directory.CreateDirectory("Temp/ImageGen/rbintro");
var started = new System.Collections.Generic.List<string>();
foreach (var p in pictures)
{
    if (Only.Length > 0 && System.Array.IndexOf(Only, p.Name) < 0) continue;
    var path = "Temp/ImageGen/rbintro/" + p.Name + ".png";
    if (System.IO.File.Exists(path)) continue;
    var options = new ImageGen.Options { Transparent = false, AutoCutout = false, Quality = "high", Size = "1792x864" };
    ImageGen.Generate(Style + Wide + p.Prompt, path, options, null, "rbintro:" + p.Name);
    started.Add(p.Name);
}
return "started " + started.Count + ": " + string.Join(", ", started);
