// Run with: unity command eval_file tools/tower/galewharf-concepts.cs
//
// Concept paintings for Galewharf (tower band 3's town, the user 2026-11-19: "give some time to think about how it should
// look"), three directions, in the style of the game's pictures. Into Temp/ImageGen/galewharf; skipped when there.
// Every picture costs money: never run it twice while jobs are running.
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
var running = ImageGen.Jobs.Where(j => j.Tag != null && j.Tag.StartsWith("galewharf:") && j.State == ImageGen.JobState.Running).Select(j => j.Tag).ToList();
if (running.Count > 0) return "already running: " + string.Join(", ", running);
const string Style = "Stylized low-poly 3D game art in the style of a cozy fantasy village game, chunky simple shapes, clean flat-shaded polygons, " +
    "gentle ambient occlusion, rich but not garish colours, no text, no letters, no numbers, no user interface, no border, no frame. ";
const string World = "A town of sky sailors high above a sea of white clouds, under a brooding storm sky with a few distant lightning bolts and shafts of silver light. ";
var concepts = new (string Name, string Prompt)[]
{
    ("concept-a-island", "Seen from above at a three-quarter angle like a strategy game camera. The whole town sits on one large floating island: its top is grassy " +
        "with a cobbled square and a giant anchor monument in the middle, cozy timber houses with blue-grey roofs, an inn with a crow's nest on its roof, a windmill " +
        "and a stone lighthouse on a rocky point. Its underside is a huge jagged inverted cone of layered grey rock with roots and small waterfalls spilling off the edges " +
        "into the clouds. Wooden piers stick out over the void with a tall sailing ship moored at the longest one. Behind the island a colossal ancient stone spire rises " +
        "out of the clouds into the storm, with a carved stair winding up its side and a stone bridge from the island to the stair."),
    ("concept-b-terraces", "Seen from a three-quarter angle. The town clings to the flank of a colossal ancient stone spire that rises out of the clouds: stone terraces " +
        "and wooden platforms stacked up the cliff, cozy timber houses with blue-grey roofs on every terrace, stairs and ladders between them, a windmill on a ledge, " +
        "lanterns, and wooden piers sticking out over the void at the bottom terrace with a tall sailing ship moored there."),
    ("concept-c-islets", "Seen from above at a three-quarter angle like a strategy game camera. The town is spread over a cluster of small floating rock islets joined by " +
        "sagging rope bridges and one stone arch bridge: the biggest islet holds a cobbled square with a giant anchor and cozy timber houses, another the inn, another " +
        "a windmill, another a stone lighthouse, the outer ones wooden docks with sailing ships moored. Each islet has a jagged rocky underside with dangling roots. A " +
        "colossal ancient stone spire rises behind them out of the clouds into the storm."),
};
System.IO.Directory.CreateDirectory("Temp/ImageGen/galewharf");
var started = new System.Collections.Generic.List<string>();
foreach (var c in concepts)
{
    var path = "Temp/ImageGen/galewharf/" + c.Name + ".png";
    if (System.IO.File.Exists(path)) continue;
    var options = new ImageGen.Options { Model = ImageGen.Model.Flare, Transparent = false, AutoCutout = false, Quality = "high", Size = "1536x1024" };
    ImageGen.Generate(Style + World + c.Prompt, path, options, null, "galewharf:" + c.Name);
    started.Add(c.Name);
}
return "started " + started.Count + ": " + string.Join(", ", started);
