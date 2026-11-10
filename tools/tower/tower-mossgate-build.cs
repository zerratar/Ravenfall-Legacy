// Builds Assets/Scenes/Tower/Tower_Mossgate.unity: Mossgate, the town at the tower's foot (docs/tower-design.md,
// section 4). A mossy hamlet in a forest clearing: a cobbled square with a well, the Sleepy Stump inn, a few houses,
// Bramble's herb hut and garden, and at the north end the tower's great gate in a fortress wall with the tower rising
// behind it. Its four people (IslandNpc), stroll spots, roads drawn with the village road tool's roads (SceneRoads:
// cobbles round the square, mossy flagstones up to the gate, dirt lanes), its own navmesh. At (600, 0, -6000), beside
// whatever floor is loaded. Also makes Resources/Roads/Road_4 (mossy flagstones) when it is missing.
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
town.SkyAmbient = new UnityEngine.Color(0.62f, 0.62f, 0.52f);
town.EquatorAmbient = new UnityEngine.Color(0.48f, 0.46f, 0.36f);
town.GroundAmbient = new UnityEngine.Color(0.20f, 0.18f, 0.12f);
town.FogColor = new UnityEngine.Color(0.70f, 0.68f, 0.55f);
town.FogDensity = 0.007f;
town.SunColor = new UnityEngine.Color(1f, 0.86f, 0.64f);
town.SunIntensity = 1.2f;
town.SunAngles = new UnityEngine.Vector3(32f, -55f, 0f);

// ---- the ground ----
const float size = 220f, height = 32f, baseHeight = 2f, flatRadius = 50f;
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
// an invisible box over a building's bounds, so the navmesh goes round it
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
var fvb = "Assets/Synty/PolygonFantasyVillage/Prefabs/Buildings/Preset/";
var fvp = "Assets/Synty/PolygonFantasyVillage/Prefabs/Props/";
var fve = "Assets/Synty/PolygonFantasyVillage/Prefabs/Environment/";
var dfb = "Assets/Synty/PolygonDarkFortress/Prefabs/Buildings/";
var dfe = "Assets/Synty/PolygonDarkFortress/Prefabs/Environment/";
var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";

// the square, the inn, houses and the herb hut
Place(fvp + "SM_Prop_Well_01.prefab", 0f, 0f, 1.2f, 20f, props);
var inn = Place(fvb + "Bld_Preset_Tavern_01.prefab", -21f, 9f, 1f, Facing(-21f, 9f, 0f, 0f), buildings); Block(inn);
var houses = new[] { ("Bld_Preset_House_01", 19f, 13f), ("Bld_Preset_House_03", 22f, -9f), ("Bld_Preset_House_02", -22f, -14f), ("Bld_Preset_House_04", 30f, 26f), ("Bld_Preset_Smithy_01", -33f, 25f) };
foreach (var (h, x, z) in houses) Block(Place(fvb + h + ".prefab", x, z, 1f, Facing(x, z, 0f, 0f), buildings));
var hut = Place(fvb + "Bld_Preset_Shed_02.prefab", 13f, -27f, 1.1f, Facing(13f, -27f, 0f, -10f), buildings); Block(hut);
// Bramble's garden: shelves, pots, potions, flowers
Place(fvp + "SM_Prop_Garden_Shelf_01.prefab", 19f, -24f, 1f, Facing(19f, -24f, 0f, -10f), props);
Place(fvp + "SM_Prop_Table_03.prefab", 8f, -22f, 1f, 30f, props);
for (var i = 0; i < 4; i++) Place(fvp + "SM_Prop_Potion_0" + (1 + i % 2) + ".prefab", 8f + (R() - 0.5f) * 1.2f, -22f + (R() - 0.5f) * 0.8f, 1f, -1, props).transform.position += UnityEngine.Vector3.up * 0.95f;
for (var i = 0; i < 10; i++) Place(fvp + "SM_Prop_Pot_0" + (1 + rng.Next(5)) + ".prefab", 6f + R() * 14f, -30f + R() * 3f, 1f, -1, props);
for (var i = 0; i < 18; i++) Place(pnb + "SM_Env_Wildflowers_01.prefab", 5f + R() * 16f, -33f + R() * 6f, 1f + R() * 0.4f, -1, foliage);
// the inn's front: benches, tables, barrels, a sign
Place(fvp + "SM_Prop_Picnic_Table_01.prefab", -12f, 4f, 1f, 60f, props);
Place(fvp + "SM_Prop_Picnic_Table_01.prefab", -13f, 13f, 1f, 100f, props);
for (var i = 0; i < 5; i++) Place(fvp + (i % 2 == 0 ? "SM_Prop_Barrel_01" : "SM_Prop_Wine_Barrel_01") + ".prefab", -15f + R() * 2f, 17f + R() * 3f, 1f, -1, props);
Place(fvp + "SM_Prop_Sign_0" + (1 + rng.Next(9)) + ".prefab", -14f, 8f, 1.2f, Facing(-14f, 8f, 0f, 0f), props);
// benches and lanterns round the square
for (var i = 0; i < 6; i++)
{
    var a = i / 6f * UnityEngine.Mathf.PI * 2f + 0.3f;
    var x = UnityEngine.Mathf.Cos(a) * 10.5f; var z = UnityEngine.Mathf.Sin(a) * 10.5f;
    if (z > 7f && UnityEngine.Mathf.Abs(x) < 4f) continue; // the way to the gate
    Place(fvp + (i % 2 == 0 ? "SM_Prop_Bench_01" : "SM_Prop_Lantern_0" + (1 + rng.Next(3))) + ".prefab", x, z, 1.1f, Facing(x, z, 0f, 0f), props);
}
for (var i = 0; i < 10; i++) Place(fvp + "SM_Prop_Sign_Pole_0" + (1 + rng.Next(2)) + ".prefab", (R() - 0.5f) * 60f, (R() - 0.5f) * 60f, 1f, -1, props);

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
// lanterns at the gate
Place("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab", -5f, gateZ - 4f, 1.4f, 0f, props);
Place("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab", 5f, gateZ - 4f, 1.4f, 0f, props);

// trees round the clearing and up the hills, bushes and grass, a few rocks
var trees = new[] { "SM_Env_Tree_Meadow_01", "SM_Env_Tree_Meadow_02", "SM_Env_Tree_Birch_01", "SM_Env_Tree_Birch_02", "SM_Env_Tree_Birch_03" };
for (var i = 0; i < 170; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 44f + R() * 50f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (z > 30f && UnityEngine.Mathf.Abs(x) < 50f && z < 80f) continue; // the fortress
    Place(pnb + trees[rng.Next(trees.Length)] + ".prefab", x, z, 1f + R() * 0.6f, -1, foliage, 0.3f);
}
var cover = new[] { "SM_Env_Grass_Med_Clump_01", "SM_Env_Grass_Med_Clump_02", "SM_Env_Grass_Tall_Clump_01", "SM_Env_Grass_Short_Clump_01", "SM_Env_Grass_Short_Clump_02", "SM_Env_Ground_Cover_01", "SM_Env_Ground_Cover_02", "SM_Env_Wildflowers_01" };
for (var i = 0; i < 420; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 12f + UnityEngine.Mathf.Sqrt(R()) * 70f;
    var x = UnityEngine.Mathf.Cos(a) * r; var z = UnityEngine.Mathf.Sin(a) * r;
    if (UnityEngine.Mathf.Abs(x) < 3.5f && z > 0f && z < gateZ) continue; // the road to the gate
    if (UnityEngine.Mathf.Abs(x) < 2.5f && z < -8f) continue; // the lane from the south
    Place(pnb + cover[rng.Next(cover.Length)] + ".prefab", x, z, 0.9f + R() * 0.5f, -1, foliage);
}
for (var i = 0; i < 30; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f; var r = 30f + R() * 18f;
    if (UnityEngine.Mathf.Sin(a) * r > 26f && UnityEngine.Mathf.Abs(UnityEngine.Mathf.Cos(a) * r) < 24f) continue; // the gate
    Place(pnb + (rng.Next(2) == 0 ? "SM_Env_Bush_0" + (1 + rng.Next(3)) : "SM_Env_Rock_0" + (1 + rng.Next(6))) + ".prefab", UnityEngine.Mathf.Cos(a) * r, UnityEngine.Mathf.Sin(a) * r, 0.9f + R() * 0.5f, -1, foliage, 0.1f);
}
log.AppendLine("placed " + placed);

// ---- roads, the village road tool's ----
var roadsGo = new UnityEngine.GameObject("Roads"); roadsGo.transform.SetParent(root.transform, false);
var roads = roadsGo.AddComponent<SceneRoads>();
System.Collections.Generic.List<UnityEngine.Vector2> P(params float[] xz) { var l = new System.Collections.Generic.List<UnityEngine.Vector2>(); for (var i = 0; i + 1 < xz.Length; i += 2) l.Add(new UnityEngine.Vector2(xz[i], xz[i + 1])); return l; }
// cobbles round the square (a closed ring: the first points again at the end)
var ring = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var i = 0; i <= 16; i++) { var a = i / 16f * UnityEngine.Mathf.PI * 2f; ring.Add(new UnityEngine.Vector2(UnityEngine.Mathf.Cos(a) * 7f, UnityEngine.Mathf.Sin(a) * 7f)); }
ring.Add(ring[1]);
roads.Roads.Add(new SceneRoads.Road { Type = 2, Width = 3.2f, Points = ring, StartRound = 0f, EndRound = 0f });
// mossy flagstones up to the gate
roads.Roads.Add(new SceneRoads.Road { Type = 4, Width = 3.6f, Points = P(0f, 7f, 0.6f, 16f, -0.8f, 26f, 0f, gateZ - 2f), EndRound = 0f });
// dirt lanes: from the south where climbers arrive, to the inn, the houses and the herb hut
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2.6f, Points = P(0f, -60f, 1.5f, -40f, -1f, -22f, 0f, -7f) });
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2.2f, Points = P(-6.5f, 2.5f, -11f, 6f, -15f, 8f) });
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2f, Points = P(6.5f, 2.5f, 12f, 8f, 15f, 11f) });
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2f, Points = P(6.6f, -2f, 13f, -5f, 17f, -7f) });
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2f, Points = P(-6.6f, -2.5f, -12f, -8f, -17f, -12f) });
roads.Roads.Add(new SceneRoads.Road { Type = 0, Width = 2f, Points = P(1f, -18f, 6f, -21f, 10f, -24f) });
roads.Ground = 1 << 0;
// no terrain grass clumps on the roads were placed above; also keep props off them by hand (done in the loops)

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
Person("mossbeard", "SM_Chr_Bartender_01", -13f, 6.5f, 0f, 0f, 1.05f);

// ---- arrival and strolls ----
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = OnGround(0f, -30f);
town.SpawnPoint = spawn.transform;
var strolls = new UnityEngine.GameObject("Strolls").transform; strolls.SetParent(root.transform, false);
town.Strolls = strolls;
var strollSpots = new[] { (5f, -4f), (-5f, 4f), (-4f, -5f), (6f, 5f), (-11f, 9f), (-10f, 2f), (5f, -18f), (11f, -18f), (2f, 22f), (-2f, 30f), (13f, 6f), (-14f, -6f), (0f, -14f), (16f, -2f), (-16f, 3f) };
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
foreach (var (x, z) in strollSpots.Take(4).Concat(new[] { (3.5f, gateZ - 6f) }))
{
    var path = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.SamplePosition(OnGround(0f, -30f), out var a, 3f, -1);
    UnityEngine.AI.NavMesh.SamplePosition(OnGround(x, z), out var b, 3f, -1);
    UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, -1, path);
    log.AppendLine("path to " + x + "," + z + ": " + path.status);
}

var scenePath = folder + "/Tower_Mossgate.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-mossgate-build.txt"), log.ToString());
return null;
