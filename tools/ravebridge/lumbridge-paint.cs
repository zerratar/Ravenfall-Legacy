// Run with: unity command eval_file --file tools/ravebridge/lumbridge-paint.cs, after tools/ravebridge/lumbridge-test.cs
//
// Paints the Lumbridge test's ground from the paint the builder left in Temp/lumbridge-paint.bin (512 x 512 x 8
// weights), on its own: painted at the end of the builder's long run, the paint never reached the saved terrain.
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
if (!terrain || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/LumbridgeTest.unity") return "open the Lumbridge test scene first";
if (!System.IO.File.Exists("Temp/lumbridge-paint.bin")) return "no Temp/lumbridge-paint.bin: run lumbridge-test.cs first";
var td = terrain.terrainData;
var res = td.alphamapResolution;
var bytes = System.IO.File.ReadAllBytes("Temp/lumbridge-paint.bin");
if (bytes.Length != res * res * 8 * 4) return "paint is " + bytes.Length + " bytes, the terrain wants " + res + " x " + res + " x 8";
var alpha = new float[res, res, 8];
System.Buffer.BlockCopy(bytes, 0, alpha, 0, bytes.Length);
td.SetAlphamaps(0, 0, alpha);
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
// the terrain material reads the paint from the terrain's own splat maps: linked here, once they are saved (linked by
// the builder, the link was lost when the scene was reopened)
var mat = terrain.materialTemplate;
if (mat && td.alphamapTextureCount > 0)
{
    mat.SetTexture("_RoadMask", td.alphamapTextures[0]);
    if (td.alphamapTextureCount > 1) mat.SetTexture("_SnowMask", td.alphamapTextures[1]);
    UnityEditor.EditorUtility.SetDirty(mat);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(mat);
}
var check = td.GetAlphamaps(0, 0, res, res);
var used = new int[8];
for (var j = 0; j < res; j += 8) for (var i = 0; i < res; i += 8) { var best = 0; for (var l = 1; l < 8; l++) if (check[j, i, l] > check[j, i, best]) best = l; used[best]++; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "painted: " + string.Join("/", used) + " (layers " + td.alphamapLayers + ", textures " + td.alphamapTextureCount + ", terrains " + UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>().Length + ", td " + UnityEditor.AssetDatabase.GetAssetPath(td) + " " + td.GetEntityId() + ", playing " + UnityEditor.EditorApplication.isPlaying + ", updating " + UnityEditor.EditorApplication.isUpdating + ")";
