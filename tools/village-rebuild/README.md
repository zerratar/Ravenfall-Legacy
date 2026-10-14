# Village rebuild scripts

Editor scripts that rebuilt the home island town in October 2026: the flat plateau, the coast, the
tree belt and the five town hall tiers. They run in the open Unity editor through the Unity CLI:

```
unity command eval_file --file <script.cs> --timeout 600
```

The Unity CLI lives in `C:/Users/kaaru/AppData/Local/Unity/bin`. An eval script is a plain list of
statements:

- No `using` lines. Write fully qualified names (`UnityEngine.Vector3`).
- A bare `return;` does not compile. Use `return null;`.
- Results come back through `UnityEngine.Debug.Log`. Read them with
  `unity --json command console --tail 5`.

Each script saves the scene itself where it changes it. The results are already in
`Assets/Scenes/MainWorld.unity` and `Assets/New Terrain.asset`. Rerun a script only to change
something.

## Scripts, in the order they were run

| Script | What it does |
| --- | --- |
| `flatten.cs` | Levels the town into one plateau at about 8 m. The north side slopes gently to the beach and the bridge, and the sea sides drop as cliffs. Edits `Assets/New Terrain.asset`. It reads the current heights, so it can be rerun. |
| `paint_cliffs.cs` | Paints splat layer 1 on steep ground. The `Triplanar Terrain` shader shows pale sand with a rock normal map on any slope past about 16 degrees. Layer 1 is its darkening "road" mask, which turns those faces into dark stone. Replace `CLIFFWEIGHT` with a float first (0.65f was used). |
| `landscape.cs` | Rebuilds `Main Island/Environment/Town Landscape`: mossy rocks on the cliff rim, boulders at the waterline, a tree belt, groves and flowers. It also puts every plot back on the ground. It is seeded, so a rerun gives the same result. |
| `hall_common.cs` | Shared header for the hall scripts. Holds the helpers `Put`, `Patch` (ground mesh saved in the scene), `Fire` (from the kept torch), `WallRun`, `Tower`, `Banner`, `HatOn`, `Keep` and `FitCollider`. |
| `hall_tier1.cs` ... `hall_tier5.cs` | One town hall tier each. Each one replaces the tier's `Hall` child and moves the kept pieces (resources, info point, streamer flag, lady, QR banner, jester, guards) into place. |
| `hall_finish.cs` | Places each tier's resource yard by its bounds, and moves default plots out of the tier 5 footprint. Leaves tier 5 active and saves. |
| `navmesh_full.cs` | Rebuilds the `WORLD` navmesh over the full world bounds and saves it. |
| `navmesh_seabed.cs` | Adds the `Sea Floor Not Walkable` volume under `WORLD` (y -31 to -4, the whole terrain). Run `navmesh_full.cs` after it. |
| `models_import.cs` | Sets up the models in `Assets/Village/Models` (see below): maps their materials onto the kit materials and makes the stained glass material. Run after building a model. |
| `swap_castle_pieces.cs` | One-off: put the Blender towers and the frontispiece into tiers 4 and 5 in place, without rebuilding the halls, so hand edits survive. Run as `hall_common.cs` + this file. Does not save. |
| `hall_shot.cs` | Renders one tier on its own to `Temp/village-shots/`. Replace `TIER` (1-5) and `DIST` (32f to 70f) first, and run it twice (see the ghost trap below). |
| `shot_island.cs` | Renders overview shots of the whole town to `Temp/village-shots/`. |
| `gallery.cs` | Renders a contact sheet of the prefabs listed in `gallery.txt`. Used to pick kit pieces. |

The hall scripts are run as one file: `hall_common.cs`, then any tier files, then
`hall_finish.cs`, then a final `UnityEngine.Debug.Log(sb.ToString());`. For example, to rebuild
tier 3 only:

```
cat hall_common.cs hall_tier3.cs hall_finish.cs > run.cs
echo 'UnityEngine.Debug.Log(sb.ToString());' >> run.cs
```

## Models made in Blender

`blender/` builds our own pieces from code, in the low poly Synty style, headless:

```
sh blender/run.sh tower        # or clock_tower, frontispiece, goose
```

Each script exports to `Assets/Village/Models/` and renders previews to `Temp/village-shots/`. Then run
`models_import.cs` in the editor. The FBX files and their metas are versioned (they are ours, unlike the
kits), so the scripts are the source and the models can be rebuilt at any time.

| Script | Model | Used in |
| --- | --- | --- |
| `clock_tower.py` | Clock and bell tower, modelled in the space of `SM_Bld_Preset_House_10` | Tier 3. The house's own small tower is trimmed off a copy of its mesh (`TrimAbove` in `hall_common.cs`, saved under the git-ignored `Assets/Village/Derived`). |
| `tower.py` | Round towers without the kit's stacked doorways: cone, onion and needle roofs, plus a corbelled turret | Tiers 4 and 5 |
| `frontispiece.py` | The tier 5 centre bay from the ground to a gable: buttresses, portal, lancets, rose window | Tier 5, in the two middle front wall slots |
| `goose.py` | Sir Honksworth the Crowned, the fountain goose | Tier 5 |
| `house_*.py` | One house per building type (see Houses) | Every plot |

### Houses

Every building type has its own house, built by `blender/house_<type>.py` on a shared kit:

- `house_kit.py`: timber framed cottages (stone plinth, plaster walls, a gabled roof in any of the kit's tile or
  slat colours), windows, doors, signboards with an emblem per trade, yards.
- `house_props.py`: the shared props (fences, trees, hay, sacks, logs, dummies, a well, baskets, rocks, boats) and
  the round hut.

Each house is a lot of about 9 x 9 m facing -z, towards the village camera and the build mode card camera.
To rebuild one and put it in the game, with the editor open:

```
sh house.sh Mining
```

That runs the Blender script, `models_import.cs`, `houses_synty.cs` and `houses_prefabs.cs`, then `house_shot.cs`
for three review shots in `Temp/village-shots/unity_<Name>_0..2.png` (two corners and a low close-up).

**Synty props, not modelled ones.** Anything a Synty pack has (barrels, crates, dummies, racks, benches, banners,
weapons, tools, planters, potions, boats, trees and many more) is placed, not built: the shared prop functions
and a house's own `anchor()` calls leave an empty named after a key, and `houses_synty.cs` puts the prefab for the
key there and bakes the house and every prop into one mesh with one part per material, written as a binary FBX to
`Assets/Village/Models/Baked`. The key table at the top of `houses_synty.cs` gives each key its prefab, the size it
gets (and along which axis it is measured: an upright tool is measured by its height, not its width), a turn so it
faces -z, and whether it stands on its pivot, sits on the ground or is centred over the anchor. The anchor's name
carries its turn, tilt, roll and scale, because transforms change axes on the way into Unity. Only what Synty lacks
is modelled: the cottages, the trade signs and emblems, the easter eggs, and centrepieces such as the forge, the
oven, the sailboat, the mine and the cauldron. The Polygon Nature trees use a tree shader that draws pink here;
the pine key uses the Vikings one.

**Yards.** A house no longer carries a flat dirt pad (it sat on the grass like a plate). `yard()` leaves a marker,
and `houses_prefabs.cs` gives the prefab a `HouseYard` for it: a patch of the terrain's own dirt or gravel, drawn
with the road shader, laid over the ground wherever the house stands and fading raggedly into the grass.

`house_lineup.cs` lines them all up for review shots, in three runs.

The old prefabs stay in `Assets/Prefabs/Buildings/Objects`. To go back for a type, point its definition's
Prefab at the old one: Lumbermill, Magic House, archery_range, Barracks, Cooking House, Crafting House,
Farmers House, Fishers Log and Healing Tent. Mining, Sailing, Slayer, Gathering and Alchemy all used
DefaultHouse.

Easter eggs in the houses: a beaver at the lumbermill, a black cat on the wizard's step, an apple shot
through on the archery range, a knocked-over dummy at the barracks and the same dummy bandaged at the
infirmary, a pie on the bakery windowsill, a horseshoe over the smithy door, a pumpkin-headed scarecrow, a
rubber duck in the fisher's tub, a gull on the boathouse ridge, glowing eyes in the slayers' cage, and a
garden gnome by the forager's hut.

How they fit the kits:

- **Materials by name.** Faces use materials named after the kit materials (`PolygonFantasyKingdom_Mat_01_A`,
  `..._Castle_Wall`, `..._Castle_Wall Darker`, `..._Castle_Roof`), or after our own copies of them with other kit
  textures (`Village_Wall_Stucco`, `Village_Roof_Tile_Red` and so on, `DERIVED` in `common.py`). `models_import.cs`
  makes those copies and remaps every slot, so the
  new pieces share the kit textures. Palette colours are picked by collapsing a face's UVs onto the nearest
  swatch of the palette atlas, as Synty does. Brick and roof tiles are box or cylinder projected at the kit's
  density (5 m per U and 2.5 m per V for brick).
- **Axes.** Blender x is Unity -x and Blender y is Unity -z. A piece that faces +y in Blender faces -z
  (south, towards the plaza) in Unity. Wall pieces keep the kit pivot (Unity x -5..0).
- **Face directions.** Unity culls back faces, so an inside-out face shows as a hole. Previews cull back faces
  too. Models built from open surfaces pass `recalc=False` to `finish()` and orient every face themselves;
  Blender's normal guessing gets flat glass and open tubes wrong.

## Things learned the hard way

- **Navmesh bounds.** `NavMeshBuilder.UpdateNavMeshData` keeps only the tiles inside the bounds you
  give it. Updating with town-sized bounds wiped the navmesh for every other island. Always pass the
  surface's full world bounds; `navmesh_full.cs` does. The town halls are not under `WORLD`, so they
  are not part of the navmesh, the same as before.
- **Sea floor.** The terrain's floor at y -30 covers the whole map, and without the sea floor volume
  it baked as walkable: 6.1 of 6.5 million m2 of navmesh was ocean floor. A teleport over water
  (the Eldara spawn sits past the end of its dock) snapped players onto it with no way back. The
  water surface is at y -3.31, so the volume starts at -4; the dungeons sit at -56 and below,
  outside it.
- **Editor time limit.** The eval server gives up after about 5 s on the main thread, while the
  script keeps running. A script that renders in a loop (one tried hundreds of renders) can hang
  and crash the editor. Keep renders to a handful per eval.
- **Ghosts in offline renders.** The GPU Resident Drawer applies activation and deletion at the next editor
  frame. A `Camera.Render` in the same eval as a `SetActive` or `DestroyImmediate` still draws the old
  objects. Render in a separate eval from any scene change; `hall_shot.cs` switches tiers on its first run
  and renders on the second.
- **Main Island trigger.** "Main Island" is a large sphere trigger, not ground. Raycasts against the
  ground need `QueryTriggerInteraction.Ignore`, and must skip the "Do Not Walk" cube.
- **Castle kit grid.** Fantasy Kingdom castle pieces sit on a 5 m grid. Walls span -5..0 on their
  local x and are 5 m tall. Round tower segments stack every 5 m, and the `Roof_Cap_Round_*` cones
  fit them.
- **Preset houses.** Their pivot sits at ground floor level, with 2.5 m of foundation below, so they
  stand straight on the flat plateau.
- **Untracked assets.** Sir Clucksworth (tier 5 fountain) comes from `Assets/Animals_FREE`, which is
  not in git. The ground materials in `Assets/Materials/Village/` are git-ignored like the other
  art, and so is the navmesh asset.
