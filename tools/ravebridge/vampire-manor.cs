// Run with: unity command eval_file --file tools/ravebridge/vampire-manor.cs (MainWorld open, edit mode), then
// tools/ravebridge/vampire-manor-ground.cs, then tools/ravebridge/navmesh-bake.cs
//
// The vampyre's manor for "Something in the cellar" (the Vampire Slayer tribute, whose vampyre "moved to Draynor"):
// Synty's Horror Mansion as its demo scene builds it (Assets/Synty/PolygonHorrorMansion/Scenes/Demo.unity), copied
// onto Ravebridge's north coast past the farm, its front to the farm road. The mansion, its shed, glasshouse and guest
// house come over with their roofs and upper floor; the furniture inside, the attic's and the basement and dungeon
// (they would sit in the hill) stay behind. An iron fence (WorldFence, the pack's SM_Bld_Fence_01) runs round the
// front and the sides to the cliff, its fancy iron gates standing open on the path, and a dirt path runs from the
// porch through the gate to the road. Twisted dead trees stand in the front yard's corners, a coffin with a stake
// on its lid waits by the porch (the quest's end: the vampyre was not in), and dark pines grow outside the east fence. At the castle's scale (0.85). The trees and props that stood there go; the
// ground is levelled under it by vampire-manor-ground.cs (heights and paint each want their own run). Rerun to rebuild.
const float Scale = 0.85f, Yaw = 180f;
var Centre = new UnityEngine.Vector2(-1727.5f, 1789f);
// in the demo: the footprint kept (mansion, shed, glasshouse, guest house), the front door's line and the wall's
var DemoMin = new UnityEngine.Vector2(-26f, -54f);
var DemoMax = new UnityEngine.Vector2(22f, -5f);
const float DemoDoorZ = -7f, YardDepth = 11f, RoadZ = 1752f, FenceHeight = 2.4f, FenceMargin = 3f, GateGap = 4.2f;
string[] Keep = { "Building", "Roof_Layer", "Upstairs_Layer" };
string[] Drop = { "Attic_Props", "Upstairs_Props", "BasementFloor", "Dungeon", "SM_Bld_Basement_Entrance_01" };

var log = new System.Text.StringBuilder();
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var lumbridge = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge/Lumbridge");
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge");
if (!lumbridge || !island) return "Ravebridge's Lumbridge not found";
var old = lumbridge.transform.Find("Vampyre Manor");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
foreach (var leftover in new[] { "Manor Dressing", "Path to the manor gate", "Manor Fence (west)", "Manor Fence (east)", "Manor Gate (west leaf)", "Manor Gate (east leaf)" })
{
    var o = lumbridge.transform.Find(leftover);
    if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
}

var demo = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Synty/PolygonHorrorMansion/Scenes/Demo.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var root = new UnityEngine.GameObject("Vampyre Manor");
var model = new UnityEngine.GameObject("Model");
model.transform.SetParent(root.transform, false);
foreach (var g in demo.GetRootGameObjects())
{
    if (System.Array.IndexOf(Keep, g.name) >= 0)
    {
        var copy = UnityEngine.Object.Instantiate(g, model.transform);
        copy.name = g.name;
        foreach (var t in copy.GetComponentsInChildren<UnityEngine.Transform>(true))
            if (t && System.Array.IndexOf(Drop, t.name) >= 0) UnityEngine.Object.DestroyImmediate(t.gameObject);
    }
}
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demo, true);
foreach (var c in model.GetComponentsInChildren<UnityEngine.Light>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject);

// the footprint's centre on the root, turned and scaled, on the ground's mean height over it
var demoCentre = (DemoMin + DemoMax) * 0.5f;
model.transform.localPosition = new UnityEngine.Vector3(-demoCentre.x, 0f, -demoCentre.y);
root.transform.SetParent(lumbridge.transform, true);
root.transform.rotation = UnityEngine.Quaternion.Euler(0f, Yaw, 0f);
root.transform.localScale = UnityEngine.Vector3.one * Scale / lumbridge.transform.lossyScale.x;
var half = (DemoMax - DemoMin) * 0.5f * Scale;
float sum = 0f; var n = 0;
for (var dz = -half.y; dz <= half.y; dz += 3f)
for (var dx = -half.x; dx <= half.x; dx += 3f)
    if (TerrainGround.TryHeight(new UnityEngine.Vector3(Centre.x + dx, 0f, Centre.y + dz), out var h)) { sum += h; n++; }
var level = n > 0 ? sum / n : 0f;
root.transform.position = new UnityEngine.Vector3(Centre.x, level, Centre.y);
UnityEngine.Physics.SyncTransforms();

// what stood there goes: anything with a renderer in the footprint and the yard, but not roads, fences, terrain,
// water or the island's gameplay
var keepUnder = new[] { "Playable", "Roads", "Fences" };
var area = new UnityEngine.Rect(Centre.x - half.x - 3f, Centre.y - half.y - YardDepth * Scale - 4f, half.x * 2f + 6f, half.y * 2f + YardDepth * Scale + 7f);
var removed = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
foreach (var r in island.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    if (!r || r.transform.IsChildOf(root.transform)) continue;
    var p = r.bounds.center;
    if (!area.Contains(new UnityEngine.Vector2(p.x, p.z))) continue;
    if (r.GetComponentInParent<WorldRoad>() || r.GetComponentInParent<WorldFence>() || r.GetComponentInParent<UnityEngine.Terrain>()) continue;
    if (r.name.Contains("Water") || r.name.Contains("River")) continue;
    var skip = false;
    for (var t = r.transform; t; t = t.parent) if (System.Array.IndexOf(keepUnder, t.name) >= 0) skip = true;
    if (skip) continue;
    var top = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject) ?? r.gameObject;
    if (top.transform.IsChildOf(root.transform) || top == lumbridge || top == island) continue;
    removed.Add(top);
}
foreach (var g in removed) { log.Append(g.name).Append(", "); UnityEngine.Object.DestroyImmediate(g); }

// a dirt path from the gate to the farm road
var gate = root.transform.TransformPoint(model.transform.localPosition + new UnityEngine.Vector3(0f, 0f, DemoDoorZ + YardDepth));
var pathGo = new UnityEngine.GameObject("Path to the manor gate");
pathGo.transform.SetParent(lumbridge.transform, false);
pathGo.transform.position = new UnityEngine.Vector3(gate.x, 0f, gate.z);
var road = pathGo.AddComponent<WorldRoad>();
road.RoadSurface = WorldRoad.Surface.Dirt;
road.Width = 2.6f;
var porch = root.transform.TransformPoint(model.transform.localPosition + new UnityEngine.Vector3(0f, 0f, DemoDoorZ));
road.AddPoint(pathGo.transform.InverseTransformPoint(new UnityEngine.Vector3(gate.x, 0f, porch.z - 0.5f)));
road.AddPoint(pathGo.transform.InverseTransformPoint(new UnityEngine.Vector3(gate.x, 0f, (gate.z + RoadZ) * 0.5f)));
road.AddPoint(pathGo.transform.InverseTransformPoint(new UnityEngine.Vector3(gate.x, 0f, RoadZ)));
road.Rebuild();

// the iron fence: from the cliff down the west side, along the front to the gate; from the gate along the front and
// up the east side to the cliff. The gate's two leaves stand open, swung in towards the house
var fencePiece = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Synty/PolygonHorrorMansion/Prefabs/Buildings/SM_Bld_Fence_01.prefab");
var west = Centre.x - half.x - FenceMargin; var east = Centre.x + half.x + FenceMargin;
var back = Centre.y + half.y - 2f; var front = gate.z;
void Fence(string name, params UnityEngine.Vector2[] line)
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(root.transform.parent, false);
    go.transform.position = new UnityEngine.Vector3(line[0].x, level, line[0].y);
    var fence = go.AddComponent<WorldFence>();
    fence.Piece = fencePiece;
    fence.Height = FenceHeight;
    foreach (var p in line) fence.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(p.x, level, p.y)));
    fence.Rebuild();
}
Fence("Manor Fence (west)", new UnityEngine.Vector2(west, back), new UnityEngine.Vector2(west, front), new UnityEngine.Vector2(gate.x - GateGap * 0.5f, front));
Fence("Manor Fence (east)", new UnityEngine.Vector2(gate.x + GateGap * 0.5f, front), new UnityEngine.Vector2(east, front), new UnityEngine.Vector2(east, back));
var gatePiece = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Synty/PolygonHorrorMansion/Prefabs/Buildings/SM_Bld_Fence_Gate_Fancy_01.prefab");
var gateScale = FenceHeight / 3.6f;
for (var side = -1; side <= 1; side += 2)
{
    // each leaf hinged at its post (the prefab's hinge is its origin, the leaf along +x), swung 75 degrees in
    var leaf = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(gatePiece, root.transform.parent);
    leaf.name = side < 0 ? "Manor Gate (west leaf)" : "Manor Gate (east leaf)";
    var hinge = new UnityEngine.Vector3(gate.x + side * GateGap * 0.5f, 0f, front);
    hinge.y = WorldRoad.GroundHeight(hinge) - 0.05f;
    leaf.transform.position = hinge;
    leaf.transform.rotation = UnityEngine.Quaternion.Euler(0f, side < 0 ? -75f : 180f + 75f, 0f);
    leaf.transform.localScale = UnityEngine.Vector3.one * gateScale;
    foreach (var c in leaf.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(c);
}

// the dressing, by the house's front: x across it from the path (east is +), z out from the porch towards the gate
var dressing = new UnityEngine.GameObject("Manor Dressing");
dressing.transform.SetParent(root.transform.parent, false);
const string Env = "Assets/Synty/PolygonHorrorMansion/Prefabs/Environment/", Props = "Assets/Synty/PolygonHorrorMansion/Prefabs/Props/";
void Dress(string path, float x, float z, float yaw, float scale = Scale, float lift = 0f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.Append("missing " + path + "; "); return; }
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, dressing.transform);
    var at = new UnityEngine.Vector3(gate.x + x, 0f, porch.z - z);
    at.y = WorldRoad.GroundHeight(at) - 0.05f + lift;
    o.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    o.transform.localScale = UnityEngine.Vector3.one * scale;
}
Dress(Env + "SM_Env_Tree_Large_Twisted_01.prefab", -14f, 5f, 40f);
Dress(Env + "SM_Env_Tree_Large_Twisted_02.prefab", 16f, 4.5f, 200f);
Dress(Props + "SM_Prop_Coffin_01.prefab", 5.5f, 2.2f, 90f);
Dress(Props + "SM_Prop_Exorcist_Stake_01.prefab", 5.5f, 2.2f, 30f, Scale, 0.55f);
Dress(Env + "SM_Env_Pine_01.prefab", 27f, -6f, 0f);
Dress(Env + "SM_Env_Pine_02.prefab", 28f, -20f, 120f);
Dress(Env + "SM_Env_Pine_03.prefab", 26.5f, -32f, 250f);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
var count = root.GetComponentsInChildren<UnityEngine.Renderer>().Length;
var result = "manor at " + root.transform.position.ToString("F1") + " level " + level.ToString("F2") + ", " + count + " renderers, gate " + gate.ToString("F1") + "; removed " + removed.Count + ": " + log;
System.IO.File.WriteAllText("Temp/vampire-manor.txt", result);
return result;
