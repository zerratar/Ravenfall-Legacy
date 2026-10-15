using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Atmospheric haze: the far islands sit in the air, fading toward the sky's colour with distance, more low over the
/// sea than up high, and brighter toward the sun. Near things stay crisp. On unless the setting turns it off; potato
/// mode turns it off as well (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// One fullscreen pass reading the depth, added to the game camera each frame from here, before the clouds (which
/// carry their own haze) and URP's post processing. Its colour is the sky's ambient now, so the haze darkens at
/// night. Not drawn down in a dungeon. Ported from FF3Unity's atmosphere.
/// </remarks>
public static class Haze
{
    private static readonly int ParamsId = Shader.PropertyToID("_HazeParams");
    private static readonly int SunId = Shader.PropertyToID("_HazeSun");
    private static readonly int ColorId = Shader.PropertyToID("_HazeColor");
    private static readonly int SunColorId = Shader.PropertyToID("_HazeSunColor");
    private static readonly int SunDirId = Shader.PropertyToID("_HazeSunDir");

    /// <summary>Optical depth per metre over the sea; about a third of a far island at 700 m is haze.</summary>
    public static float Density = 0.0006f;
    /// <summary>Metres of height over which the haze thins to a third.</summary>
    public static float HeightScale = 120f;
    /// <summary>The height it thins from: the sea.</summary>
    public static float BaseHeight = -3.3f;
    /// <summary>Metres before the haze begins.</summary>
    public static float StartDistance = 60f;

    private static Material material;
    private static Pass pass;

    /// <summary>The setting: atmospheric haze on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        PlaySession.Ending += () => RenderPipelineManager.beginCameraRendering -= BeforeCamera;
    }

    private static void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!Enabled || Suppressed || camera.cameraType != CameraType.Game || camera.orthographic) return;
        // down in a dungeon there is no far
        if (camera.transform.position.y < -30f) return;
        if (!material)
        {
            var shader = Resources.Load<Shader>("Haze");
            if (!shader || !shader.isSupported) return;
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        Set(material);

        var data = camera.GetUniversalAdditionalCameraData();
        if (data == null || data.scriptableRenderer == null) return;
        pass ??= new Pass();
        pass.material = material;
        data.scriptableRenderer.EnqueuePass(pass);
    }

    private static void Set(Material m)
    {
        m.SetVector(ParamsId, new Vector4(Density * Weather.HazeAmount, 1f / Mathf.Max(HeightScale, 1f), BaseHeight, StartDistance));
        m.SetVector(SunId, new Vector4(0.35f, 6f, 0.45f, 0.12f));

        // the sky's own colour, between its top and its horizon
        var tri = RenderSettings.ambientMode == AmbientMode.Trilight;
        var sky = tri ? RenderSettings.ambientSkyColor.linear : RenderSettings.ambientLight.linear;
        var equator = tri ? RenderSettings.ambientEquatorColor.linear : RenderSettings.ambientLight.linear;
        m.SetVector(ColorId, Color.Lerp(sky, equator, 0.55f) * 0.95f);

        var sun = RenderSettings.sun;
        if (sun && sun.isActiveAndEnabled)
        {
            var colour = sun.color.linear;
            if (sun.useColorTemperature) colour *= Mathf.CorrelatedColorTemperatureToRGB(sun.colorTemperature).linear;
            m.SetVector(SunDirId, -sun.transform.forward);
            m.SetVector(SunColorId, colour * Mathf.Clamp(sun.intensity, 0f, 3f) * 0.4f);
        }
        else
        {
            m.SetVector(SunDirId, Vector3.up);
            m.SetVector(SunColorId, Vector4.zero);
        }
    }

    private sealed class Pass : ScriptableRenderPass
    {
        public Material material;

        public Pass()
        {
            // before the clouds, which lay their own hazed colour over this
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing - 1;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer || !material) return;
            var source = resources.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "Haze";
            desc.clearBuffer = false;
            desc.depthBufferBits = DepthBits.None;
            desc.msaaSamples = MSAASamples.None;
            var target = renderGraph.CreateTexture(desc);
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, target, material, 0), "Haze");
            resources.cameraColor = target;
        }
    }
}
