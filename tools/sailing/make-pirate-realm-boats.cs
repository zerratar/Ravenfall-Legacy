// Run with: unity command eval_file --file tools/sailing/make-pirate-realm-boats.cs (MainWorld open, edit mode); save
// MainWorld afterwards
//
// Boats assembled from Synty's Pirate Realm (docs/own-boats.md, "Customizing boats"):
// - Assets/World/Sailing/BoatParts.asset: the small ship's hulls, fronts, middles, backs, figureheads and rams in every
//   variant, its sails, flags and plank woods, the flag and a pole for the small boats;
// - Assets/World/Sailing/Boats/Sloop Rig.prefab: what every Sloop has whatever its look, taken from the kit's preset
//   as it sets it (the mast with its booms and sails, bowsprit, rigging, helm, grate), against the hull's origin;
// - Assets/World/Sailing/Boats/Sloop.prefab: the Sloop as Pirate Realm's small ship: an empty Model (BoatAssembler
//   builds the look into it at launch), the Seat at the helm and three GuestSeats on the main deck;
// and points the scene's OwnBoatPrefabs at the parts. The old Sloop (the Pirates kit's medium boat) is replaced.
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
const string PR = "Assets/Synty/PolygonPirateRealm/";
var missing = new System.Collections.Generic.List<string>();

UnityEngine.GameObject Prefab(string name)
{
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(name + " t:Prefab", new[] { PR + "Prefabs" }))
    {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    }
    missing.Add(name);
    return null;
}
UnityEngine.GameObject[] Variants(string stem, int count)
{
    var list = new UnityEngine.GameObject[count];
    for (var i = 0; i < count; ++i) list[i] = Prefab(stem + "_" + (i + 1).ToString("00"));
    return list;
}
UnityEngine.Material Mat(string path)
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(PR + "Materials/" + path + ".mat");
    if (!m) missing.Add(path);
    return m;
}

// ---- The rig, from the preset as it sets it ---------------------------------------------------------------------
var preset = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(Prefab("SM_Veh_Preset_S_01"));
UnityEditor.PrefabUtility.UnpackPrefabInstance(preset, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);
var hull = preset.transform.Find("SM_Veh_Ship_S_Hull_01");
var rig = new UnityEngine.GameObject("Sloop Rig");
var kept = new System.Collections.Generic.List<string>();
foreach (var t in hull.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    var n = t.name;
    var fixedPart = n.StartsWith("SM_Veh_Ship_S_Mast_Middle") && t.parent.name.Contains("Locator")
        || n.StartsWith("SM_Veh_Ship_S_Bowsprit") && t.parent.name.Contains("Locator")
        || t.parent == hull && (n.StartsWith("SM_Veh_Ship_S_Rigging") || n.StartsWith("SM_Veh_Helm") || n.StartsWith("SM_Veh_Ship_Grate"));
    if (!fixedPart) continue;
    var at = hull.InverseTransformPoint(t.position);
    var rot = UnityEngine.Quaternion.Inverse(hull.rotation) * t.rotation;
    t.SetParent(rig.transform, false);
    t.localPosition = at;
    t.localRotation = rot;
    kept.Add(n + (t.gameObject.activeSelf ? "" : " (off)"));
}
foreach (var c in rig.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
var rigPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(rig, "Assets/World/Sailing/Boats/Sloop Rig.prefab");
UnityEngine.Object.DestroyImmediate(rig);
UnityEngine.Object.DestroyImmediate(preset);

// ---- The parts ---------------------------------------------------------------------------------------------------
var partsPath = "Assets/World/Sailing/BoatParts.asset";
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<BoatParts>(partsPath);
if (!parts) { parts = UnityEngine.ScriptableObject.CreateInstance<BoatParts>(); UnityEditor.AssetDatabase.CreateAsset(parts, partsPath); }
parts.Hulls = Variants("SM_Veh_Ship_S_Hull", 2);
parts.Fronts = Variants("SM_Veh_Ship_S_Front", 4);
parts.Middles = Variants("SM_Veh_Ship_S_Middle", 3);
parts.Backs = Variants("SM_Veh_Ship_S_Back", 4);
parts.Figureheads = Variants("SM_Veh_Ship_S_Figurehead", 7);
parts.Rams = Variants("SM_Veh_Ship_S_Ram", 4);
parts.Rig = rigPrefab;
var sailNames = new[] { "01", "01_Alt_01", "02", "02_Alt_01", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };
parts.Sails = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(sailNames, s => Mat("Sails/ShipSails_" + s)));
parts.Flags = new UnityEngine.Material[17];
for (var i = 0; i < 17; ++i) parts.Flags[i] = Mat("Flags/Flag_" + (i + 1).ToString("00"));
parts.Woods = new[] { Mat("ShipWood/Ship_Planks_01"), Mat("ShipWood/Ship_Planks_02"), Mat("ShipWood/Ship_Planks_02_Alt"), Mat("ShipWood/Ship_Planks_03"), Mat("ShipWood/Ship_Planks_04") };
parts.Flag = Prefab("SM_Prop_Flag_01");
parts.FlagPole = Prefab("SM_Prop_Flag_Post_01");
UnityEditor.EditorUtility.SetDirty(parts);
UnityEditor.AssetDatabase.SaveAssets();

// ---- The Sloop ---------------------------------------------------------------------------------------------------
var sloop = new UnityEngine.GameObject("Sloop");
new UnityEngine.GameObject("Model").transform.SetParent(sloop.transform, false);
UnityEngine.Transform Place(string name, UnityEngine.Vector3 at)
{
    var t = new UnityEngine.GameObject(name).transform;
    t.SetParent(sloop.transform, false);
    t.localPosition = at;
    return t;
}
// the owner at the helm on the quarterdeck, the guests on the main deck by the mast and on the grate
Place("Seat", new UnityEngine.Vector3(0f, 3.3f, -4.1f));
Place("GuestSeat1", new UnityEngine.Vector3(-1.3f, 1.72f, 0.2f));
Place("GuestSeat2", new UnityEngine.Vector3(1.3f, 1.72f, 0.2f));
Place("GuestSeat3", new UnityEngine.Vector3(0f, 1.8f, 3.6f));
UnityEditor.PrefabUtility.SaveAsPrefabAsset(sloop, "Assets/World/Sailing/Boats/Sloop.prefab");
UnityEngine.Object.DestroyImmediate(sloop);

// ---- The scene's prefab registry gets the parts ------------------------------------------------------------------
var registry = UnityEngine.Object.FindAnyObjectByType<OwnBoatPrefabs>(UnityEngine.FindObjectsInactive.Include);
if (registry)
{
    var so = new UnityEditor.SerializedObject(registry);
    so.FindProperty("parts").objectReferenceValue = parts;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
}
return "rig: " + string.Join(", ", kept) + "; registry " + (registry ? "set" : "MISSING") + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : "");
