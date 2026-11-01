// Run with: unity command eval_file --file tools/mangrove/mangrove-monkeys.cs (MainWorld open, edit mode), after
// tools/mangrove/mangrove-island.cs
//
// The banana island's monkeys (docs/banana-island.md), with stand-in models until a real monkey is found: the Low Poly
// Animated Animals gorilla, small and brown for the troop, a big silverback for the chief. Makes (and remakes):
// - Assets/Prefabs/Mangrove/Monkey.prefab: a troop monkey (Monkey, its NavMeshAgent, a collider to click);
// - Assets/Resources/Pets/MonkeyPet.prefab and Assets/Animations/Pets/MonkeyPet.controller: the treasure's Monkey Pet
//   (PetController: Walk and Run floats, Sleeping, which plays dead);
// - Assets/Resources/Character/Weapons/Bows/Bananarang.prefab: a banana laid along the Rune Bow's limbs, held like it;
// - Assets/Resources/Mangrove/Treasure Chest.prefab: the chest a treasure dig brings up, gold spilling by it;
// and in the scene "Mangrove/Monkeys": the troop (MonkeyTroop) with its homes round the island, and the chief on the
// path up from the jetty (an IslandNpc, key monkey_chief). Rerun to rebuild.
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var mangrove = UnityEngine.GameObject.Find("============ WORLD/Islands/Mangrove");
if (!mangrove) return "no Mangrove island";

const string Pack = "Assets/Low Poly Animated Animals/";
var gorilla = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(Pack + "- Prefabs/Animals/Gorilla.prefab");
// the pack's own materials are not for this pipeline: URP Lit ones with its textures (the chief statue's for the troop)
var brown = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Village/Decor/Materials/Decor_Gorilla.mat");
UnityEngine.Material Lit(string path, UnityEngine.Texture texture)
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m) { m = new UnityEngine.Material(brown); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    m.SetTexture("_BaseMap", texture);
    m.SetTexture("_MainTex", texture);
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
}
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>(Pack + "- Controllers/Gorilla.controller");
if (!gorilla || !brown || !controller) return "the gorilla's prefab, material or controller are missing";
void Folder(string path)
{
    var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
    if (!UnityEditor.AssetDatabase.IsValidFolder(parent)) { Folder(parent); }
    if (!UnityEditor.AssetDatabase.IsValidFolder(path)) UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
}

// a gorilla of this height, unpacked, without the pack's own wandering, sound and character controller
UnityEngine.GameObject Gorilla(string name, float height, UnityEngine.Material material)
{
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(gorilla);
    UnityEditor.PrefabUtility.UnpackPrefabInstance(go, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
    go.name = name;
    foreach (var kind in new[] { "WanderScript", "PlaySound" })
    {
        var c = go.GetComponent(kind);
        if (c) UnityEngine.Object.DestroyImmediate(c);
    }
    var audio = go.GetComponent<UnityEngine.AudioSource>(); if (audio) UnityEngine.Object.DestroyImmediate(audio);
    var cc = go.GetComponent<UnityEngine.CharacterController>(); if (cc) UnityEngine.Object.DestroyImmediate(cc);
    // the model's own animator (no controller) under the root's
    foreach (var a in go.GetComponentsInChildren<UnityEngine.Animator>(true)) if (a.gameObject != go) UnityEngine.Object.DestroyImmediate(a);
    var smr = go.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
    smr.sharedMaterial = material;
    var animator = go.GetComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = controller;
    animator.applyRootMotion = false;
    animator.cullingMode = UnityEngine.AnimatorCullingMode.CullUpdateTransforms;
    go.transform.position = UnityEngine.Vector3.zero;
    go.transform.localScale *= height / smr.bounds.size.y;
    var capsule = go.GetComponent<UnityEngine.CapsuleCollider>();
    if (capsule) capsule.isTrigger = false;
    return go;
}

// ---- The troop's monkey ----------------------------------------------------------------------------------------
Folder("Assets/Prefabs/Mangrove");
var silver = Lit("Assets/Prefabs/Mangrove/Monkey Chief.mat", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture>(Pack + "- Textures/Animals/Gorilla_Silver_COL_1k.png"));
var bananaMat = Lit("Assets/Prefabs/Mangrove/Banana.mat", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture>("Assets/PolyFew/demo/TestModels/Food Pack Mixed/Textures/Banana.png"));
var monkeyGo = Gorilla("Monkey", 0.85f, brown);
var agent = monkeyGo.GetComponent<UnityEngine.AI.NavMeshAgent>() ?? monkeyGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
var s = monkeyGo.transform.lossyScale.y;
agent.radius = 0.25f / s;
agent.height = 0.85f / s;
agent.baseOffset = 0f;
monkeyGo.AddComponent<Monkey>();
var monkeyPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(monkeyGo, "Assets/Prefabs/Mangrove/Monkey.prefab").GetComponent<Monkey>();
UnityEngine.Object.DestroyImmediate(monkeyGo);

// ---- The Monkey Pet ---------------------------------------------------------------------------------------------
var petControllerPath = "Assets/Animations/Pets/MonkeyPet.controller";
UnityEditor.AssetDatabase.DeleteAsset(petControllerPath);
var pc = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(petControllerPath);
pc.AddParameter("Walk", UnityEngine.AnimatorControllerParameterType.Float);
pc.AddParameter("Run", UnityEngine.AnimatorControllerParameterType.Float);
pc.AddParameter("Sleeping", UnityEngine.AnimatorControllerParameterType.Bool);
UnityEngine.AnimationClip Clip(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(Pack + "- Meshes/Animals/Gorilla/Gorilla_" + n + ".anim");
var sm = pc.layers[0].stateMachine;
var idle = sm.AddState("Idle"); idle.motion = Clip("Idle");
var walk = sm.AddState("Walk"); walk.motion = Clip("Run"); // a pet keeps up with its walking owner at a scamper
var sleep = sm.AddState("Sleep"); sleep.motion = Clip("Death"); // it plays dead
sm.defaultState = idle;
var t = idle.AddTransition(walk); t.hasExitTime = false; t.duration = 0.15f; t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Walk");
t = walk.AddTransition(idle); t.hasExitTime = false; t.duration = 0.2f; t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Walk");
t = idle.AddTransition(sleep); t.hasExitTime = false; t.duration = 0.3f; t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Sleeping");
t = sleep.AddTransition(idle); t.hasExitTime = false; t.duration = 0.3f; t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Sleeping");
var petGo = Gorilla("MonkeyPet", 0.6f, brown);
var petAgent = petGo.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (petAgent) UnityEngine.Object.DestroyImmediate(petAgent);
var petCapsule = petGo.GetComponent<UnityEngine.CapsuleCollider>(); if (petCapsule) UnityEngine.Object.DestroyImmediate(petCapsule);
petGo.GetComponent<UnityEngine.Animator>().runtimeAnimatorController = pc;
var pet = petGo.AddComponent<PetController>();
var petSo = new UnityEditor.SerializedObject(pet);
petSo.FindProperty("offsetPosition").vector3Value = new UnityEngine.Vector3(0.74f, 0f, -0.51f);
petSo.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAsset(petGo, "Assets/Resources/Pets/MonkeyPet.prefab");
UnityEngine.Object.DestroyImmediate(petGo);

// ---- The Bananarang ---------------------------------------------------------------------------------------------
// tip to tip and the bend of a mesh: the farthest two vertices, and from their middle towards the rest
(UnityEngine.Vector3 a, UnityEngine.Vector3 b, UnityEngine.Vector3 bend) Shape(UnityEngine.Mesh m)
{
    var v = m.vertices;
    var c = UnityEngine.Vector3.zero; foreach (var p in v) c += p; c /= v.Length;
    var a = v[0]; foreach (var p in v) if ((p - c).sqrMagnitude > (a - c).sqrMagnitude) a = p;
    var b = a; foreach (var p in v) if ((p - a).sqrMagnitude > (b - a).sqrMagnitude) b = p;
    var mid = (a + b) * 0.5f; var axis = (b - a).normalized;
    var off = c - mid; off -= axis * UnityEngine.Vector3.Dot(off, axis);
    return (a, b, off.normalized);
}
var runeBow = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Character/Weapons/Bows/Rune Bow.prefab");
UnityEngine.Mesh banana = null;
foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/PolyFew/demo/TestModels/Food Pack Mixed/Meshes/Banana.fbx"))
    if (o is UnityEngine.Mesh m && m.name.StartsWith("Banana_Mid")) banana = m;
var bowShape = Shape(runeBow.GetComponent<UnityEngine.MeshFilter>().sharedMesh);
var banShape = Shape(banana);
var bowAxis = (bowShape.b - bowShape.a).normalized;
var banAxis = (banShape.b - banShape.a).normalized;
var turn = UnityEngine.Quaternion.LookRotation(bowAxis, bowShape.bend) * UnityEngine.Quaternion.Inverse(UnityEngine.Quaternion.LookRotation(banAxis, banShape.bend));
var scale = (bowShape.b - bowShape.a).magnitude / (banShape.b - banShape.a).magnitude * 1.02f;
var rang = new UnityEngine.GameObject("Bananarang");
rang.transform.localPosition = runeBow.transform.localPosition;
rang.transform.localRotation = runeBow.transform.localRotation;
var peel = new UnityEngine.GameObject("Banana");
peel.transform.SetParent(rang.transform, false);
peel.transform.localRotation = turn;
peel.transform.localScale = UnityEngine.Vector3.one * scale;
peel.transform.localPosition = (bowShape.a + bowShape.b) * 0.5f - turn * ((banShape.a + banShape.b) * 0.5f) * scale;
peel.AddComponent<UnityEngine.MeshFilter>().sharedMesh = banana;
peel.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = bananaMat;
UnityEditor.PrefabUtility.SaveAsPrefabAsset(rang, "Assets/Resources/Character/Weapons/Bows/Bananarang.prefab");
UnityEngine.Object.DestroyImmediate(rang);

// ---- The treasure chest -----------------------------------------------------------------------------------------
Folder("Assets/Resources/Mangrove");
var chestRoot = new UnityEngine.GameObject("Treasure Chest");
var chest = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Chest_01.prefab"), chestRoot.transform);
chest.transform.localPosition = UnityEngine.Vector3.zero;
var pile = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonPirates/Prefabs/Props/SM_Prop_TreasurePile_01.prefab"), chestRoot.transform);
pile.transform.localPosition = new UnityEngine.Vector3(0.9f, 0f, 0.4f);
pile.transform.localScale = UnityEngine.Vector3.one * 0.5f;
foreach (var c in chestRoot.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(c);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(chestRoot, "Assets/Resources/Mangrove/Treasure Chest.prefab");
UnityEngine.Object.DestroyImmediate(chestRoot);

// ---- The regatta's buoy and the pirates' ship -------------------------------------------------------------------
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, UnityEngine.Vector3 at, UnityEngine.Vector3 euler, float scale)
{
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path), parent);
    o.transform.localPosition = at;
    o.transform.localEulerAngles = euler;
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var c in o.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(c);
    return o;
}
// a barrel riding low in the water with a pirate flag on it, at sea level
var buoy = new UnityEngine.GameObject("Regatta Buoy");
Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Barrel_01.prefab", buoy.transform, new UnityEngine.Vector3(0f, -0.55f, 0f), UnityEngine.Vector3.zero, 1.3f);
Put("Assets/PolygonPirates/Prefabs/Vehicles/SM_Flag_Pirate_01.prefab", buoy.transform, new UnityEngine.Vector3(0f, 1.3f, 0f), new UnityEngine.Vector3(0f, 90f, 0f), 0.7f);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(buoy, "Assets/Resources/Mangrove/Regatta Buoy.prefab");
UnityEngine.Object.DestroyImmediate(buoy);
// the pirates' ship: the kit's medium pirate ship with its rig, a little bigger, sitting at its draft
var ship = new UnityEngine.GameObject("Pirate Ship");
Put("Assets/PolygonPirates/Prefabs/Vehicles/SM_Veh_Boat_Medium_01_Hull_Pirate_Attachments.prefab", ship.transform, new UnityEngine.Vector3(0f, -1.1f * 1.4f, 0f), UnityEngine.Vector3.zero, 1.4f);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(ship, "Assets/Resources/Mangrove/Pirate Ship.prefab");
UnityEngine.Object.DestroyImmediate(ship);

// ---- In the scene: the troop and its chief ----------------------------------------------------------------------
var old = mangrove.transform.Find("Monkeys");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var troopGo = new UnityEngine.GameObject("Monkeys");
troopGo.transform.SetParent(mangrove.transform, false);
troopGo.transform.position = new UnityEngine.Vector3(330f, -3.2f, 832f);
var mask = 1 << 3;
// on dry ground: the sand round the stilt village, its boardwalks, the coconut beach, the farm patch and two sandbanks
// in the lagoon (away from the pirates' beach); the lagoon itself is too deep for a monkey
var spots = new[]
{
    new UnityEngine.Vector3(323.8f, -3, 844.7f), new UnityEngine.Vector3(336.9f, -3, 846.9f), new UnityEngine.Vector3(329.5f, -3, 839.5f),
    new UnityEngine.Vector3(335.5f, -3, 836.5f), new UnityEngine.Vector3(340f, -3, 829f), new UnityEngine.Vector3(350.4f, -3, 852.9f),
    new UnityEngine.Vector3(354f, -3, 848f), new UnityEngine.Vector3(286f, -3, 844f), new UnityEngine.Vector3(304f, -3, 814f),
    new UnityEngine.Vector3(322f, -3, 818f), new UnityEngine.Vector3(330f, -3, 861f), new UnityEngine.Vector3(342f, -3, 856f),
};
var homes = new System.Collections.Generic.List<UnityEngine.Transform>();
var missed = 0;
for (var i = 0; i < spots.Length; ++i)
{
    if (!UnityEngine.AI.NavMesh.SamplePosition(spots[i], out var hit, 8f, mask)) { missed++; continue; }
    var home = new UnityEngine.GameObject("Home " + (i + 1)).transform;
    home.SetParent(troopGo.transform, false);
    home.position = hit.position;
    homes.Add(home);
}
var troop = troopGo.AddComponent<MonkeyTroop>();
var so = new UnityEditor.SerializedObject(troop);
so.FindProperty("monkeyPrefab").objectReferenceValue = monkeyPrefab;
var homesProp = so.FindProperty("homes");
homesProp.arraySize = homes.Count;
for (var i = 0; i < homes.Count; ++i) homesProp.GetArrayElementAtIndex(i).objectReferenceValue = homes[i];
so.ApplyModifiedPropertiesWithoutUndo();

// the chief: on the sand by the stilt village, looking down the lagoon at whoever comes ashore
var chiefGo = Gorilla("Monkey Chief", 2.3f, silver);
var chiefAgent = chiefGo.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (chiefAgent) UnityEngine.Object.DestroyImmediate(chiefAgent);
chiefGo.transform.SetParent(troopGo.transform, true);
UnityEngine.AI.NavMesh.SamplePosition(new UnityEngine.Vector3(336.9f, -3f, 846.9f), out var chiefAt, 8f, mask);
chiefGo.transform.SetPositionAndRotation(chiefAt.position, UnityEngine.Quaternion.Euler(0f, 200f, 0f));
var npc = chiefGo.AddComponent<IslandNpc>();
npc.Key = MonkeyTroop.ChiefKey;
npc.HeadHeight = 2.9f;
foreach (var r in chiefGo.GetComponentsInChildren<UnityEngine.Renderer>()) r.gameObject.isStatic = false;

// the regatta's board, on the sand beside the chief: most wins, best times (RegattaRecords fills it). Fantasy
// Village's quest board without its notes; renamed, and without colliders, so a click is not taken for a quest board
var oldBoard = mangrove.transform.Find("Regatta Board");
if (oldBoard) UnityEngine.Object.DestroyImmediate(oldBoard.gameObject);
var board = new UnityEngine.GameObject("Regatta Board");
board.transform.SetParent(mangrove.transform, false);
UnityEngine.AI.NavMesh.SamplePosition(chiefAt.position + new UnityEngine.Vector3(2.8f, 0f, 1.2f), out var boardAt, 4f, mask);
board.transform.SetPositionAndRotation(boardAt.position - UnityEngine.Vector3.up * 0.05f, UnityEngine.Quaternion.Euler(0f, 200f, 0f));
var boardModel = Put("Assets/Synty/PolygonFantasyVillage/Prefabs/Props/SM_Prop_Quest_Board_02.prefab", board.transform, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, 1f);
boardModel.name = "Board";
boardModel.isStatic = true;
// the face: the model's bounds in the board's own space, the text on its front (local +Z, towards the lagoon)
var faceBounds = new UnityEngine.Bounds();
var firstRenderer = true;
foreach (var r in boardModel.GetComponentsInChildren<UnityEngine.MeshRenderer>())
{
    var mf = r.GetComponent<UnityEngine.MeshFilter>();
    if (!mf || !mf.sharedMesh) continue;
    foreach (var corner in new[] { mf.sharedMesh.bounds.min, mf.sharedMesh.bounds.max })
    {
        var local = board.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
        if (firstRenderer) { faceBounds = new UnityEngine.Bounds(local, UnityEngine.Vector3.zero); firstRenderer = false; }
        else faceBounds.Encapsulate(local);
    }
}
var textGo = new UnityEngine.GameObject("Text");
textGo.transform.SetParent(board.transform, false);
var tmp = textGo.AddComponent<TMPro.TextMeshPro>();
tmp.rectTransform.sizeDelta = new UnityEngine.Vector2(faceBounds.size.x * 0.72f, faceBounds.size.y * 0.34f);
textGo.transform.localPosition = new UnityEngine.Vector3(faceBounds.center.x, faceBounds.min.y + faceBounds.size.y * 0.5f, faceBounds.max.z + 0.04f);
textGo.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
tmp.enableAutoSizing = true;
tmp.fontSizeMin = 0.6f;
tmp.fontSizeMax = 2.4f;
tmp.alignment = TMPro.TextAlignmentOptions.Top;
tmp.color = new UnityEngine.Color(0.98f, 0.93f, 0.8f);
tmp.outlineWidth = 0.2f;
tmp.outlineColor = new UnityEngine.Color32(30, 18, 8, 255);
tmp.text = "<b>" + Localization.UI_REGATTA_BOARD + "</b>" + System.Environment.NewLine + Localization.UI_REGATTA_BOARD_EMPTY;

UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
return "troop of " + homes.Count + " homes (" + missed + " missed), chief at " + chiefAt.position.ToString("F1") + "; bananarang scale " + scale.ToString("F3");
