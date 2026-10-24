using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tilt shift, to make the islands look like a miniature world: a band across the screen stays sharp and the picture
/// blurs above and below it. Off unless the setting turns it on.
/// </summary>
/// <remarks>
/// The pass is added to the game camera each frame from here, so no renderer asset has to carry it. It runs before
/// URP's own post processing (bloom, colour grading), at half and quarter resolution for the blur: a handful of small
/// passes. Only the main camera gets it; potato mode keeps it off (<see cref="Suppressed"/>). The game camera sets
/// <see cref="FocusPoint"/> to the observed player, so the band follows them.
/// </remarks>
public static class TiltShift
{
    private static readonly int FocusId = Shader.PropertyToID("_TiltFocus");
    private static readonly int BandId = Shader.PropertyToID("_TiltBand");
    private static readonly int FalloffId = Shader.PropertyToID("_TiltFalloff");
    private static readonly int StrengthId = Shader.PropertyToID("_TiltStrength");
    private static readonly int HalfId = Shader.PropertyToID("_TiltHalf");
    private static readonly int QuarterId = Shader.PropertyToID("_TiltQuarter");

    private static Material material;
    private static Pass pass;
    private static float strength = 0.5f;

    /// <summary>The setting: tilt shift on or off.</summary>
    public static bool Enabled { get; set; }

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>
    /// How strong, 0 to 1: how far the blur goes at the screen's edges.
    /// </summary>
    public static float Strength
    {
        get => strength;
        set => strength = Mathf.Clamp01(value);
    }

    /// <summary>How tall the sharp band is, 0 (a thin strip) to 1 (much of the screen).</summary>
    public static float Band { get; set; } = 0.35f;

    /// <summary>How gently the picture blurs away from the band, 0 (a quick, hard change) to 1 (a long, soft one).</summary>
    public static float Softness { get; set; } = 0.5f;

    /// <summary>Where the sharp band rests when there is nothing to follow: 0 the bottom of the screen, 1 the top.</summary>
    public static float Focus { get; set; } = 0.47f;

    /// <summary>
    /// A point in the world to keep sharp, the observed player, or null for none: the band follows it up and down
    /// the screen, easing so it never jumps.
    /// </summary>
    public static Vector3? FocusPoint { get; set; }

    // where the band is now, easing towards the focus point
    private static float band = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        PlaySession.Ending += () => RenderPipelineManager.beginCameraRendering -= BeforeCamera;
    }

    private static void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!Enabled || Suppressed || camera.cameraType != CameraType.Game || camera != Camera.main) return;
        if (!material)
        {
            var shader = Resources.Load<Shader>("TiltShift");
            if (!shader || !shader.isSupported) return;
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        // the band follows the focus point, kept off the very edges, easing over about a quarter of a second
        var aim = Focus;
        if (FocusPoint.HasValue)
        {
            var onScreen = camera.WorldToViewportPoint(FocusPoint.Value);
            if (onScreen.z > 0f) aim = Mathf.Clamp(onScreen.y, 0.2f, 0.8f);
        }
        band = band < 0f ? aim : Mathf.Lerp(band, aim, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));

        // the band's height and how softly it blurs away from it are settings of their own; the strength is how far
        // the blur goes at the edges
        material.SetFloat(FocusId, band);
        material.SetFloat(BandId, Mathf.Lerp(0.04f, 0.35f, Mathf.Clamp01(Band)));
        material.SetFloat(FalloffId, Mathf.Lerp(0.15f, 0.9f, Mathf.Clamp01(Softness)));
        material.SetFloat(StrengthId, Mathf.Lerp(0.25f, 1f, strength));

        var data = camera.GetUniversalAdditionalCameraData();
        if (data == null || data.scriptableRenderer == null) return;
        pass ??= new Pass();
        pass.material = material;
        data.scriptableRenderer.EnqueuePass(pass);
    }

    private sealed class Pass : ScriptableRenderPass
    {
        public Material material;

        private sealed class CompositeData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle half;
            public TextureHandle quarter;
        }

        public Pass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
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

            TextureHandle Make(int divide, string name)
            {
                var desc = full;
                desc.width = Mathf.Max(1, full.width / divide);
                desc.height = Mathf.Max(1, full.height / divide);
                desc.name = name;
                return renderGraph.CreateTexture(desc);
            }

            // half size and quarter size, each blurred across and down
            var half = Make(2, "Tilt Shift Half");
            var halfBlur = Make(2, "Tilt Shift Half Blur");
            var quarter = Make(4, "Tilt Shift Quarter");
            var quarterBlur = Make(4, "Tilt Shift Quarter Blur");
            renderGraph.AddBlitPass(source, half, Vector2.one, Vector2.zero, passName: "Tilt Shift Half");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(half, halfBlur, material, 0), "Tilt Shift Half Across");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(halfBlur, half, material, 1), "Tilt Shift Half Down");
            renderGraph.AddBlitPass(half, quarter, Vector2.one, Vector2.zero, passName: "Tilt Shift Quarter");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(quarter, quarterBlur, material, 0), "Tilt Shift Quarter Across");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(quarterBlur, quarter, material, 1), "Tilt Shift Quarter Down");

            // sharp in the band, blurring towards the top and bottom, into a new picture that the camera then carries on with
            full.name = "Tilt Shift";
            var target = renderGraph.CreateTexture(full);
            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("Tilt Shift Composite", out var data))
            {
                data.material = material;
                data.source = source;
                data.half = half;
                data.quarter = quarter;
                builder.UseTexture(source);
                builder.UseTexture(half);
                builder.UseTexture(quarter);
                builder.SetRenderAttachment(target, 0);
                builder.SetRenderFunc((CompositeData d, RasterGraphContext context) =>
                {
                    RTHandle halfTexture = d.half;
                    RTHandle quarterTexture = d.quarter;
                    d.material.SetTexture(HalfId, halfTexture);
                    d.material.SetTexture(QuarterId, quarterTexture);
                    Blitter.BlitTexture(context.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, 2);
                });
            }
            resources.cameraColor = target;
        }
    }
}
