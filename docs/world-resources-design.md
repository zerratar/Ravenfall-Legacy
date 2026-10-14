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
- **The damage roll** in `GameMath.CalculateSkillDamage` (the `Math.Abs` on the level difference lets a player far
  below a tree's level hit it hard). Kept unless you say otherwise; see Questions.
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

`Assets/World/Resources/Types/<Name>.asset`, one per kind of tree or gather spot:

| Field | Meaning |
|---|---|
| `Kind` | Tree or Gatherable. |
| `Variants` | The looks an instance can have, picked per instance (today's random child). Each variant is a list of LOD levels, each LOD a mesh, its materials and the distance it shows to. |
| `Depleted` | The look once cut or gathered: the stump for trees, nothing for gather spots (they vanish until they regrow). |
| `Level` | The level fed to the damage roll (trees). |
| `Health` | Hits to cut it down (trees); gather spots deplete in one gather, as today. |
| `RespawnSeconds` | The base respawn time. |
| `ActionDistance` | How close a player must be to chop or gather: today's trigger SphereCollider radius (3 m). |
| `KneelToGather` | The gathering animation choice (gather spots). |
| `CastShadows` | Per type. |
| Brush defaults | Rotation, scale range and variant weights for painting. |

The existing prefabs give the first types: `Tree Lv 1`, `Tree Lv 90`, `Tree Lv 150`, `Tree Lv 600`, `Tree Lv 1000`,
the 58 hand-placed palm/elf trees (levels 500 and 700), and one type per gather look in use (Mushroom group,
Mushroom, Garden plants, Coconut, Flowers, and so on). A tool makes a type from a prefab: variants from its children,
the stump, LOD meshes and distances from the LODGroups, and the numbers from the `TreeController`/`GatherController`.

The scene overrides some levels per instance (for example 15 `Tree Lv 1` set to 100, the `Tree Lv 600` set to 200).
These become types of their own, or a per-instance level override if you prefer fewer types (see Questions).

### Resource fields (component in the scene)

`ResourceField` (`[ExecuteAlways]`), one per chunk that has trees or gather spots, on the chunk's GameObject or a child
of it, so `Chunk` keeps knowing what it owns. It saves only plain data:

```
types:     List<ResourceType>            the types used in this field
instances: List<ResourceInstance>        one entry per tree or spot
  ResourceInstance { ushort type; byte variant; Vector3 position; float yaw; float scale; }
```

The position is world space, with the height taken from the ground when placed. Nothing generated is saved into the
scene, the same rule as `WorldRoad`.

## Runtime

### WorldResources (one system per scene)

Holds the live state of every instance in every field, in arrays:

- `health`, `isStanding` (or available), `respawnAt`, the current respawn time;
- the targeter set per instance, which grows and empties as `woodCutters` and `gatherers` do today;
- per chunk, the indices of its instances, and a coarse grid for "nearest available";
- one respawn queue, ordered by time, checked once a frame. This is the only per-frame work, and it costs
  nothing while nothing is due.

### Targets

The player task API passes targets as `object`, so each instance gets one small `ResourceTarget` object made at load
and reused for its life. It is a handle (field and index) with `Position`, `Island`, `ActionDistance`, `Level`,
`IsAvailable`, the targeter list, and the variant's look for the camera. No allocation per frame, and identity
comparisons keep working.

Changes that follow from it:

- `WoodcuttingTask` and `GatheringTask` take `ResourceTarget[]` from the chunk instead of `TreeController[]` /
  `GatherController[]`.
- `PlayerController.Cut`, `Gather`, `PlayerCombat.DamageTree` and `CalculateDamage` take a `ResourceTarget`.
- `LookAt` gets a position overload.
- `PlayerController.Target` returns a Transform, and resources have none. The two users, `BotPlayerController`
  (stuck detection) and `AnimationEventController` (projectiles), move to a target position, which
  `GetTaskTargetPosition` already gives for the task.
- `Chunk.Start` collects `ResourceTarget`s from its fields instead of `GetComponentsInChildren<TreeController>()`.
- The `!chop`/`!gather` commands, island statistics (`TreesCutDown`) and `SessionStats` are unchanged.

### Drawing

A `ResourceRenderer` keeps one `BatchRendererGroup`:

- one batch per mesh and material used by any variant, LOD or stump;
- one GPU buffer of instance transforms, written at load and when a field changes in the editor (trees do not move
  at runtime);
- a culling callback per camera and per shadow pass that tests each instance against the view, picks its LOD by
  distance and its look by state (standing variant or depleted look), and emits draw commands. A Burst job, since
  `com.unity.burst` is already in the project.

It runs in the Scene view in edit mode as well as in play mode, so fields show while placing. Fallback: a material
whose shader cannot take instanced data is drawn with `Graphics.RenderMeshInstanced` and logged once, so nothing
silently disappears.

### Removed when the conversion is done

`TreeController`, `GatherController`, `TaskObject`, `TaskObjectManager` (and its manager object in the scene), plus the
tree and gather spot helpers in `Editor/RavenfallTools.cs` and `Editor/RavenfallObjectTools.cs` that the new tools replace.

## Editor tools

- **Inspector of a ResourceField:** its types with counts; a type palette; Paint and Erase modes.
- **Painting in the Scene view:**
  - Click or drag to paint, with radius, spacing and random yaw, scale and variant from the type's brush defaults.
  - Shift to erase.
  - Ctrl+click to pick one instance, then move it, turn it, or change its variant or type.
  - Everything snaps to the ground and is undoable.
- **Ravenfall > World Resources > Create Type From Prefab**, for the existing tree and gather prefabs.
- **Ravenfall > World Resources > Convert Scene**: writes every `TreeController` and `GatherController` in the open
  scene into the field of its chunk, at the same place, look, level and respawn, and lists anything it could not
  convert. Then it removes the old GameObjects. The scene is saved to a copy first
  (`MainWorld_before-resources.unity`, kept out of git like the other scene copies).

## Steps

Each step is a commit, tested in play mode and in the editor before the next.

1. **Types, fields and drawing.** `ResourceType`, `ResourceField`, `ResourceRenderer` (BRG), Create Type From Prefab,
   and a small test field next to the existing trees to compare looks, LODs and shadows in the Scene and Game views.
2. **The brush.** Painting, erasing and picking in the Scene view.
3. **Gameplay.** `WorldResources`, `ResourceTarget`, the two tasks, the player changes, the shared exp and adaptive
   respawn. Tested on the test field alongside the old trees, which keep working until step 4.
4. **Conversion.** Convert MainWorld, check every chunk's counts against the old ones, play-test chopping and gathering on
   each island, then remove the old components and code.
5. **Numbers.** Profiler before and after: GameObject count, scene load time, memory, and the frame time with 50 players
   chopping. Plus a stress field of 10,000 trees to show the headroom.

Afterwards, on the same system: rocks, fishing spots and farm patches; village decor; then NPCs. Those are animated and
skinned, so they need another technique (GPU-baked animation, for example), designed separately.

## Bugs found on the way

Fixed in step 3, since that code is rewritten anyway:

- `TreeController` has its own private `Awake`, so `TaskObject.Awake` never runs for trees. It becomes moot when both
  go.
- `PlayerController.Fish` (L2017) checks the auto-train target against `Stats.Farming` instead of `Stats.Fishing`. A
  one-line fix, done separately and first.

## Questions

1. **Damage roll:** is the `Math.Abs` in `GameMath.CalculateSkillDamage` intended? It gives a player far below a tree's
   level a big hit. The plan keeps it as is.
2. **Per-instance levels:** a type for each level used (simple, more types), or a level override per instance (fewer
   types)? The plan uses types.
3. **Gather spot looks:** keep one type per look, as the scene has them, or group them (for example "Mushrooms" with
   the four mushroom looks as variants, picked at random), which makes painting a mixed patch one brush stroke?
