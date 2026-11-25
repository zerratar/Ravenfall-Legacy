// Builds Assets/Scenes/Tower/WorldTower.unity: the tower that stands in the main world (docs/world-tower.md), from the
// models tools/tower/world/world_tower.py made and tools/tower/world/stack-built.json (the stack, the marks on the
// plaza). Run with the Unity CLI while the editor is not in play mode:
//   unity command eval_file "<full path>\tools\tower\world\world-tower-build.cs"
// It writes its log to %TEMP%/world-tower-build.txt (the eval call itself may time out while the build keeps going).
// The scene is made in its own additive scene, saved and closed again: the open scene (MainWorld) is not touched.
// The game loads it beside the main world at the start (WorldTowerScene in Assets/Scripts/Game/Tower/WorldTower.cs).
//
// The tower's own shapes are the Blender models, all drawn with one material (Ravenfall/WorldTower). The plaza is
// dressed with Synty props: the hero's statue, the banners, braziers, palms, ferns, the pier, the sign.
//
// What it makes, under the root "World Tower" (at stack.json's position, the gate turned to its "faces"):
//   Visuals   everything that is only seen, at stack.json's "scale" (the models are made at 1): the stack, the foot,
//             the props, the lamps, the stone's words, the egg, the gulls, the statue. Hidden while the tower is not drawn.
//   Place     the islet as a place players go (WorldTowerVisit.cs), always there: the island and its dock, the ground
//             to stand on, places on the plaza, the gate and the tunnel's end.
// and beside it "World Tower ferry": the ferry's way from Heim to the pier and on to Atria, in the world's own terms
// (read from the ferry's ring in the open MainWorld, which is only looked at). Run tools/tower/world/sea-chart-islet.py
// first: the ferry's ways are checked against the chart.
var log = new System.Text.StringBuilder();
var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "world-tower-build.txt");
try
{
if (UnityEngine.Application.isPlaying) { System.IO.File.WriteAllText(logPath, "not in play mode"); return "not in play mode"; }
var repo = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
var built = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repo, "tools", "tower", "world", "stack-built.json")));
var stackFile = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repo, "tools", "tower", "world", "stack.json")));
// how much bigger than the models the tower stands in the world
var S = stackFile["scale"] != null ? (float)(double)stackFile["scale"] : 1f;
const string Folder = "Assets/World/Tower/World";
const string Models = Folder + "/Models/";
const string ScenePath = "Assets/Scenes/Tower/WorldTower.unity";
const string DataFolder = "Assets/Scenes/Tower/WorldTower";
if (!UnityEditor.AssetDatabase.IsValidFolder(DataFolder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Scenes/Tower", "WorldTower");
float F(Newtonsoft.Json.Linq.JToken t) => (float)(double)t;
UnityEngine.Vector3 V(string mark) { var m = built["marks"][mark]; return new UnityEngine.Vector3(F(m[0]), F(m[1]), F(m[2])); }

// ---- the models: no materials or rigs of their own ----
var modelNames = new System.Collections.Generic.List<string> { (string)built["islet"]["model"] };
foreach (var s in built["sections"]) modelNames.Add((string)s["model"]);
foreach (var name in modelNames)
{
    var path = Models + name + ".fbx";
    var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
    if (imp == null) { log.AppendLine("MISSING MODEL " + path + " (run tools/tower/world/world_tower.py)"); continue; }
    if (imp.materialImportMode != UnityEditor.ModelImporterMaterialImportMode.None || imp.importAnimation || imp.isReadable || imp.animationType != UnityEditor.ModelImporterAnimationType.None)
    {
        imp.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.None;
        imp.importAnimation = false;
        imp.animationType = UnityEditor.ModelImporterAnimationType.None;
        imp.importBlendShapes = false;
        imp.importCameras = false;
        imp.importLights = false;
        imp.isReadable = false;
        imp.meshCompression = UnityEditor.ModelImporterMeshCompression.Off;
        imp.SaveAndReimport();
        log.AppendLine("import settings " + name);
    }
}

// ---- the one material ----
var shader = UnityEngine.Shader.Find("Ravenfall/WorldTower");
if (!shader) { System.IO.File.WriteAllText(logPath, "no shader Ravenfall/WorldTower"); return "no shader"; }
var matPath = Folder + "/WorldTower.mat";
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(matPath);
if (!mat) { mat = new UnityEngine.Material(shader); UnityEditor.AssetDatabase.CreateAsset(mat, matPath); }
mat.shader = shader;
mat.SetFloat("_Glow", 1f); mat.SetFloat("_Pulse", 0f); mat.SetFloat("_GlowBoost", 2.4f); mat.SetColor("_GlowTint", UnityEngine.Color.white);
UnityEditor.EditorUtility.SetDirty(mat);
// the sign board's face (its pack's own material has a shader this project does not have)
var facePath = Folder + "/SignFace.mat";
var signFace = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(facePath);
if (!signFace) { signFace = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(signFace, facePath); }
signFace.SetColor("_BaseColor", new UnityEngine.Color(0.17f, 0.11f, 0.07f)); signFace.SetFloat("_Smoothness", 0.1f);
UnityEditor.EditorUtility.SetDirty(signFace);

// ---- the scene ----
var old = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
if (old.IsValid() && old.isLoaded) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(old, true);
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
var root = new UnityEngine.GameObject("World Tower");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
var sea = F(built["seaLevel"]);
var at = new UnityEngine.Vector3(F(built["position"][0]), sea, F(built["position"][1]));
root.transform.position = at;
// the gate (the models' -z once turned back) looks toward the point the stack names: the first island's middle
var toward = new UnityEngine.Vector3(F(built["faces"][0]) - at.x, 0f, F(built["faces"][1]) - at.z).normalized;
root.transform.rotation = UnityEngine.Quaternion.LookRotation(-toward, UnityEngine.Vector3.up);
var tower = root.AddComponent<WorldTower>();
tower.Shared = mat;
var vis = new UnityEngine.GameObject("Visuals").transform; vis.SetParent(root.transform, false);
vis.localScale = UnityEngine.Vector3.one * S;
tower.Visuals = vis.gameObject;
var place = new UnityEngine.GameObject("Place").transform; place.SetParent(root.transform, false);

var sections = new System.Collections.Generic.List<WorldTower.Section>();
var turning = new System.Collections.Generic.List<UnityEngine.Transform>();
var speeds = new System.Collections.Generic.List<float>();
var bobbing = new System.Collections.Generic.List<UnityEngine.Transform>();
var tris0 = 0; var tris1 = 0;

// a model under a holder, turned back half round (the FBX comes in with x and z negated), its parts by LOD
UnityEngine.GameObject Model(string name, UnityEngine.Transform parent, WorldTower.Section section)
{
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(Models + name + ".fbx");
    if (!asset) { log.AppendLine("missing model " + name); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset, scene);
    // plain objects from here on: the egg's door moves under its own holder, and the scene is rebuilt whenever the models are
    UnityEditor.PrefabUtility.UnpackPrefabInstance(go, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
    go.name = name;
    go.transform.SetParent(parent, false);
    go.transform.localPosition = UnityEngine.Vector3.zero;
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
    var near = new System.Collections.Generic.List<UnityEngine.Renderer>(section.Near);
    var far = new System.Collections.Generic.List<UnityEngine.Renderer>(section.Far);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        r.sharedMaterial = mat;
        r.receiveShadows = true;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        var mesh = r.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
        var t = mesh ? mesh.triangles.Length / 3 : 0;
        // the stone's carved tiers: the game shows the lit or the dark look of each (WorldTowerStone.cs), in potato
        // mode too, so they are in no detail list
        if (r.name.StartsWith("StoneTier")) { tris0 += t; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        else if (r.name.EndsWith("_Far")) { far.Add(r); r.enabled = false; tris1 += t; }
        else if (r.name.StartsWith("Flock") || r.name.StartsWith("Flame")) { }
        else { near.Add(r); tris0 += t; }
        if (r.name.StartsWith("Ring") || r.name.StartsWith("Clouds")) { turning.Add(r.transform); speeds.Add(r.name.StartsWith("Ring") ? 1.1f : -0.35f); }
        if (r.name.StartsWith("Rocks")) bobbing.Add(r.transform);
    }
    section.Near = near.ToArray(); section.Far = far.ToArray();
    return go;
}

// ---- the stack: one section per floor, then the cap ----
var stack = new UnityEngine.GameObject("Stack").transform; stack.SetParent(vis, false);
var builtTop = 0f;
UnityEngine.GameObject firstFloor = null;
foreach (var s in built["sections"])
{
    var floor = (int)s["floor"]; var cap = (bool)s["cap"];
    var holder = new UnityEngine.GameObject(cap ? "Cap (cloud)" : "Floor " + floor + " (" + (string)s["style"] + ")").transform;
    holder.SetParent(stack, false);
    holder.localPosition = new UnityEngine.Vector3(0f, F(s["base"]), 0f);
    var section = new WorldTower.Section { Floor = cap ? 0 : floor };
    var go = Model((string)s["model"], holder, section);
    if (!cap) builtTop = UnityEngine.Mathf.Max(builtTop, (F(s["base"]) + F(s["height"])) * S);
    if (floor == 1) firstFloor = go;
    sections.Add(section);
}

// ---- the foot: the islet, the plaza, the egg; their light is always on ----
var foot = new WorldTower.Section { Floor = 0, AlwaysLit = true };
var footHolder = new UnityEngine.GameObject("Foot").transform; footHolder.SetParent(vis, false);
var isletGo = Model((string)built["islet"]["model"], footHolder, foot);
UnityEngine.Transform Part(string name) { foreach (var t in isletGo.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == name) return t; log.AppendLine("no part " + name); return null; }
UnityEngine.Mesh MeshOf(string name) { var t = Part(name); return t ? t.GetComponent<UnityEngine.MeshFilter>().sharedMesh : null; }
// what can be stood on: the low detail islet, the plaza, the tower's own wall. Under Place, where the models are but
// apart from them, so the ground stays when what is seen is hidden
var ground = new UnityEngine.GameObject("Ground").transform; ground.SetParent(place, false);
UnityEngine.MeshCollider Solid(UnityEngine.Transform part)
{
    var go = new UnityEngine.GameObject(part.name + " (ground)");
    go.transform.SetParent(ground, false);
    go.transform.SetPositionAndRotation(part.position, part.rotation);
    go.transform.localScale = part.lossyScale;
    var c = go.AddComponent<UnityEngine.MeshCollider>();
    c.sharedMesh = part.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    return c;
}
var isletCollider = Solid(Part("Islet_Far"));
var plazaCollider = Solid(Part("Plaza_Near"));
if (firstFloor) foreach (var t in firstFloor.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == "Body_Far") Solid(t);
UnityEngine.Physics.SyncTransforms();
// the flame and the flock are templates: hidden here, copied where they are wanted
var flameMesh = MeshOf("Flame_Near"); var flockMesh = MeshOf("Flock_Near");
Part("Flame_Near").gameObject.SetActive(false); Part("Flock_Near").gameObject.SetActive(false);
var footNear = new System.Collections.Generic.List<UnityEngine.Renderer>(foot.Near);
// where the pieces came out, to check the turn: the plaza must lie toward -z
{
    var pb = Part("Plaza_Near").GetComponent<UnityEngine.Renderer>().bounds;
    var local = root.transform.InverseTransformPoint(pb.center);
    log.AppendLine("plaza middle (local) " + local.ToString("0.0") + " wanted " + V("plaza").ToString("0.0"));
}

// the ground under a point of the foot, in the models' own terms (Visuals')
UnityEngine.Vector3 Ground(float x, float z, float fallback = 4.85f)
{
    var from = vis.TransformPoint(new UnityEngine.Vector3(x, 120f, z));
    var ray = new UnityEngine.Ray(from, UnityEngine.Vector3.down);
    var best = float.MaxValue;
    foreach (var c in new[] { plazaCollider, isletCollider })
        if (c.Raycast(ray, out var hit, 200f * S) && hit.distance < best) best = hit.distance;
    return new UnityEngine.Vector3(x, best < float.MaxValue ? 120f - best / S : fallback, z);
}
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(vis, false);
var extras = new UnityEngine.GameObject("Extras").transform; extras.SetParent(vis, false);
tower.Extras = extras.gameObject;
var rng = new System.Random(24);
float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
var propTris = 0;
UnityEngine.GameObject Put(string path, UnityEngine.Vector3 local, float yaw, float scale, UnityEngine.Transform parent = null)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("MISSING PROP " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent ? parent : props, false);
    go.transform.localPosition = local;
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>()) if (mf.sharedMesh) propTris += mf.sharedMesh.triangles.Length / 3;
    // scenery only: nothing here is walked into, and the navmesh is made from the foot's own ground
    foreach (var c in go.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(c);
    return go;
}
// a fire in a bowl: the tower's own flame (it shows from far, and needs no particle system), and for some a lamp
void Fire(UnityEngine.Vector3 local, float size, bool lamp)
{
    var go = new UnityEngine.GameObject("Flame");
    go.transform.SetParent(props, false);
    go.transform.localPosition = local;
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, R(0f, 360f), 0f);
    go.transform.localScale = UnityEngine.Vector3.one * size;
    go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = flameMesh;
    var r = go.AddComponent<UnityEngine.MeshRenderer>();
    r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    footNear.Add(r);
    if (!lamp) return;
    var l = new UnityEngine.GameObject("Firelight").AddComponent<UnityEngine.Light>();
    l.transform.SetParent(extras, false);
    l.transform.localPosition = local + UnityEngine.Vector3.up * (1.2f * size);
    l.type = UnityEngine.LightType.Point; l.color = new UnityEngine.Color(1f, 0.72f, 0.42f); l.range = 16f * S; l.intensity = 3f; l.shadows = UnityEngine.LightShadows.None;
}

var plaza = V("plaza"); var plazaR = F(built["marks"]["plazaRadius"]); var gate = V("gate"); var level = F(built["marks"]["plazaLevel"]);
const string FK = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
const string Jungle = "Assets/PolygonNatureBiomes/PNB_Tropical_Jungle/Prefabs/";

// the hero climber on the pedestal, looking out over the plaza to the sea
var ped = V("pedestal");
{
    // of pale stone like the plaza, three times life size, facing the middle of the plaza
    var stonePath = Folder + "/Statue.mat";
    var stone = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(stonePath);
    if (!stone) { stone = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(stone, stonePath); }
    stone.SetColor("_BaseColor", new UnityEngine.Color(0.66f, 0.64f, 0.6f)); stone.SetFloat("_Smoothness", 0.12f);
    UnityEditor.EditorUtility.SetDirty(stone);
    var yaw = UnityEngine.Mathf.Atan2(plaza.x - ped.x, plaza.z - ped.z) * UnityEngine.Mathf.Rad2Deg;
    // where a statue stands: the stream's best climber is cast here by the game (WorldTowerClimbers.cs); until there
    // is one, the nameless hero
    var spot = new UnityEngine.GameObject("Statue spot").transform; spot.SetParent(vis, false);
    spot.localPosition = ped; spot.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    tower.StatueSpot = spot; tower.Stone = stone;
    var statue = Put(FK + "SM_Prop_Statue_Hero_01.prefab", ped, yaw, 3f, vis);
    if (statue)
    {
        statue.name = "Statue (the nameless hero)";
        foreach (var r in statue.GetComponentsInChildren<UnityEngine.Renderer>()) { var ms = r.sharedMaterials; for (var i = 0; i < ms.Length; i++) ms[i] = stone; r.sharedMaterials = ms; }
        // stood by the middle of what it is made of, not by its pivot
        var b = new UnityEngine.Bounds(); var first = true;
        foreach (var r in statue.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        var want = vis.TransformPoint(ped);
        statue.transform.position += new UnityEngine.Vector3(want.x - b.center.x, want.y - b.min.y, want.z - b.center.z);
        tower.GenericStatue = statue;
        // a climber in stone stands as tall as the hero does (the spot is under Visuals: in the models' metres)
        tower.StatueHeight = b.size.y / S;
        log.AppendLine("statue " + b.size.y.ToString("0.0") + " m tall in the world");
    }
}

// banners down the gate's pilasters
foreach (var side in new[] { -1f, 1f })
    Put("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Wall_Banner_01.prefab", new UnityEngine.Vector3(side * 9f, 23.4f, gate.z - 0.5f), 180f, 2.1f);

// braziers: at the stair's foot, at the head of the quay, and a bowl on every other post of the low wall
const string BigBrazier = "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_01.prefab";
const string Bowl = "Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab";
foreach (var side in new[] { -1f, 1f })
{
    // on the ends of the stair's cheeks (clear of the elevator's door beside the stair)
    var p = new UnityEngine.Vector3(side * 10.1f, gate.y + 1.3f, V("stairFoot").z + 0.4f);
    Put(BigBrazier, p, 0f, 1.25f);
    Fire(p + UnityEngine.Vector3.up * 2.05f, 1.5f, true);
    var q = new UnityEngine.Vector3(side * 6.6f, level + 0.12f, plaza.z - plazaR - 2.0f);
    Put(BigBrazier, q, 0f, 1.1f);
    Fire(q + UnityEngine.Vector3.up * 1.8f, 1.3f, true);
}
{
    // the wall's posts stand where world_tower.py put them: two arcs, seven posts each, between the quay and the stair
    const float quayGap = 0.34f, stairGap = 0.52f;
    var arcs = new[] { (-UnityEngine.Mathf.PI / 2 + quayGap, UnityEngine.Mathf.PI / 2 - stairGap), (UnityEngine.Mathf.PI / 2 + stairGap, 1.5f * UnityEngine.Mathf.PI - quayGap) };
    foreach (var (t0, t1) in arcs)
        for (var k = 0; k <= 6; k += 2)
        {
            var t = UnityEngine.Mathf.Lerp(t0, t1, k / 6f);
            var p = new UnityEngine.Vector3(plaza.x + (plazaR - 0.5f) * UnityEngine.Mathf.Cos(t), level + 2.6f, plaza.z + (plazaR - 0.5f) * UnityEngine.Mathf.Sin(t));
            Put(Bowl, p, R(0f, 360f), 1.25f);
            Fire(p + UnityEngine.Vector3.up * 0.5f, 0.95f, false);
        }
}

// the light from the gate, falling down the stair
{
    var l = new UnityEngine.GameObject("Gate light").AddComponent<UnityEngine.Light>();
    l.transform.SetParent(extras, false);
    l.transform.localPosition = new UnityEngine.Vector3(0f, 15f, gate.z - 4f);
    l.type = UnityEngine.LightType.Point; l.color = new UnityEngine.Color(1f, 0.8f, 0.5f); l.range = 48f * S; l.intensity = 4.5f; l.shadows = UnityEngine.LightShadows.None;
}

// ivy over the gate's shoulders
foreach (var (x, y, s) in new[] { (-10.5f, 27.2f, 3.2f), (10.8f, 27.2f, 2.8f), (-6f, 34.6f, 3f), (5.5f, 34.6f, 3.4f) })
    Put("Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_Ivy_Draped_01.prefab", new UnityEngine.Vector3(x, y, gate.z - 0.8f - (y > 30f ? 1.2f : 0f)), 180f, s);

// green round the plaza: palms and bushes on the grass at its sides, ferns and flowers along the wall and the stones
var palms = new[] { "SM_Env_Tree_Palm_01", "SM_Env_Tree_Palm_03", "SM_Env_Tree_Palm_02", "SM_Env_Tree_Palm_04", "SM_Env_Tree_Palm_03" };
var bushes = new[] { "SM_Env_Bush_Palm_01", "SM_Env_Bush_Palm_02" };
var ferns = new[] { "SM_Env_Fern_01", "SM_Env_Fern_02" };
var flowers = new[] { "SM_Env_Flowers_01", "SM_Env_Flowers_03" };
bool Clear(float x, float z)
{
    // not on the paving, the stair, the quay, nor in front of the stone and the statue
    var d = new UnityEngine.Vector2(x - plaza.x, z - plaza.z).magnitude;
    if (d < plazaR + 4.6f) return false;
    if (UnityEngine.Mathf.Abs(x) < 13f && z > plaza.z) return false;
    if (UnityEngine.Mathf.Abs(x) < 7f) return false;
    var slab = V("slab");
    if (new UnityEngine.Vector2(x - slab.x, z - slab.z).magnitude < 8.5f) return false;
    if (new UnityEngine.Vector2(x - ped.x, z - ped.z).magnitude < 6.5f) return false;
    var egg = V("egg");
    if (new UnityEngine.Vector2(x - egg.x, z - egg.z).magnitude < 9f) return false;
    return true;
}
var planted = 0;
for (var tries = 0; tries < 900 && planted < 64; tries++)
{
    var x = R(-52f, 52f); var z = R(plaza.z - plazaR - 12f, -80f);
    if (!Clear(x, z)) continue;
    var p = Ground(x, z, -100f);
    // on the level ground round the plaza and the low slopes beside it, not in the sea or up the cliff
    if (p.y < 2.2f || p.y > 13f) continue;
    var pick = planted % 8;
    if (pick < 2) Put(Jungle + palms[rng.Next(palms.Length)] + ".prefab", p - UnityEngine.Vector3.up * 0.15f, R(0f, 360f), R(1.5f, 2.3f));
    else if (pick < 4) Put(Jungle + bushes[rng.Next(bushes.Length)] + ".prefab", p, R(0f, 360f), R(1.2f, 1.8f));
    else if (pick < 7) Put(Jungle + ferns[rng.Next(ferns.Length)] + ".prefab", p, R(0f, 360f), R(1.3f, 2.2f));
    else Put(Jungle + flowers[rng.Next(flowers.Length)] + ".prefab", p, R(0f, 360f), R(1.6f, 2.4f));
    planted++;
}
log.AppendLine("planted " + planted);
// (no Synty rocks: the jungle pack's are brown and textured and sat on the grey islet like something dropped; the
// islet's own boulders are part of its model)

// the pier: the Vikings' dock pieces out from the quay's stone landing, a few barrels and crates on the landing
var quay = V("quayHead");
const int PierPieces = 3;
for (var k = 0; k < PierPieces; k++)
    Put("Assets/PolygonVikings/Prefabs/Props/SM_Prop_Dock_0" + (k == PierPieces - 1 ? 2 : 1) + ".prefab", new UnityEngine.Vector3(0f, quay.y - 0.05f, quay.z - 2.5f - 5f * k), 0f, 1f);
var pierEnd = new UnityEngine.Vector3(0f, quay.y, quay.z - 5f * PierPieces);
// the pier's planks are walked on: one flat deck for the ground, level with the landing (the pieces' own shapes have
// posts and end beams the navmesh will not cross, which cut the pier in two at the first seam)
{
    var deck = new UnityEngine.GameObject("Pier deck (ground)");
    deck.transform.SetParent(ground, false);
    var a = vis.TransformPoint(new UnityEngine.Vector3(0f, quay.y, quay.z + 0.5f));
    var b = vis.TransformPoint(new UnityEngine.Vector3(0f, quay.y, pierEnd.z + 0.3f));
    deck.transform.SetPositionAndRotation((a + b) / 2f - UnityEngine.Vector3.up * 0.1f, UnityEngine.Quaternion.LookRotation((b - a).normalized, UnityEngine.Vector3.up));
    deck.AddComponent<UnityEngine.BoxCollider>().size = new UnityEngine.Vector3(3.4f * S, 0.2f, (b - a).magnitude);
}
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(-3.9f, quay.y, quay.z + 3.6f), 20f, 1.25f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(-3.6f, quay.y, quay.z + 2.2f), 80f, 1.25f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Crate_01.prefab", new UnityEngine.Vector3(3.8f, quay.y, quay.z + 3.4f), 12f, 1.4f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Crate_01.prefab", new UnityEngine.Vector3(3.6f, quay.y, quay.z + 1.7f), 40f, 1.2f);

// ---- the best climbers' stone (the tower stele, design C): its words where the model's marks say (the stream's
// place and the floor's name, the game has those at hand; the climbers' rows stay carved runes until the game is given
// a list), the carved tower's tiers in their lit and dark looks, and each tier's floor number ----
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/FOT-NewRodin Pro B SDF.asset");
if (!font) log.AppendLine("MISSING FONT FOT-NewRodin Pro B SDF");
TMPro.TextMeshPro Words(string name, UnityEngine.Transform parent, UnityEngine.Vector3 local, UnityEngine.Quaternion turn, float w, float h, UnityEngine.Color color, string sample, float max)
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.localPosition = local; go.transform.localRotation = turn;
    var tmp = go.AddComponent<TMPro.TextMeshPro>();
    if (font) tmp.font = font;
    tmp.rectTransform.sizeDelta = new UnityEngine.Vector2(w, h);
    tmp.enableAutoSizing = true; tmp.fontSizeMin = 1f; tmp.fontSizeMax = max;
    tmp.alignment = TMPro.TextAlignmentOptions.Center;
    tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
    tmp.color = color;
    tmp.fontStyle = TMPro.FontStyles.Normal; // the font is the bold cut already
    tmp.text = sample;
    tmp.GetComponent<UnityEngine.MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    return tmp;
}
{
    var slab = V("slab");
    var stele = built["marks"]["stele"];
    var boxes = stele["boxes"];
    var facing = F(built["marks"]["slabFacing"]) * UnityEngine.Mathf.Deg2Rad;
    var fwd = new UnityEngine.Vector3(UnityEngine.Mathf.Cos(facing), 0f, UnityEngine.Mathf.Sin(facing));
    // a viewer's right as they face the stone, as the model's face terms have it
    var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, -fwd).normalized;
    var turn = UnityEngine.Quaternion.LookRotation(-fwd, UnityEngine.Vector3.up);   // words are read looking along +z
    var gold = new UnityEngine.Color(1f, 0.86f, 0.45f);
    var pale = new UnityEngine.Color(0.92f, 0.9f, 0.84f);
    var holder = new UnityEngine.GameObject("Stone words").transform; holder.SetParent(vis, false);
    // a box is [x, y, width, height, out] in the face's terms
    UnityEngine.Vector3 At(Newtonsoft.Json.Linq.JToken box) => slab + right * F(box[0]) + UnityEngine.Vector3.up * F(box[1]) + fwd * F(box[4]);
    TMPro.TextMeshPro Box(string name, string key, UnityEngine.Color color, string sample, float max)
    {
        var box = boxes[key];
        var tmp = Words(name, holder, At(box), turn, F(box[2]), F(box[3]), color, sample, max);
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return tmp;
    }
    tower.SlabPlace = Box("Place", "place", gold, "FLOOR 1 - PART 1", 80f);
    tower.SlabName = Box("Floor name", "name", pale, "The Overgrown Halls", 40f);
    tower.ClimbersTitle = Box("Climbers title", "title", pale, "", 50f);
    var rows = new TMPro.TextMeshPro[5];
    for (var i = 0; i < rows.Length; i++)
    {
        rows[i] = Box("Climber " + (i + 1), "row" + (i + 1), i == 0 ? gold : pale, "", 60f);
        rows[i].overflowMode = TMPro.TextOverflowModes.Overflow;
    }
    tower.ClimberRows = rows;
    // each row's carved runes, shown while that row has no name
    var rowRunes = new UnityEngine.GameObject[rows.Length];
    for (var i = 0; i < rows.Length; i++) { var r = Part("Runes" + i + "_Near"); rowRunes[i] = r ? r.gameObject : null; }
    tower.RowRunes = rowRunes;
    // the carved tower: each tier's lit and dark look (both made in Blender), and its number in its window
    var tiers = (Newtonsoft.Json.Linq.JArray)stele["tiers"];
    var lit = new System.Collections.Generic.List<UnityEngine.Renderer>();
    var dark = new System.Collections.Generic.List<UnityEngine.GameObject>();
    var numbers = new System.Collections.Generic.List<TMPro.TextMeshPro>();
    for (var i = 0; i < tiers.Count; i++)
    {
        var l = Part("StoneTier" + i + "Lit_Near"); var d = Part("StoneTier" + i + "Dark_Near");
        if (!l || !d) break;
        lit.Add(l.GetComponent<UnityEngine.Renderer>()); dark.Add(d.gameObject);
        l.gameObject.SetActive(false);
        var num = Words("Tier " + (i + 1) + " number", holder, At(tiers[i]), turn, F(tiers[i][2]), F(tiers[i][3]), gold, "", 20f);
        num.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        numbers.Add(num);
    }
    tower.StoneTierLit = lit.ToArray(); tower.StoneTierDark = dark.ToArray(); tower.StoneTierNumbers = numbers.ToArray();
    log.AppendLine("stone: " + lit.Count + " carved tiers");
}

// ---- the plaque on the statue's pedestal: who stands there (WorldTowerClimbers.cs writes the name and hides the
// plaque while the nameless hero does). A dark plate at the head of the pedestal's front, over its sigil, where the
// plaza's wall does not hide it; the name in the stone's gold ----
{
    var platePath = Folder + "/Plaque.mat";
    var plateMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(platePath);
    if (!plateMat) { plateMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(plateMat, platePath); }
    plateMat.SetColor("_BaseColor", new UnityEngine.Color(0.2f, 0.2f, 0.23f)); plateMat.SetFloat("_Smoothness", 0.25f);
    UnityEditor.EditorUtility.SetDirty(plateMat);
    var holder = new UnityEngine.GameObject("Statue plaque").transform; holder.SetParent(vis, false);
    // the pedestal's shaft is 5.6 m across and its cap 6.4 m, their front faces 2.8 and 3.2 m out toward the sea
    holder.localPosition = new UnityEngine.Vector3(ped.x, ped.y - 0.62f, ped.z - 3.2f);
    var plate = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    plate.name = "Plate";
    UnityEngine.Object.DestroyImmediate(plate.GetComponent<UnityEngine.Collider>());
    plate.transform.SetParent(holder, false);
    plate.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -0.06f);
    plate.transform.localScale = new UnityEngine.Vector3(5.7f, 1.08f, 0.18f);
    plate.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = plateMat;
    // read from the sea's side, looking along +z
    tower.StatueName = Words("Name", holder, new UnityEngine.Vector3(0f, 0f, -0.17f), UnityEngine.Quaternion.identity, 5.3f, 0.86f, new UnityEngine.Color(1f, 0.86f, 0.45f), "", 60f);
    tower.StatueName.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    tower.StatuePlaque = holder.gameObject;
    holder.gameObject.SetActive(false);
}

// ---- the easter eggs (docs/easter-eggs.md): the elevator that is out of order with its sign, and the ferry's
// timetable on the quay ----
// a sign board with words from a Localization field, under a gag of its own
WorldGag SignGag(string name, string gagKey, UnityEngine.Vector3 local, float yaw, string key, string fallback)
{
    var eggRoot = new UnityEngine.GameObject(name);
    eggRoot.transform.SetParent(vis, false);
    var gag = eggRoot.AddComponent<WorldGag>();
    gag.Key = gagKey;
    gag.Texts = new TMPro.TextMeshPro[0]; gag.TextKeys = new string[0];
    var post = Put("Assets/Polygonal Fantasy Pack/Environment Prefabs/Misc/Text Signboard 01.prefab", local, yaw, 2.3f, eggRoot.transform);
    if (!post) return gag;
    UnityEngine.Transform face = null;
    foreach (var mf in post.GetComponentsInChildren<UnityEngine.MeshFilter>()) if (mf.name.EndsWith("Text")) face = mf.transform;
    if (!face) { log.AppendLine("no text face on the sign board"); return gag; }
    var mb = face.GetComponent<UnityEngine.MeshFilter>().sharedMesh.bounds;
    face.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = signFace;
    var field = typeof(Localization).GetField(key);
    var words = Words("Words", face, new UnityEngine.Vector3(mb.center.x, mb.center.y, mb.max.z + 0.006f), UnityEngine.Quaternion.Euler(0f, 180f, 0f), mb.size.x * 0.86f, mb.size.y * 0.84f,
        new UnityEngine.Color(0.97f, 0.91f, 0.74f), field != null ? (string)field.GetValue(null) : fallback, 1.2f);
    words.fontSizeMin = 0.05f; words.lineSpacing = -12f;
    gag.Texts = new[] { words }; gag.TextKeys = new[] { key };
    return gag;
}
{
    // between the stair and the door, turned to the plaza (beside the stone it would stand hidden behind it)
    var eggAt = V("egg");
    var elevator = SignGag("Egg (elevator)", "tower-elevator-out-of-order", Ground(eggAt.x - 3.6f, eggAt.z - 2.2f) - UnityEngine.Vector3.up * 0.05f, 180f + 14f,
        "UI_GAG_SIGN_ELEVATOR", "OUT OF ORDER. PLEASE TAKE THE STAIRS. ALL OF THEM");
    var door = Part("Egg_Near");
    door.SetParent(elevator.transform, true);
    tower.Egg = elevator;
    // on the stone landing at the head of the pier, turned to whoever comes up it
    var timetable = SignGag("Egg (ferry timetable)", "tower-ferry-timetable", new UnityEngine.Vector3(-4.3f, quay.y - 0.05f, quay.z + 0.9f), 180f - 12f,
        "UI_GAG_SIGN_FERRY_TIMES", "FERRY TIMETABLE: WHEN SOMEONE ASKS FOR IT. ASK NICELY");
    tower.MoreEggs = new[] { timetable };
}

// ---- gulls: three flocks at different heights, each on a pivot the game turns round the tower ----
var flocks = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var (radius, height, size) in new[] { (128f, 46f, 1.5f), (112f, 150f, 1.7f), (150f, 96f, 1.5f), (96f, 236f, 1.8f) })
{
    var pivot = new UnityEngine.GameObject("Gulls").transform; pivot.SetParent(vis, false);
    pivot.localRotation = UnityEngine.Quaternion.Euler(0f, R(0f, 360f), 0f);
    var go = new UnityEngine.GameObject("Flock");
    go.transform.SetParent(pivot, false);
    go.transform.localPosition = new UnityEngine.Vector3(radius, height, 0f);
    go.transform.localScale = UnityEngine.Vector3.one * size;
    go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = flockMesh;
    var r = go.AddComponent<UnityEngine.MeshRenderer>();
    r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    footNear.Add(r);
    flocks.Add(pivot);
}

foot.Near = footNear.ToArray();
sections.Add(foot);
tower.Sections = sections.ToArray();
tower.Turning = turning.ToArray(); tower.TurnSpeeds = speeds.ToArray();
tower.Bobbing = bobbing.ToArray(); tower.Flocks = flocks.ToArray();

// ---- how the top goes into cloud (WorldTower.cs): the heights follow the stack ----
var capTop = F(built["height"]) * S;
tower.BuiltHeight = builtTop;
// the shaft stands clear for a little over the last section and goes into the sky's bank of cloud round it, fading
// toward the cloud's color; over the cloud layer (SkyClouds.Ceiling) it fades on toward the live sky behind it and is
// gone a little under its own end (the shader, WorldTower.cs)
tower.CloudFrom = builtTop + 25f;
tower.CloudRadius = 340f;
tower.FadeFrom = builtTop + 35f;
// gone a little over the cloud layer: what the bank does not hide is a short stub already in the sky's color
tower.FadeTo = UnityEngine.Mathf.Min(capTop - 8f, SkyClouds.Ceiling - sea + 65f);
tower.SkyFrom = SkyClouds.Ceiling - sea - 20f;
tower.SkyBlend = 70f;
log.AppendLine("built to " + builtTop.ToString("0") + " m, shaft to " + capTop.ToString("0") + " m, fades " + tower.FadeFrom.ToString("0") + " to " + tower.FadeTo.ToString("0") + " m (toward the sky from " + tower.SkyFrom.ToString("0") + " m), cloud bank from " + tower.CloudFrom.ToString("0") + " m");

// ---- ramps over the steps (the quay's and the stair's), unseen: the navmesh goes up them whatever the steps' height ----
void Ramp(string name, UnityEngine.Vector3 low, UnityEngine.Vector3 high, float width)
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(ground, false);
    var a = vis.TransformPoint(low); var b = vis.TransformPoint(high);
    var along = b - a;
    go.transform.SetPositionAndRotation((a + b) / 2f - UnityEngine.Vector3.up * 0.1f, UnityEngine.Quaternion.LookRotation(along.normalized, UnityEngine.Vector3.up));
    var box = go.AddComponent<UnityEngine.BoxCollider>();
    box.size = new UnityEngine.Vector3(width * S, 0.2f, along.magnitude + 0.6f);
}
Ramp("Ramp (quay steps)", new UnityEngine.Vector3(0f, quay.y, quay.z + 5f), new UnityEngine.Vector3(0f, level + 0.12f, quay.z + 5f + 1.3f * 7f), 8.6f);
Ramp("Ramp (stair)", V("stairFoot"), V("gate"), 11f);
UnityEngine.Physics.SyncTransforms();

// ---- the sea floor south of the world's terrain. The terrain ends in a straight line 150 m behind the tower (its
// southern edge, z -853 today), and there the sea turned from the terrain's 26 m of water to the open deep at once:
// from the islands it read as a lighter patch of sea round the islet with straight sides and sharp corners. This strip
// carries the floor on from that edge, at the terrain's own height along it, and slopes it away into the deep over
// 600 m, so the shallows fade out as they do round the other islands. In world terms, beside the tower's root; no
// collider (nothing stands on it), no shadows ----
{
    var terrains = new System.Collections.Generic.List<UnityEngine.Terrain>();
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsInactive.Exclude))
        if (t.terrainData && t.gameObject.scene.name == "MainWorld") terrains.Add(t);
    var edgeZ = float.MaxValue;
    foreach (var t in terrains) edgeZ = UnityEngine.Mathf.Min(edgeZ, t.transform.position.z);
    float x0 = float.MaxValue, x1 = float.MinValue;
    var edge = new System.Collections.Generic.List<UnityEngine.Terrain>();
    foreach (var t in terrains)
        if (UnityEngine.Mathf.Abs(t.transform.position.z - edgeZ) < 1f)
        {
            edge.Add(t);
            x0 = UnityEngine.Mathf.Min(x0, t.transform.position.x);
            x1 = UnityEngine.Mathf.Max(x1, t.transform.position.x + t.terrainData.size.x);
        }
    if (edge.Count == 0) log.AppendLine("sea floor: no terrain found, none made");
    else
    {
        float Height(float x)
        {
            foreach (var t in edge)
                if (x >= t.transform.position.x - 0.01f && x <= t.transform.position.x + t.terrainData.size.x + 0.01f)
                    return t.SampleHeight(new UnityEngine.Vector3(x, 0f, edgeZ + 0.05f)) + t.transform.position.y;
            return sea - 26f;
        }
        const float Step = 40f, Reach = 600f, Drop = 150f;
        var outs = new[] { 0f, 25f, 60f, 110f, 170f, 240f, 320f, 410f, 500f, 600f };
        var columns = UnityEngine.Mathf.CeilToInt((x1 - x0) / Step) + 1;
        var verts = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        for (var c = 0; c < columns; c++)
        {
            var x = UnityEngine.Mathf.Min(x0 + c * Step, x1);
            var y0 = Height(x);
            foreach (var d in outs)
            {
                // eased in: level with the terrain at the seam, steepest half way out
                var drop = Drop * 0.5f * (1f - UnityEngine.Mathf.Cos(UnityEngine.Mathf.PI * d / Reach));
                verts.Add(new UnityEngine.Vector3(x, y0 - drop, edgeZ - d));
            }
        }
        var n = outs.Length;
        for (var c = 0; c < columns - 1; c++)
            for (var r = 0; r < n - 1; r++)
            {
                int a = c * n + r, b = a + 1, cc = a + n, dd = cc + 1;
                tris.AddRange(new[] { a, b, cc, b, dd, cc });
            }
        var meshPath = DataFolder + "/SeaFloor.asset";
        UnityEditor.AssetDatabase.DeleteAsset(meshPath);
        var mesh = new UnityEngine.Mesh { name = "World tower sea floor" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // facing up (the winding above faces up for a strip running south)
        if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f) { tris.Reverse(); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); }
        UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
        var floorPath = Folder + "/SeaFloor.mat";
        var floorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(floorPath);
        if (!floorMat) { floorMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(floorMat, floorPath); }
        floorMat.SetColor("_BaseColor", new UnityEngine.Color(0.34f, 0.32f, 0.26f)); floorMat.SetFloat("_Smoothness", 0.05f);
        UnityEditor.EditorUtility.SetDirty(floorMat);
        var floorGo = new UnityEngine.GameObject("World Tower sea floor");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floorGo, scene);
        floorGo.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        var fr = floorGo.AddComponent<UnityEngine.MeshRenderer>();
        fr.sharedMaterial = floorMat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fr.receiveShadows = false;
        fr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        fr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        log.AppendLine("sea floor: from the terrain's edge at z " + edgeZ.ToString("0") + ", x " + x0.ToString("0") + " to " + x1.ToString("0") + ", " + (tris.Count / 3) + " triangles, seam heights " + Height(0f).ToString("0.0") + " at x 0");
    }
}

// ---- a navmesh for the plaza, the stair into the gate's tunnel, the quay and the pier ----
var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
// the entrance only: not the sea floor, not the cliffs
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Volume;
var navNear = pierEnd.z - 1f; var navFar = V("tunnelEnd").z + 2f;
surface.center = new UnityEngine.Vector3(0f, 21f, (navNear + navFar) / 2f) * S;
surface.size = new UnityEngine.Vector3(104f, 40f, navFar - navNear) * S;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = 25556799;
surface.BuildNavMesh();
var navPath = DataFolder + "/NavMesh.asset";
if (surface.navMeshData) { UnityEditor.AssetDatabase.DeleteAsset(navPath); UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath); log.AppendLine("navmesh " + surface.navMeshData.sourceBounds); }
bool Walk(UnityEngine.Vector3 fromModel, UnityEngine.Vector3 toModel, out UnityEngine.Vector3 onMesh)
{
    var a = vis.TransformPoint(fromModel + UnityEngine.Vector3.up * 0.2f);
    var b = vis.TransformPoint(toModel + UnityEngine.Vector3.up * 0.2f);
    onMesh = a;
    var okA = UnityEngine.AI.NavMesh.SamplePosition(a, out var ha, 3f, -1); var okB = UnityEngine.AI.NavMesh.SamplePosition(b, out var hb, 3f, -1);
    if (okA) onMesh = ha.position;
    var path = new UnityEngine.AI.NavMeshPath();
    return okA && okB && UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, -1, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
}
var tunnelMark = V("tunnelEnd") + new UnityEngine.Vector3(0f, 0f, -5f);    // a little short of the tunnel's end wall
var gateMark = V("gate") + new UnityEngine.Vector3(0f, 0f, 2f);
var landing = quay + new UnityEngine.Vector3(0f, 0f, 2.5f);
var pierSpot = pierEnd + new UnityEngine.Vector3(0f, 0f, 2.2f);
var walkLanding = Walk(landing, gateMark, out var landingOn);
var walkPier = Walk(pierSpot, gateMark, out var pierOn);
var walkTunnel = Walk(gateMark, tunnelMark, out var gateOn);
var walkPlaza = Walk(plaza, gateMark, out var plazaOn);
log.AppendLine("walk: landing to gate " + (walkLanding ? "yes" : "NO") + ", pier's end to gate " + (walkPier ? "yes" : "NO") + ", gate to tunnel's end " + (walkTunnel ? "yes" : "NO") + ", plaza to gate " + (walkPlaza ? "yes" : "NO"));

// ---- the islet as a place (WorldTowerVisit.cs): an island like the others, its dock, where to stand ----
UnityEngine.Transform Mark(string name, UnityEngine.Vector3 world, UnityEngine.Transform parent = null)
{
    var m = new UnityEngine.GameObject(name).transform; m.SetParent(parent ? parent : place, false);
    m.position = world; m.rotation = root.transform.rotation;
    return m;
}
IslandController like = null;
foreach (var other in UnityEngine.Object.FindObjectsByType<IslandController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (other.Identifier == "Home") like = other;
if (!like) log.AppendLine("NO HOME ISLAND FOUND (is MainWorld open?): the islet's layer and tag are the defaults");
{
    // the sphere takes in the tunnel, the plaza, the quay, the pier and the ferry's stop off its end, and no other island's
    var middle = new UnityEngine.Vector3(0f, 8f, -108f);
    var islandGo = new UnityEngine.GameObject("Tower islet");
    islandGo.transform.SetParent(place, false);
    islandGo.transform.position = vis.TransformPoint(middle);
    if (like) { islandGo.layer = like.gameObject.layer; islandGo.tag = like.gameObject.tag; }
    var sphere = islandGo.AddComponent<UnityEngine.SphereCollider>();
    sphere.isTrigger = true; sphere.center = UnityEngine.Vector3.zero; sphere.radius = 112f;
    var island = islandGo.AddComponent<IslandController>();
    island.Island = WorldTower.IslandValue;
    island.Identifier = "Tower";
    island.LevelRequirement = 1;
    island.OpenToAllLevels = true;
    island.AllowRaidWar = false;
    island.SpawnPositionTransform = Mark("Spawn", plazaOn, islandGo.transform);
    island.CameraPanTarget = Mark("Camera pan target", vis.TransformPoint(plaza + UnityEngine.Vector3.up * 2f), islandGo.transform);
    tower.Islet = island;

    // the dock: at the pier's end if the planks can be walked, else on the stone landing
    var dockGo = new UnityEngine.GameObject("Dock");
    dockGo.transform.SetParent(islandGo.transform, false);
    var dockAt = walkPier ? pierOn : landingOn;
    dockGo.transform.position = dockAt;
    dockGo.transform.rotation = root.transform.rotation;
    if (like && like.DockingArea) { dockGo.layer = like.DockingArea.gameObject.layer; dockGo.tag = like.DockingArea.gameObject.tag; }
    var dockBox = dockGo.AddComponent<UnityEngine.BoxCollider>();
    dockBox.isTrigger = true; dockBox.center = UnityEngine.Vector3.up * 1f; dockBox.size = new UnityEngine.Vector3(4.5f, 3f, 4.5f);
    var dock = dockGo.AddComponent<DockController>();
    dock.DockPositionTransform = Mark("Dock position", dockAt, dockGo.transform);
    island.DockingArea = dock;
    log.AppendLine("dock at " + dockAt.ToString("0.0") + (walkPier ? " (the pier's end)" : " (the landing: the pier is not walkable)"));

    // places to stand on the plaza: two rings, clear of the way from the quay to the stair
    var spots = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (var (around, count, turnBy) in new[] { (plazaR * 0.42f, 6, 0.4f), (plazaR * 0.74f, 10, 0.1f) })
        for (var k = 0; k < count; k++)
        {
            var t = turnBy + k * UnityEngine.Mathf.PI * 2f / count;
            var p = new UnityEngine.Vector3(plaza.x + around * UnityEngine.Mathf.Cos(t), plaza.y, plaza.z + around * UnityEngine.Mathf.Sin(t));
            if (UnityEngine.Mathf.Abs(p.x - plaza.x) < 3.2f) continue;
            var world = vis.TransformPoint(p + UnityEngine.Vector3.up * 0.2f);
            if (!UnityEngine.AI.NavMesh.SamplePosition(world, out var hit, 2f, -1)) continue;
            var m = Mark("Spot " + (spots.Count + 1), hit.position);
            spots.Add(m);
        }
    tower.Spots = spots.ToArray();
    tower.Gate = Mark("Gate", gateOn);
    tower.TunnelEnd = Mark("Tunnel end", vis.TransformPoint(tunnelMark));
    log.AppendLine("islet: sphere at " + islandGo.transform.position.ToString("0") + " radius " + sphere.radius + ", " + spots.Count + " places on the plaza, layer " + islandGo.layer);
    // no other island's sphere may reach into this one
    foreach (var other in UnityEngine.Object.FindObjectsByType<IslandController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    {
        if (other == island) continue;
        var os = other.GetComponent<UnityEngine.SphereCollider>();
        if (!os) continue;
        var gap = UnityEngine.Vector3.Distance(other.transform.TransformPoint(os.center), islandGo.transform.position) - os.radius - sphere.radius;
        if (gap < 20f) log.AppendLine((gap < 0f ? "OVERLAPS " : "close to ") + other.Identifier + " by " + gap.ToString("0") + " m");
    }

    // ---- the ferry's way to the pier and on (PathSelector.RegisterStop): after Heim it turns in to a stop off the
    // pier's end, then goes on to Atria. In the world's own terms, like the ring's splines: a holder at the origin ----
    UnityEngine.Splines.SplineContainer ring = null;
    foreach (var sc in UnityEngine.Object.FindObjectsByType<UnityEngine.Splines.SplineContainer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
        if (sc.name == "Heim => Atria") ring = sc;
    if (!ring) log.AppendLine("NO FERRY RING FOUND (Heim => Atria; is MainWorld open?): no ferry stop");
    else
    {
        var ferryRoot = new UnityEngine.GameObject("World Tower ferry");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ferryRoot, scene);
        var front = -root.transform.forward; var side = root.transform.right;      // the way the gate faces, and across it
        var seaY = ((UnityEngine.Vector3)ring.EvaluatePosition(0f)).y;
        // a point by how far out from the tower's middle along the gate's line, and how far to the side
        UnityEngine.Vector3 Off(float along, float across) { var p = root.transform.position + front * along + side * across; p.y = seaY; return p; }
        // which side Heim lies on: the ferry comes in from there and leaves by the other
        var heim = (UnityEngine.Vector3)ring.EvaluatePosition(0f);
        var inSide = UnityEngine.Vector3.Dot(heim - root.transform.position, side) < 0f ? -1f : 1f;
        var stopAlong = -pierEnd.z * S + 8f;
        var stop = Off(stopAlong, 0f);
        UnityEngine.Splines.SplineContainer Way(string name, System.Collections.Generic.List<UnityEngine.Vector3> points)
        {
            var go = new UnityEngine.GameObject(name);
            go.transform.SetParent(ferryRoot.transform, false);
            var container = go.AddComponent<UnityEngine.Splines.SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var p in points) spline.Add(new UnityEngine.Splines.BezierKnot(new Unity.Mathematics.float3(p.x, seaY, p.z)), UnityEngine.Splines.TangentMode.AutoSmooth);
            spline.Closed = false;
            return container;
        }
        var to = new System.Collections.Generic.List<UnityEngine.Vector3>();
        for (var i = 0; i <= 4; i++) to.Add(ring.EvaluatePosition(i / 16f));      // out of Heim as the ring goes
        to.Add(Off(stopAlong + 85f, inSide * 85f)); to.Add(Off(stopAlong + 35f, inSide * 60f)); to.Add(Off(stopAlong + 7f, inSide * 32f)); to.Add(stop);
        var from = new System.Collections.Generic.List<UnityEngine.Vector3> { stop, Off(stopAlong + 7f, -inSide * 30f), Off(stopAlong + 25f, -inSide * 62f) };
        for (var i = 8; i <= 16; i++) from.Add(ring.EvaluatePosition(i / 16f));   // and on into Atria as the ring goes
        tower.FerryTo = Way("Heim => Tower", to);
        tower.FerryFrom = Way("Tower => Atria", from);
        tower.FerryAfterPath = 3; tower.FerryNext = "Atria";
        // the stop must lie in the islet's sphere (the ferry asks which island it has docked at) and both ways over water
        log.AppendLine("ferry stop " + stop.ToString("0.0") + ", " + UnityEngine.Vector3.Distance(stop, islandGo.transform.position).ToString("0") + " m from the sphere's middle (radius " + sphere.radius + "), " + UnityEngine.Vector3.Distance(stop, dockAt).ToString("0") + " m from the dock");
        try
        {
            var chart = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "Sailing", "sea-chart.json")));
            float cell = F(chart["cell"]), ox = F(chart["originX"]), oz = F(chart["originZ"]); int cw = (int)chart["width"], ch = (int)chart["height"];
            var bits = System.Convert.FromBase64String((string)chart["water"]);
            bool Water(UnityEngine.Vector3 p) { var x = UnityEngine.Mathf.FloorToInt((p.x - ox) / cell); var z = UnityEngine.Mathf.FloorToInt((p.z - oz) / cell); if (x < 0 || z < 0 || x >= cw || z >= ch) return false; var i = z * cw + x; return (bits[i >> 3] & (1 << (i & 7))) != 0; }
            foreach (var way in new[] { tower.FerryTo, tower.FerryFrom })
            {
                var length = way.Spline.GetLength(); var dry = 0; var count = 0; var worst = "";
                for (var d = 30f; d < length - 30f; d += 5f)
                {
                    var p = (UnityEngine.Vector3)UnityEngine.Splines.SplineUtility.EvaluatePosition(way.Spline, d / length);
                    count++;
                    if (!Water(p)) { dry++; if (worst.Length < 80) worst += " " + p.x.ToString("0") + "," + p.z.ToString("0"); }
                }
                log.AppendLine("ferry way " + way.name + ": " + length.ToString("0") + " m, " + (dry == 0 ? "all over water" : "NOT WATER at " + dry + " of " + count + ":" + worst));
            }
            var end = ((UnityEngine.Vector3)UnityEngine.Splines.SplineUtility.EvaluateTangent(tower.FerryTo.Spline, 1f)).normalized;
            log.AppendLine("ferry heading at the stop " + (UnityEngine.Mathf.Atan2(end.x, end.z) * UnityEngine.Mathf.Rad2Deg).ToString("0.0"));
            foreach (var d in chart["docks"]) if ((string)d["island"] == "Tower") log.AppendLine("chart dock Tower " + F(d["x"]).ToString("0.0") + "," + F(d["z"]).ToString("0.0") + " heading " + F(d["heading"]).ToString("0.0") + ", " + UnityEngine.Vector2.Distance(new UnityEngine.Vector2(F(d["x"]), F(d["z"])), new UnityEngine.Vector2(stop.x, stop.z)).ToString("0.0") + " m from the ferry's stop");
        }
        catch (System.Exception e) { log.AppendLine("sea chart check failed: " + e.Message); }
    }
}

UnityEditor.EditorUtility.SetDirty(tower);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.AssetDatabase.SaveAssets();
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
log.AppendLine("triangles: tower near " + tris0 + ", far " + tris1 + ", props " + propTris);
log.AppendLine("tower at " + at.ToString("0.0") + " scale " + S + " height " + (F(built["height"]) * S).ToString("0") + " sections " + sections.Count);
log.AppendLine(saved ? "SAVED " + ScenePath : "SAVE FAILED");
}
catch (System.Exception e) { log.AppendLine("EXCEPTION " + e); }
System.IO.File.WriteAllText(logPath, log.ToString());
return "world tower built";
