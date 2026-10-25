// Run with: unity command eval_file --file tools/mangrove/mangrove-island.cs
//
// Makes the mangrove island north east of the main island playable (phase 5b of docs/quests-design.md): the island
// the streamer's story "Insufficient bananas" unlocks per stream. Decided with the user 2026-10-25: players walk on
// the beaches and boardwalks and wade the shallow lagoon; the ferry stops there where it is unlocked; it is open to
// every level with an experience bonus; pirates are camped on the south beach.
//
// What it builds, all under "Playable" in WORLD/Islands/Mangrove (run it again to rebuild):
// - the IslandController on the Mangrove root ((Island)8 "Mangrove", level 1) with its trigger sphere;
// - an invisible floor over the shallow lagoon, knee deep, so the navmesh joins the beaches, the stilt village and
//   the boarding spot;
// - the island's own navmesh surface (area "Mangrove") with a sea floor that is not walkable;
// - the dock at the lagoon mouth, the spawn point and the camera's target;
// - training spots, copied from the Away island's and moved: palms to cut on the north beach, coconuts on the east
//   beach, farm patches on the west patch, fishing in the lagoon by the village, rocks at the east cliffs' feet, the
//   crafting, cooking and alchemy stalls in the village, and the pirates (made weaker: everyone can come here);
// - the ferry's two extra paths, Eldara to Mangrove and Mangrove to Home, given to the PathSelector.
// The island's navmesh is baked afterwards (tools/mangrove/navmesh-bake.cs).
var sb = new System.Text.StringBuilder();
var ci = System.Globalization.CultureInfo.InvariantCulture;
var stage = "start";
try
{

var mangrove = UnityEngine.GameObject.Find("============ WORLD/Islands/Mangrove");
var away = UnityEngine.GameObject.Find("============ WORLD/Islands/Small Island");
if (!mangrove || !away) return "no Mangrove or no Away island";

var old = mangrove.transform.Find("Playable");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var playable = new UnityEngine.GameObject("Playable");
playable.transform.SetParent(mangrove.transform, false);
playable.transform.position = UnityEngine.Vector3.zero;
playable.transform.rotation = UnityEngine.Quaternion.identity;

// the ground under a point: the highest collider below it, the lagoon floor included
System.Func<float, float, float> ground = (x, z) =>
{
    var hits = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x, 30f, z), UnityEngine.Vector3.down, 60f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
    var y = -3.8f;
    var best = float.MaxValue;
    foreach (var h in hits)
    {
        var n = h.collider.name;
        if (n == "Do Not Walk" || n.StartsWith("SM_Env_Tree") || n.StartsWith("SM_Env_Mangrove") || n.StartsWith("SM_Env_Rock")) continue;
        if (h.distance < best) { best = h.distance; y = h.point.y; }
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

stage = "floor";
// 1. the lagoon floor, knee deep: its top at -3.8, far enough above the world's "Sea Floor Not Walkable" volume
// (which ends at -4; at -3.95 the voxels still fell inside it) to bake as walkable; the sand under it lies at -4.2
var floor = new UnityEngine.GameObject("Lagoon Floor");
floor.transform.SetParent(playable.transform, false);
floor.transform.position = new UnityEngine.Vector3(330f, -4.05f, 835f);
var floorBox = floor.AddComponent<UnityEngine.BoxCollider>();
floorBox.size = new UnityEngine.Vector3(84f, 0.5f, 84f);

stage = "island";
// 2. the island itself
// the sphere first: IslandController reads it when it wakes
var sphere = mangrove.GetComponent<UnityEngine.SphereCollider>();
if (!sphere) sphere = mangrove.AddComponent<UnityEngine.SphereCollider>();
var island = mangrove.GetComponent<IslandController>();
if (!island) island = mangrove.AddComponent<IslandController>();
sphere.isTrigger = true;
sphere.center = mangrove.transform.InverseTransformPoint(new UnityEngine.Vector3(330f, -3f, 840f));
sphere.radius = 115f;
island.Island = (RavenNest.Models.Island)8;
island.Identifier = "Mangrove";
island.LevelRequirement = 1;
island.AllowRaidWar = false;

// its own navmesh, in its own area ("Mangrove", 3), which agents on the island keep to (NavMeshHelper): the world's
// navmesh cannot be baked again without changing paths all over the world, and it still has old scraps here, joined
// to nothing. The surface bakes the island's children like the world's does, with a sea floor of its own (the
// world's "Sea Floor Not Walkable" volume is not one of them).
island.NavMeshArea = 3;
var surface = mangrove.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if (!surface) surface = mangrove.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
var worldSurface = UnityEngine.GameObject.Find("============ WORLD").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = worldSurface.agentTypeID;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = worldSurface.layerMask;
surface.defaultArea = 3;
var seaFloor = new UnityEngine.GameObject("Sea Floor Not Walkable");
seaFloor.transform.SetParent(playable.transform, false);
seaFloor.transform.position = new UnityEngine.Vector3(330f, -17.5f, 840f);
var seaVolume = seaFloor.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
seaVolume.size = new UnityEngine.Vector3(300f, 27f, 300f);
seaVolume.center = UnityEngine.Vector3.zero;
seaVolume.area = 1;

var dockGo = new UnityEngine.GameObject("Dock");
dockGo.transform.SetParent(playable.transform, false);
dockGo.transform.position = new UnityEngine.Vector3(326f, -3.8f, 801f);
var dockBox = dockGo.AddComponent<UnityEngine.BoxCollider>();
dockBox.isTrigger = true;
dockBox.size = new UnityEngine.Vector3(6f, 2f, 5f);
var dock = dockGo.AddComponent<DockController>();
dock.DockPositionTransform = dockGo.transform;
island.DockingArea = dock;
island.SpawnPositionTransform = point("SpawnPosition", 330f, 832f);
island.CameraPanTarget = point("Camera Rotation Target", 332f, 838f);

stage = "training";
// 3. training spots, copied from the Away island and moved
// Away's chunks by their task, not their names (chunks rename themselves)
System.Func<string, UnityEngine.Vector3, Chunk> chunk = (name, at) =>
{
    var task = (TaskType)System.Enum.Parse(typeof(TaskType), name.Split(' ')[0]);
    UnityEngine.Transform source = null;
    foreach (var c0 in away.GetComponentsInChildren<Chunk>(true)) if (c0.Type == task) { source = c0.transform; break; }
    var copy = UnityEngine.Object.Instantiate(source.gameObject, playable.transform);
    copy.name = task + " Lv. 1";
    var offset = at - source.position;
    copy.transform.position = source.position + offset;
    var c = copy.GetComponent<Chunk>();
    c.RequiredSkilllevel = 1;
    c.RequiredCombatLevel = 1;
    return c;
};
System.Action<UnityEngine.Transform, UnityEngine.Vector2[]> spread = (group, spots) =>
{
    var i = 0;
    var extra = new System.Collections.Generic.List<UnityEngine.GameObject>();
    foreach (UnityEngine.Transform child in group)
    {
        if (i >= spots.Length) { extra.Add(child.gameObject); continue; }
        child.position = new UnityEngine.Vector3(spots[i].x, ground(spots[i].x, spots[i].y), spots[i].y);
        i++;
    }
    foreach (var e in extra) UnityEngine.Object.DestroyImmediate(e);
};
System.Action<Chunk, ResourceType, UnityEngine.Vector2[]> field = (c, type, spots) =>
{
    var f = c.GetComponentInChildren<ResourceField>(true);
    var so = new UnityEditor.SerializedObject(f);
    var types = so.FindProperty("types");
    types.ClearArray(); types.InsertArrayElementAtIndex(0); types.GetArrayElementAtIndex(0).objectReferenceValue = type;
    var list = so.FindProperty("instances");
    list.ClearArray();
    var random = new System.Random(8);
    for (var i = 0; i < spots.Length; ++i)
    {
        list.InsertArrayElementAtIndex(i);
        var e = list.GetArrayElementAtIndex(i);
        e.FindPropertyRelative("type").intValue = 0;
        e.FindPropertyRelative("variant").intValue = random.Next(type.variants.Length);
        e.FindPropertyRelative("position").vector3Value = new UnityEngine.Vector3(spots[i].x, ground(spots[i].x, spots[i].y), spots[i].y);
        e.FindPropertyRelative("yaw").floatValue = (float)(random.NextDouble() * 360.0);
        e.FindPropertyRelative("scale").floatValue = 0.9f + (float)random.NextDouble() * 0.2f;
    }
    so.ApplyModifiedPropertiesWithoutUndo();
};

var palms = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/World/WorldResources/Types/Ancient_Empire_Trees.asset");
var coconuts = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/World/WorldResources/Types/Coconuts.asset");

stage = "woodcutting";
var wood = chunk("Woodcutting Lv. 50", new UnityEngine.Vector3(330f, -3f, 861f));
field(wood, palms, new[] { new UnityEngine.Vector2(324, 858), new UnityEngine.Vector2(328, 864), new UnityEngine.Vector2(333, 859), new UnityEngine.Vector2(337, 864), new UnityEngine.Vector2(324.5f, 862.5f), new UnityEngine.Vector2(334, 866) });

stage = "gathering";
var gather = chunk("Gathering Lv. 50", new UnityEngine.Vector3(354f, -3f, 852f));
field(gather, coconuts, new[] { new UnityEngine.Vector2(349, 849), new UnityEngine.Vector2(352, 853), new UnityEngine.Vector2(356, 850), new UnityEngine.Vector2(350, 856), new UnityEngine.Vector2(355, 856), new UnityEngine.Vector2(358, 854), new UnityEngine.Vector2(353, 847) });

stage = "farming";
var farm = chunk("Farming Lv. 50", new UnityEngine.Vector3(289f, -3f, 845f));
spread(farm.transform.GetChild(0), new[] { new UnityEngine.Vector2(287, 845), new UnityEngine.Vector2(292, 844) });

stage = "fishing";
var fish = chunk("Fishing Lv. 50", new UnityEngine.Vector3(316f, -3.3f, 826f));
spread(fish.transform.GetChild(0), new[] { new UnityEngine.Vector2(312, 830), new UnityEngine.Vector2(316, 822), new UnityEngine.Vector2(322, 816), new UnityEngine.Vector2(308, 838) });
foreach (UnityEngine.Transform spot in fish.transform.GetChild(0)) spot.position = new UnityEngine.Vector3(spot.position.x, -3.31f, spot.position.z);

stage = "mining";
var mine = chunk("Mining Lv. 50", new UnityEngine.Vector3(361f, -3.8f, 840f));
// on the east beach, among the coconuts: under the ledge or in the lagoon they could not be reached or seen
spread(mine.transform.GetChild(0), new[] { new UnityEngine.Vector2(360.4f, 848.2f), new UnityEngine.Vector2(352f, 860f), new UnityEngine.Vector2(360f, 854f), new UnityEngine.Vector2(358.5f, 851f) });

stage = "stations";
var craft = chunk("Crafting Lv. 50", new UnityEngine.Vector3(329f, -3.8f, 832f));
spread(craft.transform, new[] { new UnityEngine.Vector2(329, 832) });
var cook = chunk("Cooking Lv. 50", new UnityEngine.Vector3(339f, -3.8f, 838f));
spread(cook.transform, new[] { new UnityEngine.Vector2(339, 838) });
var brew = chunk("Alchemy Lv. 50", new UnityEngine.Vector3(324f, -3.8f, 826f));
spread(brew.transform, new[] { new UnityEngine.Vector2(324, 826) });

stage = "pirates";
var fight = chunk("Fighting Lv. 50", new UnityEngine.Vector3(345f, -2f, 808f));
var enemies = fight.transform.GetChild(0);
spread(enemies, new[] { new UnityEngine.Vector2(344.5f, 808.8f), new UnityEngine.Vector2(341, 805), new UnityEngine.Vector2(348, 811), new UnityEngine.Vector2(343, 812), new UnityEngine.Vector2(349.9f, 809.2f), new UnityEngine.Vector2(338, 809) });
foreach (var enemy in enemies.GetComponentsInChildren<EnemyController>(true))
{
    // weaker than Away's: the island is open to every level
    var captain = enemy.name.Contains("Captain");
    enemy.Stats.Attack = new SkillStat(captain ? 25f : 12f);
    enemy.Stats.Defense = new SkillStat(captain ? 25f : 12f);
    enemy.Stats.Strength = new SkillStat(captain ? 25f : 12f);
    enemy.Stats.Health = new SkillStat(captain ? 60f : 25f);
    UnityEditor.EditorUtility.SetDirty(enemy);
}

var counts = new System.Text.StringBuilder();
foreach (var c in playable.GetComponentsInChildren<Chunk>(true)) counts.Append(c.name + " ");
sb.AppendLine("training: " + counts);

stage = "ferry";
// 4. the ferry: Eldara to Mangrove and Mangrove to Home, given to the path selector
var paths = UnityEngine.GameObject.Find("============ FERRY/Ferry Paths");
System.Func<string, UnityEngine.Vector3[], UnityEngine.Splines.SplineContainer> spline = (name, knots) =>
{
    var existing = paths.transform.Find(name);
    if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(paths.transform, false);
    go.transform.position = UnityEngine.Vector3.zero;
    var container = go.AddComponent<UnityEngine.Splines.SplineContainer>();
    var s = container.Spline;
    s.Clear();
    foreach (var k in knots) s.Add(new UnityEngine.Splines.BezierKnot(new Unity.Mathematics.float3(k.x, k.y, k.z)), UnityEngine.Splines.TangentMode.AutoSmooth);
    return container;
};
var y = -3.15f;
var to = spline("Eldara => Mangrove", new[] { new UnityEngine.Vector3(638.55f, y, 30.16f), new UnityEngine.Vector3(612f, y, 45f), new UnityEngine.Vector3(598f, y, 140f), new UnityEngine.Vector3(600f, y, 420f), new UnityEngine.Vector3(500f, y, 690f), new UnityEngine.Vector3(380f, y, 765f), new UnityEngine.Vector3(322f, y, 784f) });
var back = spline("Mangrove => Home", new[] { new UnityEngine.Vector3(322f, y, 784f), new UnityEngine.Vector3(290f, y, 690f), new UnityEngine.Vector3(240f, y, 450f), new UnityEngine.Vector3(205f, y, 220f), new UnityEngine.Vector3(170f, y, 110f), new UnityEngine.Vector3(160.7f, y, 62.3f) });
var selector = UnityEngine.Object.FindAnyObjectByType<FerryController>().GetComponent<PathSelector>();
var selectorSo = new UnityEditor.SerializedObject(selector);
selectorSo.FindProperty("toMangrove").objectReferenceValue = to;
selectorSo.FindProperty("fromMangrove").objectReferenceValue = back;
selectorSo.ApplyModifiedPropertiesWithoutUndo();

// land in the way of the ferry: anything above the water along the paths
foreach (var container in new[] { to, back })
{
    var blocked = new System.Collections.Generic.List<string>();
    for (var t = 0.02f; t < 0.98f; t += 0.01f)
    {
        var p = (UnityEngine.Vector3)container.EvaluatePosition(t);
        foreach (var side in new[] { -6f, 0f, 6f })
        {
            var q = p + new UnityEngine.Vector3(side, 0, 0);
            if (UnityEngine.Physics.Raycast(new UnityEngine.Vector3(q.x, 60f, q.z), UnityEngine.Vector3.down, out var hit, 62f, ~0, UnityEngine.QueryTriggerInteraction.Ignore) && hit.point.y > -3.0f && hit.collider.name != "Do Not Walk")
            {
                blocked.Add(hit.collider.name + "@" + ((int)q.x) + "," + ((int)q.z));
                break;
            }
        }
    }
    sb.AppendLine(container.name + ": length " + container.Spline.GetLength().ToString("0", ci) + " m, " + (blocked.Count == 0 ? "clear" : "land at " + string.Join(", ", System.Linq.Enumerable.Take(blocked, 8))));
}

UnityEditor.EditorUtility.SetDirty(island);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mangrove.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(mangrove.scene);
return sb.ToString();
}
catch (System.Exception exc)
{
    return "failed at " + stage + ": " + exc;
}
