using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ravenfall &gt; World Resources &gt; Convert Scene: every TreeController and GatherController in the open scene
/// becomes an instance in a <see cref="ResourceField"/> on its chunk, at the same place, size and turn, and the old
/// objects are removed. The scene is saved to a copy first, and the whole conversion is one undo.
/// </summary>
/// <remarks>
/// <para>
/// Types are made (or updated) as it goes. Trees from the same prefab share a type; hand-placed trees share one
/// per art kit. A tree keeps the looks it could pick from (a hand-placed one, its own); its type has them all.
/// Gather spots are grouped by what they are (mushrooms, flowers, garden plants...) and get a random look from
/// their group.
/// </para>
/// <para>
/// A chunk whose trees are tougher or slower to come back than their type's gets that as the field's health or
/// respawn. Trees take their level from the chunk as before. Tilts of a few degrees (ground alignment) are
/// dropped: an instance stands upright; anything tilted or stretched more is listed in the report.
/// </para>
/// </remarks>
public static class ResourceSceneConverter
{
    private const float ReportTilt = 10f;
    private const float ReportStretch = 0.05f;

    // the tree prefabs' types; their own names are levels, which now come from the chunk
    private static readonly Dictionary<string, string> PrefabTypeNames = new Dictionary<string, string>
    {
        { "Tree Lv 1", "Adventure Trees" },
        { "Tree Lv 90", "Adventure Trees Away" },
        { "Tree Lv 150", "Adventure Trees 150" },
        { "Tree Lv 600", "Samurai Trees" },
        { "Tree Lv 1000", "Snowy Pines" },
    };

    [MenuItem("Ravenfall/World Resources/Convert Scene")]
    private static void ConvertMenu()
    {
        var scene = SceneManager.GetActiveScene();
        var trees = Find<TreeController>(scene).Count;
        var spots = Find<GatherController>(scene).Count;
        if (trees + spots == 0)
        {
            EditorUtility.DisplayDialog("Convert scene", "There are no TreeControllers or GatherControllers in " + scene.name + ".", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Convert scene",
            "Turn " + trees + " trees and " + spots + " gather spots in " + scene.name + " into resource fields, then remove the old objects?\n\n" +
            "The scene is saved to a copy first: " + BackupPath(scene) + ". Save the scene yourself once you have checked it.", "Convert", "Cancel"))
        {
            return;
        }

        var report = Convert(scene, true);
        Debug.Log(report);
        EditorUtility.DisplayDialog("Convert scene", report.Length > 1500 ? report.Substring(0, 1500) + "\n... (the full report is in the console)" : report, "OK");
    }

    public static string BackupPath(Scene scene) =>
        Path.GetDirectoryName(scene.path).Replace('\\', '/') + "/" + scene.name + "_before-resources.unity";

    /// <summary>Converts the scene and returns the report.</summary>
    public static string Convert(Scene scene, bool backup)
    {
        var log = new StringBuilder();
        if (backup)
        {
            if (string.IsNullOrEmpty(scene.path)) return "Save the scene once first, so there is a place for the copy. Nothing converted.";
            var path = BackupPath(scene);
            if (!EditorSceneManager.SaveScene(scene, path, true)) return "Could not save the copy to " + path + ". Nothing converted.";
            log.AppendLine("Copy of the scene before: " + path);
        }

        var trees = Find<TreeController>(scene);
        var spots = Find<GatherController>(scene);
        var problems = new List<string>();

        // the types, and each type's looks in its own order
        var looksOf = new Dictionary<ResourceType, List<ResourceTypeBuilder.LookSource>>();
        var treeType = new Dictionary<TreeController, ResourceType>();
        foreach (var group in trees.Where(t => ResourceTypeBuilder.TreeLooks(t).Count > 0).GroupBy(TreeTypeName))
        {
            var list = group.ToList();
            var type = ResourceTypeBuilder.FromTrees(list, group.Key, out var looks);
            if (!type) continue;
            looksOf[type] = looks;
            foreach (var t in list) treeType[t] = type;
            log.AppendLine("Type " + type.name + ": " + looks.Count + " looks, from " + list.Count + " trees");
        }

        var spotType = new Dictionary<GatherController, ResourceType>();
        var kitsOfFamily = spots.Select(ResourceTypeBuilder.LookOf).Where(l => l)
            .GroupBy(l => Family(ResourceTypeBuilder.LookName(l)))
            .ToDictionary(g => g.Key, g => g.Select(Kit).Distinct().Count());
        foreach (var group in spots.Where(s => ResourceTypeBuilder.LookOf(s)).GroupBy(s => GatherTypeName(s, kitsOfFamily)))
        {
            var list = group.ToList();
            var type = ResourceTypeBuilder.FromGatherLooks(list.Select(s => s.gameObject), group.Key, out var looks);
            if (!type) continue;
            looksOf[type] = looks;
            foreach (var s in list) spotType[s] = type;
            log.AppendLine("Type " + type.name + ": " + looks.Count + " looks, from " + list.Count + " gather spots");
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert trees and gather spots");

        var fields = new Dictionary<Chunk, ResourceField>();
        var before = new Dictionary<Chunk, int>();
        var converted = new List<GameObject>();
        // per field, what each instance had, to find the chunk's own numbers
        var healths = new Dictionary<ResourceField, List<(int had, int type)>>();
        var respawns = new Dictionary<ResourceField, List<(float had, float type)>>();

        foreach (var tree in trees)
        {
            var where = tree.gameObject.GetHierarchyPath();
            if (!treeType.TryGetValue(tree, out var type)) { problems.Add(where + ": no looks, left as it is"); continue; }
            if (!CanRemove(tree.gameObject, where, problems)) continue;
            var chunk = tree.GetComponentInParent<Chunk>(true);
            if (!chunk) { problems.Add(where + ": not in a chunk, left as it is"); continue; }

            var looks = looksOf[type];
            var own = ResourceTypeBuilder.TreeLooks(tree);
            var look = own[Pick(tree.transform.position, own.Count)];
            var variant = looks.FindIndex(l => l.key == ResourceTypeBuilder.LookKey(look));
            var field = FieldOf(chunk, fields, before, look);
            Place(field, type, variant, look.transform.localToWorldMatrix * looks[variant].FromLook, where, problems);

            var so = new SerializedObject(tree);
            Add(healths, field, (so.FindProperty("maxHealth").intValue, type.health));
            Add(respawns, field, (so.FindProperty("respawnTimeSeconds").floatValue, type.respawnSeconds));
            before[chunk]++;
            converted.Add(tree.gameObject);
        }

        foreach (var spot in spots)
        {
            var where = spot.gameObject.GetHierarchyPath();
            if (!spotType.TryGetValue(spot, out var type)) { problems.Add(where + ": no look, left as it is"); continue; }
            if (!CanRemove(spot.gameObject, where, problems)) continue;
            var chunk = spot.GetComponentInParent<Chunk>(true);
            if (!chunk) { problems.Add(where + ": not in a chunk, left as it is"); continue; }

            var looks = looksOf[type];
            var look = ResourceTypeBuilder.LookOf(spot);
            var own = looks.FindIndex(l => l.key == ResourceTypeBuilder.LookKey(look));
            var field = FieldOf(chunk, fields, before, look);
            // stands where its own look stood, with a random look of its group
            var variant = type.PickVariant(Random01(spot.transform.position));
            Place(field, type, variant, look.transform.localToWorldMatrix * looks[own].FromLook, where, problems);

            Add(respawns, field, (new SerializedObject(spot).FindProperty("respawnTimeSeconds").floatValue, type.respawnSeconds));
            before[chunk]++;
            converted.Add(spot.gameObject);
        }

        // a chunk whose trees are tougher or slower than their types keeps that
        foreach (var field in fields.Values)
        {
            if (healths.TryGetValue(field, out var h) && h.Any(x => x.had != x.type))
            {
                field.TreeHealth = h.GroupBy(x => x.had).OrderByDescending(g => g.Count()).First().Key;
                if (h.Select(x => x.had).Distinct().Count() > 1) problems.Add(field.transform.parent.name + ": trees had different health, the field uses " + field.TreeHealth);
            }
            if (respawns.TryGetValue(field, out var r) && r.Any(x => !Mathf.Approximately(x.had, x.type)))
            {
                field.RespawnSeconds = r.GroupBy(x => x.had).OrderByDescending(g => g.Count()).First().Key;
                if (r.Select(x => x.had).Distinct().Count() > 1) problems.Add(field.transform.parent.name + ": things had different respawn times, the field uses " + field.RespawnSeconds);
            }
            EditorUtility.SetDirty(field);
        }

        // the old objects, and the containers they leave empty
        var parents = new HashSet<Transform>();
        foreach (var go in converted)
        {
            if (go.transform.parent) parents.Add(go.transform.parent);
            Undo.DestroyObjectImmediate(go);
        }
        foreach (var parent in parents)
        {
            if (parent && parent.childCount == 0 && parent.GetComponents<Component>().Length == 1) Undo.DestroyObjectImmediate(parent.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        log.AppendLine();
        foreach (var pair in fields.OrderBy(p => p.Key.name))
        {
            var field = pair.Value;
            log.AppendLine(pair.Key.name + ": " + before[pair.Key] + " converted, field holds " + field.Count
                + (field.TreeHealth > 0 ? ", tree health " + field.TreeHealth : "")
                + (field.RespawnSeconds > 0f ? ", respawn " + field.RespawnSeconds + " s" : ""));
        }
        log.AppendLine(converted.Count + " objects removed.");
        if (problems.Count > 0)
        {
            log.AppendLine();
            log.AppendLine(problems.Count + " to look at:");
            foreach (var p in problems) log.AppendLine("- " + p);
        }
        return log.ToString();
    }

    private static List<T> Find<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToList();

    private static bool CanRemove(GameObject go, string where, List<string> problems)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go) return true;
        problems.Add(where + ": part of another prefab, left as it is");
        return false;
    }

    private static ResourceField FieldOf(Chunk chunk, Dictionary<Chunk, ResourceField> fields, Dictionary<Chunk, int> before, GameObject look)
    {
        if (fields.TryGetValue(chunk, out var field)) return field;
        field = chunk.GetComponentsInChildren<ResourceField>(true).FirstOrDefault();
        if (!field)
        {
            var go = new GameObject("Resources");
            go.transform.SetParent(chunk.transform, false);
            // drawn on the layer the old trees were drawn on, so cameras show them as before
            var renderer = look.GetComponentInChildren<Renderer>(true);
            go.layer = renderer ? renderer.gameObject.layer : chunk.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(go, "Convert trees and gather spots");
            field = go.AddComponent<ResourceField>();
        }
        Undo.RecordObject(field, "Convert trees and gather spots");
        fields[chunk] = field;
        before[chunk] = 0;
        return field;
    }

    /// <summary>Adds an instance standing as <paramref name="matrix"/> does: its place, its turn about up, its size.</summary>
    private static void Place(ResourceField field, ResourceType type, int variant, Matrix4x4 matrix, string where, List<string> problems)
    {
        Vector3 x = matrix.GetColumn(0), y = matrix.GetColumn(1), z = matrix.GetColumn(2);
        var scale = (x.magnitude + y.magnitude + z.magnitude) / 3f;
        var tilt = Vector3.Angle(y, Vector3.up);
        var stretch = Mathf.Max(x.magnitude, y.magnitude, z.magnitude) / Mathf.Max(0.0001f, Mathf.Min(x.magnitude, y.magnitude, z.magnitude)) - 1f;
        if (tilt > ReportTilt) problems.Add(where + ": tilted " + tilt.ToString("0") + " degrees, now upright");
        if (stretch > ReportStretch) problems.Add(where + ": stretched " + (stretch * 100f).ToString("0") + "%, now even");

        // the turn about up: the forward axis seen from above, or the right axis when forward points up or down
        var flat = new Vector3(z.x, 0f, z.z);
        var yaw = flat.sqrMagnitude > 0.0001f ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : Mathf.Atan2(-x.z, x.x) * Mathf.Rad2Deg;
        field.Add(type, Mathf.Max(0, variant), matrix.GetColumn(3), Mathf.Repeat(yaw, 360f), scale);
    }

    private static string TreeTypeName(TreeController tree)
    {
        if (PrefabUtility.GetOutermostPrefabInstanceRoot(tree.gameObject) == tree.gameObject)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(tree.gameObject);
            if (prefab) return PrefabTypeNames.TryGetValue(prefab.name, out var name) ? name : prefab.name;
        }
        var looks = ResourceTypeBuilder.TreeLooks(tree);
        return (looks.Count > 0 ? Kit(looks[0]) : "Other") + " Trees";
    }

    private static string GatherTypeName(GatherController spot, Dictionary<string, int> kitsOfFamily)
    {
        var look = ResourceTypeBuilder.LookOf(spot);
        var family = Family(ResourceTypeBuilder.LookName(look));
        return kitsOfFamily.TryGetValue(family, out var kits) && kits > 1 ? family + " " + Kit(look) : family;
    }

    /// <summary>What a gather look is, as a group: all mushrooms together, all flowers together.</summary>
    private static string Family(string lookName)
    {
        if (lookName.Contains("MushroomHouse")) return "Mushroom Houses";
        if (lookName.Contains("Mushroom")) return "Mushrooms";
        if (lookName.Contains("Flowers")) return "Flowers";
        if (lookName.Contains("Garden_Plants")) return "Garden Plants";
        if (lookName.Contains("Coconut")) return "Coconuts";
        if (lookName.Contains("Fairy")) return "Fairies";
        return lookName;
    }

    /// <summary>The art kit a look's mesh comes from: Assets/PolygonElvenRealm/... is "Elven Realm".</summary>
    private static string Kit(GameObject look)
    {
        var filter = look.GetComponentInChildren<MeshFilter>(true);
        var path = filter && filter.sharedMesh ? AssetDatabase.GetAssetPath(filter.sharedMesh) : "";
        var parts = path.Split('/');
        var folder = parts.Length > 2 ? parts[1] : "Other";
        if (folder.StartsWith("Polygon")) folder = folder.Substring("Polygon".Length);
        var words = new StringBuilder();
        foreach (var c in folder)
        {
            if (char.IsUpper(c) && words.Length > 0) words.Append(' ');
            words.Append(c);
        }
        return words.Length > 0 ? words.ToString() : "Other";
    }

    // the same choices every time for the same scene
    private static float Random01(Vector3 position)
    {
        unchecked
        {
            var hash = Mathf.RoundToInt(position.x * 100f) * 73856093 ^ Mathf.RoundToInt(position.y * 100f) * 19349663 ^ Mathf.RoundToInt(position.z * 100f) * 83492791;
            return (float)new System.Random(hash).NextDouble();
        }
    }

    private static int Pick(Vector3 position, int count) => Mathf.Clamp((int)(Random01(position) * count), 0, count - 1);

    private static void Add<TKey, TValue>(Dictionary<TKey, List<TValue>> lists, TKey key, TValue value)
    {
        if (!lists.TryGetValue(key, out var list)) lists[key] = list = new List<TValue>();
        list.Add(value);
    }
}
