// Ironhill's east: scorched ground, burnt trees, a dragon skeleton, an egg nest with lava cracks; rerunnable (clears its group)
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ironhill Island").transform;
var group = island.Find("Dragon Country");
if (group) UnityEngine.Object.DestroyImmediate(group.gameObject);
group = new UnityEngine.GameObject("Dragon Country").transform; group.SetParent(island, false);
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
float G(float x, float z) => t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + t.transform.position.y;
float Smooth(float e0, float e1, float v) { var k = UnityEngine.Mathf.Clamp01((v - e0) / (e1 - e0)); return k * k * (3 - 2 * k); }
float Scorch(float x, float z)
{
    var m = Smooth(32, 52, x) * Smooth(352, 372, z) * (1 - Smooth(452, 470, z));
    return m * UnityEngine.Mathf.Clamp01(0.45f + Smooth(0.38f, 0.62f, UnityEngine.Mathf.PerlinNoise(x / 15f + 31, z / 15f + 7)) * 0.55f);
}
// the ground
var d = t.terrainData; var res = d.alphamapResolution; var o = t.transform.position;
int ax0 = (int)((25 - o.x) / d.size.x * res), ax1 = (int)((105 - o.x) / d.size.x * res);
int az0 = (int)((345 - o.z) / d.size.z * res), az1 = (int)((475 - o.z) / d.size.z * res);
var maps = d.GetAlphamaps(ax0, az0, ax1 - ax0, az1 - az0);
for (var j = 0; j < az1 - az0; j++)
for (var i = 0; i < ax1 - ax0; i++)
{
    var wx = o.x + (ax0 + i + 0.5f) / res * d.size.x; var wz = o.z + (az0 + j + 0.5f) / res * d.size.z;
    if (G(wx, wz) < -2.4f) continue; // the beach keeps its sand
    var s = Scorch(wx, wz);
    if (s < 0.01f) continue;
    var rockAlready = maps[j, i, 6];
    s *= 1 - rockAlready;
    var mud = s * 0.75f; var dirt = s * 0.25f;
    var keep = 1 - mud - dirt;
    for (var l = 0; l < maps.GetLength(2); l++) maps[j, i, l] *= keep;
    maps[j, i, 4] += mud; maps[j, i, 0] += dirt;
}
UnityEditor.Undo.RegisterCompleteObjectUndo(d, "Ironhill scorch");
d.SetAlphamaps(ax0, az0, maps);

var rng = new System.Random(21);
float R() => (float)rng.NextDouble();
UnityEngine.GameObject Put(string path, float x, float z, float yaw, float s, float lift = 0f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) throw new System.Exception("missing " + path);
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, G(x, z) + lift, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    go.transform.localScale = UnityEngine.Vector3.one * s;
    go.isStatic = true;
    return go;
}
var stone = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Terrain/Ironhill Iron Stone.mat");
var placed = 0;
// burnt trees where the scorch is strong, never on the dragons' field (x < 42) or the farm
var burnt = new[] { "Assets/PolygonFantasyKingdom/Prefabs/Bonus/SM_Env_Tree_Burnt_01.prefab", "Assets/PolygonFantasyKingdom/Prefabs/Bonus/SM_Env_Tree_Burnt_02.prefab", "Assets/PolygonDungeonRealms/Prefabs/Environments/SM_Env_Tree_Dead_Windswept_01.prefab" };
for (var k = 0; k < 60 && placed < 14; k++)
{
    var x = 44 + R() * 48; var z = 360 + R() * 92;
    if (Scorch(x, z) < 0.6f || G(x, z) < -2.2f || G(x, z) > 6f) continue;
    if ((x - 68) * (x - 68) + (z - 434) * (z - 434) < 100 || (x > 70 && z > 385 && z < 430)) continue;
    var pick = burnt[rng.Next(burnt.Length)];
    var size = pick.Contains("Windswept") ? UnityEngine.Mathf.Lerp(0.8f, 1.1f, R()) : UnityEngine.Mathf.Lerp(1.6f, 2.4f, R());
    Put(pick, x, z, R() * 360, size, -0.2f); placed++;
}
// the dragon skeleton along the east shore: ribs, spine, skull, scaled up
string B(string n) => "Assets/PolygonDungeon/Prefabs/Environments/Bones/" + n + ".prefab";
Put(B("SM_Env_Bone_Rigcage_01"), 80, 410, 20, 1.4f, -0.4f);
Put(B("SM_Env_Bone_Spine_01"), 78, 402, 15, 1.4f, -0.2f);
Put(B("SM_Env_Bone_Spine_02"), 76, 396, 35, 1.4f, -0.2f);
Put(B("SM_Env_Bone_Skull_01"), 83, 418, 200, 1.6f, -0.3f);
Put(B("SM_Env_Bone_Horn_01"), 86, 421, 140, 1.4f, -0.2f);
Put(B("SM_Env_BonePile_02"), 70, 418, 0, 1.6f, -0.1f);
Put(B("SM_Env_BonePile_Small_01"), 58, 398, 60, 1.5f, 0);
Put(B("SM_Env_BonePile_Small_02"), 66, 436, 120, 1.5f, 0);
placed += 8;
// the egg nest at the mountain's east foot: a loose, uneven ring of rocks and bones round three eggs
float nx = 68, nz = 434;
var ring = new[] { ("Assets/PolygonDungeon/Prefabs/Environments/Rocks/SM_Env_RockPile_Rounded_01.prefab", 0f, 3.6f, 1.6f), ("Assets/PolygonDungeon/Prefabs/Environments/Rocks/SM_Env_Rock_Round_01.prefab", 55f, 3.2f, 2.4f),
    (B("SM_Env_BonePile_Small_01"), 100f, 3.4f, 1.3f), ("Assets/PolygonDungeon/Prefabs/Environments/Rocks/SM_Env_RockPile_Square_05.prefab", 150f, 3.8f, 1.1f),
    ("Assets/PolygonDungeon/Prefabs/Environments/Rocks/SM_Env_Rock_Round_01.prefab", 215f, 3.0f, 3.0f),
    ("Assets/PolygonDungeon/Prefabs/Environments/Rocks/SM_Env_RockPile_Rounded_01.prefab", 300f, 3.5f, 1.2f) };
foreach (var (path, ang, rad, sc) in ring)
{
    var a2 = (ang + (R() - 0.5f) * 20f) * UnityEngine.Mathf.Deg2Rad;
    Put(path, nx + UnityEngine.Mathf.Cos(a2) * rad, nz + UnityEngine.Mathf.Sin(a2) * rad, R() * 360, sc, -0.15f); placed++;
}
Put("Assets/Malbers Animations/Dragons/Eggs/Prefabs/Toon Dragon Egg.prefab", nx - 1, nz, 10, 2.2f, 0);
Put("Assets/Malbers Animations/Dragons/Eggs/Prefabs/Toon Dragon Egg.prefab", nx + 1.3f, nz + 0.8f, 70, 1.9f, 0);
Put("Assets/Malbers Animations/Dragons/Eggs/Prefabs/Toon Dragon Egg.prefab", nx + 0.4f, nz - 1.4f, 200, 2.0f, 0);
Put(B("SM_Env_Bone_Skull_02"), nx + 6.5f, nz - 2.5f, 230, 0.45f, -0.1f);
placed += 4;
// the burned house that stood where the hold's yard is now: east, by the other ruins
var house = island.Find("Terrain/SM_Prop_Destroyed_House_Preset_02");
if (house)
{
    house.gameObject.SetActive(true);
    house.position = new UnityEngine.Vector3(76, G(76, 392) - 0.2f, 392);
    house.rotation = UnityEngine.Quaternion.Euler(0, 210, 0);
}
// the green grove that stood on this shore would have burned with the rest
var grove = island.Find("Terrain/SM_Env_Tree_Cluster_01 (4)"); // deleted 2026-10-26; kept for reruns on an old scene
if (grove) grove.gameObject.SetActive(false);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return "dragon country: " + placed + " pieces";
