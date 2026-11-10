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
| tower-mossgate-build.cs + moss-rest.cs | Tower_Mossgate (town) | 1 |
| tower-glacier-build.cs | Tower_Glacier (11, 12, 15, 16, 19) | 2 |
| make-vault.py -> tower-frostvault-build.cs | Tower_FrostVault (13, 14, 17, 18, 20), generated from the crypt's builder | 2 |
| tower-rimeholt-build.cs | Tower_Rimeholt (town) | 2 |

`tower-capture2.cs` renders a scene from cameras listed in `%TEMP%/tower-shots.txt` (scene path in
`%TEMP%/tower-capture.txt`); `tower2-test.cs` drives bots through the tower in play mode (steps in its header).

Assets in `Assets/Scenes/Tower/*/` (NavMesh.asset, Terrain.asset, materials, metas) are gitignored by the project's
rules: add them with `git add -f`.
