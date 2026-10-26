// writes the designed Ironhill window (ih-heights.bin, from ih_sculpt.py) into the world terrain
var S = "C:/Users/kaaru/AppData/Local/Temp/claude/G--Ravenfall-Projects-Ravenfall-Legacy/86b0e615-81c6-408c-acb9-282e465846ce/scratchpad/";
var w = System.IO.File.ReadAllText(S + "ih-window.txt").Split(' ');
int ix0 = int.Parse(w[0]), iz0 = int.Parse(w[1]), nx = int.Parse(w[2]), nz = int.Parse(w[3]);
var bytes = System.IO.File.ReadAllBytes(S + "ih-heights.bin");
var h = new float[nz, nx]; System.Buffer.BlockCopy(bytes, 0, h, 0, bytes.Length);
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
UnityEditor.Undo.RegisterCompleteObjectUndo(t.terrainData, "Ironhill mountain");
t.terrainData.SetHeights(ix0, iz0, h);
t.Flush();
return "applied " + nx + "x" + nz + " at " + ix0 + "," + iz0;
