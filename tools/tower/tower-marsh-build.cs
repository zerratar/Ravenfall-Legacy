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
var dataFolder = folder + "/Tower_Marsh";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes", "Tower");
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_Marsh");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Drowned Marsh");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 115f;
floor.SkyAmbient = new UnityEngine.Color(0.40f, 0.48f, 0.40f);
floor.EquatorAmbient = new UnityEngine.Color(0.40f, 0.45f, 0.34f);
floor.GroundAmbient = new UnityEngine.Color(0.17f, 0.16f, 0.11f);
// a green haze, thin enough that the drowned hall's walls show (concept-marsh)
floor.FogColor = new UnityEngine.Color(0.40f, 0.52f, 0.42f);
floor.FogDensity = 0.0058f;
// its life (Ambience, with its setting): fireflies over the water
floor.Fireflies = 40f; floor.Butterflies = 1f;
floor.SunColor = new UnityEngine.Color(0.85f, 0.90f, 0.70f);
floor.SunIntensity = 0.95f;
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
    // pools of black water in the hollows, with steep banks so nobody walks into them
    var pool = UnityEngine.Mathf.InverseLerp(0.56f, 0.40f, Noise(x + 300f, z - 200f, 0.045f));
    var keep = UnityEngine.Mathf.Max(UnityEngine.Mathf.InverseLerp(plazaRadius + 12f, plazaRadius + 4f, dp), UnityEngine.Mathf.InverseLerp(9f, 5f, DistToPath(new UnityEngine.Vector3(x, 0, z))));
    keep = UnityEngine.Mathf.Max(keep, UnityEngine.Mathf.InverseLerp(16f, 10f, new UnityEngine.Vector2(x - spawnLocal.x, z - spawnLocal.z).magnitude));
    pool *= 1f - keep;
    pool *= 1f - UnityEngine.Mathf.InverseLerp(flatRadius - 8f, flatRadius + 4f, r);
    h -= UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.Clamp01(pool * 3f)) * 2.8f;
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
var layerNames = new[] { "Dirt_Cracked_Leaves_Heavy_01", "Moss_01", "Cobblestone_01", "Mud_Debris_01", "Rock_Moss_01", "Footpath_Tile_01", "Mud_01" };
var layers = layerNames.Select(n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(L(n))).ToArray();
foreach (var i in Enumerable.Range(0, layers.Length)) if (!layers[i]) log.AppendLine("missing layer " + layerNames[i]);
td.terrainLayers = layers.Where(l => l).ToArray();
var amRes = 256;
td.alphamapResolution = amRes;
var maps = new float[amRes, amRes, td.terrainLayers.Length];
int Idx(string n) => System.Array.IndexOf(td.terrainLayers, layers[System.Array.IndexOf(layerNames, n)]);
var iGrass = Idx("Dirt_Cracked_Leaves_Heavy_01"); var iMoss = Idx("Moss_01"); var iRuin = Idx("Cobblestone_01"); var iMud = Idx("Mud_Debris_01");
var iRock = Idx("Rock_Moss_01"); var iPath = Idx("Footpath_Tile_01"); var iGrass2 = Idx("Mud_01");
for (var zi = 0; zi < amRes; zi++)
    for (var xi = 0; xi < amRes; xi++)
    {
        var x = (xi + 0.5f) / amRes * size - size / 2f;
        var z = (zi + 0.5f) / amRes * size - size / 2f;
        var w = new float[td.terrainLayers.Length];
        var steep = td.GetSteepness((xi + 0.5f) / amRes, (zi + 0.5f) / amRes);
        w[iGrass] = 1f;
        if (iGrass2 >= 0) w[iGrass2] = UnityEngine.Mathf.Clamp01((Noise(x, z, 0.06f) - 0.45f) * 3f);
        if (iMoss >= 0) w[iMoss] = UnityEngine.Mathf.Clamp01((Noise(x + 100, z, 0.045f) - 0.4f) * 3f);
        if (iMud >= 0) w[iMud] = UnityEngine.Mathf.Clamp01((Noise(x, z + 50, 0.08f) - 0.5f) * 3f);
        { var hh = GroundAt(x, z); if (iGrass2 >= 0 && hh < -0.2f) w[iGrass2] += 4f; }
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

// ---- the hall (tools/tower/blender/tower_halls.py, concept docs/mockups/tower/concept-marsh.jpg): this band is the Overgrown
// Halls, inside the tower, so the floor stands in a vast ruined hall rather than under open hills: tall walls with pointed
// windows, broken at the top, whole stretches fallen, giant roots over them. They show from inside only, so the camera
// over the players is never walled off. The FBX comes in turned half round: turned back. Casts no shadow (the walls would
// shade half the floor at this sun).
{
    UnityEngine.Material HallMat(string name, UnityEngine.Texture2D tex, UnityEngine.Texture2D normal, UnityEngine.Color tint)
    {
        var path = dataFolder + "/" + name + ".mat";
        var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (!m) { m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Smoothness", 0.05f);
        UnityEditor.EditorUtility.SetDirty(m);
        return m;
    }
    var ae = "Assets/PolygonAncientEmpire/Textures/BuildingAlts/";
    var stoneTex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(ae + "Building_Large_Brick_01.png");
    var stoneNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(ae + "Building_Wall_01_Normals.png");
    var mossLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Terrain_Meadow_Moss_01.terrainlayer");
    if (!stoneTex) log.AppendLine("missing hall stone texture");
    var hallMats = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>
    {
        ["Stone"] = HallMat("HallStone", stoneTex, stoneNormal, new UnityEngine.Color(0.86f, 0.90f, 0.82f)),
        ["Moss"] = HallMat("HallMoss", mossLayer ? mossLayer.diffuseTexture : null, null, new UnityEngine.Color(0.85f, 0.95f, 0.75f)),
        ["Root"] = HallMat("HallRoot", null, null, new UnityEngine.Color(0.34f, 0.24f, 0.15f)),
    };
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/Halls/Models/MarshHall.fbx");
    if (!prefab) log.AppendLine("missing model MarshHall (run tools/tower/blender/tower_halls.py)");
    else
    {
        var hall = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
        hall.name = "Hall Walls";
        hall.transform.SetParent(root.transform, false);
        hall.transform.localPosition = UnityEngine.Vector3.zero;
        hall.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
        foreach (var r in hall.GetComponentsInChildren<UnityEngine.MeshRenderer>())
        {
            var ms = r.sharedMaterials;
            for (var k = 0; k < ms.Length; k++) { var key = ms[k] ? ms[k].name.Split(' ')[0].Split('.')[0] : ""; if (hallMats.TryGetValue(key, out var mm)) ms[k] = mm; }
            r.sharedMaterials = ms;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}

// sunlight pouring in through the hall's windows on the sun's side (TowerLightShaft; off in potato mode). The windows are
// tower_halls.py's: 28 bays, none where a stretch has fallen (its gaps, in Blender's angles; the model is turned half
// round, so Unity's angle is Blender's plus 180), the inside of the wall 74 m out, the openings' middles about 10.5 m up.
{
    var sunFwd = UnityEngine.Quaternion.Euler(floor.SunAngles) * UnityEngine.Vector3.forward;
    var toSun = new UnityEngine.Vector3(-sunFwd.x, 0f, -sunFwd.z).normalized;
    var gaps = new[] { -90f, 60f, 120f, 250f, 330f };
    var shaftsT = new UnityEngine.GameObject("Light Shafts").transform; shaftsT.SetParent(root.transform, false);
    var shaftCount = 0;
    for (var i = 0; i < 28; i++)
    {
        var blenderDeg = (i + 0.5f) / 28f * 360f;
        var fallen = 0f;
        foreach (var g in gaps) fallen = UnityEngine.Mathf.Max(fallen, 1f - UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(blenderDeg, g)) / 11f);
        if (fallen > 0.5f) continue;
        var a = (blenderDeg + 180f) * UnityEngine.Mathf.Deg2Rad;
        var outward = new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a));
        if (UnityEngine.Vector3.Dot(outward, toSun) < 0.3f) continue;
        var at = outward * 74.3f + UnityEngine.Vector3.up * 10.5f;
        var shaft = new UnityEngine.GameObject("Light Shaft");
        shaft.transform.SetParent(shaftsT, false);
        shaft.transform.localPosition = at;
        shaft.transform.rotation = UnityEngine.Quaternion.LookRotation(sunFwd, UnityEngine.Vector3.up);
        var ls = shaft.AddComponent<TowerLightShaft>();
        ls.Width = 4.4f; ls.Height = 11f; ls.Length = 10.5f / UnityEngine.Mathf.Max(0.2f, -sunFwd.y) + 3f;
        ls.Tint = new UnityEngine.Color(0.86f, 1f, 0.62f, 0.24f);
        shaftCount++;
    }
    log.AppendLine("light shafts " + shaftCount);
}

// ---- props ----
var rng = new System.Random(37);
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
var trees = new[] { "SM_Env_Tree_Meadow_02", "SM_Env_Tree_Birch_01", "SM_Env_Tree_Birch_03", "SM_Env_Tree_Meadow_02" };
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
// the marsh: dead trees in the clearing's edges, reeds round the pools, the water, mist, fireflies and frogs
var dr = "Assets/PolygonDungeonRealms/Prefabs/Environments/";
for (var i = 0; i < 40; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 40f + R() * 26f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 6f)) continue;
    Place(dr + "SM_Env_Bramble_0" + (1 + rng.Next(3)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.4f, 1.6f + R() * 1.4f, -1, foliage);
}
var reedCount = 0;
for (var i = 0; i < 3000 && reedCount < 260; i++)
{
    var x = (R() - 0.5f) * 120f; var z = (R() - 0.5f) * 120f;
    var hh = GroundAt(x, z);
    if (hh > -0.5f || hh < -1.5f) continue; // the banks
    Place("Assets/PolygonNature/Prefabs/Plants/SM_Plant_Reeds_0" + (1 + rng.Next(2)) + ".prefab", OnGround(x, z), 0.9f + R() * 0.7f, -1, foliage);
    reedCount++;
}
log.AppendLine("reeds " + reedCount);
// the water: a plain dark, glossy surface (the nature pack's water shader is not made for this pipeline)
var water = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
water.name = "Marsh Water";
UnityEngine.Object.DestroyImmediate(water.GetComponent<UnityEngine.Collider>());
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(water, scene);
water.transform.SetParent(props, false);
water.transform.position = origin + new UnityEngine.Vector3(0f, -0.95f, 0f);
water.transform.localScale = new UnityEngine.Vector3(14f, 1f, 14f);
var waterMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
waterMat.name = "Marsh Water";
waterMat.SetColor("_BaseColor", new UnityEngine.Color(0.09f, 0.13f, 0.09f, 1f));
waterMat.SetFloat("_Smoothness", 0.92f);
waterMat.SetFloat("_Metallic", 0.1f);
UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/Water.mat");
UnityEditor.AssetDatabase.CreateAsset(waterMat, dataFolder + "/Water.mat");
var waterRenderer = water.GetComponent<UnityEngine.MeshRenderer>();
waterRenderer.sharedMaterial = waterMat;
waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
// lily pads and floating leaves on it
for (var i = 0; i < 2000 && i < 60000; i++)
{
    var x = (R() - 0.5f) * 120f; var z = (R() - 0.5f) * 120f;
    if (GroundAt(x, z) > -1.4f) continue;
    if (rng.Next(4) != 0) continue;
    // real lily pads now (the pick by count, not by a random number, so the props after keep their places)
    var leaf = Place("Assets/PolygonAdventure/Prefabs/Environments/SM_Env_Lillypads_0" + (1 + i % 3) + ".prefab", origin + new UnityEngine.Vector3(x, -0.92f, z), 0.6f + R() * 0.5f, -1, foliage);
}
// mangroves (tools/tower/blender/tower_mangroves.py, concept-marsh: twisted roots arching out of the water) standing in the
// pools and on their banks, off the paths, the plaza and the arrival; their own random numbers, so the rest keeps its place
{
    UnityEngine.Material TreeMat(string name, UnityEngine.Color c)
    {
        var path = dataFolder + "/" + name + ".mat";
        var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (!m) { m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.1f);
        UnityEditor.EditorUtility.SetDirty(m);
        return m;
    }
    var treeMats = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>
    {
        ["Bark"] = TreeMat("MangroveBark", new UnityEngine.Color(0.34f, 0.25f, 0.17f)),
        ["Leaves"] = TreeMat("MangroveLeaves", new UnityEngine.Color(0.20f, 0.38f, 0.15f)),
        ["LeavesLight"] = TreeMat("MangroveLeavesLight", new UnityEngine.Color(0.34f, 0.50f, 0.20f)),
    };
    var mrng = new System.Random(77);
    var placedTrees = new System.Collections.Generic.List<UnityEngine.Vector2>();
    for (var i = 0; i < 4000 && placedTrees.Count < 18; i++)
    {
        var x = ((float)mrng.NextDouble() - 0.5f) * 124f; var z = ((float)mrng.NextDouble() - 0.5f) * 124f;
        var hh = GroundAt(x, z);
        if (hh > -0.7f || hh < -2.6f) continue;
        if (!Clear(x, z, 4f)) continue;
        var p2 = new UnityEngine.Vector2(x, z);
        if (placedTrees.Any(q => (q - p2).magnitude < 10f)) continue;
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/Halls/Models/Mangrove_0" + (1 + placedTrees.Count % 4) + ".fbx");
        if (!prefab) { log.AppendLine("missing mangrove model (run tools/tower/blender/tower_mangroves.py)"); break; }
        var tree = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
        tree.name = "Mangrove";
        tree.transform.SetParent(foliage, false);
        tree.transform.position = origin + new UnityEngine.Vector3(x, -0.95f, z);
        tree.transform.rotation = UnityEngine.Quaternion.Euler(0f, (float)mrng.NextDouble() * 360f, 0f);
        tree.transform.localScale = UnityEngine.Vector3.one * (0.95f + (float)mrng.NextDouble() * 0.4f);
        foreach (var r in tree.GetComponentsInChildren<UnityEngine.MeshRenderer>())
        {
            var ms = r.sharedMaterials;
            for (var k = 0; k < ms.Length; k++) { var key = ms[k] ? ms[k].name.Split(' ')[0].Split('.')[0] : ""; if (treeMats.TryGetValue(key, out var mm)) ms[k] = mm; }
            r.sharedMaterials = ms;
        }
        placedTrees.Add(p2);
    }
    log.AppendLine("mangroves " + placedTrees.Count);
}

var fx = "Assets/PolygonNatureBiomes/PNB_Swamp_Marshland/FX/FX_Prefabs/";
foreach (var (name, count) in new[] { ("FX_Fog_01", 8), ("FX_Fireflies_01", 6), ("FX_Swamp_Bubbles_01", 5), ("FX_Dragonflies_01", 3), ("FX_Frogs_01", 3) })
    for (var i = 0; i < count; i++)
    {
        var a = R() * UnityEngine.Mathf.PI * 2f; var r = 15f + R() * 40f;
        var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
        Place(fx + name + ".prefab", OnGround(x, z) + UnityEngine.Vector3.up * 0.3f, 1f, -1, props);
    }

// ground cover: grass, flowers, bushes, leaves
var cover = new[] { "SM_Env_Grass_Med_Clump_01", "SM_Env_Grass_Med_Clump_02", "SM_Env_Grass_Med_Clump_03", "SM_Env_Grass_Tall_Clump_01", "SM_Env_Grass_Tall_Clump_03", "SM_Env_Grass_Tall_Clump_05", "SM_Env_Grass_Short_Clump_01", "SM_Env_Grass_Short_Clump_02", "SM_Env_Grass_Short_Clump_03", "SM_Env_Grass_Large_01", "SM_Env_Grass_Large_03", "SM_Env_Ground_Cover_01", "SM_Env_Ground_Cover_02", "SM_Env_Ground_Cover_03", "SM_Prop_Leaves_Pile_01", "SM_Env_Wildflowers_01" };
for (var i = 0; i < 300; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = UnityEngine.Mathf.Sqrt(R()) * 78f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude < plazaRadius - 2f) continue;
    if (DistToPath(new UnityEngine.Vector3(x, 0, z)) < 1.8f) continue;
    if (GroundAt(x, z) < -0.4f) continue;
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
    if (rng.Next(5) < 3) continue;
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
    if (GroundAt(x, z) < -0.15f) continue;
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
// a spot the players cannot walk to (beyond a pool's steep banks) moves to the nearest place they can reach, so no
// monster stands where nobody can fight it
{
    UnityEngine.AI.NavMesh.SamplePosition(spawn.transform.position, out var from, 4f, -1);
    var moved = 0;
    foreach (UnityEngine.Transform spot in spawns)
    {
        if (!UnityEngine.AI.NavMesh.SamplePosition(spot.position, out var at, 6f, -1)) continue;
        var path = new UnityEngine.AI.NavMeshPath();
        UnityEngine.AI.NavMesh.CalculatePath(from.position, at.position, -1, path);
        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete || path.corners.Length == 0) continue;
        spot.position = path.corners[path.corners.Length - 1];
        moved++;
    }
    log.AppendLine("spots moved within reach: " + moved);
}

var scenePath = folder + "/Tower_Marsh.unity";
log.AppendLine(root.transform.childCount + " children under the floor");
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
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-marsh-build.txt"), log.ToString());
return null;
