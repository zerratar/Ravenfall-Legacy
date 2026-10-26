// Ravebridge's places where Lumbridge has them (docs/references/ravebridge/web: RuneHQ's city map, the wikis): the
// church and its graveyard south east of the castle, between it and the river, with the swamp beyond; Bob's axes south
// of the castle. Moves the priest, the ghost and the graveyard's enemies with them. Idempotent (absolute positions);
// run before ravebridge-dress.cs, which lays the roads to them. Run in edit mode: unity command eval_file --file <this>.
var island = UnityEngine.GameObject.Find("============ WORLD/Islands").transform.Find("Ravebridge");
var places = island.Find("Playable/Places");
var terrains = island.GetComponentsInChildren<UnityEngine.Terrain>(true);
float Ground(float x, float z)
{
    foreach (var t in terrains)
    {
        var p = t.transform.position; var s = t.terrainData.size;
        if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return t.terrainData.GetInterpolatedHeight((x - p.x) / s.x, (z - p.z) / s.z) + p.y;
    }
    return 0f;
}
var log = "";
void Put(UnityEngine.Transform t, float x, float z, float yaw, float sink = 0.1f)
{
    UnityEditor.Undo.RecordObject(t, "Ravebridge layout");
    t.SetPositionAndRotation(new UnityEngine.Vector3(x, Ground(x, z) - sink, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    log += t.name + " -> (" + x + ", " + z + "); ";
}
// the presets' fronts face local +z: yaw 90 looks east, 270 west
// the church with the steeple (preset B): it reads as a church from across the town, the plain chapel (A) did not
var church = places.Find("Church");
var churchB = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Buildings/Preset_Houses/SM_Bld_Preset_Church_01_B_Optimized.prefab");
if (UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(church.gameObject) != churchB)
{
    var swapped = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(churchB, places);
    swapped.transform.SetSiblingIndex(church.GetSiblingIndex());
    UnityEngine.Object.DestroyImmediate(church.gameObject);
    swapped.name = "Church";
    swapped.isStatic = true;
    church = swapped.transform;
}
Put(church, -1597f, 1479f, 270f);
Put(places.Find("House (Bob's axes)"), -1644f, 1478f, 90f);
Put(island.Find("Playable/NPCs/priest"), -1607f, 1479f, 90f, 0f);
Put(island.Find("Playable/NPCs/ghost"), -1598f, 1447f, 0f, 0f);

// the graveyard, rebuilt south of the church: rows of stones, a cross, the crypt at its south end, a fence with its
// gate on the road side
var grave = places.Find("Graveyard");
for (var i = grave.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(grave.GetChild(i).gameObject);
string K(string n) => "Assets/PolygonFantasyKingdom/Prefabs/" + n + ".prefab";
var stones = new[] { K("Bonus/SM_Prop_Gravestone_01"), K("Bonus/SM_Prop_Gravestone_02"), K("Bonus/SM_Prop_Gravestone_03"), K("Bonus/SM_Prop_Gravestone_04"), K("Bonus/SM_Prop_Gravestone_05"), K("Bonus/SM_Prop_Gravestone_Broken_01"), K("Bonus/SM_Prop_Gravestone_Dirt_01") };
var rng = new System.Random(9);
UnityEngine.GameObject Place(string path, float x, float z, float yaw, float scale = 1f, float sink = 0.05f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) throw new System.Exception("missing " + path);
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, grave);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, Ground(x, z) - sink, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    go.isStatic = true;
    return go;
}
const float X0 = -1614f, X1 = -1584f, Z0 = 1432f, Z1 = 1464f;
for (var row = 0; row < 4; row++)
    for (var col = 0; col < 6; col++)
    {
        if (row == 1 && (col == 2 || col == 3)) continue;   // room for the ghost
        var x = X0 + 4f + col * 4.4f + ((float)rng.NextDouble() - 0.5f) * 0.8f;
        var z = Z0 + 9f + row * 5.2f + ((float)rng.NextDouble() - 0.5f) * 0.6f;
        Place(stones[rng.Next(stones.Length)], x, z, 270f + ((float)rng.NextDouble() - 0.5f) * 14f);
    }
Place(K("Props/SM_Prop_Cross_01"), -1599f, Z1 - 3f, 270f, 1.3f);
Place("Assets/PolygonDungeon/Prefabs/Environments/Misc/SM_Env_Entrance_Crypt_02.prefab", -1592f, Z0 + 4f, 0f, 1f, 0.2f);   // the tomb by the south fence
// the fence: pale rails, a gap on the west side for the gate
var rail = K("Props/SM_Prop_Fence_01");
void Fence(float ax, float az, float bx, float bz, float gapFrom = -1f, float gapTo = -1f)
{
    var len = UnityEngine.Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
    var n = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(len / 2.5f)); var step = len / n;
    var yaw = UnityEngine.Mathf.Atan2(bx - ax, bz - az) * UnityEngine.Mathf.Rad2Deg - 90f;
    for (var k = 0; k < n; k++)
    {
        var walked = (k + 0.5f) * step;
        if (walked >= gapFrom && walked <= gapTo) continue;
        var go = Place(rail, ax + (bx - ax) * walked / len, az + (bz - az) * walked / len, yaw, 1f, 0.05f);
        go.transform.localScale = new UnityEngine.Vector3(step / 2.5f, 1f, 1f);
    }
}
Fence(X0, Z0, X1, Z0);
Fence(X1, Z0, X1, Z1);
Fence(X1, Z1, X0, Z1);
Fence(X0, Z1, X0, Z0, 12f, 17f);
log += "graveyard " + grave.childCount + " pieces; ";

// the restless soul's spot moves into the graveyard; every enemy stands on the ground there
var fight = island.Find("Playable/Fighting Lv. 15 (soul)") ?? island.Find("Playable/Fighting Lv. 15");
if (fight)
{
    UnityEditor.Undo.RecordObject(fight, "Ravebridge layout");
    var delta = new UnityEngine.Vector3(-1596f, 0f, 1452f) - new UnityEngine.Vector3(fight.position.x, 0f, fight.position.z);
    fight.position += delta;
    foreach (var c in fight.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (c == fight || !c.GetComponent<EnemyController>()) continue;
        c.position = new UnityEngine.Vector3(c.position.x, Ground(c.position.x, c.position.z), c.position.z);
    }
    log += fight.name + " moved; ";
}
else log += "NO soul fight spot; ";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return log;
