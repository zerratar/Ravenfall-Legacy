using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The character parts of the players, shared. The player prefab holds every hair, head, helmet, torso and boot
/// a player could wear, each with its own skinned mesh renderer: over 700 parts, of which a player uses about 17.
/// Every player used to carry all of them, about 3 MB and 5 ms to instantiate each.
/// </summary>
/// <remarks>
/// <para>
/// Players are now made from a copy of the prefab whose unused parts have lost their renderers: the part objects stay,
/// so the appearance code keeps switching them on and off as before, but a part only gets a renderer when it is put
/// on (<see cref="RendererOf"/>), copied from the prefab and bound to the player's own bones. A player is about
/// 0.85 MB and 1.7 ms to instantiate.
/// </para>
/// <para>
/// Parts are matched by where they are in the hierarchy, which the game never reorders (equipment is added at the end
/// of a bone's children); a bone is checked against the prefab's bone name, and looked for by name if it differs.
/// </para>
/// </remarks>
public static class PlayerParts
{
    /// <summary>A prefab's stripped copy and where each stripped part's renderer came from.</summary>
    public sealed class Source
    {
        public GameObject prefab;
        public GameObject lean;
        public readonly Dictionary<string, SkinnedMeshRenderer> parts = new Dictionary<string, SkinnedMeshRenderer>();
        public readonly Dictionary<Transform, int[]> bonePaths = new Dictionary<Transform, int[]>();
    }

    private static readonly Dictionary<GameObject, Source> sources = new Dictionary<GameObject, Source>();
    private static readonly List<int> pathScratch = new List<int>();
    private static Transform holder;

    /// <summary>Players made from the stripped copy; off makes them from the full prefab again, as before.</summary>
    public static bool Shared { get; set; } = true;

    /// <summary>
    /// A new player from <paramref name="prefab"/>, its unused parts without renderers. The appearance knows where to
    /// get them when it needs them.
    /// </summary>
    public static GameObject Instantiate(GameObject prefab)
    {
        if (!Shared) return Object.Instantiate(prefab);
        var source = SourceOf(prefab);
        var player = Object.Instantiate(source.lean);
        var appearance = player.GetComponentInChildren<SyntyPlayerAppearance>(true);
        if (appearance) appearance.PartsSource = source;
        return player;
    }

    /// <summary>
    /// The renderer of a part, given one first if the part has lost it: the prefab's mesh, materials and settings,
    /// bound to this player's bones. Null for a part that never had one.
    /// </summary>
    public static SkinnedMeshRenderer RendererOf(GameObject part, SyntyPlayerAppearance appearance)
    {
        if (!part) return null;
        if (part.TryGetComponent<SkinnedMeshRenderer>(out var existing)) return existing;
        var source = appearance ? appearance.PartsSource : null;
        if (source == null) return null;

        var root = appearance.transform;
        if (!source.parts.TryGetValue(PathOf(part.transform, root), out var from) || !from) return null;

        var renderer = part.AddComponent<SkinnedMeshRenderer>();
        var bones = from.bones;
        var mapped = new Transform[bones.Length];
        for (var i = 0; i < bones.Length; ++i) mapped[i] = Bone(source, bones[i], root);
        renderer.sharedMesh = from.sharedMesh;
        renderer.bones = mapped;
        renderer.rootBone = Bone(source, from.rootBone, root);
        renderer.sharedMaterials = from.sharedMaterials;
        renderer.localBounds = from.localBounds;
        renderer.quality = from.quality;
        renderer.updateWhenOffscreen = from.updateWhenOffscreen;
        renderer.skinnedMotionVectors = from.skinnedMotionVectors;
        renderer.shadowCastingMode = from.shadowCastingMode;
        renderer.receiveShadows = from.receiveShadows;
        renderer.lightProbeUsage = from.lightProbeUsage;
        renderer.reflectionProbeUsage = from.reflectionProbeUsage;
        renderer.motionVectorGenerationMode = from.motionVectorGenerationMode;
        renderer.renderingLayerMask = from.renderingLayerMask;
        renderer.probeAnchor = Bone(source, from.probeAnchor, root);
        return renderer;
    }

    /// <summary>The prefab's stripped copy, made the first time: kept inactive and out of the scene's way.</summary>
    private static Source SourceOf(GameObject prefab)
    {
        if (sources.TryGetValue(prefab, out var source) && source.lean) return source;

        if (!holder)
        {
            var go = new GameObject("Player Parts");
            go.SetActive(false);
            // out of the hierarchy; in play it goes with the session (a DontSave object would outlive it in the
            // editor, one more copy of the prefab each time play starts)
            go.hideFlags = Application.isPlaying ? HideFlags.HideInHierarchy : HideFlags.HideAndDontSave;
            if (Application.isPlaying) Object.DontDestroyOnLoad(go);
            holder = go.transform;
        }

        source = new Source { prefab = prefab, lean = Object.Instantiate(prefab, holder) };
        source.lean.name = prefab.name;
        var prefabRoot = prefab.transform;
        var leanRoot = source.lean.transform;
        foreach (var renderer in source.lean.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.gameObject.activeSelf) continue;
            var path = PathOf(renderer.transform, leanRoot);
            var original = Resolve(prefabRoot, path);
            var from = original ? original.GetComponent<SkinnedMeshRenderer>() : null;
            if (!from) continue;
            source.parts[path] = from;
            Object.DestroyImmediate(renderer);
        }
        sources[prefab] = source;
        return source;
    }

    // a prefab bone, found in the player by the same place in the hierarchy, or by name if that is not it
    private static Transform Bone(Source source, Transform bone, Transform root)
    {
        if (!bone) return null;
        if (!source.bonePaths.TryGetValue(bone, out var path))
        {
            path = IndexPath(bone, source.prefab.transform);
            source.bonePaths[bone] = path;
        }
        if (path.Length == 0) return root;

        var found = root;
        foreach (var index in path)
        {
            if (index >= found.childCount) { found = null; break; }
            found = found.GetChild(index);
        }
        if (found && found.name == bone.name) return found;
        return FindByName(root, bone.name);
    }

    private static Transform FindByName(Transform under, string name)
    {
        if (under.name == name) return under;
        for (var i = 0; i < under.childCount; ++i)
        {
            var found = FindByName(under.GetChild(i), name);
            if (found) return found;
        }
        return null;
    }

    private static int[] IndexPath(Transform t, Transform root)
    {
        pathScratch.Clear();
        while (t && t != root)
        {
            pathScratch.Add(t.GetSiblingIndex());
            t = t.parent;
        }
        pathScratch.Reverse();
        return pathScratch.ToArray();
    }

    private static string PathOf(Transform t, Transform root) => string.Join("/", IndexPath(t, root));

    private static Transform Resolve(Transform root, string path)
    {
        var t = root;
        foreach (var part in path.Split('/'))
        {
            if (part.Length == 0) continue;
            var index = int.Parse(part);
            if (index >= t.childCount) return null;
            t = t.GetChild(index);
        }
        return t;
    }
}
