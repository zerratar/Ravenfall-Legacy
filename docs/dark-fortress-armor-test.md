# Dark Fortress armor as player armor - feasibility test

Date: 2026-11-23. Step 3 of the Dark Fortress plan: can the body armor of the Synty POLYGON Dark Fortress
characters (one skinned mesh each) become wearable modular armor on Ravenfall's Modular Fantasy Hero (MFH) player,
male and female? Test subject: `SM_Chr_Knight_01`, with Inquisitor, Barbarian, Plague Lord and Apothecary run through
the same method to see whether it carries over.

**Verdict: yes, good enough to build on.** The Dark Fortress characters are built on the MFH skeleton and body, so
the male pieces are the original geometry and the original skin weights, cut where the game's items meet; they
deform exactly like the original Knight and sit on the MFH head and other MFH parts without gaps. The female pieces
come from a smoothed male-to-female body field and look right in A-pose and in motion. The open work is not the
geometry but the wiring: a material per set (the Dark Fortress atlas, not the tier tint materials), the player's
skin color on bare skin, and new part indices on the player prefab and the website figure.

Not done in this test: the Unity editor check (step 6). The editor-busy flag was set by the agent turning the pack's
weapons, helmets and characters into gear and enemies, and stayed set for the whole test, so nothing was imported or
previewed in Unity. The FBX export was verified by a round trip in Blender instead (see Method step 6).

# Color pass and single pieces (2026-11-23, approved: "yes do the color pass and the single pieces")

## Color pass

Tier 7 and tier 8 of a set have to read as different tiers at stream distance; the pack's own colorways did not do
that (Voidknight's 01_B violet barely showed, Voidweaver's tabard glowed white like a lamp). Where no colorway of the
pack reads clearly, the tier gets a tinted copy of the atlas (`tools/dark-fortress-armor/colorways.py`, Pillow, into
`Assets/World/DarkFortressArmor/Textures`; `armor-sets.cs` makes `DFArmor_<name>.mat` from the pack's material with
them; the part names keep their old suffix so the indices stay):

| Tier | Before | Now |
| --- | --- | --- |
| Voidknight (Dark Knight 7) | pack 01_B, a hint of violet | the plate a deep violet (every grey of 01_A, luminance kept) |
| Abyssal (Dark Knight 8) | pack 01_A | unchanged: the Knight's own dark steel |
| Stormborn (Barbarian 7) | pack 02_A, blue trim on brown leather | the leather and fur a cold storm grey, the blue trim kept |
| Worldbreaker (Barbarian 8) | pack 01_A | unchanged: brown leather, green trim |
| Voidweaver (Mage 7) | pack 01_B, white glow | the tabard, runes and their glow violet (albedo and emissive map, where the emissive is lit) |
| Starcaller (Mage 8) | pack 01_A | unchanged: green glow |

The website export picks the tinted textures up from the materials (`textures/df-dfarmor_*.jpg`).
`docs/mockups/dark-fortress-armor-colors.png`: the six looks side by side, male and female, at the game camera.

## Single pieces

| Piece | Source | Became | On | Index |
| --- | --- | --- | --- | --- |
| Plate shoulder guards | `SM_Chr_Attach_Armor_01` (two loose guards) | shoulder pads, skinned to clavicle and shoulder as the pack has them | Melee Hauberk tiers 7-8 (were pad 8, as at tiers 4-6 with another) | pads 21 |
| Fur mantle | `SM_Chr_Attach_Fur_01` | shoulder pads (split at the middle into its two sides; always worn together) | Beastmaster Hide Vest 7-8 (were pad 8) | pads 22 |
| Spiked pauldrons | Undead 02 (loose parts, clean) | shoulder pads | Rogue Tunic 7-8 (had none: the tunic's mask drops NoShoulderPads there) | pads 23 |
| Mossy fur collar with skulls | Wraith (cut out of its robe: the light moss, fur and bone faces around the neck, not its head) | shoulder pads, split at the middle | Witch Hunter Coat 7-8 (had none: NoShoulderPads dropped there) | pads 24 |
| Spiked bracers | Plague Lord (the cut, male bulk, female fit) | lower arms; the line's own glove hand stays | Witch Hunter Gloves 7-8 | lower arms 25 |
| Spiked boots and greaves | Undead 02 (loose parts; the skeleton's glowing shin and the knee shards left out) | legs, with the game's own bare shin above the boot in the player's skin (as the robes' shoes), male bulk, female fit | Rogue Boots 7-8 | legs 26 |
| Pouch belt | Plague Lord | **not taken**: it is a ring of pustules on its belly over an iron corset band, not pouches; as a belt it would hang a rotten gut on the player | - | - |

The pads are one pair for both sexes like the game's own, 20 % bigger than the pack's and 1.5 cm further out (the pack's
bodies are slim). All are drawn with the pack's 01_A material (own material: `_DF` in their name), so they keep their
look on any tier color. 0.9.6 games read the old ids in the old positions (the new parts are tokens of -1000 and less,
as for the sets): they see tier 6's look. Every piece is a look of an existing item, so transmog takes it like any
other. Made by `tools/dark-fortress-armor/dump_singles.py` (JSON `DFSingles_<sex>.json`, with `render` a check
picture), put on the prefabs by the same `armor-sets.cs` (rerun it after `dump_sets.py` and `dump_singles.py`).

Pictures: `docs/mockups/dark-fortress-single-pieces-*.png` (worn in idle, sword attack and run, from behind, and mixed
with other gear, male and female).

Verified through the real equip code in the editor (`class-gear-preview.cs` with the game's clips; not in play mode
this time): every piece on male and female, the six colors at the game camera, mixed with Rune plate and the Dark
Fortress sets; the server's rows after a restart (the new model ids and masks), the 30 icons of the affected items,
and the website figure (`/dev/gear-preview?wear=Voidknight`, `Voidweaver`, `Godslayer`, `Vanquisher`). Judged: the
three tier 7 colors read as their own tier at the game camera; the Voidweaver glow is a pale violet next to
Starcaller's green (it reads lavender through the bloom; fine at stream distance); the Undead pauldrons are dark on
the Rogue's dark tunic and read mostly by their spikes; the collar sits high, over the Witch Hunter coat's own collar,
as intended. Not verified: owning and equipping one with `!equip` on a live session.

# Wired in (2026-11-23): the Knight, Barbarian and Necromancer as class gear looks

The owner approved wiring the armor in ("yes go ahead when the editor is free"). Three sets are now real, wearable
looks of existing class gear items, male and female, through the game's own equip code. No new items, no new
ItemMaterial values, no stat changes: the items of tiers 7 and 8 of three lines got a new look.

| Line | Tier 7 (look) | Tier 8 (look) | Pack character | Colorway (tier 7 / 8) | Head piece of the same tier (the other agent's) |
| --- | --- | --- | --- | --- | --- |
| Dark Knight | Voidknight Chestplate, Gauntlets, Legplates, Warboots | Abyssal (same pieces) | Knight | 01_B violet / 01_A the pack's own | Fortress Helm (Helmet_03 / Helmet_08, the Knight's own helm) |
| Barbarian | Stormborn Harness, Bracers, Kilt, Fur Boots | Worldbreaker (same pieces) | Barbarian | 02_A blue / 01_A | Antler Crown (Crown_03 / horned skull Mask_03) |
| Mage | Voidweaver Robe, Gloves, Robe Skirt, Boots | Starcaller (same pieces) | Necromancer | 01_B violet / 01_A, glowing tabard | Lich Crown (Mask_01 / bat-winged Mask_02) |

Why tiers 7 and 8: the top two tiers had the line's tier 6 parts again (tier 7 and 8 only differed by material), and
the other agent's Dark Fortress head pieces already start there, so body and head now match per tier. Being looks of
items that exist, they are transmog looks like every other class gear piece (a Rune Chest looking like an Abyssal
Chestplate was checked in the preview).

**How an item names a part that 0.9.6 games do not have.** The class gear's model ids must stay valid ints for 0.9.6
games. The new parts are named with extra tokens of -1000 and less: `-(1000 * (position + 1) + index)` takes that
position's place (`ItemManager.ParseModelIds`, mirrored in the website's `modelIds`). So the Abyssal Chestplate is
`21,4,15,5,-1030,-3022`: a 0.9.7 game reads torso 30, upper arms 22; a 0.9.6 game parses every token as a number,
reads only the first four positions and draws the tier 6 chestplate. Leggings are `13,2,-1030` (0.9.6: tier 6 hips
and knee pad 2), boots `12,-1021`, gloves `1,12,-1019,-2020`. The server rows are in RavenNest's
`GameData.ClassGear.cs` (`.From(7, ...)`, `.From(8, ...)`, mask `DarkFortressBody`), mirrored in
`tools/items/class-gear-parts.txt`.

**Flags.** The pauldrons and elbows are part of the upper arm and forearm pieces, the knee caps part of the hips, so
the chests carry `NoShoulderPads | NoElbowPads` and the leggings `NoKneePads` (docs/class-gear-design.md: "none" is a
HeadMask flag, the id positions still hold a harmless real index for 0.9.6).

**Part indices** (male and female arrays alike, all three prefabs): torso and hips 29 to 34, upper arms 21 to 26,
lower arms 19 to 24, hands 18 to 23, legs 20 to 25, in the order Knight 7, Knight 8, Barbarian 7, Barbarian 8,
Necromancer 7, Necromancer 8. 120 parts, 60 meshes (`Assets/World/DarkFortressArmor/Meshes`, made by the script, not
versioned like the prefabs).

**The prefab script** `tools/dark-fortress-armor/armor-sets.cs` (rerunnable; the prefabs are not versioned, so the
script is the record): reads the JSON that `dump_sets.py` writes (mesh data, not FBX: the Dark Fortress rig's bones
export from Blender with other rest orientations than the game's), finds the mapping from Blender's space into the
MFH meshes' space by matching the MFH male torso (and left from right by the hand), binds each piece to the player's
own bones by name (the right hand's fingers are `Finger_01 1` in the prefab) with the MFH parts' own bind poses, and
appends the parts to the arrays of `Player`, `Player Appearance Items` and `Item Display` (a rerun updates them in
place, so the indices never move). Each part has the colorway's material of the pack and, when it shows skin, a
second submesh with `FantasyHero.mat`. To redo: `python tools/dark-fortress-armor/colorways.py <repo>`, `blender -b --factory-startup --python tools/dark-fortress-armor/dump_sets.py -- <repo> <repo>/Temp/dark-fortress-armor`
and the same with `dump_singles.py`, then `unity command eval_file --file tools/dark-fortress-armor/armor-sets.cs`, then the icons
(`tools/items/class-gear-icons.cs` with `only=abyssal` etc.) and the website export.

## The code change: the skin slot and the pack's own material

`SyntyPlayerAppearance.SetPartMaterials` (used by `Equip` and by `Get`, the model on its own for chests and icons):
- a part with **one submesh** is as before: the item's tier material (with the player's skin for a `SkinParts` part);
- a part with **two submeshes** shows bare skin in the second: `{ item material, player's copy of the body material }`
  (`SkinOf`: one copy per body material, recolored with the others when the skin color changes, freed on destroy);
- a part named `_DF...` keeps **its own material** (the pack's atlas it is mapped to): the tier materials paint the
  MFH atlas and would make nonsense of it;
- a model on its own gets its skin slot in the item material's `_Color_Skin` (`ShownSkin`), so a loot chest or icon
  matches the set.

`ItemManager.ParseModelIds` is now the one parser (the appearance used a copy). The mesh combiner keeps the two
materials (the play captures went through `EquipAll` and `Optimize`).

Website: `export-gear-web.cs` writes a `_DF` part's own picture and glow (`textures/df-*.jpg`) and its skin submesh as
`<part>__skin` (`gear.json` `ownParts`); `rf-character-gear.js` draws the part with its picture and the skin file with
the body's material in the wearer's skin, and reads the new model id tokens. Seen on `/dev/gear-preview?wear=Abyssal`
(and Worldbreaker, Starcaller). The skin files carry the whole piece's vertices (about 90 KB each): could be trimmed.

## Male bulk pass (the owner: "the chests seem to be more to the female side")

The owner was right, and so was the "breasts" impression. The pack dresses its characters on a slim body: on our
male the Knight's torso sat about 1 cm outside the bare body (18 % of it inside), the waist was cinched, and the
Knight's and Necromancer's chest plates are two sculpted mounds with a valley and an undercut between them, a
woman's breastplate shape (the front profile across the chest: two peaks at about 12 cm with a 10.5 cm valley; the
game's plate is one flat front). The Barbarian's leather was broad already but slim at the waist.

`tools/dark-fortress-armor/male_bulk.py` is a "male" shape key on the uncut character, like the female fit, so the
cut lines and the seams stay as they were: torso 19 % wider at the chest and 20 % at the waist (a straighter taper),
the front pushed out 20 %, the chest's middle pulled out to one straight front line from the waist to the chest's
peak (fills the valley and the undercut, keeps the plate edges and details), pauldrons 45 % bigger and 4.5 cm further
out, upper arms 24 % and forearms 16 % thicker about their own line, legs 8 %, and nothing inside the male body.
The female pieces keep the female fit.

| Torso slice (cm) | Chest width / depth | Under the chest | Waist | Upper arm (depth / height) |
| --- | --- | --- | --- | --- |
| Game plate (Rune) | 44.0 / 30.7 | 37.6 / 31.0 | 31.8 / 13.1 | 13.5 / 14.2 |
| Knight before | 46.9 / 27.9 | 34.7 / 25.3 | 32.4 / 26.8 | 9.5 / 11.4 |
| Knight after | 50.7 / 34.1 | 43.4 / 31.4 | 38.8 / 30.4 | 13.4 / 15.1 |
| Barbarian before / after | 48.3 / 34.2 to 50.6 / 39.3 | | 32.4 to 38.8 wide | 12.6 / 15.1 to 15.6 / 18.8 |
| Necromancer before / after | 45.1 / 30.7 to 49.0 / 35.1 | | 37.2 to 44.7 wide | 10.7 / 13.3 to 13.3 / 16.5 |

`docs/mockups/dark-fortress-armor-male-bulk.png`: before and after beside the game's plate and the Tank class gear,
idle and combat, front, side and the game camera, plus chest close-ups. Honest read: the male Knight now reads as a
broad armored man and no longer as a breastplate; it is still slimmer than the game's plate, which is mostly the
plate's huge pauldrons, and that is the Knight's design (fitted plate) rather than a fit problem.

## Verified, and how

In the editor with the real equip code (`tools/items/class-gear-preview.cs`, which can now pose with any of the
player's clips, `clip=`), and in **play mode on the dev character** (`tools/dark-fortress-armor/armor-play.cs`:
dresses the joined player through `PlayerEquipment.EquipAll` from items made in memory from the server's rows,
nothing given or saved, the player's own gear, cape and skin put back after; chat muted and the bot stopped):

- all three sets, tiers 7 and 8, male and female: `docs/mockups/dark-fortress-armor-sets-worn.png`
- the game's clips (idle, run, sword attack, combat stance, woodcutting, fishing):
  `docs/mockups/dark-fortress-armor-sets-animations.png`
- light, medium and dark skin: `docs/mockups/dark-fortress-armor-sets-skin.png` (the bare forearms and hands are the
  body's skin in every tone)
- mixed with the game's gear (Rune pieces, a robe with a kilt, a skirt alone, a chest alone):
  `docs/mockups/dark-fortress-armor-sets-mixed.png`
- from behind and the side: `docs/mockups/dark-fortress-armor-sets-back-side.png`
- the icons of the 24 pieces (Item Icon Generator, copied to the website), the server's item rows after a restart, the
  website figure.

Not verified: an item actually owned and equipped through chat (`!equip`) on a live session, a 0.9.6 game drawing the
tier 6 fallback (reasoned from its parser, not run), the export on the build machine (the pack and the prefabs are
local), the female pieces under the male bulk check (left as they were: they read female on purpose).

**The single pieces** (shoulder guards Attach_Armor_01, fur mantle Attach_Fur_01, the Plague Lord's spiked gauntlets
and pouch belt, the Wraith's fur collar) were not done: listed for later. The two attachments would fit as a chest's
back piece (the back attachment slot) or new shoulder pads; the gauntlets need a cut of their own (gloves slot); the
belt would be a hips attachment.

# Second pass (2026-11-23, after the owner's feedback)

The owner: "not bad, but it needs more work". Arms looked broken or twisted (maybe the size), some characters are not
humanoid enough (zombie-ish, bent), bare skin on the pieces did not match the body, pauldrons sat oddly. Asked: find
which characters are really usable, fix pads, the broken-looking limbs and the skin.

**Verdict after the second pass: still good to build on, for five of the thirteen characters** (Knight, Barbarian,
Necromancer as they are; Inquisitor and Apothecary as robes with some extra work). The broken look was the test's
hand-made poses, not the pieces; in the game's own animations the pieces move exactly like the original characters
and like the game's plate set. Bare skin now uses the player's own body material, so it matches for every skin color.
The pauldrons are baked into the upper arms. The Unity editor was busy again ("dark fortress round 2"), so the check
through the real appearance code is still open.

## What caused the broken or twisted arms and legs

Checked one by one:

| Suspect | Finding |
| --- | --- |
| The hand-made test poses | **The cause.** The first pass posed the bones by stacking rotations about world axes. That rolled whole arms about their own length: palms facing backwards in the "walking stride", forearms rolled 90 degrees and an arm reaching stiffly forward in the "combat stance", elbows bending sideways with arms raised, and in an early version legs crossing. Plates on a rolled arm read as a broken arm. |
| Bone roll differences | None. Every bone of the Dark Fortress rig has the same rest orientation as the game's rig (0 degrees difference, measured on the bind matrices), as well as the same joint positions. |
| Size of the pieces | Not bigger than the game's armor. Mean radius around the bone, Knight vs the game's bare body and plate set: upper arm 5.5 cm (body 5.9, plate 6.6), forearm 4.8 (4.7, 6.4), thigh 15.9 (15.0, 20.7), shin 5.5 (6.8, 8.4). Inquisitor, Apothecary and Plague Lord are bulkier only in their robe skirts. |
| The female reshape | Moves arms and legs by under a centimeter (the female upper arm is about 8 mm thinner than the male); female and male pieces deform the same in every frame. |
| Weights near the cut lines | The pieces keep the original skin weights and the cuts only split faces, so pieces meet exactly in every frame; the close-ups show no opening or kink at elbow, waist or knee. |

**The fix:** the renders now use the game's own animations. `anim.py` retargets the clips the player animator plays
(`Assets/Animations/PlayerAnimationController.controller`): the relaxed `Idle` (RPG Character pack,
`Relax/RPG-Character@Idle`), `Unarmed-Run-Forward`, `Sword-Attack-R1`, the `Armed-Idle` combat stance and the Crafting
pack's `Chop-Finish` (woodcutting) and `Fishing-Cast`. Those FBX files have no armature, only animated empties
(`B_Pelvis`, `B_L_UpperArm`, ...); the clip's rest pose and humanoid bone map are read from its `.meta`, the
player's bone map from `ModularCharacters.fbx.meta`, and each humanoid bone gets the world rotation it has relative
to the clip's rest put onto the game rig relative to its T-pose, like Mecanim does (with half the hand's roll moved
onto the forearm, the avatar's `foreArmTwist 0.5`). The Crafting pack's avatar was made from a model standing with its
arms down; its rest is put into a T-pose first, as Unity's Enforce T-Pose does. A plain game character in the same
frames (the survey sheet, first column) moves like the game.

## Shoulder pads

The game's own pads (`Chr_ShoulderAttach*`) are rigid on a `Shoulder_Attachment` bone under the clavicle, sit well out
(the male plate pad 20 reaches x 0.40 m, center x 0.23, top 1.64 m) and are one set for both sexes. The Knight's
pauldrons hug the shoulder (center x 0.19, out to 0.31, top 1.56) and are skinned half to the clavicle and half to
the upper arm, so they follow a raised arm part way instead of floating above it. As a rigid game pad they would lose
that, and in the shared array the female would wear the male one. **Decision: baked into the upper arm pieces** (the
knee caps likewise into the hips piece). The chest item then carries `NoShoulderPads` (and the leggings
`NoKneePads`), which also keeps a big game pad from being put on top. `cut.cut(pads="separate")` still makes them
loose parts if the arrays are wanted later. The pads sheet shows the baked pauldrons next to the game's pads and
the plate set, male and female, in the idle and the sword attack.

## Skin

How the game colors skin: every body part uses `FantasyHero.mat` (shader `PolygonFantasyHeroCharacters/Shaders/Synty.shadergraph`),
which starts from the atlas and lerps in flat colors by mask channels; skin is Mask_03 red (black = skin), filled
with the flat `_Color_Skin` that `SyntyPlayerAppearance` sets from the player's `SkinColor` (stubble is Mask_03 blue,
`_Color_Stubble`). The Dark Fortress atlas keeps its skin in a block of swatches in the lower left, marked by
`PolygonGeneric/Textures/SkinMask.png`, and its own `Generic_Standard` shader tints them with `_Skin_Color` - which
would not match the MFH shader's flat skin.

**Solution (`skin.py`):** every face whose UV lies in the skin swatches goes to a second material slot, and its UVs
move onto a skin texel of the MFH atlas. In the game that slot gets the player's own body material, the same one their
bare arms use, so the skin on the armor is the skin next to it, for every color, with no new shader. The robes'
boots borrow the game's own bare shin the same way (below). Barbarian (7 % of its body faces are skin: forearms, chest
opening, hands), Necromancer (8 %: upper arms, hands) and Apothecary (10 %: hands) need it; Knight and Inquisitor have
none. The skin sheet shows all three in a light, a medium and a dark tone, male and female.

## Robes

Inquisitor and Apothecary (and Plague Lord) wear skirts to the ankle. With the knee cut a robe was half leggings, half
boots. Now `{"robe": 1}`: the whole skirt is the leggings piece; the boots are only the faces on the foot bones (the
shoes), plus the game's own bare shin above them in the skin slot (`cut.add_bare_shins`), because the game's boots
slot replaces the whole lower leg and other leggings would leave the shins missing. On the male the game shin is
thicker than the Inquisitor's boot shaft, so with bare legs the boot reads as a low shoe. In wide lunges the skirt
stretches between the legs as on the original character (Synty weights it to both thighs).

## Suitability of all thirteen characters

From `survey_v2.py` (picture: the survey sheet). Numbers: how far the surface lies outside the game's male body on
average (bulk), the share of vertices more than 1 cm inside it (thin, skeletal or rotten), the 95th percentile
outside (silhouette), and the share of bare skin faces (head excluded).

| Character | Rating | Bulk torso / arms / hips (cm) | Inside torso / arms | Why |
| --- | --- | --- | --- | --- |
| Knight | **good** | 1.1 / 0.5 / 1.7 | 18 % / 6 % | Plate on our body shape, no skin. The first set to build. |
| Barbarian | **good** | 0.9 / 0.8 / 2.2 | 10 % / 3 % | Leather and fur, upright, bare forearms and chest opening take the player's skin. |
| Necromancer | **good** | 0.9 / 0.9 / 1.2 | 16 % / 5 % | Dark leather armor, upright; bare upper arms take the player's skin; a glowing green tabard (the pack's emissive). |
| Inquisitor | with work | 1.6 / 1.1 / 4.8 | 4 % / 0 % | Robe (whole skirt as leggings, boots = shoes + game shin); high collar fits the game head. |
| Apothecary | with work | 0.3 / 0.7 / 5.2 | 18 % / 6 % | Robe with apron and pouches; hands are skin. |
| Plague Lord | with work | 2.9 / 3.1 / 4.3 | 1 % / 0 % | Bulkiest; spiked gauntlets and belt are good, but the look leans on the tall hat (a head piece) and a ragged robe. |
| Plague | not suitable | -1.5 / -0.1 / -0.6 | 63 % / 30 % | A rotten, skinny body (63 % of its torso is inside ours), not clothing. |
| Undead 01 | not suitable | -0.7 / -0.6 / 0.3 | 35 % / 36 % | A skeleton. |
| Undead 02 | not suitable as a set | 0.6 / -0.1 / 0.2 | 23 % / 24 % | Armor plates over a skeleton; the bones show between them. |
| Wraith | not suitable as a set | 0.7 / 0.3 / 4.5 | 6 % / 16 % | A ghost: tattered robe over a thin body, glowing hands. |
| Corpse | not suitable | -1.4 / -1.1 / -1.3 | 52 % / 57 % | A naked body (all of it is skin). |
| Behemoth, Colossus | not suitable | - | - | Their own bigger rig (`BR_Characters.fbx`, arm span 2.88 m). |

Posture: all of them share the skeleton, so none is bent in its bones; the zombie look of Plague, Undead and Corpse
is in the mesh (thin limbs, rotten or bony bodies), which the "inside" column catches.

**Single pieces worth taking**, even from characters that are not suitable as a set:
- `SM_Chr_Attach_Armor_01` (a pair of plate shoulder guards) and `SM_Chr_Attach_Fur_01` (a fur mantle): loose
  attachments of the pack that sit on the game's body as they are (survey sheet, last two columns). Good as shoulder or
  back pieces.
- Plague Lord's spiked gauntlets (gloves) and its belt with pouches.
- Undead 02's greaves and boots and its pauldrons (plate, but cut from a skeleton: needs a look at what lies under).
- Wraith's fur collar mantle.
- The pack's hoods (`SM_Chr_Attach_Hood_01-03`) are head pieces, the other agent's part.

## Second pass renders

All in `docs/mockups/`, retargeted game animations, medium skin tone unless the sheet says otherwise:

| File | What it shows |
| --- | --- |
| `dark-fortress-armor-test-v2-survey.png` | Every character beside the plain game body: idle front, side, back and a run frame, plus the two loose attachments on the game body |
| `dark-fortress-armor-test-v2-<knight, barbarian, necromancer, inquisitor, apothecary>-anim.png` | Original vs fitted male vs fitted female in the idle, run, sword attack and woodcutting chop; front, back, side and the game camera |
| `dark-fortress-armor-test-v2-<same five>-mix.png` | The pieces worn with the game's own parts (bare body, plate set), five mixes per sex, idle and sword attack, front, game camera and back |
| `dark-fortress-armor-test-v2-skin.png` | Barbarian, Necromancer, Apothecary in a light, a medium and a dark skin tone, male and female |
| `dark-fortress-armor-test-v2-knight-pads.png` | Baked pauldrons vs the game's shoulder pads vs the plate set |
| `dark-fortress-armor-test-v2-knight-seams.png` | Knight close-ups: neck (game camera, behind), shoulders, elbow, waist and knee in game frames |

What I would still look at (critical review): the male MFH head's stubble band reads dark on the medium and dark
tones in Workbench (the game draws it with `_Color_Stubble`); robe skirts in deep lunges; the Necromancer's glowing
tabard needs the pack's emissive map in the game material.

## What changed for wiring (on top of the first pass list below)

- Each piece has two material slots: 0 the set's own material (Dark Fortress atlas), 1 the player's body material. The
  equip code assigns one material per part today (`renderer.sharedMaterial`); it would need to set
  `sharedMaterials = { item material, body material }` for pieces with a second slot (the body material is the one
  the `_00` body parts keep).
- Pads baked: chest items with `NoShoulderPads`, leggings with `NoKneePads`; no new pad indices.
- Robe sets: leggings = the skirt, boots = shoes with the game's shin.
- `export.py` writes all of this (`DF<Name>_<Sex>.fbx`, slot 1 named `PlayerBody`).
- Time per character is unchanged (seconds of compute, 30-60 minutes of review); a robe adds the `robe` flag.

## Second pass scripts

`anim.py` (game clips onto the Blender rig), `skin.py` (skin slot and tone textures), `survey_v2.py`,
`render_v2.py`, `compose_v2.py`; `cut.py` gained baked pads, robe mode, caps only on cut openings and
`add_bare_shins`; `export.py` writes the skin slot and robes. Example:
`blender -b --factory-startup --python tools/dark-fortress-armor/render_v2.py -- <repo> <out> SM_Chr_Inquisitor_01 anim,mix '{"robe": 1}'`,
then `python tools/dark-fortress-armor/compose_v2.py <out> <survey out> docs/mockups`.

# First pass (kept for reference; pads, poses and the candidate list are superseded above)

## Renders

All in `docs/mockups/`, Workbench renders with the pack's own texture and flat shading, back faces culled:

| File | What it shows |
| --- | --- |
| `dark-fortress-armor-test-apose.png` | Original Knight, fitted male, fitted female; front, back, side, three-quarter |
| `dark-fortress-armor-test-poses.png` | The same three with arms raised, a walking stride and a combat stance; front, three-quarter, game camera |
| `dark-fortress-armor-test-mix-apose.png`, `-mix-combat.png` | Knight pieces worn with existing game parts (bare body, the plate set), five mixes per sex |
| `dark-fortress-armor-test-seams.png` | Close-ups where items meet: neck (game camera and behind), shoulders raised, hips, elbow, waist, knees |
| `dark-fortress-armor-test-<inquisitor, barbarian, plague_lord, apothecary>-poses.png` | The four other characters through the same scripts (original, male, female in three poses) |

## Rig comparison

Measured with `tools/dark-fortress-armor/inspect_rigs.py`:

- **Same skeleton.** Every joint the two rigs share (Root, Hips, Spine_01-03, Neck, Head, Eyes, Eyebrows, Clavicle,
  Shoulder, Elbow, Hand, UpperLeg, LowerLeg, Ankle, Ball, Toes, all fingers) is within 0.07 mm. Both are in a T-pose,
  1.79 m tall, arm span 2.05 m. Differences in bone tails are only Blender's guess for leaf bones.
- **Same body.** The Dark Fortress characters are dressed versions of the MFH male body: same height, shoulders,
  waist, hands (the bare MFH hand and the Dark Fortress hands have identical bounds).
- **Bone names differ in two places.** Dark Fortress fingers are `Finger_01_L` / `Finger_01_R` (also `Thumb_*`,
  `IndexFinger_*`); the MFH rig calls both sides `Finger_01` (under `Hand_L` / `Hand_R`). Dark Fortress has a `Jaw`
  bone (head only, dropped with the head). MFH has attachment bones the Dark Fortress rig lacks
  (`Shoulder_Attachment_L/R`, `Elbow_Attachment_L/R`, `Knee_Attachment_L/R`, `Hips_Attachment`, `Back_Attachment`,
  `Chest_Attachment`, `Head_Attachment`, `Cape_Attachment_01-04`); none are needed by the cut pieces.
- **Scale.** The Dark Fortress FBX imports with the armature at 0.01 (centimeters inside). Blender's importer brings
  the MFH file in with bones in centimeters and meshes in meters, so posing the MFH armature in Blender tears the
  meshes apart; the scripts therefore pose everything on the Dark Fortress rig (MFH meshes baked to world space and
  re-bound, finger groups renamed). Unity imports both correctly.
- **MFH naming quirk.** Male `Chr_LegLeft_Male_*` parts sit on the character's right side (x < 0); female
  `Chr_LegLeft_Female_*` on the left. Arms and hands are `Left` at x > 0 for both. The export names the pieces after
  whatever the MFH base part on that side is called.

## Where the MFH parts end (the seams)

From `seams.py`: the MFH parts are closed shells that overlap, not open edges that meet.

| Part (male) | Range | Meets |
| --- | --- | --- |
| Head | z 1.436 to top (female 1.419) | torso top 1.506 (the neck overlaps by 7 cm) |
| Torso | z 0.83 to 1.506, out to x 0.247 | upper arm from x 0.19, hips from z 0.936 |
| Upper arm | x 0.19 to 0.526 | lower arm from x 0.51 (elbow) |
| Lower arm | x 0.51 to 0.80 | hand from x 0.79 (wrist) |
| Hips | z 0.381 to 0.936 (pelvis and thighs) | legs from z 0.401 (knee) |
| Legs | z 0 to 0.401 (shin and foot) | |

So the cuts between items go inside these overlaps: elbow |x| 0.52, waist z 0.90, knee z 0.40 (wrist |x| 0.795 and
torso/upper arm only matter within one item).

## Method, step by step

Scripts in `tools/dark-fortress-armor/`, all for headless Blender 5.2
(`"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python <script> -- <repo> <out dir> [character] ...`):

| Script | Does |
| --- | --- |
| `dfa_lib.py` | Imports both FBX files, binds the MFH meshes to the Dark Fortress rig, poses (A-pose, arms raised, stride, combat), Workbench render helpers |
| `inspect_rigs.py` | Rig comparison to JSON |
| `seams.py` | Where the MFH base parts end, to JSON |
| `survey.py` | The test characters next to the MFH bodies, and each character's loose parts with their bones |
| `cut.py` | The cut (below) |
| `fit.py` | The female fit (below) |
| `check_cut.py` | A character, its pieces put back together, and the pieces pulled apart |
| `render_test.py` | All comparison renders for one character (`apose,poses,mix,seams`, optional cut overrides as JSON) |
| `compose.py` | System Python with Pillow: labeled sheets from the renders |
| `export.py` | The FBX files for Unity, one per sex |

1. **Import.** The character FBX and `ModularCharacters.fbx`. Bake the character mesh to world meters (Z up, facing
   -Y). Nothing else is needed to "re-skin": the weights are already for this skeleton.
2. **Female shape key, before any cut** (`fit.add_female_key`). For each MFH base body part (`_00`, subdivided twice),
   every male sample point is projected onto the female part on the same side (nearest surface point), giving
   "here the female surface is this far away". Each armor vertex moves by the inverse-distance mean of its 10
   nearest samples, and the moves are then smoothed 8 times along the mesh so a plate moves as one (without the
   smoothing the hanging front flap of the Knight was pinched between the female thighs). The result is stored as a
   shape key on the uncut mesh, so it rides through the cuts and pieces still meet exactly.
3. **Sort loose parts by their bones** (`cut.cut`). Pauldrons (Clavicle + Shoulder, 50/50) become shoulder pads;
   knee caps (LowerLeg near the knee) become knee pads; eye and eyebrow bits go with the head.
4. **Cut between items with planes** (bmesh `bisect_plane`, which interpolates UVs, weights and the shape key):
   elbow |x| 0.52, wrist |x| 0.795 (arm faces only), waist z 0.90 and knee z 0.40 (body column only). Faces go to
   regions by centroid; torso against upper arm by bone weight (Shoulder + Elbow above half).
5. **Drop the head**: faces weighted to Head / Eyes / Eyebrows / Jaw, and the character's own neck where it lies
   inside the MFH male head mesh (hidden on a male anyway; on the thinner female neck it poked through as dark
   specks).
6. **Cap every opening** with a fill in a dark texel of the atlas, so the elevated stream camera cannot see into a
   hollow piece. The neck opening gets a funnel sunk 10 cm into the chest instead of a flat lid, so it can never cut
   through the player's own neck. Shading stays flat; UVs are the pack's.
7. **Female pieces** (`fit.morph_female`): the shape key positions, then a push-out pass: no vertex may be deeper in
   the female body than it was in the male one (a pure function of position, so seams stay closed); collar vertices
   keep their depth against the female neck. Pads are not moved: the game's pad arrays are shared by both sexes, and
   the field at the pads is only 4-6 mm.
8. **Export** (`export.py`): `DF<Name>_Male.fbx` (14 pieces including the pads) and `DF<Name>_Female.fbx` (10 pieces),
   rig plus pieces, named like the MFH parts (`Chr_Torso_Male_DFKnight`, `Chr_ArmUpperLeft_Female_DFKnight`,
   `Chr_ShoulderAttachLeft_DFKnight`, ...). A round trip (import the FBX into an empty Blender scene, pose it) deforms
   the same as before export. The files were left in the session scratchpad and not added to the project, since the
   Unity step was skipped; `export.py` remakes them in seconds.
9. **Review** with `render_test.py` and `compose.py`; per character, look at the seams sheet before anything else.

To do another character: `render_test.py ... SM_Chr_<Name>_01` and look; if the waist or knee cut falls badly, pass
overrides, e.g. `'{"knee": 0.12}'`, to `render_test.py` and `export.py`.

## What worked

- Male fit: the pieces are the original, so there is nothing to fit. Neck, elbows, waist and knees match the original
  in every pose; the Knight collar takes the MFH head cleanly from the game camera and from behind.
- Mixing with existing parts works both ways: Knight chest over plate or bare hips, plate chest over Knight hips,
  Knight gloves on bare or plate upper arms, Knight boots under bare thighs. The cuts sit inside the MFH overlaps, so
  no mix showed a gap.
- Female fit: waist narrower, chest and hips follow the female body, the arms and legs barely change (they are
  almost the same mesh). Stride and combat stance deform as on the male.
- All four other characters went through unchanged scripts with no new problem in A-pose, stride or combat.
- Time: the scripts cut, fit and export one character in about 10 seconds; all its review renders take about 20
  seconds. Most of the time per character is looking at the sheets and, for some, picking cut heights: roughly
  30-60 minutes a character once this exists, longer for one that needs hand fixes.

## What did not work, or needs care

- **Material.** The pieces are UV mapped to the Dark Fortress atlas. The game puts a tier's tint material on an armor
  part (`ItemManager.GetMaterial(item.Material)`), which uses the MFH mask atlas: on these pieces it would paint
  nonsense. Each Dark Fortress set needs its own material on the Dark Fortress texture. The pack has 12 colorways
  (`Textures/Alts/PolygonDarkFortress_Texture_01..04_A..C`) that could become tiers.
- **Skin.** The Dark Fortress skin is a pale grey in the atlas (Barbarian's and Apothecary's bare hands, the
  Barbarian's chest opening). The pack's own material (`PolygonGeneric/Generic_Standard` with `_Skin_Mask` and
  `_Skin_Color`) can tint it, so `WithSkin` would set `_Skin_Color` to the player's skin color the way it does for the
  class gear's skin parts.
- **Robes are cut at the knee.** Inquisitor, Plague Lord and Apothecary skirts end up half in the leggings and half
  in the boots. Worn together they look like the original; with other boots the skirt stops at the knee. For robes,
  cut the knee low: `{"knee": 0.12}` on the Inquisitor (tried, renders in the session only) keeps the skirt down to
  the ankle in the hips piece, leaves the boots as the feet and the last few centimeters of hem, and deforms the
  same. Or treat the leggings and boots as one set.
- **Head pieces are not part of this.** The heads are dropped; the Plague Lord's tall hat is in the head and was
  dropped with it. The pack's separate helmets and hoods are the other agent's work.
- **Small dark notch at the throat.** Where the collar opening is lower than the MFH neck at the front, a few pixels of
  the collar's dark inside show below the chin (male and female). It reads as
  the collar's shadow; nothing pokes through.
- **The female chest** follows the female body but has no shaped breastplate: the plates just move out. Fine at game
  distance, plainer than Synty's own female armor.
- **Unity untested.** Bindposes, bone remapping (finger names) and the mesh combiner were not tried in the editor.

## Good candidates

Judged from the survey and the renders:

| Character | Fit | Notes |
| --- | --- | --- |
| Knight | very good | Plate with pauldrons and knee pads that split out as the game's pad parts. The best first set. |
| Inquisitor | good | Long robe with a high collar: needs the low knee cut. A strong caster look. |
| Barbarian | good | Leather and fur, bare forearms: needs the skin tint. Fits the Barbarian class line. |
| Apothecary | good | Robe with apron and pouches; bare hands need the skin tint. Healer look. |
| Plague Lord | fair | Spiked gauntlets and torso work; the look depends on the tall hat, which is a head piece; robe skirt. |
| Necromancer, Undead 01/02, Wraith, Plague, Corpse | not checked | Same rig and base body, so the method applies; Wraith and Corpse are likely too ragged or monstrous for player gear. |
| Behemoth, Colossus | no | Big creatures, not on this body. |

## Wiring them in as items (not done in this test)

1. **Prefab parts.** An editor script imports `DF<Name>_<Sex>.fbx`, makes each piece a SkinnedMeshRenderer under the
   Player prefab's model, bound to the player's own bones by path (`Finger_01_L` -> `Hand_L/.../Finger_01`, likewise
   the thumbs and index fingers), with bindposes taken from the player's bind pose (or from an MFH body part's
   SkinnedMeshRenderer for the same bones). Then appends them to the arrays: `maleTorso` / `femaleTorso`,
   `maleArmUpperLeft/Right` and female, `maleArmLowerLeft/Right` and female, `maleHandsLeft/Right` and female,
   `maleHips` / `femaleHips`, `maleLegsLeft/Right` and female, `shoulderPadsLeft/Right` and
   `kneeAttachmentsLeft/Right` (shared by both sexes). The new indices are what item model ids name. A preview can
   do the same on a copy of the prefab in a preview scene without saving it (as `tools/items/class-gear-preview.cs`
   does for the class gear).
2. **Items.** Chest: torso index, shoulder pad index, upper arm index, with `HeadMask.NoElbowPads` (the Knight's
   elbows are in its arm pieces). Gloves: hand index, lower arm index. Leggings: hips index, knee pad index (or
   `NoKneePads` for robes). Boots: leg index. Male and female model ids separately, as now.
3. **Materials.** One new ItemMaterial per set (or per colorway tier), whose material is the Dark Fortress texture
   with the pack's shader or a copy of the game's shader that takes a plain albedo and a skin mask; that means
   rebuilding RavenNest.Models.dll and adding the materials to the serialized `itemMaterials` arrays, as for the class
   gear. Skin parts listed in `SkinParts` so `WithSkin` gives them the player's skin color.
4. **Website figure.** `tools/characters/export-gear-web.cs` exports every part in the prefab arrays already, but the
   website's materials are the MFH mask atlas with seven tint colors; `gear.json` and `rf-character-gear.js` need a
   material kind "own texture" (albedo plus skin mask) and the Dark Fortress texture copied beside the MFH one.
5. **Icons and stats** like any other gear line (the Item Icon Generator scene; ItemLines for path stats).
6. **Head pieces**: the pack's helmets and hoods (the other agent's gear) complete a set; hoods keep the face
   (HeadMask flags as for the class gear's hats).
