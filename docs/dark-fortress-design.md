# Dark Fortress gear and enemies

Synty's POLYGON Dark Fortress pack in Ravenfall (v0.9.7, started 2026-11-23). Zerratar: "the gear look so amazing and
would love to have it in game ... yeah start on the dark fortress plan", and other Synty packs may be mixed in ("the more
the merrier"). This part covers the pack's weapons and head pieces as player gear, and its characters as enemies with a
test boss. The Knight's body armor is a separate test cut in Blender (tools/dark-fortress-armor), not covered here.

The pack is a local import in `Assets/Synty/PolygonDarkFortress` and is not versioned, like every other Synty pack
(`Assets/Polygon*` is ignored, `Assets/Synty` is untracked): the prefabs, the tower templates and the icons that use it
only work on a machine that has it, the build machine included. Everything made from it is listed below so it can be
remade on a new checkout.

## 1. The plan

Fit the pieces where they belong instead of one disconnected set:

- **Head pieces** become a third head look of the class gear lines they suit, from a high tier on (same stats as the
  line's other head pieces of that tier, so it is a choice of look, and every head piece can take them by transmog),
  plus a few cosmetic finds.
- **Weapons** fill a real gap in the weapon ladders (Atlarus at 400, Elder Bronze at 500) as a crafted **Dreadforged**
  set, and the rest are uniques: trophies of a tower boss and rare finds in the high dungeon and tower chests.
- **Characters** become the undead of the tower's crypt and marsh floors, and the **Behemoth** becomes the floor 9
  boss, the Gardener.

## 2. What became what

### Weapons and shields

All are items with a model of their own (`GenericPrefab` under `Character/Weapons/DarkFortress/DF <model>`), made at
server start by `GameData.DarkFortress.cs` with fixed ids (MD5 of "ravenfall-dark-fortress-<key>"), like the class gear.
Stats follow the ladder of their type at their level, plus one fortress touch in `ExtraStats`.

| Item | Model | Type | Needs | Accuracy / power or armor | Extra | How |
|---|---|---|---|---|---|---|
| Dreadforged Sword | Sword_03 | 1H sword | Attack 450 | 275 / 275 | life steal 2% | Crafting 455: 3 Atlarus bars, 1 Elder Iron bar |
| Dreadforged Morningstar | Mace_02 | 1H (axe slot) | Attack 450 | 258 / 291 | armor penetration 6% | Crafting 459: 4 Atlarus, 1 Elder Iron |
| Dreadforged Cleaver | Sword_02 | 2H sword | Attack 450 | 390 / 390 | life steal 2% | Crafting 463: 5 Atlarus, 2 Elder Iron |
| Dreadforged Battleaxe | Axe_01 | 2H axe | Attack 450 | 343 / 429 | armor penetration 6% | Crafting 463: 5 Atlarus, 2 Elder Iron |
| Dreadforged Staff | Staff_02 | staff | Magic 450 | 390 / 390 | life steal 2% | Crafting 463: 4 Atlarus, 1 Elder Iron, 2 Shadowoak logs |
| Dreadforged Shield | Shield_02 | shield | Defense 450 | 226 armor | thorns 4% | Crafting 464: 4 Atlarus, 1 Elder Iron |
| Gardeners Bonesaw | Saw_01 | 2H axe | Attack 90 | 75 / 94 | poison 25% | the Gardener's trophy |
| Rotreaper | Scythe_01 | staff | Magic 90 | 86 / 86 | poison 25% | the Gardener's trophy |
| Skullcrusher | Club_01 | 1H (axe slot) | Attack 300 | 150 / 170 | crit damage 15% | dungeon/tower chests, tier 3, Slayer 30 |
| Fortress Hand Cannon | Cannon_01 | bow | Ranged 500 | 470 / 480 | crit chance 5% | chests, tier 4, Slayer 50 |
| Gravebloom Warhammer | Hammer_01 | 1H (axe slot) | Attack 650 | 357 / 404 | poison 35% | chests, tier 4, Slayer 65 |
| Sunstaff of the Inquisitor | Staff_03 | staff | Magic 700 | 585 / 585 | healing power 10%, undead damage 15% | chests, tier 4, Slayer 70 |
| Hellhorn Mace | Mace_01 | 1H (axe slot) | Attack 750 | 386 / 436 | fire 35% | chests, tier 4, Slayer 75 |
| Batwing Staff | Staff_01 | staff | Magic 800 | 608 / 608 | life steal 3% | chests, tier 4, Slayer 80 |
| Spiked Bulwark | Shield_01 | shield | Defense 850 | 380 armor | thorns 8% | chests, tier 4, Slayer 85 |
| Soulreaver | Sword_01 | 2H sword | Attack 900 | 711 / 711 | life steal 3% | chests, tier 4, Slayer 90 |

The chest finds are `ItemDrop` rows with a weight of 0.01 (most drops are 0.05), so they are rare. A tier 3 find drops
from chests of tier 3 and up (the harder dungeons, tower floors 10 and up), a tier 4 find only from tier 4 chests (the
hardest dungeons, tower floors 13 and up); the Slayer level keeps them from characters far below them.

**Odd ones.** The **Hand Cannon** is a bow: held in the off hand it points down at rest and straight at the enemy
when the bow is drawn, and it still shoots arrows (its description says so; the Bananarang does the same). The **saw**
is a two handed axe, the **scythe** a staff (a reaper's scythe for casters). **Shield_03** is not used: it is the same
model as Shield_02. Nothing was skipped for not fitting a weapon type.

Every weapon was fitted in the hand through the game's own bones (Hand_R, Hand_L) with the animation pack's idle and
attack poses of its type, next to the existing weapons of that type (the 1H axe, 2H axe, 2H sword, staff, shield and
bow prefabs as reference), and seen in play on floor 9 of the tower. Scales: one handed about 0.9 to 1.15 m, two
handed 1.4 to 1.7 m, staffs 1.75 to 1.9 m. Maces, the hammer, the battleaxe and the saw hold their grip, not their
pivot (a child `Model` moved along the haft); the staffs are held about a third of the way up, as the game's staffs.

### Head pieces

Models of their own on the head bone (`Character/Appearance/Head/DarkFortress/DF <model>`). The Dark Fortress characters
share the game's Synty skeleton (CharactersAvatar), so each piece sits where the pack puts it on its own characters: the
Head bone's place, which is (0, -0.216, -0.011) from the game's Head_Attachment. Male and female heads are the same
size there, so one prefab fits both.

Class gear (`GameData.ClassGear.cs`, a third head piece "head3" from a tier on, `.Starting(tier).FromPrefab(...)`):

| Line | Piece | Tiers | Model | Hides |
|---|---|---|---|---|
| Melee | Kettle Helm | 4 to 8 | Helmet_02 (open face, spiked brim) | hair, ears |
| Dark Knight | Fortress Helm | 6 / 7 / 8 | Helmet_06 (spiked ring) / Helmet_03 (hooded, glowing eyes) / Helmet_08 (the Knight's) | hair, ears, brows, beard |
| Paladin | Closed Helm | 5 to 8 | Helmet_07 | hair, ears, brows, beard |
| Witch Hunter | Inquisitor Helm | 5 to 8 | Helmet_09 (white and gold) | hair, ears, brows, beard |
| Barbarian | Antler Crown | 5 to 7, then 8 | Crown_03 (antlers and a bird skull), then Mask_03 (horned skull) | nothing, then brows and beard |
| Mage | Lich Crown | 6 to 7, then 8 | Mask_01 (crown and half mask), then Mask_02 (bat-winged) | brows |

23 items. Their model ids are empty: a game before 0.9.7 has no such prefab and shows no head piece for them.

Cosmetic (category Cosmetic, worn over everything like the Santa Hat): **Thorn Crown** (Crown_01) and **Grinning
Skull** (Mask_04) from the tier 3 chests, and the **Plague Mitre** (Crown_02) as the Gardener's trophy.

**Not used, and why:** Helmet_01 has an undead face and hair built in (it would put a ghoul's face on the player);
Helmet_04 (bat wings) and Helmet_05 (fanged) are made for the Colossus's forward-tilted head and the top of a player's
head comes through them even scaled up; Hair_01/02, Beard_01, Collar_01 and Pouch_01 are not head gear.

### How the game draws a head piece that is a model

`SyntyPlayerAppearance.IsModelHeadPiece`: armor of a head type with a `GenericPrefab`. `EquipModelHeadPiece` puts it on
the head bone, switches off any head piece made of parts (and its attachments), and hides what its `HeadMask` says;
the helmet toggle hides it and what it hid comes back; taking it off shows the head again. One that keeps the hair (the
crowns, the Lich Crown's half mask) lays every hair style flat under it (the short or the neck-long stand-in that the
class gear hats use): big hair came through the crowns and the bangs covered the half mask. Putting on another model
head piece of any head type takes the one before off. Equipping one dresses the
character from nothing (`Inventory`), as armor made of parts does, and taking it off rebuilds the mesh
(`PlayerEquipment.Unequip`). A transmog works both ways (a plate helmet looking like a Fortress Helm, or a Fortress Helm
looking like a hood). Cosmetic hats with a prefab are unchanged: they still sit on top.

The website's figure (`rf-character-gear.js`) does the same with the exported prop: the head piece of parts goes, the
mask hides the head's parts, the hair is laid flat, the helmet switch hides the prop. The cosmetic crowns (Plague
Mitre, Thorn Crown, Grinning Skull) are cosmetics like the Santa Hat and do not lay the hair flat.

## 3. Enemies and the test boss

`tools/dark-fortress/dark-fortress-enemies.cs` makes one prefab per kind in `Assets/World/Enemies/DarkFortress` and one
`EnemyDefinitionAsset` per kind (`df-*.asset`): MainWorld's Ravebridge skeleton (its EnemyController, movement,
colliders and the Humanoid Enemy animator) with the skeleton's bodies taken out and a Dark Fortress character put in,
its avatar on the animator. The humanoid enemy animations (idle, run, unarmed attack, get hit, death, cheer) play on all
of them, the Behemoth's bigger skeleton (BR_CharactersAvatar) included; checked in poses and in play.
`dark-fortress-templates.cs` puts them into the tower layouts' hidden templates (the crypt and marsh builders do the same
when they rebuild).

| Model id | Character | Name in the tower | Floors | Level | Style |
|---|---|---|---|---|---|
| df-undead | Undead_01 | Risen Dead | 4 | floor | melee |
| df-undead-soldier | Undead_02 | Risen Soldier | 4 | floor + 1 | melee, 1.1 s |
| df-grave-knight | Knight_01 | Grave Knight | 7 | floor + 3 | melee, 1.2 s |
| df-plague | Plague_01 | Plague Walker | 8 | floor + 1 | melee, 1.1 s |
| df-corpse | Corpse_01 | Shambling Corpse | 9 | floor - 1 | melee, 1.3 s |
| df-wraith | Wraith_01 | Crypt Wraith | 9 | floor + 1 | magic, 1.4 s |
| df-behemoth | Behemoth_01 | The Gardener (boss) | 9 | the boss's | magic, poison |

Their numbers come from the floor (level, power, armor, the band's element), as every tower monster's, so the floors
are as hard as before; the offsets are those of similar monsters (the red Metalon is +4). What changed in
`RavenNest.Combat/Tower.cs`:

- Floor 4, Hall of Bones: skeletons, Risen Dead and Risen Soldiers (quests: 40 skeletons, 30 Risen Dead).
- Floor 7, Goblin Keep: Grave Knights join the goblins and skeletons (quests unchanged).
- Floor 8, Blighted Grove: red Metalons, Plague Walkers and purple Metalons (quests: 40 red Metalons, 40 Plague Walkers).
- Floor 9, The Deep Roots: Shambling Corpses, Crypt Wraiths and red Metalons (quests: 45 corpses, 45 wraiths).

**The Gardener is the Behemoth now** (it was a skeleton made 2.8 times bigger): the same fight, numbers and line
("Everything grows. Even you, in time."): magic, poison, a spit every 4 swings, help at two thirds and one third (three
Shambling Corpses each time), the slam, the enrage. Scale 2.3, about twice a player's height (1.5 looked like a big
player, checked in play). Its intro taunt is Cheer3 (`tools/tower/tower-boss-taunts.cs`; Resources/Tower/Boss Taunts
was remade). The Colossus was not needed: the Behemoth plays the animations well.

**Trophies** (`TowerBoss.Trophies`, new): on the stream's first clear of the floor, each player who fought has a 1 in
4 chance (`TowerLoot.TrophyChance`) of one of the boss's trophies: the Gardener gives the Gardeners Bonesaw, the
Rotreaper or the Plague Mitre (`GameManager.RollTowerTrophies`). Any boss can get trophies by naming Dark Fortress items.

Not made (yet): Inquisitor, Necromancer, Apothecary, Barbarian, Colossus and Plague Lord as enemies or mini bosses.
They are one row each in the enemy builder; the Plague Lord and the Colossus would make good band bosses.

## 4. Icons and the website

- Icons: `tools/dark-fortress/dark-fortress-icons.cs` renders every item of `dark-fortress-icons.txt` (made from the
  item list) from its own prefab, lit like the class gear icons, into Assets/Resources/Items and the website's
  imgs/items. Not the image generator.
- The website's figure: `tools/characters/export-gear-web.cs` was run again (it takes every prefab the items name), so
  the 3D figure on the Equipment tab can hold and wear them.
- Item pages are the usual ones (name, icon, stats, recipe, where it drops).

## 5. Remaking it on another machine

1. Import POLYGON Dark Fortress into `Assets/Synty/PolygonDarkFortress`.
2. The weapon and head prefabs under `Assets/Resources/Character/.../DarkFortress` are local (Resources is not
   versioned). Their record is `tools/dark-fortress/dark-fortress-prefabs.py` (grip, rotation, scale of every one):
   run it, then `dark-fortress-prefabs.cs`. `rot.py` composes the rotations from the reference weapons'.
3. Run `dark-fortress-enemies.cs`, then `dark-fortress-templates.cs` (or the crypt and marsh builders),
   `tower-boss-taunts.cs`, `dark-fortress-icons.cs` and `tools/characters/export-gear-web.cs`.

## 6. What was checked, what was not

Checked: every weapon in the hand in idle and attack poses of its type on male and female characters, next to the
game's own weapons; every head piece on male and female heads from the front and the back; in play on the dev
character (a Fortress Helm on, hair hidden; a Lich Crown with the hair laid flat, the helmet toggle off and on, then an
Inquisitor Helm and the horned skull in its place; the Dreadforged Sword, Cleaver, Battleaxe, Soulreaver, Bonesaw and
Hellhorn Mace in a fight); all enemies on tower floor 9 (idle, walking, attacking, getting hit, dying) and a full boss fight
against the Gardener with four bots (slam ring, poison spit, adds, it fell); the icons; the server makes all 42 items.

Not checked: a real clear of floor 9 (the trophy roll on the server, only its rule is tested), crafting a Dreadforged
piece, a chest drop, the website figure in a browser (the export and the script are done), the cosmetic crowns over
big hair styles, floors 4, 7 and 8 in play.

Pictures: docs/mockups/dark-fortress-weapons.png (held in idle poses, with head pieces), -heads.png (every head piece
on male and female heads), -icons.png, -enemies.png, -crypt.png and -boss.png (in play, the Gardener with the poison
ring under a player), -in-play.png (the dev character: Inquisitor Helm, horned skull, Fortress Helm).

## 7. Calls for Zerratar

- The Gardener kept its name, line and numbers with the Behemoth's body. A new name ("The Rot Behemoth") is one line.
- Uniques are rare (weight 0.01 in the chest table) and trophies 1 in 4 per fighter on the first clear only.
- Dreadforged at level 450 fills the Atlarus to Elder Bronze gap; another set at a higher gap is easy to add.
- The Hand Cannon is a silly bow; it can go if it is too silly.
