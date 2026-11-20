// Run with: unity command eval_file tools/tower/tower-concepts.cs
//
// Concept paintings for every tower place (the user 2026-11-20: "see if every tower biome can get better with the same
// thing", as Galewharf did: concept art first, then the shapes in Blender). Each is painted over a capture of the place as
// it is (Temp/ImageGen/tower-concepts/input-<name>.png, the play mode survey), so the camera and the play area stay and
// only the look changes. Into Temp/ImageGen/tower-concepts; one is skipped when it is there already.
// Every picture costs money: never run it twice while jobs are running.
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
var running = ImageGen.Jobs.Where(j => j.Tag != null && j.Tag.StartsWith("tower-concept:") && j.State == ImageGen.JobState.Running).Select(j => j.Tag).ToList();
if (running.Count > 0) return "already running: " + string.Join(", ", running);
const string Ask = "Repaint this screenshot from a cozy low-poly fantasy game as polished concept art of the same place from the same camera. " +
    "Keep the camera angle, the paths and the open play area where the fights happen, and keep the game's chunky low-poly flat-shaded style " +
    "(simple shapes, clean polygons, gentle ambient occlusion, rich but not garish colours). Leave out every bit of user interface, text, letters, " +
    "numbers, borders and frames. Change the place's look like this: ";
var dir = "Temp/ImageGen/tower-concepts";
var concepts = new (string Name, string Prompt)[]
{
    ("overgrown", "It is a vast ruined hall inside an ancient tower, not an open field: tall broken stone walls with huge arched windows enclose the whole " +
        "area on every side so no horizon shows, the roof has fallen in and warm shafts of sunlight pour down through the gaps, giant tree roots crawl over the " +
        "walls and floor, moss and ferns everywhere, toppled pillars and statues, old flagstones showing through the grass along the paths. Light mist only near the floor."),
    ("marsh", "It is a flooded hall inside an ancient tower, a drowned marsh: tall crumbling stone walls with arched windows enclose the area on every side, " +
        "dark still water with lily pads and reeds, twisted mangrove-like roots arching out of the water, little wooden boardwalks and stepping stones, " +
        "glowing green fireflies and mushrooms, broken columns standing in the water, a greenish haze low over the water but the place clearly visible."),
    ("crypt", "It is a crypt deep under the tower, a real underground place: the floor is enclosed by rough rock and earthen cave walls rising into darkness, " +
        "a low vaulted rock ceiling with thick tree roots hanging down through it, no sky anywhere, stone sarcophagi and bone piles, cobwebs, warm torchlight pools " +
        "and faint teal glowing runes on the floor, deep shadows at the edges."),
    ("frostvault", "It is a vault carved inside a glacier: walls of translucent blue ice and frosted rock enclose the floor on every side and rise into a ceiling " +
        "of icicles, no sky anywhere, frozen pillars, bones and old weapons frozen inside the ice walls, glowing cyan runes, cold blue light with a few warm braziers, " +
        "drifts of snow in the corners."),
    ("glacier", "It is a snowy glacier valley under a crisp blue sky: tall glacier walls of layered blue ice and grey rock with crevasses ring the valley, " +
        "a frozen pond, snow-laden pine trees, a palisade gate and a few viking tents, icicles hanging off rock ledges, wind-blown snow, warm braziers along the path."),
    ("skydocks", "It is a harbour of planks and rope on a floating rock island high above a sea of fluffy white clouds: wooden docks and piers stick out over " +
        "the edge with sailing ships moored, cranes and cargo, the island's rim is jagged layered grey rock (not round boulders) with roots, and smaller floating " +
        "rocks drift nearby joined by rope bridges, under a stormy sky."),
    ("stormspire", "It is the open top of a colossal stone spire above the clouds, in a storm: a ring of tall ruined stone walls and arches with lightning rods " +
        "on top encloses a paved arena, glowing blue runes in the floor, crackling lightning between metal rods, statues of storm guardians, banners whipping in " +
        "the wind, dark storm clouds above and bright white clouds below."),
    ("mossgate", "It is a cozy forest town at the foot of a giant tower: mossy thatched and timber cottages, a well in the cobbled square, flower gardens, " +
        "lanterns, a huge ancient tower gate in the background, big oak trees all round so the forest closes in, golden afternoon light and light haze only far away."),
    ("rimeholt", "It is a snug viking town in a snowy valley: log longhouses with thick snow on the roofs and warm glowing windows, smoke from the chimneys, " +
        "a cobbled square with a big fire pit, wooden palisade, carved totem posts, snow-laden pines, mountains behind, warm lights against the cold blue snow."),
};
var started = new System.Collections.Generic.List<string>();
foreach (var c in concepts)
{
    var input = dir + "/input-" + c.Name + ".png";
    var path = dir + "/concept-" + c.Name + ".png";
    if (System.IO.File.Exists(path) || !System.IO.File.Exists(input)) continue;
    var options = new ImageGen.Options { Model = ImageGen.Model.Sunburst, Transparent = false, AutoCutout = false, Quality = "high", Size = "1536x1024" };
    ImageGen.Edit(Ask + c.Prompt, new[] { input }, path, options, null, null, "tower-concept:" + c.Name);
    started.Add(c.Name);
}
return "started " + started.Count + ": " + string.Join(", ", started);
