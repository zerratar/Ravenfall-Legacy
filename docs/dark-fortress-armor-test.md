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
