# Class gear

Part of [combat-v2-design.md](combat-v2-design.md), v0.9.7. Plate (the 28 metal tiers, Bronze to Elder Atlarus) is
armor and nothing else, so every path wore the same thing. Class gear is armor for a path: five lines that trade armor
for the stats of what a player trains, and look the part. Approved 2026-11-21, to be expanded later (more lines,
socketed tower drops).

This file says what exists now. Server: `RavenNest.BusinessLogic/Data/GameData.ClassGear.cs` makes all of it at
startup from two tables (the pieces, and what each line gives); the code is the record, a row that differs is put right
at the next start. Shared: `RavenNest.Models/ItemLines.cs` (lines, tiers, levels, armor factors) and
`RavenNest.Models/GearScore.cs` (the auto-equip rule).

## The lines

| Line | For those who train | Armor next to plate | Needs level in | Gives |
|---|---|---|---|---|
| Mage | Magic | 40% | Magic (Healing passes it) | magic power, cast speed, accuracy |
| Healer | Healing | 40% | Magic (Healing passes it) | healing power, heal shield, cooldown reduction, max health |
| Ranger | Ranged | 60% | Ranged | ranged power, crit chance, attack speed, evasion |
| Melee | Attack or Strength | 75% | Attack | melee power, crit damage, attack speed, armor penetration |
| Tank | Defense | 100% | Defense | max health, threat, damage reduction |

Eight tiers a line. The tier's name is its material (`ItemMaterial`, 40 values after `ElderAtlarus`) and the first word
of every piece: "Adept Robe", "Tracker Jerkin". The level a tier needs, tier 1 first: 1, 30, 70, 120, 200, 400, 650,
900, the levels of Bronze/Iron, Mithril, Rune, Abraxas, Lionsbane, Atlarus, Elder Mithril and Elder Ether.

Six pieces a tier, 240 items in all (5 x 8 x 6): two head pieces to choose a look from (same stats, usable for
transmogrification), chest, gloves, leggings and boots. Category Armor, no prefabs: a piece is character parts
(`MaleModelId` / `FemaleModelId`, the game's part indices) in the tier's material.

| Line | Head pieces | Chest | Gloves | Leggings | Boots |
|---|---|---|---|---|---|
| Mage | Wizard Hat (Hat), Hood (HeadCovering) | Robe | Gloves | Robe Skirt | Boots |
| Healer | Cowl, Cap (HeadCovering) | Vestment | Gloves | Tabard | Boots |
| Ranger | Cap (Hat), Hood (HeadCovering) | Jerkin | Gloves | Trousers | Boots |
| Melee | Helm, Coif (HeadCovering) | Hauberk | Gauntlets | Tassets | Boots |
| Tank | Greathelm, Visor Helm (Helmet) | Cuirass | Gauntlets | Greaves | Sabatons |

An item's id is fixed (made from line, tier and the piece's key: `GameData.ClassGearItemId`), so it is the same in
every database and a piece can be renamed, or a head piece given another part or type, by changing its row.

Only the Tank's helms are of type Helmet. The other head pieces are hats and head coverings, and those two types
take no gems and give no armor health (`ItemExtension.TryGetGemSlot`, `CombatMath.IsArmorPiece`): a small price for
the look, and the same for both head pieces of a line.

## Attachments: higher tiers wear more

The tiers of a line differ by color (the material) and, since 2026-11-21, by silhouette: tiers 1 and 2 are plain, the
middle tiers gain one or two things, tiers 7 and 8 the most. What is added comes from the character kit's attachment
parts: plumes, wings, antlers, crowns and crosses on the head, pouches, bottles, a scroll and a rope on the belt,
pauldrons and knee pads, and a cloak, a pack or a pelt on the back.

| Line | Piece | Tiers 1-2 | Tiers 3-5 | Tiers 6-8 |
|---|---|---|---|---|
| Mage | Wizard Hat / Hood | plain | a crystal pin (8) | a crown band / circlet (7) |
| Mage | Robe | plain | plain | a long trimmed cloak (back 4) |
| Mage | Robe Skirt | 1 plain; 2: a scroll (belt 7) | 3: scroll; 4-5: scroll and a pouch (belt 0) | scroll and pouch |
| Healer | Cowl / Cap | plain | a cross (10) | 6: a crown (7); 7-8: wings (2) |
| Healer | Tabard | 1 plain; 2: a healer's kit (belt 2) | 3: kit; 4-5: kit and a bottle (belt 8) | kit and bottle |
| Ranger | Cap | plain | 3-4 plain; 5: long feathers (11) | long feathers |
| Ranger | Hood | plain | a feather (12) | antlers (3) |
| Ranger | Jerkin | plain | 4: leather pauldrons (15); 5: and a pack (back 1) | 6: pack; 7-8: a bedroll pack (back 0) |
| Ranger | Trousers | 1 plain; 2: a coiled rope (belt 3) | 3: rope; 4-5: rope and knee guards (4) | rope, knee guards and a game bag (belt 6) |
| Melee | Helm | plain | a plume (1) | horns (6) |
| Melee | Coif | plain | a spike (8) | a crown (7) |
| Melee | Hauberk | round pauldrons (13) | 3: round; 4-5: spiked pauldrons (9) | 6: spiked; 7-8: wolf heads (8) and a pelt (back 3) |
| Melee | Tassets | 1 plain; 2: a coin pouch (belt 0) | 3: pouch; 4-5: pouch, a sheath (belt 9) and knee cops (8) | pouch, sheath and knee cops |
| Tank | Greathelm | plain | a great plume (0) | 6: plume and cross (10); 7: wings (2); 8: wings and a crown (7) |
| Tank | Visor Helm | plain | a tail plume (1) | 6: plume and cross; 7: wings; 8: wings and a crown |
| Tank | Cuirass | flat pauldrons (12) | round pauldrons (10) | tall pauldrons (11) and a heraldic cloak (back 9) |
| Tank | Greaves | round knee cops (7) | shield knee cops (6) | shield knee cops |

Gloves, boots and the healer's vestment are the same in every tier. The numbers are the part indices in the model ids.
The exact ids of every tier are in `tools/items/class-gear-parts.txt` (the game's table: icons and previews are made
from it) and in the server's `ClassGearPieces`, where a piece says its tier 1 look and `.From(tier, ids, mask)` for the
tiers that have more. The two must agree; item ids do not change with the look (they are made from line, tier and key).

### Where the parts go in the model ids

| Item type | Model ids, in order | New in 0.9.7 |
|---|---|---|
| Helmet, Hat, Mask, HeadCovering | the piece, then any number of head attachments (0-12) | nothing: this is how the metal helmets carry their horns |
| Chest | torso, shoulder pads, upper arms, elbow pads, **back piece** (0-9) | the fifth position |
| Leggings | hips, knee pads, **belt parts** (0-11), any number | the third position and on |
| Gloves, Boots | as before | nothing |

The belt parts (`Chr_HipsAttachment_01` to `12`) and the back parts (`Chr_BackAttachment_01` to `10`) are in the Player
prefab but in none of `SyntyPlayerAppearance`'s lists. The game finds them by where they are, next to the capes, the
first time an item wears one (`FindLooseAttachments`); the prefab is unchanged. An index is the part's number less one,
as for every attachment. Index 0 to 5 hang on the right hip, 6 to 11 on the left, so one of each side fits together.

Rules the code keeps:
- **One owner per slot.** The belt belongs to the leggings and the back to the chest: putting on other leggings or
  another chest takes the old one's parts off, with or without a full rebuild, and so does taking the piece off.
- **A cape wins the back, unless it is hidden.** A player whose cape shows (their own or their clan's) keeps it and the
  armor's back piece stays off: both hang from the same bone and cut through each other. `!toggle cape` hides the
  cape, as `!toggle helmet` hides the helmet: then the chest's back piece shows, or a bare back when the chest has
  none, and `!toggle cape` again brings the cape back with its clan image. The default is shown, so a clan member sees
  no cloak, pack or pelt until they hide their cape. See "The cape toggle" below.
- **The helmet toggle** hides the head piece with everything on it.
- **Transmogrification** takes all of it from the look item: its parts with their attachments, its material and its mask.
- **The item on its own** (`SyntyPlayerAppearance.Get`: treasure chests, the icon scene) has its attachments too. The
  icons leave a chest's back piece out: from the front it is behind the torso, and a cloak would shrink the chest to a
  third of its picture.
- **An index the model set does not have** is skipped, in every position.
- Attachments are drawn in the tier's material like the rest of the piece. Feathers, wolf heads, the green bottle and
  the bedroll keep the colors of their texture.

### The cape toggle

- **The choice** is the character's, kept by the server (a user property for each character) and sent with every
  appearance as `SyntyAppearance.CapeHidden`. The game holds it as `SyntyPlayerAppearance.CapeVisible`, set wherever
  an appearance is applied (`SetAppearance`: joining, and every later appearance update), and hands it back in
  `ToSyntyAppearanceData`, so dressing a player again from their own appearance (another clan cape shape) keeps it.
- **The command:** the bot sends `toggle_cape`; `ToggleCapeVisibility` (the packet handler) tells the server
  (`equipment.toggle-cape`, V2 only: there is no older web call, and a server without it changes nothing), toggles at
  once and answers in chat: hidden with the armor's own cloak or pack showing, hidden with nothing in its place, shown
  again, or no cape to hide or show (the choice is still kept, for a cape the player gets later).
- **Hidden** means no cape is drawn, own or clan. `Cape` still says which one the player has, the clan's shape and
  image are still kept up to date on the cape's material (without dressing the player again), and
  `UpdateAppearance` takes the cape off after every rebuild. So equipping, unequipping, the helmet toggle,
  transmogrification, a clan's new cape shape or image, and joining or leaving a clan do not bring it back.
- **Toggling** dresses the player again (`PlayerEquipment.UpdateAppearance`: everything off, the worn items on, one new
  combined mesh), because capes and back pieces are part of the combined mesh.
- Nothing else draws a second copy of a player: the details window and hero mode show the player itself.

Looked at on both bodies and left out: the other cloaks (back 5 to 8: plain, one color with the robe from behind) and
the tattered one (back 2); the fan of feathers, the helmet fin and the single big feather (head 4, 5, 9: too close to
the plumes); the other pauldrons (one change of pauldron a line was enough, and the metal sets use several); the other
knee pads; the elbow pads other than the tank's (too small to tell apart from the game's camera); the round bottle,
the horn and the small bags on the belt (belt 1, 4, 5, 10, 11: lost against the cloth, or a third thing on one hip).
The tank's belt stays empty: its helm, pauldrons and cloak already change.

To try parts: `tools/items/class-gear-preview.cs` dresses a character through the game's own equip code, with
`custom <ItemType> <Material> <ids joined by +> [mask flags joined by +]` for a piece that is in no table.

## Stats

**Armor** (the classic `ArmorPower` column) is the line's factor times the armor of the best plate piece of that slot
at that Defense level, rounded up. Head pieces use the helmet's number. Taken from the metal sets as they are when the
server starts (a point on the line between two sets if a tier's level ever falls between them). Tier 1 is measured
against Iron: Bronze and Iron both need Defense 1, and Iron is the better.

**The path's stats** are in `ExtraStats`, in the text `RavenNest.Combat.GearStats` reads
(`MAGIC_POWER_BONUS:1.2%;CAST_SPEED:0.6%;ACCURACY_BONUS:0.6%`). A full set is five pieces (one head piece). A set's
total grows in a straight line from tier 1 to tier 8, in halves of a percent; leggings carry 25% of it, head, gloves
and boots 15% each, in tenths of a percent, and the chest what is left (about 30%), so five pieces add up to the total
exactly. No element stats: gems and the themed metal sets do those.

What each stat does, as the game applies it (`CombatRules.Sheet`, `GearStats.ApplyExtras`):

| Stat | Text key | What a "+2%" is |
|---|---|---|
| Magic / ranged / melee / healing power | `MAGIC_POWER_BONUS` and so on | 2% more of the power your gear gives that style (the weapon's, mostly). Damage follows power less than one to one at low levels and nearly so at high ones. Healing power is the staff's magic power. |
| Accuracy | `ACCURACY_BONUS` | 2% more of the accuracy your gear gives (every style; a mage's is the staff's). |
| Cast speed, attack speed | `CAST_SPEED`, `ATTACK_SPEED` | spells and heals, or swings, 2% more often. |
| Crit chance | `CRIT_CHANCE` | 2 points on the chance (5% becomes 7%). |
| Crit damage | `CRIT_DAMAGE` | 2 points on the multiplier (150% becomes 152%). |
| Armor penetration | `ARMOR_PENETRATION` | your blows ignore 2% of the target's armor (75% at most). |
| Heal shield | `HEAL_SHIELD` | a heal also shields its target for 2% of the heal. |
| Cooldown reduction | `COOLDOWN_REDUCTION` | combat skills come back 2% sooner (30% at most, gems included). |
| Max health | `MAX_HEALTH_BONUS` | 2% more of the whole pool, Vitality's and gear's. |
| Threat | `THREAT` | everything you do builds 2% more threat. |
| Damage reduction | `DAMAGE_REDUCTION` | 2% off all damage taken (50% at most). |
| Evasion | `EVASION` | a rating, not a share: see below. |

Two stats are not what the spec named, because the named ones do not exist as gear stats:
- **Magic accuracy as a share** does not exist (only the flat `MAGIC_ACCURACY`): the Mage line has `ACCURACY_BONUS`,
  a share of all gear accuracy. A mage's accuracy is the staff's, so it comes to the same.
- **Evasion is a rating** (2 per Defense level plus gear; an attacker hits half the time against an equal rating).
  The Ranger line's "1% to 6%" is given as the rating that takes about that many points off the hit chance of an enemy
  of the tier's level: 4 x level x the share, at least 1 (`GameData.ClassGearEvasion`).

The tables: a set's total in bold, then one piece's share as (chest / leggings / each of head, gloves and boots).

### Mage

Armor is 40% of plate. Pieces: Wizard Hat, Hood, Robe, Gloves, Robe Skirt, Boots.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Magic power | Cast speed | Accuracy |
|---|---|---|---|---|---|
| 1 Apprentice | 1 | 3 / 8 / 3 / 4 / 4 | **2%** (0.6 / 0.5 / 0.3) | **1%** (0.4 / 0.3 / 0.1) | **1%** (0.4 / 0.3 / 0.1) |
| 2 Adept | 30 | 6 / 18 / 8 / 9 / 10 | **4%** (1.2 / 1 / 0.6) | **2%** (0.6 / 0.5 / 0.3) | **2%** (0.6 / 0.5 / 0.3) |
| 3 Conjurer | 70 | 12 / 32 / 16 / 20 / 18 | **6%** (1.8 / 1.5 / 0.9) | **3%** (1 / 0.8 / 0.4) | **3%** (1 / 0.8 / 0.4) |
| 4 Sorcerer | 120 | 30 / 52 / 32 / 34 / 36 | **8%** (2.4 / 2 / 1.2) | **4%** (1.2 / 1 / 0.6) | **4%** (1.2 / 1 / 0.6) |
| 5 Warlock | 200 | 44 / 63 / 42 / 46 / 43 | **10%** (3 / 2.5 / 1.5) | **5%** (1.6 / 1.3 / 0.7) | **5%** (1.6 / 1.3 / 0.7) |
| 6 Archmage | 400 | 76 / 84 / 60 / 73 / 54 | **12%** (3.6 / 3 / 1.8) | **6%** (1.8 / 1.5 / 0.9) | **6%** (1.8 / 1.5 / 0.9) |
| 7 Voidweaver | 650 | 131 / 144 / 104 / 125 / 92 | **14%** (4.2 / 3.5 / 2.1) | **7%** (2.2 / 1.8 / 1) | **7%** (2.2 / 1.8 / 1) |
| 8 Starcaller | 900 | 174 / 190 / 137 / 165 / 122 | **16%** (4.8 / 4 / 2.4) | **8%** (2.4 / 2 / 1.2) | **8%** (2.4 / 2 / 1.2) |

### Healer

Armor is 40% of plate. Pieces: Cowl, Cap, Vestment, Gloves, Tabard, Boots.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Healing power | Heal shield | Cooldown reduction | Max health |
|---|---|---|---|---|---|---|
| 1 Acolyte | 1 | 3 / 8 / 3 / 4 / 4 | **2%** (0.6 / 0.5 / 0.3) | **1%** (0.4 / 0.3 / 0.1) | **1%** (0.4 / 0.3 / 0.1) | **1%** (0.4 / 0.3 / 0.1) |
| 2 Mender | 30 | 6 / 18 / 8 / 9 / 10 | **4%** (1.2 / 1 / 0.6) | **2%** (0.6 / 0.5 / 0.3) | **1.5%** (0.5 / 0.4 / 0.2) | **1.5%** (0.5 / 0.4 / 0.2) |
| 3 Cleric | 70 | 12 / 32 / 16 / 20 / 18 | **6%** (1.8 / 1.5 / 0.9) | **3%** (1 / 0.8 / 0.4) | **2.5%** (0.7 / 0.6 / 0.4) | **2%** (0.6 / 0.5 / 0.3) |
| 4 Priest | 120 | 30 / 52 / 32 / 34 / 36 | **8%** (2.4 / 2 / 1.2) | **4%** (1.2 / 1 / 0.6) | **3%** (1 / 0.8 / 0.4) | **2.5%** (0.7 / 0.6 / 0.4) |
| 5 Hierophant | 200 | 44 / 63 / 42 / 46 / 43 | **10%** (3 / 2.5 / 1.5) | **5%** (1.6 / 1.3 / 0.7) | **4%** (1.2 / 1 / 0.6) | **3.5%** (1.1 / 0.9 / 0.5) |
| 6 Saint | 400 | 76 / 84 / 60 / 73 / 54 | **12%** (3.6 / 3 / 1.8) | **6%** (1.8 / 1.5 / 0.9) | **4.5%** (1.3 / 1.1 / 0.7) | **4%** (1.2 / 1 / 0.6) |
| 7 Seraph | 650 | 131 / 144 / 104 / 125 / 92 | **14%** (4.2 / 3.5 / 2.1) | **7%** (2.2 / 1.8 / 1) | **5.5%** (1.7 / 1.4 / 0.8) | **4.5%** (1.3 / 1.1 / 0.7) |
| 8 Lightbringer | 900 | 174 / 190 / 137 / 165 / 122 | **16%** (4.8 / 4 / 2.4) | **8%** (2.4 / 2 / 1.2) | **6%** (1.8 / 1.5 / 0.9) | **5%** (1.6 / 1.3 / 0.7) |

### Ranger

Armor is 60% of plate. Pieces: Cap, Hood, Jerkin, Gloves, Trousers, Boots.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Ranged power | Crit chance | Attack speed | Evasion (rating) |
|---|---|---|---|---|---|---|
| 1 Scout | 1 | 4 / 12 / 5 / 6 / 6 | **2%** (0.6 / 0.5 / 0.3) | **0.5%** (0.1 / 0.1 / 0.1) | **1%** (0.4 / 0.3 / 0.1) | **1** (1 / 0 / 0) |
| 2 Tracker | 30 | 8 / 27 / 11 / 14 / 14 | **3.5%** (1.1 / 0.9 / 0.5) | **1%** (0.4 / 0.3 / 0.1) | **1.5%** (0.5 / 0.4 / 0.2) | **2** (1 / 1 / 0) |
| 3 Hunter | 70 | 18 / 48 / 23 / 30 / 27 | **5.5%** (1.7 / 1.4 / 0.8) | **1.5%** (0.5 / 0.4 / 0.2) | **2.5%** (0.7 / 0.6 / 0.4) | **7** (2 / 2 / 1) |
| 4 Stalker | 120 | 44 / 77 / 48 / 50 / 53 | **7%** (2.2 / 1.8 / 1) | **2%** (0.6 / 0.5 / 0.3) | **3%** (1 / 0.8 / 0.4) | **14** (4 / 4 / 2) |
| 5 Pathfinder | 200 | 65 / 95 / 63 / 69 / 64 | **9%** (2.8 / 2.3 / 1.3) | **2.5%** (0.7 / 0.6 / 0.4) | **4%** (1.2 / 1 / 0.6) | **32** (9 / 8 / 5) |
| 6 Warden | 400 | 114 / 125 / 90 / 109 / 80 | **10.5%** (3.1 / 2.6 / 1.6) | **3%** (1 / 0.8 / 0.4) | **4.5%** (1.3 / 1.1 / 0.7) | **72** (21 / 18 / 11) |
| 7 Windrunner | 650 | 197 / 215 / 155 / 188 / 138 | **12.5%** (3.7 / 3.1 / 1.9) | **3.5%** (1.1 / 0.9 / 0.5) | **5.5%** (1.7 / 1.4 / 0.8) | **143** (44 / 36 / 21) |
| 8 Hawkeye | 900 | 260 / 285 / 206 / 248 / 182 | **14%** (4.2 / 3.5 / 2.1) | **4%** (1.2 / 1 / 0.6) | **6%** (1.8 / 1.5 / 0.9) | **216** (66 / 54 / 32) |

### Melee

Armor is 75% of plate. Pieces: Helm, Coif, Hauberk, Gauntlets, Tassets, Boots.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Melee power | Crit damage | Attack speed | Armor penetration |
|---|---|---|---|---|---|---|
| 1 Footman | 1 | 5 / 15 / 6 / 8 / 8 | **2%** (0.6 / 0.5 / 0.3) | **3%** (1 / 0.8 / 0.4) | **1%** (0.4 / 0.3 / 0.1) | **2%** (0.6 / 0.5 / 0.3) |
| 2 Mercenary | 30 | 10 / 33 / 14 / 17 / 18 | **3.5%** (1.1 / 0.9 / 0.5) | **5.5%** (1.7 / 1.4 / 0.8) | **1.5%** (0.5 / 0.4 / 0.2) | **3.5%** (1.1 / 0.9 / 0.5) |
| 3 Duelist | 70 | 23 / 60 / 29 / 37 / 34 | **5.5%** (1.7 / 1.4 / 0.8) | **8%** (2.4 / 2 / 1.2) | **2%** (0.6 / 0.5 / 0.3) | **5%** (1.6 / 1.3 / 0.7) |
| 4 Reaver | 120 | 55 / 96 / 60 / 63 / 66 | **7%** (2.2 / 1.8 / 1) | **10.5%** (3.1 / 2.6 / 1.6) | **2.5%** (0.7 / 0.6 / 0.4) | **6.5%** (1.9 / 1.6 / 1) |
| 5 Champion | 200 | 81 / 118 / 79 / 87 / 80 | **9%** (2.8 / 2.3 / 1.3) | **12.5%** (3.7 / 3.1 / 1.9) | **3.5%** (1.1 / 0.9 / 0.5) | **7.5%** (2.3 / 1.9 / 1.1) |
| 6 Warlord | 400 | 143 / 156 / 113 / 136 / 100 | **10.5%** (3.1 / 2.6 / 1.6) | **15%** (4.6 / 3.8 / 2.2) | **4%** (1.2 / 1 / 0.6) | **9%** (2.8 / 2.3 / 1.3) |
| 7 Bloodsworn | 650 | 246 / 269 / 194 / 234 / 172 | **12.5%** (3.7 / 3.1 / 1.9) | **17.5%** (5.3 / 4.4 / 2.6) | **4.5%** (1.3 / 1.1 / 0.7) | **10.5%** (3.1 / 2.6 / 1.6) |
| 8 Godslayer | 900 | 325 / 356 / 257 / 309 / 228 | **14%** (4.2 / 3.5 / 2.1) | **20%** (6 / 5 / 3) | **5%** (1.6 / 1.3 / 0.7) | **12%** (3.6 / 3 / 1.8) |

### Tank

Armor is 100% of plate. Pieces: Greathelm, Visor Helm, Cuirass, Gauntlets, Greaves, Sabatons.

| Tier | Level | Armor head / chest / gloves / leggings / boots | Max health | Threat | Damage reduction |
|---|---|---|---|---|---|
| 1 Guard | 1 | 6 / 20 / 7 / 10 / 10 | **2%** (0.6 / 0.5 / 0.3) | **5%** (1.6 / 1.3 / 0.7) | **1%** (0.4 / 0.3 / 0.1) |
| 2 Sentinel | 30 | 13 / 44 / 18 / 22 / 23 | **3.5%** (1.1 / 0.9 / 0.5) | **8.5%** (2.5 / 2.1 / 1.3) | **1.5%** (0.5 / 0.4 / 0.2) |
| 3 Defender | 70 | 30 / 80 / 38 / 49 / 45 | **5%** (1.6 / 1.3 / 0.7) | **12%** (3.6 / 3 / 1.8) | **2%** (0.6 / 0.5 / 0.3) |
| 4 Bastion | 120 | 73 / 128 / 80 / 83 / 88 | **6.5%** (1.9 / 1.6 / 1) | **15.5%** (4.7 / 3.9 / 2.3) | **2.5%** (0.7 / 0.6 / 0.4) |
| 5 Juggernaut | 200 | 108 / 157 / 105 / 115 / 106 | **7.5%** (2.3 / 1.9 / 1.1) | **19.5%** (5.9 / 4.9 / 2.9) | **3.5%** (1.1 / 0.9 / 0.5) |
| 6 Templar | 400 | 190 / 208 / 150 / 181 / 133 | **9%** (2.8 / 2.3 / 1.3) | **23%** (7 / 5.8 / 3.4) | **4%** (1.2 / 1 / 0.6) |
| 7 Ironclad | 650 | 327 / 358 / 258 / 312 / 229 | **10.5%** (3.1 / 2.6 / 1.6) | **26.5%** (7.9 / 6.6 / 4) | **4.5%** (1.3 / 1.1 / 0.7) |
| 8 Aegis | 900 | 433 / 474 / 342 / 412 / 303 | **12%** (3.6 / 3 / 1.8) | **30%** (9 / 7.5 / 4.5) | **5%** (1.6 / 1.3 / 0.7) |

## How it is made

Two new cloths, one of each for every tier (the higher tiers take more of it):

| Item | From | How |
|---|---|---|
| Flax | Farming level 5 | a farming drop like any crop (20 s cooldown, 15% a round and more with level) |
| Linen | 2 Flax | Crafting level 1 |
| Hide | training a combat skill | see below |
| Leather | 2 Hide | Crafting level 1 |

**Hide comes from fighting.** The server never saw single kills on the islands, so Hide is given the way every other
resource is: a resource drop of the Slayer skill (`ResourceItemDrop`, Slayer level 1, 20 s cooldown, 20% a round and
more with Slayer level), rolled every gathering round (10 s) while the character's task is Fighting
(`FightingTaskProcessor`, which did nothing before). Not in raids, dungeons, the arena, the onsen or on the ferry
(those never get that far). `!req hide` in the game answers "Hide requires level 1 Slayer"; the website says
"Fighting, Slayer level 1".

A piece is crafted at **Crafting level = the tier's level** and always succeeds, like the metal tiers. Every recipe
takes the bar of the tier's level: Bronze, Mithril, Rune, Abraxas, Lionite, Atlarus, Elder Mithril, Elder Ethereum.

| Line | Chest | Leggings | Head, gloves, boots (each) |
|---|---|---|---|
| Mage, Healer | 4 x tier Linen + 1 bar | 3 x tier Linen + 1 bar | 2 x tier Linen + 1 bar |
| Ranger | 4 x tier Leather + 1 bar | 3 x tier Leather + 1 bar | 2 x tier Leather + 1 bar |
| Melee | 4 bars + tier Leather | 3 bars + tier Leather | 2 bars + tier Leather |
| Tank | 6 bars + tier Leather | 5 bars + tier Leather | 3 bars + tier Leather |

A Sorcerer Robe (tier 4) is 16 Linen and 1 Abraxas Bar at Crafting 120; an Aegis Cuirass (tier 8) is 6 Elder Ethereum
Bars and 8 Leather at Crafting 900.

To know: the bars of tiers 2 to 5 need a higher Crafting level than the piece does (a Mithril Bar is Crafting 50, the
tier 2 pieces Crafting 30), so until then a crafter needs the bar from someone else.

No vendor buy price. The sell price follows the recipe as for everything crafted (`ApplyVendorPrices`: what the
ingredients sell for, plus 5%), so making a piece and selling it never pays; cloth pieces sell for less than plate
of their level because less metal goes into them. Flax sells for 5, Hide 5, Linen and Leather 10.

Socketed class gear as tower drops comes later.

## Auto-equip respects the path

"Equip the best" (`!equip all`, an item that arrives and may replace what is worn) used to add up the seven classic
columns, so class gear would always lose to plate. Now each side still works out that sum, with the enchantments it
counts, and passes it through one shared rule, `RavenNest.Models.GearScore.Value(material, classic value, line that fits)`:

- **Everything that is not class gear:** the sum, as before.
- **Class gear whose line fits:** the sum divided by the line's armor factor (that is the plate piece of its level),
  plus 25%. It beats plate of its level and the next metals up, and loses to plate many tiers above.
- **Class gear whose line does not fit:** its own, lower armor. The Tank line has the armor of plate, so it counts as
  95% of it (and at least one point less): nobody ends up in threat gear by accident.

The line that fits is the skill being trained: Magic is Mage, Healing is Healer, Ranged is Ranger, Attack or Strength
is Melee, Defense is Tank. "All" names no line (the game trains it as Health and the server keeps it as "All", so
Health cannot mean Tank), and neither does a skill that is not combat. When the skill names no line, the line of the
class piece already worn in that slot is the one that fits: what a player wears is kept, a better tier of it is taken,
and with plate or nothing worn it is plain armor that wins.

Only the ranking changed. When something is replaced is as before on both sides.

Two examples, chest pieces for a player at level 120:
1. Training Magic in an Abraxas Chest (128 armor), with a Sorcerer Robe (52 armor) in the bag. The robe fits:
   52 / 0.4 x 1.25 = 163, more than 128, so `!equip all` puts the robe on. It also beats a Phantom Chest (142) and a
   Lionsbane Chest (157) and loses to an Ether Chest (173). If the same player trains Ranged instead, the robe counts
   as 52 and the plate stays.
2. Training Attack in an Abraxas Chest (128), and a Bastion Cuirass (Tank, 128 armor) arrives. It does not fit:
   128 x 0.95 = 121, less than 128, so it stays in the bag. Training Defense it counts as 128 x 1.25 = 160 and goes on.

Where it is called: the server in `PlayerInventory.EquipBestItems` (the trained skill is the character state's task
"Fighting" and its argument, `GearScore.LineFor(task, argument)`), the game in `Inventory.EquipBestItem` and
`PlayerController.EquipIfBetter` (`GearScore.LineFor(ActiveSkill, worn material)`, through
`ItemExtension.GetGearScore`). The game's "equip best" skipped every hat and mask as a look; class gear's hats (the
Wizard Hat, the Ranger's Cap) are no longer skipped.

Not changed: the server's own sum for an enchanted item is only the enchantments' share (`GetItemValue` replaces the
total instead of adding to it), so the two sides already ranked enchanted gear differently. The rule sits on top of
whatever each side counts.

## Games before 0.9.7

They get the same items and must not break on them:
- **Model ids** are parsed by every version the same way (each one as a whole number), and an old game uses the ones it
  knows as part indices: all of a head piece's (the piece, then head attachments), a chest's first four (torso,
  pauldrons, upper arms, elbow pads) and the leggings' first two (hips, knee pads). So every id is a whole number, never
  negative or missing, and those positions always hold a real index. The positions 0.9.7 added (a chest's fifth: the
  back piece; the leggings' third and on: the belt) are only parsed by an old game, never used. Leggings with a belt
  and no knee pads say knee pad 10 and `NoKneePads`: an old game shows that small knee pad, 0.9.7 none.
- **What a piece hides or leaves off** is in `HeadMask`, a flags number saved on every item and sent to the game, which
  no game before 0.9.7 reads. For a head piece: Hair, Brows, Beard, Ears, Head (a hood hides hair and ears, a closed
  helm everything). For a body piece: NoShoulderPads (32), NoElbowPads (64), NoForearms (128), NoKneePads (256). An old
  game shows its default pauldrons on a robe, and has the head attachments but no belt or back parts: acceptable.
- **Materials** past `ElderAtlarus` are new numbers. 0.9.7 loads a material by its name when the number is beyond its
  own list; never reorder or insert in `ItemMaterial`.
- **The stats** are in `Item.Stats` text, which old games do not read: there the pieces are low armor and nothing else.
- **Hide** only drops through the server, so it reaches old games as any item does.

## Website

The public items list says the stats for people ("+1.2% Magic Power", one chip a stat) and a Slayer drop as
"Fighting, Slayer level 1". The rarity color of a class gear piece follows the metal of its tier's level (the new
materials come after the Elder ones, which would have made an Apprentice Robe mythic). The admin item editor's material
list has the 40 new materials.

## Open

- The attachments (belt, back, the tiers' plumes and wings) are tested through the equip code in the editor, not in
  play: the combined mesh with a belt or back part on, and how a cloak moves, are still to be seen in the game.
- Not played yet: how the numbers feel, and how fast Flax and Hide come in at low and high levels.
- The server's task for a character follows the game's saves, so right after `!train` it can be a moment behind the
  game when `!equip all` is used.
- The second head pieces' parts and masks are first picks; they are one row each in `ClassGearPieces`.
