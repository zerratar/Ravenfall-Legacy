// Run with: unity command eval_file --file tools/village-rebuild/decor-defaults.cs
//
// Makes the village island's own trees, bushes, flowers and grass into decor (docs/village-decor-design.md):
// - every piece under Town Landscape/Tree Belt and Town Landscape/Flowers And Stones gets a VillageDecorItem with a
//   stable key (its piece and where it stands, "tree_02@-31.2,-150.4") and its piece of the price list;
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
