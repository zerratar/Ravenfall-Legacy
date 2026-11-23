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
- **Characters** become the undead of the tower's crypt and marsh floors and a few elites, and the bigger or odder ones
  become the bosses of floors 4, 7, 8 and 9 (round 2, 2026-11-23: the Necromancer, the Colossus, the Plague Lord and
  the Behemoth).

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
| Bonewhistle Staff | Staff_01 | staff | Magic 40 | 34 / 34 | life steal 1.5% | Mortimer Bonewhistle's trophy (floor 4) |
| Colossus Cleaver | Sword_02 | 2H sword | Attack 70 | 70 / 72 | armor penetration 5% | Big Gorp's trophy (floor 7) |
| Keepward Shield | Shield_01 | shield | Defense 70 | 50 armor | thorns 3% | Big Gorp's trophy (floor 7) |
| Blightbone Staff | Staff_02 | staff | Magic 80 | 78 / 78 | poison 25% | Lord Pestilus's trophy (floor 8) |
| Rotbelly Bonesaw | Saw_01 | 2H axe | Attack 90 | 75 / 94 | poison 25% | Old Rotbelly's trophy (floor 9) |
| Rotreaper | Scythe_01 | staff | Magic 90 | 86 / 86 | poison 25% | Old Rotbelly's trophy (floor 9) |
| Skullcrusher | Club_01 | 1H (axe slot) | Attack 300 | 150 / 170 | crit damage 15% | dungeon/tower chests, tier 3, Slayer 30 |
| Fortress Hand Cannon | Cannon_01 | bow | Ranged 500 | 470 / 480 | crit chance 5% | chests, tier 4, Slayer 50 |
| Gravebloom Warhammer | Hammer_01 | 1H (axe slot) | Attack 650 | 357 / 404 | poison 35% | chests, tier 4, Slayer 65 |
| Sunstaff of the Inquisitor | Staff_03 | staff | Magic 700 | 585 / 585 | healing power 10%, undead damage 15% | chests, tier 4, Slayer 70 |
| Hellhorn Mace | Mace_01 | 1H (axe slot) | Attack 750 | 386 / 436 | fire 35% | chests, tier 4, Slayer 75 |
| Batwing Staff | Staff_01 | staff | Magic 800 | 608 / 608 | life steal 3% | chests, tier 4, Slayer 80 |
| Spiked Bulwark | Shield_01 | shield | Defense 850 | 380 armor | thorns 8% | chests, tier 4, Slayer 85 |
| Soulreaver | Sword_01 | 2H sword | Attack 900 | 711 / 711 | life steal 3% | chests, tier 4, Slayer 90 |

**Legendaries (2026-11-23).** Five of the finds are the first Legendaries (docs/legendary-design.md): the
Soulreaver, Hellhorn Mace, Batwing Staff, Spiked Bulwark and Fortress Hand Cannon. They left the chest table, ask for
950 in their skill, are soulbound and sell for a coin, and their numbers are no longer the ones in this table: each is
the best normal item of its type plus 10% (20% at awakening rank 5), worked out by the server from the catalog. The
Hellhorn Mace's fire is 15% (35% made it half again as strong as the best axe in the simulator). Each has a power of
its own. Skullcrusher, the Gravebloom Warhammer and the Sunstaff of the Inquisitor stay chest finds as below.

The chest finds are `ItemDrop` rows with a weight of 0.01 (most drops are 0.05), so they are rare. A tier 3 find drops
from chests of tier 3 and up (the harder dungeons, tower floors 10 and up), a tier 4 find only from tier 4 chests (the
hardest dungeons, tower floors 13 and up); the Slayer level keeps them from characters far below them.

The trophies share their models with high level pieces (the pack has sixteen weapons): the Bonewhistle Staff is the
Batwing Staff's model, the Colossus Cleaver the Dreadforged Cleaver's, the Keepward Shield the Spiked Bulwark's, the
Blightbone Staff the Dreadforged Staff's.

**Odd ones.** The **Hand Cannon** is a bow: held in the off hand it points down at rest and straight at the enemy
when the bow is drawn. Since round 2 it fires a cannonball, not an arrow (`CannonShot`, see "The hand cannon's shot"
below), and its description says so ("Feed it arrows and they come out round"). The **saw**
is a two handed axe, the **scythe** a staff (a reaper's scythe for casters). **Shield_03** is not used: it is the same
model as Shield_02. Nothing was skipped for not fitting a weapon type.

Every weapon was fitted in the hand through the game's own bones (Hand_R, Hand_L) with the animation pack's idle and
attack poses of its type, next to the existing weapons of that type (the 1H axe, 2H axe, 2H sword, staff, shield and
bow prefabs as reference), and seen in play on floor 9 of the tower. Scales: one handed about 0.9 to 1.15 m, two
handed 1.4 to 1.7 m, staffs 1.75 to 1.9 m. Maces, the hammer, the battleaxe and the saw hold their grip, not their
pivot (a child `Model` moved along the haft); the staffs are held about a third of the way up, as the game's staffs.

**Shields, round 2.** The first fit used the rotation of the game's own shields, and in the one handed idle (the stance
the game uses with a shield, Armed-Idle) the long dark kite shields hung edge-on below the hand, away from the forearm
(docs/mockups/dark-fortress-weapons.png). The game's round shields hang the same way, but the long ones read as not
held. Both Dark Fortress shields (the Dreadforged Shield and the Spiked Bulwark, the Keepward Shield with it) are now
turned 90 degrees about the forearm so the face is in front of the arm, and moved out along the face and down the
kite so the hand holds them from behind instead of through them; checked on male and female in the idle, the shield
idle and a sword attack, and in play (docs/mockups/dark-fortress-shields.png). The **Dreadforged Sword**'s crescent
guard now stands up and down: flat, it cut into the thigh in the idle. Every other weapon was checked again in its
type's idle and attack from the front and the side next to the game's own: they sit as the game's do.

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
Skull** (Mask_04) from the tier 3 chests (the Grinning Skull is also Mortimer Bonewhistle's trophy), and the
**Plague Mitre** (Crown_02, the Plague Lord's own) as Lord Pestilus's trophy.

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

## 3. Enemies and bosses

`tools/dark-fortress/dark-fortress-enemies.cs` makes one prefab per kind in `Assets/World/Enemies/DarkFortress` and one
`EnemyDefinitionAsset` per kind (`df-*.asset`): MainWorld's Ravebridge skeleton (its EnemyController, movement,
colliders and the Humanoid Enemy animator) with the skeleton's bodies taken out and a Dark Fortress character put in,
its avatar on the animator. The humanoid enemy animations (idle, run, unarmed attack, get hit, death, cheer) play on all
of them, the Behemoth's bigger skeleton (BR_CharactersAvatar) included; checked in poses and in play.
`dark-fortress-templates.cs` puts them into the tower layouts' hidden templates (the crypt, marsh and overgrown builders
do the same when they rebuild).

| Model id | Character | Name in the tower | Floors | Level | Style |
|---|---|---|---|---|---|
| df-undead | Undead_01 | Risen Dead | 4 | floor | melee |
| df-undead-soldier | Undead_02 | Risen Soldier | 4 | floor + 1 | melee, 1.1 s |
| df-grave-knight | Knight_01 | Grave Knight | 7 | floor + 3 | melee, 1.2 s |
| df-plague | Plague_01 | Plague Walker | 8 | floor + 1 | melee, 1.1 s |
| df-corpse | Corpse_01 | Shambling Corpse | 9 | floor - 1 | melee, 1.3 s |
| df-wraith | Wraith_01 | Crypt Wraith | 9 | floor + 1 | magic, 1.4 s |
| df-apothecary | Apothecary_01 | Bog Apothecary (elite) | 3 | floor + 1 | magic, 1.3 s |
| df-barbarian | Barbarian_01 | Antlered Reaver (elite) | 5 | floor + 2 | melee, 1.1 s |
| df-inquisitor | Inquisitor_01 | Fallen Inquisitor (elite) | 10 | floor + 3 | magic, 1.3 s |
| df-necromancer | Necromancer_01 | Mortimer Bonewhistle (boss) | 4 | the boss's | magic |
| df-colossus | Colossus_01 | Big Gorp, the Keep Colossus (boss) | 7 | the boss's | melee |
| df-plague-lord | Plague_Lord_01 | Lord Pestilus (boss) | 8 | the boss's | magic, poison |
| df-behemoth | Behemoth_01 | Old Rotbelly (boss) | 9 | the boss's | magic, poison |

Their numbers come from the floor (level, power, armor, the band's element), as every tower monster's, so the floors
are as hard as before; the offsets are those of similar monsters (the red Metalon is +4). What changed in
`RavenNest.Combat/Tower.cs`:

- Floor 3, The Nest: Bog Apothecaries join the green Metalons and goblins (quests unchanged).
- Floor 4, Hall of Bones: Risen Dead, skeletons and Risen Soldiers (quests: 40 skeletons, 30 Risen Dead).
- Floor 5, Thornwood: Antlered Reavers join the purple Metalons and goblins (quests unchanged).
- Floor 7, Goblin Keep: Grave Knights join the goblins and skeletons (quests unchanged).
- Floor 8, Blighted Grove: Plague Walkers, red Metalons and purple Metalons (quests: 40 red Metalons, 40 Plague Walkers).
- Floor 9, The Deep Roots: Shambling Corpses, Crypt Wraiths and red Metalons (quests: 45 corpses, 45 wraiths).
- Floor 10, The Verdant Throne: Fallen Inquisitors join the red and purple Metalons (quests unchanged).

**The bosses** (band 1; each reuses the tower's boss moves: help called at shares of its health, the slam ring, the
marked spit circle, the enrage). The smaller, odder looking characters (Inquisitor, Barbarian, Apothecary) are elites
on the floors; the big or unique ones are bosses. Each boss slot they took was a reused look before (a big skeleton, a
big goblin, a big Metalon), so band 1 keeps its bunny, goblin, Metalon, cow and dragon bosses on the other floors.

| Floor | Boss | Was | Fight | Line | Trophies |
|---|---|---|---|---|---|
| 4 Hall of Bones | **Mortimer Bonewhistle**, the Necromancer, scale 2.2 | the Rootbound King | a caster; raises three Risen Dead at three quarters, half and a quarter of its health; a spit every 5 swings; no slam | "Rise, my darlings. Supper has arrived." | Bonewhistle Staff, Grinning Skull |
| 7 Goblin Keep | **Big Gorp, the Keep Colossus**, scale 2.4 | King Grubnose the Second | slow, heavy swings (3 s); a ground slam every third swing; the keep's goblins (four) come at half its health | "King Grubnose says: no visitors." | Colossus Cleaver, Keepward Shield |
| 8 Blighted Grove | **Lord Pestilus**, the Plague Lord, scale 2.3 | Redshell the Blighted | a caster of poison (60%); a poison cloud (the marked spit circle, 1.3 times a hit) every third swing; Plague Walkers at half its health | "Breathe deep, little ones. It is good for you." | Plague Mitre, Blightbone Staff |
| 9 The Deep Roots | **Old Rotbelly**, the Behemoth, scale 2.3 | the Gardener (a big skeleton) | unchanged since round 1: magic, poison, a spit every 4 swings, Shambling Corpses at two thirds and one third | "Fresh compost! Come closer. Closer." | Rotbelly Bonesaw, Rotreaper |

Their numbers were set with the simulator's `tower` scenario so the floors stay about as hard as with the bosses they
replaced (five players at the floor's level, win rate old to new): floor 4 77% to 67%, floor 7 72% to 85%, floor 8
10% to 15%; floor 9 is unchanged. Scales were checked in play: at 1.5 to 2.0 they read as big players. Intro taunts:
Cheer2 (Necromancer), Cheer3 (Colossus, Behemoth), Cheer1 (Plague Lord) in `tools/tower/tower-boss-taunts.cs`.

**Trophies** (`TowerBoss.Trophies`): on the stream's first clear of the floor, each player who fought has a 1 in 4
chance (`TowerLoot.TrophyChance`) of one of the boss's trophies (`GameManager.RollTowerTrophies`). Any boss can get
trophies by naming Dark Fortress items.

Not used as enemies: the Knight's body armor is the armor test cut; the Colossus's and Behemoth's bigger skeletons play
the humanoid animations as well as the others.

### The hand cannon's shot

`CannonShot` (Game/Combat), called where the ranged attack's arrow is shot (`EffectHandler.ShootRangedProjectile`):
when the ranged weapon in the hand is a cannon (its prefab path), no arrow flies. Instead:

- a flash (a short burst of orange sparks) and a puff of grey smoke at the muzzle (the end of the barrel that points
  at the target), and the cannon kicks back 12 cm along the barrel and settles in 0.18 s;
- a boom: `SoundKind.CannonShot`, two clips in Assets/Sounds/Effects/CannonShot made from the Environment Sounds pack's
  thunder with a rock impact on the attack (ffmpeg, nothing downloaded; local like the other effect sounds);
- an iron ball (the cannon's own material) flies in a low arc to the target;
- where it lands, a puff of dust and the pack's rock impact.

The hit itself is the bow's as before (the arrow was only a picture too). The flash, smoke and dust follow the Weapon
effects setting and potato mode like the weapons' element effects; the ball and the sounds stay. Particle systems are
made once per player and emitted in bursts. Checked in play (docs/mockups/dark-fortress-cannon.png).

### Mirrored name tags (round 2)

The backwards name tags in the first boss capture came from the capture, not the game: name tags turn to the game
camera every frame (`NameTag`, `GameCamera.Rotation`), and that capture was rendered by a second camera from the
other side. In the game's own view (and in captures from the game camera's side, as all round 2 captures are) they
read the right way. Nothing changed in the game for it.

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

Round 2 (2026-11-23), played with the dev character and four bots: full fights against Mortimer Bonewhistle (Risen
Dead raised at each share), Big Gorp (slam rings, goblins at half health) and Lord Pestilus (Plague Walkers at half
health), each to the boss's fall; the new monsters and elites spawning and fighting on floors 3, 4, 5, 7, 8 and 10;
the hand cannon firing (flash and smoke at the muzzle, the ball, the dust where it lands); the Dreadforged Sword and
Shield in a fight; the shields and every weapon again in the fit renders.

Not checked: a real clear (the trophy roll on the server, only its rule is tested; the dev stream is on floor 6, so
every boss fight was a training attempt), the boom heard (the clips were measured, not listened to: -13 LUFS, a little
louder than the bow), crafting a Dreadforged piece, a chest drop, the website figure in a browser (the export and the
script are done), the cosmetic crowns over big hair styles.

Pictures: docs/mockups/dark-fortress-weapons.png (held in idle poses with head pieces, the shields as fixed in round
2), -shields.png (the shield fix: fit renders and in play), -heads.png (every head piece on male and female heads),
-icons.png, -enemies.png and -enemies-2.png (every character as an enemy), -elites.png, -crypt.png, -boss.png (Old
Rotbelly), -necromancer.png, -colossus.png, -plague-lord.png, -cannon.png (flash, smoke, impact), -in-play.png (the
dev character: Inquisitor Helm, horned skull, Fortress Helm).

## 7. Calls for Zerratar

- The bosses' names and lines (Mortimer Bonewhistle, Big Gorp, Lord Pestilus, Old Rotbelly) are one line each.
- Three band 1 bosses with reused looks gave their slots to fortress bosses (the Rootbound King, King Grubnose the
  Second, Redshell the Blighted); they could come back on later floors.
- Uniques are rare (weight 0.01 in the chest table) and trophies 1 in 4 per fighter on the first clear only.
- Dreadforged at level 450 fills the Atlarus to Elder Bronze gap; another set at a higher gap is easy to add.
- The Hand Cannon stays (Zerratar: "does it have proper projectile effects and stuff? :D"): it does now.
