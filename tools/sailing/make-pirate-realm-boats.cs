// Run with: unity command eval_file --file tools/sailing/make-pirate-realm-boats.cs (MainWorld open, edit mode); save
// MainWorld afterwards
//
// Boats assembled from Synty's Pirate Realm (docs/own-boats.md, "Customizing boats"), for its small, medium and
// large ships (the Sloop, the Brigantine and the Galleon):
// - Assets/World/Sailing/BoatParts.asset: each ship's hulls, fronts, middles, backs, figureheads and rams in every
//   variant (the medium and large ships one hull: their rigging is made for it), the sails, flags and plank woods, the
//   flag and a pole for the small boats;
// - Assets/World/Sailing/Boats/<Boat> Rig.prefab: what every one of that ship has whatever its look, taken from the
//   kit's preset as it sets it (everything on its hull but the parts a look picks), against the hull's origin;
// - Assets/World/Sailing/Boats/<Boat>.prefab: an empty Model (BoatAssembler builds the look into it at launch), the
//   Seat at the helm and the GuestSeats on the main deck, found on the default look's decks;
// and points the scene's OwnBoatPrefabs at the parts and the two new boats.
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
const string PR = "Assets/Synty/PolygonPirateRealm/";
var missing = new System.Collections.Generic.List<string>();
var report = new System.Text.StringBuilder();

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

// the parts a look picks: a ship's own front, middle, back, figurehead or ram, straight on the hull
var slotPart = new System.Text.RegularExpressions.Regex(@"^SM_Veh_Ship_[SML]_(Front|Middle|Back|Figurehead|Ram)_\d\d$");

// the rig: everything on the preset's hull but the parts a look picks, kept as the preset sets it
UnityEngine.GameObject Rig(string presetName, string rigName)
{
    var preset = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(Prefab(presetName));
    UnityEditor.PrefabUtility.UnpackPrefabInstance(preset, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
    var hull = preset.transform.GetChild(0);
    var rig = new UnityEngine.GameObject(rigName);
    var kept = 0;
    foreach (var t in new System.Collections.Generic.List<UnityEngine.Transform>(System.Linq.Enumerable.Cast<UnityEngine.Transform>(hull)))
    {
        if (slotPart.IsMatch(t.name)) continue;
        var at = t.localPosition; var rot = t.localRotation; var scale = t.localScale;
        t.SetParent(rig.transform, false);
        t.localPosition = at; t.localRotation = rot; t.localScale = scale;
        kept++;
    }
    foreach (var c in rig.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    var saved = UnityEditor.PrefabUtility.SaveAsPrefabAsset(rig, "Assets/World/Sailing/Boats/" + rigName + ".prefab");
    UnityEngine.Object.DestroyImmediate(rig);
    UnityEngine.Object.DestroyImmediate(preset);
    report.Append(rigName + ": " + kept + " pieces; ");
    return saved;
}

BoatParts.Ship Ship(string size, int hulls, int fronts, int middles, int backs, string preset, string rigName) => new BoatParts.Ship
{
    Hulls = Variants("SM_Veh_Ship_" + size + "_Hull", hulls),
    Fronts = Variants("SM_Veh_Ship_" + size + "_Front", fronts),
    Middles = Variants("SM_Veh_Ship_" + size + "_Middle", middles),
    Backs = Variants("SM_Veh_Ship_" + size + "_Back", backs),
    Figureheads = Variants("SM_Veh_Ship_" + size + "_Figurehead", 7),
    Rams = Variants("SM_Veh_Ship_" + size + "_Ram", 4),
    Rig = Rig(preset, rigName),
};

// ---- The parts ---------------------------------------------------------------------------------------------------
var partsPath = "Assets/World/Sailing/BoatParts.asset";
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<BoatParts>(partsPath);
if (!parts) { parts = UnityEngine.ScriptableObject.CreateInstance<BoatParts>(); UnityEditor.AssetDatabase.CreateAsset(parts, partsPath); }
parts.Small = Ship("S", 2, 4, 3, 4, "SM_Veh_Preset_S_01", "Sloop Rig");
parts.Medium = Ship("M", 1, 3, 4, 4, "SM_Veh_Preset_M_01", "Brigantine Rig");
parts.Large = Ship("L", 1, 3, 3, 3, "SM_Veh_Preset_L_01", "Galleon Rig");
var sailNames = new[] { "01", "01_Alt_01", "02", "02_Alt_01", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };
parts.Sails = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(sailNames, s => Mat("Sails/ShipSails_" + s)));
parts.Flags = new UnityEngine.Material[17];
for (var i = 0; i < 17; ++i) parts.Flags[i] = Mat("Flags/Flag_" + (i + 1).ToString("00"));
parts.Woods = new[] { Mat("ShipWood/Ship_Planks_01"), Mat("ShipWood/Ship_Planks_02"), Mat("ShipWood/Ship_Planks_02_Alt"), Mat("ShipWood/Ship_Planks_03"), Mat("ShipWood/Ship_Planks_04") };
parts.Flag = Prefab("SM_Prop_Flag_01");
// a straight banded pole, standing upright (Post_01 was a crooked branch lying on its side)
parts.FlagPole = Prefab("SM_Prop_Flag_Post_04");
UnityEditor.EditorUtility.SetDirty(parts);
UnityEditor.AssetDatabase.SaveAssets();

// ---- The boats ---------------------------------------------------------------------------------------------------
// a boat prefab: its Model empty, the Seat just in front of the helm, the guests spread over the main deck. The decks
// are found on the default look built for the purpose, rays down onto its hull and deck pieces (not the rigging)
UnityEngine.GameObject Boat(string name, BoatItems.Tier tier, int guests, float fromZ, float toZ)
{
    var boat = new UnityEngine.GameObject(name);
    // measured high above the world, where the rays find nothing but this ship
    boat.transform.position = new UnityEngine.Vector3(0f, 2000f, 0f);
    new UnityEngine.GameObject("Model").transform.SetParent(boat.transform, false);
    var probe = new UnityEngine.GameObject("Probe");
    probe.transform.SetParent(boat.transform, false);
    var ship = parts.For(tier);
    var hull = UnityEngine.Object.Instantiate(ship.Hulls[0], probe.transform, false);
    foreach (var p in new[] { ship.Fronts[0], ship.Middles[0], ship.Backs[tier == BoatItems.Tier.Sloop ? 2 : 0], ship.Rig }) UnityEngine.Object.Instantiate(p, hull.transform, false);
    var helm = System.Linq.Enumerable.FirstOrDefault(probe.GetComponentsInChildren<UnityEngine.Transform>(), t => t.name.StartsWith("SM_Veh_Helm"));
    foreach (var mf in probe.GetComponentsInChildren<UnityEngine.MeshFilter>())
    {
        var n = mf.name;
        if (n.Contains("Rigging") || n.Contains("Sail") || n.Contains("Mast") || n.Contains("Boom") || n.Contains("Ratlines") || n.Contains("Bowsprit") || n.Contains("Helm")) continue;
        mf.gameObject.AddComponent<UnityEngine.MeshCollider>().sharedMesh = mf.sharedMesh;
    }
    UnityEngine.Physics.SyncTransforms();
    float DeckAt(float x, float z) => UnityEngine.Physics.Raycast(boat.transform.TransformPoint(new UnityEngine.Vector3(x, 60f, z)), UnityEngine.Vector3.down, out var hit, 120f) ? boat.transform.InverseTransformPoint(hit.point).y : float.NaN;

    var seat = new UnityEngine.GameObject("Seat").transform;
    seat.SetParent(boat.transform, false);
    var helmAt = helm ? boat.transform.InverseTransformPoint(helm.position) : new UnityEngine.Vector3(0f, 3f, fromZ);
    // the helm stands on the quarterdeck: its own height is the deck there (the deck pieces' rays miss that platform)
    seat.localPosition = new UnityEngine.Vector3(0f, helmAt.y, helmAt.z + 0.7f);

    // the guests: two by two down the main deck, either side of the middle line
    var placed = 0;
    for (var row = 0; placed < guests && row < 20; ++row)
    {
        var z = UnityEngine.Mathf.Lerp(fromZ, toZ, (row + 0.5f) / UnityEngine.Mathf.Max(1, (guests + 1) / 2));
        foreach (var x in new[] { -1.4f, 1.4f })
        {
            if (placed >= guests) break;
            var y = DeckAt(x, z);
            if (float.IsNaN(y)) continue;
            var g = new UnityEngine.GameObject("GuestSeat" + (++placed)).transform;
            g.SetParent(boat.transform, false);
            g.localPosition = new UnityEngine.Vector3(x, y, z);
        }
    }
    UnityEngine.Object.DestroyImmediate(probe);
    boat.transform.position = UnityEngine.Vector3.zero;
    var saved = UnityEditor.PrefabUtility.SaveAsPrefabAsset(boat, "Assets/World/Sailing/Boats/" + name + ".prefab");
    report.Append(name + ": seat " + seat.localPosition.ToString("F1") + ", " + placed + " guests; ");
    UnityEngine.Object.DestroyImmediate(boat);
    return saved;
}
// guests' rows: the Sloop between its mast and the grate, the bigger ships along their waists
var sloop = Boat("Sloop", BoatItems.Tier.Sloop, 3, -0.5f, 3.6f);
var brigantine = Boat("Brigantine", BoatItems.Tier.Brigantine, 5, -4f, 8f);
var galleon = Boat("Galleon", BoatItems.Tier.Galleon, 9, -8f, 10f);

// ---- The scene's prefab registry -----------------------------------------------------------------------------------
var registry = UnityEngine.Object.FindAnyObjectByType<OwnBoatPrefabs>(UnityEngine.FindObjectsInactive.Include);
if (registry)
{
    var so = new UnityEditor.SerializedObject(registry);
    so.FindProperty("parts").objectReferenceValue = parts;
    so.FindProperty("sloop").objectReferenceValue = sloop;
    so.FindProperty("brigantine").objectReferenceValue = brigantine;
    so.FindProperty("galleon").objectReferenceValue = galleon;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(world);
}
return report + "registry " + (registry ? "set" : "MISSING") + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : "");
