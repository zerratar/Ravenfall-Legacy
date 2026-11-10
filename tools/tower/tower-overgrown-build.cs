// Builds Assets/Scenes/Tower/Tower_Overgrown.unity: the first band's floor layout ("The Overgrown Halls"), a sunken
// courtyard of an old hall gone wild, at (0, 0, -6000). A terrain (grass and leaves, moss, mud, ruin tiles in the boss's
// plaza, a path from the arrival point) in a bowl of wooded hills so the floor never ends at an edge; ruined pillars and
// walls around the plaza; trees, rocks, bushes and grass; 24 monster spots; the boss's spot; one hidden template per
// monster look (copied from MainWorld's enemies); its own navmesh. Editor only, not in play mode.
// Writes the result to %TEMP%/tower-overgrown-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_Overgrown";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes", "Tower");
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Overgrown");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Overgrown Halls");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 115f;
floor.SkyAmbient = new UnityEngine.Color(0.52f, 0.60f, 0.50f);
floor.EquatorAmbient = new UnityEngine.Color(0.40f, 0.45f, 0.34f);
floor.GroundAmbient = new UnityEngine.Color(0.17f, 0.16f, 0.11f);
floor.FogColor = new UnityEngine.Color(0.56f, 0.66f, 0.55f);
floor.FogDensity = 0.0085f;
floor.SunColor = new UnityEngine.Color(1f, 0.93f, 0.80f);
floor.SunIntensity = 1.15f;
floor.SunAngles = new UnityEngine.Vector3(48f, -35f, 0f);

// ---- the layout (local to the root) ----
var spawnLocal = new UnityEngine.Vector3(0f, 0f, -52f);
var plaza = new UnityEngine.Vector3(0f, 0f, 36f);
const float plazaRadius = 15f;
const float flatRadius = 64f;   // the play area
const float size = 240f, height = 30f, baseHeight = 2f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 31.7f, z * s + 77.3f);
float DistToPath(UnityEngine.Vector3 p)
{
    var a = spawnLocal; var b = plaza;
    var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0;
    var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    var c = a + ab * t; c.y = 0; var q = p; q.y = 0;
    // a gentle bend
    return (q - c).magnitude + UnityEngine.Mathf.Sin(t * 3.1f) * 0f;
}
// the ground height (local, metres above the floor's level) at a point of the floor
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var h = (Noise(x, z, 0.035f) - 0.5f) * 0.9f + (Noise(x, z, 0.11f) - 0.5f) * 0.25f;
    // the plaza and the path are flatter
    var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    var flat = UnityEngine.Mathf.Max(UnityEngine.Mathf.InverseLerp(plazaRadius + 6f, plazaRadius, dp), UnityEngine.Mathf.InverseLerp(6f, 2f, DistToPath(new UnityEngine.Vector3(x, 0, z))));
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    // the bowl: hills rise past the play area
    var rim = UnityEngine.Mathf.InverseLerp(flatRadius, 104f, r);
    rim = rim * rim * (3f - 2f * rim);
    h += rim * (14f + Noise(x, z, 0.05f) * 10f);
    return h;
}

// ---- the terrain ----
var res = 257;
var td = new UnityEngine.TerrainData();
td.heightmapResolution = res;
td.size = new UnityEngine.Vector3(size, height, size);
var tdPath = dataFolder + "/Terrain.asset";
UnityEditor.AssetDatabase.DeleteAsset(tdPath);
UnityEditor.AssetDatabase.CreateAsset(td, tdPath);
var heights = new float[res, res];
for (var zi = 0; zi < res; zi++)
    for (var xi = 0; xi < res; xi++)
    {
        var x = xi / (float)(res - 1) * size - size / 2f;
        var z = zi / (float)(res - 1) * size - size / 2f;
        heights[zi, xi] = UnityEngine.Mathf.Clamp01((GroundAt(x, z) + baseHeight) / height);
    }
td.SetHeights(0, 0, heights);

string L(string n) => "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Terrain_Meadow_" + n + ".terrainlayer";
var layerNames = new[] { "Grass_Leaves_01", "Moss_01", "Cobblestone_01", "Mud_Debris_01", "Rock_Moss_01", "Footpath_Tile_01", "Grass_02" };
var layers = layerNames.Select(n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(L(n))).ToArray();
foreach (var i in Enumerable.Range(0, layers.Length)) if (!layers[i]) log.AppendLine("missing layer " + layerNames[i]);
td.terrainLayers = layers.Where(l => l).ToArray();
var amRes = 256;
td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, td.terrainLayers.Length];
int Idx(string n) => System.Array.IndexOf(td.terrainLayers, layers[System.Array.IndexOf(layerNames, n)]);
var iGrass = Idx("Grass_Leaves_01"); var iMoss = Idx("Moss_01"); var iRuin = Idx("Cobblestone_01"); var iMud = Idx("Mud_Debris_01");
var iRock = Idx("Rock_Moss_01"); var iPath = Idx("Footpath_Tile_01"); var iGrass2 = Idx("Grass_02");
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f;
        var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[td.terrainLayers.Length];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        w[iGrass] = 1f;
        if (iGrass2 >= 0) w[iGrass2] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.06f) - 0.45f) * 3f);
        if (iMoss >= 0) w[iMoss] = UnityEngine.Mathf.Clamp01((Noise(x + 100, z, 0.045f) - 0.55f) * 4f);
        if (iMud >= 0) w[iMud] = UnityEngine.Mathf.Clamp01((Noise(x, z + 50, 0.08f) - 0.72f) * 5f);
        if (iRock >= 0) w[iRock] = UnityEngine.Mathf.Clamp01((steep - 22f) / 12f) * 2f;
        var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
        var ruin = UnityEngine.Mathf.Clamp01((plazaRadius + 1f - dp) / 3f + (Noise(x, z, 0.3f) - 0.5f) * 0.8f);
        if (iRuin >= 0) w[iRuin] = ruin * 3f;
        var path = UnityEngine.Mathf.Clamp01((2.6f - DistToPath(new UnityEngine.Vector3(x, 0, z))) / 1.6f + (Noise(x, z, 0.4f) - 0.5f) * 0.6f);
        if (iPath >= 0) w[iPath] = path * (1f - ruin) * 0.6f;
        var sum = w.Sum();
        for (var k = 0; k < w.Length; k++) maps[zi, xi, k] = w[k] / sum;
    }
td.SetAlphamaps(0, 0, maps);
{ var pz = (int)((plaza.z + size / 2f) / size * amRes); var px = (int)((plaza.x + size / 2f) / size * amRes); var am = td.GetAlphamaps(px, pz, 1, 1); log.AppendLine("plaza paint: " + string.Join(", ", Enumerable.Range(0, td.terrainLayers.Length).Select(k => td.terrainLayers[k].name + " " + am[0, 0, k].ToString("0.00")))); }
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssets();
var terrainGo = UnityEngine.Terrain.CreateTerrainGameObject(td);
terrainGo.name = "Terrain";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(terrainGo, scene);
terrainGo.transform.SetParent(root.transform, false);
terrainGo.transform.localPosition = new UnityEngine.Vector3(-size / 2f, -baseHeight, -size / 2f);
var terrain = terrainGo.GetComponent<UnityEngine.Terrain>();
terrain.drawInstanced = true;
terrain.heightmapPixelError = 4f;
terrain.basemapDistance = 400f;
log.AppendLine("terrain layers " + td.terrainLayers.Length);

// the ground's height in world space at a local point
UnityEngine.Vector3 OnGround(float x, float z) => origin + new UnityEngine.Vector3(x, GroundAt(x, z), z);

// ---- props ----
var rng = new System.Random(11);
float R() => (float)rng.NextDouble();
var placed = 0;
UnityEngine.GameObject Place(string path, UnityEngine.Vector3 world, float scale, float yaw = -1f, UnityEngine.Transform parent = null)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent ? parent : root.transform, true);
    go.transform.position = world;
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw < 0 ? R() * 360f : yaw, 0f);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    placed++;
    return go;
}
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var ruins = new UnityEngine.GameObject("Ruins").transform; ruins.SetParent(root.transform, false);
var foliage = new UnityEngine.GameObject("Foliage").transform; foliage.SetParent(root.transform, false);
var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";
var fv = "Assets/Synty/PolygonFantasyVillage/Prefabs/Environment/";
var df = "Assets/Synty/PolygonDarkFortress/Prefabs/Buildings/";

bool Clear(float x, float z, float margin)
{
    var p = new UnityEngine.Vector3(x, 0, z);
    if (new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude < plazaRadius + margin) return false;
    if (DistToPath(p) < 3.5f + margin * 0.5f) return false;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - spawnLocal.z).magnitude < 9f + margin) return false;
    return true;
}

// trees: a thick ring on the hills, a few in the clearing
var trees = new[] { "SM_Env_Tree_Meadow_01", "SM_Env_Tree_Meadow_02", "SM_Env_Tree_Birch_01", "SM_Env_Tree_Birch_02", "SM_Env_Tree_Birch_03", "SM_Env_Tree_Meadow_01" };
for (var i = 0; i < 150; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 64f + R() * 44f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    Place(pnb + trees[rng.Next(trees.Length)] + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.3f, 1.0f + R() * 0.6f, -1, foliage);
}
for (var i = 0; i < 0; i++) // the follow camera sits behind and above players: no trees inside the clearing
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 24f + R() * 36f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 6f)) continue;
    Place(pnb + trees[rng.Next(trees.Length)] + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, 0.9f + R() * 0.4f, -1, foliage);
    if (foliage.childCount > 172) break;
}
// big cliffs and mounds on the hills, rocks in the clearing
for (var i = 0; i < 26; i++)
{
    var a = i / 26f * UnityEngine.Mathf.PI * 2f + R() * 0.15f; var r = 74f + R() * 18f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (rng.Next(3) == 0) continue;
    Place(pnb + "SM_Env_Rock_Cliff_0" + (1 + rng.Next(3)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 4f, 1.1f + R() * 0.6f, -1, props);
}
for (var i = 0; i < 40; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 10f + R() * 52f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 1f)) continue;
    var kind = rng.Next(4);
    var path = kind == 0 ? "SM_Env_Rock_0" + (1 + rng.Next(6)) : kind == 1 ? "SM_Env_Rock_Pile_0" + (1 + rng.Next(7)) : kind == 2 ? "SM_Env_Rock_Small_Pile_0" + (1 + rng.Next(2)) : "SM_Env_Rock_Round_01";
    Place(pnb + path + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.15f, 0.7f + R() * 0.8f, -1, props);
}
// ground cover: grass, flowers, bushes, leaves
var cover = new[] { "SM_Env_Grass_Med_Clump_01", "SM_Env_Grass_Med_Clump_02", "SM_Env_Grass_Med_Clump_03", "SM_Env_Grass_Tall_Clump_01", "SM_Env_Grass_Tall_Clump_03", "SM_Env_Grass_Tall_Clump_05", "SM_Env_Grass_Short_Clump_01", "SM_Env_Grass_Short_Clump_02", "SM_Env_Grass_Short_Clump_03", "SM_Env_Grass_Large_01", "SM_Env_Grass_Large_03", "SM_Env_Ground_Cover_01", "SM_Env_Ground_Cover_02", "SM_Env_Ground_Cover_03", "SM_Prop_Leaves_Pile_01", "SM_Env_Wildflowers_01" };
for (var i = 0; i < 520; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = UnityEngine.Mathf.Sqrt(R()) * 78f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude < plazaRadius - 2f) continue;
    if (DistToPath(new UnityEngine.Vector3(x, 0, z)) < 1.8f) continue;
    Place(pnb + cover[rng.Next(cover.Length)] + ".prefab", OnGround(x, z), 0.9f + R() * 0.6f, -1, foliage);
}
for (var i = 0; i < 45; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 20f + R() * 50f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 2f)) continue;
    Place(pnb + (rng.Next(3) == 0 ? "SM_Env_Grass_Bush_01" : "SM_Env_Bush_0" + (1 + rng.Next(3))) + ".prefab", OnGround(x, z), 0.9f + R() * 0.5f, -1, foliage);
}

// the old hall: tall stone pillars around the plaza (some broken, vines on the whole ones), its flagstones and rubble
var dfe = "Assets/Synty/PolygonDarkFortress/Prefabs/Environment/";
var pillars = 12;
for (var i = 0; i < pillars; i++)
{
    var a = i / (float)pillars * UnityEngine.Mathf.PI * 2f;
    // leave the side facing the path open
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 20f) continue;
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * (plazaRadius + 0.5f);
    var z = plaza.z + UnityEngine.Mathf.Sin(a) * (plazaRadius + 0.5f);
    var broken = i % 3 == 1;
    var p = Place(df + (broken ? "SM_Bld_L_Pillar_Half_01" : "SM_Bld_L_Pillar_01") + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.3f, 2f, -a * UnityEngine.Mathf.Rad2Deg, ruins);
    if (p && !broken && rng.Next(2) == 0) Place(fv + "SM_Env_Vine_Large_0" + (1 + rng.Next(3)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.up * (5f + R() * 4f), 1.4f, -a * UnityEngine.Mathf.Rad2Deg + 180f, ruins);
    if (broken) Place(dfe + "SM_Env_Rubble_Pile_0" + (1 + rng.Next(3)) + ".prefab", OnGround(x + 1.5f, z + 1f), 1.5f, -1, ruins);
}
// flagstones on the plaza, broken up, and rubble
for (var i = 0; i < 8; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = plazaRadius * (0.5f + R() * 0.45f);
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * r; var z = plaza.z + UnityEngine.Mathf.Sin(a) * r;
    Place(dfe + (rng.Next(2) == 0 ? "SM_Env_Rubble_Pile_0" + (1 + rng.Next(3)) : "SM_Env_Rubble_Brick_0" + (1 + rng.Next(4))) + ".prefab", OnGround(x, z), 1.3f, -1, ruins);
}
// what is left of the hall's outer walls: a colonnade of broken arches and wall stubs around the clearing
var arches = new[] { "SM_Env_Ruin_03", "SM_Env_Ruin_04", "SM_Env_Ruin_05", "SM_Env_Ruin_03", "SM_Env_Ruin_01", "SM_Env_Ruin_02" };
for (var i = 0; i < 22; i++)
{
    var a = i / 22f * UnityEngine.Mathf.PI * 2f + (R() - 0.5f) * 0.08f;
    if (rng.Next(5) == 0) continue;
    var r = 57f + (R() - 0.5f) * 3f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - spawnLocal.z).magnitude < 10f) continue;
    var piece = arches[rng.Next(arches.Length)];
    Place(dfe + piece + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.4f, 2.4f + R() * 0.5f, -a * UnityEngine.Mathf.Rad2Deg + 90f, ruins);
    if (rng.Next(2) == 0) Place(fv + "SM_Env_Vine_Creeping_0" + (1 + rng.Next(7)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.up * 0.2f, 1.6f, -1, ruins);
}
// fallen blocks in the clearing
for (var i = 0; i < 14; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 22f + R() * 30f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 4f)) continue;
    Place(dfe + (rng.Next(3) == 0 ? "SM_Env_Ruin_0" + (1 + rng.Next(2)) : "SM_Env_Rubble_Pile_0" + (1 + rng.Next(3))) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.3f, 1.4f + R() * 0.6f, -1, ruins);
}
log.AppendLine("placed " + placed);


// ---- the road from the arrival to the plaza: the village road tool's mossy flagstones ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var sceneRoads = roadsGo.AddComponent<SceneRoads>();
var roadPts = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 8; i++) { var t = i / 8f; var p = UnityEngine.Vector3.Lerp(spawnLocal, plaza + new UnityEngine.Vector3(0, 0, -plazaRadius + 1f), t); roadPts.Add(new UnityEngine.Vector2(p.x, p.z)); }
sceneRoads.Roads.Add(new SceneRoads.Road { Type = 4, Width = 3.4f, Points = roadPts, EndRound = 0.4f });

// ---- arrival, boss and monster spots ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(spawnLocal.x, spawnLocal.z);
floor.SpawnPoint = spawn.transform;
var bossPoint = new UnityEngine.GameObject("Boss Point"); bossPoint.transform.SetParent(root.transform, false);
bossPoint.transform.position = OnGround(plaza.x, plaza.z);
bossPoint.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f); // facing the path
floor.BossPoint = bossPoint.transform;
var spawns = new UnityEngine.GameObject("Spawns").transform; spawns.SetParent(root.transform, false);
floor.Spawns = spawns;
var spots = new System.Collections.Generic.List<UnityEngine.Vector3>();
var tries = 0;
while (spots.Count < 24 && tries++ < 2000)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 14f + R() * 40f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r - 4f;
    if (!Clear(x, z, 3f)) continue;
    if (spots.Any(s => (new UnityEngine.Vector3(x, 0, z) - s).magnitude < 7f)) continue;
    spots.Add(new UnityEngine.Vector3(x, 0, z));
}
foreach (var s in spots)
{
    var go = new UnityEngine.GameObject("Spot"); go.transform.SetParent(spawns, false);
    go.transform.position = OnGround(s.x, s.z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, R() * 360f, 0f);
}
log.AppendLine("spots " + spots.Count);

// ---- monster templates, hidden ----
var templates = new UnityEngine.GameObject("Templates").transform; templates.SetParent(root.transform, false);
templates.gameObject.SetActive(false);
floor.Templates = templates;
var models = new[] { "home-bunny", "ravebridge-goblin", "home-metalon-green", "home-metalon-purple", "home-metalon-red", "ravebridge-skeleton", "home-cow", "ironhill-dragon-green" };
var all = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
foreach (var m in models)
{
    var source = all.FirstOrDefault(e => e.gameObject.scene == main && e.Definition && e.Definition.Definition != null && e.Definition.Definition.Id == m && !e.IsRaidBoss && !e.IsDungeonBoss);
    if (!source) { log.AppendLine("no enemy for " + m); continue; }
    var copy = UnityEngine.Object.Instantiate(source.gameObject);
    copy.name = m;
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, scene);
    copy.transform.SetParent(templates, false);
    copy.transform.localPosition = UnityEngine.Vector3.zero;
    copy.transform.localRotation = UnityEngine.Quaternion.identity;
    copy.SetActive(true); // the parent hides it
    copy.GetComponent<EnemyController>().Island = null;
    log.AppendLine("template " + m + " from " + source.name + " scale " + source.transform.lossyScale.x.ToString("0.00"));
}

// ---- navmesh: the floor's own ----
var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = 25556799;
surface.defaultArea = 0;
surface.BuildNavMesh();
var navPath = dataFolder + "/NavMesh.asset";
if (surface.navMeshData)
{
    UnityEditor.AssetDatabase.DeleteAsset(navPath);
    UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath);
    log.AppendLine("navmesh bounds " + surface.navMeshData.sourceBounds);
}
else log.AppendLine("navmesh build gave nothing");

var scenePath = folder + "/Tower_Overgrown.unity";
log.AppendLine(root.transform.childCount + " children under the floor");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-overgrown-build.txt"), log.ToString());
return null;
