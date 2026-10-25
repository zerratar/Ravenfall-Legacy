// Run with: unity command eval_file --file tools/village-rebuild/decor-defaults.cs
//
// Makes the village island's own trees, bushes, flowers and grass into decor (docs/village-decor-design.md):
// - every piece under Town Landscape/Tree Belt and Town Landscape/Flowers And Stones gets a VillageDecorItem with a
//   stable key (its piece and where it stands, "tree_02@-31.2,-150.4") and its piece of the price list;
// - the props of phase 2 (fences, lights, market, farm, village, banners, statues) are made into decor prefabs in
//   Assets/Village/Decor;
// - the village manager gets the VillageDecor layer, with the prefab for every piece of the price list;
// - the server's list of the island's pieces is written to RavenNest
//   (BusinessLogic/Game/Managers/VillageDecorDefaults.cs), so only those can be moved or removed for a refund.
// Keys already given are kept. Run it again after changing the island's pieces, then build and deploy the server.
var sb = new System.Text.StringBuilder();
var ci = System.Globalization.CultureInfo.InvariantCulture;

// the price list's pieces and their prefabs (the same keys as VillageDecorManager.Pieces on the server)
var prefabPaths = new System.Collections.Generic.Dictionary<string, string>();
foreach (var k in new[] { "Tree_02", "Tree_03", "Tree_04", "Tree_Large_01", "Tree_Large_02", "Tree_Large_03", "Tree_Thin_01", "Tree_Thin_02", "Tree_Bush_01", "Tree_Bush_02",
    "Bush_Cluster_01", "Bush_Cluster_02", "Bush_Cluster_03", "Bush_Cluster_04", "Bush_Cluster_05", "Grass_01", "Grass_02" })
    prefabPaths[k.ToLowerInvariant()] = "Assets/PolygonAncientEmpire/Prefabs/Environment/SM_Env_" + k + ".prefab";
foreach (var k in new[] { "Flowers_01", "Flowers_02", "Flowers_04", "Flowers_07", "Grass_03" })
    prefabPaths[k.ToLowerInvariant()] = "Assets/PolygonElvenRealm/Prefabs/Environment/Flowers/SM_Env_" + k + ".prefab";
// garden sized rocks from the meadow pack, the island's own look (the Elven Realm "rocks" are 6 to 10 m cliff pieces)
prefabPaths["rock_01"] = "Assets/PolygonSamurai/Prefabs/Environments/SM_Env_Rock_01.prefab"; // the meadow boulder uses a triplanar shader that renders blank in the tray
prefabPaths["rock_02"] = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_02.prefab";
prefabPaths["rock_03"] = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Small_Pile_01.prefab";

// phase 2: props, each made into its own decor prefab in Assets/Village/Decor. The model sits under a root at the
// middle of its foot (some Synty props have their pivot at a hinge, a corner or half way up), the root carries a
// VillageDecorItem measured as a rectangle, and the lit ones get the island torches' fire and light, lit by night.
var kingdom = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
var props = new (string key, string path, string[] extra, float[] fires, float fireScale, bool lift)[]
{
    ("fence_01", kingdom + "SM_Prop_Fence_01.prefab", null, null, 0, false),
    ("fence_02", kingdom + "SM_Prop_Fence_02.prefab", null, null, 0, false),
    ("fence_03", kingdom + "SM_Prop_Fence_03.prefab", null, null, 0, false),
    ("fence_04", kingdom + "SM_Prop_Fence_04.prefab", null, null, 0, false),
    ("fence_gate_01", kingdom + "SM_Prop_Fence_Gate_01.prefab", null, null, 0, false),
    ("archway_01", kingdom + "SM_Prop_Archway_01.prefab", null, null, 0, false),
    ("archway_02", kingdom + "SM_Prop_Archway_02.prefab", null, null, 0, false),
    // fires as (x, y, z) triples, from the model's own pivot
    ("torch_01", "Assets/PolygonNature/Prefabs/Props/SM_Prop_TorchStick_01.prefab", null, new[] { 0f, 1.72f, 0f }, 1f, false),
    ("brazier_01", "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_03.prefab", new[] { "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_03_Insert_01.prefab" }, new[] { 0f, 0.89f, 0f }, 0.49f, false),
    ("brazier_02", "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_01.prefab", new[] { "Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Brazier_01_Insert_01.prefab" }, new[] { 0f, 1.75f, 0f }, 1f, false),
    // a small flame in each lantern, mostly for its light
    ("street_lamp_01", kingdom + "SM_Prop_Street_Lamp_01.prefab", null, new[] { 0f, 2.05f, -0.53f }, 0.12f, false),
    ("street_lamp_02", kingdom + "SM_Prop_Street_Lamp_02.prefab", null, new[] { 0f, 2.05f, -0.53f, 0f, 2.05f, 0.53f }, 0.12f, false),
    ("market_stall_01", kingdom + "SM_Prop_Market_Stall_01.prefab", null, null, 0, false),
    ("market_stall_07", kingdom + "SM_Prop_Market_Stall_07.prefab", null, null, 0, false),
    ("market_stall_03", kingdom + "SM_Prop_Market_Stall_03.prefab", null, null, 0, false),
    ("market_stall_05", kingdom + "SM_Prop_Market_Stall_05.prefab", null, null, 0, false),
    ("barrel_01", kingdom + "SM_Prop_Barrel_01.prefab", null, null, 0, false),
    ("barrel_stack_01", kingdom + "SM_Prop_Barrel_Stack_01.prefab", null, null, 0, false),
    ("crate_wood_01", kingdom + "SM_Prop_Crate_Wood_01.prefab", null, null, 0, false),
    ("crate_stack_01", kingdom + "SM_Prop_Crate_Stack_01.prefab", null, null, 0, false),
    ("sack_stack_01", kingdom + "SM_Prop_Sack_Stack_01.prefab", null, null, 0, false),
    ("table_barrel_01", kingdom + "SM_Prop_Table_Barrel_01.prefab", null, null, 0, false),
    // these four have their pivot half way up: lifted to stand on the ground
    ("hay_bale_01", kingdom + "SM_Prop_Hay_Bale_01.prefab", null, null, 0, true),
    ("hay_pile_01", kingdom + "SM_Prop_Hay_Pile_01.prefab", null, null, 0, false),
    ("hay_cart_01", kingdom + "SM_Prop_Hay_Cart_01.prefab", null, null, 0, false),
    ("log_01", kingdom + "SM_Prop_Log_01.prefab", null, null, 0, true),
    ("ground_logs_01", kingdom + "SM_Prop_Ground_Logs_01.prefab", null, null, 0, true),
    ("planterbox_preset_01", kingdom + "SM_Prop_PlanterBox_Preset_01.prefab", null, null, 0, false),
    ("planterbox_small_preset_01", kingdom + "SM_Prop_PlanterBox_Small_Preset_01.prefab", null, null, 0, false),
    ("well_01", kingdom + "SM_Prop_Well_01.prefab", null, null, 0, false),
    ("direction_sign_01", kingdom + "SM_Prop_Direction_Sign_01.prefab", null, null, 0, false),
    ("grinding_wheel_01", kingdom + "SM_Prop_Grinding_Wheel_01.prefab", null, null, 0, false),
    ("anvil_01", kingdom + "SM_Prop_Anvil_01.prefab", null, null, 0, false),
    ("bench_seat_01", kingdom + "Furniture/SM_Prop_Bench_Seat_01.prefab", null, null, 0, false),
    ("bench_seat_02", kingdom + "Furniture/SM_Prop_Bench_Seat_02.prefab", null, null, 0, false),
    // the pack's poles come bare; a flag flies from the top ("@x,y,z,scale" places an extra piece)
    ("flag_pole_01", kingdom + "Banners/SM_Prop_Flag_Pole_02.prefab", new[] { kingdom + "Banners/SM_Prop_Flag_Horizontal_01.prefab@0,3.3,0,0.7" }, null, 0, false),
    ("battle_banner_01", kingdom + "Banners/SM_Prop_Battle_Banner_01.prefab", null, null, 0, false),
    ("statue_knight_01", kingdom + "SM_Prop_Statue_Knight_01.prefab", null, null, 0, false),
    ("statue_hero_01", kingdom + "SM_Prop_Statue_Hero_01.prefab", null, null, 0, false),
    ("statue_angel_01", kingdom + "SM_Prop_Statue_Angel_01.prefab", null, null, 0, false),
};
var decorFolder = "Assets/Village/Decor";
if (!UnityEditor.AssetDatabase.IsValidFolder(decorFolder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Village", "Decor");
var firePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyRivals/Prefabs/FX/Fire_01_FX.prefab");
foreach (var prop in props)
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prop.path);
    if (!source) { sb.AppendLine("missing prop " + prop.path); continue; }
    var lit = prop.fires != null;
    var root = new UnityEngine.GameObject(lit ? (prop.key.StartsWith("brazier") ? "Brazier" : "Torch") : prop.key);
    try
    {
        var model = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false);
        if (prop.extra != null)
            foreach (var extraEntry in prop.extra)
            {
                var parts = extraEntry.Split('@');
                var extra = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(parts[0]));
                extra.transform.SetParent(model.transform, false);
                if (parts.Length > 1)
                {
                    var v = System.Array.ConvertAll(parts[1].Split(','), x => float.Parse(x, ci));
                    extra.transform.localPosition = new UnityEngine.Vector3(v[0], v[1], v[2]);
                    extra.transform.localScale = UnityEngine.Vector3.one * v[3];
                }
            }

        // the foot's middle at the root
        var bounds = new UnityEngine.Bounds(); var first = true;
        foreach (var r in model.GetComponentsInChildren<UnityEngine.Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
        }
        var shift = new UnityEngine.Vector3(-bounds.center.x, prop.lift ? -bounds.min.y : 0f, -bounds.center.z);
        if (UnityEngine.Mathf.Abs(shift.x) < 0.25f) shift.x = 0f;
        if (UnityEngine.Mathf.Abs(shift.z) < 0.25f) shift.z = 0f;
        model.transform.localPosition = shift;

        UnityEngine.ParticleSystem firstFire = null;
        if (lit)
        {
            for (var f = 0; f + 2 < prop.fires.Length; f += 3)
            {
                var fire = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(firePrefab);
                // "Torch" in the name: NightLights finds the light by it, whatever the root is called once placed
                fire.name = "Torch Fire";
                var at = shift + new UnityEngine.Vector3(prop.fires[f], prop.fires[f + 1], prop.fires[f + 2]);
                if (!firstFire)
                {
                    fire.transform.SetParent(root.transform, false);
                    fire.transform.localPosition = at;
                    fire.transform.localScale = UnityEngine.Vector3.one * prop.fireScale;
                }
                else
                {
                    // under the first, so the Torch lights and puts out both together
                    fire.transform.SetParent(firstFire.transform, false);
                    fire.transform.localPosition = (at - firstFire.transform.localPosition) / prop.fireScale;
                }
                var lightObject = new UnityEngine.GameObject("Point Light");
                lightObject.transform.SetParent(fire.transform, false);
                lightObject.transform.localPosition = new UnityEngine.Vector3(0f, 0.11f, 0f);
                // the island torches' own light
                var light = lightObject.AddComponent<UnityEngine.Light>();
                light.type = UnityEngine.LightType.Point;
                light.range = 10f;
                light.intensity = prop.fireScale < 0.3f ? 3f : 4.19f;
                light.color = new UnityEngine.Color(1f, 0.907f, 0.693f);
                light.shadows = UnityEngine.LightShadows.None;
                // lit by the Torch at night, so the tray's daylight picture shows the piece unlit
                if (!firstFire)
                {
                    fire.SetActive(false);
                    firstFire = fire.GetComponent<UnityEngine.ParticleSystem>();
                }
            }
        }

        var item = root.AddComponent<VillageDecorItem>();
        var itemSo = new UnityEditor.SerializedObject(item);
        itemSo.FindProperty("piece").stringValue = prop.key;
        itemSo.FindProperty("rectangle").boolValue = true;
        itemSo.ApplyModifiedPropertiesWithoutUndo();

        if (lit)
        {
            var torch = root.AddComponent<Torch>();
            var torchSo = new UnityEditor.SerializedObject(torch);
            // set here: the fire starts hidden, and Torch only looks for a shown one
            torchSo.FindProperty("fire").objectReferenceValue = firstFire;
            torchSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var prefabPath = decorFolder + "/Decor_" + prop.key + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        prefabPaths[prop.key] = prefabPath;
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(root);
    }
}
sb.AppendLine(props.Length + " prop prefabs in " + decorFolder);

// 1. the island's pieces
var land = UnityEngine.GameObject.Find("Town Landscape");
if (!land) return "no Town Landscape";
var defaults = new System.Collections.Generic.SortedDictionary<string, string>(System.StringComparer.Ordinal);
var unknown = new System.Collections.Generic.HashSet<string>();
foreach (var groupName in new[] { "Tree Belt", "Flowers And Stones" })
{
    var group = land.transform.Find(groupName);
    if (!group) { sb.AppendLine("no " + groupName); continue; }
    foreach (UnityEngine.Transform t in group)
    {
        var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
        var kind = (source ? source.name : System.Text.RegularExpressions.Regex.Replace(t.name, @"\s*\(\d+\)$", "")).Replace("SM_Env_", "").ToLowerInvariant();
        if (!prefabPaths.ContainsKey(kind)) { unknown.Add(kind); continue; }

        var item = t.GetComponent<VillageDecorItem>();
        string key;
        if (item && !string.IsNullOrEmpty(item.DefaultKey)) key = item.DefaultKey;
        else
        {
            key = kind + "@" + t.position.x.ToString("0.0", ci) + "," + t.position.z.ToString("0.0", ci);
            var n = 2; var baseKey = key;
            while (defaults.ContainsKey(key)) key = baseKey + "#" + n++;
            if (!item) item = UnityEditor.Undo.AddComponent<VillageDecorItem>(t.gameObject);
            item.Setup(key, kind);
            UnityEditor.EditorUtility.SetDirty(item);
        }
        defaults[key] = item.Piece;
    }
}
sb.AppendLine(defaults.Count + " island pieces" + (unknown.Count > 0 ? "; not on the price list: " + string.Join(", ", unknown) : ""));

// 2. the decor layer on the village manager, with the prefabs
var village = UnityEngine.Object.FindAnyObjectByType<Assets.Scripts.VillageManager>(UnityEngine.FindObjectsInactive.Include);
var layer = village.GetComponent<VillageDecor>() ?? UnityEditor.Undo.AddComponent<VillageDecor>(village.gameObject);
var so = new UnityEditor.SerializedObject(layer);
var list = so.FindProperty("prefabs");
list.ClearArray();
var i = 0;
foreach (var kv in prefabPaths)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(kv.Value);
    if (!prefab) { sb.AppendLine("missing prefab " + kv.Value); continue; }
    list.InsertArrayElementAtIndex(i);
    var entry = list.GetArrayElementAtIndex(i++);
    entry.FindPropertyRelative("Key").stringValue = kv.Key;
    entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
}
so.ApplyModifiedProperties();
sb.AppendLine(i + " piece prefabs on " + village.name);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(village.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(village.gameObject.scene);

// 3. the server's list
var cs = new System.Text.StringBuilder();
cs.AppendLine("using System.Collections.Generic;");
cs.AppendLine();
cs.AppendLine("namespace RavenNest.BusinessLogic.Game");
cs.AppendLine("{");
cs.AppendLine("    /// <summary>");
cs.AppendLine("    /// The village island's own decor pieces (trees, bushes, flowers, grass): key and piece. Generated by the game's");
cs.AppendLine("    /// tools/village-rebuild/decor-defaults.cs from its scene; do not edit by hand. Only these can be moved or removed");
cs.AppendLine("    /// as island pieces, so a made-up key cannot be removed for a refund.");
cs.AppendLine("    /// </summary>");
cs.AppendLine("    public static class VillageDecorDefaults");
cs.AppendLine("    {");
cs.AppendLine("        public static readonly IReadOnlyDictionary<string, string> Pieces = new Dictionary<string, string>");
cs.AppendLine("        {");
foreach (var kv in defaults) cs.AppendLine("            [\"" + kv.Key + "\"] = \"" + kv.Value + "\",");
cs.AppendLine("        };");
cs.AppendLine("    }");
cs.AppendLine("}");
System.IO.File.WriteAllText(@"G:\Ravenfall\Projects\RavenNest\src\RavenNest.BusinessLogic\Game\Managers\VillageDecorDefaults.cs", cs.ToString().Replace("\n", "\r\n"));
sb.AppendLine("server list written");
return sb.ToString();
