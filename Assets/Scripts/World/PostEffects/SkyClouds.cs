using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Volumetric clouds over the islands: a slab of cumulus, marched per pixel and lit by the sun through its own bulk,
/// drifting with the same wind as <see cref="CloudShadows"/>, with stars twinkling between them at night. On unless the
/// setting turns it off; potato mode turns it off as well (<see cref="Suppressed"/>). While it is off, the scene's
/// low-poly cloud meshes show instead.
/// </summary>
/// <remarks>
/// The pass is added to the game camera each frame from here, so no renderer asset carries it. It runs before URP's
/// own post processing, after the sea. The march is at half the screen's size; a ray that never reaches the slab (the
/// usual view, looking down at the islands from under it) ends at once, so it costs most when the camera looks at the
/// sky. Ported from FF3Unity's clouds.
/// </remarks>
public static class SkyClouds
{
    private static readonly int SlabId = Shader.PropertyToID("_CloudSlab");
    private static readonly int ParamsId = Shader.PropertyToID("_CloudParams");
    private static readonly int WindId = Shader.PropertyToID("_CloudWind");
    private static readonly int SunDirId = Shader.PropertyToID("_CloudSunDir");
    private static readonly int SunColorId = Shader.PropertyToID("_CloudSunColor");
    private static readonly int SkyColorId = Shader.PropertyToID("_CloudSkyColor");
    private static readonly int ShadeColorId = Shader.PropertyToID("_CloudShadeColor");
    private static readonly int HazeColorId = Shader.PropertyToID("_CloudHazeColor");
    private static readonly int LookId = Shader.PropertyToID("_CloudLook");
    private static readonly int BufferId = Shader.PropertyToID("_SkyCloudBuffer");
    private static readonly int TexelId = Shader.PropertyToID("_SkyCloudTexel");
    private static readonly int NightId = Shader.PropertyToID("_CloudNight");
    private static DayNightCycle dayNight;

    /// <summary>The cloud layer's floor and ceiling, metres; where the scene's own cloud meshes float.</summary>
    public const float Floor = 230f, Ceiling = 480f;

    /// <summary>How big a puff is, metres.</summary>
    public const float PuffSize = 340f;

    /// <summary>How much of the sky is cloud, 0 to 1.</summary>
    public static float Coverage = 0.5f;

    /// <summary>The wind, metres a second over the ground; the cloud shadows drift with it too.</summary>
    public static readonly Vector2 Wind = new Vector2(1.6f, 0.7f);

    private static readonly int ShadowLightToWorldId = Shader.PropertyToID("_CloudShadowLightToWorld");
    private static readonly int ShadowWindowId = Shader.PropertyToID("_CloudShadowWindow");
    private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");

    // the clouds' shadow on the sun: a window round the game camera, in the light's space
    private const int ShadowSize = 256;
    private const float ShadowWindow = 3000f;
    private const int ShadowEvery = 4;   // frames between redraws; the clouds move a few metres a second

    private static Material material;
    private static Pass pass;
    private static RenderTexture shadowMap;
    private static CommandBuffer shadowCommands;
    private static int shadowDrawnAt = -1000;
    private static Vector2 shadowCentre;
    private static readonly List<Renderer> meshClouds = new List<Renderer>();
    private static bool meshCloudsShown = true;
    private static bool meshCloudsFound;

    /// <summary>The setting: volumetric clouds on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>The clouds are drawn: the setting is on and nothing holds them back.</summary>
    public static bool Active => Enabled && !Suppressed;

    /// <summary>The cloud shadows' texture while the clouds draw it, for <see cref="CloudShadows"/> to take off the sun.</summary>
    public static Texture ShadowMap => shadowMap;

    private static bool EnsureMaterial()
    {
        if (material) return true;
        var shader = Resources.Load<Shader>("SkyClouds");
        if (!shader || !shader.isSupported) return false;
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }

    /// <summary>
    /// Puts the clouds' own shadows on the sun as its cookie: a window round the game camera, drawn every few frames
    /// from the same cloud as the sky shows, so the shadow on the ground is the shadow of the cloud overhead. False
    /// (and nothing done) while the clouds are off, when <see cref="CloudShadows"/> draws its own.
    /// </summary>
    public static bool DrawShadows(ScriptableRenderContext context, Light sun, UniversalAdditionalLightData sunData, float strength)
    {
        if (!Active || !sun || sunData == null || !EnsureMaterial()) return false;
        if (!shadowMap)
        {
            shadowMap = new RenderTexture(ShadowSize, ShadowSize, GraphicsFormat.R8_UNorm, GraphicsFormat.None)
            {
                name = "Cloud Shadows (from the clouds)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            shadowMap.Create();
            shadowDrawnAt = -1000;
        }
        shadowCommands ??= new CommandBuffer { name = "Cloud Shadows" };

        if (Time.frameCount - shadowDrawnAt >= ShadowEvery)
        {
            shadowDrawnAt = Time.frameCount;
            var lightToWorld = sun.transform.localToWorldMatrix;
            var focus = Camera.main ? Camera.main.transform.position : Vector3.zero;
            var centre = lightToWorld.inverse.MultiplyPoint3x4(focus);
            // on whole texels, so the shadows hold still under a moving camera
            var texel = ShadowWindow / ShadowSize;
            shadowCentre = new Vector2(Mathf.Round(centre.x / texel) * texel, Mathf.Round(centre.y / texel) * texel);

            Set(material);
            material.SetMatrix(ShadowLightToWorldId, lightToWorld);
            material.SetVector(ShadowWindowId, new Vector4(shadowCentre.x, shadowCentre.y, ShadowWindow, strength));
            shadowCommands.Clear();
            shadowCommands.SetRenderTarget(shadowMap);
            shadowCommands.SetGlobalVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
            shadowCommands.DrawProcedural(Matrix4x4.identity, material, 2, MeshTopology.Triangles, 3);
            context.ExecuteCommandBuffer(shadowCommands);
        }

        if (sun.cookie != shadowMap) sun.cookie = shadowMap;
        sunData.lightCookieSize = new Vector2(ShadowWindow, ShadowWindow);
        sunData.lightCookieOffset = shadowCentre;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        meshClouds.Clear();
        meshCloudsFound = false;
        meshCloudsShown = true;
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        PlaySession.Ending += () =>
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            ShowMeshClouds(true);
            meshClouds.Clear();
            meshCloudsFound = false;
        };
    }

    private static void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        var on = Enabled && !Suppressed;
        ShowMeshClouds(!on);
        if (!on || camera.cameraType != CameraType.Game || camera.orthographic) return;
        if (!EnsureMaterial()) return;
        Set(material);

        var data = camera.GetUniversalAdditionalCameraData();
        if (data == null || data.scriptableRenderer == null) return;
        pass ??= new Pass();
        pass.material = material;
        data.scriptableRenderer.EnqueuePass(pass);
    }

    /// <summary>
    /// The scene's low-poly clouds (meshes drawn with the Cloud shader graph) show only while these are off, as the
    /// cheap stand-in.
    /// </summary>
    private static void ShowMeshClouds(bool show)
    {
        if (!meshCloudsFound)
        {
            meshCloudsFound = true;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var m = r.sharedMaterial;
                if (m && m.shader && m.shader.name == "Shader Graphs/Cloud") meshClouds.Add(r);
            }
        }
        if (show == meshCloudsShown) return;
        meshCloudsShown = show;
        foreach (var r in meshClouds) if (r) r.enabled = show;
    }

    /// <summary>How much it is night, 0 by day to 1 in the dark, from the cycle's light: how much the stars show.</summary>
    private static float Night()
    {
        if (!dayNight) dayNight = Object.FindAnyObjectByType<DayNightCycle>();
        var sun = RenderSettings.sun;
        if (!dayNight || !dayNight.DayLight || !dayNight.NightLight || !sun) return 0f;
        var day = Mathf.InverseLerp(dayNight.NightLight.intensity, dayNight.DayLight.intensity, sun.intensity);
        var t = Mathf.Clamp01((day - 0.1f) / 0.35f);
        return 1f - t * t * (3f - 2f * t);
    }

    private static void Set(Material m)
    {
        m.SetVector(SlabId, new Vector4(Floor, Ceiling, Coverage, PuffSize));
        m.SetFloat(NightId, Night());
        m.SetVector(ParamsId, new Vector4(0.85f, 24f, 1f, 0.6f));
        var t = Time.time;
        // a little haze, so the far cloud melts into the horizon
        m.SetVector(WindId, new Vector4(Wind.x * t, Wind.y * t, 0.00022f, 600f));
        m.SetVector(LookId, new Vector4(1f, 1f, 0.45f, 0f));

        var sun = RenderSettings.sun;
        if (sun && sun.isActiveAndEnabled)
        {
            var colour = sun.color.linear;
            if (sun.useColorTemperature) colour *= Mathf.CorrelatedColorTemperatureToRGB(sun.colorTemperature).linear;
            m.SetVector(SunDirId, -sun.transform.forward);
            m.SetVector(SunColorId, colour * Mathf.Clamp(sun.intensity, 0f, 6f) * 0.75f);
        }
        else
        {
            m.SetVector(SunDirId, Vector3.up);
            m.SetVector(SunColorId, Vector4.zero);
        }

        // the sky's own colour on the tops, and in the shade the same, deeper: a cloud's belly is lit by the sky round
        // it, so it stays blue-grey rather than going muddy
        var tri = RenderSettings.ambientMode == AmbientMode.Trilight;
        var sky = tri ? RenderSettings.ambientSkyColor.linear : RenderSettings.ambientLight.linear;
        var equator = tri ? RenderSettings.ambientEquatorColor.linear : RenderSettings.ambientLight.linear;
        m.SetVector(SkyColorId, Color.Lerp(sky, equator, 0.3f) * 0.85f);
        m.SetVector(ShadeColorId, Color.Lerp(sky, equator, 0.5f) * 0.4f);
        m.SetVector(HazeColorId, Color.Lerp(sky, equator, 0.7f) * 0.95f);
    }

    private sealed class Pass : ScriptableRenderPass
    {
        public Material material;

        private sealed class CompositeData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle clouds;
            public Vector4 texel;
        }

        public Pass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer || !material) return;

            var source = resources.activeColorTexture;
            var full = renderGraph.GetTextureDesc(source);
            full.depthBufferBits = DepthBits.None;
            full.msaaSamples = MSAASamples.None;
            full.clearBuffer = false;

            // the march, at half size, into colour and transmittance
            var half = full;
            half.width = Mathf.Max(1, full.width / 2);
            half.height = Mathf.Max(1, full.height / 2);
            half.format = GraphicsFormat.R16G16B16A16_SFloat;
            half.name = "Sky Clouds March";
            var buffer = renderGraph.CreateTexture(half);
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, buffer, material, 0), "Sky Clouds March");

            full.name = "Sky Clouds";
            var target = renderGraph.CreateTexture(full);
            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("Sky Clouds Composite", out var data))
            {
                data.material = material;
                data.source = source;
                data.clouds = buffer;
                data.texel = new Vector4(1f / half.width, 1f / half.height, 0f, 0f);
                builder.UseTexture(source);
                builder.UseTexture(buffer);
                builder.SetRenderAttachment(target, 0);
                builder.SetRenderFunc((CompositeData d, RasterGraphContext context) =>
                {
                    RTHandle clouds = d.clouds;
                    d.material.SetTexture(BufferId, clouds);
                    d.material.SetVector(TexelId, d.texel);
                    Blitter.BlitTexture(context.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, 1);
                });
            }
            resources.cameraColor = target;
        }
    }
}
