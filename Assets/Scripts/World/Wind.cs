using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Wind in the trees: trees, bushes and plants sway, each in its own time, with a slow gust rolling over the islands,
/// along the same wind the clouds drift on. On unless the setting turns it off; potato mode stills it
/// (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// The plants are drawn with <c>Ravenfall/Foliage</c>, which lights them as the Lit shader graph does and moves their
/// vertices. Their materials are shared with buildings (one palette texture for a whole kit), so the plants get copies
/// of them (<see cref="Foliage"/>, one per material) while the game runs: the world's trees through the resource
/// renderer, and the scene's own trees, bushes and plants, found by name, when play starts. Nothing in the scene or the
/// project is changed. Off, the shader's sway is zero; the cost is a few instructions a vertex.
/// </remarks>
public static class Wind
{
    private static readonly int WindId = Shader.PropertyToID("_RavenfallWind");
    private static readonly Regex PlantName = new Regex(@"SM_Env_.*(Tree|Bush|Fern|Palm|Plant|Hedge|Bamboo)", RegexOptions.IgnoreCase);
    private static readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    private static Shader shader;
    private static bool shaderLooked;

    /// <summary>The setting: the plants move in the wind.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Still for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>How hard the wind blows, 0 to 2.</summary>
    public static float Strength { get; set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        copies.Clear();
        RenderPipelineManager.beginContextRendering -= BeforeFrame;
        RenderPipelineManager.beginContextRendering += BeforeFrame;
        PlaySession.Ending += () =>
        {
            RenderPipelineManager.beginContextRendering -= BeforeFrame;
            Shader.SetGlobalVector(WindId, Vector4.zero);
        };
        SwapScenePlants();
    }

    private static void BeforeFrame(ScriptableRenderContext context, List<Camera> cameras)
    {
        var direction = SkyClouds.Wind.normalized;
        var strength = Enabled && !Suppressed ? Strength : 0f;
        Shader.SetGlobalVector(WindId, new Vector4(direction.x, direction.y, strength, 1f));
    }

    /// <summary>
    /// The swaying copy of a plant's material: the same texture, colour and shine, drawn with the foliage shader. The
    /// material itself when the shader is missing, or outside play.
    /// </summary>
    public static Material Foliage(Material material)
    {
        if (!material || !Application.isPlaying) return material;
        if (copies.TryGetValue(material, out var copy) && copy) return copy;
        if (!shaderLooked)
        {
            shaderLooked = true;
            shader = Resources.Load<Shader>("Foliage");
            if (shader && !shader.isSupported) shader = null;
        }
        // only what the Lit graph draws can be copied faithfully
        if (!shader || !material.shader || material.shader.name != "Shader Graphs/Lit") return material;

        copy = new Material(shader) { name = material.name + " (wind)" };
        if (material.HasProperty("_MainTex"))
        {
            copy.SetTexture("_MainTex", material.GetTexture("_MainTex"));
            copy.SetTextureScale("_MainTex", material.GetTextureScale("_MainTex"));
            copy.SetTextureOffset("_MainTex", material.GetTextureOffset("_MainTex"));
        }
        if (material.HasProperty("_Color")) copy.SetColor("_Color", material.GetColor("_Color"));
        if (material.HasProperty("_SpecColor")) copy.SetColor("_SpecColor", material.GetColor("_SpecColor"));
        if (material.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", material.GetFloat("_Smoothness"));
        copy.renderQueue = material.renderQueue;
        copies[material] = copy;
        return copy;
    }

    /// <summary>The scene's own trees, bushes and plants, found by their kit names, get the swaying copies.</summary>
    private static void SwapScenePlants()
    {
        if (!Application.isPlaying) return;
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
        {
            if (!PlantName.IsMatch(renderer.name)) continue;
            var materials = renderer.sharedMaterials;
            var changed = false;
            for (var i = 0; i < materials.Length; ++i)
            {
                var foliage = Foliage(materials[i]);
                if (foliage == materials[i]) continue;
                materials[i] = foliage;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }
}
