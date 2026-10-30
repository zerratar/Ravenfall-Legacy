# Moving the Ravenfall Lumbridge into MainWorld

Plan, 2026-10-30. The Lumbridge built from the OSRS map (Assets/Scenes/LumbridgeRavenfall.unity) replaces the old
Ravebridge island in MainWorld. Ravebridge stays what it is in the game: Island 9, event only, open to all levels,
navmesh area 4, the same quests, NPCs and hero mode. Only its ground and buildings change.

## What we are working with

**MainWorld today** (135 MB, in LFS):
- Ravebridge is `============ WORLD/Islands/Ravebridge` at (-1704, -6, 1522). Its land covers roughly 390 x 500 m.
- It stands on two terrains of its own (Ravebridge Terrain South and North, 2560 m each, 5 m per height sample,
  meeting at z 1707).
- Its root carries the IslandController, a trigger sphere (radius 240 round (-1640, 1610)) and a NavMeshSurface of
  480 x 140 x 480 round the same point.
- The ocean is one global surface at y -3.31.

**The Lumbridge copy** (82 MB):
- A 480 m terrain (0.94 m per sample), about 7200 renderers and 151 meshes baked into the scene.
- Those baked meshes are mostly the bent fences and walls, the roofs, the floors and the road meshes.
- Its sea is at y -0.47 and the terrain stands at y -6.
- It has no gameplay parts at all: no chunks, NPCs, enemies, resource fields or navmesh.

**Lucky fits:**
- Ravebridge's navmesh box is already 480 x 480 m, the size of the Lumbridge terrain.
- The Lumbridge castle is MainWorld's own Ravebridge castle, scaled 0.86. Everything placed against the castle can
  be carried over exactly with the castle's own move: spawn, camera target, duke, cook, kitchen, captain, courtyard.

## Decisions

**1. Merge the content into MainWorld itself** (not a prefab and not an additively loaded scene):
- **No prefab:** `.gitignore` ignores `*.prefab` and `*.asset`, so a prefab and its baked meshes would not be
  versioned. The scene file (LFS) is the only container git keeps.
- **No additive scene:** the client never loads a scene additively. `GameUpdater` hardcodes the build indices, and
  the island's gameplay lives in MainWorld anyway.
- **Cost:** MainWorld grows by roughly 40 to 60 MB, after the old Ravebridge (4600 renderers) goes and the switched
  off generated walls are stripped.

**2. Keep Lumbridge's own 480 m terrain:**
- Stamping it into the old 2560 m terrains would lose detail: 5 m samples against 0.94 m.
- The two old terrains stay as the sea floor round it.
- Their island land is lowered under the new terrain, and their edge is blended to the new terrain's edge depth.
- Their GrassFields go (no land left on them).

**3. Centre it where Ravebridge is:**
- The terrain's middle goes on the navmesh box's middle (-1640, 1610): the offset is (-1880, -2.84, 1370).
- The new castle then lands 20 m from the old one.
- The 2.84 m drop puts Lumbridge's sea (-0.47) on the ocean (-3.31). The terrain material's sea and beach heights
  move with it.

**4. The island's radius grows from 240 to 340 m.**
- Lumbridge's land reaches into the corners (339 m from the middle); 15 % of it lies outside the current sphere.
- The sphere decides both "on the island" (chunks, unstuck, saves) and the navmesh area 4 circle.
- No other island is within 1300 m.

## Before anything: the safety copy

1. **Your MainWorld edits.** `Assets/Scenes/MainWorld.unity` and `Assets/New Terrain.asset` have uncommitted
   changes (saved 2026-10-28 14:22 and 14:24). If they are yours, they get committed first, so the backup holds them.
2. **Commit and tag.** Commit, then tag `before-lumbridge-move`. LFS keeps the full scene, so the tag can always be
   checked out again.
3. **A local copy of the scene:** `Assets/Scenes/MainWorld_before-lumbridge.unity`. It stays local, like the other
   MainWorld copies already there.
4. **The files git does not keep**, copied to `Backups/before-lumbridge-move/` outside Assets:
   - `Assets/Scenes/MainWorld/NavMesh-Ravebridge.asset`
   - the two Ravebridge TerrainData assets and their .meta files
   - `ProjectSettings/NavMeshAreas.asset`
   - Note: the TerrainData .meta files were never in git, so a fresh clone would already lose the terrains.
5. **Restoring** is: check out the tag (or the local copy) and put those files back.

The move itself is a script (`tools/ravebridge/lumbridge-move.cs`), so a bad result is fixed by restoring and
rerunning, never by patching by hand.

## Phase 1: code, tested on today's world

1. **One ground lookup: the highest terrain under a point.**
   - Five places take the first terrain whose rectangle holds a point: `WorldRoad.GroundHeight`,
     `Ambience.TerrainUnder`, `TownHallManager`, `HouseYard` and `LavaCrack`.
   - `GrassRenderer` picks a field the same way, and `Boats` uses `Terrain.activeTerrain`.
   - With the new terrain over the old ones, that choice would depend on load order.
2. **Fences that block.** Decide if they should (question 3 below). If yes, `WorldFence` gets thin box colliders
   along its runs, on a layer the navmesh bake uses. Today the bent fences have no collider, so the navmesh runs
   straight through them.

## Phase 2: getting the Lumbridge content ready (in LumbridgeRavenfall)

1. **Remove what is only for the test scene:** Main Camera, Directional Light, Post Processing and Sea (test only).
2. **Remove what is only for the tools:**
   - the generated wall pieces the swaps switched off (Al Kharid keeps about 900 of them for reruns);
   - the props inside Al Kharid's closed buildings, which nobody can see.
3. **Move its assets where git keeps them**, with `.gitignore` and LFS rules for each:
   - the TerrainData, as a top-level `Assets/TerrainData_Lumbridge.asset` plus its .meta, so the LFS pattern covers it;
   - the terrain material, to `Assets/World/Terrain/`;
   - the river water mesh, to `Assets/World/Water/`;
   - the grass profile, to `Assets/World/Grass/`;
   - the roof, floor and terrace materials and their generated textures, to a new `Assets/World/Lumbridge/`.

## Phase 3: the move script (`tools/ravebridge/lumbridge-move.cs`)

1. **Open MainWorld and clear the old Ravebridge environment.**
   - Delete: Environment, Castle, Small Goblin House, Small House, Farmer House, Roads, Swamp Pools, Dressing, and
     the old Places markers.
   - Keep: the island root (IslandController, sphere, NavMeshSurface), `Playable` and the two terrains.
2. **Lower the old terrains.** Their land goes down under the new terrain's footprint, with a 40 m blend to its edge
   depth outside the footprint. Remove their GrassFields.
3. **Bring the Lumbridge root in** under the island at (-1880, -2.84, 1370), and rebuild its fences on the moved
   ground.
4. **Size the island.**
   - Sphere radius 340.
   - The NavMeshSurface stays 480 x 480; its height grows to cover the castle keep and the Al Kharid rooftops.
   - `Sea Floor Not Walkable` is reshaped to the new sea floor.

## Phase 4: putting the gameplay back

All of it lives under `Playable`. Names and keys stay as they are, because the quests, loot, hero mode and the
shop find things by name.

**Carried over with the castle's move:**
- SpawnPosition and Camera Rotation Target
- the duke, the cook and the captain
- the Castle Kitchen and the Cooking chunk
- the hero mode Courtyard

**Moved to their OSRS place in the new layout:**
- **Church:** the priest and the Graveyard.
- **General store:** the shopkeeper and the market stall. Crafting and Alchemy stand by it.
- **Windmill:** the miller and the Windmill place.
- **Bob's axes:** its house.
- **Fred's farm:** the Chicken Coop and the Gathering spots. Farming goes in the farm's fields.
- **Cow field, east of the river:** the Fighting Lv. 1 cows. Commaster and RavenMMO keep their names.
- **Goblin houses east of the river:** Fighting Lv. 5, bentnose and wartfist. The goblin war chief template stays in
  this chunk.
- **Swamp:** the ghost and Fighting Lv. 15 (soul). Mining goes in the swamp mine.
- **River and swamp coast:** Fishing, with its surface at -3.31.
- **Woods west of the castle:** Woodcutting.
- **The keeper:** placed wherever the quests' text places them.

**Resource fields are redrawn.** Woodcutting and Gathering hold world positions, so they do not move with a parent:
- they are drawn again on the new ground;
- the plain Lumbridge trees inside the woodcutting field give way to the field's own trees.

**Wave gates** keep their five names (Stone bridge, Wooden bridge, Desert gate, Swamp, Mill road) and stand at:
- the two bridge ends;
- Al Kharid's toll gate;
- the swamp road;
- the road to the windmill.

**Enemies** get "Assign Dependencies" again. Their spawn points come from where they stand.

**The camera** gets an `island-camera-settings.json` entry for the bigger island if the default feels too close.

## Phase 5: navmesh

1. **Make sure buildings block.** Buildings and walls block only if they have colliders: the bake uses physics
   colliders.
   - The Synty presets bring collision meshes; the Al Kharid pieces and the castle's need checking.
   - Where a building lets the navmesh through, it gets a "not walkable" box over its footprint (from `buildings.json`).
2. **Bake** with `tools/ravebridge/navmesh-bake.cs`. It already writes `NavMesh-Ravebridge.asset` and checks that the
   spawn reaches every chunk. Its paths to the chunks need updating after the move.

## Phase 6: testing (in play mode, local RavenNest)

1. **Start a Ravebridge event with test bots** and train every skill there. Watch for anyone stuck or failing to find
   a spot.
2. **Play the quests by hand:** talk to every NPC key, kill cows and goblins, collect from the coop, fish the river.
3. **Hero mode:** waves from all five gates, the courtyard, loot drops.
4. **Unstuck:** someone in each corner of the island.
5. **Leaving:** the event returns players to where they were.
6. **Frame rate** over the town and Al Kharid, normal and potato mode. If small props cost too much, potato mode gets
   distance culling for them.
7. **The rest of the world is unchanged:** a raid, a dungeon, the ferry, roads and grass on the home island.

## Phase 7: after

- **Commit the moved MainWorld and the new assets.** Update `docs/ravebridge-design.md` and the hero mode doc (old
  coordinates, the gates, the two terrains).
- **The Lumbridge build chain is frozen from then on.**
  - `lumbridge-castle.cs` cannot rerun, because it copies the old Ravebridge out of MainWorld.
  - `LumbridgeTest.unity` stays as the OSRS reference.
  - From then on, Ravebridge is edited by hand in MainWorld, and placements can be fixed freely.

## Questions for you

1. Are the uncommitted changes in MainWorld.unity and New Terrain.asset (2026-10-28 14:22 and 14:24) yours, to
   commit before the backup?
2. Should Al Kharid be part of the Ravebridge event area, or closed off? The wave gate "Desert gate" suggests it is
   in.
3. Should fences block walking, as they do in OSRS? Bots then path round them through the gates.
4. Is it fine that the old Ravebridge layout (farm, swamp pools, goblin houses) is deleted, with only the tag and the
   local copy keeping it?
