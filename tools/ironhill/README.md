# Ironhill rework scripts

The scripts that rebuilt Ironhill on 2026-10-26 (docs/ironhill-rework.md). They ran through the Unity CLI
(`unity command eval_file --file <script>`) against the open editor, in this order. Each one clears what it made
before, so they can be rerun after a tweak.

| Script | What it does |
| --- | --- |
| `ih_sculpt.py` | Designs the new heights (the mountain, the hold's yard, the shoulder round the cave) from a backup of the terrain's heights and draws a shaded relief to judge them. Writes `ih-heights.bin` and `ih-window.txt`. |
| `ih-apply.cs` | Writes that window into the world terrain (`Assets/New Terrain.asset`). |
| `ih-paint.cs` | Rock on steep faces and high up, dark gravel on the shoulders. |
| `ih-yard-paint.cs` | The hold's yard floor. |
| `ih-crags.cs` | Grey crags over the mountain (group `Mountain`), kept off the cave, the yard and the gate's face. |
| `ih-hold.cs` | The dwarf hold (group `Dwarf Hold`): gate, obelisks, braziers, smelters, lava wheel, crane, paving, dwarves. |
| `ih-dragonland.cs` | The east (group `Dragon Country`): scorched ground, burnt trees, the skeleton, the egg nest. |
| `ih-dragon.cs` | The sleeping dragon on the summit; `DRAGON_X/Z/YAW/SCALE/CLEAR` are filled in before running (10, 519, 200, 2.6, 14). |
| `ih-spots.cs` | Moves the woodcutting tree out of the yard and seats every resource spot on the island on the ground. |

Paths in them point at the session scratchpad where they ran (the heights backup and the outputs); change `S` before
running them again. Judge every step from the island camera and in play mode: edit-mode `Camera.Render` shots can
keep drawing deleted objects (see the Unity CLI notes).
