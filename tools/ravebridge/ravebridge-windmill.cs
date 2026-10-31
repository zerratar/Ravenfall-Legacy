// Run with: unity command eval_file --file tools/ravebridge/ravebridge-windmill.cs (MainWorld open, edit mode), then
// tools/ravebridge/navmesh-bake.cs
//
// Ravebridge's windmill (building 4 of the OSRS map) as Synty Fantasy Village's windmill: a round stone tower with a
// thatched cap and four sails, much nearer Lumbridge's own mill than the timbered house mill (Fantasy Kingdom's
// SM_Bld_Preset_House_Windmill_01) lumbridge-swap.cs put there. The preset tower has no door, so one is set into its
// wall on the north, the side the map has its door and the miller stands, leaning with the tower's taper; the sails
// face south, to the game camera, and turn with VillageLife. Sacks, flour and a barrel by the door. The miller moves
// to the new door. Rerun to rebuild (it replaces whatever "Building 4 " stands there).
const float Scale = 1.3f, Yaw = 180f;
// the tower's wall (SM_Bld_Preset_Tower_03, unscaled): 2.62 m out at the ground, narrowing 0.13 m a metre up
const float BaseRadius = 2.62f, Taper = 0.13f;
const string FV = "Assets/Synty/PolygonFantasyVillage/Prefabs/";

var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var buildings = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge/Lumbridge/Ravenfall Buildings");
if (!buildings) return "Ravebridge's buildings not found";
UnityEngine.Transform old = null;
foreach (UnityEngine.Transform c in buildings.transform) if (c.name.StartsWith("Building 4 ")) old = c;
if (!old) return "no building 4";
var centre = old.position;
if (old.name.StartsWith("Building 4 (House_Windmill_01)"))
{
    // the house mill's root stood off its middle: the middle of what it drew
    var rs = old.GetComponentsInChildren<UnityEngine.Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    centre = new UnityEngine.Vector3(b.center.x, centre.y, b.center.z);
}
var siblingIndex = old.GetSiblingIndex();
UnityEngine.Object.DestroyImmediate(old.gameObject);

// on the lowest ground under the tower, so no side of it floats
var radius = BaseRadius * Scale;
var ground = WorldRoad.GroundHeight(centre);
for (var a = 0; a < 16; a++)
{
    var q = centre + UnityEngine.Quaternion.Euler(0f, a * 22.5f, 0f) * UnityEngine.Vector3.forward * radius;
    ground = UnityEngine.Mathf.Min(ground, WorldRoad.GroundHeight(q));
}
centre.y = ground - 0.1f;

var mill = new UnityEngine.GameObject("Building 4 (Windmill_01)").transform;
mill.SetParent(buildings.transform, false);
mill.SetSiblingIndex(siblingIndex);
mill.position = centre;
mill.gameObject.isStatic = true;
UnityEngine.GameObject Put(string path, UnityEngine.Vector3 at, UnityEngine.Quaternion rotation, float scale)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) throw new System.Exception("missing " + path);
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, mill);
    o.transform.SetPositionAndRotation(at, rotation);
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    return o;
}
var tower = Put(FV + "Buildings/Preset/Bld_Preset_Windmill_01.prefab", centre, UnityEngine.Quaternion.Euler(0f, Yaw, 0f), Scale);
// the sails turn (VillageLife spins them about their thin axis), so they stay out of static batching
foreach (var r in tower.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    r.gameObject.isStatic = !r.name.StartsWith("SM_Bld_Windmill_Sail");

// the door, north, set into the wall: the plank's hinge is its pivot and it spans 1 m to its left
var outward = UnityEngine.Vector3.forward;
var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, outward);
var lean = UnityEngine.Mathf.Atan(Taper) * UnityEngine.Mathf.Rad2Deg;
var doorRotation = UnityEngine.Quaternion.AngleAxis(-lean, right) * UnityEngine.Quaternion.LookRotation(outward);
var doorFoot = centre + outward * (BaseRadius - 0.04f) * Scale + UnityEngine.Vector3.up * 0.1f;
var door = Put(FV + "Buildings/SM_Bld_Door_01.prefab", doorFoot + right * 0.5f * Scale, doorRotation, Scale);
// a stone step under it (the piece runs 2.7 m to its left from its pivot, its middle 1.24 m in)
const float StepScale = 0.55f * Scale;
Put(FV + "Buildings/SM_Bld_Stone_Edge_Doorway_01.prefab", doorFoot + outward * 0.15f + right * 1.24f * StepScale, UnityEngine.Quaternion.LookRotation(outward), StepScale);

// by the door: x across it (east is +), z out from it
void Prop(string name, float x, float z, float yaw, float scale = 1f)
{
    var at = doorFoot + right * x + outward * z;
    at.y = WorldRoad.GroundHeight(at) - 0.03f;
    var o = Put(FV + "Props/" + name + ".prefab", at, UnityEngine.Quaternion.Euler(0f, yaw, 0f), scale);
    o.isStatic = true;
}
Prop("SM_Prop_Sack_01", -2.1f, 0.9f, 20f, 1.5f);
Prop("SM_Prop_Sack_02", -2.8f, 0.4f, 75f, 1.5f);
Prop("SM_Prop_Food_Flour_01", -1.9f, 1.7f, 140f, 1.5f);
Prop("SM_Prop_Barrel_01", 2.2f, 0.6f, 0f);
Prop("SM_Prop_Crate_02", 2.9f, 1.4f, 30f);
Prop("SM_Prop_Wheelbarrow_01", 3.2f, 2.6f, 250f);

// the miller at the new door, 2 m out, looking out
var moved = "";
foreach (var npc in UnityEngine.Object.FindObjectsByType<IslandNpc>(UnityEngine.FindObjectsInactive.Include))
{
    if (npc.Key != "miller") continue;
    var at = doorFoot + outward * 2.2f + right * 0.8f;
    at.y = WorldRoad.GroundHeight(at);
    npc.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.LookRotation(outward));
    moved = "miller at " + at.ToString("F1");
}

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
return "windmill at " + centre.ToString("F1") + ", door " + door.transform.position.ToString("F1") + "; " + moved;
