// Builds Assets/Scenes/Tower/Tower_Mossgate.unity: Mossgate, the town at the tower's foot (docs/tower-design.md,
// section 4). A mossy hamlet in a forest clearing: a cobbled square with a well, the Sleepy Stump inn, a few houses,
// Bramble's herb hut and garden, and at the north end the tower's great gate in a fortress wall with the tower rising
// behind it, a mill pond, and a campfire to rest at (!tower rest). Its four people (IslandNpc), stroll spots, a work place
// for every skill (TowerTown.Work: anvils, the wardens' dummies, targets, ore, trees, mushrooms, a vegetable patch, a
// spit, cauldrons, the pond's banks, a rowing boat, a bunny hutch; docs/tower-design.md section 16), lanes from the
// square to every door and work place drawn with the village road tool's roads (SceneRoads: cobbles round the square,
// mossy flagstones up to the gate, dirt lanes), its own navmesh. At (600, 0, -6000), beside whatever floor is loaded.
// Also makes Resources/Roads/Road_4 (mossy flagstones) when it is missing. (The resting place was moss-rest.cs.)
// Writes %TEMP%/tower-mossgate-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(600f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_Mossgate";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Mossgate");
string L(string n) => "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Terrain_Meadow_" + n + ".terrainlayer";

// ---- the mossy flagstone road type (4), made from the forest's footpath and moss like the village's from its island ----
var road4 = "Assets/Village/Resources/Roads/Road_4.mat";
{
    var basis = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Village/Resources/Roads/Road_2.mat");
    var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(L("Footpath_Tile_01"));
    var moss = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(L("Moss_01"));
    var m = new UnityEngine.Material(basis) { name = "Road_4" };
    if (tile) { m.SetTexture("_BaseMap", tile.diffuseTexture); if (tile.normalMapTexture) m.SetTexture("_BumpMap", tile.normalMapTexture); }
    if (moss) { m.SetTexture("_EdgeMap", moss.diffuseTexture); if (moss.normalMapTexture) m.SetTexture("_EdgeBumpMap", moss.normalMapTexture); }
    m.SetFloat("_Tile", 3f); m.SetFloat("_EdgeTile", 4f); m.SetFloat("_Shoulder", 0.5f); m.SetFloat("_ShoulderSoftness", 0.14f);
    m.SetFloat("_Fade", 0.64f); m.SetFloat("_FadeWidth", 0.2f); m.SetFloat("_Ragged", 0.24f); m.SetFloat("_HeightBlend", 0.55f);
    UnityEditor.AssetDatabase.DeleteAsset(road4);
    UnityEditor.AssetDatabase.CreateAsset(m, road4);
    log.AppendLine("made " + road4 + (tile ? "" : " (no footpath layer)"));
}

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var root = new UnityEngine.GameObject("Tower Town - Mossgate");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var town = root.AddComponent<TowerTown>();
town.Key = "mossgate";
town.Radius = 95f;
town.SkyAmbient = new UnityEngine.Color(0.62f, 0.66f, 0.58f);
town.EquatorAmbient = new UnityEngine.Color(0.48f, 0.46f, 0.36f);
town.GroundAmbient = new UnityEngine.Color(0.20f, 0.18f, 0.12f);
// clear golden afternoon air, the haze only far off (concept docs/mockups/tower/concept-mossgate.jpg)
town.FogColor = new UnityEngine.Color(0.74f, 0.76f, 0.64f);
town.FogDensity = 0.0034f;
town.SunColor = new UnityEngine.Color(1f, 0.86f, 0.64f);
town.SunIntensity = 1.35f;
town.SunAngles = new UnityEngine.Vector3(32f, -55f, 0f);

// ---- the ground ----
const float size = 220f, height = 32f, baseHeight = 2f, flatRadius = 50f;
// the mill pond: a hollow with water in it
var pond = new UnityEngine.Vector2(29f, -30f);
const float pondRadius = 6f, waterLevel = -0.45f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 13.1f, z * s + 41.9f);
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var h = (Noise(x, z, 0.03f) - 0.5f) * 0.6f;
    var flat = UnityEngine.Mathf.InverseLerp(30f, 18f, r);
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    var rim = UnityEngine.Mathf.InverseLerp(flatRadius, 95f, r); rim = rim * rim * (3f - 2f * rim);
    // the hills rise higher behind the tower
    h += rim * (12f + Noise(x, z, 0.05f) * 8f + UnityEngine.Mathf.Max(0f, z) * 0.12f);
    var dp = (new UnityEngine.Vector2(x, z) - pond).magnitude;
    h = UnityEngine.Mathf.Lerp(h, -1.4f, UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.InverseLerp(pondRadius + 2.5f, pondRadius - 1.5f, dp)));
    return h;
}
var res = 257;
var td = new UnityEngine.TerrainData();
td.heightmapResolution = res;
td.size = new UnityEngine.Vector3(size, height, size);
var tdPath = dataFolder + "/Terrain.asset";
UnityEditor.AssetDatabase.DeleteAsset(tdPath);
UnityEditor.AssetDatabase.CreateAsset(td, tdPath);
var heights = new float[res, res];
for (var zi = 0; zi < res; zi++) for (var xi = 0; xi < res; xi++)
{
    var x = xi / (float)(res - 1) * size - size / 2f; var z = zi / (float)(res - 1) * size - size / 2f;
    heights[zi, xi] = UnityEngine.Mathf.Clamp01((GroundAt(x, z) + baseHeight) / height);
}
td.SetHeights(0, 0, heights);
var layerNames = new[] { "Grass_01", "Grass_Clover_01", "Moss_01", "Dirt_01", "Rock_Moss_01", "Grass_Flowers_01" };
var layers = layerNames.Select(n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(L(n))).ToArray();
td.terrainLayers = layers.Where(l => l).ToArray();
int Idx(string n) => System.Array.IndexOf(td.terrainLayers, layers[System.Array.IndexOf(layerNames, n)]);
var amRes = 256; td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, td.terrainLayers.Length];
for (var zi = 0; zi < amRes; zi++) for (var xi = 0; xi < amRes; xi++)
{
    var x = (xi + 0.5f) / amRes * size - size / 2f; var z = (zi + 0.5f) / amRes * size - size / 2f;
    var w = new float[td.terrainLayers.Length];
    var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
    w[Idx("Grass_01")] = 1f;
    w[Idx("Grass_Clover_01")] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.07f) - 0.45f) * 3f);
    w[Idx("Moss_01")] = UnityEngine.Mathf.Clamp01((Noise(x + 70, z, 0.05f) - 0.5f) * 4f);
    w[Idx("Grass_Flowers_01")] = UnityEngine.Mathf.Clamp01((Noise(x, z + 90, 0.09f) - 0.7f) * 5f);
    w[Idx("Rock_Moss_01")] = UnityEngine.Mathf.Clamp01((steep - 22f) / 12f) * 2f;
    w[Idx("Dirt_01")] = UnityEngine.Mathf.Clamp01((pondRadius + 2.2f - (new UnityEngine.Vector2(x, z) - pond).magnitude) / 1.5f) * 4f;
    var sum = w.Sum(); for (var k = 0; k < w.Length; k++) maps[zi, xi, k] = w[k] / sum;
}
td.SetAlphamaps(0, 0, maps);
UnityEditor.EditorUtility.SetDirty(td); UnityEditor.AssetDatabase.SaveAssets();
var terrainGo = UnityEngine.Terrain.CreateTerrainGameObject(td);
terrainGo.name = "Terrain";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(terrainGo, scene);
terrainGo.transform.SetParent(root.transform, false);
terrainGo.transform.localPosition = new UnityEngine.Vector3(-size / 2f, -baseHeight, -size / 2f);
var terrain = terrainGo.GetComponent<UnityEngine.Terrain>();
terrain.drawInstanced = true; terrain.heightmapPixelError = 4f; terrain.basemapDistance = 400f;
UnityEngine.Vector3 OnGround(float x, float z) => origin + new UnityEngine.Vector3(x, GroundAt(x, z), z);

// ---- placing ----
var rng = new System.Random(5);
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
UnityEngine.Vector2 V(float x, float z) => new UnityEngine.Vector2(x, z);
// an invisible box over a building's bounds, so the navmesh goes round it
var blockers = new UnityEngine.GameObject("Blockers").transform; blockers.SetParent(root.transform, false);
UnityEngine.Bounds BoundsOf(UnityEngine.GameObject go)
{
    var rs = go.GetComponentsInChildren<UnityEngine.MeshRenderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    return b;
}
void Block(UnityEngine.GameObject go, float shrink = 0.85f)
{
    if (!go) return;
    var rs = go.GetComponentsInChildren<UnityEngine.MeshRenderer>(); if (rs.Length == 0) return;
    var b = BoundsOf(go);
    var c = new UnityEngine.GameObject("Block " + go.name); c.transform.SetParent(blockers, false);
    c.transform.position = new UnityEngine.Vector3(b.center.x, b.min.y + 1.5f, b.center.z);
    var bc = c.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(b.size.x * shrink, 3f, b.size.z * shrink);
    c.AddComponent<GrassKeepOut>(); // and no grass through the floor
}
void NoCollide(UnityEngine.GameObject go) { if (go) foreach (var col in go.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(col); }
// a seat on a log bench (the bench runs along its z, so it is turned a quarter): the stand sits on top of it, across it toward what the sitter
// watches; the sitting pose has the hips 0.7 m above the feet
void Seat(UnityEngine.Transform group, string name, string bench, float x, float z, float faceX, float faceZ, float scale, UnityEngine.Transform props_)
{
    var yaw = Facing(x, z, faceX, faceZ);
    var go = Place(bench, x, z, scale, yaw + 90f, props_, 0.1f);
    var top = go ? BoundsOf(go).max.y : OnGround(x, z).y + 0.6f;
    NoCollide(go); // walked up to, not round
    var fwd = UnityEngine.Quaternion.Euler(0f, yaw, 0f) * UnityEngine.Vector3.forward;
    var s = new UnityEngine.GameObject(name).transform; s.SetParent(group, false);
    var p = OnGround(x, z) + fwd * 0.12f; p.y = top - 0.7f;
    s.SetPositionAndRotation(p, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
}
// a small round blocker (a tree trunk, a dummy, a cauldron) the navmesh goes round
void Post(float x, float z, float radius)
{
    var c = new UnityEngine.GameObject("Block post"); c.transform.SetParent(blockers, false);
    c.transform.position = OnGround(x, z) + UnityEngine.Vector3.up * 1.5f;
    var cc = c.AddComponent<UnityEngine.CapsuleCollider>(); cc.radius = radius; cc.height = 3f;
}
// the point just outside a building's door, on the side toward the square
UnityEngine.Vector2 DoorOf(UnityEngine.GameObject go, float extra = 0.8f)
{
    var b = BoundsOf(go);
    var c = V(b.center.x - origin.x, b.center.z - origin.z);
    var dir = (-c).normalized;
    var t = UnityEngine.Mathf.Min(b.extents.x / UnityEngine.Mathf.Max(0.01f, UnityEngine.Mathf.Abs(dir.x)), b.extents.z / UnityEngine.Mathf.Max(0.01f, UnityEngine.Mathf.Abs(dir.y)));
    return c + dir * (t * 0.85f + extra);
}
var buildings = new UnityEngine.GameObject("Buildings").transform; buildings.SetParent(root.transform, false);
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var foliage = new UnityEngine.GameObject("Foliage").transform; foliage.SetParent(root.transform, false);
var people = new UnityEngine.GameObject("People").transform; people.SetParent(root.transform, false);
var lights = new UnityEngine.GameObject("Lights").transform; lights.SetParent(root.transform, false);
var fvb = "Assets/Synty/PolygonFantasyVillage/Prefabs/Buildings/Preset/";
var fvp = "Assets/Synty/PolygonFantasyVillage/Prefabs/Props/";
var fve = "Assets/Synty/PolygonFantasyVillage/Prefabs/Environment/";
var dfb = "Assets/Synty/PolygonDarkFortress/Prefabs/Buildings/";
var dfe = "Assets/Synty/PolygonDarkFortress/Prefabs/Environment/";
var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";
var fk = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
var vk = "Assets/PolygonVikings/Prefabs/";
var vr = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
var dr = "Assets/PolygonDungeonRealms/Prefabs/";
UnityEngine.Light Lamp(float x, float y, float z, UnityEngine.Color c, float range, float intensity)
{
    var l = new UnityEngine.GameObject("Light"); l.transform.SetParent(lights, false); l.transform.position = OnGround(x, z) + UnityEngine.Vector3.up * y;
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = c; light.range = range; light.intensity = intensity; light.shadows = UnityEngine.LightShadows.None;
    return light;
}
var warm = new UnityEngine.Color(1f, 0.62f, 0.30f);
// a creature for a pen: one of MainWorld's monsters as it looks, with nothing but its looks and animator kept
var enemiesAll = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include);
UnityEngine.GameObject Critter(string model, float x, float z, float yaw, float scaleMul, UnityEngine.Transform parent)
{
    var source = enemiesAll.FirstOrDefault(e => e.gameObject.scene == main && e.Definition && e.Definition.Definition != null && e.Definition.Definition.Id == model && !e.IsRaidBoss && !e.IsDungeonBoss);
    if (!source) { log.AppendLine("no enemy for " + model); return null; }
    var go = UnityEngine.Object.Instantiate(source.gameObject);
    go.name = model + " (pen)";
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    go.transform.SetParent(parent, true);
    go.transform.SetPositionAndRotation(OnGround(x, z), UnityEngine.Quaternion.Euler(0f, yaw, 0f) * source.transform.rotation);
    go.transform.localScale = source.transform.lossyScale * scaleMul;
    for (var pass = 0; pass < 5; pass++)
        foreach (var c in go.GetComponentsInChildren<UnityEngine.Component>(true))
        {
            if (!c || c is UnityEngine.Transform || c is UnityEngine.MeshFilter || c is UnityEngine.Animator || c is UnityEngine.LODGroup) continue;
            if (c is UnityEngine.Renderer && !(c is UnityEngine.ParticleSystemRenderer)) continue;
            UnityEngine.Object.DestroyImmediate(c);
        }
    go.SetActive(true);
    return go;
}

// places nothing else may be scattered on (work places, the pond, the resting place), and the lanes
var zones = new System.Collections.Generic.List<(UnityEngine.Vector2 c, float r)>();
void Zone(float x, float z, float r) => zones.Add((V(x, z), r));
var lanes = new System.Collections.Generic.List<SceneRoads.Road>();
void Lane(int type, float width, params UnityEngine.Vector2[] pts) => lanes.Add(new SceneRoads.Road { Type = type, Width = width, Points = pts.ToList() });
const float ringR = 7f;
UnityEngine.Vector2 OnRing(UnityEngine.Vector2 toward) => toward.normalized * ringR;
bool OnLane(float x, float z, float margin)
{
    var p = V(x, z);
    foreach (var road in lanes)
        for (var k = 0; k + 1 < road.Points.Count; k++)
        {
            var a = road.Points[k]; var ab = road.Points[k + 1] - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p - a, ab) / UnityEngine.Mathf.Max(0.001f, ab.sqrMagnitude));
            if ((a + ab * t - p).magnitude < road.Width * 0.5f + margin) return true;
        }
    return false;
}
bool Free(float x, float z, float margin = 1f)
{
    var p = V(x, z);
    foreach (var (c, r) in zones) if ((p - c).magnitude < r) return false;
    foreach (var road in lanes)
        for (var k = 0; k + 1 < road.Points.Count; k++)
        {
            var a = road.Points[k]; var ab = road.Points[k + 1] - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p - a, ab) / UnityEngine.Mathf.Max(0.001f, ab.sqrMagnitude));
            if ((a + ab * t - p).magnitude < road.Width * 0.5f + margin) return false;
        }
    return p.magnitude > ringR + 1.5f + margin || p.magnitude < ringR - 2.5f;
}

// the square, the inn, houses and the herb hut; the village's preset buildings have their pivot off to one side, so each
// is moved until the middle of what it looks like stands where it is meant to
UnityEngine.GameObject Building(string name, float x, float z, float scale, float faceX, float faceZ)
{
    var go = Place(fvb + name + ".prefab", x, z, scale, Facing(x, z, faceX, faceZ), buildings);
    if (!go) return null;
    var b = BoundsOf(go);
    go.transform.position += new UnityEngine.Vector3(origin.x + x - b.center.x, 0f, origin.z + z - b.center.z);
    go.transform.position = new UnityEngine.Vector3(go.transform.position.x, OnGround(x, z).y, go.transform.position.z);
    Block(go);
    return go;
}
Place(fvp + "SM_Prop_Well_01.prefab", 0f, 0f, 1.2f, 20f, props);
Post(0f, 0f, 1.3f);
var inn = Building("Bld_Preset_Tavern_01", -22f, 10f, 1f, 0f, 0f);
var houseList = new[] { ("Bld_Preset_House_01", 19f, 12f), ("Bld_Preset_House_03", 22f, -9f), ("Bld_Preset_House_02", -22f, -13f), ("Bld_Preset_House_04", 31f, 25f) };
var homes = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (var (h, x, z) in houseList) homes.Add(Building(h, x, z, 1f, 0f, 0f));
var smithy = Building("Bld_Preset_Smithy_01", -31f, 31.5f, 1f, 0f, 0f);
var hut = Building("Bld_Preset_Shed_02", 13f, -27f, 1.1f, 0f, -10f);
// Bramble's garden: shelves, pots, potions, flowers
Place(fvp + "SM_Prop_Garden_Shelf_01.prefab", 19f, -24f, 1f, Facing(19f, -24f, 0f, -10f), props);
Place(fvp + "SM_Prop_Table_03.prefab", 8f, -22f, 1f, 30f, props);
for (var i = 0; i < 4; i++) Place(fvp + "SM_Prop_Potion_0" + (1 + i % 2) + ".prefab", 8f + (R() - 0.5f) * 1.2f, -22f + (R() - 0.5f) * 0.8f, 1f, -1, props).transform.position += UnityEngine.Vector3.up * 0.95f;
for (var i = 0; i < 10; i++) NoCollide(Place(fvp + "SM_Prop_Pot_0" + (1 + rng.Next(5)) + ".prefab", 6f + R() * 12f, -31.5f + R() * 2.5f, 1f, -1, props));
for (var i = 0; i < 18; i++) Place(pnb + "SM_Env_Wildflowers_01.prefab", 5f + R() * 14f, -34f + R() * 5f, 1f + R() * 0.4f, -1, foliage);
Zone(13f, -29f, 7f);
// the inn's front: benches, tables, barrels, a sign
var innDoor = DoorOf(inn, 0.6f);
var innOut = (-innDoor).normalized; var innAcross = V(innOut.y, -innOut.x);
UnityEngine.Vector2 AtInn(float side, float out_) => innDoor + innAcross * side + innOut * out_;
{ var p = AtInn(-4.5f, 1.8f); Place(fvp + "SM_Prop_Picnic_Table_01.prefab", p.x, p.y, 1f, Facing(p.x, p.y, 0f, 0f), props); Zone(p.x, p.y, 2f); }
{ var p = AtInn(4.8f, 1.6f); Place(fvp + "SM_Prop_Picnic_Table_01.prefab", p.x, p.y, 1f, Facing(p.x, p.y, 0f, 0f) + 20f, props); Zone(p.x, p.y, 2f); }
{ var p = AtInn(2.2f, 0.4f); Place(fvp + "SM_Prop_Sign_0" + (1 + rng.Next(9)) + ".prefab", p.x, p.y, 1.2f, Facing(p.x, p.y, 0f, 0f), props); }
for (var i = 0; i < 5; i++) Place(fvp + (i % 2 == 0 ? "SM_Prop_Barrel_01" : "SM_Prop_Wine_Barrel_01") + ".prefab", -15f + R() * 2f, 17f + R() * 3f, 1f, -1, props);
// benches and lanterns round the square
for (var i = 0; i < 6; i++)
{
    var a = i / 6f * UnityEngine.Mathf.PI * 2f + 0.3f;
    var x = UnityEngine.Mathf.Cos(a) * 10.5f; var z = UnityEngine.Mathf.Sin(a) * 10.5f;
    if (z > 7f && UnityEngine.Mathf.Abs(x) < 4f) continue; // the way to the gate
    Place(fvp + (i % 2 == 0 ? "SM_Prop_Bench_01" : "SM_Prop_Lantern_0" + (1 + rng.Next(3))) + ".prefab", x, z, 1.1f, Facing(x, z, 0f, 0f), props);
}

// the tower's gate at the north end: a fortress wall with its gate, corner towers, the tower rising behind
var gateZ = 40f;
float WidthOf(string path, float scale)
{
    var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path); if (!pf) return 8f;
    var rs = pf.GetComponentsInChildren<UnityEngine.MeshRenderer>(); if (rs.Length == 0) return 8f;
    var mn = float.MaxValue; var mx = float.MinValue;
    foreach (var r in rs) { var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (!mf || !mf.sharedMesh) continue; var bb = mf.sharedMesh.bounds; var p0 = r.transform.TransformPoint(bb.min); var p1 = r.transform.TransformPoint(bb.max); mn = UnityEngine.Mathf.Min(mn, UnityEngine.Mathf.Min(p0.x, p1.x)); mx = UnityEngine.Mathf.Max(mx, UnityEngine.Mathf.Max(p0.x, p1.x)); }
    return (mx - mn) * scale;
}
var ws = 1.4f;
var gatePath = dfb + "SM_Bld_Wall_L_Gate_01.prefab";
var gateW = WidthOf(gatePath, ws);
var gate = Place(gatePath, 0f, gateZ, ws, 180f, buildings, 0.3f);
// centre the gate on the road whatever its pivot
{ var rs = gate.GetComponentsInChildren<UnityEngine.MeshRenderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); gate.transform.position += new UnityEngine.Vector3(origin.x - bb.center.x, 0f, 0f); }
var wallPieces = new[] { "SM_Bld_Wall_L_01", "SM_Bld_Wall_L_02", "SM_Bld_Wall_L_Damaged_01", "SM_Bld_Wall_L_03", "SM_Bld_Wall_L_Damaged_03" };
var endX = 0f;
foreach (var side in new[] { -1f, 1f })
{
    var x = gateW / 2f;
    for (var k = 0; k < 4; k++)
    {
        var piece = dfb + wallPieces[(k + (side > 0 ? 2 : 0)) % wallPieces.Length] + ".prefab";
        var w = WidthOf(piece, ws);
        var go = Place(piece, side * (x + w / 2f), gateZ + k * 0.8f, ws, 180f, buildings, 0.4f);
        if (go) { var rs = go.GetComponentsInChildren<UnityEngine.MeshRenderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); go.transform.position += new UnityEngine.Vector3(origin.x + side * (x + w / 2f) - bb.center.x, 0f, 0f); }
        x += w;
    }
    endX = x;
    Place(dfb + "SM_Bld_Corner_Tower_0" + (side < 0 ? 1 : 2) + ".prefab", side * (x + 2f), gateZ + 3f, 1.6f, 180f, buildings, 0.3f);
}
log.AppendLine("gate " + gateW.ToString("0.0") + " m, wall to " + endX.ToString("0.0") + " m each side");
// the tower itself, far bigger than anything in the world, rising behind the wall into the haze
var tower = Place(dfb + "SM_Bld_Corner_Tower_03.prefab", 0f, gateZ + 42f, 9f, 180f, buildings, 4f);
for (var i = 0; i < 10; i++) Place(fve + "SM_Env_Vine_Large_0" + (1 + rng.Next(3)) + ".prefab", (R() - 0.5f) * 36f, gateZ + 0.5f, 1.6f, 180f, props).transform.position += UnityEngine.Vector3.up * (2f + R() * 6f);
var gateBlock = new UnityEngine.GameObject("Block gate wall"); gateBlock.transform.SetParent(blockers, false);
gateBlock.transform.position = origin + new UnityEngine.Vector3(0f, 1.5f, gateZ + 1.5f);
gateBlock.AddComponent<UnityEngine.BoxCollider>().size = new UnityEngine.Vector3(endX * 2f + 8f, 3f, 4f);
Zone(0f, gateZ + 2f, 6f);
// lanterns at the gate
Place("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab", -5f, gateZ - 4f, 1.4f, 0f, props);
Place("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab", 5f, gateZ - 4f, 1.4f, 0f, props);

// the mill pond, south-east: water in a hollow, reeds, a rowing boat
// a plain glossy surface (the nature pack's water shader is not made for this pipeline, as in the marsh); the banks
// hide its square edges
{
    var water = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
    water.name = "Pond Water";
    UnityEngine.Object.DestroyImmediate(water.GetComponent<UnityEngine.Collider>());
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(water, scene);
    water.transform.SetParent(props, false);
    water.transform.position = origin + new UnityEngine.Vector3(pond.x, waterLevel, pond.y);
    water.transform.localScale = new UnityEngine.Vector3((pondRadius + 3f) * 0.2f, 1f, (pondRadius + 3f) * 0.2f);
    var waterMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")) { name = "Pond Water" };
    waterMat.SetColor("_BaseColor", new UnityEngine.Color(0.24f, 0.36f, 0.33f, 1f));
    waterMat.SetFloat("_Smoothness", 0.93f);
    waterMat.SetFloat("_Metallic", 0.1f);
    UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/Water.mat");
    UnityEditor.AssetDatabase.CreateAsset(waterMat, dataFolder + "/Water.mat");
    var wr = water.GetComponent<UnityEngine.MeshRenderer>(); wr.sharedMaterial = waterMat; wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
}
Post(pond.x, pond.y, pondRadius + 0.8f);
Zone(pond.x, pond.y, pondRadius + 3.5f);

// ---- the resting place (!tower rest): a campfire south-west of the square, cushions to meditate on, rugs to sleep on ----
var restC = V(-10f, -24f);
Zone(restC.x, restC.y, 7f);
var rest = new UnityEngine.GameObject("Resting Place").transform; rest.SetParent(root.transform, false);
var restSpots = new UnityEngine.GameObject("Rest Spots").transform; restSpots.SetParent(rest, false);
town.RestSpots = restSpots;
{
    var pit = Place(vr + "SM_Prop_Fire_Pit_01.prefab", restC.x, restC.y, 1f, 0f, rest); NoCollide(pit);
    var top = pit ? BoundsOf(pit).max.y - 0.15f : OnGround(restC.x, restC.y).y + 0.3f;
    var fire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", restC.x, restC.y, 1.2f, 0f, rest);
    if (fire) { var fp = fire.transform.position; fire.transform.position = new UnityEngine.Vector3(fp.x, top, fp.z); }
    Lamp(restC.x, 1.2f, restC.y, warm, 9f, 2.2f);
    for (var i = 0; i < 6; i++)
    {
        var a = (i / 6f) * UnityEngine.Mathf.PI * 2f + 0.25f;
        var p2 = restC + V(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * 2.5f;
        var yaw = Facing(p2.x, p2.y, restC.x, restC.y);
        NoCollide(Place(fvp + "SM_Prop_Pillow_0" + (1 + i % 4) + ".prefab", p2.x, p2.y, 1.6f, yaw + 90f, rest));
        var s = new UnityEngine.GameObject("Meditate " + (i + 1)).transform; s.SetParent(restSpots, false);
        s.SetPositionAndRotation(OnGround(p2.x, p2.y) + UnityEngine.Vector3.up * 0.08f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    }
    for (var i = 0; i < 5; i++)
    {
        var a = (i / 5f) * UnityEngine.Mathf.PI * 2f + 0.9f;
        var p2 = restC + V(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * 5f;
        var yaw = Facing(p2.x, p2.y, restC.x, restC.y);
        NoCollide(Place(fvp + "SM_Prop_Picnic_Rug_01.prefab", p2.x, p2.y, 1f, yaw, rest));
        var s = new UnityEngine.GameObject("Sleep " + (i + 1)).transform; s.SetParent(restSpots, false);
        s.SetPositionAndRotation(OnGround(p2.x, p2.y) + UnityEngine.Vector3.up * 0.05f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    }
    for (var i = 0; i < 3; i++)
    {
        var a = (i / 3f) * UnityEngine.Mathf.PI * 2f + 0.6f;
        var p2 = restC + V(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a)) * 3.7f;
        NoCollide(Place(vr + "SM_Prop_Log_0" + (1 + i % 2) + ".prefab", p2.x, p2.y, 1f, Facing(p2.x, p2.y, restC.x, restC.y) + 90f, rest));
    }
    NoCollide(Place(fvp + "SM_Prop_Firewood_Rack_01.prefab", restC.x - 6.5f, restC.y + 1.5f, 1f, 80f, rest));
    NoCollide(Place(fvp + "SM_Prop_Lantern_01.prefab", restC.x + 6.2f, restC.y - 1f, 1.1f, 0f, rest));
    NoCollide(Place(fvp + "SM_Prop_Lantern_02.prefab", restC.x - 3f, restC.y - 6.3f, 1.1f, 0f, rest));
}

// ---- work places (docs/tower-design.md, section 16): every skill has one; stands face the work ----
var work = new UnityEngine.GameObject("Work").transform; work.SetParent(root.transform, false);
town.Work = work;
var workProps = new UnityEngine.GameObject("Work Props").transform; workProps.SetParent(root.transform, false);
var standCount = 0;
UnityEngine.Transform WorkPlace(string kind) { var t = new UnityEngine.GameObject(kind).transform; t.SetParent(work, false); return t; }
void Stand(UnityEngine.Transform place, float x, float z, float tx, float tz)
{
    var s = new UnityEngine.GameObject("Stand " + (place.childCount + 1)).transform; s.SetParent(place, false);
    s.position = OnGround(x, z); s.rotation = UnityEngine.Quaternion.Euler(0f, Facing(x, z, tx, tz), 0f);
    standCount++;
}
// stands on an arc round a thing (compass degrees: 0 north, 90 east), facing it
void Around(UnityEngine.Transform place, float cx, float cz, float r, int n, float from, float to)
{
    for (var i = 0; i < n; i++)
    {
        var a = (n == 1 ? (from + to) * 0.5f : UnityEngine.Mathf.Lerp(from, to, i / (float)(n - 1))) * UnityEngine.Mathf.Deg2Rad;
        Stand(place, cx + UnityEngine.Mathf.Sin(a) * r, cz + UnityEngine.Mathf.Cos(a) * r, cx, cz);
    }
}
float Bearing(UnityEngine.Vector2 d) => UnityEngine.Mathf.Atan2(d.x, d.y) * UnityEngine.Mathf.Rad2Deg;

// Crafting: two anvils and a workbench in front of the smithy
var smithyDoor = DoorOf(smithy, 0.6f);
{
    var place = WorkPlace("Crafting");
    var o = (-smithyDoor).normalized; var s = V(o.y, -o.x); var b = Bearing(o);
    var a1 = smithyDoor + s * 2.8f + o * 2f; var a2 = smithyDoor - s * 2.8f + o * 2f; var wb = smithyDoor + s * 5.6f + o * 0.6f;
    Place(vk + "Props/SM_Prop_Anvil_Stump_01.prefab", a1.x, a1.y, 1.2f, b + 30f, workProps);
    Place(vk + "Props/SM_Prop_Anvil_Stump_01.prefab", a2.x, a2.y, 1.2f, b - 20f, workProps);
    Place(fk + "Furniture/SM_Prop_Workbench_01.prefab", wb.x, wb.y, 1f, b + 180f, workProps);
    Around(place, a1.x, a1.y, 1.3f, 2, b - 40f, b + 40f);
    Around(place, a2.x, a2.y, 1.3f, 2, b - 40f, b + 40f);
    Around(place, wb.x, wb.y, 1.4f, 2, b - 30f, b + 30f);
    Post(a1.x, a1.y, 0.5f); Post(a2.x, a2.y, 0.5f); Post(wb.x, wb.y, 0.8f);
    var wp = smithyDoor - s * 5.5f + o * 0.4f;
    Place(fvp + "SM_Prop_Wood_Pile_01.prefab", wp.x, wp.y, 1f, b, workProps);
    Zone(smithyDoor.x + o.x * 1.5f, smithyDoor.y + o.y * 1.5f, 6.5f);
}
// Melee (attack, strength, defense): the wardens' straw dummies west of the gate road; Healing watches over them
{
    var place = WorkPlace("Melee");
    foreach (var x in new[] { -9.5f, -13.5f, -17.5f })
    {
        Place(fk + "BattleGround/SM_Prop_Dummy_Armour_0" + (x < -15f ? 2 : 1) + ".prefab", x, 32f, 1.1f, 180f, workProps);
        Stand(place, x - 0.7f, 30.2f, x, 32f); Stand(place, x + 0.7f, 30.2f, x, 32f);
        Post(x, 32f, 0.45f);
    }
    Place(fk + "BattleGround/SM_Prop_Weapon_Rack_01.prefab", -21.5f, 32.5f, 1f, 90f, workProps);
    var heal = WorkPlace("Healing");
    foreach (var x in new[] { -11.5f, -15.5f, -19.5f }) Stand(heal, x, 26.5f, x, 31f);
    Zone(-13.5f, 29.5f, 7f);
}
// Ranged and Magic: targets east of the gate road, shot at from the south
{
    var ranged = WorkPlace("Ranged");
    var magic = WorkPlace("Magic");
    var targets = new[] { (11f, 34f), (15f, 34f), (19.5f, 33.5f) };
    for (var i = 0; i < targets.Length; i++)
    {
        var (tx, tz) = targets[i];
        Place(fk + "BattleGround/SM_Prop_Target_0" + (1 + i) + ".prefab", tx, tz, 1.1f, 180f + (i - 1) * 6f, workProps);
        Post(tx, tz, 0.5f);
        var into = i == 2 ? magic : ranged;
        Stand(into, tx - 0.8f, tz - 9f, tx, tz); Stand(into, tx + 0.8f, tz - 9.2f, tx, tz);
    }
    Stand(magic, 22f, 24.4f, 19.5f, 33.5f);
    Zone(15f, 29f, 7.5f);
}
// Mining: mossy rocks with ore at the east edge
{
    var place = WorkPlace("Mining");
    foreach (var (rx, rz, s) in new[] { (38.5f, -3f, 1.5f), (36f, 8.5f, 1.3f) })
    {
        var rock = Place(pnb + "SM_Env_Rock_0" + (s > 1.4f ? 5 : 3) + ".prefab", rx, rz, s, -1, workProps, 0.2f);
        // stands just outside the rock however big it came out; a round blocker instead of its own collider
        var rr = rock ? UnityEngine.Mathf.Max(BoundsOf(rock).extents.x, BoundsOf(rock).extents.z) : 1.6f;
        NoCollide(rock);
        NoCollide(Place(dr + "Props/SM_Prop_Ore_Insert_Large_01.prefab", rx - 1.1f, rz - 0.4f, 1.2f, 250f, workProps, 0.1f));
        Around(place, rx, rz, rr + 0.8f, 3, 220f, 320f);
        Post(rx, rz, rr * 0.75f);
        Zone(rx - 1f, rz, 4.5f);
    }
}
// Woodcutting: two big trees at the clearing's west edge, a chopping block and a wood pile
{
    var place = WorkPlace("Woodcutting");
    foreach (var (tx, tz) in new[] { (-39f, -1f), (-37.5f, -13f) })
    {
        Place(pnb + "SM_Env_Tree_Meadow_0" + (tz < -5f ? 1 : 2) + ".prefab", tx, tz, 1.1f, -1, workProps, 0.3f);
        Around(place, tx, tz, 1.8f, 3, 40f, 140f);
        Post(tx, tz, 0.7f);
        Zone(tx + 1.5f, tz, 4.5f);
    }
    Place(fvp + "SM_Prop_Chopping_Block_01.prefab", -33.5f, -6.5f, 1f, -1, workProps);
    Place(fvp + "SM_Prop_Wood_Pile_02.prefab", -32f, -15.5f, 1f, 80f, workProps);
}
// Gathering: mushrooms and herbs under the trees, south-west
{
    var place = WorkPlace("Gathering");
    foreach (var (gx, gz) in new[] { (-30f, -25f), (-25.5f, -31f), (-33.5f, -18f) })
    {
        NoCollide(Place(pnb + "Props/SM_Prop_Mushroom_Group_0" + (2 + rng.Next(4)) + ".prefab", gx, gz, 2.2f, -1, workProps));
        Around(place, gx, gz, 1.3f, 2, 30f, 120f);
        Zone(gx, gz, 3f);
    }
    for (var i = 0; i < 8; i++) Place(pnb + "SM_Env_Wildflowers_01.prefab", -33f + R() * 7f, -32f + R() * 5f, 1f, -1, workProps);
}
// Farming: Bramble's vegetable patch: cabbages and pumpkins in rows behind a low fence, south of the square
{
    var place = WorkPlace("Farming");
    var fc = V(-15f, -36f);
    for (var row = 0; row < 3; row++)
        for (var k = 0; k < 6; k++)
            NoCollide(Place(row == 1 ? fk + "SM_Prop_Pumpkin_Plant_0" + (1 + k % 2) + ".prefab" : fk + "SM_Prop_Cabbage_0" + (1 + (k + row) % 2) + ".prefab", fc.x - 3.5f + k * 1.4f, fc.y - 1.5f + row * 1.5f, 1.1f, -1, workProps));
    // fence pieces run along their x (2.2 m at this scale): a line each side, the ends open
    for (var k = 0; k < 5; k++)
    {
        Place(vk + "Props/SM_Prop_Fence_Wood_01.prefab", fc.x - 4.4f + k * 2.15f, fc.y - 3.1f, 1.1f, 0f, workProps);
        Place(vk + "Props/SM_Prop_Fence_Wood_01.prefab", fc.x - 4.4f + k * 2.15f, fc.y + 3f, 1.1f, 0f, workProps);
    }
    foreach (var k in new[] { -3f, -1f, 1f, 3f }) Stand(place, fc.x + k, fc.y + 0.75f, fc.x + k, fc.y - 0.75f);
    foreach (var k in new[] { -2f, 2f }) Stand(place, fc.x + k, fc.y - 0.75f, fc.x + k, fc.y - 2f);
    Zone(fc.x, fc.y, 6.5f);
}
// Cooking: a spit roaster by the inn
{
    var place = WorkPlace("Cooking");
    var sp = AtInn(-5.5f, 4.2f);
    Place(fk + "BattleGround/SM_Prop_Spit_Roaster_01.prefab", sp.x, sp.y, 1f, Facing(sp.x, sp.y, 0f, 0f) + 90f, workProps);
    var fire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", sp.x, sp.y, 0.8f, 0f, workProps);
    if (fire) fire.transform.position += UnityEngine.Vector3.up * 0.1f;
    Lamp(sp.x, 1.2f, sp.y, warm, 7f, 1.6f);
    var b = Bearing((-sp).normalized);
    Around(place, sp.x, sp.y, 1.5f, 3, b - 60f, b + 60f);
    Post(sp.x, sp.y, 0.6f);
    Zone(sp.x, sp.y, 2.6f);
}
// Alchemy: Bramble's cauldrons by her table
{
    var place = WorkPlace("Alchemy");
    foreach (var (cx, cz) in new[] { (5f, -25f), (16f, -20.5f) })
    {
        Place(fk + "SM_Prop_Cauldron_0" + (cx > 10f ? 1 : 2) + ".prefab", cx, cz, 1.1f, -1, workProps);
        Lamp(cx, 1.4f, cz, new UnityEngine.Color(0.45f, 1f, 0.55f), 5f, 1.2f);
        Around(place, cx, cz, 1.3f, 3, -60f, 60f);
        Post(cx, cz, 0.55f);
        Zone(cx, cz, 2.6f);
    }
}
// Fishing: round the pond's north and west banks; Sailing: two logs by a rowing boat on the water
{
    var place = WorkPlace("Fishing");
    foreach (var bearing in new[] { 250f, 280f, 310f, 340f, 10f, 40f, 70f })
    {
        var a = bearing * UnityEngine.Mathf.Deg2Rad;
        Stand(place, pond.x + UnityEngine.Mathf.Sin(a) * (pondRadius + 2.4f), pond.y + UnityEngine.Mathf.Cos(a) * (pondRadius + 2.4f), pond.x, pond.y);
    }
    Place(fvp + "SM_Prop_Stool_01.prefab", pond.x - 5.5f, pond.y + 7.5f, 1f, -1, workProps);
    Place(fvp + "SM_Prop_Bucket_01.prefab", pond.x - 4.6f, pond.y + 8.1f, 1f, -1, workProps);
    for (var i = 0; i < 12; i++)
    {
        var a = R() * UnityEngine.Mathf.PI * 2f; if (UnityEngine.Mathf.Sin(a) < -0.2f && UnityEngine.Mathf.Cos(a) > 0f) continue;
        var rr = pondRadius + 1f + R() * 0.8f;
        NoCollide(Place(pnb + "SM_Env_Grass_Tall_Clump_01.prefab", pond.x + UnityEngine.Mathf.Sin(a) * rr, pond.y + UnityEngine.Mathf.Cos(a) * rr, 1.1f + R() * 0.4f, -1, workProps));
    }
    var sail = WorkPlace("Sailing");
    var boat = V(pond.x + 1.5f, pond.y - 1f);
    var boatGo = Place(vk + "Vehicles/SM_Veh_FishingBoat_01.prefab", boat.x, boat.y, 0.6f, 60f, workProps);
    if (boatGo) { NoCollide(boatGo); var bp = boatGo.transform.position; boatGo.transform.position = new UnityEngine.Vector3(bp.x, origin.y + waterLevel - 0.15f, bp.z); }
    foreach (var bearing in new[] { 110f, 140f })
    {
        var a = bearing * UnityEngine.Mathf.Deg2Rad;
        var lx = pond.x + UnityEngine.Mathf.Sin(a) * (pondRadius + 2.6f); var lz = pond.y + UnityEngine.Mathf.Cos(a) * (pondRadius + 2.6f);
        Seat(sail, "Stand " + (sail.childCount + 1), vk + "Props/SM_Prop_Log_01.prefab", lx, lz, boat.x, boat.y, 0.9f, workProps);
        standCount++;
    }
}
// Taming: Pip's bunny hutch (Nibbles is not in it) with a cow for company
{
    var place = WorkPlace("Taming");
    var pc = V(11f, -38f);
    // ten pieces round, each along the ring (a piece runs along its x), one left out for the way in (west)
    for (var i = 0; i < 10; i++)
    {
        if (i == 7) continue;
        var a = (i / 10f * 360f + 18f) * UnityEngine.Mathf.Deg2Rad;
        Place(vk + "Props/SM_Prop_Fence_Wood_01.prefab", pc.x + UnityEngine.Mathf.Sin(a) * 3.6f, pc.y + UnityEngine.Mathf.Cos(a) * 3.6f, 1.1f, a * UnityEngine.Mathf.Rad2Deg, workProps);
    }
    Critter("home-bunny", pc.x - 1f, pc.y + 0.8f, 200f, 1f, workProps);
    Critter("home-bunny", pc.x + 1.2f, pc.y + 1.2f, 120f, 0.9f, workProps);
    Critter("home-cow", pc.x + 0.3f, pc.y - 1f, 60f, 0.9f, workProps);
    Around(place, pc.x, pc.y, 4.4f, 4, 270f, 380f);
    Post(pc.x, pc.y, 2.8f);
    Zone(pc.x, pc.y, 6f);
}

// ---- lanes: every one leads from the square (or another lane) to a door or a work place ----
// mossy flagstones up to the gate, the dirt lane from the south where climbers arrive
Lane(4, 3.6f, V(0f, ringR), V(0.6f, 16f), V(-0.8f, 26f), V(0f, gateZ - 2f));
Lane(0, 2.6f, V(0f, -60f), V(1.5f, -40f), V(-1f, -22f), V(0f, -ringR));
// the inn's door
Lane(0, 2.2f, OnRing(innDoor), innDoor);
// off the gate road: west to the wardens' yard and on to the smithy, east to the archery range
{ var o = (-smithyDoor).normalized; Lane(0, 2.2f, V(-0.4f, 25.5f), V(-8f, 27.6f), V(-18f, 28f), smithyDoor + o * 4.5f, smithyDoor); }
Lane(0, 2.2f, V(-0.2f, 23.5f), V(8f, 23.6f), V(16f, 23f), V(22f, 22.5f));
// north-east to the house by the square
{ var d = DoorOf(homes[0]); Lane(0, 2f, OnRing(d), d); }
// east past the house to the rocks
{ var d = DoorOf(homes[1]); Lane(0, 2.2f, OnRing(V(1f, -0.15f)), V(13f, -3f), V(26f, -1f), V(33.5f, -2f)); Lane(0, 1.8f, V(26f, -1f), V(31f, 6.5f), V(33f, 8f)); Lane(0, 1.8f, V(13f, -3f), d); }
// south-east to Bramble's table, the herb hut and on to the pond
Lane(0, 2.2f, OnRing(V(0.55f, -1f)), V(6.5f, -14f), V(8.5f, -19.5f), V(14.5f, -22.5f), V(pond.x - pondRadius - 3f, pond.y + 1.5f));
// south-west to the resting place, the vegetable patch and the mushrooms
Lane(0, 2.2f, OnRing(V(-0.5f, -1f)), V(-6.5f, -14f), V(restC.x + 1.5f, restC.y + 5.5f));
Lane(0, 1.8f, V(restC.x - 5.5f, restC.y - 1f), V(-22f, -24.5f), V(-28f, -23f));
Lane(0, 1.8f, V(-6.5f, -14f), V(restC.x - 1f, restC.y + 4.5f), V(restC.x - 5.5f, restC.y - 1f));
// west to the woodcutters
Lane(0, 2.2f, OnRing(V(-1f, -0.1f)), V(-17f, -1.5f), V(-27f, -3f), V(-34f, -2.5f));
Lane(0, 1.8f, V(-27f, -3f), V(-31f, -9f), V(-34.5f, -12f));
// the vegetable patch's open east end, and the bunny hutch, off the south lane
Lane(0, 1.8f, V(-0.9f, -35.5f), V(-5f, -36.2f), V(-9.6f, -36f));
Lane(0, 1.8f, V(1f, -34f), V(5.5f, -36.5f), V(7.5f, -37.5f));

// trees round the clearing and up the hills, bushes and grass, a few rocks
var trees = new[] { "SM_Env_Tree_Meadow_01", "SM_Env_Tree_Meadow_02", "SM_Env_Tree_Birch_01", "SM_Env_Tree_Birch_02", "SM_Env_Tree_Birch_03" };
for (var i = 0; i < 170; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 44f + R() * 50f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 30f && UnityEngine.Mathf.Abs(x) < 50f && z < 80f) continue; // the fortress
    if (!Free(x, z, 2.5f)) continue;
    Place(pnb + trees[rng.Next(trees.Length)] + ".prefab", x, z, 1f + R() * 0.6f, -1, foliage, 0.3f);
}
var cover = new[] { "SM_Env_Grass_Med_Clump_01", "SM_Env_Grass_Med_Clump_02", "SM_Env_Grass_Tall_Clump_01", "SM_Env_Grass_Short_Clump_01", "SM_Env_Grass_Short_Clump_02", "SM_Env_Ground_Cover_01", "SM_Env_Ground_Cover_02", "SM_Env_Wildflowers_01" };
for (var i = 0; i < 420; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 12f + UnityEngine.Mathf.Sqrt(R()) * 70f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Free(x, z, 0.8f)) continue;
    NoCollide(Place(pnb + cover[rng.Next(cover.Length)] + ".prefab", x, z, 0.9f + R() * 0.5f, -1, foliage));
}
for (var i = 0; i < 30; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 30f + R() * 18f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 26f && UnityEngine.Mathf.Abs(x) < 24f) continue; // the gate
    if (!Free(x, z, 1.5f)) continue;
    Place(pnb + (rng.Next(2) == 0 ? "SM_Env_Bush_0" + (1 + rng.Next(3)) : "SM_Env_Rock_0" + (1 + rng.Next(6))) + ".prefab", x, z, 0.9f + R() * 0.5f, -1, foliage, 0.1f);
}
for (var i = 0; i < 10; i++)
{
    var x = (R() - 0.5f) * 60f; var z = (R() - 0.5f) * 60f;
    if (!Free(x, z, 1.2f)) continue;
    Place(fvp + "SM_Prop_Sign_Pole_0" + (1 + rng.Next(2)) + ".prefab", x, z, 1f, -1, props);
}
foreach (var group in new[] { props, buildings, workProps })
    foreach (UnityEngine.Transform t in group)
    {
        var lp = V(t.position.x - origin.x, t.position.z - origin.z);
        foreach (var road in lanes)
            for (var k = 0; k + 1 < road.Points.Count; k++)
            {
                var a = road.Points[k]; var ab = road.Points[k + 1] - a; var tt = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(lp - a, ab) / UnityEngine.Mathf.Max(0.001f, ab.sqrMagnitude));
                if ((a + ab * tt - lp).magnitude < road.Width * 0.5f) { log.AppendLine("on a lane: " + t.name + " at " + lp.ToString("F1")); goto nextProp; }
            }
        nextProp:;
    }
log.AppendLine("placed " + placed + ", work stands " + standCount);

// ---- roads, the village road tool's ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var roads = roadsGo.AddComponent<SceneRoads>();
// cobbles round the square (a closed ring: the first points again at the end)
var ring = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 16; i++) { var a = i / 16f * UnityEngine.Mathf.PI * 2f; ring.Add(new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a) * ringR, UnityEngine.Mathf.Sin(a) * ringR)); }
ring.Add(ring[1]);
roads.Roads.Add(new SceneRoads.Road { Type = 2, Width = 3.2f, Points = ring, StartRound = 0f, EndRound = 0f });
roads.Roads.AddRange(lanes);
roads.Ground = 1 << 0;

// ---- the people ----
var allNpcs = UnityEngine.Object.FindObjectsByType<IslandNpc>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
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
Person("elm", "SM_Chr_Soldier_Male_01", 3.5f, gateZ - 5f, 0f, 0f, 1f);
Person("bramble", "SM_Chr_Hermit_01", 9.5f, -20.5f, 0f, -8f, 1f);
Person("pip", "SM_Chr_Peasant_Male_01", 2.2f, 2.4f, 0f, -6f, 0.68f);
{ var m = AtInn(1.2f, 1.6f); Person("mossbeard", "SM_Chr_Bartender_01", m.x, m.y, 0f, 0f, 1.05f); }

// ---- arrival and strolls ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(0f, -30f);
town.SpawnPoint = spawn.transform;
var strolls = new UnityEngine.GameObject("Strolls").transform; strolls.SetParent(root.transform, false);
town.Strolls = strolls;
var strollSpots = new[] { (4f, -4f), (-4f, 4f), (-4f, -4f), (4.5f, 4f), (-11f, 4f), (-10f, 1f), (5f, -15f), (11f, -17f), (2f, 20f), (-2f, 30f), (12f, 7f), (-13f, -6f), (0f, -14f), (15f, -2f), (-16f, 2f) };
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
UnityEngine.AI.NavMesh.SamplePosition(OnGround(0f, -30f), out var startHit, 3f, -1);
var unreachable = 0;
foreach (var st in work.GetComponentsInChildren<UnityEngine.Transform>())
{
    if (!st.name.StartsWith("Stand")) continue;
    var path = new UnityEngine.AI.NavMeshPath();
    if (!UnityEngine.AI.NavMesh.SamplePosition(st.position, out var b, 1.5f, -1)) { unreachable++; log.AppendLine("off navmesh " + st.parent.name + "/" + st.name + " " + (st.position - origin).ToString("F1")); continue; }
    UnityEngine.AI.NavMesh.CalculatePath(startHit.position, b.position, -1, path);
    if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) { unreachable++; log.AppendLine("no path to " + st.parent.name + "/" + st.name + " " + (st.position - origin).ToString("F1")); }
}
log.AppendLine("work stands unreachable: " + unreachable);

// ---- the cutscene (TowerCutscene): the camera's way through the town, poses in order (tools/tower/tower-cutscene-preview.cs
// renders them); Arrival the first time a stream gets here, Opening on from it up the tower the first time the tower opens
var cut = new UnityEngine.GameObject("Cutscene").transform;
cut.SetParent(root.transform, false);
void Pose(UnityEngine.Transform row, int n, float x, float y, float z, float lx, float ly, float lz)
{
    var pose = new UnityEngine.GameObject("Shot " + n).transform;
    pose.SetParent(row, false);
    pose.position = origin + new UnityEngine.Vector3(x, y, z);
    pose.LookAt(origin + new UnityEngine.Vector3(lx, ly, lz));
}
{
    var row = new UnityEngine.GameObject("Arrival").transform; row.SetParent(cut, false);
    Pose(row, 1, 0f, 55f, -125f, 0f, 20f, 40f);
    Pose(row, 2, -4f, 26f, -60f, 0f, 8f, 20f);
    Pose(row, 3, 4f, 12f, -14f, 0f, 8f, 40f);
    Pose(row, 4, 6f, 7f, 10f, 0f, 14f, 60f);
}
{
    var row = new UnityEngine.GameObject("Opening").transform; row.SetParent(cut, false);
    Pose(row, 1, 12f, 4f, 18f, 0f, 32f, 82f);
    Pose(row, 2, 17f, 2.5f, 23f, -2f, 50f, 82f);
}
town.Cutscene = cut;
town.TitleColor = new UnityEngine.Color(1f, 0.84f, 0.36f);
town.TitleOutline = new UnityEngine.Color(0.25f, 0.15f, 0.02f);

var scenePath = folder + "/Tower_Mossgate.unity";
// chimney smoke (ChimneySmoke: the islands' own smoke, with its setting; VillageLife only finds chimneys when play starts)
var smokes = 0;
void Smoke(UnityEngine.Vector3 at, float strength)
{
    var s = new UnityEngine.GameObject("Chimney Smoke");
    s.transform.SetParent(root.transform, false);
    s.transform.position = at;
    s.AddComponent<ChimneySmoke>().Strength = strength;
    smokes++;
}
// the presets' own Synty smoke sits right on the flues: ours takes its place, so the town's smoke is the islands'
foreach (var go in scene.GetRootGameObjects())
    foreach (var p in go.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
    {
        if (!p.name.StartsWith("FX_Chimney_Smoke") || !p.gameObject.activeSelf) continue;
        Smoke(p.transform.position, 0.5f);
        p.gameObject.SetActive(false);
    }
log.AppendLine("chimney smokes " + smokes);

// our grass (GrassField with Assets/World/Grass/Tower Grass Profile.asset, made by tools/tower/tower-grass-profile.cs): the
// islands' blades, tufts and flowers grown round the camera on the grassy layers; its own setting, off in potato mode
{
    var grassProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<GrassProfile>("Assets/World/Grass/Tower Grass Profile.asset");
    var grassFields = 0;
    if (!grassProfile) log.AppendLine("missing tower grass profile (run tools/tower/tower-grass-profile.cs)");
    else foreach (var go in scene.GetRootGameObjects()) foreach (var t in go.GetComponentsInChildren<UnityEngine.Terrain>(true))
    {
        if (!t.TryGetComponent<GrassField>(out var gf)) gf = t.gameObject.AddComponent<GrassField>();
        gf.Profile = grassProfile;
        grassFields++;
    }
    log.AppendLine("grass fields " + grassFields);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-mossgate-build.txt"), log.ToString());
return null;
