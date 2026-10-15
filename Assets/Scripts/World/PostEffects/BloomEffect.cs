using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Bloom as a setting of its own, on by default: what is brighter than white glows softly into what is round it, the
/// fires and lit windows after dark, lightning, the sun glinting off the sea. Potato mode turns it off
/// (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// A global volume made at runtime, above the scene's post processing volume, so its bloom is the one used whether that
/// volume is on or not; off, it holds bloom at nothing. With HDR (the High quality level) the threshold is about white, so
/// a sunlit day is left as it is and the fires, lit windows and lightning glow; without HDR (Medium) only the whitest glow,
/// gently; the Low renderer draws no post effects at all.
/// </remarks>
public static class BloomEffect
{
    // with HDR (High) what is past white glows; without it (Medium) nothing is, so only the whitest glow, and less
    private const float Threshold = 0.95f, Intensity = 0.85f;
    private const float ThresholdWithoutHdr = 1f, IntensityWithoutHdr = 0.4f;
    private const float Scatter = 0.7f;

    private static Volume volume;
    private static Bloom look;
    private static bool appliedOn;
    private static RenderPipelineAsset appliedAsset;

    /// <summary>The setting: bloom on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        RenderPipelineManager.beginContextRendering -= BeforeFrame;
        RenderPipelineManager.beginContextRendering += BeforeFrame;
        PlaySession.Ending += () =>
        {
            RenderPipelineManager.beginContextRendering -= BeforeFrame;
            if (volume)
            {
                if (volume.sharedProfile) Object.DestroyImmediate(volume.sharedProfile);
                Object.DestroyImmediate(volume.gameObject);
            }
            volume = null;
            look = null;
        };
    }

    private static void BeforeFrame(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
    {
        var on = Enabled && !Suppressed;
        var asset = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
        if (volume && on == appliedOn && asset == appliedAsset) return;
        appliedOn = on;
        appliedAsset = asset;
        if (!volume) Make();
        var hdr = asset is UniversalRenderPipelineAsset urp && urp.supportsHDR;
        look.threshold.value = hdr ? Threshold : ThresholdWithoutHdr;
        look.intensity.value = on ? (hdr ? Intensity : IntensityWithoutHdr) : 0f;
    }

    /// <summary>The global volume with the look, made once; not saved and not in the hierarchy.</summary>
    private static void Make()
    {
        var go = new GameObject("Bloom Volume") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 60f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        look = profile.Add<Bloom>(false);
        look.threshold.Override(Threshold);
        look.intensity.Override(Intensity);
        look.scatter.Override(Scatter);
        volume.sharedProfile = profile;
    }
}
