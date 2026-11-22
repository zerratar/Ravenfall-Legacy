# Outfits: gear that follows what you train, and the website's gear pages

**Status: approved 2026-11-22** ("go with your recommendations, build the outfits and gear pages"). Step 1 (the
server, the game and chat) and the website's two tabs without outfits are built, see the "Built" sections; outfits
on the website (the picker, pins, the pet) are next.
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

## Built: outfits on the server, in the game and in chat (step 1, 2026-11-22)

RavenNest bdde541e, ee4d6fd9, 856cf682, 2b951b55; game 2116c487, 10a01005, 731ebbd6; RavenBot bb74263. Nothing is
deployed. No database change: see "Where it is kept".

**The rules in one place.** `RavenNest.Models/Outfits.cs`, in the library the game and the server share (as
`GearScore` is): `CharacterOutfits` (a character's outfits, the two switches, which outfit is on), `Outfit` and
`OutfitSlot`, and `OutfitRules`:
- `Remember`: what is worn becomes an outfit. Every slot that is not pinned takes the piece worn there, an empty slot
  is forgotten, a pinned slot stays as it is, and the pet that is out becomes the outfit's pet.
- `Wear`: an outfit's pieces go on; slots it has nothing for keep what is worn (an outfit never takes gear off
  without putting something on). A slot whose piece is gone is filled with the best piece owned (`Best`: the rule
  "equip best" uses, for the outfit's own skill, and of the line the rest of the outfit is of so a set stays whole),
  loses its pin, and is reported once. With nothing to fill it, the slot is forgotten.
- `Desired`: the skill whose outfit should be on: what is trained, and in a raid, dungeon or tower floor the combat
  skill that is trained or was trained last. A gatherer who never trained combat keeps their own outfit in a fight.
- `Upgrades`: does a piece that arrives go into an outfit that is not on (its slot there is "best", the outfit's piece
  is still owned, and the new one beats it by the rule that puts a found piece on, for that outfit's skill).
- `MayReplace`: may an arriving piece replace what is worn by itself (not with "upgrade automatically" off, never in
  a pinned slot of the outfit that is on).
- `NeedsAsking`: does the game have to ask the server at all (see "When it happens").

An outfit has eleven slots: head, chest, gloves, leggings, boots, melee, ranged and magic weapon, shield, amulet and
ring. The pet item, the cape and the boat are not part of one. A slot names its piece twice: by inventory item (the
exact enchanted or socketed piece) and by kind of item. A plain piece gets a new id when it is taken off and joins
its stack, so it is found by kind then; an enchanted or socketed piece is only ever itself.

**Where it is kept.** One short text per character in a user property, `outfits.<character id>` (as the cape toggle
is `cape.hidden.<id>`): `1;f1;u1;w7;c12|7;;<pet>;1:<item>:<kind>:p,4:<item>:<kind>|12;ranger;...`. No table, no
column, no script at deploy. About 70 characters a slot, under a kilobyte an outfit. Text this version does not
know is skipped, so a newer server's text still reads. Nothing is cleaned up when an item is sold, traded, stashed,
given or destroyed: a piece that is not there is simply a slot to fill the next time the outfit goes on.

**The server decides** (`OutfitManager`), the game shows what comes back. Six commands on the game connection, each
answering with an `OutfitResult` (the outfits as they are now, whether gear was put on, the pieces that were gone
and what filled them, the stable when a pet went out); the gear itself comes with the answer as inventory changes,
like every equipment change:

| Command | What it does |
|---|---|
| `outfits.get` | the character's outfits |
| `outfits.follow` (trained, fighting, from) | training changed, or a fight began or ended: the outfit that belongs to it goes on |
| `outfits.wear` (skill) | that skill's outfit goes on now (`!outfit attack`) |
| `outfits.save` (skill, mode) | what is worn becomes the skill's outfit (0), is pinned as it (1), every slot "best" again (2) |
| `outfits.switches` (follow, upgrade) | the two switches |
| `outfits.offer` (inventory item, item) | a piece that arrived, for the outfits that are not on |

**When it happens.** The game looks at each player once a second (`PlayerOutfits.Tick`): what they train, and
whether they are in a raid, in a dungeon or on a tower floor (a tower town is a calm place and keeps the training
outfit). It asks the server only when the outfit that should be on is not on. So `!train` and every other way of
changing the task, joining a raid by hand or by auto join, a dungeon starting, going up to a tower floor, leaving
early, being sent home at the end and being knocked out are all the same thing to it, and none of them can be
missed. A fight that begins asks only for those whose fight outfit is another one than the one they wear: a raid
with 300 fighters in it asks nothing.
- Leaving a skill keeps what is worn as its outfit. A skill with no outfit keeps what is worn, so a player sees
  nothing until they have worn different gear for different skills.
- Never in the middle of a fight: someone who picks another skill while the raid, dungeon or floor goes on keeps
  what they wear until it is over. (`!outfit <skill>` changes it at once, for those who mean it.)
- A character that leaves the game, or whose stream ends, in the middle of a fight: the server remembers which
  outfit to go back to (`Back`) and puts it on when the character leaves or next joins a game, before the game gets
  the character (`OutfitManager.ComeBack`, called from `PlayerManager`).

**Better gear.** A piece that arrives is judged for what is worn as before (`PlayerController.EquipIfBetter`), with
two new stops: nothing replaces a pinned slot of the outfit that is on, and nothing is replaced by itself with
"upgrade automatically" off (an empty slot is still filled). When the game puts a piece on by itself it now tells
the server, which it never did; without that the server would keep a different outfit than the player wears. The
piece is also offered to the outfits that are not on (`outfits.offer`), and goes into every one whose slot is
"best" and whose piece it beats. "Equip best" (`!equip all`, `!equip tank`) puts the pinned pieces of the outfit
that is on back afterwards, on both sides by the same rule.

**The pet.** The battle pet that is out when a skill is left becomes that outfit's pet, and goes out with the
outfit, by the rules of `!pets use` (one pet out; not an egg, not a pet whose item is gone). No pet out when the
skill was left, or a pet that is gone: the pet stays as it is.

**Where the game follows the server.** When the server puts a piece on, anything else the game has on in that slot
comes off (`Inventory.ApplyServerStacks`). Before, a piece the game had put on by itself, or the rest of a stack one
piece was taken from, could stay "worn" beside it.

### Chat

`!outfit` in both bots (also `!outfits`), sent to the game as `outfit_command`:

| Command | What it does |
|---|---|
| `!outfit` | the outfit of what you train, in a few words, its pet, and what is special about it |
| `!outfit save` | what you wear now is this skill's outfit (pinned slots stay as they are) |
| `!outfit pin` | the same, and every slot is pinned: better gear will not replace it |
| `!outfit unpin` | every slot is "best" again |
| `!outfit <skill>` | that skill's outfit goes on now, whatever you train (`all`, `atk`, `tank`, `mining` ...; also `!outfit wear <skill>`) |
| `!outfit auto on` / `off` | change outfit with training, raids, dungeons and the tower |
| `!outfit upgrade on` / `off` | put better gear on by itself |

Replies (`Localization.MSG_OUTFIT_...`): "Your Ranged outfit: Windrunner and Stalker (ranger). Disco Raven Pet goes
out with it. Pinned slots: 1." / "What you wear is now your Farming outfit: Archmage (mage)." / "You have no Fishing
outfit yet. What you wear while you train it becomes its outfit." / "Gone from your Farming outfit: Bronze Helmet. In
its place now: Starcaller Hood." Gear is named by its armor: the material with the most pieces, a second of the same
line, and the line or "plate".

### Decisions made while building

- **`!outfit save` does not pin; `!outfit pin` does.** Saving is what leaving a skill does by itself, so it is only
  needed to keep the outfit now. Chat pins and unpins the whole outfit; one slot at a time is the website's.
- **`!equip <role or line>` belongs to the outfit that is on.** The word is kept with that outfit on the server
  (`Outfit.Word`), so gear that arrives is ranked for it as before (`Inventory.AskedOutfit` is set from it), it comes
  back with the outfit after training something else, and `!equip all` forgets it. Before, it was the game's alone
  and gone when the player left. Asked for while training something else (`!equip tank` while training Attack), it
  makes the tank gear the Attack outfit: what you wear while you train a skill is that skill's outfit.
- **`!outfit <skill>` leaves the training alone.** That outfit is "on" until training or a fight next changes; what
  is worn then is kept as that skill's outfit, not as the trained one's.
- **Turning "change with training" back on** puts the outfit of what is trained on at once.
- **Training "all"** has its own outfit (kept as Health, shown as "All").
- **Taming** has no outfit (it is never the task).
- **A raid skill set with `!raid skill`** is what is trained inside the raid, so that skill's outfit is the one worn there.
- **An outfit can hold a piece another outfit holds.** A better piece goes into every outfit it improves.
- **Hats and masks that are looks** (no stats) are kept in an outfit as worn, and never picked to fill a slot.

### Games before 0.9.7

They never ask about outfits, and the server never changes an outfit by itself: every change starts with a command
only a 0.9.7 game sends. So their gear is never changed behind their back. Two places know the game's version (the
session's client version, 0.9.7 or later, or a session that has ever sent an outfit command):
- after an older game's "equip best" the server does not put pinned pieces back (that game would go on showing what
  its own equip best put on);
- a change made on the website is told to a 0.9.7 game as `OutfitsChanged` (event 60); an older one hears of gear
  put on item by item, as for every website equip.
One thing does happen for a character in any game: a fight's outfit that was left on comes off when it joins or
leaves (`ComeBack`), before the game gets the character, so the game shows it as it is.
A character that played 0.9.6 in between, in other gear, has that gear kept as the outfit of the skill the server
last saw worn when it next trains something else in 0.9.7.

### For the website (phase 2)

`RavenNest.Blazor.Services.OutfitService` (registered in `Startup.cs`; inject it like `GearService`). Every method
takes the character's id and checks it itself: signed in, and the character's owner or an administrator. Every
method returns an `OutfitChange`:
- `Refused` (string): why nothing was done ("You need to be signed in to do that.", "That character is gone.", "That
  character does not belong to you."); null otherwise.
- `Result` (`RavenNest.Models.OutfitResult`): `Status` (`Ok`, `Error`, `UnknownSkill`, `NoOutfit`, `BadItem`,
  `BadPet`), `Changed` (gear was put on), `Skill` (the outfit the call was about), `Gone` and `Filled` (item ids:
  pieces the outfit had that the character no longer owns, and what filled each; an empty id where nothing did),
  `Pets` (the stable, when a pet went out).
- `Outfits` (`RavenNest.Models.CharacterOutfits`, never null): the character's outfits afterwards. `Follow` and
  `Upgrade` are the two switches. `Worn` is the skill whose outfit is on (`Skill.None` before anything was kept).
  `Outfits` is the list: each `Outfit` has `Skill`, `Pet` (a battle pet's id, empty for none), `Word` (the role
  asked for with `!equip tank`, null for none) and `Slots`, each an `OutfitSlot` with `Slot`
  (`RavenNest.Models.EquipmentSlot`), `Item` (inventory item id, may be empty), `ItemId` (the kind of item) and
  `Pinned`. A skill that is not in the list has no outfit: it keeps what is worn. `Find(skill)` gets one.
- `Ok`: not refused and `Status` is `Ok`.

| Method | What it does |
|---|---|
| `Get(characterId)` | reads the outfits and switches |
| `SetSlot(characterId, skill, slot, inventoryItemId, pinned)` | puts one of the character's items into a slot of a skill's outfit, pinned or best. `BadItem` when the item is not the character's, does not belong in that slot or cannot be worn at its levels. Makes the outfit if the skill has none. When that outfit is on, the piece is put on at once |
| `ClearSlot(characterId, skill, slot)` | takes the slot out of the outfit: what is worn there stays when the outfit goes on. An outfit with nothing left in it is no outfit |
| `SetPet(characterId, skill, petId)` | the battle pet that goes out with the outfit; an empty id for none. `BadPet` for an egg or someone else's pet. When that outfit is on, the pet goes out at once |
| `Wear(characterId, skill)` | puts the outfit on now, as `!outfit <skill>` does. `NoOutfit` when the skill has none |
| `SetSwitches(characterId, changeWithTraining, upgradeAutomatically)` | the two switches; null leaves one as it is |

Skills are `RavenNest.Models.Skill`; training "all" is `Skill.Health` (`OutfitRules.Normalize` maps `Melee` to it),
and `Taming` has no outfit (`UnknownSkill`). The slots an outfit has are `OutfitRules.Slots`. To show a slot's
piece, look the `Item` up in the character's inventory and fall back to any stack of `ItemId` (a plain piece changes
its inventory id when it is taken off). `OutfitRules.Find(slot, pieces)` is that rule, should the page want the very
same answer the server gives. To pin what is already in a slot, call `SetSlot` with the same item and `pinned` true.
There is no "save what is worn" method here: `SetSlot` per slot does it, or ask for one. A game the character plays
in is told of every change and shows it.

### Tested

Unit tests (`Tests/RavenNest.UnitTests/OutfitRulesTests.cs`, 20 tests): the kept text (round trip, unknown parts,
unreadable text), skills and what players type, which outfit a fight uses, when the game asks, remembering, pins,
wearing, a plain piece found by its kind, a gone piece filled (from the outfit's own line, and told once), a better
piece into a best slot and never a pinned one, ranking for the outfit's own skill, and the slot mapping against the
server's own.

Played on the local server with the development character, chat cut off, comparing what the game shows with what
the server has equipped after every step (they were the same every time):
- **Back and forth**: Crafting in Elder Atlarus with the Disco Raven, Ranged in Windrunner (`!equip ranger`),
  Farming in Archmage robes with the Jackalope: three rounds through all three, each outfit and its pet came back.
- **Pins**: a pinned pair of gloves stayed on when a better pair arrived, through `!equip all` and `!equip ranger`,
  and came back after other boots were put on by hand over a pin and `!equip all` was used. `!outfit unpin` let the
  better pair on.
- **A best slot while another outfit is worn**: better boots that arrived while crafting went into the Ranged
  outfit, the better gloves did not go into its pinned slot, and the boots were on when Ranged was next trained.
- **Upgrade off**: nothing was put on by itself, on the body or in another outfit; on again, it was.
- **A gone piece**: a helmet sold out of the Farming outfit was replaced by the best mage hood owned and told once
  (the text was read; not sent to real chat), and not again the next time; the same for a pinned piece through
  `!outfit farming`, in the reply.
- **A raid** as a farmer whose last combat skill was Ranged: the ranger outfit and its pet on joining, the farming
  outfit back after leaving early, the ranger outfit again on joining again, no change when another skill was
  picked mid-fight, no change when knocked out, the farming outfit when the raid ended. Someone training Ranged:
  nothing asked, nothing changed.
- **A dungeon**: nothing while waiting, the ranger outfit when it started, the farming outfit when it ended.
- **The tower**: the ranger outfit on floor 6, the farming outfit after leaving.
- **Leaving in the middle**: leaving the game in a raid put the farming outfit back on the server at once; stopping
  the game in a raid left the fight outfit on the server, and the character came back in its farming outfit when
  the game was started again and it joined.
- **The switches**: with auto off training changed and nothing else did; turned on, the trained skill's outfit went
  on at once.
- **A rejoin**: the outfits came back with the character and went on changing.
- **Chat**, through the game's handlers with a stand-in for the bot: every form in the table above, a skill with
  no outfit, a word that is nothing, `!outfit` while another skill's outfit is on.
- **The website's calls**, through a temporary page of the local server that called the same `OutfitManager`
  methods (deleted afterwards): pin a slot of the outfit that is on (the piece went on in the game at once), a wrong
  slot and an item that is nobody's (`BadItem`), a pet (out at once), no such pet (`BadPet`), clear a slot, both
  switches (the game had them a moment later), wear another skill's outfit, a skill with none (`NoOutfit`).

### Not tested

- `OutfitService` itself (the signed-in check in front of those methods): there is no page that uses it yet.
- `!outfit` from real chat through either bot: the bots compile, the packet name is the one the game registers.
- A game before 0.9.7 against this server: no such game was run. What it rests on is above.
- The gone-piece line arriving in real chat when training changes (the bot was cut off; the text was read).
- A piece really found or bought in play going into another outfit: arriving pieces were pieces already in the bag
  handed to the same function a find calls. The path a found piece takes while its add is still on its way to the
  server (`PlayerController.IsBeingAdded`) was not run.
- Auto join, a raid won with rewards, a dungeon finished, a tower boss fight, and many players at once.
- A second character of the same account, and a character moving between two streams.

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
  outfit, save what is worn, and the two switches go there, and `GearPreviewBackend` gets the same. The server's
  side of each is `OutfitService`, described in "For the website (phase 2)" above.
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
