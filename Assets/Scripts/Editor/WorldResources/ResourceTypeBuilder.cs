using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Makes <see cref="ResourceType"/>s from the existing tree and gather prefabs or scene objects: Ravenfall &gt;
/// World Resources &gt; Create Type From Selection.
/// </summary>
/// <remarks>
/// <para>
/// Select one object with a TreeController (a tree prefab, or a tree in the scene): its variants and stump
/// become the type's looks, and its health, respawn and reach the type's numbers. Or select a GatherController,
/// or several of them, or several plain objects: each distinct look among them becomes a variant, so a type
/// such as Mushrooms picks one of its looks at random per spot.
/// </para>
/// <para>
/// Each look is baked into one mesh per level of detail: all its parts in the resource's own space, one
/// submesh per material, so a tree draws as one instanced mesh per material. The levels and their distances
/// come from the parts' LODGroups. The meshes are written as one binary FBX per type in
/// Assets/World/WorldResources/Models, and the type asset to Assets/World/WorldResources/Types.
/// </para>
/// </remarks>
public static class ResourceTypeBuilder
{
    public const string TypesDir = "Assets/World/WorldResources/Types";
    public const string ModelsDir = "Assets/World/WorldResources/Models";
    private const float TanHalfReferenceFov = 0.57735f;   // LOD distances are stored for a 60 degree view

    /// <summary>One look to bake: a name, the object it comes from, and the space it is baked in.</summary>
    public struct LookSource
    {
        public string name;
        public GameObject source;
        public Transform space;
        /// <summary>What makes it this look (see <see cref="LookKey"/>).</summary>
        public string key;

        /// <summary>
        /// From the look object's space to the baked space. Another object with the same look stands at
        /// <c>its localToWorldMatrix * FromLook</c>, so it can be drawn exactly where it is.
        /// </summary>
        public Matrix4x4 FromLook => source.transform.worldToLocalMatrix * space.localToWorldMatrix;
    }

    [MenuItem("Ravenfall/World Resources/Create Type From Selection")]
    private static void FromSelection()
    {
        var selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Create resource type", "Select a tree (an object with a TreeController), or gather spots or plain objects to become one type's looks.", "OK");
            return;
        }

        var tree = selected.Select(s => s.GetComponent<TreeController>()).FirstOrDefault(t => t);
        var type = tree ? FromTree(tree, selected[0].name) : FromGatherLooks(selected, selected[0].name);
        if (type)
        {
            Selection.activeObject = type;
            EditorGUIUtility.PingObject(type);
        }
    }

    /// <summary>A tree type from a TreeController: its variants, its stump and its numbers.</summary>
    public static ResourceType FromTree(TreeController tree, string name) => FromTrees(new[] { tree }, name, out _);

    /// <summary>
    /// A tree type from several TreeControllers: every distinct look among their variants, the first stump, and
    /// the numbers most of them have. <paramref name="looks"/> are the variants in the type's order.
    /// </summary>
    public static ResourceType FromTrees(IList<TreeController> trees, string name, out List<LookSource> looks)
    {
        looks = new List<LookSource>();
        var seen = new HashSet<string>();
        LookSource depleted = default;
        foreach (var tree in trees)
        {
            foreach (var go in TreeLooks(tree))
            {
                var key = LookKey(go);
                if (seen.Add(key)) looks.Add(new LookSource { name = UniqueName(looks, LookName(go)), source = go, space = tree.transform, key = key });
            }
            var stump = new SerializedObject(tree).FindProperty("stump").objectReferenceValue as GameObject;
            if (stump && !depleted.source) depleted = new LookSource { name = "Stump", source = stump, space = tree.transform, key = LookKey(stump) };
        }

        var type = Build(name, ResourceType.ResourceKind.Tree, looks, depleted);
        if (!type) return null;
        type.health = Mathf.Max(1, MostCommon(trees.Select(t => new SerializedObject(t).FindProperty("maxHealth").intValue)));
        type.respawnSeconds = Mathf.Max(1f, MostCommon(trees.Select(t => new SerializedObject(t).FindProperty("respawnTimeSeconds").floatValue)));
        type.actionDistance = ReachOf(trees[0].gameObject, 3f);
        Save(type);
        return type;
    }

    /// <summary>The looks a tree picks from, as its TreeController lists them.</summary>
    public static List<GameObject> TreeLooks(TreeController tree)
    {
        var looks = new List<GameObject>();
        var list = new SerializedObject(tree).FindProperty("trees");
        for (var i = 0; i < list.arraySize; ++i)
        {
            var go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go) looks.Add(go);
        }
        return looks;
    }

    /// <summary>
    /// What a look is: its meshes and their materials. Two objects with the same key look the same, whatever
    /// they are called; the same mesh in another colour is another look.
    /// </summary>
    public static string LookKey(GameObject look)
    {
        var parts = new List<string>();
        foreach (var r in look.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = r.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;
            parts.Add(filter.sharedMesh.name + ":" + string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "-")));
        }
        return string.Join("|", parts);
    }

    private static string UniqueName(List<LookSource> looks, string name)
    {
        if (looks.All(l => l.name != name)) return name;
        for (var n = 2; ; ++n)
        {
            var candidate = name + " " + n;
            if (looks.All(l => l.name != candidate)) return candidate;
        }
    }

    private static T MostCommon<T>(IEnumerable<T> values) => values.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;

    /// <summary>A gather type whose looks are the distinct looks among the selected spots or objects.</summary>
    public static ResourceType FromGatherLooks(IEnumerable<GameObject> sources, string name) => FromGatherLooks(sources, name, out _);

    /// <summary>The same, handing back the looks in the type's order.</summary>
    public static ResourceType FromGatherLooks(IEnumerable<GameObject> sources, string name, out List<LookSource> looks)
    {
        var variants = looks = new List<LookSource>();
        var seen = new HashSet<string>();
        GatherController first = null;
        foreach (var go in sources)
        {
            var gather = go.GetComponent<GatherController>();
            if (gather && !first) first = gather;
            // a gather spot's look is its gatherObj, else its first child, as GatherController uses it; a plain
            // object is its own look
            var look = gather ? LookOf(gather) : go;
            if (!look) continue;
            var key = LookKey(look);
            if (!seen.Add(key)) continue;
            variants.Add(new LookSource { name = UniqueName(variants, LookName(look)), source = look, space = gather ? gather.transform : go.transform, key = key });
        }

        var type = Build(name, ResourceType.ResourceKind.Gatherable, variants, default);
        if (!type) return null;
        if (first)
        {
            var so = new SerializedObject(first);
            type.respawnSeconds = Mathf.Max(1f, so.FindProperty("respawnTimeSeconds").floatValue);
            type.kneelToGather = so.FindProperty("PlayKneelingAnimation")?.boolValue ?? true;
            type.actionDistance = ReachOf(first.gameObject, 3f);
        }
        Save(type);
        return type;
    }

    public static GameObject LookOf(GatherController gather)
    {
        var set = new SerializedObject(gather).FindProperty("gatherObj").objectReferenceValue as GameObject;
        if (set) return set;
        return gather.transform.childCount > 0 ? gather.transform.GetChild(0).gameObject : null;
    }

    /// <summary>The name a look goes by: its prefab's name when it is one, so the same look is recognised wherever it stands.</summary>
    public static string LookName(GameObject look)
    {
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(look);
        var name = prefab ? prefab.name : look.name;
        var paren = name.IndexOf(" (", System.StringComparison.Ordinal);
        return paren > 0 ? name.Substring(0, paren) : name;
    }

    private static float ReachOf(GameObject go, float fallback)
    {
        var sphere = go.GetComponent<SphereCollider>();
        if (!sphere) return fallback;
        var s = go.transform.lossyScale;
        return sphere.radius * Mathf.Max(s.x, Mathf.Max(s.y, s.z));
    }

    /// <summary>Bakes the looks into meshes, writes them to an FBX and makes (or updates) the type asset.</summary>
    public static ResourceType Build(string name, ResourceType.ResourceKind kind, IList<LookSource> variants, LookSource depleted)
    {
        if (variants.Count == 0)
        {
            Debug.LogWarning("Resource type " + name + ": nothing to bake.");
            return null;
        }

        EnsureFolder(TypesDir);
        EnsureFolder(ModelsDir);
        var safe = Safe(name);
        var typePath = TypesDir + "/" + safe + ".asset";
        var modelPath = ModelsDir + "/" + safe + ".fbx";

        // bake every look's levels into meshes, under one root to export
        var root = new GameObject(safe);
        var baked = new List<(int look, int lod, Mesh mesh, Material[] materials, float distance)>();
        var all = new List<LookSource>(variants);
        if (depleted.source) all.Add(depleted);
        for (var l = 0; l < all.Count; ++l)
        {
            var levels = Levels(all[l]);
            for (var d = 0; d < levels.Count; ++d)
            {
                var (mesh, materials) = Combine(levels[d].renderers, all[l].space);
                if (!mesh) continue;
                var meshName = safe + "_" + l + "_LOD" + d;
                mesh.name = meshName;
                var go = new GameObject(meshName);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = materials;
                baked.Add((l, d, mesh, materials, levels[d].distance));
            }
        }

        UnityEditor.Formats.Fbx.Exporter.ModelExporter.ExportObject(modelPath, root);
        Object.DestroyImmediate(root);
        foreach (var b in baked) Object.DestroyImmediate(b.mesh);

        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.bakeAxisConversion = true;
        importer.isReadable = false;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        var meshes = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Mesh>().ToDictionary(m => m.name);

        var type = AssetDatabase.LoadAssetAtPath<ResourceType>(typePath);
        var created = !type;
        if (created) type = ScriptableObject.CreateInstance<ResourceType>();
        type.kind = kind;
        type.variants = new ResourceType.Look[variants.Count];
        for (var l = 0; l < all.Count; ++l)
        {
            var look = new ResourceType.Look
            {
                name = all[l].name,
                weight = 1f,
                lods = baked.Where(b => b.look == l).OrderBy(b => b.lod).Select(b => new ResourceType.Lod
                {
                    mesh = meshes.TryGetValue(safe + "_" + l + "_LOD" + b.lod, out var m) ? m : null,
                    materials = b.materials,
                    maxDistance = b.distance,
                }).Where(x => x.mesh).ToArray(),
            };
            if (l < variants.Count) type.variants[l] = look;
            else type.depleted = look;
        }
        if (!depleted.source) type.depleted = new ResourceType.Look { name = "Depleted" };

        if (created) AssetDatabase.CreateAsset(type, typePath);
        Save(type);
        Debug.Log("Resource type " + name + ": " + variants.Count + " looks" + (depleted.source ? " and a depleted look" : "") + ", " + baked.Count + " meshes -> " + typePath);
        return type;
    }

    private struct Level
    {
        public List<Renderer> renderers;
        public float distance;
    }

    /// <summary>
    /// A look's levels of detail: from its LODGroups when it has any (parts outside every group show at
    /// every level), else one level of all its renderers, drawn at any distance.
    /// </summary>
    private static List<Level> Levels(LookSource look)
    {
        var groups = look.source.GetComponentsInChildren<LODGroup>(true);
        var renderers = look.source.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToList();
        if (groups.Length == 0) return new List<Level> { new Level { renderers = renderers, distance = 0f } };

        var inGroups = new HashSet<Renderer>();
        foreach (var g in groups) foreach (var lod in g.GetLODs()) foreach (var r in lod.renderers) if (r) inGroups.Add(r);
        var loose = renderers.Where(r => !inGroups.Contains(r)).ToList();

        var levels = new List<Level>();
        var levelCount = groups.Max(g => g.lodCount);
        var bias = QualitySettings.lodBias;
        for (var l = 0; l < levelCount; ++l)
        {
            var level = new Level { renderers = new List<Renderer>(loose) };
            foreach (var g in groups)
            {
                var lods = g.GetLODs();
                var lod = lods[Mathf.Min(l, lods.Length - 1)];
                level.renderers.AddRange(lod.renderers.Where(r => r));
            }

            // the distance at which the first group's level gives way, for an object its size at 60 degrees
            var first = groups[0];
            var firstLods = first.GetLODs();
            var height = firstLods[Mathf.Min(l, firstLods.Length - 1)].screenRelativeTransitionHeight;
            var scale = first.transform.lossyScale;
            var size = first.size * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            level.distance = height > 0.0001f ? size / (2f * height * TanHalfReferenceFov) * bias : 0f;
            levels.Add(level);
        }
        return levels;
    }

    /// <summary>The renderers combined into one mesh in <paramref name="space"/>, one submesh per material.</summary>
    private static (Mesh, Material[]) Combine(List<Renderer> renderers, Transform space)
    {
        var byMaterial = new Dictionary<Material, List<CombineInstance>>();
        var order = new List<Material>();
        var toSpace = space.worldToLocalMatrix;
        foreach (var r in renderers)
        {
            var filter = r.GetComponent<MeshFilter>();
            var mesh = filter ? filter.sharedMesh : null;
            if (!mesh) continue;
            var mats = r.sharedMaterials;
            for (var s = 0; s < mesh.subMeshCount; ++s)
            {
                var mat = mats.Length > 0 ? mats[Mathf.Min(s, mats.Length - 1)] : null;
                if (!mat) continue;
                if (!byMaterial.TryGetValue(mat, out var list)) { byMaterial[mat] = list = new List<CombineInstance>(); order.Add(mat); }
                list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = toSpace * r.transform.localToWorldMatrix });
            }
        }
        if (order.Count == 0) return (null, null);

        var parts = new List<CombineInstance>();
        foreach (var mat in order)
        {
            var part = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            part.CombineMeshes(byMaterial[mat].ToArray(), true, true);
            parts.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
        }
        // 16 bit indices when they fit; chosen before combining, since changing it afterwards drops the indices
        var vertices = parts.Sum(p => p.mesh.vertexCount);
        var combined = new Mesh { indexFormat = vertices < 65000 ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32 };
        combined.CombineMeshes(parts.ToArray(), false, false);
        combined.RecalculateBounds();
        foreach (var p in parts) Object.DestroyImmediate(p.mesh);
        return (combined, order.ToArray());
    }

    private static void Save(ResourceType type)
    {
        EditorUtility.SetDirty(type);
        AssetDatabase.SaveAssets();
    }

    private static string Safe(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace(' ', '_').Replace('.', '_');
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
