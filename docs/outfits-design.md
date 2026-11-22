# Outfits: gear that follows what you train, and the website's gear pages

**Status: approved 2026-11-22** ("go with your recommendations, build the outfits and gear pages"), being built.
The open questions below are settled by the proposals: the pet is part of an outfit, every skill has its own
outfit, and a raid uses the combat skill's own outfit. Written 2026-11-22. From the owner: "will player auto equip gear
loadouts when they switch training? for instance when joining raid/dungeon/towers, so they dont have to equip
everytime. It can remember what you wore when you trained something and fill in missing gear ... maybe let them
choose what gear they want for each skill and fall back to equip best ... may need to be an option", and "ive
wanted for a long time a rework on both inventory tab and equipment tab because they feel very outdated".

## Today

Nothing changes when a player switches what they train. Gear only changes when:
- a new item arrives and beats what is worn for the trained skill (it is put on by itself),
- the player types `!equip <item>` or `!equip all` (the best for the trained skill, one class gear line),
- `!equip <role>` (being built 2026-11-22): the best for a named skill or gear line.

So a ranger who joins a raid as a healer, or a miner who goes fighting, re-equips by hand every time.

## The proposal

**An outfit per skill.** Each skill a character trains has an outfit: what goes in every slot (head, chest,
gloves, leggings, boots, weapon, shield, amulet, ring) and which **pet** is out. A tank pet for the Defense
outfit, a gatherer pet for Mining.

**Every slot is either "best" or pinned.**
- *Best* (the default): the game keeps the best piece for that skill there, by the rule `!equip all` uses.
- *Pinned*: the player's own pick, which nothing replaces. A pin is set on the website (or `!outfit pin`).

**It remembers by itself.** With no outfit set for a skill, the first time you leave that skill what you are
wearing is kept as its outfit. So a player who never opens the website still gets "what I wore last time I
trained this" and never something they did not choose.

**When training changes, the outfit changes.** On `!train`, on joining a raid, dungeon or tower floor, and back
again afterwards:
1. the new skill's outfit is put on;
2. a slot whose item is gone (sold, traded, stashed) is filled with the best piece owned, and the player is told
   once in chat;
3. a skill that has no outfit yet keeps what is worn (and that becomes its outfit).

The change happens when the activity starts, never in the middle of a fight.

**Raids, dungeons and the tower.** A character fights there with the combat skill it trains. One that is
gathering fights with the combat skill it trained last, and that skill's outfit is what goes on; when the fight
is over the gathering outfit comes back.

**Better gear found later.** A new piece that beats a *best* slot of any outfit goes into that outfit, also
while another outfit is worn (today it is only compared with what is on the body, for the skill trained right
now). Pinned slots are left alone.

**Two switches, per character** (website, and `!outfit auto on|off`):
- "Change outfit when I switch training": on by default. Because of rule 3 it does nothing to a player until
  they have worn different gear for different skills.
- "Upgrade to better gear automatically": on by default, which is how the game behaves today.

**Chat, kept small:** `!outfit` (what the outfit for the trained skill is), `!outfit save` (keep what I wear
now for this skill), `!outfit <skill>` (wear that skill's outfit now), `!outfit auto on|off`.

**Where it lives.** On the server, per character: skill, slot, the item, pinned or not, and the pet. The server
changes the equipment when the task changes and the game shows it, as with every equipment change. Transmog
(looks) is separate and unaffected.

**Open questions**
- Should the pet be part of the outfit (proposed), or stay as it is?
- One outfit for all gathering and making skills together, or one each (tools differ)?
- A separate "raid outfit" a player can pick, or simply the combat skill's own (proposed)?

## The website: Equipment and Inventory

Three mockups (generated pictures to go by, not designs to copy; the numbers in them are invented):

- `docs/mockups/gear-equipment-mockup.png`: **Equipment with outfits.** Outfits per skill down the left with
  the two switches under them, a 3D doll in the middle with the slots around it (gem dots on each, a pin on a
  pinned slot, a plus on an empty one), stats on the right with what the outfit changes. The "Set bonus" box in
  the picture is not a feature the game has; it is there as an idea only.
- `docs/mockups/gear-inventory-mockup.png`: **Inventory.** Search, filter chips, sorting, a grid with rarity
  borders, stack counts, an "E" on worn items and a green arrow on upgrades, and a side panel for the picked
  item: compare with what is worn, the gem sockets, Equip, Add to outfit, Send to stash, Sell. A bag meter and
  "Select several" for doing one thing to many items.
- `docs/mockups/gear-unified-mockup.png`: **One "Gear" tab.** The doll and the bag side by side: a row of skill
  buttons picks the outfit, pieces that fit it are outlined, and a piece is dragged from the bag onto a slot.

**Recommendation:** the third for wearing things and the second for everything else. That is, an Equipment tab
built like the "Gear" mockup (doll, outfit row, and the wearable part of the bag beside it for dragging), and an
Inventory tab like the second mockup for the whole bag: selling, the stash, gems, food, resources. The outfits
list of the first mockup becomes the row of skill buttons.

What it can reuse: the 3D character of the customizer (three.js, with the pre-rendered fallback), the gem picker
and socket rows, the item tooltip, the pets tab's pictures.

## Steps, if approved

1. Outfits on the server, in the game and in chat (no website needed to benefit: it remembers by itself).
2. The Equipment tab (doll, outfits, pins, drag and drop, phone layout).
3. The Inventory tab (grid, filters, compare, several at once).

## Built: the website's Equipment and Inventory tabs (phase 1, 2026-11-22)

The two tabs of a character page, working with the equipment functions the server had before outfits. RavenNest
commits 5dbff418, aae5dfb0 and d193aeb7. The old `PlayerInventory.razor` (one "Inventory" tab with two sub-tabs)
is gone; everything it did is in the new pages (the list is in "Kept from the old page" below).

**Files** (all in `src/RavenNest.Blazor` unless said):
- `Components/PlayerGear.razor` with `PlayerGear.razor.cs`, `PlayerGear.Gems.cs` and `PlayerGear.Skins.cs`: one
  component for both tabs (`View="equipment"` or `"inventory"`; without a view it shows both behind its own switch,
  which is what the admin pages use).
- `../RavenNest.Blazor.Services/GearService.cs`: `IGearBackend` (every action the pages ask for, each answering with
  the character as the server has it afterwards), `GearService` (the real one: signed in, and the owner or an
  administrator, checked in the service), `GearRules` (slots, kinds, the worth of a piece, "beats what is worn") and
  `GearSheet` (the combat numbers).
- `wwwroot/css/ravenfall-gear.css` and `wwwroot/js/rf-gear.js` (dragging, hover outlines, the character).
- `Pages/Dev/GearPreview.razor` and `GearPreviewBackend.cs`: `/dev/gear-preview`, signed out, development only.
- Changed: `PlayerView.razor` (an Equipment tab and an Inventory tab; the tab name `equipment` or `gear` in a
  character page address opens the first), `AdminCharacterView.razor`, `_Host.cshtml` (the stylesheet), `Startup.cs` (the service),
  `rf-character-designer.js` (an `inline` option: no wheel zoom, a finger still scrolls the page).

**Equipment.** The character in the middle with the game's thirteen slots around it: head, amulet, chest, gloves,
leggings and boots on the left; melee, ranged and magic weapon, shield, ring and pet on the right; the boat under
the character. (The game wears a weapon per way of fighting, and has no belt or cape slot in use.) A slot shows the
piece's picture in a border of its rarity, its name, gem sockets as stones set in the frame, and marks for an
enchantment and a skin; an empty slot is dashed with the slot's own glyph from the game. Beside it the wearable
part of the bag with search and kind chips. A piece goes on by dragging it onto its slot, by double click, by
picking it and pressing Equip, or by picking it and then its slot. With a slot picked or under the mouse, the
pieces that go in it are outlined and the rest dimmed; while a piece is dragged its slot lights up and the others
step back. A piece the character cannot wear yet carries a lock and says what is missing. The line under the
character names what is picked and what it would change against the worn piece. Details opens the item panel
(a drawer on a wide screen, a sheet on a phone).

**The numbers** are the combat sheet a fight in the game reads, rebuilt on the server side from the same library
(`RavenNest.Combat`: the game's `CombatRules.Sheet` and `PlayerController.UpdateEquipmentEffect`): skills, worn
gear with enchantments and gems, skill tree passives. Six in the strip (health, armor and its damage cut, power,
accuracy, max hit, crit; for Healing max heal and the crit as two), and "All stats" lists everything in the groups
and words of the game's own stats view. Not counted, and said so on the page: food, potions and a battle pet's
bonuses. The shield is left out for Ranged, Magic and Healing, as in the game.

**The row of skills** ("Dress for": Attack, Defense, Strength, Ranged, Magic, Healing) is where phase 2 puts the
outfit picker. In phase 1 a picked skill changes three things: the numbers are shown for it, the "better than
worn" marks are judged for it, and the button reads "Equip best for Healing" and calls the server's equip best
with that word (`!equip healing`). With none picked it is the plain equip best for what is trained.

**The character** is the customizer's 3D figure (the flat pictures without WebGL), loaded after the page. It shows
the character's look, not its gear, and says so ("Shown without gear"). See "The character and worn gear" below.

**Inventory.** The whole bag: search, chips by kind with counts (weapons, armor, jewelry, pets, boats, gems, food,
potions, scrolls, resources, other; only the kinds the bag holds) and, inside a kind, the narrower groups the old
filter had (swords, bows, mining, cooking, ...), sorting (best first, name, amount, vendor value, kind), and a grid
of tiles: rarity border, stack count, an "E" on worn pieces, a green arrow on pieces that "equip best" would wear
instead of what is worn (the game's own `GearScore`, so it is only shown where that rule can tell), socket dots,
enchantment and skin marks. The panel beside the grid (a sheet on a phone) shows the picked item: picture, rarity,
what it is, a comparison with the worn piece of its slot stat by stat (decimals kept), gem sockets (the gem picker
for the owner), enchantments, requirements, vendor price, and the actions. The bag has no size limit in the game,
so the footer counts stacks and items and shows no meter. "Select several" turns the tiles into check boxes for
sending to the stash or selling; the confirmation names how many stacks and items and for how much, lists them,
and says a sale cannot be undone.

**Layout.** The Equipment tab measures the width it is given (a container query), because the same tab stands in
the dashboard, the narrower front page (`/inspect`) and the admin pages: named slots from about 1150 px of room,
slots as pictures with the bag still beside the character down to 760 px, the bag under the character below that.
Beside the character the bag is as tall as the window and scrolls inside itself, so a long bag and the slots are on
the screen together. Under the character, the picked piece and its Equip button stick to the bottom of the screen.

**Kept from the old page**, and where each went:
- The slots with what is in them, and empty slots named: the slots around the character.
- Totals of the worn stats: the numbers strip and All stats (one way of fighting at a time, picked in the row).
- Item details (picture, amount, rarity, enchanted, skin, soulbound, description, type and material, stats with
  the enchantment's part, enchantments, sockets, requirements, vendor price, why a piece cannot be worn): the item
  panel on both tabs.
- Equip and unequip: both tabs, plus drag, double click and "pick, then the slot".
- Send to stash, send to another character of the account, sell with an amount and quick amounts: the item panel.
- Send all of a category to the stash, vendor all of a category: Select several with "Pick all shown", for any
  filter or search, with a confirmation (the old send all had none).
- Search and the nineteen categories: search, kinds and their groups.
- Gem sockets and the gem picker with its two-step confirmation: unchanged, opened from the item panel.
- Skins (transmogrification): choose from the bag or the stash, replace, remove; one dialog instead of two steps.
- Coins of the account, and for administrators Set coins and Add item: the bag's header and the footer.
- The visitor's view (`/inspect`, signed in or not): everything shown, nothing that changes anything.
- The admin character view: the same component with its own Equipment and Inventory switch.

New with the rework: an Equip best button on the website (the server's own), the comparison, the upgrade marks,
drag and drop, sorting, several at once for any selection, the 3D character, the real combat numbers.

### Tested

Through `/dev/gear-preview` (a made up character carrying real items of the development database, signed out) with
real mouse and touch events in headless Chrome, reading the page after each step:
- Equipment: equip by button, by drag, by double click, by picking a piece and then its slot; a drag onto the wrong
  slot does nothing; a piece that cannot be worn is not put on by drag or double click and says why; unequip by
  button and by dragging the worn piece into the bag; the numbers change with each; Dress for Defense changes the
  numbers and the button, Equip best for Defense and plain Equip best report how many slots changed; the detail
  drawer; send to stash from it; search and kind chips.
- Inventory: kinds, groups, sorting by amount, search; selling 28 of a stack of 128 (the amount, the price, the coins
  afterwards); several at once to the stash and to the vendor, with a worn piece refused; "Pick all shown"; equip
  and take off from the panel; a skin applied and removed; a gem socketed through the picker.
- A visitor: no button that changes anything, drag and double click do nothing, sockets are plain rows.
- An administrator (the preview's own): the two views behind the component's switch, Set coins, the Add item dialog.
- A phone (400 px): tap a piece and its slot, tap Equip in the bar at the bottom, the detail sheet, the item sheet,
  several at once.
- Screenshots looked at for polish at 1500, 1366, 1100, 820 and 400 px, and the empty character.
- The real server as a signed-out visitor: the development character on `/inspect` at 1500 and 1366 px. Its
  health (10,067) is the 7,992 + 2,075 the combat library's own notes give for a level 999 in Elder Atlarus.

### Not tested

- Anything signed in on the real pages: every action of `GearService` (equip, unequip, equip best with and without
  a skill, stash, send, sell, skins, gems through this component, Add item, Set coins) ran against the preview's
  stand-in only. The stand-in's equip best is not the server's (it does not narrow the class gear lines to one).
- A running game watching a change made here (the inventory feed should carry it, as for every change on the site).
- Sending a worn piece to the stash (the old page offered it too; whether the server takes it was not seen).
- The admin character view itself, and the flat (no WebGL) character on this page.
- Firefox and Safari; a real phone.

### The character and worn gear

The customizer's export holds the body, heads, hair, eyebrows and beards (134 meshes, 1.6 MB), which is why the
figure wears its look and nothing else. Showing the real worn gear would need, in the same export
(`tools/characters/export-characters-web.cs`): the armor parts of the Synty character per gender (helmets and head
coverings, torsos, upper and lower arms, hands, hips, legs, shoulder, elbow, knee and hip attachments, capes),
several hundred skinned meshes; the class gear lines' own colors per material (the game picks them by material);
the weapons, shields, pets and hats as their own prefabs (hundreds more, each with a hand or back mount); and the
item-to-part table (`maleModelId`, `femaleModelId`, `genericPrefab`, head masks that hide hair). The page would
then load the dozen parts a character wears, so it stays fast; the work is the export, its size on disk (a rough
guess is 30 to 60 MB against today's 1.6) and keeping it current when gear is added. Until then a face-and-
shoulders portrait is one line away (`focus('face')` in `rf-gear.js`) if the bare figure is not wanted.

### For phase 2 (outfits on these pages)

- `IGearBackend` is the seam: outfits per skill (slot, item, pinned or best, the pet), pin and unpin, wear an
  outfit, save what is worn, and the two switches go there, and `GearPreviewBackend` gets the same.
- The row of skills is the outfit picker's place; `dressFor` in `PlayerGear.razor.cs` is the picked skill.
- A slot's marks have a free corner for the pin; the item panel has a marked place for "Add to outfit".
- `GearSheet.Build` takes the worn set from the character it is given, so the numbers of an outfit that is not
  worn can be shown by handing it a copy with other pieces marked worn ("what this outfit changes").
- The Pet slot here is the pet item. The outfit's pet is the battle pet (the Pets tab): it needs its own place.

### Decisions on the gear pages the owner may want to change

- The character stands there without its gear, with a caption saying so, rather than as a portrait or not at all.
- "Equip best" asks for no confirmation; a notice says how many slots changed.
- The Inventory tab shows worn pieces too (marked "E"); they cannot be picked for selling or the stash in Select
  several.
- Rarity is a full border on a tile, as in the mockups (the old page used a bar along the bottom edge).
- The Equipment tab's bag chips carry no icons and no counts, so they fit one line beside the character.
- A share on class gear is named without "Bonus" ("1.5% Healing Power"), as the game's item tooltips do.
- Double click equips on the Equipment tab only.
- The old page's styles for its own Equipment grid (`.rf-gear`, `.rf-slots`, `.rf-slot`, `.rf-inv...`,
  `.rf-itemview...`) are still in `ravenfall-components.css`, unused: that file was being worked on by someone
  else. They can be deleted (check `/stash` and the admin inventory for `rf-tile` and `rf-item` first).
