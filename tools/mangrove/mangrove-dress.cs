// Run with: unity command eval_file --file tools/mangrove/mangrove-dress.cs (MainWorld open, edit mode), after
// tools/mangrove/mangrove-monkeys.cs; save MainWorld afterwards
//
// Dresses the banana island with Synty's Pirate Realm (docs/banana-island.md), all under "Mangrove/Dressing":
// - the monkeys' banana pile, a heap of bunches by the chief that follows the real pile (BananaPileView);
// - the pirates' camp on their cove: a campfire, flags, a cannon looking out to sea, barrels, crates, treasure,
//   skulls and lanterns;
// - palms on the sand mounds and the cove;
// - parrots on hut roofs, the cannon, the flag post and the barrels, turning their heads (ParrotIdle);
// - signs: "NO PIRATES" by the village, "NO MONKEYS" on the pirates' cove;
// - birds over the island, fish in the lagoon, fireflies at night (IslandAmbience: off in potato mode);
// and remakes Resources/Mangrove/Pirate Ship (the pirates' raid) as Pirate Realm's medium ship.
// Props go on the island's navmesh height (or on what is below them, for roofs); none block anyone (the island's own
// navmesh is baked without them). Rerun to rebuild.
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var mangrove = UnityEngine.GameObject.Find("============ WORLD/Islands/Mangrove");
if (!mangrove) return "no Mangrove island";
var chief = mangrove.transform.Find("Monkeys/Monkey Chief");
if (!chief) return "no chief (run mangrove-monkeys.cs first)";

const string PR = "Assets/Synty/PolygonPirateRealm/Prefabs/";
var old = mangrove.transform.Find("Dressing");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new UnityEngine.GameObject("Dressing").transform;
root.SetParent(mangrove.transform, false);
var mask = 1 << 3;
var missing = new System.Collections.Generic.List<string>();

UnityEngine.Transform Group(string name)
{
    var g = new UnityEngine.GameObject(name).transform;
    g.SetParent(root, false);
    return g;
}
// on the island's walkable ground near (x, z), or null where there is none close
UnityEngine.Vector3? Ground(float x, float z, float within = 3f)
{
    if (UnityEngine.AI.NavMesh.SamplePosition(new UnityEngine.Vector3(x, -3f, z), out var hit, within, mask)) return hit.position;
    return null;
}
// on whatever is highest below (x, z): a roof, a rock
UnityEngine.Vector3? Top(float x, float z)
{
    if (UnityEngine.Physics.Raycast(new UnityEngine.Vector3(x, 60f, z), UnityEngine.Vector3.down, out var hit, 100f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) return hit.point;
    return null;
}
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, UnityEngine.Vector3 at, float yaw, float scale = 1f, bool still = true)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(PR + path + ".prefab");
    if (!prefab) { missing.Add(path); return null; }
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
    o.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var c in o.GetComponentsInChildren<UnityEngine.Collider>()) UnityEngine.Object.DestroyImmediate(c);
    if (still) foreach (var t in o.GetComponentsInChildren<UnityEngine.Transform>()) t.gameObject.isStatic = true;
    return o;
}
UnityEngine.GameObject PutOn(string path, UnityEngine.Transform parent, float x, float z, float yaw, float scale = 1f)
{
    var at = Ground(x, z);
    if (at == null) { missing.Add(path + " (no ground at " + x + ", " + z + ")"); return null; }
    return Put(path, parent, at.Value - UnityEngine.Vector3.up * 0.02f, yaw, scale);
}
// the top of a placed thing, for a parrot to sit on
UnityEngine.Vector3 TopOf(UnityEngine.GameObject o)
{
    var rs = o.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    return new UnityEngine.Vector3(b.center.x, b.max.y, b.center.z);
}
var parrots = new System.Collections.Generic.List<UnityEngine.Vector3>();

// ---- The banana pile: bunches heaped from the bottom up beside the chief ----------------------------------------
var pileGroup = Group("Banana Pile");
var chiefPos = chief.position;
// on the sand in front of the chief, short of the regatta board
var pileCentre = Ground(chiefPos.x + 1.0f, chiefPos.z - 1.2f, 1.5f) ?? chiefPos;
pileGroup.position = pileCentre;
var rng = new System.Random(8);
const int Bunches = 46;
const float BunchScale = 2.6f;
for (var i = 0; i < Bunches; ++i)
{
    // a cone: wide rings low down, narrower and higher as it fills
    var layer = (int)System.Math.Floor(System.Math.Sqrt(i * 1.1));
    var radius = UnityEngine.Mathf.Max(0.04f, 0.55f - layer * 0.08f) * (float)(0.3 + rng.NextDouble() * 0.7);
    var angle = (float)(rng.NextDouble() * 360.0);
    var local = UnityEngine.Quaternion.Euler(0f, angle, 0f) * UnityEngine.Vector3.forward * radius + UnityEngine.Vector3.up * (layer * 0.13f);
    var bunch = Put("Props/SM_Prop_Food_Banana_01", pileGroup, pileCentre + local, (float)(rng.NextDouble() * 360.0), BunchScale);
    if (!bunch) break;
    bunch.transform.rotation = UnityEngine.Quaternion.Euler((float)(rng.NextDouble() * 50 - 25), (float)(rng.NextDouble() * 360.0), (float)(rng.NextDouble() * 50 - 25));
    foreach (var t in bunch.GetComponentsInChildren<UnityEngine.Transform>()) t.gameObject.isStatic = false;
    bunch.name = "Bunch " + (i + 1);
}
pileGroup.gameObject.AddComponent<BananaPileView>();

// ---- The pirates' camp on their cove ---------------------------------------------------------------------------
var camp = Group("Pirate Camp");
PutOn("Props/SM_Prop_Campfire_01", camp, 340.9f, 806.9f, 20f, 1.3f);
var flagPost = PutOn("Props/SM_Prop_Flag_Post_01", camp, 351.0f, 813.2f, 0f);
if (flagPost) { flagPost.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 90f); }
// the old pirate kit's skull and bones at the top of the post (Pirate Realm's own flag renders blank here)
if (flagPost)
{
    var skullFlag = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonPirates/Prefabs/Vehicles/SM_Flag_Pirate_01.prefab");
    var flag = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(skullFlag, camp);
    flag.transform.SetPositionAndRotation(TopOf(flagPost) - UnityEngine.Vector3.up * 0.8f, UnityEngine.Quaternion.Euler(0f, 290f, 0f));
}
var cannon = PutOn("Weapons/SM_Wep_Cannon_01", camp, 351.4f, 808.4f, 100f);
PutOn("Props/SM_Prop_Cannonball_Holder_01_Insert_01", camp, 350.2f, 806.6f, 30f);
var barrel = PutOn("Props/SM_Prop_Barrel_01", camp, 338.9f, 811.4f, 10f);
PutOn("Props/SM_Prop_Barrel_03", camp, 339.8f, 812.3f, 70f);
PutOn("Props/SM_Prop_Crate_02", camp, 338.4f, 810.0f, 35f);
PutOn("Props/SM_Prop_Crate_04", camp, 339.2f, 809.2f, 80f);
PutOn("Props/SM_Prop_Treasure_Chest_02", camp, 347.6f, 804.9f, 160f);
PutOn("Props/SM_Prop_Treasure_Pile_02", camp, 348.9f, 805.2f, 40f);
PutOn("Props/SM_Prop_Treasure_Pile_03", camp, 346.6f, 804.3f, 110f);
PutOn("Props/SM_Prop_Skull_01", camp, 349.6f, 804.4f, 220f);
PutOn("Props/SM_Prop_Skull_01", camp, 341.6f, 806.0f, 60f, 0.9f);
PutOn("Props/SM_Prop_Lantern_01", camp, 340.2f, 808.2f, 0f);
PutOn("Props/SM_Prop_Map_01", camp, 342.0f, 807.7f, 75f);
PutOn("Props/SM_Prop_Food_Fish_01", camp, 341.4f, 807.5f, 140f);
if (cannon) parrots.Add(TopOf(cannon));
if (barrel) parrots.Add(TopOf(barrel));
if (flagPost) parrots.Add(TopOf(flagPost));

// ---- Palms: on the sand mounds by the village and on the cove ---------------------------------------------------
var palms = Group("Palms");
var palmSpots = new[] { (333.0f, 863.6f, 0.62f), (325.1f, 859.4f, 0.55f), (323.9f, 847.1f, 0.5f), (338.9f, 813.8f, 0.6f), (351.0f, 804.9f, 0.55f), (327.6f, 841.0f, 0.6f) };
var p = 0;
foreach (var (x, z, s) in palmSpots)
    PutOn("Environment/SM_Env_Tree_Palm_0" + (1 + p++ % 3), palms, x, z, (float)(rng.NextDouble() * 360.0), s);

// ---- Signs: each side keeps the other out ----------------------------------------------------------------------
var signs = Group("Signs");
void Sign(float x, float z, float yaw, string text)
{
    var post = PutOn("Props/SM_Prop_Sign_Post_01", signs, x, z, yaw, 1.4f);
    if (!post) return;
    var rs = post.GetComponentsInChildren<UnityEngine.MeshRenderer>();
    var mf = rs[0].GetComponent<UnityEngine.MeshFilter>();
    var mb = mf.sharedMesh.bounds;
    var label = new UnityEngine.GameObject("Text");
    label.transform.SetParent(rs[0].transform, false);
    var tmp = label.AddComponent<TMPro.TextMeshPro>();
    tmp.rectTransform.sizeDelta = new UnityEngine.Vector2(mb.size.x * 0.85f, mb.size.y * 0.3f);
    label.transform.localPosition = new UnityEngine.Vector3(mb.center.x, mb.min.y + mb.size.y * 0.78f, mb.max.z + 0.01f);
    label.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
    tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.3f; tmp.fontSizeMax = 1.6f;
    tmp.alignment = TMPro.TextAlignmentOptions.Center;
    tmp.color = new UnityEngine.Color(0.12f, 0.07f, 0.03f);
    tmp.fontStyle = TMPro.FontStyles.Bold;
    tmp.text = text;
    parrots.Add(TopOf(post));
}
Sign(332.0f, 838.0f, 200f, "NO PIRATES");
Sign(342.6f, 813.0f, 20f, "NO MONKEYS");

// ---- Parrots: on hut roofs, and on the cannon, the flag post, a barrel and the signs ---------------------------
var birds = Group("Parrots");
foreach (var (x, z) in new[] { (327.0f, 867.9f), (340.4f, 854.5f), (334.3f, 844.7f), (344.7f, 827.0f) })
{
    var top = Top(x, z);
    if (top != null && top.Value.y > -2f) parrots.Add(top.Value);
}
var pi = 0;
foreach (var at in parrots)
{
    var parrot = Put("Props/SM_Prop_Pet_Parrot_0" + (1 + pi++ % 2), birds, at, (float)(rng.NextDouble() * 360.0), 1.6f, false);
    if (parrot) parrot.AddComponent<ParrotIdle>();
}

// ---- Life in the air and water -----------------------------------------------------------------------------------
var life = Group("Ambience");
var ambience = life.gameObject.AddComponent<IslandAmbience>();
UnityEngine.ParticleSystem Fx(string path, UnityEngine.Vector3 at)
{
    var o = Put(path, life, at, 0f, 1f, false);
    if (!o) return null;
    var ps = o.GetComponentInChildren<UnityEngine.ParticleSystem>();
    o.SetActive(false);
    return ps;
}
var sea = SeaChart.Sea;
var alwaysFx = new System.Collections.Generic.List<UnityEngine.ParticleSystem>
{
    Fx("FX/FX_Birds_01", new UnityEngine.Vector3(330f, 28f, 848f)),
    Fx("FX/FX_Fish_01", new UnityEngine.Vector3(312f, sea - 0.3f, 828f)),
    Fx("FX/FX_Fish_01", new UnityEngine.Vector3(322f, sea - 0.3f, 814f)),
};
var nightFx = new System.Collections.Generic.List<UnityEngine.ParticleSystem>
{
    Fx("FX/FX_Fireflies_01", chiefPos + UnityEngine.Vector3.up * 1.2f),
    Fx("FX/FX_Fireflies_01", new UnityEngine.Vector3(330f, -1.5f, 860f)),
    Fx("FX/FX_Fireflies_01", new UnityEngine.Vector3(342f, -1f, 808f)),
};
var amb = new UnityEditor.SerializedObject(ambience);
void Fill(string prop, System.Collections.Generic.List<UnityEngine.ParticleSystem> list)
{
    list.RemoveAll(x => !x);
    var arr = amb.FindProperty(prop);
    arr.arraySize = list.Count;
    for (var i = 0; i < list.Count; ++i) arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
}
Fill("always", alwaysFx);
Fill("nightOnly", nightFx);
amb.ApplyModifiedPropertiesWithoutUndo();

// ---- The pirates' ship for their raid: Pirate Realm's medium ship ------------------------------------------------
var ship = new UnityEngine.GameObject("Pirate Ship");
var hull = Put("Vehicles/SM_Veh_Preset_M_01", ship.transform, UnityEngine.Vector3.zero, 0f, 1f, false);
string shipNote = "";
if (hull)
{
    // its keel below the water: the preset's lowest point a third of its depth down
    var rs = hull.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    hull.transform.localPosition = new UnityEngine.Vector3(0f, -b.min.y - 2.4f, 0f);
    shipNote = ", ship " + b.size.ToString("F0") + " min y " + b.min.y.ToString("F1");
    // the skull and bones at the masthead, so nobody mistakes whose ship it is
    var mastFlag = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonPirates/Prefabs/Vehicles/SM_Flag_Pirate_01.prefab"), ship.transform);
    // on the tallest mast's top: the flag's pivot is its pole edge, so it flies out from the mast
    UnityEngine.Renderer mast = null;
    foreach (var r in rs) if (r.name.Contains("Mast") && (mast == null || r.bounds.max.y > mast.bounds.max.y)) mast = r;
    var mb = mast ? mast.bounds : b;
    var mastTop = ship.transform.InverseTransformPoint(new UnityEngine.Vector3(mb.center.x, mb.max.y, mb.center.z));
    mastFlag.transform.localPosition = mastTop + UnityEngine.Vector3.down * 1.4f;
    mastFlag.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 90f, 0f);
    mastFlag.transform.localScale = UnityEngine.Vector3.one * 1.6f;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(ship, "Assets/Resources/Mangrove/Pirate Ship.prefab");
}
UnityEngine.Object.DestroyImmediate(ship);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
return "dressed: " + root.childCount + " groups, " + parrots.Count + " parrots" + shipNote + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : "");
