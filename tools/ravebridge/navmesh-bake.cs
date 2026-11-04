// Run with: unity command eval_file --file tools/ravebridge/navmesh-bake.cs (twice: bake, then check; the bake runs for
// minutes past the eval's 5 s, poll SessionState "ravebridge-navmesh-bake" for "baked")
//
// Bakes Ravebridge's own navmesh (the NavMeshSurface on WORLD/Islands/Ravebridge, area "Ravebridge", over the
// island only: its terrains reach far beyond it) into Assets/Scenes/MainWorld/NavMesh-Ravebridge.asset and saves
// the scene, with steeper slopes and higher steps than the other islands (see below; the Bake button on the surface
// would bake it with the world's limits instead). Agents on the island keep to its area
// (NavMeshHelper.AreaMaskAt), like the mangrove island's (tools/mangrove/navmesh-bake.cs).
// The second run checks the island: its spawn to every training spot, in the island's area only.
var islandRoot = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge");
var surface = islandRoot.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var island = islandRoot.GetComponent<IslandController>();
var key = "ravebridge-navmesh-bake";
var path = "Assets/Scenes/MainWorld/NavMesh-Ravebridge.asset";
var stage = UnityEditor.SessionState.GetString(key, "");
if (stage == "" || stage == "baking")
{
    // built in one go on the editor's own thread: the asynchronous build stalled with the editor in the background
    // (the island since the Lumbridge move is 640 m square and takes a few minutes)
    UnityEditor.SessionState.SetString(key, "baking");
    // The island is walked by hand as the hero: banks and hillsides up to 45 degrees and steps of a metre, where the
    // world's agent stops at 35 and 0.75 (the user, 2026-11-04: the hero got stuck on simple slopes and in the swamp,
    // whose pools' rims ran 31 to 41 degrees). The limits belong to the agent type, which every island shares, so they
    // are set for this bake only and put back: the other islands' navmeshes are baked as before.
    var nav = new UnityEditor.SerializedObject(UnityEditor.Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings"));
    var agents = nav.FindProperty("m_Settings");
    UnityEditor.SerializedProperty agent = null;
    for (var i = 0; i < agents.arraySize; i++)
        if (agents.GetArrayElementAtIndex(i).FindPropertyRelative("agentTypeID").intValue == surface.agentTypeID) agent = agents.GetArrayElementAtIndex(i);
    var slope = agent.FindPropertyRelative("agentSlope");
    var climb = agent.FindPropertyRelative("agentClimb");
    var oldSlope = slope.floatValue;
    var oldClimb = climb.floatValue;
    slope.floatValue = 45f;
    climb.floatValue = 1f;
    nav.ApplyModifiedPropertiesWithoutUndo();
    try
    {
        surface.BuildNavMesh();
    }
    finally
    {
        slope.floatValue = oldSlope;
        climb.floatValue = oldClimb;
        nav.ApplyModifiedPropertiesWithoutUndo();
    }
    var data = surface.navMeshData;
    data.name = "NavMesh-Ravebridge";
    var old = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
    if (old) UnityEditor.AssetDatabase.DeleteAsset(path);
    UnityEditor.AssetDatabase.CreateAsset(data, path);
    surface.enabled = false;
    surface.navMeshData = data;
    surface.enabled = true;
    UnityEditor.EditorUtility.SetDirty(surface);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(surface.gameObject.scene);
    UnityEditor.SessionState.SetString(key, "baked");
    return "baked";
}

UnityEditor.SessionState.EraseString(key);
var area = 1 << island.NavMeshArea;
var sb = new System.Text.StringBuilder("asset " + UnityEditor.AssetDatabase.GetAssetPath(surface.navMeshData) + "; ");
var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
var size = 0f;
for (int t = 0; t < tri.indices.Length / 3; t++)
{
    if (tri.areas[t] != island.NavMeshArea) continue;
    var a = tri.vertices[tri.indices[t * 3]]; var b = tri.vertices[tri.indices[t * 3 + 1]]; var c = tri.vertices[tri.indices[t * 3 + 2]];
    size += UnityEngine.Vector3.Cross(b - a, c - a).magnitude * 0.5f;
}
sb.Append("island area " + size.ToString("0") + " m2\n");
UnityEngine.AI.NavMeshHit hs;
if (!UnityEngine.AI.NavMesh.SamplePosition(island.SpawnPositionTransform.position, out hs, 2f, area)) return sb + "the spawn is off the island's navmesh";
System.Func<UnityEngine.Vector3, float, string> reachWithin = (pos, range) =>
{
    UnityEngine.AI.NavMeshHit h;
    if (!UnityEngine.AI.NavMesh.SamplePosition(pos, out h, range, area)) return "off";
    var p = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.CalculatePath(hs.position, h.position, area, p);
    if (p.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) return "ok";
    var end = p.corners.Length > 0 ? p.corners[p.corners.Length - 1] : hs.position;
    return "partial, " + UnityEngine.Vector3.Distance(end, pos).ToString("0.0") + " m short";
};
// a fishing spot is reached from as far as its sphere's radius (the river's steep banks)
System.Func<UnityEngine.Transform, string> reachSpot = spot =>
{
    var fishing = spot.GetComponent<FishingController>();
    var sphere = fishing ? spot.GetComponent<UnityEngine.SphereCollider>() : null;
    return reachWithin(spot.position, sphere ? UnityEngine.Mathf.Max(4f, sphere.radius * spot.lossyScale.x) : 4f);
};
System.Func<UnityEngine.Vector3, string> reach = pos => reachWithin(pos, 4f);
if (island.DockingArea) sb.Append("dock " + reach(island.DockingArea.DockPositionTransform.position) + "\n");
foreach (var chunk in islandRoot.GetComponentsInChildren<Chunk>(true))
{
    sb.Append(chunk.name + ": ");
    foreach (UnityEngine.Transform group in chunk.transform)
    {
        if (group.childCount == 0) sb.Append(group.name + " " + reachSpot(group) + "; ");
        foreach (UnityEngine.Transform spot in group) sb.Append(spot.name.Split(' ')[0] + " " + reachSpot(spot) + "; ");
    }
    sb.Append("\n");
}
return sb.ToString();
