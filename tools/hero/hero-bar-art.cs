// Run with: unity command eval_file tools/hero/hero-bar-art.cs
//
// Paints the hero skill bar's art after the user's first HUD reference (docs/mockups/hero-mode-hud.jpg): the carved plate
// behind the bar and the gold frame round each slot, with the minimap frame's polished gold so the HUD stays one piece.
// Into Temp/ImageGen/herobar; copy them to Assets/World/Hero/Resources/HeroUI/ (bar-plate.png, slot-frame.png) after.
// Pictures already there are skipped. Every picture costs money: never run it twice while jobs are running.
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
var running = ImageGen.Jobs.Where(j => j.Tag != null && j.Tag.StartsWith("herobar:") && j.State == ImageGen.JobState.Running).Select(j => j.Tag).ToList();
if (running.Count > 0) return "already running: " + string.Join(", ", running);
var gold = "Assets/World/Hero/Resources/HeroUI/minimap-frame.png";
var started = new System.Collections.Generic.List<string>();
void Paint(string name, string reference, string size, string prompt)
{
    var path = "Temp/ImageGen/herobar/" + name + ".png";
    if (System.IO.File.Exists(path)) return;
    var options = new ImageGen.Options { Model = ImageGen.Model.Sunburst, Transparent = true, AutoCutout = true, Quality = "high", Size = size };
    ImageGen.Edit(prompt, new[] { reference, gold }, path, options, null, null, "herobar:" + name);
    started.Add(name);
}
Paint("bar-plate", "Temp/ImageGen/herobar/ref-bar.png", "1536x512",
    "A game user interface asset: only the long ornate hotbar plate from the first image, on its own, without the round orbs, the skill icons and the number tabs. " +
    "A dark carved panel, near black brown, framed by a border of carved gold filigree scrollwork in the polished gold of the second image, with a small " +
    "ornate gold crest at the top centre and curled scroll ornaments at both ends. Seen straight on, perfectly symmetrical, the inside of the panel plain " +
    "and dark and empty so slots can sit on it. Transparent background outside the plate, no shadow, no text, no letters, no numbers.");
Paint("slot-frame", "Temp/ImageGen/herobar/ref-slot.png", "1024x1024",
    "A game user interface asset: one square skill slot frame like the one in the first image, on its own: a bevelled square frame of polished gold like " +
    "the second image, with small carved filigree ornaments at the four corners and a thin dark inner edge. The inside of the square is completely empty and " +
    "transparent, a clean square hole. Seen straight on, perfectly symmetrical, transparent background, no icon, no text, no letters, no numbers.");
return "started " + started.Count + ": " + string.Join(", ", started);
