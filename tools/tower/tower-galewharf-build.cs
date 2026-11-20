// Builds Assets/Scenes/Tower/Tower_Galewharf.unity: Galewharf, the tower's third town (the Storm Spire band, reached by
// beating floor 20; docs/tower-design.md, section 4), after concept A (docs/mockups/galewharf-concept-a-island.jpg): a
// floating island whose terrain is cut along its rim (tools/tower/galewharf-rim.json) and sits in a rock made in Blender
// (tools/tower/blender/galewharf_rocks.py: the island's underside, the lighthouse's islet, the spire). A harbour where sky sailors moor: a
// cobbled square round a giant anchor, the Crow's Nest inn, timber houses and sailors' shacks, a windmill turning in the
// wind, a lighthouse, a long pier out over the cloud sea with Captain Brisa's ship moored at it (sailors and cloud fishers
// work there), Tinker Volt's shed with a lightning rod the storm likes, Tessa's sheep pen, a campfire to rest at (!tower
// rest), and at the north end a gate with stairs up to the spire. Its four people (IslandNpc), stroll spots, a work place
// for every skill (TowerTown.Work), lanes from the square to every door and work place, its own navmesh, the storm's sky
// and far lightning (TowerSky, TowerLightning), the camera's way in (Cutscene). At (1800, 0, -6000), beside whatever
// floor is loaded (Mossgate sits at 600, Rimeholt at 1200). Writes %TEMP%/tower-galewharf-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(1800f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_Galewharf";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Galewharf");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var root = new UnityEngine.GameObject("Tower Town - Galewharf");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var town = root.AddComponent<TowerTown>();
town.Key = "galewharf";
town.Radius = 95f;
// under the storm but out of the worst of it: grey-blue light, pale fog far below, a silver sun breaking through
town.SkyAmbient = new UnityEngine.Color(0.52f, 0.56f, 0.68f);
town.EquatorAmbient = new UnityEngine.Color(0.42f, 0.44f, 0.52f);
town.GroundAmbient = new UnityEngine.Color(0.24f, 0.24f, 0.28f);
town.FogColor = new UnityEngine.Color(0.70f, 0.74f, 0.82f);
town.FogDensity = 0.0040f;
town.SunColor = new UnityEngine.Color(0.96f, 0.94f, 1f);
town.SunIntensity = 1.05f;
town.SunAngles = new UnityEngine.Vector3(32f, -55f, 0f);
// the cloud sea far below the rim
town.HazeDepth = 92f;

// ---- the ground: the island's top, cut along the rim the Blender rock follows ----
const float size = 240f, height = 30f, baseHeight = 6f, flatRadius = 44f;
var pond = new UnityEngine.Vector2(46f, -4f);   // unused (Rimeholt's pond)
const float pondRadius = 0.1f;
var ci = System.Globalization.CultureInfo.InvariantCulture;
var rimTable = new System.Collections.Generic.List<UnityEngine.Vector2>();
foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText("tools/tower/galewharf-rim.json"), @"\[\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*\]"))
    rimTable.Add(new UnityEngine.Vector2(float.Parse(mm.Groups[1].Value, ci), float.Parse(mm.Groups[2].Value, ci)));
log.AppendLine("rim points " + rimTable.Count);
// the rim's radius toward a compass angle (degrees from east toward north)
float RimAt(float deg)
{
    deg = ((deg % 360f) + 360f) % 360f;
    var step = 360f / rimTable.Count;
    var i = (int)(deg / step);
    var f = (deg - i * step) / step;
    return rimTable[i % rimTable.Count].y * (1f - f) + rimTable[(i + 1) % rimTable.Count].y * f;
}
float EdgeAt(float x, float z) => RimAt(UnityEngine.Mathf.Atan2(z, x) * UnityEngine.Mathf.Rad2Deg);
UnityEngine.Vector2 Dir(float deg) => new UnityEngine.Vector2(UnityEngine.Mathf.Cos(deg * UnityEngine.Mathf.Deg2Rad), UnityEngine.Mathf.Sin(deg * UnityEngine.Mathf.Deg2Rad));
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 71.3f, z * s + 9.4f);
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var h = (Noise(x, z, 0.03f) - 0.5f) * 0.9f;
    var flat = UnityEngine.Mathf.InverseLerp(30f, 18f, r);
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    // the ground dips a little toward the rim, where the earth meets the rock
    var e = EdgeAt(x, z);
    h -= 0.5f * UnityEngine.Mathf.InverseLerp(e - 6f, e, r);
    return h;
}
var res = 513;
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
// the meadow pack's layers: grass, worn grass, a trodden town floor, dirt by the edge, rock on the cliffs
UnityEngine.TerrainLayer GL(string n) { var l = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/" + n + ".terrainlayer"); if (!l) log.AppendLine("missing layer " + n); return l; }
td.terrainLayers = new[] { GL("Terrain_Meadow_Grass_02"), GL("Terrain_Meadow_Grass_Pebbles_01"), GL("Terrain_Meadow_Dirt_Cracked_Pebbles_01"), GL("Terrain_Meadow_Dirt_01"), GL("Terrain_Meadow_Moss_Rock_01"), GL("Terrain_Meadow_Ruin_Tiles_01") };
var amRes = 256; td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, 6];
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f; var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[6];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
        w[0] = 1f;
        w[1] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.05f) - 0.45f) * 3f);
        // trodden ground in the middle of town, bare dirt toward the wind-scoured edge
        w[2] = UnityEngine.Mathf.Clamp01((24f - r) / 10f) * (0.6f + Noise(x + 9f, z, 0.2f) * 0.6f);
        w[3] = UnityEngine.Mathf.Clamp01((r - (EdgeAt(x, z) - 5f)) / 4f) * 1.2f
            // all dirt where the terrain is cut, so its stepped edge meets the collar dirt on dirt
            + UnityEngine.Mathf.Clamp01((r - (EdgeAt(x, z) - 3.4f)) / 1.2f) * 14f;
        w[4] = UnityEngine.Mathf.Clamp01((steep - 26f) / 10f) * 4f;
        // the plaza's flagstones, a ragged edge into the trodden ground (concept A's paved square)
        w[5] = UnityEngine.Mathf.Clamp01((13.5f - r) / 2.5f + (Noise(x * 1.7f, z * 1.7f, 0.25f) - 0.5f) * 0.8f) * 3f;
        var sum = w.Sum(); for (var k = 0; k < 6; k++) maps[zi, xi, k] = w[k] / sum;
    }
td.SetAlphamaps(0, 0, maps);
// cut the terrain along the rim: the rock (Blender) holds it up from below
var holeRes = res - 1;
var solid = new bool[holeRes, holeRes];
for (var zi = 0; zi < holeRes; zi++)
    for (var xi = 0; xi < holeRes; xi++)
    {
        var x = (xi + 0.5f) / holeRes * size - size / 2f; var z = (zi + 0.5f) / holeRes * size - size / 2f;
        // well inside the rim: the earth collar (below) runs smooth over the stepped hole edge
        solid[zi, xi] = UnityEngine.Mathf.Sqrt(x * x + z * z) < EdgeAt(x, z) - 1.2f;
    }
td.SetHoles(0, 0, solid);
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
var sv = "Assets/PolygonVikings/Prefabs/Props/";   // the Vikings pack's plain props (Rimeholt used the snowy ones)
var vb = "Assets/PolygonVikings/Prefabs/Buildings/";
var fv = "Assets/Synty/PolygonFantasyVillage/Prefabs/Buildings/Preset/";
var fkh = "Assets/PolygonFantasyKingdom/Prefabs/Buildings/Preset_Houses/SM_Bld_Preset_";
var pr = "Assets/Synty/PolygonPirateRealm/Prefabs/";
var pp = "Assets/PolygonPirates/Prefabs/Buildings/";
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

// a building that faces the square (its own pivot, its own roof), with a blocker the navmesh goes round
UnityEngine.GameObject Building(string path, float x, float z, float scale, float yawOffset = 0f)
{
    var yaw = Facing(x, z, 0f, 0f) + yawOffset;
    var h = Place(path, x, z, scale, yaw, buildings, 0.1f);
    Block(h);
    return h;
}
var homes = new System.Collections.Generic.List<UnityEngine.GameObject>
{
    // timber houses on stone feet with slate and grey roofs, close round the square (concept A)
    Building(fkh + "House_07_Optimized.prefab", -21f, 7f, 0.72f),
    Building(fkh + "House_03_Optimized.prefab", -21f, -9f, 0.85f),
    Building(fkh + "House_01_A_Optimized.prefab", 22f, 9f, 0.7f),
    Building(fkh + "House_08_Optimized.prefab", 22.5f, -5.5f, 0.85f),
    Building(fkh + "Hut_01_Optimized.prefab", -27.5f, 13f, 0.8f),
    Building(fkh + "House_10_Optimized.prefab", 16.5f, -15f, 0.5f),
    Building(fkh + "House_09_A_Optimized.prefab", -9f, -22f, 0.75f),
};
// the Crow's Nest: the inn north-east of the square, its door toward the square (the gate road stays clear)
var hallC = V(14f, 23f);
var hall = Building(fkh + "Tavern_01_Optimized.prefab", hallC.x, hallC.y, 0.85f);
var toSquare = (-hallC).normalized;
var across = V(toSquare.y, -toSquare.x);
var hallDoor = DoorOf(hall, 0.6f);
UnityEngine.Vector2 AtDoor(float side, float out_) => hallDoor + across * side + toSquare * out_;
var hallYaw = Facing(hallC.x, hallC.y, 0f, 0f);
{ var p = AtDoor(3.4f, -0.2f); Place(pr + "Props/SM_Prop_Barrel_01.prefab", p.x, p.y, 1.1f, -1, props); }
{ var p = AtDoor(-3.6f, -0.3f); Place(pr + "Props/SM_Prop_Barrel_02.prefab", p.x, p.y, 1.1f, -1, props); }
{ var p = AtDoor(-4.6f, 0.4f); Place(pr + "Props/SM_Prop_Barrel_Stand_01.prefab", p.x, p.y, 1.1f, hallYaw, props); }
{ var p = AtDoor(2f, -0.6f); Place(pr + "Props/SM_Prop_Sign_Post_01.prefab", p.x, p.y, 1.2f, hallYaw, props); }
foreach (var s2 in new[] { -2.2f, 2.2f }) { var p = AtDoor(s2, 0f); Place(pr + "Props/SM_Prop_Lamp_Post_01.prefab", p.x, p.y, 1f, hallYaw, props); }
{ var p = AtDoor(0f, 0.3f); Lamp(p.x, 2.5f, p.y, warm, 10f, 1.8f); }
// the windmill turning in the wind, west (the lighthouse stands on its own islet, below)
Block(Place(fkh + "House_Windmill_01_Optimized.prefab", -33f, -2f, 0.75f, 90f, buildings), 0.5f);
// watchtowers either side of the gate, out of the inn's way


// the gate up to the spire: the village entrance with stone stairs climbing behind it, flags and lamps
Place(sv + "SM_Prop_Village_Entrance_01.prefab", 0f, 36f, 2.2f, 180f, buildings);

foreach (var side in new[] { -1f, 1f })
{
    Place("Assets/PolygonFantasyKingdom/Prefabs/Props/Banners/SM_Prop_Banner_0" + (side < 0 ? 1 : 2) + ".prefab", side * 5.5f, 34.5f, 1.1f, 180f, props);
    Place(pr + "Props/SM_Prop_Lamp_Post_01.prefab", side * 3.4f, 34f, 1.1f, 180f, props);
    Lamp(side * 3.4f, 2.6f, 34f, warm, 9f, 1.6f);
}
for (var i = 0; i < 4; i++)
{
    foreach (var side in new[] { -1f, 1f })
    {
        var x = side * (7.4f + i * 2.7f);
        Place(sv + "SM_Prop_Fence_Wood_01.prefab", x, 36.6f + i * 0.15f, 1.4f, 0f, props);
    }
}

// Tinker Volt's shed: a timber shed, a workbench, coils of wire and jars, a lightning rod on a pole the storm likes
var fx0 = -12f; var fz0 = 13f;
var stall = Place(fkh + "Blacksmith_01_Optimized.prefab", fx0 - 1.5f, fz0 + 4.5f, 0.55f, Facing(fx0, fz0, 0f, 0f), buildings);
Place(vr + "SM_Prop_Furnace_01.prefab", fx0 - 3.2f, fz0 + 1.2f, 1.0f, Facing(fx0, fz0, 0f, 0f), props);
Place(sv + "SM_Prop_Anvil_Stump_01.prefab", fx0 + 1.2f, fz0 + 0.3f, 1.2f, 30f, props);
Place(vr + "SM_Prop_Grindstone_01.prefab", fx0 + 2.8f, fz0 + 2.2f, 1.1f, -1, props);
Place(pr + "Props/SM_Prop_Bottle_01.prefab", fx0 + 0.4f, fz0 + 1.8f, 1.4f, -1, props);
var rodPole = Place("Assets/Synty/PolygonDarkFortress/Prefabs/Buildings/SM_Bld_L_Pillar_01.prefab", fx0 + 3.6f, fz0 + 4.6f, 1.2f, 0f, buildings);
NoCollide(rodPole); Post(fx0 + 3.6f, fz0 + 4.6f, 0.6f);
var voltRod = new UnityEngine.GameObject("Volt's Rod"); voltRod.transform.SetParent(buildings, false);
voltRod.transform.position = OnGround(fx0 + 3.6f, fz0 + 4.6f) + UnityEngine.Vector3.up * (6f * 1.2f + 3f);
Place("Assets/Synty/PolygonSamuraiEmpire/Prefabs/Weapons/SM_Wep_Spear_01.prefab", fx0 + 3.6f, fz0 + 4.6f, 1.6f, 0f, buildings).transform.position += UnityEngine.Vector3.up * (6f * 1.2f + 1.1f);
var spark = Place("Assets/NeatWolf/Bundle03-MagicSpells/_Pillars/Pillar01_lightning.prefab", fx0 + 0.4f, fz0 + 1.8f, 0.25f, 0f, props);
if (spark) spark.transform.position += UnityEngine.Vector3.up * 1.2f;
Lamp(fx0 - 1f, 2f, fz0 + 1.5f, new UnityEngine.Color(0.6f, 0.75f, 1f), 10f, 2.2f);
Block(stall, 0.8f);

// ---- the rock (made in Blender, tools/tower/blender/galewharf_rocks.py): the island's underside under the rim, the
// lighthouse's islet, the spire. The FBX comes in turned half round (Unity's x and z are Blender's negated): each is
// turned back. Materials by the names Blender gave them, made here from the terrain's own textures.
UnityEngine.Material RockMat(string name, UnityEngine.Texture2D tex, UnityEngine.Texture2D normal, UnityEngine.Color tint, float smooth)
{
    var path = dataFolder + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m) { m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    if (tex) m.SetTexture("_BaseMap", tex);
    m.SetColor("_BaseColor", tint);
    if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
    m.SetFloat("_Smoothness", smooth);
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
}
UnityEngine.Texture2D T(string path) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path); if (!t) log.AppendLine("missing texture " + path); return t; }
var vtex = "Assets/Synty/PolygonVikingRealm/Textures/Env/";
var rockMats = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>
{
    ["Rock"] = RockMat("IslandRock", T(vtex + "Rock_01.png"), T(vtex + "Rock_01_Normals.png"), new UnityEngine.Color(0.78f, 0.79f, 0.84f), 0.05f),
    ["Earth"] = RockMat("IslandEarth", td.terrainLayers[3] ? td.terrainLayers[3].diffuseTexture : null, null, new UnityEngine.Color(0.82f, 0.72f, 0.62f), 0.05f),
    ["Grass"] = RockMat("IslandGrass", td.terrainLayers[0] ? td.terrainLayers[0].diffuseTexture : null, null, UnityEngine.Color.white, 0.05f),
    ["Stone"] = RockMat("IslandStone", T(vtex + "Rock_01.png"), T(vtex + "Rock_01_Normals.png"), new UnityEngine.Color(0.95f, 0.93f, 0.88f), 0.1f),
    ["Root"] = RockMat("IslandRoot", null, null, new UnityEngine.Color(0.30f, 0.20f, 0.12f), 0.1f),
};
UnityEngine.GameObject Model(string name, UnityEngine.Vector3 local, float yaw, bool collide)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/Galewharf/Models/" + name + ".fbx");
    if (!prefab) { log.AppendLine("missing model " + name + " (run tools/tower/blender/galewharf_rocks.py)"); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.name = name;
    go.transform.SetParent(buildings, true);
    go.transform.SetPositionAndRotation(origin + local, UnityEngine.Quaternion.Euler(0f, 180f + yaw, 0f));
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        var mats = r.sharedMaterials;
        for (var i = 0; i < mats.Length; i++)
        {
            var key = mats[i] ? mats[i].name.Split(' ')[0].Split('.')[0] : "";
            if (rockMats.TryGetValue(key, out var mm)) mats[i] = mm;
        }
        r.sharedMaterials = mats;
        if (collide) r.gameObject.AddComponent<UnityEngine.MeshCollider>();
    }
    return go;
}
// the underside, its top ring just under the terrain's cut edge
Model("GalewharfIsland", new UnityEngine.Vector3(0f, 0.05f, 0f), 0f, false);
// the earth collar: a smooth band along the rim, just under the ground, out to the rock's top ring. The terrain's holes
// can only cut in steps (one per heightmap cell); seen from above that edge was a saw blade with sky between the teeth.
{
    const int seg = 360;
    var rockTop = 0.05f - 0.25f;
    var rings = new[] { (-2.4f, true), (-0.5f, true), (0.65f, false) };
    var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uvs = new System.Collections.Generic.List<UnityEngine.Vector2>();
    for (var i = 0; i <= seg; i++)
    {
        var deg = i * 360f / seg;
        var d = Dir(deg); var e = RimAt(deg);
        foreach (var (off, onGround) in rings)
        {
            var p = d * (e + off);
            var y = onGround ? GroundAt(p.x, p.y) - 0.05f : rockTop;
            verts.Add(new UnityEngine.Vector3(p.x, y, p.y));
            uvs.Add(new UnityEngine.Vector2(p.x, p.y) / 4f);
        }
    }
    var tris = new System.Collections.Generic.List<int>();
    var n = rings.Length;
    for (var i = 0; i < seg; i++)
        for (var k = 0; k < n - 1; k++)
        {
            int a = i * n + k, b = (i + 1) * n + k;
            tris.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
        }
    var mesh = new UnityEngine.Mesh { name = "Galewharf Rim Collar" };
    mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
    // facing up (the winding runs the other way when the rim turns counter-clockwise)
    mesh.RecalculateNormals();
    if (mesh.normals[0].y < 0f) { tris.Reverse(); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); }
    mesh.RecalculateBounds();
    var meshPath = dataFolder + "/RimCollar.asset";
    UnityEditor.AssetDatabase.DeleteAsset(meshPath);
    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
    var collar = new UnityEngine.GameObject("Rim Collar");
    collar.transform.SetParent(buildings, false);
    collar.transform.position = origin;
    collar.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    // the terrain's own dirt (same texture, tiling and normals), untinted, so the seam does not show
    var dirtLayer = td.terrainLayers[3];
    var collarMat = RockMat("RimEarth", dirtLayer ? dirtLayer.diffuseTexture : null, dirtLayer ? dirtLayer.normalMapTexture : null, UnityEngine.Color.white, 0f);
    collar.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = collarMat;
    collar.AddComponent<UnityEngine.MeshCollider>().sharedMesh = mesh;
}

// the spire in the north, its foot lost in the clouds; a stone bridge from the gate to the foot of its stair
var spireZ = RimAt(90f) + 39f;
Model("GalewharfSpire", new UnityEngine.Vector3(0f, -1.9f, spireZ), 0f, true);
var bridgeFrom = RimAt(90f) - 1.8f;
var landing = spireZ - 22.5f;
var bridgeY0 = GroundAt(0f, bridgeFrom);
var bridgeT = new UnityEngine.GameObject("Spire Bridge").transform; bridgeT.SetParent(buildings, false);
var spans = UnityEngine.Mathf.CeilToInt((landing - bridgeFrom) / 4.8f);
for (var i = 0; i < spans; i++)
{
    var z = bridgeFrom + i * 4.8f;
    var y = UnityEngine.Mathf.Lerp(bridgeY0, 0.1f, i / (float)UnityEngine.Mathf.Max(1, spans - 1));
    var seg = Place("Assets/PolygonDungeonRealms/Prefabs/Buildings/SM_Bld_Ruin_Bridge_0" + (1 + i % 3) + ".prefab", 0f, z, 1.15f, 0f, bridgeT);
    if (seg) seg.transform.position = origin + new UnityEngine.Vector3(0f, y - 0.15f, z);
}
var bridgePillar = Place("Assets/PolygonDungeonRealms/Prefabs/Buildings/SM_Bld_Ruin_Bridge_Pillar_Large_01.prefab", 0f, (bridgeFrom + landing) * 0.5f, 1.15f, 0f, bridgeT);
if (bridgePillar) bridgePillar.transform.position = origin + new UnityEngine.Vector3(0f, -16f, (bridgeFrom + landing) * 0.5f);

// the lighthouse's islet in the east, over a rope bridge (concept C's islet, the user's pick)
var bridgeDeg = 15f;
var bridgeDir = Dir(bridgeDeg);
var bridgeStart = bridgeDir * (RimAt(bridgeDeg) - 1.2f);
var isletC = bridgeDir * (RimAt(bridgeDeg) + 15.5f + 9.6f);
Model("GalewharfIslet", new UnityEngine.Vector3(isletC.x, 0.3f, isletC.y), 0f, true);
var rope = Place(pr + "Buildings/SM_Bld_Ropebridge_02.prefab", bridgeStart.x, bridgeStart.y, 1f, UnityEngine.Mathf.Atan2(bridgeDir.x, bridgeDir.y) * UnityEngine.Mathf.Rad2Deg, buildings);
if (rope) { rope.name = "Rope Bridge"; rope.transform.position = origin + new UnityEngine.Vector3(bridgeStart.x, GroundAt(bridgeStart.x, bridgeStart.y) - 0.1f, bridgeStart.y); }
var lighthouse = Place(pr + "Buildings/SM_Bld_Lighthouse_01.prefab", isletC.x + 2.5f, isletC.y + 1.5f, 0.6f, 250f, buildings);
if (lighthouse)
{
    lighthouse.transform.position = origin + new UnityEngine.Vector3(isletC.x + 2.5f, 0.3f, isletC.y + 1.5f);
    var beam = new UnityEngine.GameObject("Lighthouse Light"); beam.transform.SetParent(lights, false);
    beam.transform.position = lighthouse.transform.position + UnityEngine.Vector3.up * 14.5f;
    var bl = beam.AddComponent<UnityEngine.Light>(); bl.type = UnityEngine.LightType.Point; bl.color = new UnityEngine.Color(1f, 0.85f, 0.55f); bl.range = 26f; bl.intensity = 3.2f; bl.shadows = UnityEngine.LightShadows.None;
}
foreach (var (dx, dz, path) in new[] { (-3f, -2f, "Props/SM_Prop_Barrel_01"), (-4f, 1.5f, "Props/SM_Prop_Crate_01"), (1f, -4.5f, "Props/SM_Prop_Lamp_Post_01") })
{
    var o = Place(pr + path + ".prefab", isletC.x + dx, isletC.y + dz, 1f, -1, props);
    if (o) o.transform.position = origin + new UnityEngine.Vector3(isletC.x + dx, 0.3f, isletC.y + dz);
}

// the harbour: a pier on stilts out over the cloud sea (south-east), Captain Brisa's Gullwing moored along it
var pierDeg = -30f;
var pierDir = Dir(pierDeg);
var pierSide = new UnityEngine.Vector2(-pierDir.y, pierDir.x);
var pierFoot = pierDir * (RimAt(pierDeg) - 2f);
var pierY = GroundAt(pierFoot.x, pierFoot.y) + 0.15f;
var pierYaw = UnityEngine.Mathf.Atan2(pierDir.x, pierDir.y) * UnityEngine.Mathf.Rad2Deg;
var pier = new UnityEngine.GameObject("Pier").transform; pier.SetParent(buildings, false);
for (var i = 0; i < 4; i++)
{
    var p = pierFoot + pierDir * (i * 7.4f + 7.4f);
    var seg = Place(pr + "Buildings/SM_Bld_Dock_Long_01.prefab", p.x, p.y, 1f, pierYaw, pier);
    if (seg) seg.transform.position = origin + new UnityEngine.Vector3(p.x, pierY, p.y);
    // stilts down into the clouds either side
    if (i % 2 == 1)
        foreach (var side in new[] { -1.2f, 1.2f })
        {
            var sp = p - pierDir * 3.7f + pierSide * side;
            var stilt = Place(pr + "Props/SM_Prop_Flag_Post_02.prefab", sp.x, sp.y, 4f, pierYaw, pier);
            if (stilt) { stilt.transform.position = origin + new UnityEngine.Vector3(sp.x, pierY - 8.6f, sp.y); stilt.transform.rotation = UnityEngine.Quaternion.Euler(0f, pierYaw, 90f); NoCollide(stilt); }
        }
}
var shipAt = pierFoot + pierDir * 22f + pierSide * 6.5f;
var gullwing = Place(pr + "Vehicles/SM_Veh_Preset_M_01.prefab", 0f, 0f, 0.6f, pierYaw, buildings);
if (gullwing) { gullwing.name = "Gullwing"; gullwing.transform.position = origin + new UnityEngine.Vector3(shipAt.x, pierY - 6.5f, shipAt.y); NoCollide(gullwing); }
foreach (var (along, side, path, sc) in new[] { (-3f, 3f, "Props/SM_Prop_Crate_01", 1f), (-4.5f, -2.5f, "Props/SM_Prop_Barrel_01", 1f), (-6f, -4f, "Props/SM_Prop_Sails_Rolled_Pile_01", 1f), (-6.5f, 4.5f, "Props/SM_Prop_Anchor_01", 1.2f) })
{
    var at = pierFoot + pierDir * along + pierSide * side;
    Place(pr + path + ".prefab", at.x, at.y, sc, -1, props);
}

// waterfalls off the rim (springs in the rock), west and south-west, as in the concept
foreach (var deg in new[] { 172f, 236f })
{
    var d = Dir(deg);
    var at = d * (RimAt(deg) - 0.3f);
    // our own waterfall (Synty's shader does not draw here): a strip arcing out over the rim and falling
    var fallGo = new UnityEngine.GameObject("Waterfall"); fallGo.transform.SetParent(props, false);
    fallGo.transform.SetPositionAndRotation(origin + new UnityEngine.Vector3(at.x, GroundAt(at.x, at.y) - 0.15f, at.y), UnityEngine.Quaternion.Euler(0f, UnityEngine.Mathf.Atan2(d.x, d.y) * UnityEngine.Mathf.Rad2Deg, 0f));
    fallGo.AddComponent<TowerWaterfall>();
    foreach (var side in new[] { -1.8f, 1.8f })
    {
        var rp = at - d * 1.5f + new UnityEngine.Vector2(-d.y, d.x) * side;
        Place(pr + "Environment/SM_Env_Rock_Basalt_0" + (1 + rng.Next(2)) + ".prefab", rp.x, rp.y, 1.1f, -1, props, 0.3f);
    }
}

// clouds under the pier and the ship, and the cloud sea round the rock
var cloudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonAdventure/Prefabs/Environments/SM_Env_Cloud_01.prefab");
var cloudBase = cloudPrefab ? cloudPrefab.GetComponentInChildren<UnityEngine.Renderer>().sharedMaterial : null;
UnityEngine.Material seaMat = null;
if (cloudBase)
{
    var mp = dataFolder + "/CloudSea.mat";
    seaMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(mp);
    if (!seaMat) { seaMat = new UnityEngine.Material(cloudBase); UnityEditor.AssetDatabase.CreateAsset(seaMat, mp); } else seaMat.CopyPropertiesFromMaterial(cloudBase);
    var c = new UnityEngine.Color(0.86f, 0.88f, 0.93f);
    if (seaMat.HasProperty("_BaseColor")) seaMat.SetColor("_BaseColor", c);
    if (seaMat.HasProperty("_Color")) seaMat.SetColor("_Color", c);
    UnityEditor.EditorUtility.SetDirty(seaMat);
}
var skyT = new UnityEngine.GameObject("Sky").transform; skyT.SetParent(root.transform, false);
void Cloud(UnityEngine.Vector3 world, float scale)
{
    var go = Place("Assets/PolygonAdventure/Prefabs/Environments/SM_Env_Cloud_0" + (1 + rng.Next(6)) + ".prefab", 0f, 0f, scale, -1, skyT);
    if (!go) return;
    go.transform.position = world;
    NoCollide(go);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) { if (seaMat) r.sharedMaterial = seaMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
}

for (var i = 0; i < 120; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 56f + R() * R() * 170f;
    var cx = UnityEngine.Mathf.Cos(a) * r; var cz = UnityEngine.Mathf.Sin(a) * r;
    if (cz > 40f && UnityEngine.Mathf.Abs(cx) < 40f) continue; // the spire's wall
    Cloud(origin + new UnityEngine.Vector3(cx, -52f - R() * 34f, cz), 6f + R() * 8f);
}
// the square: a giant anchor in the middle (sky sailors' luck), benches round it
Place("Assets/PolygonDungeonRealms/Prefabs/Buildings/SM_Bld_Ruin_Tiles_01.prefab", 0f, 0f, 0.75f, 0f, props, 1.05f);
var anchorGo = Place(pr + "Props/SM_Prop_Anchor_01.prefab", 0f, 0f, 5f, 180f, props);
if (anchorGo) anchorGo.transform.position += UnityEngine.Vector3.up * 0.35f;
var statueBlock = new UnityEngine.GameObject("Block anchor"); statueBlock.transform.SetParent(blockers, false); statueBlock.transform.position = OnGround(0f, 0f) + UnityEngine.Vector3.up * 1.5f;
statueBlock.AddComponent<UnityEngine.CapsuleCollider>().radius = 1.4f;
for (var i = 0; i < 4; i++)
{
    var a = i / 4f * UnityEngine.Mathf.PI * 2f + 0.785f; var x = UnityEngine.Mathf.Cos(a) * 5.6f; var z = UnityEngine.Mathf.Sin(a) * 5.6f;
    Place(sv + "SM_Prop_Bench_01.prefab", x, z, 1.2f, Facing(x, z, 0f, 0f) + 90f, props);
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
    Place(sv + "SM_Prop_Anvil_Stump_01.prefab", b.x, b.y, 1.2f, 70f, workProps);
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
        var rock = Place(pr + "Environment/SM_Env_Rock_Basalt_0" + (s > 1.5f ? 1 : 2) + ".prefab", rx, rz, s * 1.4f, -1, workProps, 0.2f);
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
    foreach (var (tx, tz) in new[] { (-34f, -13f), (-30f, -20f) })
    {
        Place(pr + "Environment/SM_Env_Conifer_0" + (tz < -5f ? 2 : 1) + ".prefab", tx, tz, 1.5f, -1, workProps, 0.3f);
        Around(place, tx, tz, 1.7f, 3, 40f, 140f);
        Post(tx, tz, 0.6f);
        Zone(tx + 1.5f, tz, 4.5f);
    }
    Place(vr + "SM_Prop_Wood_Stump_01.prefab", -27.5f, -15.5f, 1.1f, -1, workProps);
    Place(vr + "SM_Prop_Wood_Pile_02.prefab", -36f, -18f, 1.1f, 80f, workProps);
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
    var fc = V(-17f, -30f);
    for (var row = 0; row < 3; row++)
        for (var k = 0; k < 6; k++)
            NoCollide(Place((row + k) % 3 == 0 ? fk + "SM_Prop_Cabbage_02.prefab" : vr + "SM_Prop_Cabbage_01.prefab", fc.x - 3.5f + k * 1.4f, fc.y - 1.5f + row * 1.5f, 1.1f, -1, workProps));
    // fence pieces run along their x (2.2 m at this scale): a line each side, the ends open
    for (var k = 0; k < 5; k++)
    {
        Place(sv + "SM_Prop_Fence_Wood_01.prefab", fc.x - 4.4f + k * 2.15f, fc.y - 3.1f, 1.1f, 0f, workProps);
        Place(sv + "SM_Prop_Fence_Wood_01.prefab", fc.x - 4.4f + k * 2.15f, fc.y + 3f, 1.1f, 0f, workProps);
    }
    foreach (var k in new[] { -3f, -1f, 1f, 3f }) { Stand(place, fc.x + k, fc.y + 0.75f, fc.x + k, fc.y - 0.75f); }
    foreach (var k in new[] { -2f, 2f }) { Stand(place, fc.x + k, fc.y - 0.75f, fc.x + k, fc.y - 2f); }
    Zone(fc.x, fc.y, 6.5f);
}
// Cooking: a spit over a fire at the hall's door
{
    var place = WorkPlace("Cooking");
    var sp = V(6f, -17f); // open ground south of the square (the inn's door looks onto the gate road)
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
    var hut = Place(vb + "SM_Bld_Stall_Cover_01.prefab", 7.4f, -38.4f, 1.5f, 15f, buildings);
    Block(hut, 0.5f);
    Place(vr + "SM_Prop_Herb_02.prefab", 9.6f, -39.4f, 1.1f, -1, workProps);
}
// Fishing: cloud fishing off the pier's sides (lines dropped into the clouds); Sailing: sailors mending rope by the ship
{
    var place = WorkPlace("Fishing");
    foreach (var k in new[] { 0, 1, 2 })
    {
        var along = pierFoot + pierDir * (k * 5f + 6f);
        foreach (var side in new[] { -1f, 1f })
        {
            var st = along + pierSide * (side * 0.9f);
            var s3 = new UnityEngine.GameObject("Stand " + (place.childCount + 1)).transform; s3.SetParent(place, false);
            s3.position = origin + new UnityEngine.Vector3(st.x, pierY + 0.05f, st.y);
            var outward = pierSide * side;
            s3.rotation = UnityEngine.Quaternion.Euler(0f, UnityEngine.Mathf.Atan2(outward.x, outward.y) * UnityEngine.Mathf.Rad2Deg, 0f);
            standCount++;
        }
    }
    var sail = WorkPlace("Sailing");
    // two logs on the rock by the pier's root, facing the ship
    foreach (var off in new[] { -3.5f, 3.5f })
    {
        var at = pierFoot - pierDir * 5f + pierSide * off;
        Seat(sail, "Stand " + (sail.childCount + 1), sv + "SM_Prop_Log_01.prefab", at.x, at.y, shipAt.x, shipAt.y, 0.9f, workProps);
        standCount++;
    }
    Zone(pierFoot.x - pierDir.x * 4f, pierFoot.y - pierDir.y * 4f, 6f);
}
// Taming: a pen with two frostbitten cows and a snow hare
{
    var place = WorkPlace("Taming");
    var pc = V(28f, -24f);
    // ten pieces round, each along the ring (a piece runs along its x), one left out for the way in (west-south-west)
    for (var i = 0; i < 10; i++)
    {
        if (i == 6) continue;
        var a = (i / 10f * 360f + 18f) * UnityEngine.Mathf.Deg2Rad;
        Place(sv + "SM_Prop_Fence_Wood_01.prefab", pc.x + UnityEngine.Mathf.Sin(a) * 3.6f, pc.y + UnityEngine.Mathf.Cos(a) * 3.6f, 1.1f, a * UnityEngine.Mathf.Rad2Deg, workProps);
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
    if (!sleep) { Seat(restSpots, "Sit " + (i + 1), sv + "SM_Prop_Log_01.prefab", p.x, p.y, restC.x, restC.y, 0.9f, rest); continue; }
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
Lane(1, 2.8f, V(0f, -RimAt(270f) + 3f), V(1.2f, -32f), V(-0.8f, -24f), V(0f, -ringR + 0.2f));
// the forge, and on round the west to the woodcutters
Lane(1, 2.2f, OnRing(V(-10f, 10f)), V(-10.2f, 10.8f));
Lane(1, 2.2f, OnRing(V(-1f, -0.15f)), V(-18f, -1.5f), V(-27f, -3f), V(-30f, -2.5f));
Lane(1, 1.8f, V(-27f, -3f), V(-29f, -9f), V(-31f, -14f));
// the pond (Tove), the resting place and on to the farm
Lane(1, 2.2f, OnRing(V(1f, -0.4f)), V(18f, -5f), pierFoot - pierDir * 3f + pierSide * 1.5f, pierFoot);
Lane(1, 2.2f, OnRing(V(-1f, -1.3f)), V(-10f, -14f), V(-13.2f, -17.6f));
Lane(1, 1.8f, V(-13.2f, -17.6f), V(-15f, -23f), V(-16f, -26f));
// the training yard (melee, healing), on to the mines
Lane(1, 2f, V(0f, 24f), V(-8f, 24.2f), V(-20f, 24.4f), V(-25f, 28f), V(-31f, 27.5f));
// east: one lane that forks, north round the hall's south side to the archery range, east to the berries
Lane(1, 2.2f, V(18f, -5f), V(28f, 0.5f), V(33.5f, 0.5f));
// out along the rim to the rope bridge and the lighthouse
Lane(1, 2f, V(28f, 0.5f), V(38f, 6f), bridgeStart);
Lane(1, 2f, V(15f, 3f), V(17f, 10.5f), V(23f, 17f), V(30f, 18.5f));
// the cauldrons and the pen, off the south road
Lane(1, 1.8f, V(0.4f, -31f), V(8.5f, -31.5f), V(18f, -29.5f), V(26.5f, -24.5f));

// market stalls with awnings round the plaza (off the lanes), lantern posts, banners: a busy harbour square
var market = new UnityEngine.GameObject("Market").transform; market.SetParent(root.transform, false);
var stallKinds = new[] { 1, 3, 5, 7, 2, 6 };
var stallN = 0;
foreach (var deg in new[] { 28f, 160f, 200f, 320f, 248f, 118f })
{
    var d = Dir(deg); var at = d * (ringR + 4.2f);
    if (!Free(at.x, at.y, 1.2f)) { log.AppendLine("no room for a stall at " + deg); continue; }
    var st2 = Place("Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Market_Stall_0" + stallKinds[stallN % stallKinds.Length] + ".prefab", at.x, at.y, 1f, Facing(at.x, at.y, 0f, 0f), market);
    Block(st2, 0.7f);
    Zone(at.x, at.y, 2.6f);
    stallN++;
}
for (var i = 0; i < 8; i++)
{
    var d = Dir(22.5f + i * 45f); var at = d * (ringR + 1.9f);
    if (!Free(at.x, at.y, 0.4f)) continue;
    Place(pr + "Props/SM_Prop_Lamp_Post_01.prefab", at.x, at.y, 1f, Facing(at.x, at.y, 0f, 0f), market);
    Lamp(at.x, 2.6f, at.y, warm, 7f, 1.1f);
}
log.AppendLine("market stalls " + stallN);

// wind-bent conifers and basalt round the edge of town (none in town: the camera looks down from behind)
for (var i = 0; i < 70; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 36f + R() * 10f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 28f && UnityEngine.Mathf.Abs(x) < 12f) continue; // the stairs
    if (z < -28f && UnityEngine.Mathf.Abs(x) < 8f) continue; // the way in
    if (r > EdgeAt(x, z) - 3.5f || !Free(x, z, 2f)) continue;
    var k = rng.Next(4);
    Place(k < 2 ? pr + "Environment/SM_Env_Conifer_0" + (1 + k) + ".prefab" : pr + "Environment/SM_Env_Rock_Basalt_0" + (k - 1) + ".prefab", x, z, k < 2 ? 1.0f + R() * 0.5f : 0.9f + R() * 0.8f, -1, foliage, 0.2f);
}
for (var i = 0; i < 140; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 14f + R() * 30f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Free(x, z, 0.8f) || r > EdgeAt(x, z) - 3f) continue;
    var g = Place(rng.Next(3) == 0 ? pr + "Environment/SM_Env_Ground_Cover_0" + (1 + rng.Next(8)) + ".prefab" : "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_Tall_0" + (1 + rng.Next(4)) + ".prefab", x, z, 0.6f + R() * 0.5f, -1, foliage, 0.05f);
    NoCollide(g);
}
// odds and ends: barrels, logs, a wagon, rope and sails, a flag on the windward side
Place(sv + "SM_Prop_Wagon_01.prefab", 16.5f, -7f, 1.2f, 35f, props);
Place(sv + "SM_Prop_Logs_01.prefab", -16f, 19.5f, 1.3f, 10f, props);
Place(sv + "SM_Prop_Barrel_Half_01.prefab", 17.5f, 2f, 1.2f, -1, props);
Place(sv + "SM_Prop_Barrel_Half_01.prefab", -17f, -4f, 1.2f, -1, props);
Place(pr + "Props/SM_Prop_Ropes_01.prefab", 25.5f, 13.8f, 1.1f, -1, props);
Place(pr + "Props/SM_Prop_Sails_Rolled_Pile_01.prefab", -25.5f, 1.5f, 1.1f, 90f, props);
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
foreach (var st in work.GetComponentsInChildren<UnityEngine.Transform>())
{
    if (!st.name.StartsWith("Stand")) continue;
    var lp = st.position - origin;
    var margin = EdgeAt(lp.x, lp.z) - UnityEngine.Mathf.Sqrt(lp.x * lp.x + lp.z * lp.z);
    if (margin < 2f && st.parent.name != "Fishing") log.AppendLine("near the rim: " + st.parent.name + "/" + st.name + " margin " + margin.ToString("F1"));
}
foreach (UnityEngine.Transform b in buildings)
{
    var lp = b.position - origin;
    if (UnityEngine.Mathf.Sqrt(lp.x * lp.x + lp.z * lp.z) > EdgeAt(lp.x, lp.z) - 3f && !b.name.Contains("Pier") && !b.name.Contains("Gullwing") && !b.name.Contains("Island") && !b.name.Contains("Spire") && !b.name.Contains("Islet") && !b.name.Contains("Bridge") && !b.name.Contains("Lighthouse") && !b.name.Contains("Rope"))
        log.AppendLine("building near the rim: " + b.name + " at " + lp.ToString("F1"));
}

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
Person("volt", "SM_Chr_Mage_01", fx0 + 2.6f, fz0 - 1.6f, 0f, 0f, 0.95f);
Person("brisa", "SM_Chr_Soldier_Female_01", 38.5f, -1.5f, 0f, 0f, 1.08f);
Person("tessa", "SM_Chr_Peasant_Female_01", 4.5f, -4f, 0f, -20f, 0.68f);
{ var g = AtDoor(-1.6f, 1.2f); Person("hob", "SM_Chr_Bartender_01", g.x, g.y, 0f, 0f, 1.05f); }

// ---- arrival and strolls ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(0f, -32f);
town.SpawnPoint = spawn.transform;
var strolls = new UnityEngine.GameObject("Strolls").transform; strolls.SetParent(root.transform, false);
town.Strolls = strolls;
var strollSpots = new[] { (5f, -6f), (-5f, 6f), (-6f, -5f), (6f, 5f), (-4f, 12f), (4f, 11f), (-9f, 6f), (10f, -10f), (8f, -9f), (-11f, -14f), (8f, 9f), (-14f, 2f), (14f, 3f), (0f, -16f), (0f, 30f), (34f, -2f) };
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
    // in over the cloud sea from the east, past the moored Gullwing and the lighthouse, down into the square
    Pose(row, 1, 110f, 34f, -40f, 30f, 0f, 0f);
    Pose(row, 2, 70f, 16f, -16f, 40f, 2f, 4f);
    Pose(row, 3, 34f, 9f, -14f, 4f, 3f, 10f);
    Pose(row, 4, 10f, 6f, -14f, -2f, 5f, 18f);
}
town.Cutscene = cut;
// storm blue with a dark outline, readable over clouds and sky
town.TitleColor = new UnityEngine.Color(0.70f, 0.86f, 1f);
town.TitleOutline = new UnityEngine.Color(0.06f, 0.10f, 0.24f);
var townSky = root.AddComponent<TowerSky>();
townSky.Zenith = new UnityEngine.Color(0.26f, 0.29f, 0.37f);
townSky.Horizon = town.FogColor;
var townStorm = root.AddComponent<TowerLightning>();
townStorm.Rods = new[] { voltRod.transform };
townStorm.MinGap = 14f; townStorm.MaxGap = 32f; townStorm.RodShare = 0.15f;

var scenePath = folder + "/Tower_Galewharf.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-galewharf-build.txt"), log.ToString());
return null;
