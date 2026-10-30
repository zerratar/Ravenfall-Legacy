// Run with: unity command eval_file --file tools/ravebridge/lumbridge-move-paint.cs, after tools/ravebridge/lumbridge-move.cs
//
// Paints Ravebridge's island terrain (Assets/TerrainData_Lumbridge.asset) from the paint lumbridge-move.cs left in
// Temp/lumbridge-move-paint.bin, on its own: set at the end of the move's long run, the paint never reached the saved
// terrain. Then links the terrain material's masks to the terrain's splat maps and saves MainWorld.
var td = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainData>("Assets/TerrainData_Lumbridge.asset");
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Terrain/Ravebridge Terrain Lumbridge.mat");
if (!td || !mat) return "run lumbridge-move.cs first";
if (!System.IO.File.Exists("Temp/lumbridge-move-paint.bin")) return "no Temp/lumbridge-move-paint.bin: run lumbridge-move.cs first";
var res = td.alphamapResolution; var layers = td.alphamapLayers;
var bytes = System.IO.File.ReadAllBytes("Temp/lumbridge-move-paint.bin");
if (bytes.Length != res * res * layers * 4) return "paint is " + bytes.Length + " bytes, the terrain wants " + res + " x " + res + " x " + layers;
var alpha = new float[res, res, layers];
System.Buffer.BlockCopy(bytes, 0, alpha, 0, bytes.Length);
td.SetAlphamaps(0, 0, alpha);
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
mat.SetTexture("_RoadMask", td.alphamapTextures[0]);
if (td.alphamapTextureCount > 1) mat.SetTexture("_SnowMask", td.alphamapTextures[1]);
UnityEditor.EditorUtility.SetDirty(mat);
UnityEditor.AssetDatabase.SaveAssetIfDirty(mat);
var check = td.GetAlphamaps(0, 0, res, res);
var used = new int[layers];
for (var j = 0; j < res; j += 8) for (var i = 0; i < res; i += 8) { var best = 0; for (var l = 1; l < layers; l++) if (check[j, i, l] > check[j, i, best]) best = l; used[best]++; }
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path == "Assets/Scenes/MainWorld.unity") { UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); }
return "painted: " + string.Join("/", used);
