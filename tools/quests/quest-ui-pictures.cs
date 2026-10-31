// Run with: unity command eval_file --file tools/quests/quest-ui-pictures.cs
//
// Draws the quest window's pictures with the Image Generator (Ravenfall > Image Generator, the user's key) into
// Temp/ImageGen/questui: a scene for each stream quest kind (opaque, for the offer cards), the stories' and empty
// states' subjects and the small reward icons (transparent). Pictures already there are skipped; delete one to draw it
// again. tools/quests/quest-ui-install.cs shrinks them into Assets/World/Quests/Resources/QuestUI afterwards.
// Set Only to a few names to draw just those (the first test).
var Only = new string[0];
const string Style = "Stylized low-poly 3D game art in the style of a cozy fantasy village game, soft warm lighting, chunky simple shapes, " +
    "clean flat-shaded polygons, gentle ambient occlusion, rich but not garish colours, no text, no letters, no border, no frame. ";
const string Scene = "Square composition, the subject centred with a little room round it, a simple darkened backdrop that fits the subject. ";
const string Cutout = "Isolated subject on a fully transparent background, no backdrop, no ground plane beyond a small base, no shadow outside the subject. ";
var pictures = new System.Collections.Generic.List<(string Name, bool Transparent, string Prompt)>
{
    // the stream quest cards, one per objective
    ("quest-fish", false, "A blue fish leaping out of splashing water beside a fishing hook on a line, deep blue water backdrop."),
    ("quest-ore", false, "A wooden-handled iron pickaxe resting on grey rocks with glinting blue ore crystals, dark cave backdrop."),
    ("quest-wood", false, "A woodcutter's axe stuck in a tree stump with a small stack of cut logs and wood chips, forest backdrop."),
    ("quest-wheat", false, "Golden wheat sheaves tied with twine beside a sickle in a small field, warm sunset sky backdrop."),
    ("quest-herbs", false, "A small woven basket of fresh green herbs and flowers on mossy ground, soft meadow backdrop."),
    ("quest-kill", false, "A green goblin with a crude club facing a raised knight's sword and round shield, misty field backdrop."),
    ("quest-make", false, "A blacksmith's anvil with a hammer and a glowing hot sword blade, sparks, dim forge backdrop."),
    ("quest-levels", false, "A glowing golden upward arrow rising out of an open spell book with stars, soft purple backdrop."),
    ("quest-raid", false, "A huge horned raid boss silhouette looming over a group of tiny heroes with raised swords, red stormy backdrop."),
    ("quest-dungeon", false, "A stone dungeon entrance in a mossy rock wall with a burning torch beside it and two crossed swords in front, dark backdrop."),
    ("quest-hero", false, "A heroic knight standing on a castle bridge holding a shield up against a wave of small monsters, dusk backdrop."),
    // the stories and the empty states
    ("story-runestone", true, "An ancient standing runestone with glowing teal runes on a small grassy base with a few rocks."),
    ("story-bananas", true, "A bunch of ripe yellow bananas."),
    ("story-roadwest", true, "A torn old parchment map with a dotted path and a red X, pinned to a tree stump by a hand axe, small ferns and rocks round the stump."),
    ("story-ravebridge", true, "A small fantasy castle with a stone bridge over a river, red roofs, on a little grassy island base."),
    ("empty-board", true, "A wooden village notice board with a pinned parchment, beside a rolled scroll with a red wax seal, an ink pot with a white feather quill and small grass tufts, on a flat stone base."),
    ("village-houses", true, "Three small cozy medieval village cottages with warm lit windows, thatched and red roofs, on a little grassy base."),
    // Ravebridge's island quests (the quest window's Ravebridge tab), and one for every bounty
    ("rb-defend", true, "A sturdy wooden castle gate with a round shield and two crossed swords leaning against it, a torch beside it."),
    ("rb-windmill", true, "A small stone windmill with wooden sails, three big wooden cogs lying at its foot, on a little grassy base."),
    ("rb-cook", true, "A tall birthday cake on a wooden board beside an egg, a small bucket of milk and a pot of flour."),
    ("rb-ghost", true, "A friendly pale blue ghost floating above an old gravestone, holding a skull."),
    ("rb-goblins", true, "Two crude goblin armour chestplates, one dyed red and one dyed blue, propped against a wooden war banner."),
    ("rb-bridge", true, "A short mended wooden plank bridge section with a hammer, a pile of nails and fresh planks."),
    ("rb-talisman", true, "A glowing teal stone talisman on a cord, humming with faint runes, on a small pile of pale crystals."),
    ("rb-romance", true, "A folded love letter sealed with a red wax heart, tied to a small bunch of wildflowers."),
    ("rb-cows", true, "Two brass cowbells on leather collars, one with a little gold star tag."),
    ("rb-sauce", true, "A small cooking pot of bubbling orange sauce with two river trout, onions and a little spice pouch beside it."),
    ("rb-vampire", true, "A closed dark wooden coffin with a sharp wooden stake and a braid of garlic bulbs lying on its lid."),
    ("rb-bounty", true, "A parchment bounty notice pinned to a wooden post with a dagger, a small coin pouch hanging below it."),
    // the small reward icons
    ("icon-coin", true, "A single shiny gold coin seen from the front, slightly tilted, with a simple embossed emblem. Game UI icon."),
    ("icon-wood", true, "Three cut wooden logs stacked. Game UI icon."),
    ("icon-ore", true, "A grey rock chunk with blue glinting ore crystals. Game UI icon."),
    ("icon-fish", true, "A single blue fish seen from the side. Game UI icon."),
    ("icon-wheat", true, "A small bundle of golden wheat ears. Game UI icon."),
    ("icon-loyalty", true, "A round orange and gold medal badge with a simple flag emblem in the middle. Game UI icon."),
};
if (!ImageGen.HasKey) return "no key: enter it in Ravenfall > Image Generator";
System.IO.Directory.CreateDirectory("Temp/ImageGen/questui");
var started = new System.Collections.Generic.List<string>();
foreach (var p in pictures)
{
    if (Only.Length > 0 && System.Array.IndexOf(Only, p.Name) < 0) continue;
    var path = "Temp/ImageGen/questui/" + p.Name + ".png";
    if (System.IO.File.Exists(path)) continue;
    var options = new ImageGen.Options { Transparent = p.Transparent, AutoCutout = p.Transparent, Quality = "high" };
    ImageGen.Generate(Style + (p.Transparent ? Cutout : Scene) + p.Prompt, path, options, null, "questui:" + p.Name);
    started.Add(p.Name);
}
return "started " + started.Count + ": " + string.Join(", ", started);
