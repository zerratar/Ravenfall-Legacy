// Run with: unity command eval_file --file tools/ravebridge/lumbridge-ravenfall-copy.cs (after lumbridge-test.cs and
// lumbridge-paint.cs)
//
// Makes the Ravenfall Lumbridge (Assets/Scenes/LumbridgeRavenfall.unity) afresh as a copy of the OSRS-faithful
// reference (Assets/Scenes/LumbridgeTest.unity), with a terrain of its own so its ground can change without touching
// the reference. Everything in it comes from the tools that follow, in this order: lumbridge-swap.cs,
// lumbridge-castle.cs, lumbridge-courtyard.cs, lumbridge-alkharid.cs, lumbridge-ground.cs. An old copy is replaced.
const string From = "Assets/Scenes/LumbridgeTest.unity", To = "Assets/Scenes/LumbridgeRavenfall.unity", Folder = "Assets/Scenes/LumbridgeRavenfall";
if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (active.isDirty) return "unsaved changes in " + active.path;
// off the copy before it goes
if (active.path == To) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(From);
UnityEditor.AssetDatabase.DeleteAsset(To);
UnityEditor.AssetDatabase.DeleteAsset(Folder);
if (!UnityEditor.AssetDatabase.CopyAsset(From, To)) return "copy failed";
UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes", "LumbridgeRavenfall");
UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/LumbridgeTest/Lumbridge TerrainData.asset", Folder + "/Lumbridge Ravenfall TerrainData.asset");
UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/LumbridgeTest/Lumbridge Terrain.mat", Folder + "/Lumbridge Ravenfall Terrain.mat");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(To);
var td = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainData>(Folder + "/Lumbridge Ravenfall TerrainData.asset");
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(Folder + "/Lumbridge Ravenfall Terrain.mat");
var t = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
t.terrainData = td;
var col = t.GetComponent<UnityEngine.TerrainCollider>(); if (col) col.terrainData = td;
mat.SetTexture("_RoadMask", td.alphamapTextures[0]);
if (td.alphamapTextureCount > 1) mat.SetTexture("_SnowMask", td.alphamapTextures[1]);
t.materialTemplate = mat;
UnityEditor.EditorUtility.SetDirty(mat);
UnityEngine.GameObject.Find("Lumbridge").name = "Lumbridge (Ravenfall)";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
return "made " + scene.path;
