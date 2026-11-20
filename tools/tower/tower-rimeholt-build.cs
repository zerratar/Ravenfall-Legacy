// Builds Assets/Scenes/Tower/Tower_Rimeholt.unity: Rimeholt, the tower's second town (the Frozen Vault band, reached by
// beating floor 10; docs/tower-design.md, section 4). A snowed-in hamlet on a ledge of the tower: a cobbled square round a
// stone statue, Hilda's forge (glowing), the Thawing Mead hall, snow-roofed houses, Old Tove's frozen fishing pond, a
// campfire with pelts and logs to rest at (!tower rest), watchtowers, and at the north end a gate with stairs up to the
// vault. Its four people (IslandNpc), stroll spots, a work place for every skill (TowerTown.Work: anvils, dummies,
// targets, ore, pines, berries, cabbages, a spit, cauldrons, ice holes, a frozen boat, a pen; docs/tower-design.md
// section 16), lanes from the square to every door and work place (SceneRoads: cobbles round the square, gravel lanes),
// its own navmesh. At (1200, 0, -6000), beside whatever floor is loaded (Mossgate sits at 600).
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
// a blue winter evening with warm lights in it (concept docs/mockups/tower/concept-rimeholt.jpg)
town.SkyAmbient = new UnityEngine.Color(0.48f, 0.56f, 0.74f);
town.EquatorAmbient = new UnityEngine.Color(0.40f, 0.44f, 0.56f);
town.GroundAmbient = new UnityEngine.Color(0.30f, 0.30f, 0.34f);
town.FogColor = new UnityEngine.Color(0.64f, 0.70f, 0.84f);
town.FogDensity = 0.0038f;
town.SunColor = new UnityEngine.Color(1f, 0.74f, 0.52f);
town.SunIntensity = 0.95f;
town.SunAngles = new UnityEngine.Vector3(16f, -50f, 0f);

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
UnityEngine.Vector2 V(float x, float z) => new UnityEngine.Vector2(x, z);
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
var sv = "Assets/PolygonVikings/Prefabs/Snow_Variant/";
var vp = "Assets/PolygonVikings/Prefabs/Props/";
var vr = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
var ve = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";
var vfx = "Assets/Synty/PolygonVikingRealm/Prefabs/FX/";
var fk = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
var dr = "Assets/PolygonDungeonRealms/Prefabs/";
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
UnityEngine.Light Lamp(float x, float y, float z, UnityEngine.Color c, float range, float intensity)
{
    var l = new UnityEngine.GameObject("Light"); l.transform.SetParent(lights, false); l.transform.position = OnGround(x, z) + UnityEngine.Vector3.up * y;
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = c; light.range = range; light.intensity = intensity; light.shadows = UnityEngine.LightShadows.None;
    return light;
}
var warm = new UnityEngine.Color(1f, 0.62f, 0.30f);

// places nothing else may be scattered on (work places, the pond, the resting place), and the lanes
var zones = new System.Collections.Generic.List<(UnityEngine.Vector2 c, float r)>();
void Zone(float x, float z, float r) => zones.Add((V(x, z), r));
var lanes = new System.Collections.Generic.List<SceneRoads.Road>();
void Lane(int type, float width, params UnityEngine.Vector2[] pts) => lanes.Add(new SceneRoads.Road { Type = type, Width = width, Points = pts.ToList() });
const float ringR = 8.5f;
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
    return p.magnitude > ringR + 1.5f || p.magnitude < ringR - 2.5f;
}

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
var homes = new System.Collections.Generic.List<UnityEngine.GameObject>
{
    House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", -21f, 7f, 1.55f),
    House("SM_Bld_Base_Door_02_Snow", "SM_Bld_Roof_01_Snow", -22f, -9f, 1.5f),
    House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", 22f, 9f, 1.55f),
    House("SM_Bld_Base_Door_02_Snow", "SM_Bld_Roof_01_Snow", 26f, -4f, 1.45f),
    House("SM_Bld_Base_Door_01_Snow", "SM_Bld_Roof_01_Snow", -30f, 18f, 1.4f),
};
// warm light at every door against the blue evening snow
foreach (var home in homes)
{
    if (!home) continue;
    var door = DoorOf(home, 1.2f);
    Lamp(door.x, 2.2f, door.y, warm, 7f, 1.5f);
}
// the Thawing Mead: the long hall north-east of the square, its door toward the square (the gate road stays clear)
var hallC = V(12.5f, 22f);
var hall = House("SM_Bld_Base_Long_Door_01_Snow", "SM_Bld_Roof_Long_01_Snow", hallC.x, hallC.y, 2.0f);
var toSquare = (-hallC).normalized;
var across = V(toSquare.y, -toSquare.x);
var hallDoor = DoorOf(hall, 0.6f);
UnityEngine.Vector2 AtDoor(float side, float out_) => hallDoor + across * side + toSquare * out_;
var hallYaw = Facing(hallC.x, hallC.y, 0f, 0f);
{ var p = AtDoor(3.4f, -0.2f); Place(vr + "SM_Prop_Mead_Barrel_01.prefab", p.x, p.y, 1.2f, hallYaw, props); }
{ var p = AtDoor(-3.6f, -0.3f); Place(vr + "SM_Prop_Barrel_01.prefab", p.x, p.y, 1.1f, -1, props); }
{ var p = AtDoor(-4.6f, 0.4f); Place(vr + "SM_Prop_Barrel_02.prefab", p.x, p.y, 1.1f, -1, props); }
{ var p = AtDoor(2f, -0.6f); Place(sv + "SM_Prop_Shield_Decor_01_Snow.prefab", p.x, p.y, 1.3f, hallYaw, props); }
foreach (var s in new[] { -2.2f, 2.2f }) { var p = AtDoor(s, 0f); Place(vp + "SM_Prop_TorchStick_01.prefab", p.x, p.y, 1.3f, 0f, props); }
{ var p = AtDoor(0f, 0.3f); Lamp(p.x, 2.5f, p.y, warm, 10f, 1.8f); }
// watchtowers either side of the gate, out of the hall's way
Block(Place(sv + "SM_Bld_Guard_Tower_01_Snow.prefab", -20f, 35f, 1.4f, 180f, buildings));
Block(Place(sv + "SM_Bld_Guard_Tower_01_Snow.prefab", 20f, 35f, 1.4f, 180f, buildings));

// the gate up to the vault: the village entrance with stone stairs climbing behind it, flags and torches
Place(sv + "SM_Prop_Village_Entrance_01_Snow.prefab", 0f, 36f, 2.2f, 180f, buildings);
for (var i = 0; i < 3; i++) Place(ve + "SM_Env_Stone_Stairs_0" + (1 + i % 3) + ".prefab", 0f, 41f + i * 4.5f, 2.2f, 180f, props, 0.4f);
foreach (var side in new[] { -1f, 1f })
{
    Place(sv + "SM_Prop_Flag_01_Snow.prefab", side * 5f, 34.5f, 1.6f, 180f, props);
    Place(vp + "SM_Prop_TorchStick_01.prefab", side * 3.4f, 34f, 1.4f, 0f, props);
    Lamp(side * 3.4f, 2.6f, 34f, warm, 9f, 1.6f);
}
// fences from the gate along the wall to the watchtowers (a piece runs along its x: 2.7 m at this scale)
for (var i = 0; i < 4; i++)
{
    foreach (var side in new[] { -1f, 1f })
    {
        var x = side * (7.4f + i * 2.7f);
        Place(sv + "SM_Prop_Fence_Wood_01_Snow.prefab", x, 36.6f + i * 0.15f, 1.4f, 0f, props);
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

// Tove's frozen pond: a fish rack, a stool, fish, a bucket, rocks round the edge
Place(vr + "SM_Prop_Fish_Rack_01.prefab", pond.x - 7.5f, pond.y - 4.5f, 1.1f, 60f, props);
Place(sv + "SM_Prop_Fish_Rack_02_Snow.prefab", pond.x + 7.5f, pond.y + 6.5f, 1.1f, -1, props);
Place(vr + "SM_Prop_Stool_01.prefab", pond.x - 4.6f, pond.y + 2.5f, 1f, -1, props);
Place(vr + "SM_Prop_Fish_Pile_01.prefab", pond.x - 5.2f, pond.y + 1f, 1f, -1, props);
Place(vr + "SM_Prop_Bucket_01.prefab", pond.x - 3.8f, pond.y + 3.6f, 1f, -1, props);
Zone(pond.x, pond.y, pondRadius + 3.5f);

// the square: a stone statue in the middle, benches round it
Place(sv + "SM_Stone_Statue_01_Snow.prefab", 0f, 0f, 1.5f, 180f, props);
var statueBlock = new UnityEngine.GameObject("Block statue"); statueBlock.transform.SetParent(blockers, false); statueBlock.transform.position = OnGround(0f, 0f) + UnityEngine.Vector3.up * 1.5f;
statueBlock.AddComponent<UnityEngine.CapsuleCollider>().radius = 1.4f;
for (var i = 0; i < 4; i++)
{
    var a = i / 4f * UnityEngine.Mathf.PI * 2f + 0.785f; var x = UnityEngine.Mathf.Cos(a) * 5.6f; var z = UnityEngine.Mathf.Sin(a) * 5.6f;
    Place(sv + "SM_Prop_Bench_01_Snow.prefab", x, z, 1.2f, Facing(x, z, 0f, 0f) + 90f, props);
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

// Crafting: Hilda's anvil and two more by the forge
{
    var place = WorkPlace("Crafting");
    var ax = fx0 + 1.2f; var az = fz0 + 0.3f;
    Around(place, ax, az, 1.3f, 2, 150f, 230f);
    var b = V(-15f, 10.2f); var c = V(-7.8f, 16.6f);
    Place(sv + "SM_Prop_Anvil_Stump_01_Snow.prefab", b.x, b.y, 1.2f, 70f, workProps);
    Place(vr + "SM_Prop_Wood_Pile_01.prefab", b.x - 2.2f, b.y + 1.2f, 1f, -1, workProps);
    Around(place, b.x, b.y, 1.3f, 2, 120f, 200f);
    Place(fk + "Furniture/SM_Prop_Workbench_01.prefab", c.x, c.y, 1f, Facing(c.x, c.y, 0f, 0f), workProps);
    Around(place, c.x, c.y, 1.4f, 2, 160f, 230f);
    Post(b.x, b.y, 0.5f); Post(c.x, c.y, 0.8f);
}
// Melee (attack, strength, defense): straw dummies in a row west of the gate road; Healing watches over them
{
    var place = WorkPlace("Melee");
    foreach (var x in new[] { -8.5f, -12.5f, -16.5f })
    {
        Place(fk + "BattleGround/SM_Prop_Dummy_Armour_0" + (x < -14f ? 2 : 1) + ".prefab", x, 28f, 1.1f, 180f, workProps);
        Stand(place, x - 0.7f, 26.2f, x, 28f); Stand(place, x + 0.7f, 26.2f, x, 28f);
        Post(x, 28f, 0.45f);
    }
    Place(vr + "SM_Prop_Weapon_Rack_01.prefab", -20f, 28.5f, 1.1f, 90f, workProps);
    var heal = WorkPlace("Healing");
    foreach (var x in new[] { -10.5f, -14.5f, -18.5f }) Stand(heal, x, 22.5f, x, 27f);
    Zone(-12.5f, 25f, 6.5f);
}
// Ranged and Magic: targets east of the hall, shot at from the south
{
    var ranged = WorkPlace("Ranged");
    var magic = WorkPlace("Magic");
    var targets = new[] { (24.5f, 30f), (28.5f, 29.5f), (32.5f, 28.5f) };
    for (var i = 0; i < targets.Length; i++)
    {
        var (tx, tz) = targets[i];
        Place((i == 2 ? vr + "SM_Prop_Target_02.prefab" : vr + "SM_Prop_Target_01.prefab"), tx, tz, 1.2f, 180f + (i - 1) * 6f, workProps);
        Post(tx, tz, 0.5f);
        var into = i == 2 ? magic : ranged;
        Stand(into, tx - 0.8f, tz - 9f, tx, tz); Stand(into, tx + 0.8f, tz - 9.2f, tx, tz);
    }
    Stand(magic, 34.6f, 19.6f, 32.5f, 28.5f);
    Zone(28.5f, 24.5f, 7.5f);
}
// Mining: snowy rocks with ore under the north-west slope
{
    var place = WorkPlace("Mining");
    foreach (var (rx, rz, s) in new[] { (-28f, 31f, 1.6f), (-35f, 24.5f, 1.4f) })
    {
        var rock = Place(sv + "SM_Env_Rock_0" + (s > 1.5f ? 5 : 3) + "_Snow.prefab", rx, rz, s, -1, workProps, 0.2f);
        // stands just outside the rock however big it came out; a round blocker instead of its own collider
        var rr = rock ? UnityEngine.Mathf.Max(BoundsOf(rock).extents.x, BoundsOf(rock).extents.z) : 1.6f;
        NoCollide(rock);
        NoCollide(Place(dr + "Props/SM_Prop_Ore_Insert_Large_01.prefab", rx + 0.6f, rz - 1.1f, 1.2f, 160f, workProps, 0.1f));
        Around(place, rx, rz, rr + 0.8f, 3, 130f, 230f);
        Post(rx, rz, rr * 0.75f);
        Zone(rx, rz - 1f, 4.5f);
    }
}
// Woodcutting: two big pines at the forest's west edge, stumps and a wood pile
{
    var place = WorkPlace("Woodcutting");
    foreach (var (tx, tz) in new[] { (-38f, -2f), (-36.5f, -14f) })
    {
        Place(sv + "SM_Env_Tree_Pine_Large_0" + (tz < -5f ? 2 : 1) + "_Snow.prefab", tx, tz, 1.25f, -1, workProps, 0.3f);
        Around(place, tx, tz, 1.7f, 3, 40f, 140f);
        Post(tx, tz, 0.6f);
        Zone(tx + 1.5f, tz, 4.5f);
    }
    Place(vr + "SM_Prop_Wood_Stump_01.prefab", -33.5f, -7.5f, 1.1f, -1, workProps);
    Place(vr + "SM_Prop_Wood_Pile_02.prefab", -31.5f, -15.5f, 1.1f, 80f, workProps);
}
// Gathering: berry bushes and herbs at the east edge
{
    var place = WorkPlace("Gathering");
    foreach (var (bx, bz) in new[] { (35.5f, -7f), (37f, 3f), (33f, 9.5f) })
    {
        Place(ve + "SM_Env_Bush_Berries_0" + (1 + rng.Next(4)) + ".prefab", bx, bz, 1.2f, -1, workProps);
        Around(place, bx, bz, 1.4f, 2, 230f, 310f);
        Zone(bx - 1f, bz, 3.2f);
    }
    for (var i = 0; i < 5; i++) NoCollide(Place(vr + "SM_Prop_Herb_0" + (1 + i % 2) + ".prefab", 34f + R() * 3f, -3f + R() * 6f, 1f, -1, workProps));
}
// Farming: a snowy cabbage patch with a low fence, south-west
{
    var place = WorkPlace("Farming");
    var fc = V(-29f, -27f);
    for (var row = 0; row < 3; row++)
        for (var k = 0; k < 6; k++)
            NoCollide(Place((row + k) % 3 == 0 ? fk + "SM_Prop_Cabbage_02.prefab" : vr + "SM_Prop_Cabbage_01.prefab", fc.x - 3.5f + k * 1.4f, fc.y - 1.5f + row * 1.5f, 1.1f, -1, workProps));
    // fence pieces run along their x (2.2 m at this scale): a line each side, the ends open
    for (var k = 0; k < 5; k++)
    {
        Place(sv + "SM_Prop_Fence_Wood_01_Snow.prefab", fc.x - 4.4f + k * 2.15f, fc.y - 3.1f, 1.1f, 0f, workProps);
        Place(sv + "SM_Prop_Fence_Wood_01_Snow.prefab", fc.x - 4.4f + k * 2.15f, fc.y + 3f, 1.1f, 0f, workProps);
    }
    foreach (var k in new[] { -3f, -1f, 1f, 3f }) { Stand(place, fc.x + k, fc.y + 0.75f, fc.x + k, fc.y - 0.75f); }
    foreach (var k in new[] { -2f, 2f }) { Stand(place, fc.x + k, fc.y - 0.75f, fc.x + k, fc.y - 2f); }
    Zone(fc.x, fc.y, 6.5f);
}
// Cooking: a spit over a fire at the hall's door
{
    var place = WorkPlace("Cooking");
    var sp = AtDoor(5.8f, 2.6f);
    Place(vr + "SM_Prop_Spit_01.prefab", sp.x, sp.y, 1.1f, hallYaw + 90f, workProps);
    var fire = Place("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", sp.x, sp.y, 0.8f, 0f, workProps);
    if (fire) fire.transform.position += UnityEngine.Vector3.up * 0.1f;
    Lamp(sp.x, 1.2f, sp.y, warm, 7f, 1.6f);
    Around(place, sp.x, sp.y, 1.5f, 3, 120f, 240f);
    Post(sp.x, sp.y, 0.6f);
    Zone(sp.x, sp.y, 2.6f);
}
// Alchemy: the wise woman's cauldrons by the totem, a green glow
{
    var place = WorkPlace("Alchemy");
    foreach (var (cx, cz) in new[] { (10f, -35f), (4.6f, -37.6f) })
    {
        Place(vr + "SM_Prop_Cauldron_0" + (cx > 10f ? 1 : 2) + ".prefab", cx, cz, 1.2f, -1, workProps);
        Lamp(cx, 1.4f, cz, new UnityEngine.Color(0.45f, 1f, 0.55f), 5f, 1.4f);
        Around(place, cx, cz, 1.3f, 3, -60f, 60f);
        Post(cx, cz, 0.55f);
        Zone(cx, cz, 2.6f);
    }
    NoCollide(Place(vr + "SM_Prop_Herb_01.prefab", 7.2f, -38.6f, 1.1f, -1, workProps));
    // a stall roof over them, as at the forge, with drying herbs
    var hut = Place(sv + "SM_Bld_Stall_Cover_01_Snow.prefab", 7.4f, -38.4f, 1.5f, 15f, buildings);
    Block(hut, 0.5f);
    Place(vr + "SM_Prop_Herb_02.prefab", 9.6f, -39.4f, 1.1f, -1, workProps);
}
UnityEngine.Material FrozenWaterMat()
{
    // shared by every frozen pond (Assets/World/Water/FrozenWater.shader); made once, then kept as it is tuned
    var path = "Assets/World/Water/Frozen Water.mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (m) return m;
    m = new UnityEngine.Material(UnityEngine.Shader.Find("Ravenfall/Frozen Water")) { name = "Frozen Water" };
    UnityEditor.AssetDatabase.CreateAsset(m, path);
    return m;
}
// Fishing: holes in the ice round the pond; Sailing: an old fishing boat frozen in, sailors sit on logs by it
{
    var place = WorkPlace("Fishing");
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(dataFolder + "/IceHole.mat")) UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/IceHole.mat");
    var iceHoles = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var bearing in new[] { 10f, 45f, 80f, 115f, 150f, 245f })
    {
        var a = bearing * UnityEngine.Mathf.Deg2Rad;
        var hx = pond.x + UnityEngine.Mathf.Sin(a) * 4.4f; var hz = pond.y + UnityEngine.Mathf.Cos(a) * 4.4f;
        // the hole is cut in the pond's ice (FrozenPond below), not a disc laid on it
        iceHoles.Add(new UnityEngine.Vector3(hx - pond.x, 0.5f, hz - pond.y));
        var sx = pond.x + UnityEngine.Mathf.Sin(a) * 6f; var sz = pond.y + UnityEngine.Mathf.Cos(a) * 6f;
        Stand(place, sx, sz, hx, hz);
    }
    {
        var ice = new UnityEngine.GameObject("Pond Ice");
        ice.transform.SetParent(workProps, false);
        ice.transform.position = OnGround(pond.x, pond.y) + UnityEngine.Vector3.up * 0.03f;
        ice.AddComponent<UnityEngine.MeshFilter>();
        ice.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = FrozenWaterMat();
        var fp = ice.AddComponent<FrozenPond>();
        fp.Radius = pondRadius + 1.0f;
        fp.Holes = iceHoles;
        fp.enabled = false; fp.enabled = true;
    }
    var sail = WorkPlace("Sailing");
    var boat = V(pond.x + 1.5f, pond.y - 2f);
    Place(sv + "SM_Veh_FishingBoat_01_Snow.prefab", boat.x, boat.y, 1f, 115f, workProps, 0.35f);
    // two log benches on the south bank, facing the boat (off the lane to the cauldrons)
    foreach (var (lx, lz) in new[] { (pond.x - 1.5f, pond.y - 7.3f), (pond.x + 2.6f, pond.y - 7.4f) })
    {
        Seat(sail, "Stand " + (sail.childCount + 1), sv + "SM_Prop_Log_01_Snow.prefab", lx, lz, boat.x, boat.y, 0.9f, workProps);
        standCount++;
    }
}
// Taming: a pen with two frostbitten cows and a snow hare
{
    var place = WorkPlace("Taming");
    var pc = V(30f, -23f);
    // ten pieces round, each along the ring (a piece runs along its x), one left out for the way in (west-south-west)
    for (var i = 0; i < 10; i++)
    {
        if (i == 6) continue;
        var a = (i / 10f * 360f + 18f) * UnityEngine.Mathf.Deg2Rad;
        Place(sv + "SM_Prop_Fence_Wood_01_Snow.prefab", pc.x + UnityEngine.Mathf.Sin(a) * 3.6f, pc.y + UnityEngine.Mathf.Cos(a) * 3.6f, 1.1f, a * UnityEngine.Mathf.Rad2Deg, workProps);
    }
    Critter("home-cow", pc.x - 1f, pc.y + 0.6f, 200f, 1f, workProps);
    Critter("home-cow", pc.x + 1.1f, pc.y - 0.8f, 40f, 0.85f, workProps);
    Critter("home-bunny", pc.x + 0.4f, pc.y + 1.7f, 160f, 1f, workProps);
    Around(place, pc.x, pc.y, 4.4f, 4, 230f, 340f);
    Post(pc.x, pc.y, 2.8f);
    Zone(pc.x, pc.y, 6f);
}

// the resting place (!tower rest): a fire pit south-west of the square, logs to sit and meditate on, pelts to sleep on
var restC = new UnityEngine.Vector2(-15f, -21f);
Zone(restC.x, restC.y, 5f);
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
    // pelts to sleep on, log benches to sit on round the fire
    var sleep = i % 3 == 2;
    if (!sleep) { Seat(restSpots, "Sit " + (i + 1), sv + "SM_Prop_Log_01_Snow.prefab", p.x, p.y, restC.x, restC.y, 0.9f, rest); continue; }
    var pelt = Place(vr + "SM_Prop_Pelt_0" + (3 + rng.Next(2)) + ".prefab", p.x, p.y, 1.1f, yaw, rest); // the two flat rugs (the others hang or are rolled)
    NoCollide(pelt);
    var s = new UnityEngine.GameObject("Sleep " + (i + 1)).transform; s.SetParent(restSpots, false);
    // on top of the pelt, not under it
    var lie = OnGround(p.x, p.y); if (pelt) lie.y = UnityEngine.Mathf.Max(lie.y, BoundsOf(pelt).max.y - 0.05f);
    s.SetPositionAndRotation(lie + UnityEngine.Vector3.up * 0.03f, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
}

// ---- lanes: every one leads from the square (or another lane) to a door or a work place ----
// the gate road straight up, cobbles to the hall's door
Lane(1, 3f, V(0f, ringR - 0.2f), V(0f, 18f), V(0f, 28f), V(0f, 37f));
Lane(2, 2.8f, OnRing(hallDoor), hallDoor);
// the way in from the south
Lane(1, 2.8f, V(0f, -60f), V(1.2f, -42f), V(-0.8f, -24f), V(0f, -ringR + 0.2f));
// the forge, and on round the west to the woodcutters
Lane(1, 2.2f, OnRing(V(-10f, 10f)), V(-10.2f, 10.8f));
Lane(1, 2.2f, OnRing(V(-1f, -0.15f)), V(-18f, -1.5f), V(-27f, -3f), V(-33.5f, -3.5f));
Lane(1, 1.8f, V(-27f, -3f), V(-31f, -9f), V(-34f, -12.5f));
// the pond (Tove), the resting place and on to the farm
Lane(1, 2.2f, OnRing(V(1f, -1.2f)), V(10f, -11.5f), V(12f, -14f));
Lane(1, 2.2f, OnRing(V(-1f, -1.3f)), V(-10f, -14f), V(-13.2f, -17.6f));
Lane(1, 1.8f, V(-13.2f, -17.6f), V(-20f, -23f), V(-24f, -25f));
// the training yard (melee, healing), on to the mines
Lane(1, 2f, V(0f, 24f), V(-8f, 24.2f), V(-20f, 24.4f), V(-25f, 28f), V(-31f, 27.5f));
// east: one lane that forks, north round the hall's south side to the archery range, east to the berries
Lane(1, 2.2f, OnRing(V(1f, 0.2f)), V(15f, 3f), V(28f, 2.5f), V(33.5f, 0.5f));
Lane(1, 2f, V(15f, 3f), V(17f, 10.5f), V(23f, 17f), V(30f, 18.5f));
// the cauldrons and the pen, off the south road
Lane(1, 1.8f, V(0.4f, -31f), V(8.5f, -31.5f), V(18f, -29.5f), V(26.5f, -24.5f));

// pines on the slopes (none in town: the camera looks down from behind)
var trees = new[] { "SM_Env_Tree_Pine_01_Snow", "SM_Env_Tree_Pine_02_Snow", "SM_Env_Tree_Pine_03_Snow", "SM_Env_Tree_Pine_04_Snow", "SM_Env_Tree_Pine_Large_01_Snow", "SM_Env_Tree_Pine_Large_02_Snow", "SM_Env_Tree_Pine_Large_03_Snow" };
for (var i = 0; i < 240; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 44f + R() * 50f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 28f && UnityEngine.Mathf.Abs(x) < 12f) continue; // the stairs
    if (z < -30f && UnityEngine.Mathf.Abs(x) < 10f && z > -60f) continue; // the way in
    if (!Free(x, z, 2f)) continue;
    var k = rng.Next(trees.Length);
    Place(sv + trees[k] + ".prefab", x, z, k < 4 ? 2.4f + R() : 1.1f + R() * 0.5f, -1, foliage, 0.3f);
}
// no SM_Env_Mountain behind: scaled up, it drew white streaks across the sky
// snow drifts round the edge of town, rocks
for (var i = 0; i < 40; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 30f + R() * 14f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (UnityEngine.Mathf.Abs(x) < 8f || !Free(x, z, 1.5f)) continue;
    Place(rng.Next(2) == 0 ? ve + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3)) + ".prefab" : sv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow.prefab", x, z, 0.6f + R() * 0.6f, -1, props, 0.2f);
}
for (var i = 0; i < 9; i++)
{
    var a = i / 9f * UnityEngine.Mathf.PI * 2f + R() * 0.3f; var r = pondRadius + 1.2f + R();
    var x = pond.x + UnityEngine.Mathf.Cos(a) * r; var z = pond.y + UnityEngine.Mathf.Sin(a) * r;
    if ((V(x, z) - V(pond.x - 4.6f, pond.y + 2.5f)).magnitude < 2.5f || OnLane(x, z, 0.6f)) continue;
    var near = false; foreach (UnityEngine.Transform st in work.Find("Fishing")) if ((V(st.position.x - origin.x, st.position.z - origin.z) - V(x, z)).magnitude < 1.8f) near = true;
    if (near) continue;
    Place(sv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow.prefab", x, z, 0.5f + R() * 0.4f, -1, props, 0.15f);
}
// odds and ends: barrels, logs, a wagon, a totem, a cow skull on a pole
Place(sv + "SM_Prop_Wagon_01_Snow.prefab", 16.5f, -6.5f, 1.2f, 35f, props);
Place(sv + "SM_Prop_Logs_01_Snow.prefab", -16f, 19.5f, 1.3f, 10f, props);
Place(sv + "SM_Prop_Barrel_Half_01_Snow.prefab", 17.5f, -0.5f, 1.2f, -1, props);
Place(sv + "SM_Prop_Barrel_Half_01_Snow.prefab", -17f, -4f, 1.2f, -1, props);
Place(sv + "SM_Prop_Skull_Pole_01_Snow.prefab", -9f, -28f, 1.3f, 0f, props);
Place(sv + "SM_Prop_Clay_Pot_02_Snow.prefab", 25.5f, 13.8f, 1.1f, -1, props);
Place(sv + "SM_Prop_Rack_01_Snow.prefab", -25.5f, 1.5f, 1.2f, 90f, props);
// a gentle snowfall
var snowfall = Place("Assets/PolygonVikings/Prefabs/FX_Snow_Particle.prefab", 0f, 0f, 1f, 0f, props);
if (snowfall)
{
    snowfall.transform.position = origin + new UnityEngine.Vector3(0f, 14f, 0f);
    snowfall.transform.localScale = new UnityEngine.Vector3(1.8f, 1.2f, 1.8f);
    foreach (var ps in snowfall.GetComponentsInChildren<UnityEngine.ParticleSystem>()) { var em = ps.emission; em.rateOverTimeMultiplier = 500f; var mn = ps.main; mn.maxParticles = 3000; }
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

// ---- roads: cobbles round the square, the lanes ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var roads = roadsGo.AddComponent<SceneRoads>();
var ring = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 24; i++) { var a = i / 24f * UnityEngine.Mathf.PI * 2f; ring.Add(new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a) * ringR, UnityEngine.Mathf.Sin(a) * ringR)); }
ring.Add(ring[1]);
roads.Roads.Add(new SceneRoads.Road { Type = 2, Width = 3.2f, Points = ring, StartRound = 0f, EndRound = 0f });
roads.Roads.AddRange(lanes);
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
Person("hilda", "SM_Chr_Blacksmith_Female_01", fx0 + 2.6f, fz0 - 1.6f, 0f, 0f, 1.05f);
Person("tove", "SM_Chr_Hermit_01", pond.x - 4.6f, pond.y + 2.5f, pond.x, pond.y, 1f);
Person("ylva", "SM_Chr_Peasant_Female_01", 4.5f, -4f, 0f, -20f, 0.68f);
{ var g = AtDoor(-1.6f, 1.2f); Person("gunnhild", "SM_Chr_Soldier_Female_01", g.x, g.y, 0f, 0f, 1.14f); }

// ---- arrival and strolls ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(0f, -32f);
town.SpawnPoint = spawn.transform;
var strolls = new UnityEngine.GameObject("Strolls").transform; strolls.SetParent(root.transform, false);
town.Strolls = strolls;
var strollSpots = new[] { (5f, -6f), (-5f, 6f), (-6f, -5f), (6f, 5f), (-4f, 12f), (4f, 11f), (-9f, 6f), (10f, -12f), (8f, -9f), (-11f, -14f), (8f, 9f), (-14f, 2f), (14f, 3f), (0f, -16f), (0f, 30f), (-8f, -24f) };
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
UnityEngine.AI.NavMesh.SamplePosition(OnGround(0f, -32f), out var startHit, 3f, -1);
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
// renders them); Arrival the first time a stream gets here
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
    Pose(row, 1, 0f, 50f, -100f, 0f, 0f, 10f);
    Pose(row, 2, -25f, 22f, -42f, 0f, 4f, 15f);
    Pose(row, 3, -12f, 10f, -12f, 4f, 5f, 30f);
    Pose(row, 4, -5f, 7f, 2f, 3f, 10f, 45f);
}
town.Cutscene = cut;
town.TitleColor = new UnityEngine.Color(0.80f, 0.93f, 1f);
town.TitleOutline = new UnityEngine.Color(0.05f, 0.14f, 0.28f);

var scenePath = folder + "/Tower_Rimeholt.unity";
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
// the log houses have no chimneys: smoke from the smoke hole in the ridge, a quarter of the way along it
foreach (var home in homes)
{
    if (!home) continue;
    foreach (var f in home.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        if (!f.name.StartsWith("SM_Bld_Roof") || !f.sharedMesh) continue;
        var b = f.sharedMesh.bounds;
        var alongX = b.extents.x >= b.extents.z;
        var local = new UnityEngine.Vector3(b.center.x + (alongX ? b.extents.x * 0.4f : 0f), b.max.y - 0.15f, b.center.z + (alongX ? 0f : b.extents.z * 0.4f));
        Smoke(f.transform.TransformPoint(local), 0.4f);
        break;
    }
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
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-rimeholt-build.txt"), log.ToString());
return null;
