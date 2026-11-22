# Taming gear: the Beastmaster line

**Status: built 2026-11-22** (approved by the owner the same day). The owner: "do you think we should add in Taming
gear too? that helps out with taming/boosts your pet?", then "do it. also general improvement of pets/boosts to the
pets and stuff. but should still be combat related for both somewhat, because player would be using the gear and be
in combat at the same time. pet isnt the only thing in combat."

The other twelve lines are in `class-gear-design.md`; this is the thirteenth and follows their rules unless it says
otherwise here. Code: RavenNest `RavenNest.Models/ItemLines.cs` (the line, `TamingLevelFor`), `GearOutfit.cs` (the
words), `RavenNest.Combat/GearStats.cs` (`TameChance`), `PetSpeciesList.cs` (`Taming.Chance`), `TowerLoot.cs`
(`BeastmasterChance`), `RavenNest.BusinessLogic/Data/GameData.ClassGear.cs` (the 48 items), `BattlePetManager.cs`
(worn `PET_GATHERING`), `GameManager.RollTowerGear`; game `Core/EquipmentStats.cs` (`PetGear`),
`PlayerController.UpdateEquipmentEffect`, `BattlePetController.Refresh`, `PlayerBattlePets` (the tame roll),
`Assets/World/ClassGear/Resources/ClassGear/<tier>.mat`, `tools/items/class-gear-*.cs` and `class-gear-parts.txt`.

## The idea

Gear for the player who fights with a pet. It makes the **pet** better at whatever it does and keeps the
**wearer** a fighter: the set has combat numbers of its own that work with any weapon, since a tamer may swing a
sword, shoot or cast. What it gives up is the power bonus of a damage line. With a pet out it is a real choice beside
a damage set; without a pet it is plainly the weaker one (see "The simulator").

## The line

- **Line:** `ItemLine.Beastmaster` (13), eight tiers at levels 1, 30, 70, 120, 200, 400, 650, 900 **of Taming**, the
  first gear that Taming levels open. Six pieces a tier, two head pieces with the same stats: 48 items.
- **Tier names** (`ItemMaterial` values after Celestial, never renumbered): Fledgling, **Trapper**, Houndmaster,
  Falconer, Wildheart, Packlord, Primal, Wyrmkin. The spec's "Tracker" is the Ranger's second tier, so tier 2 is
  Trapper.
- **Pieces:** Feathered Hat (Hat) and Pelt Hood (HeadCovering), Hide Vest (chest), Handler Gloves, Hide Breeches
  (leggings), Tracking Boots. "Wyrmkin Hide Vest", "Trapper Pelt Hood".
- **Armor:** 60% of plate (the Ranger's factor). Head pieces give no armor health (a Hat and a HeadCovering, like
  the Ranger's).
- **Needs Taming.** The item rows have no Taming column, and a new column would have to reach every game and the
  website, so the rule is in the shared library: `ItemLines.TamingLevelFor(material, level)` is the item's level for a
  Beastmaster piece and 0 for anything else. The server (`PlayerInventory.CanEquipItem`), the game
  (`GameInventoryItem.CanBeEquipped`, `Inventory.CanEquipItem`, the missing-level reply "You require level 120
  Taming.", the hero inventory, `!req`) and the website (the Taming chip on the items and stash pages,
  `ItemService`'s requirement text) all read it. Every other requirement column is 0.
- **Made from** Leather times the tier (4 for the vest, 3 for the breeches, 2 for the head, gloves and boots) and one
  bar of the tier's metal, at Crafting = the tier's level, like the Ranger's leathers. A Houndmaster Hide Vest is 12
  Leather and a Rune Bar at Crafting 70.

### The look

Furs, hides, feathers and bone from the kit's parts, in earthy browns and greens, with the teal of the Beastmaster
tree (#3FD0C9, the tree's color on the website) coming in from tier 4 and gold at the top. Renders:
`docs/mockups/taming-gear-t1.jpg`, `-t4.jpg`, `-t8.jpg` (male and female, hat and hood, from behind, beside the
Ranger and the Barbarian of the same tier), `taming-gear-tiers.jpg` (the eight tiers) and
`taming-gear-icons-packlord.jpg`.

| Piece | Tiers 1-2 | Tiers 3-5 | Tiers 6-8 |
|---|---|---|---|
| Feathered Hat | a brimmed leather hat with a feather (hats 7) | and long feathers (head 11) | and a fan of feathers (head 4) |
| Pelt Hood | a wolf pelt hood (head coverings 9) with a feather sprig (12) | with long feathers (11) | with antlers (3) |
| Hide Vest | studded hide with a fur collar and a cross strap (torso 12), strapped sleeves (upper arms 5), no pauldrons | bone pauldrons (shoulders 0) | wolf heads on the shoulders (8) and a pelt on the back (back 3) |
| Handler Gloves | a falconer's leather glove (hands 6) with banded bracers (lower arms 16) | the same | the same |
| Hide Breeches | 1: trousers with a front flap (hips 27); 2-3: and a beast-calling horn (belt 11) | 4-5: horn and a belt bag (belt 2) | horn and belt bag |
| Tracking Boots | wrapped leather boots (legs 7) | the same | the same |

What keeps it apart from its neighbours: the Barbarian shows bare skin under a bear or wolf hood (the Pelt Hood is
the same wolf part, so it is never worn plain: feathers, then antlers); the Ranger is a slim green jerkin with a
hunter's cap; the Beastmaster is the only line in studded hide with a fur collar, a falconer's glove and a horn.
The hat (hats 7) and the wolf hood keep their texture's colors (brown leather, grey fur): the tier shows on the
vest, the flaps, the gloves and the boots. Hair that comes through hat 7 is swapped for a flat style while it
shows (`SyntyPlayerAppearance.HairThroughHat`, measured with `tools/items/class-gear-hair-under-hats.py`: the same
22 styles as the buckle hat's, near enough). Torso 12 leaves one shoulder bare; it is a skin part, so it takes the
player's own skin color (checked with a dark skin in the preview).

| Tier | cloth (flaps) | trim (stripes) | hide (vest, gloves, boots) | bone (apron, pauldrons) |
|---|---|---|---|---|
| 1 Fledgling | drab olive | sand | tan | pale bone |
| 2 Trapper | moss | straw | russet | bone |
| 3 Houndmaster | forest green | wheat | brown | bone |
| 4 Falconer | slate | teal | warm tan | bone |
| 5 Wildheart | leaf green | the tree's teal | dark hide | bone |
| 6 Packlord | wolf grey | the tree's teal | grey hide | white bone |
| 7 Primal | jungle teal-green | amber | bark | white bone |
| 8 Wyrmkin | deep teal | gold | muted teal scale | white bone |

The exact colors are the Beastmaster rows of `tools/items/class-gear-materials.cs` (its `only` setting writes just
some rows, so the other lines' materials are not rewritten).

## Stats

A full set (five pieces, one head piece), in the measure the other lines use. A set's total is a straight line from
tier 1 to tier 8 in halves; leggings carry 25%, head, gloves and boots 15% each and the chest the rest.

| For | Stat | Text key | Tier 1 | Tier 8 | What it does |
|---|---|---|---|---|---|
| You | Max health | `MAX_HEALTH_BONUS` | 1% | 8% | a share more of the whole pool, any weapon |
| You | Crit chance | `CRIT_CHANCE` | 0.5% | 3% | points on the chance, any weapon |
| Pet | Pet power | `PET_POWER` | 4% | **75%** | its blows, skills, damage over time and heals |
| Pet | Pet health | `PET_HEALTH` | 3% | 20% | its health pool |
| Pet | Pet recovery | `PET_RECOVERY` | 2% | 15% | a share off the 30 s it lies down after a knockout (75% at most with the tree) |
| Pet | Gatherer pet's skills | `PET_GATHERING` | 1% | 10% | a share stronger gather skills (the server rolls them) |
| Taming | Tame chance | `TAME_CHANCE` (new) | 2% | 15% | a share more of the chance, after a treat: 5.76% becomes 6.08% with 5.5% |

No melee, ranged or magic power bonus: that is the price. Pet power is the one number changed from the spec (3% to
20%): see the simulator below.

The pet stats are the same stats the Beastmaster tree gives (Fang and Claw, Thick Hide, Back on Its Feet, Keen
Senses) and add to them in the same places: the game keeps what the worn gear gives the pet apart from the tree's
passives (`EquipmentStats.PetGear`, made in `UpdateEquipmentEffect`), and `BattlePetController.Refresh` adds it to the
tree's numbers; a change of gear refreshes the pet and its packmate at once (`PlayerBattlePets.GearChanged`). The
server adds the worn `PET_GATHERING` to the tree's in `BattlePetManager.GatherBonusOf` (`WornPetStat`, which skips
pieces without the key before parsing: it runs every gathering round). Like the tree, the pet stats count only with
the new combat rules on.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Max health | Crit chance | Pet power | Pet health | Pet recovery | Pet gathering | Tame chance |
|---|---|---|---|---|---|---|---|---|---|
| 1 Fledgling | 1 | 4 / 12 / 5 / 6 / 6 | **1%** (0.4 / 0.3 / 0.1) | **0.5%** (0.1 / 0.1 / 0.1) | **4%** (1.2 / 1 / 0.6) | **3%** (1 / 0.8 / 0.4) | **2%** (0.6 / 0.5 / 0.3) | **1%** (0.4 / 0.3 / 0.1) | **2%** (0.6 / 0.5 / 0.3) |
| 2 Trapper | 30 | 8 / 27 / 11 / 14 / 14 | **2%** (0.6 / 0.5 / 0.3) | **1%** (0.4 / 0.3 / 0.1) | **14%** (4.2 / 3.5 / 2.1) | **5.5%** (1.7 / 1.4 / 0.8) | **4%** (1.2 / 1 / 0.6) | **2.5%** (0.7 / 0.6 / 0.4) | **4%** (1.2 / 1 / 0.6) |
| 3 Houndmaster | 70 | 18 / 48 / 23 / 30 / 27 | **3%** (1 / 0.8 / 0.4) | **1%** (0.4 / 0.3 / 0.1) | **24.5%** (7.3 / 6.1 / 3.7) | **8%** (2.4 / 2 / 1.2) | **5.5%** (1.7 / 1.4 / 0.8) | **3.5%** (1.1 / 0.9 / 0.5) | **5.5%** (1.7 / 1.4 / 0.8) |
| 4 Falconer | 120 | 44 / 77 / 48 / 50 / 53 | **4%** (1.2 / 1 / 0.6) | **1.5%** (0.5 / 0.4 / 0.2) | **34.5%** (10.3 / 8.6 / 5.2) | **10.5%** (3.1 / 2.6 / 1.6) | **7.5%** (2.3 / 1.9 / 1.1) | **5%** (1.6 / 1.3 / 0.7) | **7.5%** (2.3 / 1.9 / 1.1) |
| 5 Wildheart | 200 | 65 / 95 / 63 / 69 / 64 | **5%** (1.6 / 1.3 / 0.7) | **2%** (0.6 / 0.5 / 0.3) | **44.5%** (13.3 / 11.1 / 6.7) | **12.5%** (3.7 / 3.1 / 1.9) | **9.5%** (2.9 / 2.4 / 1.4) | **6%** (1.8 / 1.5 / 0.9) | **9.5%** (2.9 / 2.4 / 1.4) |
| 6 Packlord | 400 | 114 / 125 / 90 / 109 / 80 | **6%** (1.8 / 1.5 / 0.9) | **2.5%** (0.7 / 0.6 / 0.4) | **54.5%** (16.3 / 13.6 / 8.2) | **15%** (4.6 / 3.8 / 2.2) | **11.5%** (3.5 / 2.9 / 1.7) | **7.5%** (2.3 / 1.9 / 1.1) | **11.5%** (3.5 / 2.9 / 1.7) |
| 7 Primal | 650 | 197 / 215 / 155 / 188 / 138 | **7%** (2.2 / 1.8 / 1) | **2.5%** (0.7 / 0.6 / 0.4) | **65%** (19.6 / 16.3 / 9.7) | **17.5%** (5.3 / 4.4 / 2.6) | **13%** (4 / 3.3 / 1.9) | **8.5%** (2.5 / 2.1 / 1.3) | **13%** (4 / 3.3 / 1.9) |
| 8 Wyrmkin | 900 | 260 / 285 / 206 / 248 / 182 | **8%** (2.4 / 2 / 1.2) | **3%** (1 / 0.8 / 0.4) | **75%** (22.6 / 18.8 / 11.2) | **20%** (6 / 5 / 3) | **15%** (4.6 / 3.8 / 2.2) | **10%** (3 / 2.5 / 1.5) | **15%** (4.6 / 3.8 / 2.2) |

A set's total in bold, then one piece's share as (chest / leggings / each of head, gloves and boots).

### The simulator

`RavenNest.CombatSimulator taminggear [runs] [KEY=from:to ...]` (RavenNest `src/tools/RavenNest.CombatSimulator/
TamingGearScenarios.cs`): a fighter at the tier's level in every combat skill (tier 1 at level 10), with the standard
weapon of that level, in five pieces of either the style's damage line (Melee, Ranger, Mage) or the Beastmaster
line (each the plate piece of its slot times the line's armor factor, plus the line's set stats), against an enemy
that never falls (the standard melee kit at its level, 75% of its armor, half its offence), 200 fights of 120 s a
cell. The pet is the damage stand-in at the fighter's level (evolved at 30 and 100); the Beastmaster set's pet stats
reach it as the game gives them (`PetScenarios.Fight`, which now takes any owner and the pet bond). No skill tree.

With the spec's 3% to 20% pet power the Beastmaster wearer and pet fell behind the damage line with the same pet
from tier 5 on (0.86 to 0.89 at tier 8): at high levels a pet is a quarter of the damage, so 20% more of it is 5%
of the total, against the damage line's 14% to 16% power and the rest. As the spec says, pet power went up first:
4% to 75%. Tried: 4:60 (tier 8 at 0.92 to 0.96), 5:80 (tiers 4 and 5 ranged up to 1.07), 4:70, 4:75 (kept).

Damage per second, the Beastmaster set over the damage line (the pet's share is of the Beastmaster wearer's total):

| Tier | Level | Against | Alone (no pet) | With a damage pet | Pet's share |
|---|---|---|---|---|---|
| 1 | 10 | Melee | 0.99 | 0.99 | 23% |
| 1 | 10 | Ranger | 0.98 | 1.00 | 33% |
| 1 | 10 | Mage | 0.98 | 1.01 | 33% |
| 2 | 30 | Melee | 0.96 | 1.00 | 26% |
| 2 | 30 | Ranger | 0.97 | 1.02 | 38% |
| 2 | 30 | Mage | 0.97 | 1.05 | 38% |
| 3 | 70 | Melee | 0.95 | 1.00 | 27% |
| 3 | 70 | Ranger | 0.95 | 1.04 | 37% |
| 3 | 70 | Mage | 0.94 | 1.02 | 37% |
| 4 | 120 | Melee | 0.91 | 1.01 | 31% |
| 4 | 120 | Ranger | 0.91 | 1.05 | 39% |
| 4 | 120 | Mage | 0.90 | 1.03 | 39% |
| 5 | 200 | Melee | 0.88 | 1.00 | 32% |
| 5 | 200 | Ranger | 0.89 | 1.05 | 39% |
| 5 | 200 | Mage | 0.89 | 1.05 | 39% |
| 6 | 400 | Melee | 0.84 | 0.97 | 31% |
| 6 | 400 | Ranger | 0.87 | 1.02 | 36% |
| 6 | 400 | Mage | 0.85 | 1.02 | 35% |
| 7 | 650 | Melee | 0.80 | 0.96 | 31% |
| 7 | 650 | Ranger | 0.84 | 1.02 | 34% |
| 7 | 650 | Mage | 0.83 | 1.00 | 34% |
| 8 | 900 | Melee | 0.82 | 0.94 | 26% |
| 8 | 900 | Ranger | 0.83 | 1.00 | 29% |
| 8 | 900 | Mage | 0.81 | 0.95 | 29% |

With a damage pet every cell is within 0.94 to 1.05; without one the set is clearly behind from tier 4 (0.80 to
0.91), and at tiers 1 to 3 a little behind (0.94 to 0.99), because the damage lines' own bonuses are small there.
The Melee line is the strongest damage line, so melee sits at the low end. A tamer with a tank or healer pet gets
no damage back for the power: they get a sturdier pet (health, recovery) and the same wearer numbers. The tree's
PET_POWER adds to the gear's, so with Fang and Claw and Alpha the gear's share of the pet's power is a little less.
The simulator's Beastmaster numbers are a copy of `GameData.ClassGear`'s: keep the two the same.

## How it fits the rest

- **Auto-equip:** `ItemLines.Fits(Skill.Taming)` is the line and no combat skill names it, so "equip best" for
  Attack, Ranged and the rest never picks it by itself (a worn piece is still kept by a skill that names no line,
  as every line's is). Nobody trains Taming as a task (`!tame` keeps the combat skill), so the fit is for words and
  outfits only.
- **`!equip beastmaster`**, `!equip tamer`, `!equip taming` and `!equip beastmaster line`: the line by name (armor
  only, the best piece per slot), never a role. "You put on your best beastmaster gear: Houndmaster."
- **Outfits:** pinned like any gear (`!outfit pin`), so a skill's outfit can be the Beastmaster set.
- **Tower chests:** a fighter with a battle pet out (an active pet whose item is not gone) gets a Beastmaster piece
  in place of their skill's line one time in five (`TowerLoot.BeastmasterChance` = 0.2): about six pieces over the
  30 floors, a set's worth, while most of what they get stays their skill's line. The tier is lowered to what their
  Taming allows. The line is never picked as "any line" for someone whose skill and gear name none.
- **Tame chance:** `Taming.Chance(level, species, treat, gearBonus)`; the game rolls it with the worn TAME_CHANCE
  (`PlayerBattlePets.GearTameChance`) and `!tame` says the chance with it in.
- **Details window, pet view:** "Power from you" (the tree's and the gear's PET_POWER together), "Up after a
  knockout" and "Tame chance from gear". Item tooltips and the website's item chips list every stat by its name
  (Pet Power, Tame Chance).
- **Supply:** the gatherer stat is up to 10% of Forage's 5% and the other gather skills, on top of the Gathering and
  Beastmaster trees; small, and on the list in `trade-skills-design.md`.
- **Older games (0.9.6)** show the pieces in the default look, read none of the stats and do not know the Taming
  requirement: they would put a piece on locally, and the server refuses it below the Taming level.

## Built

- RavenNest 353c985d (the line, items, stats, gate, tame chance, worn gathering, tower, words, website chips, tests),
  08d6eadd (the simulator), 3bdce52d (the looks and the 48 website icons).
- Game 93ca03e8 (the libraries, gate, pet and tame code, details window, materials, parts, tools, renders) and this
  doc's commit.
- Tests: RavenNest `Tests/RavenNest.UnitTests/TamingGearTests.cs` (materials after Celestial and nothing moved, the
  fit rule, the Taming gate on the server, the words, a set's stats add up, six pieces a tier with fixed ids) and
  `src/RavenNest.Combat.Tests/TamingGearTests.cs` (TAME_CHANCE read and written, the chance a share more, the tower's
  one in five).
- Icons: 48, rendered with `class-gear-icons.cs` into the game's `Assets/Resources/Items` (not versioned) and the
  website's `wwwroot/imgs/items`.

## Tested

Played 2026-11-22 on the local server with the development character (Taming 76, Attack 999; Elder Atlarus, a
Phantom Ring, the Disco Raven out, a Support pet), through the real chat handlers with the bot cut off:

- Given a Houndmaster set (tier 3), a Falconer Hide Vest (tier 4) and what a Fledgling vest takes.
- `!equip beastmaster`: "You put on your best beastmaster gear: Houndmaster. It stays your outfit until you use
  !equip all." The game and the server had the same 13 items equipped. `!equip tamer` and `!equip Taming`: "You
  already wear your best beastmaster gear: Houndmaster."
- The pet at once: health 870 to 940 (8%), power x1.000 to x1.245 (24.5%), up after 30 s to 28.4 s (5.5%); the
  owner's health 10,067 to 8,722 (less armor health from 60% armor, 3% more of the pool) and 1% crit from gear.
- The pet's blows, counted in real fights on Home against metalons, 90 s each: 18 hits at 33.9 a hit without the
  set (the outfit of Attack, Nightfall, was on), 18 hits at 40.4 with it (x1.19 against x1.245 expected; the metalons differ). On Ironhill against dragons 60
  s each: 8 hits at 24.0, then 3 hits at 35.7 (too few to read).
- The Falconer Hide Vest stayed off at Taming 76 (its reply goes to the bot, which was cut off: not read).
- `!tame metalon`: "5.8% each" without the set, "6.1% each" in it (5.76% and 5.5% more); a roll was logged at
  6.077% with the gear and tamed a Metalon (one try).
- `!outfit pin` with the set on: "Pinned as your Attack outfit: Houndmaster (beastmaster)." `!equip all`, a change
  to Defense and back to Attack all kept the set on. After `!outfit unpin`, `!equip all` for Attack put on the
  Nightfall (Rogue) set and no Beastmaster piece.
- `!craft fledgling hide vest`: "You have successfully crafted a Fledgling Hide Vest" (4 Leather and a Bronze Bar).
  `!req houndmaster hide vest`: "requires level 70 Crafting and Rune Bar 0/1, Leather 0/12".
- Left as found: Elder Atlarus and the ring on, the Disco Raven out, Ironhill, training Crafting; the database put
  back afterwards (the eight pieces removed, the two tamed Metalons removed, the outfits text set back to what it
  was at the start: the character had outfits for four skills, kept from earlier tests).

Not verified:
- The set in the game's own light at a normal camera distance (the in-game capture was from far away); the looks are
  checked in the editor preview through the game's equip code, as the other lines' were.
- A damage pet with the set in play (the character's pet is a Support pet), PET_RECOVERY on a real knockout,
  PET_GUARD (no Beastmaster piece gives it), and worn PET_GATHERING in a real gathering round.
- A Beastmaster piece from a real tower chest; the missing-Taming reply in chat; the website's Taming chip signed in;
  a 0.9.6 game; tiers above 3 worn in play.

## Open, for the owner

- Pet power at 75% a set is far above the spec's 20%: the simulator needed it to keep the line beside the damage
  lines with a damage pet out. With the tree's PET_POWER on top a tamer's pet hits hard; a tank or healer pet only
  heals or soaks more with it.
- Tier 2 is Trapper, not Tracker (the Ranger's).
- The Pelt Hood is the Barbarian's wolf hood part with feathers or antlers on it; the Feathered Hat stays brown at
  every tier (its texture), and the vest leaves one shoulder bare.
- One Beastmaster piece in five for fighters with a pet out; never as "any line".
- The Taming requirement is a rule in the library, not an item column, so the website shows it on the items and
  stash pages but not yet on the new gear pages (the gear pages agent's `PlayerGear.razor.cs` lists the columns).
