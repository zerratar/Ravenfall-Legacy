# World Resources: Trees and Gatherables as Data

Status: plan, for review. Nothing here is built yet.

Trees and gather spots become data in a field rather than a GameObject each, drawn instanced and handled by one
system. This is the first step towards data-oriented world objects; rocks, fishing and farm spots, village decor
and later NPCs can follow on the same base.

## Why

Measured in `Assets/Scenes/MainWorld.unity` (2026-10-14):

- About 188 trees (58 placed by hand, 130 prefab instances of `Tree Lv 1/90/600/1000`) and 199 gather spots.
- None of them runs an `Update`, so today's cost is overhead rather than work:
  - Every tree carries every variant it can turn into, 4 to 6 models, as inactive child GameObjects, plus a
    stump. The Lv 600 and Lv 1000 variants are Synty prefabs with a LODGroup of 3 levels and a MeshCollider
    each.
  - Gather spots register with `TaskObjectManager`, which walks all 199 twice a frame under a lock to call
    `Poll()`, an empty method. It also throws once it holds about 1,000, so it caps how many could exist.
  - Each player picking a new target filters the chunk's trees into a new list and sorts all of them.
  - Every GameObject is scene weight: the scene has about 28,000 already.
- So adding trees costs GameObjects, hierarchy and memory, not frame time, and that is what limits "a real
  forest". The new system adds no GameObject per tree, so a field of 10,000 costs about what one of 100 does.

What helps: the URP assets already run the GPU Resident Drawer (instanced drawing via BatchRendererGroup), and the
Synty materials these trees use draw through it, so the shaders take instanced data.

## What stays as it is

These are design choices, kept exactly:

- **Shared experience.** Everyone who has targeted a tree gets its experience when it falls, and everyone who has
  targeted a gather spot gets theirs, whether they hit it or not. On a crowded server most players would
  otherwise never gain anything. The targeter lists move into the new state; the rule does not change.
- **Experience comes from the player, island and chunk**, not the tree: `Chunk.CalculateExpFactor`, the island level
  ranges in `IslandManager`, and `GameMath.Exp`. A tree's level only feeds the damage roll.
- **The damage roll** in `GameMath.CalculateSkillDamage`, apart from one fix: the `Math.Abs` on the level difference
  lets a player far below a tree's level hit it hard. Step 3 makes it `Max(1, difference)`. Players can only train
  where they meet the island's requirement, so today only the Lv 50 chunk on Away (with level 90 trees) is affected.
- **Adaptive respawn**: when a quarter or less of a chunk's trees stand, respawn speeds up by a second, down to 1 s;
  at half or more it slows back to the type's time. Same for gather spots.
- **Target choice**: the nearest available spot, give or take a random 0 to 15 m, so players spread out. Done in
  one pass instead of a sort, which picks from the same distribution.
- **Walk-through.** Trees do not block walking (as today), standing or cut, and gather spots never do. No
  colliders and no navmesh holes.
- **Item drops** stay on the server (RavenNest), from the task argument the client already sends.
- **Resource state is local to the game client**, as today: no server change.

## Data

### Resource types (ScriptableObject assets)

`Assets/World/WorldResources/Types/<Name>.asset`, one per kind of tree or gather spot (not under a `Resources`
folder, so Unity does not pack them all into every build):

| Field | Meaning |
|---|---|
| `Kind` | Tree or Gatherable. |
| `Variants` | The looks an instance can have, picked per instance (today's random child). Each variant is a list of LOD levels, each LOD a mesh, its materials and the distance it shows to. |
| `Depleted` | The look once cut or gathered: the stump for trees, nothing for gather spots (they vanish until they regrow). |
| `Health` | Hits to cut it down (trees); gather spots deplete in one gather, as today. |
| `RespawnSeconds` | The base respawn time. |
| `ActionDistance` | How close a player must be to chop or gather: today's trigger SphereCollider radius (3 m). |
| `KneelToGather` | The gathering animation choice (gather spots). |
| `CastShadows` | Per type. |
| Brush defaults | Rotation, scale range and variant weights for painting. |

**A tree's level is the level its chunk requires**, not part of the type. The scene's per-instance levels already
match their chunk everywhere but Away, whose Lv 50 chunk has level 90 trees; those become 50. A type is therefore
only a look and its numbers, and one type can be used on any island.

The existing prefabs give the first types: `Tree Lv 1`, `Tree Lv 90`, `Tree Lv 150`, `Tree Lv 600`, `Tree Lv 1000`
and the 58 hand-placed palm/elf trees. Gather spots are grouped, each group picking a random look per spot:
Mushrooms, Garden plants, Flowers and Coconuts, plus the few one-offs (mushroom houses, fairies) as types of their
own. **Ravenfall > World Resources > Create Type From Selection** makes a type: variants from the tree's children
or the selected spots' looks, the stump, LOD meshes and distances from the LODGroups, and the numbers from the
`TreeController`/`GatherController`. Each look's parts are baked into one mesh per LOD (one submesh per material)
and saved to `WorldResources/Models/<Name>.fbx`. Those models are copies of kit meshes, so they stay out of git
like the kits; the type assets are versioned.

### Resource fields (component in the scene)

`ResourceField` (`[ExecuteAlways]`), one per chunk that has trees or gather spots, on the chunk's GameObject or a child
of it, so `Chunk` keeps knowing what it owns. It saves only plain data:

```
types:     List<ResourceType>            the types used in this field
instances: List<ResourceInstance>        one entry per tree or spot
  ResourceInstance { ushort type; byte variant; Vector3 position; float yaw; float scale; }
```

The position is world space, with the height taken from the ground when placed. Nothing generated is saved into the
scene, the same rule as `WorldRoad`. A field can also set `Tree health` and `Respawn seconds` for everything in it
(0 uses each type's own), since some chunks made the same trees tougher or slower: Ironhill's trees have 15 health
and come back in 20 s, Home's the same trees with 4 and 15 s.

## Runtime

### Targets and state

The player task API passes targets as `object`, so each instance gets one small `ResourceTarget`, made by its field
the first time the chunk asks and kept for the field's life. It holds the instance's live state (health, the
current respawn time, its targeters) and knows its `Position`, `Island`, `ActionDistance` and `Level` (the chunk's
requirement). No allocation per frame, and identity comparisons keep working. (The plan had the state in arrays in
one `WorldResources` system; with one object per instance needed anyway, keeping the state on it is simpler and
costs the same.)

Whether an instance is standing lives in the field, where the renderer reads it while culling. Each field keeps the
used up ones waiting to come back and checks them once a frame only when the next one is due, so a field with
nothing cut costs one comparison per frame. Picking a target is one pass (nearest standing, give or take 0 to 15 m)
instead of a sort.

Changes that follow from it:

- `WoodcuttingTask` and `GatheringTask` take the chunk's `ResourceTarget`s instead of `TreeController[]` /
  `GatherController[]`.
- `PlayerController.Cut`, `Gather`, `PlayerCombat.DamageTree` and `CalculateDamage` take a `ResourceTarget`.
- `LookAt` gets a position overload.
- `PlayerController.Target` returns a Transform, and resources have none. The stuck checks in `BotPlayerController`
  and `PlayerValidator` use the new `HasTarget`, `TargetPosition` and `TargetName` instead. `AnimationEventController`
  only shoots at combat targets, so it is unchanged.
- `ChunkTask` caches targets by reference instead of entity id, which plain targets do not have.
- `Chunk.Start` collects `ResourceTarget`s from its fields instead of `GetComponentsInChildren<TreeController>()`.
- The `!chop`/`!gather` commands, island statistics (`TreesCutDown`) and `SessionStats` are unchanged.

### Drawing

A `ResourceRenderer` keeps one `BatchRendererGroup`:

- one batch per mesh and material used by any variant, LOD or stump;
- one GPU buffer of instance transforms, written at load and when a field changes in the editor (trees do not move
  at runtime);
- a culling callback per camera and per shadow pass that schedules a Burst job: it tests each instance against
  the view, picks its LOD by distance and its look by state (standing variant or depleted look), and writes the
  draw commands. The render pipeline waits on the job only when it draws, so the main thread only copies the
  view's planes and one byte of state per instance. (Step 1 had a plain loop; step 5 measured it and moved it.)

It runs in the Scene view in edit mode as well as in play mode, so fields show while placing. Every tree and gather
spot material uses the one Shader Graph Lit shader, which takes instanced data; a fallback with
`Graphics.RenderMeshInstanced` is added only if a type turns up that does not.

### Removed after the conversion

`TreeController`, `GatherController`, `TaskObject`, `TaskObjectManager` (and its component on GameState), the tree and
gather spot helpers in `Editor/RavenfallTools.cs` and `Editor/RavenfallObjectTools.cs`, and the converter itself.
The old tree prefabs in `Assets/Prefabs/Objects` are local only (prefabs are not versioned) and now carry a missing
script; they are no longer used.

## Editor tools

- **GameObject > Ravenfall > Resource Field** adds a field under the selected chunk, ready to paint.
- **Inspector of a ResourceField:** its types with counts, Snap All To Ground, and four modes (Off, Paint, Erase,
  Pick). A field starts in Off when selected, so a click in the scene never paints by accident.
- **Painting in the Scene view** (Paint):
  - Click or drag to paint the brush type, picked from a palette of every type in the project, with a size, an
    amount per dab, a spacing kept to everything already in the field and a max slope. The look is random by the
    type's weights or a chosen one; turn and size come from the type's brush defaults. An amount of 1 places one
    exactly under the mouse.
  - Shift to erase while painting; Erase mode removes everything in the circle, or only the brush type.
  - [ and ] change the brush size, Escape leaves the mode.
- **Pick:** click a tree or spot, then drag the arrows to move it (it follows the ground), the ring to turn it and
  the cube to size it, or change its type, look, turn and size in the inspector; Delete removes it.
- Ground is the terrain or any ground mesh, never players, buildings, water or roads. One undo per stroke.
- **Ravenfall > World Resources > Create Tree Type From Selection** and **Create Gather Type From Selection**: each
  selected object (in the scene or a prefab) is a look, the same meshes and materials counting once; for a tree, one
  with "stump" in its name is the cut down look.
- **Convert Scene** (now removed) wrote every `TreeController` and `GatherController` into the field of its chunk,
  after saving a copy of the scene (`MainWorld_before-resources.unity`, kept out of git like the other scene copies).
  Looks were told apart by mesh and material, since Eldara uses the same three trees in many colours. A tree kept the
  looks it could pick from; a gather spot got a random look of its group. It is in the history (commit bd64ef7) if
  another scene ever needs it.

## Steps

Each step is a commit, tested in play mode and in the editor before the next.

1. **Types, fields and drawing.** `ResourceType`, `ResourceField`, `ResourceRenderer` (BRG), Create Type From
   Selection, and test fields in place of the Home and Heim trees to compare looks, LODs and shadows. Done.
2. **The brush.** Painting, erasing and picking in the Scene view. Done.
3. **Gameplay.** `ResourceTarget`, the two tasks, the player changes, the shared exp and adaptive respawn. Done.
4. **Conversion.** MainWorld converted: 188 trees and 199 gather spots into 14 fields and 13 types, every chunk's
   count the same as before. Tested in play mode: every chunk has its targets on its island at its level, trees fall
   after their health in hits and show their stump, spots cannot be gathered twice, and both come back on time. The
   old components and code are removed. Done; a play-test with players chopping and gathering is still to do.
5. **Numbers.** Measured, and the culling moved to a Burst job; see Numbers below. Done.

Afterwards, on the same system: rocks, fishing spots and farm patches; village decor; then NPCs. Those are animated and
skinned, so they need another technique (GPU-baked animation, for example), designed separately.

## Numbers

Measured in the editor on 2026-10-14 (editor timings are noisy and slower than a build; the comparisons hold).

**The scene, loaded** (MainWorld before the conversion, against after):

| | Before | After | Less |
|---|---|---|---|
| GameObjects | 57,218 | 54,315 | 2,903 |
| Mesh renderers | 24,334 | 22,844 | 1,490 |
| Colliders | 17,742 (675 triggers) | 16,883 (288 triggers) | 859 |
| LOD groups | 574 | 325 | 249 |
| Behaviours | 28,019 | 27,033 | 986 |

In edit mode every variant of an old tree was active; at runtime each tree showed one, so the renderers saved while
playing are fewer than 1,490 (about 400 trees and spots, with their LOD levels). Opening the scene in the editor
took 3.4 s before and 3.2 s after. The 387 instances cost 28 bytes each in the scene and 96 bytes each on the GPU.

**Drawing**, the Home island view from above (main view and its shadow passes), with 387 instances and with a
temporary field of 10,000 more trees on top:

| | 387 | 10,387 |
|---|---|---|
| Culling on the main thread, per render, plain loop (step 1) | 0.09 ms | 3.8 ms |
| Culling on the main thread, per render, Burst job | 0.1 to 0.2 ms | 0.15 to 0.23 ms |
| Whole camera render, plain loop | 2.8 ms | 8.4 ms |
| Whole camera render, Burst job | 4 to 5 ms | 3 to 6 ms (the same, within the noise) |
| Rebuilding the buffer (once per change) | | 10 ms |

The 10,000 trees drew correctly, stumps came and went with the Burst job, and nothing leaked.

**Gameplay:** picking a target for 50 players takes 0.26 ms over a chunk's 47 trees. It is a straight pass, so a
chunk of 10,000 would take 1 ms a pick in the editor; no chunk comes near that, but a far bigger field would want a
grid. A field with nothing cut costs one comparison per frame.

## Bugs found on the way

- `TreeController` had its own private `Awake`, so `TaskObject.Awake` never ran for trees. Gone with both.
- `PlayerController.Fish` checked the auto-train target against `Stats.Farming` instead of `Stats.Fishing`. Fixed
  separately, first.

## Answers

1. **Damage roll:** the `Math.Abs` was a bug; it is `Max(1, difference)` since step 3 (see above).
2. **Levels:** from the chunk's requirement, not per type or per instance.
3. **Gather spot looks:** grouped, with a random look per spot.
