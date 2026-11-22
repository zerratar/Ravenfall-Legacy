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

**The row of skills** ("Dress for": Attack, Defense, Strength, Ranged, Magic, Healing) is the outfit picker since
phase 2 (see "Built: outfits on the website" below). For a visitor it is still what phase 1 made it: a picked skill
changes the numbers shown and the "better than worn" marks. For the owner the button under the numbers reads "Equip
best for Healing" for a picked fighting skill and calls the server's equip best with that word (`!equip healing`);
with none picked it is the plain equip best for what is trained.

**The character** is the customizer's 3D figure, loaded after the page, and since 2026-11-22 it wears what the
character wears (see "The character wears its gear" below). Without WebGL it is the customizer's flat picture of
the character's look, with the note "Shown without gear".

**Tooltips** (RavenNest 063479b2). Every piece on the Equipment tab has one: a bag piece alone when its slot is
empty, a worn piece on its slot, and for a bag piece whose slot is taken the two side by side as one card ("In your
bag" and "Worn now", each stat's difference in green or red). The card keeps off the slot it is about and off the
screen's edges (it flips, or stacks the two when there is no room beside), does not flicker between tiles, and goes
when a drag starts. On a phone a tap opens the detail sheet with the same comparison and Equip.

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
- The admin character view itself.
- Firefox and Safari; a real phone.

### The character wears its gear (3D, 2026-11-22)

The figure in the middle of the Equipment tab is dressed as the game dresses it. RavenNest e41bfabb (the page),
26b0823e (the export and the fixes from comparing with the game), 687b0f90; game a7bb888f (the export script).

**The export** is `tools/characters/export-gear-web.cs` in the game repo, beside the body's
`export-characters-web.cs`: an edit mode eval (`unity command eval_file --file tools/characters/export-gear-web.cs`,
about 15 seconds, spread over editor frames; it reads assets only, and the one copy of the player it makes to sample
the stances lives in a preview scene of its own). It writes into RavenNest's `wwwroot/characters/gear` (26 MB, 870
files):
- `parts/`: every armor part of the Player prefab, one small file each (578 files, 8.8 MB), skinned to the body
  export's skeleton: torsos, arms, hands, hips, legs, helmets, hats, masks, hoods, shoulder, elbow and knee pads,
  what hangs on the belt and the back, capes.
- `props/`: every item that is a model of its own on a bone (161 files, 5.4 MB): weapons, shields, novelty hats,
  necklaces. The list is the prefabs the local server's items name, kept in `tools/characters/gear-prefabs.txt` for
  when no server answers.
- `textures/` (9.2 MB): the armor's atlas and its masks, and the props' pictures.
- `stances/`: the idle of each kind of weapon from the animation pack the game uses (sword, two handed sword, bow,
  staff, spear, axe, shield), and how far each turns the chest.
- `gear.json` (106 KB): the part lists by the game's own field names and order (an item's model ids are indices into
  them), every `ItemMaterial` with its thirteen colors and glow, the parts that show the wearer's skin, the hair that
  is swapped under hats, the props with their materials, the armor's own bind pose for the attachment bones.

Nothing in it names an item or a gear line. The page reads an item's model ids, material, `HeadMask` and prefab from
the server and looks them up, as the game does in its prefab; a new gear line is new materials (found by walking
`ItemMaterial`) and new item rows. The Taming line was added while this was built and a re-run picked it up (132
materials). **Run the export again when armor parts, materials or item prefabs are added**, and restart the server
(new files under `wwwroot` are served after a build).

**The page** (`wwwroot/js/rf-character-gear.js`, a port of `SyntyPlayerAppearance.EquipArmor` and what it calls; the
stage is the customizer's, `rf-character-designer.js`): the armor parts by the item's model ids for the character's
gender, the tier's material and colors, what a helmet hides (head, hair, brows, beard, by `HeadMask`), the flat hair
under a hat, the wearer's skin color on the parts that show skin, the weapon and shield of the style that is trained
or picked (a shield only with a one handed weapon or none, as in the game), the necklace, the cape in its shape with
the clan's cape picture (the game's default cape picture otherwise), the helmet and cape switches, and a skin's look
instead of the item's own. Pets and boats are not on the figure.

Loading: the body shows first; then only the worn parts are fetched, and any other piece when the mouse is over it
in the bag (the figure tries it on), when it is dragged, or when it is put on. Fetched parts stay in memory for the
page, and every file is asked for with the export's version in its address, so the browser keeps it (the server
sends such addresses as immutable for a year). A small spinner in the stage's corner shows while a piece is on its
way; the page is never blocked. A piece dropped on its slot stays on the figure until the server has answered, and
the figure then shows what the server says is worn. A character with a weapon out stands in that weapon's stance
(the game's own idle for it), turned so its front shows, with the camera a step back.

Measured on the development machine (headless Chrome with software rendering, cache off): a typical dressed
character (five armor pieces, a necklace and a staff) costs 25 to 28 requests and 460 to 505 KB beyond the bare
figure's 18 requests and 500 KB: 14 to 17 part files (200 to 240 KB), 2 props (40 KB), 6 pictures (190 KB, shared
by every tier), the manifest (15 KB compressed) and a stance (8 KB). The dressed figure stood 0.6 to 1.5 seconds
after the script started, about half a second after the bare one.

**Compared with the game.** `tools/items/class-gear-preview.cs` rendered the same real items (Elder Atlarus plate
with its antler helmet; Mage tier 1 and 8 with the wizard hat over swapped hair; Healer tier 1 and 8 with the cowl
that hides hair; Barbarian tier 1 and 8 with the bear hood and bare skin), male and female, and the page's figures
were put under them piece for piece. What differed and was fixed: props were a hundredth of their size (the bones
are in centimeters under a root scaled down; the game hangs an item at its true size); parts weighted to the cape's
and the attachments' bones flew off (the body export has no bind pose for those bones, so the armor brings its
own); the glowing tiers were flat (the shader graph's drifting band of color is ported); bows and staffs were black
(their own shader's banded colors are ported); katanas are drawn unlit as in the game; and the customizer's moody
stage light made a tier's colors darker and warmer than in the game, so this page lights its figure evener and
whiter. What still differs: the page's light is its own, so shading is a little stronger than in the game's
preview; the two glowing tiers whose materials use other textures than the body's atlas (ElderSteel, ElderAbraxas)
are drawn with the atlas; the Turd Sword and the Ultimate Tuna Slapper have no pictures in their materials and are
drawn in plain colors.

Not seen: a clan's cape picture on the figure (the preview character has no clan; the address the page asks is
`/api/clan/cape/{clan id}`), a transmog look on the figure (the look's model ids and material are sent in place of
the item's; the preview's skinned piece was not opened on the dressed figure), and any signed-in character.

## Built: outfits on the website (phase 2, 2026-11-22)

RavenNest 47c45ddb, on top of the outfits agent's `OutfitService`.

**Files:** `Components/PlayerGear.Outfits.cs` (the state and what each button does), the row and the outfit bar in
`PlayerGear.razor`, `IGearBackend` and `GearService` in `GearService.cs` (six outfit calls that hand over to
`OutfitService`, which checks each one), `GearPreviewBackend.cs` (outfits kept in memory for the preview), the
"Outfits" block in `ravenfall-gear.css`.

**The picker.** The row of skills on the Equipment tab is headed "Outfits". The fighting skills are always there
(All, Attack, Defense, Strength, Ranged, Magic, Healing; "All" is training all, which the server keeps as Health);
another skill joins the row once it has an outfit or is trained, and "More skills" shows the rest (Woodcutting to
Sailing; Taming has no outfit). A skill with an outfit wears a small shirt in its corner, gold on the one that is on;
the green light is the skill trained now. Under the row a bar says in words what the picked skill's outfit is ("On
now. 10 pieces, 1 pinned.", "Not on now: the slots, the figure and the numbers show it.", "No Attack outfit yet"),
holds the outfit's battle pet (a list of the character's pets, or "Stays as it is") and, for an outfit that is not
on, "Put it on". Under the bar are the two switches: "Change outfit when training changes" and "Put better gear on
automatically".

**Looking at an outfit that is not on.** Picking its skill shows it everywhere: the slots show its pieces (a slot it
leaves alone shows what is worn, set back, with "Stays as worn"; a piece it names that is no longer owned says "Gone
from the bag"), the 3D figure wears it, and the numbers are counted for it under the line "The numbers with the
Magic outfit on". The big button becomes "Put the Magic outfit on". Picking the skill again goes back to what is
worn.

**A slot.** A pinned slot carries a gold pin in the corner of its picture. With a slot picked, the line under the
figure offers Pin or Unpin, and for an outfit that is not on, "Leave out" (the slot goes out of the outfit and what
is worn stays there). A bag piece goes into the outfit that is looked at by "Add to Magic outfit" in that line, by
picking the piece and then its slot, or by dragging it onto its slot; it goes in pinned, since it is the player's
own pick. While the worn outfit is looked at, the same things equip as before, and the item panel has "Pin in
Healing outfit" for a bag piece (the server puts it on at once when that outfit is on). Adding a piece for a skill
that has no outfit starts one.

**What the server decides.** Every button calls the server and the page then shows the outfits and the character as
the server gives them back; nothing is patched on the page. The messages for a refusal are the server's (not signed
in, not your character) or name its status (a piece that cannot go in, no outfit for that skill, a pet that cannot
go). Visitors see no outfits (`OutfitService.Get` is for the owner and administrators): for them the row is headed
"Dress for" and works as in phase 1.

**The Beastmaster line** needs Taming, which is not an item column: the requirement (`ItemLines.TamingLevelFor`) is
listed with the others in the item panel and the tooltip, and a piece below it is locked with "Needs Taming 900."

### Tested (phase 2 and the figure)

Through `/dev/gear-preview`, whose stand-in keeps a healing outfit that is on (the cowl pinned, a pet) and a magic
outfit that is not, with real mouse events in headless Chrome, reading the page after each step:
- the worn outfit's bar and pin; picking Magic: its four pieces in the slots, eight slots "Stays as worn", the
  numbers for it (power 84 Magic instead of 186 Healing), the figure in the Phantom chest and Conjurer hood with the
  Dragon staff, the button "Put the Magic outfit on";
- Unpin the staff; Leave out the chest (3 pieces left); a bag chest added by the button (pinned); a hood dragged
  onto the head slot goes into the outfit and not onto the body; the pet changed; Put it on (the slots, numbers and
  figure are the outfit's, the bar says "On now");
- both switches off, each with its notice; a skill without an outfit says so and "Add to Attack outfit" starts one;
  More skills lists all seventeen;
- a visitor: "Dress for", six skills, no bar, no switches;
- the Taming requirement on a tier 8 Beastmaster vest (locked, "Needs Taming 900."), and Beastmaster pieces on the
  figure (feathered hat, pelt hood with antlers, the vest with one bare shoulder in skin color);
- the figure: hover tries a piece on, a dragged piece stays on while dragged and after the drop, the cape, the cape
  hidden, the helmet switched off, the flat fallback without WebGL ("Shown without gear");
- screenshots at 1500 and 400 px looked at for polish.

### Not tested (phase 2 and the figure)

- Everything signed in: `OutfitService` through this page (the preview's stand-in answers in its place and is
  simpler: its "put on" does not fill a gone piece with the best one owned, and it knows nothing of the role word).
- A running game seeing an outfit change made here, and the page seeing a change made by the game while it is open
  (the page reads the outfits when the tab opens and after each of its own actions).
- A piece of an outfit that is gone from the bag (the "Gone from the bag" slot was not brought about).
- The admin character view with outfits (an administrator gets the same picker for any character).
- A clan cape's picture and a transmog look on the figure; Firefox, Safari and a real phone.

### Decisions on the gear pages the owner may want to change

- With a weapon out the figure stands in the weapon's stance, turned to show its front, instead of the relaxed idle.
  The stance is the game's and puts the weapon in the hand properly, but it is side-on and wide, so the figure is
  a little smaller. The other way is the relaxed idle with no weapon shown.
- The figure's light on this page is evener and whiter than the customizer's, to match the game's colors.
- A piece put into an outfit from the website goes in pinned. Leaving a slot to "best" is the Unpin button.
- The pet list of an outfit offers every battle pet of the character and "Stays as it is" for none.
- Outfits are shown to the owner (and administrators) only; a visitor does not see what a character keeps for other
  skills.
- The fighting skills are always in the row, the other skills only once they matter (More skills shows them all).
- Looking at an outfit that is not on shows its numbers, not a difference against what is worn.
- There are two "put it on" buttons while an outfit is looked at (in the bar and the big one under the numbers).
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
- The export is 26 MB in the RavenNest repository. It could live outside it (a release asset or a storage bucket)
  if the repository's size matters more than having the site whole in one checkout.

## The figure's switches (2026-11-22)

The owner, after trying the dressed figure: "we need to be able to toggle the helmet/cape visibility on the website
under gear", "when i try to see how it looks like when i wear another helmet on it just shows like im not wearing a
helmet at all", "maybe able to toggle combat stance vs normal idle stance?" and "can we expand the 3d view slightly
more? theres lots of unused space here on the equipment rows" (RavenNest 7bdff90e).

- **Helmet and Cape** buttons under the figure, for the owner and administrators: they set (not flip) the same
  settings as `!toggle helmet` and `!toggle cape` through `PlayerManager.SetHelmetAndCape(characterId, helmet,
  capeHidden)`, which sends the appearance to a running game as a website appearance save does
  (`GameEventType.PlayerAppearance`; the game's `SetAppearance` reads `HelmetVisible` and `CapeHidden`). The Cape
  button shows only for a character that has a cape (its own or its clan's).
- **A tried-on head piece always shows.** The helmet switch hid every head piece on the figure, so hovering a helmet
  with the switch off showed a bare head. Now it hides only the worn one; a hovered, dragged or outfit-viewed head
  piece shows.
- **Combat or At ease:** the figure stands in its weapon's stance, or at ease with empty hands (no weapon, no
  shield). Each viewer's own choice, kept in the browser (`rf.gear.stance`); combat by default, as before. A weapon
  that is hovered is held in either.
- **More room:** on a wide page the slot columns keep the room their names need and the figure's column takes the
  rest (it was a fixed 13rem). On a stage much taller than wide the camera steps back so the fighting stance is not
  cut off at the side; on a narrow stage the three buttons are pictures only.
- **Checked** on `/dev/gear-preview` at 1920, 1500, 1280 and 400 wide: both stances, the helmet switch both ways,
  hovering a helmet with the switch off and leaving it. **Not checked:** signed in, and a running game following the
  website's switches.

## "Now" and the skills' outfits (2026-11-22)

The owner, training Crafting, picked Strength, pressed "Equip best for Strength", then Ranged and "Equip best for
Ranged", and back to Strength showed the Ranged gear: the button changed what was worn and kept nothing for either
skill, and a skill without an outfit shows what is worn. Their idea, built (RavenNest 340c9a0f):

- **"Now"** is the first button in the row and where the page opens: the gear worn this moment. Equipping, dragging
  and "Equip best for (what is trained)" there put gear on in the game at once, whatever is trained.
- **Every skill after it is that skill's outfit.** What is done there is saved to the outfit: adding a piece (button,
  drag, double click, click on a slot), pinning, leaving a slot out. Gear only goes on when that outfit is the one
  that is on, or with "Put it on now".
- **"Make a (skill) outfit from my best gear"** for a skill with no outfit, and **"Refill with best"** for one that has
  one: `OutfitManager.FillBest`, every slot that is not pinned gets the best piece owned for that skill by
  `OutfitRules.Best` (the rule an outfit going on uses for a gone piece). For the outfit that is on it is "Equip best
  for (skill)" and puts it on too.
- The text under the row says which it is: "Worn now", "No Strength outfit yet" (the slots show what is worn and
  nothing worn changes), "Not on now" (changes are saved to the outfit) or "On now" (changes go on in the game too).
- **Equip best on "Now"** picks for what the character trains. Crafting and the other skills without a gear line get
  the highest-worth pieces, and the class gear line already worn is kept.
- **Checked** on `/dev/gear-preview` with the owner's steps: Now kept its gear throughout, the Strength and Ranged
  outfits were made and kept apart, Now's button reads "Equip best for Healing". **Not checked:** signed in, and the
  game following an outfit filled from the website.


## After the UX review (2026-11-22)

An independent review of the Equipment tab on the preview (training Healing) found the page measuring everything
against the worn gear in a skill's tab, two "best" rules giving different gear, and several places that did not say
what was going on. The model stays the owner's: "Now" is what is worn this moment, every skill after it is that
skill's outfit. Built in RavenNest f26aee13, 48484a90 and 8c2e115d, and the Models DLL in game 2a2b3ec6; the owner
then approved the held back ideas too (A to E below).

**One best rule.** `OutfitRules.BestPicks` (and `FillBest`, `FitsForBest`) pick, slot by slot, what the server's
equip best picks for a skill: the worth with enchantments, the lines of the skill narrowed to one with the worn set
winning among equals, and worth alone for a skill without a line. `OutfitManager.FillBest`, the website's "Make" and
"Refill" and, on "Now", "Equip best for (the outfit that is on)" all use it, so Now and the outfit no longer drift
apart through the button. The server's equip best now counts one-handed axes and spears as melee weapons, as the
outfits always did. Unit tests (`OutfitRulesTests`) restate the server's equip best on the preview's bag and check
every skill agrees, and that Strength and Ranged differ where the bag has gear of their lines.
Why the review saw different numbers: the preview stand-in filled outfits without enchantments, and its equip best
kept worn pieces the character may not wear (the Archmages Pendant and Owls Eye Ring need Slayer 200). The real
server takes those off. The stand-in now builds its pieces as the server does and dresses with the shared rule.
On the preview, before: Now 191 power, 719 max heal, 12% crit; the Healing outfit 150, 602, 7%; Strength and Ranged
identical. After: both 150, 602, 7% (the lower numbers are the pendant and ring coming off, as on the server);
Strength gets the Reaver Hauberk, Ranged the Hunter Gloves (two pieces added to the preview's bag to show it).

**The review's findings, as built:**
1. In a skill's tab a bag piece is compared with the outfit's piece for its slot: the pick bar ("Instead of Abraxas
   Helmet (in the Strength outfit)"), the card's second column ("In the Strength outfit") and the arrows. A shirt
   mark shows bag pieces that are in the outfit looked at (on Now: in the outfit that is on, not worn). "Equip best
   would pick this" only shows on the piece the one rule picks.
2. No "Equip now" (and no "Take off", drag to the bag or Equip in the drawer) in a skill's tab: Add to the outfit,
   Pin, Leave out, Details.
3. One best rule (above).
4. The trained skill with no outfit: "Make a Healing outfit from my best gear and wear it", with words that say it
   goes on.
5. "Now" says which outfit is on ("Now: your Magic outfit is on (put on by hand). You train Healing; it changes
   back when training changes"), its tile reads "Magic on", its numbers are counted for that skill ("As worn now,
   counted for Magic") and its button is "Equip best for Magic", which fills that outfit and puts it on.
6. A slot whose worn piece is not the outfit's: "Your Healing outfit keeps Priest Cowl here (pinned) and puts it
   back when it next goes on", with "Keep Dragon Helmet in the Healing outfit" and "Put Priest Cowl back". The
   outfit's tab no longer says "It is what you wear" then; it says how many slots differ.
7. Slots that differ carry a "Changes" badge in a skill's tab ("Not saved" on Now); the bar counts them and the big
   button says "Put the Strength outfit on now (changes 6 slots)".
8. A word under each skill: On, Saved, Training, On, training. The gold fill is the picked tile's alone; the green
   light and the small shirts are gone.
9. Notices list what changed: "Equip best for Healing changed Amulet: Archmages Pendant to Phantom Amulet, Gloves:
   Rune Gloves to Priest Gloves, ..., and 2 more"; the same for making, refilling, putting on, saving and equipping.
10. The outfit bar's buttons go under its words when narrow; the row of skills gets columns that leave no tile
   alone on a row; names fit.
11. The outfit's pet list is "Battle pet", with "Whichever is out" for none.
12. A character with nothing to wear: no equip best and no switches, "Buy gear at the Vendor" (/vendor) instead.
13. A second click on the picked skill does nothing.
14. The big button ignores clicks for a second after it changes meaning; "Put on now" has its own outlined look.
15. "What the marks mean" under the bag, folded; the Inventory tab's legend got the skin mark so both match.
16. Visitors seeing the coins and the whole bag: the old page (before 5dbff418) showed both to everyone too, so it
   was left as it was.

**Built after the owner's approval of the held back ideas:**
- A. On Now, when what is worn differs from the outfit that is on, a strip over the slots ("2 changes not in your
  Healing outfit") with "Save to the Healing outfit" (`OutfitManager.KeepWorn`: each differing slot takes the worn
  piece, a pinned slot stays pinned) and "Put the outfit back" (`OutfitManager.PutBack`: every piece of it, not only
  the pinned ones). The per-slot "Keep" and "Put back" of finding 6 stay in the pick bar: one slot at a time.
- B. A skill without an outfit shows what "Make" would pick, dashed and marked "Proposed", on the figure and in the
  numbers ("not saved"); `OutfitManager.Propose` returns it without keeping anything.
- C. "All outfits" by the row: every outfit with its pieces as small pictures, pins, battle pet and its main number
  (Max heal for Healing, Armor for Defense and the gathering skills, Power for the rest); Open, and "Start another
  outfit from this" (`OutfitManager.Copy`: slots, pins and pet).
- D. The character's column has 1.75 parts to the bag's 1 (1.5 to 1 on middling widths); the Equipment bag keeps
  five columns beside the character.
- E. The Equipment bag has counts on its kind chips, a sort (by slot, best for the skill, rarity, name) and the legend.

**Calls the owner may want to change:** equip best on Now saves into the outfit that is on (so Now and the outfit
stay one); "Put the outfit back" also refills a slot whose piece the character may no longer wear, so it can change
more slots than the strip counted; the proposal is asked of the server again after every change on that tab; the
"Proposed" note shows on every proposed slot; the badges say "Changes" and "Not saved"; Defense's main number is
Armor; the copy leaves the role word ("!equip tank") behind; five bag columns at every width beside the character.

**Checked** on /dev/gear-preview with the review's steps at 1920, 1500, 1280 and 400 wide, a visitor and an empty
character. **Not checked:** signed in, the real database through OutfitService, a running game following these
changes, hover cards by a real mouse (shown by a synthetic pointer event), Firefox, Safari and a real phone.
