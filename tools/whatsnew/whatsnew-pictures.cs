// Run with: unity command eval_file tools/whatsnew/whatsnew-pictures.cs
//
// Draws the What's new pictures (Shinobytes.UI.WhatsNew, one per page) with the Image Generator (Ravenfall > Image
// Generator, the user's key) into Temp/ImageGen/whatsnew, in the style of Ravebridge's introduction
// (tools/ravebridge/intro-pictures.cs). Pictures already there are skipped; delete one to draw it again. Copy them as
// JPGs into Assets/World/WhatsNew/Resources/WhatsNew/whatsnew-N.jpg after (tools/whatsnew/README.md).
// Set Only to a few names to draw just those. Every picture costs money: never run it twice while jobs are running.
var Only = new string[0];
const string Style = "Stylized low-poly 3D game art in the style of a cozy fantasy village game, soft warm lighting, chunky simple shapes, " +
    "clean flat-shaded polygons, gentle ambient occlusion, rich but not garish colours, no text, no letters, no numbers, no user interface, no border, no frame. ";
const string Wide = "Wide cinematic composition, the subject in the middle third so the edges can be cropped, a clear readable silhouette. ";
var pictures = new System.Collections.Generic.List<(string Name, string Prompt)>
{
    ("whatsnew-1", "A small band of colourful adventurers, a knight with a shield, an archer and a mage, fighting a towering crowned skeleton king in an overgrown stone ruin with vine-covered pillars; one sword trails fire, an arrow crackles with lightning, the mage's staff throws frost; dramatic golden evening light."),
    ("whatsnew-2", "A sturdy adventurer in plate armour and a round shield bracing against a green goblin's heavy club blow, the club glancing off the shield in a burst of sparks, a soft warm red glow of vitality around the adventurer, a grassy village square with wooden houses behind."),
    ("whatsnew-3", "Four adventurers standing back to back in a circle, each holding a weapon of a different element: a sword wreathed in flames, an axe coated in glittering ice and frost, a spear crackling with blue lightning, and a dagger dripping glowing green poison, the elements lighting their faces, a dark forest clearing at dusk."),
    ("whatsnew-4", "An adventurer training against a wooden practice dummy in a quiet courtyard at night, above them in the sky a glowing constellation shaped like a branching skill tree, stars joined by golden lines, a few stars brightly lit and the rest faint, a lantern and a weapon rack nearby."),
    ("whatsnew-5", "An adventurer fighting side by side with two loyal pet companions, a small armoured purple beetle creature with glowing spikes and a fluffy fox cub, against a big green slime in a sunny meadow; at the adventurer's feet a speckled egg with a crack of light in its shell."),
    ("whatsnew-6", "A sword lying on a jeweller's wooden workbench with an empty glowing socket in its crossguard, beside it cut gems sparkling in the lamplight: a red ruby, a blue sapphire, a green emerald, a yellow topaz and a purple amethyst, a pair of tweezers, a small anvil and a clan banner on the wall behind."),
    ("whatsnew-7", "A gigantic ancient stone tower with moss and vines rising far up into the clouds and haze, at its foot a cozy mossy village with crooked wooden houses and warm lit windows, a long line of tiny adventurers walking along a winding road toward the tower's great gate, soft misty morning light."),
};
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
var running = ImageGen.Jobs.Where(j => j.Tag != null && j.Tag.StartsWith("whatsnew:") && j.State == ImageGen.JobState.Running).Select(j => j.Tag).ToList();
if (running.Count > 0) return "already running: " + string.Join(", ", running);
System.IO.Directory.CreateDirectory("Temp/ImageGen/whatsnew");
var started = new System.Collections.Generic.List<string>();
foreach (var p in pictures)
{
    if (Only.Length > 0 && System.Array.IndexOf(Only, p.Name) < 0) continue;
    var path = "Temp/ImageGen/whatsnew/" + p.Name + ".png";
    if (System.IO.File.Exists(path)) continue;
    var options = new ImageGen.Options { Transparent = false, AutoCutout = false, Quality = "high", Size = "1792x864" };
    ImageGen.Generate(Style + Wide + p.Prompt, path, options, null, "whatsnew:" + p.Name);
    started.Add(p.Name);
}
return "started " + started.Count + ": " + string.Join(", ", started);
