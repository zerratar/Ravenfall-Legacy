// Builds Assets/Scenes/Tower/Tower_Glacier.unity: the second band's open layout ("The Frozen Vault"), a snowed-in valley
// under the open sky at (0, 0, -6000). A terrain (deep snow, packed snow on the path, blue ice on a frozen pond, rock on
// the slopes, frozen flagstones in the boss's ring) in a bowl of pine-covered hills with mountains behind, so the floor
// never ends at an edge; a ring of rune stones and braziers around the plaza; cairns, snow mounds, rocks; falling snow
// and an aurora; 24 monster spots; the boss's spot; one hidden template per monster look (copied from MainWorld's
// enemies); its own navmesh. Editor only, not in play mode. Writes the result to %TEMP%/tower-glacier-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_Glacier";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Glacier");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Frozen Vault");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 115f;
// a cold clear day: blue sky light, white fog, a low pale sun
floor.SkyAmbient = new UnityEngine.Color(0.62f, 0.70f, 0.82f);
floor.EquatorAmbient = new UnityEngine.Color(0.50f, 0.56f, 0.64f);
floor.GroundAmbient = new UnityEngine.Color(0.30f, 0.32f, 0.36f);
floor.FogColor = new UnityEngine.Color(0.78f, 0.84f, 0.92f);
floor.FogDensity = 0.0042f;
floor.SunColor = new UnityEngine.Color(1f, 0.95f, 0.88f);
floor.SunIntensity = 1.25f;
floor.SunAngles = new UnityEngine.Vector3(32f, -40f, 0f);

// ---- the layout (local to the root) ----
var spawnLocal = new UnityEngine.Vector3(0f, 0f, -52f);
var plaza = new UnityEngine.Vector3(0f, 0f, 36f);
var pond = new UnityEngine.Vector3(-34f, 0f, -6f);
const float pondRadius = 13f;
const float plazaRadius = 15f;
const float flatRadius = 64f;   // the play area
const float size = 240f, height = 40f, baseHeight = 2f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 51.7f, z * s + 17.3f);
float DistToPath(UnityEngine.Vector3 p)
{
    var a = spawnLocal; var b = plaza;
    var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0;
    var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    var c = a + ab * t; c.y = 0; var q = p; q.y = 0;
    return (q - c).magnitude;
}
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    // soft drifts
    var h = (Noise(x, z, 0.03f) - 0.5f) * 1.2f + (Noise(x, z, 0.09f) - 0.5f) * 0.35f;
    var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    var flat = UnityEngine.Mathf.Max(UnityEngine.Mathf.InverseLerp(plazaRadius + 6f, plazaRadius, dp), UnityEngine.Mathf.InverseLerp(6f, 2f, DistToPath(new UnityEngine.Vector3(x, 0, z))));
    h = UnityEngine.Mathf.Lerp(h, 0.05f, flat);
    // the frozen pond sits a little lower, dead flat
    var dpond = new UnityEngine.Vector2(x - pond.x, z - pond.z).magnitude;
    var inPond = UnityEngine.Mathf.InverseLerp(pondRadius + 4f, pondRadius - 1f, dpond);
    h = UnityEngine.Mathf.Lerp(h, -0.35f, inPond);
    // the bowl: steep white hills rise past the play area
    var rim = UnityEngine.Mathf.InverseLerp(flatRadius, 100f, r);
    rim = rim * rim * (3f - 2f * rim);
    h += rim * (18f + Noise(x, z, 0.045f) * 14f);
    return h;
}

// ---- terrain layers made from the Viking Realm textures (tinted for ice and packed snow) ----
UnityEngine.Texture2D Tex(string path) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path); if (!t) log.AppendLine("missing texture " + path); return t; }
var vr = "Assets/Synty/PolygonVikingRealm/Textures/Env/";
UnityEngine.TerrainLayer Layer(string name, UnityEngine.Texture2D diffuse, UnityEngine.Texture2D normal, float tile, UnityEngine.Color tint, float smooth)
{
    var path = dataFolder + "/" + name + ".terrainlayer";
    UnityEditor.AssetDatabase.DeleteAsset(path);
    var l = new UnityEngine.TerrainLayer { diffuseTexture = diffuse, normalMapTexture = normal, tileSize = new UnityEngine.Vector2(tile, tile), smoothness = smooth };
    l.diffuseRemapMin = UnityEngine.Vector4.zero;
    l.diffuseRemapMax = new UnityEngine.Vector4(tint.r, tint.g, tint.b, 1f);
    UnityEditor.AssetDatabase.CreateAsset(l, path);
    return l;
}
var snowTex = Tex(vr + "Snow_01.png");
var rockTex = Tex(vr + "Rock_01.png");
var rockNormal = Tex(vr + "Rock_01_Normals.png");
var groundNormal = Tex(vr + "Ground_01_Normals.png");
var ruinTiles = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Terrain_Meadow_Ruin_Tiles_01.terrainlayer");
var lSnow = Layer("Snow", snowTex, groundNormal, 7f, new UnityEngine.Color(1f, 1f, 1f), 0.15f);
var lDrift = Layer("Snow_Shade", snowTex, groundNormal, 11f, new UnityEngine.Color(0.86f, 0.90f, 0.98f), 0.1f);
var lPacked = Layer("Snow_Packed", snowTex, groundNormal, 4f, new UnityEngine.Color(0.78f, 0.78f, 0.80f), 0.1f);
var lIce = Layer("Ice", snowTex, groundNormal, 9f, new UnityEngine.Color(0.55f, 0.78f, 0.95f), 0.85f);
var lRock = Layer("Rock", rockTex, rockNormal, 8f, new UnityEngine.Color(0.85f, 0.88f, 0.95f), 0.1f);
var lFlag = ruinTiles ? Layer("Flagstones_Frozen", ruinTiles.diffuseTexture, ruinTiles.normalMapTexture, ruinTiles.tileSize.x, new UnityEngine.Color(0.80f, 0.86f, 0.95f), 0.4f) : null;
if (!lFlag) log.AppendLine("missing ruin tiles");

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
var layerList = new System.Collections.Generic.List<UnityEngine.TerrainLayer> { lSnow, lDrift, lPacked, lIce, lRock };
if (lFlag) layerList.Add(lFlag);
td.terrainLayers = layerList.ToArray();
var amRes = 256;
td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, td.terrainLayers.Length];
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f;
        var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[td.terrainLayers.Length];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        w[0] = 1f;
        w[1] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.05f) - 0.45f) * 3f);
        w[4] = UnityEngine.Mathf.Clamp01((steep - 28f) / 10f) * 2.5f;
        var dpond = new UnityEngine.Vector2(x - pond.x, z - pond.z).magnitude;
        w[3] = UnityEngine.Mathf.Clamp01((pondRadius + 0.5f - dpond) / 1.5f) * 6f;
        var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
        var ring = UnityEngine.Mathf.Clamp01((plazaRadius + 1f - dp) / 3f + (Noise(x, z, 0.3f) - 0.5f) * 0.6f);
        if (lFlag) w[5] = ring * 1.6f * UnityEngine.Mathf.Clamp01(1.3f - Noise(x + 40f, z, 0.25f));
        var path = UnityEngine.Mathf.Clamp01((2.8f - DistToPath(new UnityEngine.Vector3(x, 0, z))) / 1.6f + (Noise(x, z, 0.4f) - 0.5f) * 0.6f);
        w[2] = path * (1f - ring) * 1.2f;
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
var rng = new System.Random(23);
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
var ring2 = new UnityEngine.GameObject("Stone Ring").transform; ring2.SetParent(root.transform, false);
var foliage = new UnityEngine.GameObject("Pines").transform; foliage.SetParent(root.transform, false);
var fx = new UnityEngine.GameObject("FX").transform; fx.SetParent(root.transform, false);
var env = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";
var vp = "Assets/Synty/PolygonVikingRealm/Prefabs/Props/";
var vfx = "Assets/Synty/PolygonVikingRealm/Prefabs/FX/";
var snowv = "Assets/PolygonVikings/Prefabs/Snow_Variant/";

bool Clear(float x, float z, float margin)
{
    var p = new UnityEngine.Vector3(x, 0, z);
    if (new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude < plazaRadius + margin) return false;
    if (new UnityEngine.Vector2(x - pond.x, z - pond.z).magnitude < pondRadius + margin) return false;
    if (DistToPath(p) < 3.5f + margin * 0.5f) return false;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - spawnLocal.z).magnitude < 9f + margin) return false;
    return true;
}

// pines: a thick ring on the hills (the follow camera sits behind and above players, so none in the clearing)
for (var i = 0; i < 270; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 64f + R() * 46f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (new UnityEngine.Vector2(x - spawnLocal.x, z - (spawnLocal.z - 18f)).magnitude < 20f) continue; // the camera behind the arrival
    // mostly snow-laden pines (the Vikings pack's snow variants), a few dark green ones between
    var kind = rng.Next(10);
    var name = kind < 4 ? snowv + "SM_Env_Tree_Pine_0" + (1 + kind) + "_Snow" : kind < 7 ? snowv + "SM_Env_Tree_Pine_Large_0" + (kind - 3) + "_Snow" : env + "SM_Env_Tree_Pine_0" + (1 + rng.Next(6));
    // the snow pines are a third of the size of the green ones
    var pineScale = kind < 4 ? 2.4f + R() * 1.0f : kind < 7 ? 1.1f + R() * 0.5f : 1.0f + R() * 0.6f;
    Place(name + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.4f, pineScale, -1, foliage);
}
// mountains far behind the hills, cliffs on the rim
for (var i = 0; i < 9; i++)
{
    var a = i / 9f * UnityEngine.Mathf.PI * 2f + R() * 0.3f; var r = 150f + R() * 25f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    Place(env + "SM_Env_Mountain_0" + (1 + rng.Next(3)) + ".prefab", origin + new UnityEngine.Vector3(x, -6f, z), 3.5f + R() * 1.5f, -1, props);
}
for (var i = 0; i < 24; i++)
{
    var a = i / 24f * UnityEngine.Mathf.PI * 2f + R() * 0.15f; var r = 76f + R() * 16f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (rng.Next(3) == 0) continue;
    Place(env + "SM_Env_Rock_Cliff_0" + (1 + rng.Next(5)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 6.5f, 1.2f + R() * 0.6f, -a * UnityEngine.Mathf.Rad2Deg + 90f + (R() - 0.5f) * 30f, props);
}
// snow mounds and hills, stones and cairns in the clearing
for (var i = 0; i < 46; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 10f + R() * 52f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    var kind = rng.Next(6);
    if (!Clear(x, z, kind == 5 || kind == 0 || kind == 3 ? 6f : 1.5f)) continue; // the big drifts keep well off the path
    var path = kind == 0 ? env + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3))
        : kind == 1 ? snowv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow"
        : kind == 2 ? vp + "SM_Prop_Cairn_0" + (1 + rng.Next(2))
        : kind == 3 ? env + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3))
        : kind == 4 ? snowv + "SM_Env_Rock_0" + (1 + rng.Next(7)) + "_Snow"
        : env + "SM_Env_Snow_Hill_0" + (1 + rng.Next(3));
    Place(path + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, 0.8f + R() * 0.7f, -1, props);
}
// the frozen pond's edge: stones, a broken boat and a fishing hole with a little fire
for (var i = 0; i < 12; i++)
{
    var a = i / 12f * UnityEngine.Mathf.PI * 2f + R() * 0.2f; var r = pondRadius + 1.5f + R() * 1.5f;
    var x = pond.x + UnityEngine.Mathf.Cos(a) * r; var z = pond.z + UnityEngine.Mathf.Sin(a) * r;
    if (DistToPath(new UnityEngine.Vector3(x, 0, z)) < 4f) continue;
    Place(env + (rng.Next(2) == 0 ? "SM_Env_Stone_0" + (1 + rng.Next(4)) : "SM_Env_Snow_Edge_0" + (1 + rng.Next(6))) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, 0.7f + R() * 0.5f, -1, props);
}
Place(vp + "SM_Prop_Boat_Destroyed_01.prefab", OnGround(pond.x - 6f, pond.z + 8f) + UnityEngine.Vector3.down * 0.3f, 1f, 35f, props);
Place(vp + "SM_Prop_Fire_Pit_01.prefab", OnGround(pond.x + 9f, pond.z - 9f), 1f, -1, props);

// the plaza: a ring of rune stones with braziers between them, statues at the way in
var stones = 10;
for (var i = 0; i < stones; i++)
{
    var a = i / (float)stones * UnityEngine.Mathf.PI * 2f;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 20f) continue; // the way in
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * (plazaRadius + 0.8f);
    var z = plaza.z + UnityEngine.Mathf.Sin(a) * (plazaRadius + 0.8f);
    Place(vp + "SM_Prop_RuneStone_0" + (1 + rng.Next(4)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, 1.7f, -a * UnityEngine.Mathf.Rad2Deg + 90f, ring2);
    var b = a + UnityEngine.Mathf.PI / stones;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(b * UnityEngine.Mathf.Rad2Deg, -90f)) < 25f) continue;
    if (i % 2 == 0)
    {
        var bx = plaza.x + UnityEngine.Mathf.Cos(b) * (plazaRadius + 0.6f);
        var bz = plaza.z + UnityEngine.Mathf.Sin(b) * (plazaRadius + 0.6f);
        Place(vp + "SM_Prop_Brazier_01.prefab", OnGround(bx, bz), 1.9f, -1, ring2);
        Place(vp + "SM_Prop_Brazier_01_Insert_01.prefab", OnGround(bx, bz), 1.9f, -1, ring2);
        Place(vfx + "FX_Fire_Large_01.prefab", OnGround(bx, bz) + UnityEngine.Vector3.up * 1.5f, 0.35f, 0f, ring2);
    }
}
foreach (var side in new[] { -1f, 1f })
{
    Place(vp + "SM_Prop_Statue_0" + (side < 0 ? 1 : 2) + ".prefab", OnGround(plaza.x + side * 4.5f, plaza.z - plazaRadius - 1.5f), 1.3f, 180f, ring2);
}
// snow falling over the play area, an aurora in the north
// one soft snowfall over the play area (the Viking pack's flakes, fewer of them)
var snowfall = Place("Assets/PolygonVikings/Prefabs/FX_Snow_Particle.prefab", origin + new UnityEngine.Vector3(0f, 14f, 0f), 1f, 0f, fx);
if (snowfall)
{
    snowfall.transform.localScale = new UnityEngine.Vector3(2.2f, 1.2f, 2.2f);
    foreach (var ps in snowfall.GetComponentsInChildren<UnityEngine.ParticleSystem>())
    {
        var em = ps.emission; em.rateOverTimeMultiplier = 700f;
        var mn = ps.main; mn.scalingMode = UnityEngine.ParticleSystemScalingMode.Shape; mn.maxParticles = 4000;
    }
}
log.AppendLine("placed " + placed);

// ---- the road from the arrival to the plaza: gravel through the snow ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var sceneRoads = roadsGo.AddComponent<SceneRoads>();
var roadPts = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 8; i++) { var t = i / 8f; var p = UnityEngine.Vector3.Lerp(spawnLocal, plaza + new UnityEngine.Vector3(0, 0, -plazaRadius + 1f), t); roadPts.Add(new UnityEngine.Vector2(p.x, p.z)); }
sceneRoads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 3.2f, Points = roadPts, EndRound = 0.4f });

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
while (spots.Count < 24 && tries++ < 3000)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 14f + R() * 40f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r - 4f;
    // monsters may stand on the pond (the ducks of floor 15 like it), not in the plaza or on the path
    var onPond = new UnityEngine.Vector2(x - pond.x, z - pond.z).magnitude < pondRadius - 2f;
    if (!onPond && !Clear(x, z, 3f)) continue;
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
var models = new[] { "iceheim-viking", "home-bunny", "ravebridge-skeleton", "ravebridge-tormented-soul", "ironhill-lord-quackers", "iceheim-viking-chief", "ironhill-dragon-white", "kyojin-samurai", "home-cow" };
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

var scenePath = folder + "/Tower_Glacier.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-glacier-build.txt"), log.ToString());
return null;
