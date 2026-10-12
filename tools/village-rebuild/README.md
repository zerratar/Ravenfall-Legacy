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
| `hall_shot.cs` | Renders one tier on its own to `Temp/village-shots/`. Replace `TIER` (1-5) and `DIST` (32f to 70f) first. |
| `shot_island.cs` | Renders overview shots of the whole town to `Temp/village-shots/`. |
| `gallery.cs` | Renders a contact sheet of the prefabs listed in `gallery.txt`. Used to pick kit pieces. |

The hall scripts are run as one file: `hall_common.cs`, then any tier files, then
`hall_finish.cs`, then a final `UnityEngine.Debug.Log(sb.ToString());`. For example, to rebuild
tier 3 only:

```
cat hall_common.cs hall_tier3.cs hall_finish.cs > run.cs
echo 'UnityEngine.Debug.Log(sb.ToString());' >> run.cs
```

## Things learned the hard way

- **Navmesh bounds.** `NavMeshBuilder.UpdateNavMeshData` keeps only the tiles inside the bounds you
  give it. Updating with town-sized bounds wiped the navmesh for every other island. Always pass the
  surface's full world bounds; `navmesh_full.cs` does. The town halls are not under `WORLD`, so they
  are not part of the navmesh, the same as before.
- **Editor time limit.** The eval server gives up after about 5 s on the main thread, while the
  script keeps running. A script that renders in a loop (one tried hundreds of renders) can hang
  and crash the editor. Keep renders to a handful per eval.
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
