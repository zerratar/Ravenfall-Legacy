// Run with: unity command eval_file --file tools/ravebridge/ravebridge-island.cs
//
// Makes Ravebridge, the tribute to RuneScape Classic's Lumbridge far to the west, a playable event island (phase 6a
// of docs/ravebridge-design.md). Decided with the user 2026-10-25: only the Ravebridge event takes players there;
// it is open to every level; viewers train as they always do and the streamer can play by hand.
//
// What it builds, all under "Playable" in WORLD/Islands/Ravebridge (run it again to rebuild):
// - the IslandController ((Island)9 "Ravebridge", open to every level, event only, no dock) with its trigger sphere,
//   the spawn in the castle courtyard and the camera's target;
// - the island's own navmesh surface (area "Ravebridge", 4) over the island only, with the sea and river bed not
//   walkable;
// - the places Lumbridge has and Ravebridge did not: the church and its graveyard, the general store, the castle
//   kitchen, the windmill to the north west, houses, the farm's chicken coop, the swamp to the south and its mine,
//   signposts;
// - the people the streamer talks to (IslandNpc): Duke Ravacio, the cook, the miller, the priest, the restless ghost,
//   the goblin generals, the bridge keeper and the shopkeeper;
// - training spots, copied from the main island's level 1 ones: trees north west of the castle, the swamp mine,
//   fishing in the river by the swamp, farm patches in the crop field, gathering by the coop, cooking in the kitchen,
//   crafting and alchemy in the store's yard;
// - the enemies: the goblin camp's goblins (the old rigs, given the pirates' enemy parts), cows in the cow field
//   (copies of the main island's) and the restless ghost in the graveyard. The old decorative ones are switched off.
// The island's navmesh is baked afterwards (tools/ravebridge/navmesh-bake.cs).
var sb = new System.Text.StringBuilder();
var stage = "start";
try
{

var islands = UnityEngine.GameObject.Find("============ WORLD/Islands");
var root = islands.transform.Find("Ravebridge");
var home = islands.transform.Find("Main Island");
var away = islands.transform.Find("Small Island");
if (!root || !home || !away) return "no Ravebridge, Main Island or Small Island";
root.gameObject.SetActive(true);

var old = root.Find("Playable");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var playable = new UnityEngine.GameObject("Playable");
playable.transform.SetParent(root, false);
playable.transform.position = UnityEngine.Vector3.zero;
playable.transform.rotation = UnityEngine.Quaternion.identity;
UnityEngine.Physics.SyncTransforms();

System.Func<string, UnityEngine.Transform> group = name =>
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(playable.transform, false);
    return go.transform;
};

// the terrain's height under a point (buildings stand on it), and the highest collider (spots stand on paths too)
System.Func<float, float, float> terrain = (x, z) =>
{
    foreach (var t in root.GetComponentsInChildren<UnityEngine.Terrain>())
    {
        var p = t.transform.position; var s = t.terrainData.size;
        if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return p.y + t.SampleHeight(new UnityEngine.Vector3(x, 0, z));
    }
    return 0f;
};
System.Func<float, float, float> ground = (x, z) =>
{
    var hits = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x, 120f, z), UnityEngine.Vector3.down, 200f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
    var y = terrain(x, z);
    var best = float.MaxValue;
    foreach (var h in hits)
    {
        if (h.collider.name == "Do Not Walk" || !h.collider.transform.IsChildOf(root)) continue;
        if (h.distance < best && h.point.y < y + 3f) { best = h.distance; y = h.point.y; }
    }
    return y;
};
System.Func<string, float, float, UnityEngine.Transform> point = (name, x, z) =>
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(playable.transform, false);
    go.transform.position = new UnityEngine.Vector3(x, ground(x, z), z);
    return go.transform;
};
System.Func<string, UnityEngine.Transform, float, float, float, UnityEngine.GameObject> place = (path, parent, x, z, yaw) =>
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { sb.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
    go.transform.position = new UnityEngine.Vector3(x, terrain(x, z), z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    return go;
};
const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";

// 1. the terrain keeps its own material for now: the Ravenfall terrain material reads the painted layers in the
// other islands' order, and on Ravebridge's it lost the roads, the desert and the dirt (tried 2026-10-25)

stage = "island";
// 2. the island: event only, no dock (it cannot be sailed to), open to every level
var sphere = root.GetComponent<UnityEngine.SphereCollider>();
if (!sphere) sphere = root.gameObject.AddComponent<UnityEngine.SphereCollider>();
var island = root.GetComponent<IslandController>();
if (!island) island = root.gameObject.AddComponent<IslandController>();
sphere.isTrigger = true;
sphere.center = root.InverseTransformPoint(new UnityEngine.Vector3(-1640f, 5f, 1610f));
sphere.radius = 240f;
island.Island = (RavenNest.Models.Island)9;
island.Identifier = "Ravebridge";
island.LevelRequirement = 1;
island.OpenToAllLevels = true;
island.EventOnly = true;
island.AllowRaidWar = false;
island.DockingArea = null;
island.NavMeshArea = 4;
island.SpawnPositionTransform = point("SpawnPosition", -1630f, 1555f);
island.CameraPanTarget = point("Camera Rotation Target", -1640f, 1580f);

// its own navmesh over the island only (its terrains reach far beyond it), in its own area
var surface = root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if (!surface) surface = root.gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
var worldSurface = UnityEngine.GameObject.Find("============ WORLD").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = worldSurface.agentTypeID;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Volume;
surface.center = root.InverseTransformPoint(new UnityEngine.Vector3(-1640f, 10f, 1610f));
surface.size = new UnityEngine.Vector3(480f, 140f, 480f);
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = worldSurface.layerMask;
surface.defaultArea = 4;
var seaFloor = new UnityEngine.GameObject("Sea Floor Not Walkable");
seaFloor.transform.SetParent(playable.transform, false);
seaFloor.transform.position = new UnityEngine.Vector3(-1640f, -22f, 1610f);
var seaVolume = seaFloor.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
seaVolume.size = new UnityEngine.Vector3(480f, 36f, 480f); // up to -4: the river and the sea
seaVolume.area = 1;

stage = "places";
// 3. the places Lumbridge has: north of the castle the church, the store and the kitchen, the windmill north west,
// the coop on the farm, the swamp south of the castle
var places = group("Places");
var church = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_Church_01_A_Optimized.prefab", places, -1700f, 1700f, 90f);
if (church) church.name = "Church";
var graveyard = group("Graveyard");
graveyard.SetParent(places, true);
var stones = new[] { "SM_Prop_Gravestone_01", "SM_Prop_Gravestone_02", "SM_Prop_Gravestone_01", "SM_Prop_Gravestone_02", "SM_Prop_Gravestone_01", "SM_Prop_Gravestone_02", "SM_Prop_Gravestone_01", "SM_Prop_Gravestone_02" };
for (var i = 0; i < stones.Length; i++)
{
    place(Kingdom + "Bonus/" + stones[i] + ".prefab", graveyard, -1678f + (i % 4) * 3.2f, 1692f + (i / 4) * 4f, 90f + (i * 37 % 11) - 5f);
}
place(Kingdom + "Props/SM_Prop_Cross_01.prefab", graveyard, -1666f, 1707f, 90f);
var store = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_05_Optimized.prefab", places, -1660f, 1655f, 180f);
if (store) store.name = "General Store";
place(Kingdom + "Props/SM_Prop_Market_Stall_02.prefab", places, -1660f, 1641f, 180f);
var kitchen = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_03_Optimized.prefab", places, -1716f, 1628f, 180f);
if (kitchen) kitchen.name = "Castle Kitchen";
var windmill = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_Windmill_01_Optimized.prefab", places, -1785f, 1745f, 135f);
if (windmill) windmill.name = "Windmill";
var house1 = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_01_A_Optimized.prefab", places, -1684f, 1645f, 180f);
if (house1) house1.name = "House (Bob's axes)";
var house2 = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_07_Optimized.prefab", places, -1738f, 1660f, 90f);
if (house2) house2.name = "House";
var coop = place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_Hut_01_Optimized.prefab", places, -1560f, 1758f, 0f);
if (coop) coop.name = "Chicken Coop";
place(Kingdom + "Props/SM_Prop_Hay_Pile_01.prefab", places, -1555f, 1752f, 30f);
place(Kingdom + "Props/SM_Prop_Hay_Bale_01.prefab", places, -1566f, 1752f, 70f);
place(Kingdom + "Props/SM_Prop_Direction_Sign_01.prefab", places, -1642f, 1628f, 45f);
place(Kingdom + "Props/SM_Prop_Direction_Sign_02.prefab", places, -1600f, 1690f, 200f);
var swamp = group("Swamp");
swamp.SetParent(places, true);
var swampRandom = new System.Random(6);
for (var i = 0; i < 16; i++)
{
    var x = -1735f + (float)swampRandom.NextDouble() * 80f;
    var z = 1415f + (float)swampRandom.NextDouble() * 55f;
    // mangrove roots and dead trees (PolygonNature's swamp growth has no URP materials)
    var swampProp = new[] { "Assets/PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Tree_Dead_01.prefab", "Assets/PolygonPirates/Prefabs/Environments/SM_Env_Mangrove_Roots_01.prefab", "Assets/PolygonPirates/Prefabs/Environments/SM_Env_Mangrove_Tree_01.prefab", "Assets/PolygonPirates/Prefabs/Environments/SM_Env_Mangrove_Tree_02.prefab" }[i % 4];
    place(swampProp, swamp, x, z, (float)swampRandom.NextDouble() * 360f);
}
// the swamp's ground: mud (layer 4) painted in, soft at the edges
foreach (var t in root.GetComponentsInChildren<UnityEngine.Terrain>())
{
    var data = t.terrainData;
    var mud = System.Array.FindIndex(data.terrainLayers, l => l && l.name.Contains("Mud"));
    if (mud < 0) continue;
    var res = data.alphamapResolution; var size = data.size; var origin = t.transform.position;
    int x0 = UnityEngine.Mathf.Clamp((int)((-1750f - origin.x) / size.x * res), 0, res), x1 = UnityEngine.Mathf.Clamp((int)((-1640f - origin.x) / size.x * res), 0, res);
    int z0 = UnityEngine.Mathf.Clamp((int)((1400f - origin.z) / size.z * res), 0, res), z1 = UnityEngine.Mathf.Clamp((int)((1490f - origin.z) / size.z * res), 0, res);
    if (x1 <= x0 || z1 <= z0) continue;
    var maps = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
    for (var zi = 0; zi < z1 - z0; zi++)
        for (var xi = 0; xi < x1 - x0; xi++)
        {
            var wx = origin.x + (x0 + xi + 0.5f) / res * size.x; var wz = origin.z + (z0 + zi + 0.5f) / res * size.z;
            var d = UnityEngine.Mathf.Sqrt(UnityEngine.Mathf.Pow((wx + 1695f) / 50f, 2) + UnityEngine.Mathf.Pow((wz - 1445f) / 35f, 2));
            var w = UnityEngine.Mathf.Clamp01((1f - d) * 3f) * (0.75f + 0.25f * UnityEngine.Mathf.PerlinNoise(wx * 0.08f, wz * 0.08f));
            if (w <= 0f) continue;
            for (var l = 0; l < data.alphamapLayers; l++) maps[zi, xi, l] *= 1f - w;
            maps[zi, xi, mud] += w;
        }
    data.SetAlphamaps(x0, z0, maps);
    UnityEditor.EditorUtility.SetDirty(data);
}

stage = "training";
// 4. training spots, copied from the main island's level 1 ones (chunks by their task: they rename themselves)
System.Func<TaskType, UnityEngine.Transform, int, UnityEngine.Vector3, Chunk> chunk = (task, from, level, at) =>
{
    UnityEngine.Transform source = null;
    foreach (var c0 in from.GetComponentsInChildren<Chunk>(true))
    {
        if (c0.Type != task) continue;
        if (!source || UnityEngine.Mathf.Abs(c0.RequiredCombatLevel - level) < UnityEngine.Mathf.Abs(source.GetComponent<Chunk>().RequiredCombatLevel - level)) source = c0.transform;
    }
    var copy = UnityEngine.Object.Instantiate(source.gameObject, playable.transform);
    copy.name = task + " Lv. " + level;
    // the source island's scenery and people stay there
    foreach (var name in new[] { "Environment", "NPCs" })
    {
        var extra = copy.transform.Find(name);
        if (extra) UnityEngine.Object.DestroyImmediate(extra.gameObject);
    }
    copy.transform.position = at;
    var c = copy.GetComponent<Chunk>();
    c.RequiredSkilllevel = level;
    c.RequiredCombatLevel = level;
    return c;
};
System.Action<UnityEngine.Transform, UnityEngine.Vector2[]> spread = (g, spots) =>
{
    var i = 0;
    var extra = new System.Collections.Generic.List<UnityEngine.GameObject>();
    foreach (UnityEngine.Transform child in g)
    {
        if (i >= spots.Length) { extra.Add(child.gameObject); continue; }
        child.position = new UnityEngine.Vector3(spots[i].x, ground(spots[i].x, spots[i].y), spots[i].y);
        i++;
    }
    foreach (var e in extra) UnityEngine.Object.DestroyImmediate(e);
    if (i < spots.Length) sb.AppendLine(g.name + ": only " + i + " of " + spots.Length + " spots");
};
System.Action<Chunk, ResourceType, UnityEngine.Vector2[]> field = (c, type, spots) =>
{
    var f = c.GetComponentInChildren<ResourceField>(true);
    var so = new UnityEditor.SerializedObject(f);
    var types = so.FindProperty("types");
    types.ClearArray(); types.InsertArrayElementAtIndex(0); types.GetArrayElementAtIndex(0).objectReferenceValue = type;
    var list = so.FindProperty("instances");
    list.ClearArray();
    var random = new System.Random(9);
    for (var i = 0; i < spots.Length; ++i)
    {
        list.InsertArrayElementAtIndex(i);
        var e = list.GetArrayElementAtIndex(i);
        e.FindPropertyRelative("type").intValue = 0;
        e.FindPropertyRelative("variant").intValue = random.Next(type.variants.Length);
        e.FindPropertyRelative("position").vector3Value = new UnityEngine.Vector3(spots[i].x, terrain(spots[i].x, spots[i].y), spots[i].y);
        e.FindPropertyRelative("yaw").floatValue = (float)(random.NextDouble() * 360.0);
        e.FindPropertyRelative("scale").floatValue = 0.9f + (float)random.NextDouble() * 0.2f;
    }
    so.ApplyModifiedPropertiesWithoutUndo();
};
var trees = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/World/WorldResources/Types/Adventure_Trees.asset");
var plants = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/World/WorldResources/Types/Garden_Plants.asset");

var wood = chunk(TaskType.Woodcutting, home, 1, new UnityEngine.Vector3(-1750f, terrain(-1750f, 1675f), 1675f));
field(wood, trees, new[] { new UnityEngine.Vector2(-1768, 1662), new UnityEngine.Vector2(-1760, 1672), new UnityEngine.Vector2(-1752, 1660), new UnityEngine.Vector2(-1745, 1685), new UnityEngine.Vector2(-1762, 1690), new UnityEngine.Vector2(-1738, 1672), new UnityEngine.Vector2(-1772, 1680), new UnityEngine.Vector2(-1750, 1698) });

var gather = chunk(TaskType.Gathering, home, 1, new UnityEngine.Vector3(-1548f, terrain(-1548f, 1765f), 1765f));
field(gather, plants, new[] { new UnityEngine.Vector2(-1552, 1768), new UnityEngine.Vector2(-1546, 1772), new UnityEngine.Vector2(-1540, 1766), new UnityEngine.Vector2(-1548, 1760), new UnityEngine.Vector2(-1542, 1776), new UnityEngine.Vector2(-1554, 1776) });

var farm = chunk(TaskType.Farming, home, 1, new UnityEngine.Vector3(-1524f, terrain(-1524f, 1797f), 1797f));
// the main island's patch group holds its windmill and fences too: only the patches come along
var patches = farm.transform.GetChild(0);
foreach (UnityEngine.Transform other in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<UnityEngine.Transform>(patches)))
    if (!other.GetComponentInChildren<FarmController>(true)) UnityEngine.Object.DestroyImmediate(other.gameObject);
spread(patches, new[] { new UnityEngine.Vector2(-1534, 1790), new UnityEngine.Vector2(-1524, 1790), new UnityEngine.Vector2(-1514, 1790), new UnityEngine.Vector2(-1529, 1805) });

var fish = chunk(TaskType.Fishing, home, 1, new UnityEngine.Vector3(-1582f, -3.31f, 1455f));
// on the river by the swamp: water with a bank within a few metres, the spots apart from each other
var fishSpots = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var z = 1425f; z <= 1495f && fishSpots.Count < 4; z += 2f)
    for (var x = -1610f; x <= -1530f && fishSpots.Count < 4; x += 2f)
    {
        if (terrain(x, z) > -3.8f) continue;
        // walkable bank within reach: the world's "Do Not Walk" volume keeps everything below -2 off the navmesh
        var bank = terrain(x - 3f, z) > -1.5f || terrain(x + 3f, z) > -1.5f || terrain(x, z - 3f) > -1.5f || terrain(x, z + 3f) > -1.5f;
        if (!bank) continue;
        var apart = true;
        foreach (var f in fishSpots) if (UnityEngine.Vector2.Distance(f, new UnityEngine.Vector2(x, z)) < 10f) apart = false;
        if (apart) fishSpots.Add(new UnityEngine.Vector2(x, z));
    }
spread(fish.transform.GetChild(0), fishSpots.ToArray());
foreach (UnityEngine.Transform spot in fish.transform.GetChild(0)) spot.position = new UnityEngine.Vector3(spot.position.x, -3.31f, spot.position.z);
// the river runs between steep banks: the walkable ground is 10 to 13 m from the water, so the spots reach further
// (a fishing spot's reach is its sphere's radius)
foreach (var spotCollider in fish.GetComponentsInChildren<FishingController>(true))
{
    var reach = spotCollider.GetComponent<UnityEngine.SphereCollider>();
    if (reach) reach.radius = 14f / UnityEngine.Mathf.Max(0.01f, spotCollider.transform.lossyScale.x);
}

var mine = chunk(TaskType.Mining, home, 1, new UnityEngine.Vector3(-1698f, terrain(-1698f, 1445f), 1445f));
spread(mine.transform.GetChild(0), new[] { new UnityEngine.Vector2(-1704, 1440), new UnityEngine.Vector2(-1698, 1436), new UnityEngine.Vector2(-1692, 1442), new UnityEngine.Vector2(-1700, 1448) });

// stations move as a whole (their props stay round them)
chunk(TaskType.Cooking, home, 1, new UnityEngine.Vector3(-1716f, terrain(-1716f, 1618f), 1618f));
chunk(TaskType.Crafting, home, 1, new UnityEngine.Vector3(-1650f, terrain(-1650f, 1640f), 1640f));
chunk(TaskType.Alchemy, home, 1, new UnityEngine.Vector3(-1670f, terrain(-1670f, 1640f), 1640f));

stage = "enemies";
// 5. enemies. The goblin camp's goblins are Synty rigs like the pirates': they get a pirate's enemy parts.
var pirate = away.GetComponentInChildren<EnemyController>(true).gameObject;
System.Action<UnityEngine.GameObject, float, float, float, float> makeEnemy = (go, attack, defense, strength, health) =>
{
    foreach (var type in new[] { typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.SphereCollider), typeof(UnityEngine.AI.NavMeshAgent), typeof(EnemyMovementController), typeof(EnemyController) })
    {
        var existing = go.GetComponent(type);
        if (existing) continue;
        UnityEditorInternal.ComponentUtility.CopyComponent(pirate.GetComponent(type));
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
    }
    var animator = go.GetComponent<UnityEngine.Animator>();
    if (!animator) animator = go.AddComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = pirate.GetComponent<UnityEngine.Animator>().runtimeAnimatorController;
    var enemy = go.GetComponent<EnemyController>();
    enemy.Stats.Attack = new SkillStat(attack);
    enemy.Stats.Defense = new SkillStat(defense);
    enemy.Stats.Strength = new SkillStat(strength);
    enemy.Stats.Health = new SkillStat(health);
    var so = new UnityEditor.SerializedObject(enemy);
    so.FindProperty("spawnPoint").vector3Value = go.transform.position;
    so.FindProperty("spawnRotation").quaternionValue = go.transform.rotation;
    so.ApplyModifiedPropertiesWithoutUndo();
};
var monsters = root.Find("Environment/Monsters");

// the goblins: a fighting chunk whose enemies are copies of the camp's rigs (the old ones are switched off)
var goblins = chunk(TaskType.Fighting, home, 5, new UnityEngine.Vector3(-1535f, terrain(-1535f, 1630f), 1630f));
var goblinGroup = goblins.transform.GetChild(0);
foreach (UnityEngine.Transform e in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<UnityEngine.Transform>(goblinGroup))) UnityEngine.Object.DestroyImmediate(e.gameObject);
var goblinCount = 0;
UnityEngine.GameObject ghostSource = null;
foreach (UnityEngine.Transform rig in monsters)
{
    if (!rig.name.StartsWith("Character_Goblin")) continue;
    ghostSource = rig.gameObject;
    var copy = UnityEngine.Object.Instantiate(rig.gameObject, goblinGroup);
    copy.name = rig.name.Contains("Female") ? "Goblin (female)" : "Goblin";
    copy.transform.position = new UnityEngine.Vector3(rig.position.x, ground(rig.position.x, rig.position.z), rig.position.z);
    copy.SetActive(true);
    makeEnemy(copy, 6f, 5f, 6f, 18f);
    goblinCount++;
}

// the cows: copies of the main island's, in the cow field
var cows = chunk(TaskType.Fighting, home, 20, new UnityEngine.Vector3(-1525f, terrain(-1525f, 1716f), 1716f));
cows.name = "Fighting Lv. 1 (cows)";
cows.RequiredSkilllevel = 1;
cows.RequiredCombatLevel = 1;
var cowSpots = new System.Collections.Generic.List<UnityEngine.Vector2>();
foreach (UnityEngine.Transform c in monsters) if (c.name.StartsWith("Cow")) cowSpots.Add(new UnityEngine.Vector2(c.position.x, c.position.z));
var cowGroup = cows.GetComponentInChildren<EnemyController>(true).transform.parent; // Enemies/Cows
foreach (UnityEngine.Transform other in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<UnityEngine.Transform>(cowGroup.parent)))
    if (other != cowGroup) UnityEngine.Object.DestroyImmediate(other.gameObject);
spread(cowGroup, cowSpots.ToArray());
foreach (var cow in cowGroup.GetComponentsInChildren<EnemyController>(true))
{
    cow.Stats.Attack = new SkillStat(2f); cow.Stats.Defense = new SkillStat(2f); cow.Stats.Strength = new SkillStat(2f); cow.Stats.Health = new SkillStat(10f);
    var so = new UnityEditor.SerializedObject(cow);
    so.FindProperty("spawnPoint").vector3Value = cow.transform.position;
    so.ApplyModifiedPropertiesWithoutUndo();
}

// a tormented soul haunting the graveyard to fight (the restless ghost itself is an NPC, below)
var ghosts = chunk(TaskType.Fighting, home, 1, new UnityEngine.Vector3(-1672f, terrain(-1672f, 1700f), 1700f));
ghosts.name = "Fighting Lv. 15 (soul)";
ghosts.RequiredSkilllevel = 15;
ghosts.RequiredCombatLevel = 15;
var ghostGroup = ghosts.transform.GetChild(0);
foreach (UnityEngine.Transform e in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<UnityEngine.Transform>(ghostGroup))) UnityEngine.Object.DestroyImmediate(e.gameObject);
var ghost = UnityEngine.Object.Instantiate(ghostSource, ghostGroup);
ghost.name = "Tormented Soul";
ghost.transform.position = new UnityEngine.Vector3(-1676f, ground(-1676f, 1690f), 1690f);
foreach (UnityEngine.Transform part in ghost.transform) part.gameObject.SetActive(part.name == "Root" || part.name == "Character_Tormented_Soul");
ghost.SetActive(true);
makeEnemy(ghost, 16f, 14f, 16f, 60f);
monsters.gameObject.SetActive(false);

stage = "npcs";
// 6. the people the streamer talks to while playing by hand (IslandNpc, StreamingAssets/Ravebridge/quests.json)
var npcs = group("NPCs");
var idle = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>("Assets/Animations/Barkeep.controller");
System.Action<UnityEngine.GameObject, string> makeNpc = (go, key) =>
{
    go.SetActive(true);
    var animator = go.GetComponentInChildren<UnityEngine.Animator>(true);
    if (!animator) animator = go.AddComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = idle;
    var capsule = go.AddComponent<UnityEngine.CapsuleCollider>();
    capsule.isTrigger = true;
    capsule.center = new UnityEngine.Vector3(0f, 1f, 0f);
    capsule.height = 2f;
    capsule.radius = 0.5f;
    var npc = go.AddComponent<IslandNpc>();
    npc.Key = key;
};
System.Action<string, string, float, float, float> person = (prefab, key, x, z, yaw) =>
{
    var go = place("Assets/PolygonFantasyKingdom/Prefabs/Characters/" + prefab + ".prefab", npcs, x, z, yaw);
    if (!go) return;
    go.name = key;
    go.transform.position = new UnityEngine.Vector3(x, ground(x, z), z);
    makeNpc(go, key);
};
person("SM_Chr_King_01", "duke", -1637f, 1562f, 200f);
person("SM_Chr_Bartender_01", "cook", -1709f, 1621f, 200f);
person("SM_Chr_Peasant_Male_01", "miller", -1776f, 1736f, 135f);
person("SM_Chr_Priest_01", "priest", -1688f, 1703f, 90f);
person("SM_Chr_Hermit_01", "keeper", -1645f, 1677f, 80f);
person("SM_Chr_Merchant_01", "shopkeeper", -1660f, 1637f, 180f);
// the goblin generals and the restless ghost: goblin camp rigs shown as them
System.Action<string, string, float, float, float> rigPerson = (variant, key, x, z, yaw) =>
{
    var go = UnityEngine.Object.Instantiate(ghostSource, npcs);
    go.name = key;
    go.transform.position = new UnityEngine.Vector3(x, ground(x, z), z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    foreach (UnityEngine.Transform part in go.transform) part.gameObject.SetActive(part.name == "Root" || part.name == variant);
    makeNpc(go, key);
};
rigPerson("Character_Goblin_WarChief", "bentnose", -1556f, 1650f, 60f);
rigPerson("Character_Goblin_Shaman", "wartfist", -1552f, 1653f, 240f);
rigPerson("Character_Ghost_02", "ghost", -1669f, 1711f, 270f);
sb.AppendLine("npcs " + npcs.childCount);

var counts = new System.Text.StringBuilder();
foreach (var c in playable.GetComponentsInChildren<Chunk>(true)) counts.Append(c.name + " ");
sb.AppendLine("training: " + counts);
sb.AppendLine("goblins " + goblinCount + ", cows " + cowGroup.childCount + ", fishing spots " + fishSpots.Count);

UnityEditor.EditorUtility.SetDirty(island);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.gameObject.scene);
return sb.ToString();
}
catch (System.Exception exc)
{
    return "failed at " + stage + ": " + exc;
}
