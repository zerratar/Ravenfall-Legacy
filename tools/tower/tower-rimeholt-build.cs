// Builds Assets/Scenes/Tower/Tower_Rimeholt.unity: Rimeholt, the tower's second town (the Frozen Vault band, reached by
// beating floor 10; docs/tower-design.md, section 4). A snowed-in hamlet on a ledge of the tower: a cobbled square round a
// stone statue, Hilda's forge (glowing), the Thawing Mead hall, snow-roofed houses, Old Tove's frozen fishing pond, a
// campfire with pelts and logs to rest at (!tower rest), watchtowers, and at the north end a gate with stairs up to the
// vault. Its four people (IslandNpc), stroll spots, roads (SceneRoads: cobbles round the square, gravel lanes), its own
// navmesh. At (1200, 0, -6000), beside whatever floor is loaded (Mossgate sits at 600).
// Writes %TEMP%/tower-rimeholt-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(1200f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_Rimeholt";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Rimeholt");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var root = new UnityEngine.GameObject("Tower Town - Rimeholt");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var town = root.AddComponent<TowerTown>();
town.Key = "rimeholt";
town.Radius = 95f;
// a cold late afternoon: blue shadows, a low warm sun
town.SkyAmbient = new UnityEngine.Color(0.60f, 0.66f, 0.78f);
town.EquatorAmbient = new UnityEngine.Color(0.50f, 0.52f, 0.58f);
town.GroundAmbient = new UnityEngine.Color(0.30f, 0.30f, 0.34f);
town.FogColor = new UnityEngine.Color(0.80f, 0.82f, 0.88f);
town.FogDensity = 0.0048f;
town.SunColor = new UnityEngine.Color(1f, 0.86f, 0.70f);
town.SunIntensity = 1.2f;
town.SunAngles = new UnityEngine.Vector3(24f, -50f, 0f);

// ---- the ground ----
const float size = 220f, height = 36f, baseHeight = 2f, flatRadius = 46f;
var pond = new UnityEngine.Vector2(17f, -20f);
const float pondRadius = 6.5f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 71.3f, z * s + 9.4f);
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var h = (Noise(x, z, 0.03f) - 0.5f) * 0.7f;
    var flat = UnityEngine.Mathf.InverseLerp(30f, 18f, r);
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    var dp = (new UnityEngine.Vector2(x, z) - pond).magnitude;
    h = UnityEngine.Mathf.Lerp(h, -0.3f, UnityEngine.Mathf.InverseLerp(pondRadius + 3f, pondRadius - 0.5f, dp));
    var rim = UnityEngine.Mathf.InverseLerp(flatRadius, 92f, r); rim = rim * rim * (3f - 2f * rim);
    // the tower's wall rises behind the gate in the north
    var north = UnityEngine.Mathf.Clamp01((z - 30f) / 30f);
    h += rim * (16f + Noise(x, z, 0.05f) * 12f) + north * north * 10f * UnityEngine.Mathf.InverseLerp(14f, 30f, UnityEngine.Mathf.Abs(x));
    return h;
}
var res = 257;
var td = new UnityEngine.TerrainData { heightmapResolution = res, size = new UnityEngine.Vector3(size, height, size) };
var tdPath = dataFolder + "/Terrain.asset";
UnityEditor.AssetDatabase.DeleteAsset(tdPath);
UnityEditor.AssetDatabase.CreateAsset(td, tdPath);
var heights = new float[res, res];
for (var zi = 0; zi < res; zi++)
    for (var xi = 0; xi < res; xi++)
    {
        var x = xi / (float)(res - 1) * size - size / 2f; var z = zi / (float)(res - 1) * size - size / 2f;
        heights[zi, xi] = UnityEngine.Mathf.Clamp01((GroundAt(x, z) + baseHeight) / height);
    }
td.SetHeights(0, 0, heights);
// the glacier floor's snow layers
UnityEngine.TerrainLayer GL(string n) { var l = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(folder + "/Tower_Glacier/" + n + ".terrainlayer"); if (!l) log.AppendLine("missing layer " + n); return l; }
td.terrainLayers = new[] { GL("Snow"), GL("Snow_Shade"), GL("Snow_Packed"), GL("Ice"), GL("Rock") };
var amRes = 256; td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, 5];
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f; var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[5];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
        w[0] = 1f;
        w[1] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.05f) - 0.45f) * 3f);
        // trodden snow in the middle of town
        w[2] = UnityEngine.Mathf.Clamp01((24f - r) / 10f) * (0.6f + Noise(x + 9f, z, 0.2f) * 0.6f);
        w[3] = UnityEngine.Mathf.Clamp01((pondRadius + 0.4f - (new UnityEngine.Vector2(x, z) - pond).magnitude) / 1.2f) * 6f;
        w[4] = UnityEngine.Mathf.Clamp01((steep - 28f) / 10f) * 2.5f;
        var sum = w.Sum(); for (var k = 0; k < 5; k++) maps[zi, xi, k] = w[k] / sum;
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
terrain.drawInstanced = true; terrain.heightmapPixelError = 4f; terrain.basemapDistance = 400f;
UnityEngine.Vector3 OnGround(float x, float z) => origin + new UnityEngine.Vector3(x, GroundAt(x, z), z);

// ---- placing ----
var rng = new System.Random(17);
float R() => (float)rng.NextDouble();
var placed = 0;
UnityEngine.GameObject Place(string path, float x, float z, float scale, float yaw, UnityEngine.Transform parent, float sink = 0f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent, true);
    go.transform.position = OnGround(x, z) + UnityEngine.Vector3.down * sink;
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw < 0 ? R() * 360f : yaw, 0f);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    placed++;
    return go;
}
float Facing(float x, float z, float tx, float tz) => UnityEngine.Mathf.Atan2(tx - x, tz - z) * UnityEngine.Mathf.Rad2Deg;
var blockers = new UnityEngine.GameObject("Blockers").transform; blockers.SetParent(root.transform, false);
void Block(UnityEngine.GameObject go, float shrink = 0.85f)
{
    if (!go) return;
    var rs = go.GetComponentsInChildren<UnityEngine.MeshRenderer>(); if (rs.Length == 0) return;
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    var c = new UnityEngine.GameObject("Block " + go.name); c.transform.SetParent(blockers, false);
    c.transform.position = new UnityEngine.Vector3(b.center.x, b.min.y + 1.5f, b.center.z);
    var bc = c.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(b.size.x * shrink, 3f, b.size.z * shrink);
}
var buildings = new UnityEngine.GameObject("Buildings").transform; buildings.SetParent(root.transform, false);
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var foliage = new UnityEngine.GameObject("Foliage").transform; foliage.SetParent(root.transform, false);
var people = new UnityEngine.GameObject("People").transform; people.SetParent(root.transform, false);
var lights = new UnityEngine.GameObject("Lights").transform; lights.SetParent(root.transform, false);
var sv = "Assets/PolygonVikings/Prefabs/Snow_Variant/";
var vp = "Assets/PolygonVikings/Prefabs/Props/";
var vr = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
var ve = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";
var vfx = "Assets/Synty/PolygonVikingRealm/Prefabs/FX/";
UnityEngine.Light Lamp(float x, float y, float z, UnityEngine.Color c, float range, float intensity)
{
    var l = new UnityEngine.GameObject("Light"); l.transform.SetParent(lights, false); l.transform.position = OnGround(x, z) + UnityEngine.Vector3.up * y;
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = c; light.range = range; light.intensity = intensity; light.shadows = UnityEngine.LightShadows.None;
    return light;
}
var warm = new UnityEngine.Color(1f, 0.62f, 0.30f);

// a house: the Vikings pack's snowy walls with the roof that belongs on them
UnityEngine.GameObject House(string walls, string roof, float x, float z, float scale)
{
    var yaw = Facing(x, z, 0f, 0f);
    var h = Place(sv + walls + ".prefab", x, z, scale, yaw, buildings, 0.1f);
    if (h && roof != null)
    {
        var rp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(sv + roof + ".prefab");
        if (rp) { var r = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(rp, scene); r.transform.SetParent(h.transform, false); r.transform.localPosition = UnityEngine.Vector3.zero; r.transform.localRotation = UnityEngine.Quaternion.identity; r.transform.localScale = UnityEngine.Vector3.one; }
        else log.AppendLine("missing roof " + roof);
    }
    Block(h);
    return h;
}
House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", -21f, 7f, 1.55f);
House("SM_Bld_Base_Door_02_Snow", "SM_Bld_Roof_01_Snow", -22f, -9f, 1.5f);
House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", 22f, 9f, 1.55f);
House("SM_Bld_Base_Door_02_Snow", "SM_Bld_Roof_01_Snow", 26f, -4f, 1.45f);
House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", -30f, 18f, 1.4f);
// the Thawing Mead: the long hall north of the square
var hall = House("SM_Bld_Base_Long_Door_01_Snow", "SM_Bld_Roof_Long_01_Snow", 0f, 22f, 2.0f);
Place(vr + "SM_Prop_Mead_Barrel_01.prefab", 5.5f, 16.5f, 1.2f, 200f, props);
Place(vr + "SM_Prop_Barrel_01.prefab", -5f, 16.8f, 1.1f, -1, props);
Place(vr + "SM_Prop_Barrel_02.prefab", -6.2f, 17.4f, 1.1f, -1, props);
Place(sv + "SM_Prop_Shield_Decor_01_Snow.prefab", 3.5f, 15.6f, 1.3f, 180f, props);
Place(vp + "SM_Prop_TorchStick_01.prefab", -3f, 15.8f, 1.3f, 0f, props);
Place(vp + "SM_Prop_TorchStick_01.prefab", 3f, 15.8f, 1.3f, 0f, props);
Lamp(0f, 2.5f, 15.5f, warm, 10f, 1.8f);
// watchtowers either side of the gate
Block(Place(sv + "SM_Bld_Guard_Tower_01_Snow.prefab", -16f, 34f, 1.4f, 180f, buildings));
Block(Place(sv + "SM_Bld_Guard_Tower_01_Snow.prefab", 16f, 34f, 1.4f, 180f, buildings));

// the gate up to the vault: the village entrance with stone stairs climbing behind it, flags and torches
Place(sv + "SM_Prop_Village_Entrance_01_Snow.prefab", 0f, 36f, 2.2f, 180f, buildings);
for (var i = 0; i < 3; i++) Place(ve + "SM_Env_Stone_Stairs_0" + (1 + i % 3) + ".prefab", 0f, 41f + i * 4.5f, 2.2f, 180f, props, 0.4f);
foreach (var side in new[] { -1f, 1f })
{
    Place(sv + "SM_Prop_Flag_01_Snow.prefab", side * 5f, 34.5f, 1.6f, 180f, props);
    Place(vp + "SM_Prop_TorchStick_01.prefab", side * 3.4f, 34f, 1.4f, 0f, props);
    Lamp(side * 3.4f, 2.6f, 34f, warm, 9f, 1.6f);
}
// fences from the gate along the wall
for (var i = 0; i < 6; i++)
{
    foreach (var side in new[] { -1f, 1f })
    {
        var x = side * (6f + i * 3.6f);
        Place(sv + "SM_Prop_Fence_Wood_01_Snow.prefab", x, 36.5f + i * 0.3f, 1.4f, 90f, props);
    }
}

// Hilda's forge: a stall roof over the furnace and anvil, firelight
var fx0 = -12f; var fz0 = 13f;
var stall = Place(sv + "SM_Bld_Stall_Cover_01_Snow.prefab", fx0, fz0 + 1.5f, 1.6f, Facing(fx0, fz0, 0f, 0f), buildings);
Place(vr + "SM_Prop_Furnace_01.prefab", fx0 - 1.6f, fz0 + 2.2f, 1.1f, Facing(fx0, fz0, 0f, 0f), props);
Place(sv + "SM_Prop_Anvil_Stump_01_Snow.prefab", fx0 + 1.2f, fz0 + 0.3f, 1.2f, 30f, props);
Place(vr + "SM_Prop_Grindstone_01.prefab", fx0 + 2.8f, fz0 + 2.2f, 1.1f, -1, props);
Place(vr + "SM_Prop_Weapon_Rack_01.prefab", fx0 - 3.2f, fz0 - 0.4f, 1.1f, 120f, props);
var forgeFire = Place(vfx + "FX_Fire_Large_01.prefab", fx0 - 1.6f, fz0 + 2.2f, 0.35f, 0f, props);
if (forgeFire) forgeFire.transform.position += UnityEngine.Vector3.up * 0.9f;
Lamp(fx0 - 1f, 2f, fz0 + 1.5f, new UnityEngine.Color(1f, 0.5f, 0.2f), 11f, 2.6f);
Block(stall, 0.6f);

// Tove's frozen pond: a fishing hole, a fish rack, a stool and a small fire
Place(vr + "SM_Prop_Fish_Rack_01.prefab", pond.x - 7f, pond.y - 4f, 1.1f, 60f, props);
Place(sv + "SM_Prop_Fish_Rack_02_Snow.prefab", pond.x + 4f, pond.y - 8f, 1.1f, -1, props);
Place(vr + "SM_Prop_Stool_01.prefab", pond.x - 4.6f, pond.y + 2.5f, 1f, -1, props);
Place(vr + "SM_Prop_Fish_Pile_01.prefab", pond.x - 5.2f, pond.y + 1f, 1f, -1, props);
Place(vr + "SM_Prop_Bucket_01.prefab", pond.x - 3.8f, pond.y + 3.6f, 1f, -1, props);
for (var i = 0; i < 9; i++)
{
    var a = i / 9f * UnityEngine.Mathf.PI * 2f + R() * 0.3f; var r = pondRadius + 1.2f + R();
    var x = pond.x + UnityEngine.Mathf.Cos(a) * r; var z = pond.y + UnityEngine.Mathf.Sin(a) * r;
    if ((new UnityEngine.Vector2(x, z) - new UnityEngine.Vector2(pond.x - 4.6f, pond.y + 2.5f)).magnitude < 2.5f) continue;
    Place(sv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow.prefab", x, z, 0.5f + R() * 0.4f, -1, props, 0.15f);
}

// the square: a stone statue in the middle, benches round it
Place(sv + "SM_Stone_Statue_01_Snow.prefab", 0f, 0f, 1.5f, 180f, props);
var statueBlock = new UnityEngine.GameObject("Block statue"); statueBlock.transform.SetParent(blockers, false); statueBlock.transform.position = OnGround(0f, 0f) + UnityEngine.Vector3.up * 1.5f;
statueBlock.AddComponent<UnityEngine.CapsuleCollider>().radius = 1.4f;
for (var i = 0; i < 4; i++)
{
    var a = i / 4f * UnityEngine.Mathf.PI * 2f + 0.785f; var x = UnityEngine.Mathf.Cos(a) * 6.2f; var z = UnityEngine.Mathf.Sin(a) * 6.2f;
    Place(sv + "SM_Prop_Bench_01_Snow.prefab", x, z, 1.2f, Facing(x, z, 0f, 0f) + 90f, props);
}
// odds and ends: barrels, logs, a wagon, a totem, a cow skull on a pole
Place(sv + "SM_Prop_Wagon_01_Snow.prefab", 12f, 15f, 1.2f, 35f, props);
Place(sv + "SM_Prop_Logs_01_Snow.prefab", -16f, 20f, 1.3f, -1, props);
Place(sv + "SM_Prop_Barrel_Half_01_Snow.prefab", 18f, 2f, 1.2f, -1, props);
Place(sv + "SM_Prop_Barrel_Half_01_Snow.prefab", -17f, -2f, 1.2f, -1, props);
Place(sv + "SM_Prop_Rock_Totem_01_Snow.prefab", 9f, -27f, 1.4f, -1, props);
Place(sv + "SM_Prop_Skull_Pole_01_Snow.prefab", -9f, -28f, 1.3f, 0f, props);
Place(sv + "SM_Prop_Clay_Pot_02_Snow.prefab", 19.5f, 5f, 1.1f, -1, props);
Place(sv + "SM_Prop_Rack_01_Snow.prefab", -24.5f, 1.5f, 1.2f, 90f, props);

// the resting place (!tower rest): a fire pit south-west of the square, logs to sit and meditate on, pelts to sleep on
var restC = new UnityEngine.Vector2(-15f, -21f);
var rest = new UnityEngine.GameObject("Resting Place").transform; rest.SetParent(root.transform, false);
var restSpots = new UnityEngine.GameObject("Rest Spots").transform; restSpots.SetParent(rest, false);
town.RestSpots = restSpots;
Place(vr + "SM_Prop_Fire_Pit_02.prefab", restC.x, restC.y, 1.1f, 0f, rest);
var campfire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", restC.x, restC.y, 1.2f, 0f, rest);
if (campfire) campfire.transform.position += UnityEngine.Vector3.up * 0.25f;
Lamp(restC.x, 1.2f, restC.y, warm, 9f, 2.2f);
for (var i = 0; i < 6; i++)
{
    var a = i / 6f * UnityEngine.Mathf.PI * 2f + 0.3f;
    var p = restC + new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * 2.6f;
    var yaw = Facing(p.x, p.y, restC.x, restC.y);
    var sleep = i % 3 == 2;
    if (sleep) Place(vr + "SM_Prop_Pelt_0" + (1 + rng.Next(7)) + ".prefab", p.x, p.y, 1.2f, yaw, rest);
    else Place(sv + "SM_Prop_Log_01_Snow.prefab", p.x, p.y, 0.9f, yaw + 90f, rest, 0.1f);
    var s = new UnityEngine.GameObject((sleep ? "Sleep " : "Meditate ") + (i + 1)).transform; s.SetParent(restSpots, false);
    s.SetPositionAndRotation(OnGround(p.x, p.y) + UnityEngine.Vector3.up * 0.08f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
}

// pines on the slopes (none in town: the camera looks down from behind), mountains behind
var trees = new[] { "SM_Env_Tree_Pine_01_Snow", "SM_Env_Tree_Pine_02_Snow", "SM_Env_Tree_Pine_03_Snow", "SM_Env_Tree_Pine_04_Snow", "SM_Env_Tree_Pine_Large_01_Snow", "SM_Env_Tree_Pine_Large_02_Snow", "SM_Env_Tree_Pine_Large_03_Snow" };
for (var i = 0; i < 240; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 44f + R() * 50f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 28f && UnityEngine.Mathf.Abs(x) < 12f) continue; // the stairs
    if (z < -30f && UnityEngine.Mathf.Abs(x) < 10f && z > -60f) continue; // the way in
    var k = rng.Next(trees.Length);
    Place(sv + trees[k] + ".prefab", x, z, k < 4 ? 2.4f + R() : 1.1f + R() * 0.5f, -1, foliage, 0.3f);
}
for (var i = 0; i < 8; i++)
{
    var a = i / 8f * UnityEngine.Mathf.PI * 2f + R() * 0.3f; var r = 140f + R() * 20f;
    Place(ve + "SM_Env_Mountain_0" + (1 + rng.Next(3)) + ".prefab", UnityEngine.Mathf.Cos(a) * r, UnityEngine.Mathf.Sin(a) * r, 3.5f + R() * 1.5f, -1, props, 6f);
}
// snow drifts round the edge of town, rocks
for (var i = 0; i < 30; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 30f + R() * 14f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (UnityEngine.Mathf.Abs(x) < 8f) continue;
    Place(rng.Next(2) == 0 ? ve + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3)) + ".prefab" : sv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow.prefab", x, z, 0.6f + R() * 0.6f, -1, props, 0.2f);
}
// a gentle snowfall
var snowfall = Place("Assets/PolygonVikings/Prefabs/FX_Snow_Particle.prefab", 0f, 0f, 1f, 0f, props);
if (snowfall)
{
    snowfall.transform.position = origin + new UnityEngine.Vector3(0f, 14f, 0f);
    snowfall.transform.localScale = new UnityEngine.Vector3(1.8f, 1.2f, 1.8f);
    foreach (var ps in snowfall.GetComponentsInChildren<UnityEngine.ParticleSystem>()) { var em = ps.emission; em.rateOverTimeMultiplier = 500f; var mn = ps.main; mn.maxParticles = 3000; }
}
log.AppendLine("placed " + placed);

// ---- roads: cobbles round the square, gravel lanes ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var roads = roadsGo.AddComponent<SceneRoads>();
System.Collections.Generic.List<UnityEngine.Vector2> P(params float[] xz) { var l = new System.Collections.Generic.List<UnityEngine.Vector2>(); for (var i = 0; i + 1 < xz.Length; i += 2) l.Add(new UnityEngine.Vector2(xz[i], xz[i + 1])); return l; }
var ring = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 24; i++) { var a = i / 24f * UnityEngine.Mathf.PI * 2f; ring.Add(new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a) * 8.5f, UnityEngine.Mathf.Sin(a) * 8.5f)); }
roads.Roads.Add(new SceneRoads.Road { Type = 2, Width = 3.2f, Points = ring, StartRound = 0f, EndRound = 0f });
roads.Roads.Add(new SceneRoads.Road { Type = 2, Width = 3.4f, Points = P(0f, 9f, 0f, 16f), EndRound = 0f });
roads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 3f, Points = P(0f, 24f, 0f, 30f, 0f, 37f) });
roads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 2.8f, Points = P(0f, -60f, 1.2f, -42f, -0.8f, -24f, 0f, -9f) });
roads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 2.2f, Points = P(-6.5f, 5.5f, -9f, 9f, -11f, 11.5f) });
roads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 2.2f, Points = P(6.5f, -5.5f, 10f, -11f, 12.5f, -15.5f) });
roads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 2.2f, Points = P(-6.5f, -5.5f, -10f, -12f, -13f, -17.5f) });
roads.Ground = 1 << 0;

// ---- the people ----
var allNpcs = UnityEngine.Object.FindObjectsByType<IslandNpc>(UnityEngine.FindObjectsInactive.Include);
var template = allNpcs.FirstOrDefault(n => n.Key == "cook" && n.gameObject.scene == main) ?? allNpcs.FirstOrDefault(n => n.gameObject.scene == main);
void Person(string key, string look, float x, float z, float faceX, float faceZ, float scale)
{
    if (!template) { log.AppendLine("no npc template"); return; }
    var go = UnityEngine.Object.Instantiate(template.gameObject);
    go.name = key;
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    go.transform.SetParent(people, false);
    go.transform.position = OnGround(x, z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, Facing(x, z, faceX, faceZ), 0f);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    go.GetComponent<IslandNpc>().Key = key;
    var found = false;
    foreach (UnityEngine.Transform child in go.transform)
    {
        if (!child.name.StartsWith("SM_Chr_")) continue;
        child.gameObject.SetActive(child.name == look);
        found |= child.name == look;
    }
    if (!found) log.AppendLine("no look " + look + " for " + key);
}
Person("hilda", "SM_Chr_Blacksmith_Female_01", fx0 + 2f, fz0 - 2f, 0f, 0f, 1.05f);
Person("tove", "SM_Chr_Hermit_01", pond.x - 4.6f, pond.y + 2.5f, pond.x, pond.y, 1f);
Person("ylva", "SM_Chr_Peasant_Female_01", 4.5f, -4f, 0f, -20f, 0.68f);
Person("gunnhild", "SM_Chr_Soldier_Female_01", 1.8f, 14.8f, 0f, 0f, 1.14f);

// ---- arrival and strolls ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(0f, -32f);
town.SpawnPoint = spawn.transform;
var strolls = new UnityEngine.GameObject("Strolls").transform; strolls.SetParent(root.transform, false);
town.Strolls = strolls;
var strollSpots = new[] { (5f, -6f), (-5f, 6f), (-6f, -5f), (6f, 5f), (-4f, 12f), (4f, 12f), (-9f, 8f), (10f, -12f), (11f, -18f), (-11f, -14f), (8f, 9f), (-14f, 4f), (14f, 4f), (0f, -16f), (0f, 30f), (-8f, -24f) };
foreach (var (x, z) in strollSpots) { var s = new UnityEngine.GameObject("Stroll"); s.transform.SetParent(strolls, false); s.transform.position = OnGround(x, z); }

// ---- navmesh ----
var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = 25556799;
surface.BuildNavMesh();
var navPath = dataFolder + "/NavMesh.asset";
if (surface.navMeshData) { UnityEditor.AssetDatabase.DeleteAsset(navPath); UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath); log.AppendLine("navmesh " + surface.navMeshData.sourceBounds); }
foreach (var (x, z) in new[] { (5f, -6f), (fx0 + 2f, fz0 - 3f), (pond.x - 4f, pond.y + 4f), (1.8f, 13.5f), (restC.x + 3f, restC.y + 3f), (0f, 30f) })
{
    var path = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.SamplePosition(OnGround(0f, -32f), out var a, 3f, -1);
    UnityEngine.AI.NavMesh.SamplePosition(OnGround(x, z), out var b, 3f, -1);
    UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, -1, path);
    log.AppendLine("path to " + x + "," + z + ": " + path.status);
}

var scenePath = folder + "/Tower_Rimeholt.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-rimeholt-build.txt"), log.ToString());
return null;
