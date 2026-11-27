// Redraws whatsnew-8 with the real class gear as a reference (2026-11-27): Temp/ImageGen/whatsnew/ref-class-gear.png is the website 3D figure in
// Starcaller, Aegis and Windrunner (/dev/gear-preview?wear=<material>&bare=1, the canvas enlarged), side by side; the first draw is the second input.
var running = ImageGen.Jobs.Where(j => j.Tag == "whatsnew:whatsnew-8b" && j.State == ImageGen.JobState.Running).Count();
if (running > 0) return "already running";
if (!ImageGen.HasKey) return "no key";
var prompt = "Stylized low-poly 3D game art, soft warm lighting, chunky simple shapes, clean flat-shaded polygons, no text, no letters, no user interface, no border. " +
  "Repaint the second picture (the cozy armorer and tailor's shop, the warm lamp light, the three adventurers posing proudly side by side, the wide composition) " +
  "but dress the three adventurers exactly in the armor sets of the three characters in the first picture, keeping their shapes and colors: " +
  "on the left a mage in the black spiked dark armor with glowing mint green accents, a dark blue hood and a small golden crown, holding a staff; " +
  "in the middle a big tank in white and gold full plate with a winged great helm with a golden visor and golden knee guards, holding a large white and gold shield; " +
  "on the right a ranger in green and brown leathers with buckled straps and a green feathered cap with red and white feathers, holding a longbow. " +
  "Keep the shop, the armor stands, the wardrobe, the bolts of cloth and the leather hides of the second picture.";
var options = new ImageGen.Options { Model = ImageGen.Model.Sunburst, Transparent = false, AutoCutout = false, Quality = "high", Size = "1792x864" };
ImageGen.Edit(prompt, new[] { "Temp/ImageGen/whatsnew/ref-class-gear.png", "Temp/ImageGen/whatsnew/whatsnew-8.png" }, "Temp/ImageGen/whatsnew/whatsnew-8b.png", options, null, null, "whatsnew:whatsnew-8b");
return "started";
