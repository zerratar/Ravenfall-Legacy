// Builds Assets/Scenes/Tower/WorldTower.unity: the tower that stands in the main world (docs/world-tower.md), from the
// models tools/tower/world/world_tower.py made and tools/tower/world/stack-built.json (the stack, the marks on the
// plaza). Run with the Unity CLI while the editor is not in play mode:
//   unity command eval_file "<full path>\tools\tower\world\world-tower-build.cs"
// It writes its log to %TEMP%/world-tower-build.txt (the eval call itself may time out while the build keeps going).
// The scene is made in its own additive scene, saved and closed again: the open scene (MainWorld) is not touched.
// The game loads it beside the main world at the start (WorldTowerScene in Assets/Scripts/Game/Tower/WorldTower.cs).
//
// The tower's own shapes are the Blender models, all drawn with one material (Ravenfall/WorldTower). The plaza is
// dressed with Synty props: the hero's statue, the banners, braziers, palms, ferns, rocks, the pier, the sign.
var log = new System.Text.StringBuilder();
var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "world-tower-build.txt");
try
{
if (UnityEngine.Application.isPlaying) { System.IO.File.WriteAllText(logPath, "not in play mode"); return "not in play mode"; }
var repo = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
var built = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(repo, "tools", "tower", "world", "stack-built.json")));
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
        if (r.name.EndsWith("_Far")) { far.Add(r); r.enabled = false; tris1 += t; }
        else if (r.name.StartsWith("Flock") || r.name.StartsWith("Flame")) { }
        else { near.Add(r); tris0 += t; }
        if (r.name.StartsWith("Ring") || r.name.StartsWith("Clouds")) { turning.Add(r.transform); speeds.Add(r.name.StartsWith("Ring") ? 1.1f : -0.35f); }
        if (r.name.StartsWith("Rocks")) bobbing.Add(r.transform);
    }
    section.Near = near.ToArray(); section.Far = far.ToArray();
    return go;
}

// ---- the stack: one section per floor, then the cap ----
var stack = new UnityEngine.GameObject("Stack").transform; stack.SetParent(root.transform, false);
UnityEngine.GameObject firstFloor = null;
foreach (var s in built["sections"])
{
    var floor = (int)s["floor"]; var cap = (bool)s["cap"];
    var holder = new UnityEngine.GameObject(cap ? "Cap (cloud)" : "Floor " + floor + " (" + (string)s["style"] + ")").transform;
    holder.SetParent(stack, false);
    holder.localPosition = new UnityEngine.Vector3(0f, F(s["base"]), 0f);
    var section = new WorldTower.Section { Floor = cap ? 0 : floor };
    var go = Model((string)s["model"], holder, section);
    if (floor == 1) firstFloor = go;
    sections.Add(section);
}

// ---- the foot: the islet, the plaza, the egg; their light is always on ----
var foot = new WorldTower.Section { Floor = 0, AlwaysLit = true };
var footHolder = new UnityEngine.GameObject("Foot").transform; footHolder.SetParent(root.transform, false);
var isletGo = Model((string)built["islet"]["model"], footHolder, foot);
UnityEngine.Transform Part(string name) { foreach (var t in isletGo.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == name) return t; log.AppendLine("no part " + name); return null; }
UnityEngine.Mesh MeshOf(string name) { var t = Part(name); return t ? t.GetComponent<UnityEngine.MeshFilter>().sharedMesh : null; }
// what can be stood on: the low detail islet and the plaza
var isletCollider = Part("Islet_Far").gameObject.AddComponent<UnityEngine.MeshCollider>();
var plazaCollider = Part("Plaza_Near").gameObject.AddComponent<UnityEngine.MeshCollider>();
if (firstFloor) foreach (var t in firstFloor.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == "Body_Far") t.gameObject.AddComponent<UnityEngine.MeshCollider>();
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

// the ground under a point of the foot, in the root's own terms
UnityEngine.Vector3 Ground(float x, float z, float fallback = 4.85f)
{
    var from = root.transform.TransformPoint(new UnityEngine.Vector3(x, 120f, z));
    var ray = new UnityEngine.Ray(from, UnityEngine.Vector3.down);
    var best = float.MaxValue;
    foreach (var c in new[] { plazaCollider, isletCollider })
        if (c.Raycast(ray, out var hit, 200f) && hit.distance < best) best = hit.distance;
    return new UnityEngine.Vector3(x, best < float.MaxValue ? 120f - best : fallback, z);
}
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var extras = new UnityEngine.GameObject("Extras").transform; extras.SetParent(root.transform, false);
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
    l.type = UnityEngine.LightType.Point; l.color = new UnityEngine.Color(1f, 0.72f, 0.42f); l.range = 16f; l.intensity = 3f; l.shadows = UnityEngine.LightShadows.None;
}

var plaza = V("plaza"); var plazaR = F(built["marks"]["plazaRadius"]); var gate = V("gate"); var level = F(built["marks"]["plazaLevel"]);
const string FK = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
const string Jungle = "Assets/PolygonNatureBiomes/PNB_Tropical_Jungle/Prefabs/";

// the hero climber on the pedestal, looking out over the plaza to the sea
var ped = V("pedestal");
Put(FK + "SM_Prop_Statue_Hero_01.prefab", ped + new UnityEngine.Vector3(-0.3f, 0f, 0f), 180f + 28f, 2.1f);

// banners down the gate's pilasters
foreach (var side in new[] { -1f, 1f })
    Put("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Wall_Banner_01.prefab", new UnityEngine.Vector3(side * 9f, 23.4f, gate.z - 0.5f), 180f, 2.1f);

// braziers: at the stair's foot, at the head of the quay, and a bowl on every other post of the low wall
const string BigBrazier = "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_01.prefab";
const string Bowl = "Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Brazier_01.prefab";
foreach (var side in new[] { -1f, 1f })
{
    var p = Ground(side * 13.2f, V("stairFoot").z + 1.5f);
    Put(BigBrazier, p, 0f, 1.5f);
    Fire(p + UnityEngine.Vector3.up * 2.75f, 1.9f, true);
    var q = new UnityEngine.Vector3(side * 6.6f, level + 0.12f, plaza.z - plazaR - 2.0f);
    Put(BigBrazier, q, 0f, 1.2f);
    Fire(q + UnityEngine.Vector3.up * 2.2f, 1.5f, true);
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
            Fire(p + UnityEngine.Vector3.up * 0.55f, 1.25f, false);
        }
}

// the light from the gate, falling down the stair
{
    var l = new UnityEngine.GameObject("Gate light").AddComponent<UnityEngine.Light>();
    l.transform.SetParent(extras, false);
    l.transform.localPosition = new UnityEngine.Vector3(0f, 15f, gate.z - 4f);
    l.type = UnityEngine.LightType.Point; l.color = new UnityEngine.Color(1f, 0.8f, 0.5f); l.range = 48f; l.intensity = 4.5f; l.shadows = UnityEngine.LightShadows.None;
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
    if (new UnityEngine.Vector2(x - egg.x, z - egg.z).magnitude < 6f) return false;
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
// rocks where the bastion meets the island's own stone, half sunk
var rocks = new[] { "SM_Env_Rock_01", "SM_Env_Rock_02", "SM_Env_Rock_03", "SM_Env_Rock_04" };
for (var k = 0; k < 16; k++)
{
    var t = R(0f, UnityEngine.Mathf.PI * 2f);
    // clear of the quay (toward -z) and of the way to the gate (toward +z)
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(t * UnityEngine.Mathf.Rad2Deg, -90f)) < 26f || UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(t * UnityEngine.Mathf.Rad2Deg, 90f)) < 48f) continue;
    var rr = plazaR + 4.5f + R(0.5f, 4f);
    var p = Ground(plaza.x + rr * UnityEngine.Mathf.Cos(t), plaza.z + rr * UnityEngine.Mathf.Sin(t), 0f);
    Put(Jungle + rocks[rng.Next(rocks.Length)] + ".prefab", p - UnityEngine.Vector3.up * 0.4f, R(0f, 360f), R(1.6f, 3.2f));
}

// the pier: the Vikings' dock pieces out from the quay's stone landing, a few barrels and crates on the landing
var quay = V("quayHead");
for (var k = 0; k < 3; k++)
    Put("Assets/PolygonVikings/Prefabs/Props/SM_Prop_Dock_0" + (k == 2 ? 2 : 1) + ".prefab", new UnityEngine.Vector3(0f, quay.y - 0.05f, quay.z - 2.5f - 5f * k), 0f, 1f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(-3.9f, quay.y, quay.z + 3.6f), 20f, 1.25f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(-3.6f, quay.y, quay.z + 2.2f), 80f, 1.25f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Crate_01.prefab", new UnityEngine.Vector3(3.8f, quay.y, quay.z + 3.4f), 12f, 1.4f);
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Crate_01.prefab", new UnityEngine.Vector3(3.6f, quay.y, quay.z + 1.7f), 40f, 1.2f);

// ---- the leaderboard stone's words: where the stream is and the floor's name (the game has those at hand); the
// climbers' rows under them stay carved runes until the game is given a list ----
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
    var slab = V("slab"); var size = built["marks"]["slabSize"]; float sw = F(size[0]), sh = F(size[1]), st = F(size[2]);
    var facing = F(built["marks"]["slabFacing"]) * UnityEngine.Mathf.Deg2Rad;
    var fwd = new UnityEngine.Vector3(UnityEngine.Mathf.Cos(facing), 0f, UnityEngine.Mathf.Sin(facing));
    var turn = UnityEngine.Quaternion.LookRotation(-fwd, UnityEngine.Vector3.up);   // words are read looking along +z
    var gold = new UnityEngine.Color(1f, 0.86f, 0.45f);
    var holder = new UnityEngine.GameObject("Leaderboard words").transform; holder.SetParent(root.transform, false);
    tower.SlabPlace = Words("Place", holder, slab + fwd * (st / 2 + 0.33f) + UnityEngine.Vector3.up * (sh * 0.565f), turn, sw * 0.68f, sh * 0.27f, gold, "FLOOR 1\nPART 1", 80f);
    tower.SlabName = Words("Floor name", holder, slab + fwd * (st / 2 + 0.33f) + UnityEngine.Vector3.up * (sh * 0.36f), turn, sw * 0.68f, sh * 0.12f, new UnityEngine.Color(0.92f, 0.9f, 0.84f), "The Overgrown Halls", 40f);
}

// ---- the easter egg (docs/easter-eggs.md): the elevator that is out of order, and its sign ----
{
    var eggRoot = new UnityEngine.GameObject("Egg (elevator)");
    eggRoot.transform.SetParent(root.transform, false);
    var gag = eggRoot.AddComponent<WorldGag>();
    gag.Key = "tower-elevator-out-of-order";
    gag.Texts = new TMPro.TextMeshPro[0]; gag.TextKeys = new string[0];
    var door = Part("Egg_Near");
    door.SetParent(eggRoot.transform, true);
    var sign = V("eggSign");
    var post = Put("Assets/Polygonal Fantasy Pack/Environment Prefabs/Misc/Text Signboard 01.prefab", Ground(sign.x + 0.4f, sign.z - 0.6f) - UnityEngine.Vector3.up * 0.05f, 180f - 16f, 2.3f, eggRoot.transform);
    if (post)
    {
        UnityEngine.Transform face = null;
        foreach (var mf in post.GetComponentsInChildren<UnityEngine.MeshFilter>()) if (mf.name.EndsWith("Text")) face = mf.transform;
        if (face)
        {
            var mb = face.GetComponent<UnityEngine.MeshFilter>().sharedMesh.bounds;
            face.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = signFace;
            const string key = "UI_GAG_SIGN_ELEVATOR";
            var field = typeof(Localization).GetField(key);
            var words = Words("Words", face, new UnityEngine.Vector3(mb.center.x, mb.center.y, mb.max.z + 0.006f), UnityEngine.Quaternion.Euler(0f, 180f, 0f), mb.size.x * 0.86f, mb.size.y * 0.84f,
                new UnityEngine.Color(0.97f, 0.91f, 0.74f), field != null ? (string)field.GetValue(null) : "OUT OF ORDER. PLEASE TAKE THE STAIRS. ALL OF THEM", 1.2f);
            words.fontSizeMin = 0.05f; words.lineSpacing = -12f;
            gag.Texts = new[] { words }; gag.TextKeys = new[] { key };
        }
        else log.AppendLine("no text face on the sign board");
    }
    tower.Egg = gag;
}

// ---- gulls: three flocks at different heights, each on a pivot the game turns round the tower ----
var flocks = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var (radius, height, size) in new[] { (128f, 46f, 1.5f), (112f, 150f, 1.7f), (150f, 96f, 1.5f), (96f, 236f, 1.8f) })
{
    var pivot = new UnityEngine.GameObject("Gulls").transform; pivot.SetParent(root.transform, false);
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

// ---- a navmesh for the plaza and the quay, for the day players come here (docs/world-tower.md, "Visiting") ----
var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
// the entrance only: the plaza, the stair into the gate's tunnel, the quay, the grass beside them; not the sea floor
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Volume;
surface.center = new UnityEngine.Vector3(0f, 21f, (V("quayHead").z + V("tunnelEnd").z) / 2f);
surface.size = new UnityEngine.Vector3(104f, 40f, V("tunnelEnd").z - V("quayHead").z + 4f);
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = 25556799;
surface.BuildNavMesh();
var navPath = DataFolder + "/NavMesh.asset";
if (surface.navMeshData) { UnityEditor.AssetDatabase.DeleteAsset(navPath); UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath); log.AppendLine("navmesh " + surface.navMeshData.sourceBounds); }
// from the pier's landing to the mouth of the gate
{
    var a = root.transform.TransformPoint(V("quayHead") + new UnityEngine.Vector3(0f, 0.2f, 2.5f));
    var b = root.transform.TransformPoint(V("gate") + new UnityEngine.Vector3(0f, 0.2f, 3f));
    var okA = UnityEngine.AI.NavMesh.SamplePosition(a, out var ha, 3f, -1); var okB = UnityEngine.AI.NavMesh.SamplePosition(b, out var hb, 3f, -1);
    var path = new UnityEngine.AI.NavMeshPath();
    var walk = okA && okB && UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, -1, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    log.AppendLine("walk quay to gate: " + (walk ? "yes" : "NO") + " (quay on mesh " + okA + ", gate on mesh " + okB + ")");
}
// markers for that day
foreach (var (name, mark) in new[] { ("Arrival (quay)", "quayHead"), ("Gate", "gate"), ("Plaza", "plaza") })
{
    var m = new UnityEngine.GameObject(name).transform; m.SetParent(root.transform, false);
    m.localPosition = V(mark); m.localRotation = UnityEngine.Quaternion.identity;
}

UnityEditor.EditorUtility.SetDirty(tower);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.AssetDatabase.SaveAssets();
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
log.AppendLine("triangles: tower near " + tris0 + ", far " + tris1 + ", props " + propTris);
log.AppendLine("tower at " + at.ToString("0.0") + " height " + F(built["height"]) + " sections " + sections.Count);
log.AppendLine(saved ? "SAVED " + ScenePath : "SAVE FAILED");
}
catch (System.Exception e) { log.AppendLine("EXCEPTION " + e); }
System.IO.File.WriteAllText(logPath, log.ToString());
return "world tower built";
