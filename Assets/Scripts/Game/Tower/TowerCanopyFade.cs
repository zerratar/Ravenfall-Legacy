using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     Tree tops between the camera and the players it watches in a tower place fade away, and come back once the camera
///     has turned past them (docs/tower-design.md section 23, playtest 2026-11-26: in the Blighted Grove the mangroves'
///     canopies hid the whole group from the tower camera). The indoor floors cut their walls down
///     (<see cref="TowerCutaway"/>); open places have trees, and a tree goes see-through instead, never at once: about a
///     third of a second out, a little more back in. Its shadow stays on the ground, so the light under it does not
///     change. Every other tree stands as it is.
/// </summary>
/// <remarks>
///     <para>
///     Two ways to fade, by the tree's shader, each with the tree's own materials so its look does not change as it
///     starts: Synty's vegetation shader dithers itself away through its own LOD dithering map (a screen-space noise
///     texture; per tree a texture of the fade's level is set with a property block, and nothing else changes), and a
///     tree in URP's Lit shader (the grove's mangroves) draws as a see-through copy of its materials while it fades. A
///     tree in any other shader (the pines' Lit shader graph takes its alpha from the texture alone) stays as it is.
///     </para>
///     <para>
///     The cost: ten times a second a ray test per tree against the watched players (a hundred trees and a dozen players
///     are a thousand box tests), and per frame only the trees that are fading. Off with its setting and in potato mode
///     (<see cref="SettingsApplier.TreeFade"/>), where nothing runs and every tree stands. Added to each tower place by
///     the place itself.
///     </para>
/// </remarks>
public sealed class TowerCanopyFade : MonoBehaviour
{
    private const float CheckEvery = 0.1f;
    private const float FadeOutSeconds = 0.35f, FadeInSeconds = 0.6f;
    /// <summary>A tree stays faded this long after it was last in the way: a slowly turning camera does not make it blink.</summary>
    private const float Linger = 1.2f;
    /// <summary>The camera farther than this from the place (another place, the world): every tree stands.</summary>
    private const float Reach = 260f;
    /// <summary>Players further than this from what the camera looks at are not what it watches.</summary>
    private const float WatchRadius = 40f;
    private const int MaxTargets = 24;
    /// <summary>Lower than this is a bush, not a tree top over anyone's head.</summary>
    private const float MinTreeHeight = 4.5f;
    /// <summary>Steps of the dither between whole and gone.</summary>
    private const int Levels = 16;
    private const int NoiseSize = 64;

    private static readonly int DitherMapId = Shader.PropertyToID("_LOD_DitheringMap");
    private static readonly int DitherMapTexelId = Shader.PropertyToID("_LOD_DitheringMap_TexelSize");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private sealed class Tree
    {
        public Renderer[] Renderers;
        /// <summary>The tree top: the upper part of its bounds, a little inside them.</summary>
        public Bounds Top;
        /// <summary>Synty's vegetation shader dithers itself; else a see-through copy of the materials.</summary>
        public bool Dither;
        /// <summary>0 as it is, 1 gone.</summary>
        public float Fade;
        public float Until;
        public bool Applied;
        public Material[][] Originals;
        public ShadowCastingMode[] Shadows;
        /// <summary>The dithering tree's shadow while it fades: the same mesh, shadow only (the dither would thin it).</summary>
        public Renderer[] Twins;
    }

    private readonly List<Tree> trees = new List<Tree>();
    private readonly List<Vector3> targets = new List<Vector3>();
    private TowerPlace place;
    private bool scanned;
    private bool anyFaded;
    private float nextCheck;
    private Camera cam;
    private OrbitCamera orbit;

    private static Texture2D[] levels;
    private static MaterialPropertyBlock block;
    private static readonly Dictionary<Material, Material> seeThrough = new Dictionary<Material, Material>();

    private void Awake() => place = GetComponent<TowerPlace>();

    private void OnDisable() => RestoreAll();

    private void LateUpdate()
    {
        if (!SettingsApplier.TreeFade)
        {
            if (anyFaded) RestoreAll();
            return;
        }
        if (!cam || !cam.isActiveAndEnabled)
        {
            cam = Camera.main;
            orbit = cam ? cam.GetComponent<OrbitCamera>() : null;
        }
        if (!cam || !place) return;
        var eye = cam.transform.position;
        var away = eye - place.Centre;
        away.y = 0f;
        if (away.sqrMagnitude > (place.Radius + Reach) * (place.Radius + Reach))
        {
            if (anyFaded) RestoreAll();
            return;
        }
        if (!scanned) Scan();
        if (trees.Count == 0) return;

        var now = Time.unscaledTime;
        if (now >= nextCheck)
        {
            nextCheck = now + CheckEvery;
            Check(eye, now);
        }

        var dt = Time.unscaledDeltaTime;
        anyFaded = false;
        for (var i = 0; i < trees.Count; i++)
        {
            var tree = trees[i];
            var target = tree.Until > now ? 1f : 0f;
            if (tree.Fade == target && !(target == 0f && tree.Applied)) { if (tree.Fade > 0f) anyFaded = true; continue; }
            tree.Fade = Mathf.MoveTowards(tree.Fade, target, dt / (target > tree.Fade ? FadeOutSeconds : FadeInSeconds));
            Apply(tree);
            if (tree.Fade > 0f) anyFaded = true;
        }
    }

    // ---- What is in the way ---------------------------------------------------------------------------------

    private void Check(Vector3 eye, float now)
    {
        var focus = Focus(eye);
        targets.Clear();
        targets.Add(focus);
        // the players here the camera watches: near what it looks at, and on the screen
        var game = GameManager.Instance;
        if (game && game.Players != null)
        {
            var town = place as TowerTown;
            var floor = place as TowerFloor;
            var players = game.Players.GetAllPlayers();
            for (var i = 0; i < players.Count && targets.Count < MaxTargets; i++)
            {
                var p = players[i];
                if (!p) continue;
                var handler = p.towerHandler;
                if (handler == null || !handler.InTower) continue;
                if (town ? !(handler.InTown && TowerTown.Loaded == town) : handler.Floor != floor) continue;
                var at = p.Position;
                var d = at - focus;
                d.y = 0f;
                if (d.sqrMagnitude > WatchRadius * WatchRadius) continue;
                var view = cam.WorldToViewportPoint(at);
                if (view.z <= 0f || view.x < -0.05f || view.x > 1.05f || view.y < -0.05f || view.y > 1.05f) continue;
                targets.Add(at + Vector3.up * 1.2f);
            }
        }

        for (var t = 0; t < trees.Count; t++)
        {
            var tree = trees[t];
            for (var i = 0; i < targets.Count; i++)
            {
                var to = targets[i] - eye;
                var length = to.magnitude;
                if (length < 0.5f) continue;
                if (tree.Top.IntersectRay(new Ray(eye, to / length), out var hit) && hit < length)
                {
                    tree.Until = now + Linger;
                    break;
                }
            }
        }
    }

    /// <summary>What the camera looks at: the point the orbit camera follows (the group's, or a player's), else where its view meets the ground.</summary>
    private Vector3 Focus(Vector3 eye)
    {
        if (orbit && orbit.isActiveAndEnabled && orbit.TargetTransform) return orbit.TargetTransform.position + Vector3.up;
        var forward = cam.transform.forward;
        var groundY = place.Centre.y;
        if (forward.y < -0.08f) return eye + forward * Mathf.Min((eye.y - groundY - 1f) / -forward.y, 60f);
        return eye + forward * 25f;
    }

    // ---- The trees --------------------------------------------------------------------------------------------

    /// <summary>The place's trees, once: by name (Synty's SM_Env_Tree_*, the grove's mangroves), tall enough, in a shader that can fade.</summary>
    private void Scan()
    {
        scanned = true;
        trees.Clear();
        var seen = new HashSet<Renderer>();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!r || seen.Contains(r) || !r.enabled || !IsTree(r.name)) continue;
                var group = r.GetComponentInParent<LODGroup>();
                Renderer[] parts;
                Renderer[] shadows;
                if (group)
                {
                    var list = new List<Renderer>();
                    var lods = group.GetLODs();
                    foreach (var lod in lods) foreach (var x in lod.renderers) if (x && !list.Contains(x)) list.Add(x);
                    parts = list.ToArray();
                    shadows = lods.Length > 0 ? System.Array.FindAll(lods[0].renderers, x => x) : parts;
                }
                else
                {
                    parts = new Renderer[] { r };
                    shadows = parts;
                }
                foreach (var x in parts) seen.Add(x);
                if (System.Array.IndexOf(parts, r) < 0) { seen.Add(r); continue; }
                var tree = Make(parts, shadows);
                if (tree != null) trees.Add(tree);
            }
        }
    }

    private static bool IsTree(string name)
    {
        if (name.StartsWith("Mangrove")) return true;
        if (name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) < 0) return false;
        // what lies on the ground or stands bare does not hide anyone
        return name.IndexOf("Stump", System.StringComparison.OrdinalIgnoreCase) < 0
            && name.IndexOf("Log", System.StringComparison.OrdinalIgnoreCase) < 0
            && name.IndexOf("Fallen", System.StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static Tree Make(Renderer[] parts, Renderer[] shadows)
    {
        var dither = 0;
        var clear = 0;
        var materials = 0;
        var first = true;
        var bounds = new Bounds();
        foreach (var r in shadows)
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        foreach (var r in parts)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
                materials++;
                if (m.HasProperty(DitherMapId)) dither++;
                else if (m.HasProperty(SurfaceId) && m.HasProperty(BaseColorId) && m.shader.name.StartsWith("Universal Render Pipeline/")) clear++;
            }
        }
        // a tree whose every material can fade the same way, tall enough to be over someone's head
        if (first || materials == 0 || bounds.size.y < MinTreeHeight) return null;
        if (dither != materials && clear != materials) return null;
        var top = bounds;
        var min = top.min;
        var max = top.max;
        // the top: from a little under halfway up (the trunk alone hides nobody), a little inside the bounds' corners
        min.y += bounds.size.y * 0.4f;
        var inset = new Vector3(bounds.size.x * 0.08f, 0f, bounds.size.z * 0.08f);
        top.SetMinMax(min + inset, max - inset);
        return new Tree
        {
            Renderers = parts,
            Top = top,
            Dither = dither == materials,
            Shadows = new ShadowCastingMode[parts.Length],
            Originals = new Material[parts.Length][],
            Twins = new Renderer[0],
        };
    }

    // ---- Fading ---------------------------------------------------------------------------------------------

    private void Apply(Tree tree)
    {
        if (tree.Fade <= 0f)
        {
            Restore(tree);
            return;
        }
        block ??= new MaterialPropertyBlock();
        if (!tree.Applied)
        {
            tree.Applied = true;
            for (var i = 0; i < tree.Renderers.Length; i++)
            {
                var r = tree.Renderers[i];
                if (!r) continue;
                tree.Shadows[i] = r.shadowCastingMode;
                tree.Originals[i] = r.sharedMaterials;
            }
            if (tree.Dither) ShowTwins(tree, true);
            else
            {
                foreach (var r in tree.Renderers)
                {
                    if (!r) continue;
                    var mats = r.sharedMaterials;
                    for (var m = 0; m < mats.Length; m++) mats[m] = SeeThrough(mats[m]);
                    r.sharedMaterials = mats;
                }
            }
        }

        if (tree.Dither)
        {
            var level = Mathf.Clamp(Mathf.CeilToInt(tree.Fade * Levels), 1, Levels);
            block.Clear();
            block.SetTexture(DitherMapId, Level(level));
            block.SetVector(DitherMapTexelId, new Vector4(1f / NoiseSize, 1f / NoiseSize, NoiseSize, NoiseSize));
            foreach (var r in tree.Renderers)
            {
                if (!r) continue;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.SetPropertyBlock(block);
            }
            return;
        }

        // see-through: its color's alpha goes down; all the way gone it only casts its shadow (an invisible tree that
        // still wrote depth would hide the spells and sparks behind it)
        var gone = tree.Fade >= 0.999f;
        for (var i = 0; i < tree.Renderers.Length; i++)
        {
            var r = tree.Renderers[i];
            if (!r) continue;
            var originals = tree.Originals[i];
            r.shadowCastingMode = gone ? ShadowCastingMode.ShadowsOnly : tree.Shadows[i];
            for (var m = 0; m < originals.Length; m++)
            {
                if (!originals[m]) continue;
                var c = originals[m].GetColor(BaseColorId);
                c.a *= 1f - tree.Fade;
                block.Clear();
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block, m);
            }
        }
    }

    private static void Restore(Tree tree)
    {
        if (!tree.Applied) return;
        tree.Applied = false;
        tree.Fade = 0f;
        for (var i = 0; i < tree.Renderers.Length; i++)
        {
            var r = tree.Renderers[i];
            if (!r) continue;
            r.SetPropertyBlock(null);
            if (!tree.Dither && tree.Originals[i] != null)
            {
                for (var m = 0; m < tree.Originals[i].Length; m++) r.SetPropertyBlock(null, m);
                r.sharedMaterials = tree.Originals[i];
            }
            r.shadowCastingMode = tree.Shadows[i];
        }
        if (tree.Dither) ShowTwins(tree, false);
    }

    private void RestoreAll()
    {
        foreach (var tree in trees)
        {
            tree.Until = 0f;
            Restore(tree);
        }
        anyFaded = false;
    }

    /// <summary>The dithering tree's shadow, cast by a shadow-only copy of its first level of detail while it fades.</summary>
    private static void ShowTwins(Tree tree, bool on)
    {
        if (on && tree.Twins.Length == 0)
        {
            var list = new List<Renderer>();
            var group = tree.Renderers.Length > 0 && tree.Renderers[0] ? tree.Renderers[0].GetComponentInParent<LODGroup>() : null;
            var first = group && group.lodCount > 0 ? group.GetLODs()[0].renderers : tree.Renderers;
            foreach (var r in first)
            {
                var filter = r ? r.GetComponent<MeshFilter>() : null;
                if (!filter || !filter.sharedMesh) continue;
                var go = new GameObject("Shadow While Faded") { hideFlags = HideFlags.DontSave };
                go.layer = r.gameObject.layer;
                go.transform.SetParent(r.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var twin = go.AddComponent<MeshRenderer>();
                twin.sharedMaterials = r.sharedMaterials;
                twin.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                twin.receiveShadows = false;
                list.Add(twin);
            }
            tree.Twins = list.ToArray();
        }
        foreach (var twin in tree.Twins) if (twin) twin.enabled = on;
    }

    /// <summary>A see-through copy of a URP Lit material (one per material, kept): alpha blended, still writing depth so the tree is not a jumble of its own leaves.</summary>
    private static Material SeeThrough(Material source)
    {
        if (!source) return source;
        if (seeThrough.TryGetValue(source, out var copy) && copy) return copy;
        copy = new Material(source) { name = source.name + " (fading)", hideFlags = HideFlags.DontSave };
        copy.SetFloat(SurfaceId, 1f);
        if (copy.HasProperty("_Blend")) copy.SetFloat("_Blend", 0f);
        if (copy.HasProperty("_SrcBlend")) copy.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (copy.HasProperty("_DstBlend")) copy.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (copy.HasProperty("_SrcBlendAlpha")) copy.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (copy.HasProperty("_DstBlendAlpha")) copy.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        if (copy.HasProperty("_ZWrite")) copy.SetFloat("_ZWrite", 1f);
        copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        copy.SetOverrideTag("RenderType", "Transparent");
        copy.renderQueue = (int)RenderQueue.Transparent;
        seeThrough[source] = copy;
        return copy;
    }

    /// <summary>
    ///     The dithering map for a level of the fade: the noise the shader compares with 1, raised by the level, so that
    ///     level / <see cref="Levels"/> of the pixels lie over 1 and are cut (half floats, sampled point by point).
    /// </summary>
    private static Texture2D Level(int level)
    {
        if (levels == null) levels = new Texture2D[Levels + 1];
        var tex = levels[level];
        if (tex) return tex;
        tex = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBAHalf, false, true)
        {
            name = "Tree Fade " + level,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.DontSave,
        };
        var pixels = new Color[NoiseSize * NoiseSize];
        var raise = level >= Levels ? 2f : level / (float)Levels;
        for (var y = 0; y < NoiseSize; y++)
        {
            for (var x = 0; x < NoiseSize; x++)
            {
                // interleaved gradient noise: even, without the grid of an ordered dither
                var noise = Mathf.Repeat(52.9829189f * Mathf.Repeat(0.06711056f * x + 0.00583715f * y, 1f), 1f);
                var v = noise * 0.999f + raise;
                pixels[y * NoiseSize + x] = new Color(v, v, v, 1f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        levels[level] = tex;
        return tex;
    }
}
