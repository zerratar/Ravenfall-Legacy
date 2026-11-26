// Builds Assets/Scenes/Tower/Tower_StormSpire.unity: the third band's summit layout ("The Storm Spire"), the paved top
// of the spire above the clouds at (0, 0, -6000), for the gallery, the arena of winds, the sentinels' stair and the eye
// of the storm. Old flagstones ringed by broken colonnades and ruined towers; six lightning rods round the arena that
// the storm strikes more often than at the docks (TowerLightning); chunks of the spire floating off into the clouds; a
// darker storm sky (TowerSky); 24 monster spots; the boss's spot on the raised dais; one hidden template per monster
// look; its own navmesh. Editor only, not in play mode. Writes the result to %TEMP%/tower-stormspire-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_StormSpire";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_StormSpire");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Storm Spire Summit");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 120f;
// a storm above the clouds: cold blue-grey light, pale cloud-coloured fog far below, a dim silver sun
// in the heart of the storm: darker and bluer than the docks, a weak sun low behind the clouds
floor.SkyAmbient = new UnityEngine.Color(0.38f, 0.41f, 0.55f);
floor.EquatorAmbient = new UnityEngine.Color(0.31f, 0.32f, 0.42f);
floor.GroundAmbient = new UnityEngine.Color(0.17f, 0.17f, 0.21f);
floor.FogColor = new UnityEngine.Color(0.50f, 0.53f, 0.63f);
floor.FogDensity = 0.0048f;
// its storm (WeatherEffects, with the changing weather and lightning flash settings): lightning in the clouds round the
// spire every ten seconds or so, thunder, a bolt now and then (0.55 put the strikes 3 km out, too far to see)
floor.Storm = 0.85f;
// and rain driven across it on the wind
floor.Rain = 0.55f;
floor.SunColor = new UnityEngine.Color(0.78f, 0.83f, 1f);
floor.SunIntensity = 0.75f;
floor.SunAngles = new UnityEngine.Vector3(38f, -120f, 0f);
// the haze floor (the cloud sea) far below the rim, so the cliffs have somewhere to fall
floor.HazeDepth = 66f;
// the world's volumetric clouds as the sea under the rock; the mesh puffs stand in while they are off
floor.CloudSea = true; floor.CloudSeaTop = -10f; floor.CloudSeaBottom = -56f; floor.CloudSeaCoverage = 0.6f; floor.CloudSeaPuff = 55f; floor.CloudSeaDark = 0.55f;

// ---- the layout (local to the root) ----
var spawnLocal = new UnityEngine.Vector3(0f, 0f, -50f);
var plaza = new UnityEngine.Vector3(0f, 0f, 14f);
var dockFoot = new UnityEngine.Vector3(0f, 0f, 14f);    // no docks up here: the second path ends in the plaza
const float plazaRadius = 15f;
const float size = 260f, height = 120f, baseHeight = 70f;
float Noise(float x, float z, float s) => UnityEngine.Mathf.PerlinNoise(x * s + 31.7f, z * s + 77.3f);
float DistToSegment(UnityEngine.Vector3 p, UnityEngine.Vector3 a, UnityEngine.Vector3 b)
{
    var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0;
    var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    var c = a + ab * t; c.y = 0; var q = p; q.y = 0;
    return (q - c).magnitude;
}
float DistToPath(UnityEngine.Vector3 p) => DistToSegment(p, spawnLocal, plaza);
// the rock's edge: an uneven ring, pushed out to the north for the docks
float Edge(float x, float z)
{
    var a = UnityEngine.Mathf.Atan2(z, x);
    var r = 56f + (Noise(UnityEngine.Mathf.Cos(a) * 40f, UnityEngine.Mathf.Sin(a) * 40f, 0.05f) - 0.5f) * 8f;
    // the south (the arrival) reaches a little farther
    r += 5f * UnityEngine.Mathf.Max(0f, -UnityEngine.Mathf.Sin(a));
    return r;
}
float GroundAt(float x, float z)
{
    var r = UnityEngine.Mathf.Sqrt(x * x + z * z);
    var edge = Edge(x, z);
    // gentle rolls on the rock, flattened round the plaza and along the paths
    var h = (Noise(x, z, 0.035f) - 0.5f) * 0.6f + (Noise(x, z, 0.11f) - 0.5f) * 0.2f;
    // the dais: the plaza a step up
    var dd = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    h += 0.6f * UnityEngine.Mathf.InverseLerp(plazaRadius + 2f, plazaRadius - 1f, dd);
    var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    var flat = UnityEngine.Mathf.Max(UnityEngine.Mathf.InverseLerp(plazaRadius + 6f, plazaRadius, dp), UnityEngine.Mathf.InverseLerp(6f, 2f, DistToPath(new UnityEngine.Vector3(x, 0, z))));
    h = UnityEngine.Mathf.Lerp(h, UnityEngine.Mathf.Max(h, 0.05f), flat);
    // a raised lip just inside the edge, then the cliff: straight down into the clouds
    var lip = UnityEngine.Mathf.InverseLerp(edge - 10f, edge - 2f, r) * (1f - UnityEngine.Mathf.InverseLerp(edge - 2f, edge, r));
    h += lip * 1.2f;
    // past the rim the terrain is cut (holes), the rock under it is Blender's
    h -= 0.4f * UnityEngine.Mathf.InverseLerp(edge - 1.5f, edge, r);
    return h;
}

// the rim for Blender (tools/tower/blender/tower_islands.py makes the rock under it from this): [degrees from east toward
// north, radius] every 5 degrees; the terrain is cut along the same line below
{
    var rimSb = new System.Text.StringBuilder("{\"note\": \"The Storm Spire rim: [degrees from east toward north, radius in metres]; written by tools/tower/tower-stormspire-build.cs\", \"rim\": [");
    for (var i = 0; i < 72; i++)
    {
        var deg = i * 5f; var ra = deg * UnityEngine.Mathf.Deg2Rad;
        var rr = Edge(UnityEngine.Mathf.Cos(ra) * 50f, UnityEngine.Mathf.Sin(ra) * 50f);
        rimSb.Append((i > 0 ? ", " : "") + "[" + deg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", " + rr.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "]");
    }
    rimSb.Append("]}");
    System.IO.File.WriteAllText(System.IO.Path.GetFullPath("tools/tower/stormspire-rim.json"), rimSb.ToString());
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
var res = 513;
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
        w[0] = 0.15f;
        w[1] = 0.05f;
        // old flagstones over the whole top, worn to cracked ground in patches
        w[4] = 1.4f * UnityEngine.Mathf.Clamp01(1.25f - Noise(x + 13f, z, 0.07f));
        w[5] += UnityEngine.Mathf.Clamp01((Noise(x, z, 0.09f) - 0.55f) * 3f);
        w[5] = UnityEngine.Mathf.Clamp01((r - (Edge(x, z) - 8f)) / 6f) * 1.5f   // bare ground toward the wind-scoured edge
            + UnityEngine.Mathf.Clamp01((r - (Edge(x, z) - 3.4f)) / 1.2f) * 14f;   // all dirt at the cut, to meet the collar
        w[2] = UnityEngine.Mathf.Clamp01((steep - 26f) / 10f) * 4f;
        var dp = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
        var ring = UnityEngine.Mathf.Clamp01((plazaRadius + 1f - dp) / 3f + (Noise(x, z, 0.3f) - 0.5f) * 0.6f);
        w[4] += ring * 1.8f;
        var path = UnityEngine.Mathf.Clamp01((2.8f - DistToPath(new UnityEngine.Vector3(x, 0, z))) / 1.6f + (Noise(x, z, 0.4f) - 0.5f) * 0.6f);
        w[3] = path * (1f - ring) * 1.3f;
        var sum = w.Sum();
        for (var k = 0; k < w.Length; k++) maps[zi, xi, k] = w[k] / sum;
    }
td.SetAlphamaps(0, 0, maps);
// cut along the rim, well inside it: the earth collar runs smooth over the hole's steps, the rock (Blender) holds it up
{
    var holeRes = res - 1;
    var solid = new bool[holeRes, holeRes];
    for (var zi = 0; zi < holeRes; zi++)
        for (var xi = 0; xi < holeRes; xi++)
        {
            var x = (xi + 0.5f) / holeRes * size - size / 2f; var z = (zi + 0.5f) / holeRes * size - size / 2f;
            solid[zi, xi] = UnityEngine.Mathf.Sqrt(x * x + z * z) < Edge(x, z) - 1.2f;
        }
    td.SetHoles(0, 0, solid);
}
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
var rng = new System.Random(37);
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
var meshClouds = new UnityEngine.GameObject("Mesh Clouds").transform; meshClouds.SetParent(sky, false);
floor.MeshClouds = meshClouds.gameObject;
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
var stormMat = Tinted("StormCloud", new UnityEngine.Color(0.26f, 0.28f, 0.36f));
void Cloud(UnityEngine.Vector3 world, float scale, UnityEngine.Material mat)
{
    var go = Place(adv + "SM_Env_Cloud_0" + (1 + rng.Next(6)) + ".prefab", world, scale, -1, meshClouds);
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

// chunks of the spire drifting off into the clouds: broken towers, walls and pillars, tilted
var ruinPath = "Assets/PolygonDungeonRealms/Prefabs/Buildings/";
var drift = new[] { "SM_Bld_Ruin_Tower_Round_Broken_01", "SM_Bld_Ruin_Wall_Broken_01", "SM_Bld_Ruin_Pillar_Broken_01", "SM_Bld_Ruin_Tiles_02", "SM_Bld_Ruin_Arch_Broken_01" };
for (var i = 0; i < 14; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 72f + R() * 70f;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 22f) continue;
    var at = origin + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a) * r, -8f + R() * 30f, UnityEngine.Mathf.Sin(a) * r);
    Bare(Place(ruinPath + drift[rng.Next(drift.Length)] + ".prefab", at, 1f + R() * 1.2f, -1, sky, (R() - 0.5f) * 70f, (R() - 0.5f) * 70f));
    if (rng.Next(3) == 0) Cloud(at + UnityEngine.Vector3.down * 4f, 3f + R() * 3f, seaMat);
}

// the broken colonnade round the summit: walls, arches, pillars and ruined towers near the edge, with gaps
var colonnade = new UnityEngine.GameObject("Colonnade").transform; colonnade.SetParent(root.transform, false);
for (var i = 0; i < 34; i++)
{
    var a = i / 34f * UnityEngine.Mathf.PI * 2f;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(a * UnityEngine.Mathf.Rad2Deg, -90f)) < 22f) continue; // the arrival
    if (rng.Next(7) == 0) continue; // fallen away
    var e = Edge(UnityEngine.Mathf.Cos(a) * 40f, UnityEngine.Mathf.Sin(a) * 40f) - 6f;
    var x = UnityEngine.Mathf.Cos(a) * e; var z = UnityEngine.Mathf.Sin(a) * e;
    var yaw = -a * UnityEngine.Mathf.Rad2Deg + 90f;
    var kind = rng.Next(6);
    var piece = kind == 0 ? "SM_Bld_Ruin_Arch_01" : kind == 1 ? "SM_Bld_Ruin_Wall_01" : kind == 2 ? "SM_Bld_Ruin_Wall_Broken_01"
        : kind == 3 ? "SM_Bld_Ruin_Tower_Round_Broken_01" : kind == 4 ? "SM_Bld_Ruin_Pillar_01" : "SM_Bld_Ruin_Pillar_Broken_0" + (1 + rng.Next(3));
    Place(ruinPath + piece + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.2f, kind >= 4 ? 2.3f : 1.7f, yaw, colonnade);
}
// statues of the storm's old wardens at the way in, and rubble about
foreach (var side in new[] { -1f, 1f })
    Place("Assets/Synty/PolygonVikingRealm/Prefabs/Props/SM_Prop_Statue_0" + (side < 0 ? 1 : 2) + ".prefab", OnGround(side * 5f, plaza.z - plazaRadius - 3f), 1.05f, 180f, colonnade);
for (var i = 0; i < 30; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 20f + R() * 32f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 2f)) continue;
    var kind = rng.Next(3);
    var piece = kind == 0 ? ruinPath + "SM_Bld_Ruin_Pillar_Broken_0" + (1 + rng.Next(3)) : kind == 1 ? pr + "Environment/SM_Env_Rock_Basalt_0" + (1 + rng.Next(2)) : ruinPath + "SM_Bld_Ruin_Tiles_0" + (1 + rng.Next(4));
    Place(piece + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * (kind == 2 ? 0.9f : 0.2f), kind == 0 ? 1.2f + R() * 0.6f : 0.6f + R() * 0.6f, -1, props, kind == 0 && rng.Next(2) == 0 ? 80f : 0f);
}

// tufts of grass in the cracks of the old stone (no colliders)
var grassT = new UnityEngine.GameObject("Grass").transform; grassT.SetParent(root.transform, false);
for (var i = 0; i < 90; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 8f + R() * 46f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (!Clear(x, z, 0.3f)) continue;
    var go = Place("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_Tall_0" + (1 + rng.Next(4)) + ".prefab", OnGround(x, z) + UnityEngine.Vector3.down * 0.05f, 0.5f + R() * 0.4f, -1, grassT);
    Bare(go);
    if (go) foreach (var rr in go.GetComponentsInChildren<UnityEngine.Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
}
// the rock under the rim (tools/tower/blender/tower_islands.py, concept docs/mockups/tower/concept-stormspire.jpg): a jagged
// layered cone with roots, as Galewharf has; the terrain is cut along the rim and an earth collar runs over the cut's steps.
// The FBX comes in turned half round: turned back. (The cliff props that dressed the old stretched cliffs are gone; their
// random draws stay so everything after keeps its place.)
for (var i = 0; i < 30; i++)
{
    R(); rng.Next(5); R(); R();
}
UnityEngine.Material RockMat(string name, UnityEngine.Texture2D tex, UnityEngine.Texture2D normal, UnityEngine.Color tint, float smooth)
{
    var path = dataFolder + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m) { m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    m.SetTexture("_BaseMap", tex);
    m.SetColor("_BaseColor", tint);
    if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
    m.SetFloat("_Smoothness", smooth);
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
}
var vtex = "Assets/Synty/PolygonVikingRealm/Textures/Env/";
var rockTex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(vtex + "Rock_01.png");
var rockNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(vtex + "Rock_01_Normals.png");
var dirtLayer = td.terrainLayers[5];
var rockMats = new System.Collections.Generic.Dictionary<string, UnityEngine.Material>
{
    ["Rock"] = RockMat("IslandRock", rockTex, rockNormal, new UnityEngine.Color(0.60f, 0.61f, 0.66f), 0.05f),
    ["Earth"] = RockMat("IslandEarth", dirtLayer ? dirtLayer.diffuseTexture : null, null, new UnityEngine.Color(0.82f, 0.72f, 0.62f), 0.05f),
    ["Root"] = RockMat("IslandRoot", null, null, new UnityEngine.Color(0.30f, 0.20f, 0.12f), 0.1f),
};
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/Sky/Models/StormSpireRock.fbx");
    if (!prefab) log.AppendLine("missing model StormSpireRock (run tools/tower/blender/tower_islands.py)");
    else
    {
        var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
        go.name = "StormSpireRock";
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new UnityEngine.Vector3(0f, 0.05f, 0f);
        go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
        foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>())
        {
            var ms = r.sharedMaterials;
            for (var k = 0; k < ms.Length; k++) { var key = ms[k] ? ms[k].name.Split(' ')[0].Split('.')[0] : ""; if (rockMats.TryGetValue(key, out var mm)) ms[k] = mm; }
            r.sharedMaterials = ms;
        }
    }
    // the earth collar: just under the ground out to the rock's top ring, smooth over the terrain hole's steps
    const int seg = 360;
    var rockTop = 0.05f - 0.25f;
    var rings = new[] { (-2.4f, true), (-0.5f, true), (0.65f, false) };
    var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uvs = new System.Collections.Generic.List<UnityEngine.Vector2>();
    for (var i = 0; i <= seg; i++)
    {
        var ra = i * 2f * UnityEngine.Mathf.PI / seg;
        var dx = UnityEngine.Mathf.Cos(ra); var dz = UnityEngine.Mathf.Sin(ra);
        var e = Edge(dx * 50f, dz * 50f);
        foreach (var (off, onGround) in rings)
        {
            var px = dx * (e + off); var pz = dz * (e + off);
            verts.Add(new UnityEngine.Vector3(px, onGround ? GroundAt(px, pz) - 0.05f : rockTop, pz));
            uvs.Add(new UnityEngine.Vector2(px, pz) / 4f);
        }
    }
    var tris = new System.Collections.Generic.List<int>();
    for (var i = 0; i < seg; i++)
        for (var k = 0; k < rings.Length - 1; k++)
        {
            int a0 = i * rings.Length + k, b0 = (i + 1) * rings.Length + k;
            tris.AddRange(new[] { a0, a0 + 1, b0, b0, a0 + 1, b0 + 1 });
        }
    var mesh = new UnityEngine.Mesh { name = "Rim Collar" };
    mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
    mesh.RecalculateNormals();
    if (mesh.normals[0].y < 0f) { tris.Reverse(); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); }
    mesh.RecalculateBounds();
    var meshPath = dataFolder + "/RimCollar.asset";
    UnityEditor.AssetDatabase.DeleteAsset(meshPath);
    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
    var collar = new UnityEngine.GameObject("Rim Collar");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(collar, scene);
    collar.transform.SetParent(root.transform, false);
    collar.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    collar.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = RockMat("RimEarth", dirtLayer ? dirtLayer.diffuseTexture : null, dirtLayer ? dirtLayer.normalMapTexture : null, UnityEngine.Color.white, 0f);
    collar.AddComponent<UnityEngine.MeshCollider>().sharedMesh = mesh;
}

// the storm ring: four lightning rods (an iron pillar each), braziers with blue fire between
var rods = new System.Collections.Generic.List<UnityEngine.Transform>();
for (var i = 0; i < 6; i++)
{
    var a = (30f + i * 60f) * UnityEngine.Mathf.Deg2Rad;
    var x = plaza.x + UnityEngine.Mathf.Cos(a) * (plazaRadius + 3f);
    var z = plaza.z + UnityEngine.Mathf.Sin(a) * (plazaRadius + 3f);
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
skyDome.Zenith = new UnityEngine.Color(0.12f, 0.13f, 0.19f);
skyDome.Horizon = floor.FogColor;
var lightning = root.AddComponent<TowerLightning>();
lightning.Rods = rods.ToArray();
lightning.CloudHeight = 70f;
lightning.MinGap = 4f; lightning.MaxGap = 11f; lightning.RodShare = 0.5f;
log.AppendLine("placed " + placed + ", rods " + rods.Count);

// ---- the roads: gravel from the arrival to the plaza and on to the docks ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var sceneRoads = roadsGo.AddComponent<SceneRoads>();
var roadPts = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 8; i++) { var t = i / 8f; var p = UnityEngine.Vector3.Lerp(spawnLocal, plaza + new UnityEngine.Vector3(0, 0, -plazaRadius + 1f), t); roadPts.Add(new UnityEngine.Vector2(p.x, p.z)); }
sceneRoads.Roads.Add(new SceneRoads.Road { Type = 1, Width = 3.2f, Points = roadPts, EndRound = 0.4f });

// the storm's sigil on the dais (concept docs/mockups/tower/concept-stormspire.jpg): two glowing rings and the spokes and
// marks between them, bright enough to bloom; storm guardians at the dais's four corners, facing the fight
{
    var sigilMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit")) { name = "Storm Sigil" };
    sigilMat.SetColor("_BaseColor", new UnityEngine.Color(0.35f, 1.3f, 3.2f));
    sigilMat.SetFloat("_Cull", 0f); // both sides: the spokes and marks wind the other way from the rings
    UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/StormSigil.mat");
    UnityEditor.AssetDatabase.CreateAsset(sigilMat, dataFolder + "/StormSigil.mat");
    var sv = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var st = new System.Collections.Generic.List<int>();
    float Lift(float x, float z) => GroundAt(plaza.x + x, plaza.z + z) + 0.04f;
    void Quad(UnityEngine.Vector2 a, UnityEngine.Vector2 b, UnityEngine.Vector2 c, UnityEngine.Vector2 d)
    {
        var i = sv.Count;
        foreach (var p in new[] { a, b, c, d }) sv.Add(new UnityEngine.Vector3(plaza.x + p.x, Lift(p.x, p.y), plaza.z + p.y));
        st.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
    }
    UnityEngine.Vector2 P(float deg, float r) => new UnityEngine.Vector2(UnityEngine.Mathf.Cos(deg * UnityEngine.Mathf.Deg2Rad), UnityEngine.Mathf.Sin(deg * UnityEngine.Mathf.Deg2Rad)) * r;
    foreach (var (r, w) in new[] { (11.6f, 0.3f), (8.2f, 0.22f), (4.0f, 0.18f) })
        for (var k = 0; k < 96; k++)
        {
            float a0 = k * 3.75f, a1 = (k + 1) * 3.75f;
            Quad(P(a0, r - w), P(a0, r + w), P(a1, r + w), P(a1, r - w));
        }
    for (var k = 0; k < 8; k++)
    {
        var a = k * 45f + 22.5f;
        // a spoke from the inner ring to the outer, and a bolt mark between the outer two rings
        var d = P(a, 1f); var side = new UnityEngine.Vector2(-d.y, d.x) * 0.34f;
        Quad(d * 4.2f - side, d * 4.2f + side, d * 8.0f + side, d * 8.0f - side);
        var m = d * 9.6f; var s2 = new UnityEngine.Vector2(-d.y, d.x) * 1.7f;
        Quad(m - d * 0.9f - s2 * 0.5f, m - d * 0.9f + s2 * 0.1f, m + d * 0.1f + s2 * 0.4f, m + d * 0.1f - s2 * 0.2f);
        Quad(m + d * 0.1f - s2 * 0.4f, m + d * 0.1f + s2 * 0.2f, m + d * 1.0f + s2 * 0.5f, m + d * 1.0f - s2 * 0.1f);
    }
    var mesh = new UnityEngine.Mesh { name = "Storm Sigil" };
    mesh.SetVertices(sv);
    mesh.SetTriangles(st, 0);
    mesh.RecalculateNormals();
    if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f) { st.Reverse(); mesh.SetTriangles(st, 0); mesh.RecalculateNormals(); }
    UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/StormSigil.asset");
    UnityEditor.AssetDatabase.CreateAsset(mesh, dataFolder + "/StormSigil.asset");
    var sigil = new UnityEngine.GameObject("Storm Sigil");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sigil, scene);
    sigil.transform.SetParent(root.transform, false);
    sigil.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    var sr = sigil.AddComponent<UnityEngine.MeshRenderer>(); sr.sharedMaterial = sigilMat; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    // its light on the dais
    var gl = new UnityEngine.GameObject("Sigil Light"); gl.transform.SetParent(root.transform, false); gl.transform.localPosition = new UnityEngine.Vector3(plaza.x, GroundAt(plaza.x, plaza.z) + 2.5f, plaza.z);
    var glight = gl.AddComponent<UnityEngine.Light>(); glight.type = UnityEngine.LightType.Point; glight.color = new UnityEngine.Color(0.45f, 0.75f, 1f); glight.range = 18f; glight.intensity = 2.2f; glight.shadows = UnityEngine.LightShadows.None;
    // the guardians, at the dais's corners (inside the plaza, where no monster spot goes), off the arrival path
    foreach (var deg in new[] { 45f, 135f, 225f, 315f })
    {
        var at = P(deg, 13.6f);
        var x = plaza.x + at.x; var z = plaza.z + at.y;
        var face = UnityEngine.Mathf.Atan2(-at.x, -at.y) * UnityEngine.Mathf.Rad2Deg;
        Place("Assets/PolygonDungeonRealms/Prefabs/Environments/SM_Env_Statue_0" + (deg < 180f ? 1 : 2) + ".prefab", OnGround(x, z), 1.5f, face, props);
    }
}

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
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 14f + R() * 27f; // inside the colonnade (its raised ruin tiles made pockets)
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r - 2f;
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
var models = new[] { "ravebridge-tormented-soul", "kyojin-ninja", "atria-gladiator", "eldara-elven-soldier", "ironhill-dragon-blue" };
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

var scenePath = folder + "/Tower_StormSpire.unity";
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
// fixed ids for every object (Ravenfall > Tower Places), then the owner's hand edits from tools/tower/layouts/<Scene>.edits.json put back (docs/tower-places-editor.md)
TowerPlaceEdits.AfterBuild(scene, scenePath, log);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-stormspire-build.txt"), log.ToString());
return null;
