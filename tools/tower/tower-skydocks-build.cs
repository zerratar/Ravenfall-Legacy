// Builds Assets/Scenes/Tower/Tower_SkyDocks.unity: the third band's open layout ("The Storm Spire"), a grassy rock high
// above a sea of clouds at (0, 0, -6000). A wind-swept mesa whose cliffs drop away into the clouds on every side; a
// wooden pier out over the void with a sky pirates' ship moored at it and more ships drifting in the clouds; floating
// rock islets and ruined bridges that end in mid-air; a stone ring with four lightning rods for the boss (TowerLightning
// strikes them now and then); a pirates' camp; storm clouds above; 24 monster spots; the boss's spot; one hidden template
// per monster look (copied from MainWorld's enemies); its own navmesh. Editor only, not in play mode. Writes the result
// to %TEMP%/tower-skydocks-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_SkyDocks";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_SkyDocks");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Storm Spire");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 120f;
// a storm above the clouds: cold blue-grey light, pale cloud-coloured fog far below, a dim silver sun
floor.SkyAmbient = new UnityEngine.Color(0.46f, 0.50f, 0.62f);
floor.EquatorAmbient = new UnityEngine.Color(0.38f, 0.40f, 0.50f);
floor.GroundAmbient = new UnityEngine.Color(0.22f, 0.22f, 0.26f);
floor.FogColor = new UnityEngine.Color(0.66f, 0.70f, 0.78f);
floor.FogDensity = 0.0042f;
floor.SunColor = new UnityEngine.Color(0.86f, 0.89f, 1f);
floor.SunIntensity = 0.95f;
floor.SunAngles = new UnityEngine.Vector3(30f, -60f, 0f);
// the haze floor (the cloud sea) far below the rim, so the cliffs have somewhere to fall
floor.HazeDepth = 48f;

// ---- the layout (local to the root) ----
var spawnLocal = new UnityEngine.Vector3(0f, 0f, -50f);
var plaza = new UnityEngine.Vector3(0f, 0f, 22f);
var dockFoot = new UnityEngine.Vector3(10f, 0f, 58f);   // where the pier leaves the rock, going north
const float plazaRadius = 14f;
const float size = 260f, height = 120f, baseHeight = 70f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 31.7f, z * s + 77.3f);
float DistToSegment(UnityEngine.Vector3 p, UnityEngine.Vector3 a, UnityEngine.Vector3 b)
{
    var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0;
    var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    var c = a + ab * t; c.y = 0; var q = p; q.y = 0;
    return (q - c).magnitude;
}
float DistToPath(UnityEngine.Vector3 p) => UnityEngine.Mathf.Min(DistToSegment(p, spawnLocal, plaza), DistToSegment(p, plaza, dockFoot));
// the rock's edge: an uneven ring, pushed out to the north for the docks
float Edge(float x, float z)
{
    var a = UnityEngine.Mathf.Atan2(z, x);
    var r = 60f + (Noise(UnityEngine.Mathf.Cos(a) * 40f, UnityEngine.Mathf.Sin(a) * 40f, 0.05f) - 0.5f) * 14f;
    // the north (docks) and the south (the arrival) reach a little farther
    r += 6f * UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Sin(a)) + 4f * UnityEngine.Mathf.Max(0f, -UnityEngine.Mathf.Sin(a));
    return r;
}
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var edge = Edge(x, z);
    // gentle rolls on the rock, flattened round the plaza and along the paths
    var h = (Noise(x, z, 0.035f) - 0.5f) * 1.6f + (Noise(x, z, 0.11f) - 0.5f) * 0.4f;
    var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    var flat = UnityEngine.Mathf.Max(UnityEngine.Mathf.InverseLerp(plazaRadius + 6f, plazaRadius, dp), UnityEngine.Mathf.InverseLerp(6f, 2f, DistToPath(new UnityEngine.Vector3(x, 0, z))));
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    // a raised lip just inside the edge, then the cliff: straight down into the clouds
    var lip = UnityEngine.Mathf.InverseLerp(edge - 10f, edge - 2f, r) * (1f - UnityEngine.Mathf.InverseLerp(edge - 2f, edge, r));
    h += lip * 1.2f;
    var drop = UnityEngine.Mathf.InverseLerp(edge - 1f, edge + 9f, r);
    drop = drop * drop;
    h -= drop * (62f + Noise(x, z, 0.08f) * 6f);
    return h;
}

// ---- terrain layers: the meadow pack's grass, rock and paths, used as they are ----
UnityEngine.TerrainLayer L(string name)
{
    var l = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/" + name + ".terrainlayer");
    if (!l) log.AppendLine("missing layer " + name);
    return l;
}
var lGrass = L("Terrain_Meadow_Grass_02");
var lGrass2 = L("Terrain_Meadow_Grass_Pebbles_01");
var lRock = L("Terrain_Meadow_Moss_Rock_01");
var lPath = L("Terrain_Meadow_Gravel_01");
var lTiles = L("Terrain_Meadow_Ruin_Tiles_01");
var lDirt = L("Terrain_Meadow_Dirt_Cracked_Pebbles_01");

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
td.terrainLayers = new[] { lGrass, lGrass2, lRock, lPath, lTiles, lDirt };
var amRes = 256;
td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, 6];
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f;
        var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[6];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
        w[0] = 1f;
        w[1] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.06f) - 0.5f) * 3f);
        w[5] = UnityEngine.Mathf.Clamp01((r - (Edge(x, z) - 8f)) / 6f) * 1.5f;   // bare ground toward the wind-scoured edge
        w[2] = UnityEngine.Mathf.Clamp01((steep - 26f) / 10f) * 4f;
        var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
        var ring = UnityEngine.Mathf.Clamp01((plazaRadius + 1f - dp) / 3f + (Noise(x, z, 0.3f) - 0.5f) * 0.6f);
        w[4] = ring * 1.8f;
        var path = UnityEngine.Mathf.Clamp01((2.8f - DistToPath(new UnityEngine.Vector3(x, 0, z))) / 1.6f + (Noise(x, z, 0.4f) - 0.5f) * 0.6f);
        w[3] = path * (1f - ring) * 1.3f;
        var sum = w.Sum();
        for (var k = 0; k < w.Length; k++) maps[zi, xi, k] = w[k] / sum;
    }
td.SetAlphamaps(0, 0, maps);
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

UnityEngine.Vector3 OnGround(float x, float z) => origin + new UnityEngine.Vector3(x, GroundAt(x, z), z);

// ---- props ----
var rng = new System.Random(31);
float R() => (float)rng.NextDouble();
var placed = 0;
UnityEngine.GameObject Place(string path, UnityEngine.Vector3 world, float scale, float yaw = -1f, UnityEngine.Transform parent = null, float pitch = 0f, float roll = 0f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent ? parent : root.transform, true);
    go.transform.position = world;
    go.transform.rotation = UnityEngine.Quaternion.Euler(pitch, yaw < 0 ? R() * 360f : yaw, roll);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    placed++;
    return go;
}
void Bare(UnityEngine.GameObject go) { if (go) foreach (var col in go.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(col); }
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var sky = new UnityEngine.GameObject("Sky").transform; sky.SetParent(root.transform, false);
var ringT = new UnityEngine.GameObject("Storm Ring").transform; ringT.SetParent(root.transform, false);
var docks = new UnityEngine.GameObject("Docks").transform; docks.SetParent(root.transform, false);
var pr = "Assets/Synty/PolygonPirateRealm/Prefabs/";
var adv = "Assets/PolygonAdventure/Prefabs/Environments/";

bool Clear(float x, float z, float margin)
{
    var p = new UnityEngine.Vector3(x, 0, z);
    if (new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude < plazaRadius + margin) return false;
    if (DistToPath(p) < 3.5f + margin * 0.5f) return false;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - spawnLocal.z).magnitude < 9f + margin) return false;
    if (UnityEngine.Mathf.Sqrt(x * x + z * z) > Edge(x, z) - 4f - margin * 0.5f) return false;
    return true;
}

// the clouds: one material for the white sea below, a darker one for the storm above
var cloudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(adv + "SM_Env_Cloud_01.prefab");
var cloudMat = cloudPrefab ? cloudPrefab.GetComponentInChildren<UnityEngine.Renderer>().sharedMaterial : null;
UnityEngine.Material Tinted(string name, UnityEngine.Color c)
{
    if (!cloudMat) return null;
    var path = dataFolder + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m) { m = new UnityEngine.Material(cloudMat); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    else m.CopyPropertiesFromMaterial(cloudMat);
    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
}
var seaMat = Tinted("CloudSea", new UnityEngine.Color(0.86f, 0.88f, 0.93f));
var stormMat = Tinted("StormCloud", new UnityEngine.Color(0.36f, 0.39f, 0.48f));
void Cloud(UnityEngine.Vector3 world, float scale, UnityEngine.Material mat)
{
    var go = Place(adv + "SM_Env_Cloud_0" + (1 + rng.Next(6)) + ".prefab", world, scale, -1, sky);
    if (!go) return;
    Bare(go);
    if (mat) foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
}
// the cloud sea: puffs below the rim, thick near the cliffs, out to the horizon
for (var i = 0; i < 150; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 62f + R() * R() * 190f;
    Cloud(origin + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a) * r, -14f - R() * 30f, UnityEngine.Mathf.Sin(a) * r), 5f + R() * 7f, seaMat);
}
// the storm above: dark heavy clouds, mostly out past the rock so the camera over the players stays clear
for (var i = 0; i < 36; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 95f + R() * 120f;
    Cloud(origin + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a) * r, 64f + R() * 30f, UnityEngine.Mathf.Sin(a) * r), 7f + R() * 7f, stormMat);
}

// floating rock islets out in the clouds: rock stacks upside down, a few with a bent pine on top
for (var i = 0; i < 9; i++)
{
    var a = i / 9f * UnityEngine.Mathf.PI * 2f + R() * 0.4f; var r = 95f + R() * 70f;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 25f) continue; // behind the arrival's camera
    var scale = 0.35f + R() * 0.45f;
    var at = origin + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a) * r, -6f + R() * 34f, UnityEngine.Mathf.Sin(a) * r);
    var islet = Place(pr + "Environment/SM_Env_Rock_Stack_01" + ".prefab", at, scale, -1, sky, 180f);
    Bare(islet);
    if (rng.Next(2) == 0) Place(pr + "Environment/SM_Env_Conifer_0" + (1 + rng.Next(2)) + ".prefab", at + UnityEngine.Vector3.up * 0.2f, 0.8f + R() * 0.4f, -1, sky);
    Cloud(at + new UnityEngine.Vector3(0f, -12f * scale * 2f, 0f), 4f + R() * 3f, seaMat);
}

// ruined bridges that once reached the islets, broken off over the void (west and east)
foreach (var (bx, bz, yaw, segments) in new[] { (-58f, 6f, -90f, 4), (60f, -10f, 90f, 3) })
{
    var dir = UnityEngine.Quaternion.Euler(0f, yaw, 0f) * UnityEngine.Vector3.forward;
    var start = new UnityEngine.Vector3(bx, 0f, bz);
    var baseY = GroundAt(bx, bz);
    for (var s2 = 0; s2 < segments; s2++)
    {
        var p = origin + start + dir * (s2 * 4.8f) + UnityEngine.Vector3.up * (baseY - 0.1f);
        Bare(Place("Assets/PolygonDungeonRealms/Prefabs/Buildings/SM_Bld_Ruin_Bridge_0" + (s2 == segments - 1 ? 4 : 1 + rng.Next(3)) + ".prefab", p, 1f, yaw, props));
    }
    Bare(Place("Assets/PolygonDungeonRealms/Prefabs/Buildings/SM_Bld_Ruin_Bridge_Pillar_Large_01.prefab", origin + start + dir * (segments * 2.4f) + UnityEngine.Vector3.up * (baseY - 14f), 1f, yaw, props));
}

// the docks: a pier out over the cloud sea with a sky pirates' ship moored at it, more ships out in the clouds
var pierDir = UnityEngine.Vector3.forward;
var pierY = GroundAt(dockFoot.x, dockFoot.z) + 0.15f;
for (var s2 = 0; s2 < 4; s2++)
{
    var p = origin + dockFoot + pierDir * (s2 * 7.4f + 7.4f) + UnityEngine.Vector3.up * pierY;
    Place(pr + "Buildings/SM_Bld_Dock_Long_0" + (1 + rng.Next(3)) + ".prefab", p, 1f, 0f, docks);
    // posts down into the clouds
    if (s2 % 2 == 1) foreach (var side in new[] { -1.3f, 1.3f }) Bare(Place(pr + "Props/SM_Prop_Flag_Post_01.prefab", p + new UnityEngine.Vector3(side, -0.2f, -3.6f), 1.2f, 0f, docks, 90f));
}
var pierEnd = origin + dockFoot + pierDir * 33f + UnityEngine.Vector3.up * pierY;
var moored = Place(pr + "Vehicles/SM_Veh_Preset_M_01.prefab", pierEnd + new UnityEngine.Vector3(10f, -7.5f, -6f), 0.7f, 8f, docks);
Bare(moored);
Cloud(pierEnd + new UnityEngine.Vector3(10f, -11f, -6f), 7f, seaMat);
var drifting = Place(pr + "Vehicles/SM_Veh_Preset_L_Dressed_01.prefab", origin + new UnityEngine.Vector3(-70f, -6f, 120f), 0.85f, 120f, sky);
Bare(drifting);
Cloud(origin + new UnityEngine.Vector3(-70f, -12f, 120f), 10f, seaMat);
var small = Place(pr + "Vehicles/SM_Veh_Preset_S_01.prefab", origin + new UnityEngine.Vector3(118f, 6f, 30f), 0.8f, 200f, sky);
Bare(small);
Cloud(origin + new UnityEngine.Vector3(118f, 1f, 30f), 7f, seaMat);
// dock clutter: crates, barrels, rolled sails, an anchor, a crow's nest knocked down, lanterns on posts
foreach (var (dx, dz, path, sc) in new[] {
    (4f, 54f, "Props/SM_Prop_Crate_01", 1f), (6f, 50f, "Props/SM_Prop_Barrel_01", 1f), (15f, 54f, "Props/SM_Prop_Sails_Rolled_Pile_01", 1f),
    (16f, 50f, "Props/SM_Prop_Anchor_01", 1.4f), (13f, 46f, "Props/SM_Prop_Barrel_02", 1f),
    (18f, 57f, "Props/SM_Prop_Lamp_Post_01", 1f), (2f, 57f, "Props/SM_Prop_Lamp_Post_01", 1f) })
{
    if (DistToPath(new UnityEngine.Vector3(dx, 0, dz)) < 2.2f) continue;
    Place(pr + path + ".prefab", OnGround(dx, dz), sc, -1, docks);
}

// the pirates' camp, west of the plaza: a cooking fire, sacks, a table with a map, flags
var camp = new UnityEngine.Vector3(-30f, 0f, -8f);
if (Clear(camp.x, camp.z, 4f))
{
    UnityEngine.Vector3 C(float dx, float dz) => OnGround(camp.x + dx, camp.z + dz);
    Bare(Place(pr + "Props/SM_Prop_Campfire_01.prefab", C(0f, 0f), 1f, -1, props));
    var fire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", C(0f, 0f) + UnityEngine.Vector3.up * 0.25f, 1f, 0f, props);
    var lg = new UnityEngine.GameObject("Camp Light"); lg.transform.SetParent(props, false); lg.transform.position = C(0f, 0f) + UnityEngine.Vector3.up * 1.2f;
    var cl = lg.AddComponent<UnityEngine.Light>(); cl.type = UnityEngine.LightType.Point; cl.color = new UnityEngine.Color(1f, 0.6f, 0.3f); cl.range = 9f; cl.intensity = 2f; cl.shadows = UnityEngine.LightShadows.None;
    Bare(Place(pr + "Props/SM_Prop_Table_01.prefab", C(3.2f, 1.2f), 1f, 30f, props));
    Bare(Place(pr + "Props/SM_Prop_Map_01.prefab", C(3.2f, 1.2f) + UnityEngine.Vector3.up * 0.9f, 1f, 30f, props));
    Bare(Place(pr + "Props/SM_Prop_Sack_01.prefab", C(-2.5f, 1.8f), 1f, -1, props));
    Bare(Place(pr + "Props/SM_Prop_Sack_02.prefab", C(-2.8f, -1.2f), 1f, -1, props));
    Bare(Place(pr + "Props/SM_Prop_Barrel_Half_01.prefab", C(1.5f, -2.6f), 1f, -1, props));
    Bare(Place(pr + "Props/SM_Prop_Flag_01.prefab", C(-3.6f, 3.6f), 1.2f, 40f, props));
    Bare(Place(pr + "Props/SM_Prop_Treasure_Chest_01.prefab", C(2.4f, -1.4f), 1f, 200f, props));
}

// the rock's own look: wind-bent pines and shrubs near the edge, basalt boulders, ground cover
for (var i = 0; i < 70; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 18f + R() * 44f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    var kind = rng.Next(6);
    if (!Clear(x, z, kind == 0 ? 4f : 1.5f)) continue;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - (spawnLocal.z - 16f)).magnitude < 18f && kind == 0) continue; // the camera behind the arrival
    var path = kind == 0 ? pr + "Environment/SM_Env_Conifer_0" + (1 + rng.Next(2))
        : kind == 1 ? pr + "Environment/SM_Env_Rock_Basalt_0" + (1 + rng.Next(2))
        : kind == 2 ? pr + "Environment/SM_Env_Shrub_0" + (1 + rng.Next(2))
        : kind == 3 ? pr + "Environment/SM_Env_Ground_Cover_0" + (1 + rng.Next(3))
        : kind == 4 ? pr + "Environment/SM_Env_Fern_0" + (1 + rng.Next(2))
        : pr + "Environment/SM_Env_Ground_Cover_0" + (1 + rng.Next(8));
    Place(path + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, kind == 1 ? 1.2f + R() * 1.4f : 0.9f + R() * 0.5f, -1, props);
}
// wind-swept tall grass over the rock (no colliders: monsters and players walk through it)
var grassT = new UnityEngine.GameObject("Grass").transform; grassT.SetParent(root.transform, false);
for (var i = 0; i < 260; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 8f + R() * 52f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 0.5f)) continue;
    var go = rng.Next(3) == 0
        ? Place(pr + "Environment/SM_Env_Ground_Cover_0" + (1 + rng.Next(8)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.05f, 0.8f + R() * 0.5f, -1, grassT)
        : Place("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_Tall_0" + (1 + rng.Next(4)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.05f, 0.7f + R() * 0.6f, -1, grassT);
    Bare(go);
    if (go) foreach (var rr in go.GetComponentsInChildren<UnityEngine.Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
}
// pirate flags on the windward edge
foreach (var (fx2, fz2, yaw2) in new[] { (-40f, 30f, 60f), (42f, 24f, -60f), (-22f, 46f, 20f), (36f, -36f, 210f) })
{
    if (!Clear(fx2, fz2, 0f)) continue;
    Bare(Place(pr + "Props/SM_Prop_Flag_Post_02.prefab", OnGround(fx2, fz2), 2.4f, yaw2, props, 0f, 90f));
    Bare(Place(pr + "Props/SM_Prop_Flag_0" + (1 + rng.Next(2)) + ".prefab", OnGround(fx2, fz2) + UnityEngine.Vector3.up * 4.8f, 0.9f, yaw2, props));
}
// cliffs dressing the edge, so the rock reads as rock and not a cut-off terrain
for (var i = 0; i < 30; i++)
{
    var a = i / 30f * UnityEngine.Mathf.PI * 2f + R() * 0.1f;
    var e = Edge(UnityEngine.Mathf.Cos(a) * 50f, UnityEngine.Mathf.Sin(a) * 50f);
    var x = UnityEngine.Mathf.Cos(a) * (e + 3f); var z = UnityEngine.Mathf.Sin(a) * (e + 3f);
    if (DistToSegment(new UnityEngine.Vector3(x, 0, z), dockFoot, dockFoot + pierDir * 40f) < 8f) continue; // the pier
    Bare(Place(pr + "Environment/SM_Env_Cliff_0" + (1 + rng.Next(3)) + ".prefab", origin + new UnityEngine.Vector3(x, -9f - R() * 4f, z), 0.9f + R() * 0.5f, -a * UnityEngine.Mathf.Rad2Deg + 90f, props));
}

// the storm ring: four lightning rods (an iron pillar each), braziers with blue fire between
var rods = new System.Collections.Generic.List<UnityEngine.Transform>();
for (var i = 0; i < 4; i++)
{
    var a = (45f + i * 90f) * UnityEngine.Mathf.Deg2Rad;
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * (plazaRadius + 2f);
    var z = plaza.z + UnityEngine.Mathf.Sin(a) * (plazaRadius + 2f);
    // a tall dark stone column with an iron spear on top: the rod the bolts find
    Place("Assets/Synty/PolygonDarkFortress/Prefabs/Buildings/SM_Bld_L_Pillar_01.prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, 1.15f, -a * UnityEngine.Mathf.Rad2Deg, ringT);
    var top = new UnityEngine.GameObject("Rod Top " + i); top.transform.SetParent(ringT, false);
    var columnTop = OnGround(x, z) + UnityEngine.Vector3.up * (6.0f * 1.15f - 0.2f);
    Place("Assets/Synty/PolygonSamuraiEmpire/Prefabs/Weapons/SM_Wep_Spear_01.prefab", columnTop + UnityEngine.Vector3.up * 1.3f, 1.6f, 0f, ringT);
    top.transform.position = columnTop + UnityEngine.Vector3.up * 3.2f;
    rods.Add(top.transform);
}
for (var i = 0; i < 8; i++)
{
    if (i % 2 == 0) continue;
    var a = (i * 45f) * UnityEngine.Mathf.Deg2Rad;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 30f) continue; // the way in
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * (plazaRadius + 1f);
    var z = plaza.z + UnityEngine.Mathf.Sin(a) * (plazaRadius + 1f);
    var vp = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
    Place(vp + "SM_Prop_Brazier_01.prefab", OnGround(x, z), 1.7f, -1, ringT);
    var flame = Place("Assets/NeatWolf/Bundle03-MagicSpells/_Pillars/Pillar01_lightning.prefab", OnGround(x, z) + UnityEngine.Vector3.up * 1.4f, 0.35f, 0f, ringT);
}
// the storm's own sky over the place (the world's sunny one is hidden while the camera is here)
var skyDome = root.AddComponent<TowerSky>();
skyDome.Zenith = new UnityEngine.Color(0.20f, 0.22f, 0.29f);
skyDome.Horizon = floor.FogColor;
var lightning = root.AddComponent<TowerLightning>();
lightning.Rods = rods.ToArray();
lightning.CloudHeight = 70f;
log.AppendLine("placed " + placed + ", rods " + rods.Count);

// ---- the roads: gravel from the arrival to the plaza and on to the docks ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var sceneRoads = roadsGo.AddComponent<SceneRoads>();
var roadPts = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 8; i++) { var t = i / 8f; var p = UnityEngine.Vector3.Lerp(spawnLocal, plaza + new UnityEngine.Vector3(0, 0, -plazaRadius + 1f), t); roadPts.Add(new UnityEngine.Vector2(p.x, p.z)); }
sceneRoads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 3.2f, Points = roadPts, EndRound = 0.4f });
var dockPts = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 6; i++) { var t = i / 6f; var p = UnityEngine.Vector3.Lerp(plaza + new UnityEngine.Vector3(0, 0, plazaRadius - 1f), dockFoot, t); dockPts.Add(new UnityEngine.Vector2(p.x, p.z)); }
sceneRoads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 2.8f, Points = dockPts, EndRound = 0.4f });

// ---- arrival, boss and monster spots ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(spawnLocal.x, spawnLocal.z);
floor.SpawnPoint = spawn.transform;
var bossPoint = new UnityEngine.GameObject("Boss Point"); bossPoint.transform.SetParent(root.transform, false);
bossPoint.transform.position = OnGround(plaza.x, plaza.z);
bossPoint.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
floor.BossPoint = bossPoint.transform;
var spawns = new UnityEngine.GameObject("Spawns").transform; spawns.SetParent(root.transform, false);
floor.Spawns = spawns;
var spots = new System.Collections.Generic.List<UnityEngine.Vector3>();
var tries = 0;
while (spots.Count < 24 && tries++ < 4000)
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
var models = new[] { "away-pirate", "away-pirate-captain", "mangrove-pirate-captain", "home-bunny", "kyojin-ninja", "ironhill-lord-quackers", "ironhill-dragon-blue", "ironhill-dragon-purple", "home-cow" };
var all = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include);
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
    copy.SetActive(true);
    copy.GetComponent<EnemyController>().Island = null;
    log.AppendLine("template " + m + " from " + source.name + " scale " + source.transform.lossyScale.x.ToString("0.00"));
}

// ---- navmesh ----
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

var scenePath = folder + "/Tower_SkyDocks.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-skydocks-build.txt"), log.ToString());
return null;
