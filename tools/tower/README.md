# Tower scene builders

Editor scripts that build the tower's floor layouts and towns (docs/tower-design.md). Run one with the Unity CLI while
MainWorld is the active scene and the editor is not in play mode (the floors copy MainWorld's enemies as templates and
the towns copy an island NPC):

```
unity command eval_file "<full path>\tools\tower\tower-glacier-build.cs"
```

Each writes its log to `%TEMP%/<name>.txt` (the eval call itself times out after 5 s while the build keeps going).

| Script | Scene | Band |
|---|---|---|
| tower-overgrown-build.cs | Tower_Overgrown (floors 1, 2, 5, 6) | 1 |
| tower-marsh-build.cs | Tower_Marsh (3, 8) | 1 |
| tower-crypt-build.cs | Tower_Crypt (4, 7, 9, 10) | 1 |
| tower-mossgate-build.cs | Tower_Mossgate (town, its resting place and work places) | 1 |
| tower-glacier-build.cs | Tower_Glacier (11, 12, 15, 16, 19) | 2 |
| make-vault.py -> tower-frostvault-build.cs | Tower_FrostVault (13, 14, 17, 18, 20), generated from the crypt's builder | 2 |
| tower-rimeholt-build.cs | Tower_Rimeholt (town, its resting place and work places) | 2 |
| tower-greathall-build.cs | Tower_GreatHall: the first floor of rooms (docs/tower-design.md section 22), not in the climb yet; its wall stubs and rune circle from blender/tower_great_hall.py | 4 (v0.9.8) |

A floor of rooms (`TowerRoute`) has its gates' NavMeshObstacles saved disabled, so the audit walks the whole route;
the game shuts them when it fills the floor. The audit also checks every room's arrival.

`tower-audit.cs` checks a scene (path in `%TEMP%/tower-audit-scene.txt`): every monster spot, the boss, the people, rest
spots, strolls, work stands and road points can be walked to from the arrival, and where each road ends; it writes
`%TEMP%/tower-audit.txt`. Run it after every rebuild. The town builders also log props standing on a lane and work stands
nobody can reach.

`tower-cutscene-preview.cs` renders frames along a town's cutscene path (`TowerCutscene`) in edit mode, from poses in
`%TEMP%/tower-cutscene.txt` or the scene's own (header says how); the town builders place the poses (`Cutscene/Arrival`,
Mossgate's `Opening`).

`tower-boss-taunts.cs` makes `Assets/Resources/Tower/Boss Taunts.asset` (`TowerBossTaunts`): the clip each boss model
plays in its intro. Resources is not in git, so run it on a new checkout, and again after adding a boss model.

`tower-capture2.cs` renders a scene from cameras listed in `%TEMP%/tower-shots.txt` (scene path in
`%TEMP%/tower-capture.txt`); `tower2-test.cs` and `tower3-test.cs` drive bots through the tower in play mode (steps in their headers; tower3 covers
the town work places, the votes and the way after a clear).

Assets in `Assets/Scenes/Tower/*/` (NavMesh.asset, Terrain.asset, materials, metas) are gitignored by the project's
rules: add them with `git add -f`.
