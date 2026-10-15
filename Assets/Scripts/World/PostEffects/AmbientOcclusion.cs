using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ambient occlusion as a setting of its own, on every quality level: the soft darkening where things meet the ground
/// and in corners, which gives the low-poly world its depth. Potato mode turns it off (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// <para>URP draws it with a renderer feature but takes its look from a volume override. The High renderer carried the
/// feature, but no volume had the override, so it ran at URP's defaults (a 0.3 m reach), too small to see from the
/// game's camera. Here a global volume made at runtime gives it the look (1.2 m; at 2.5 m it shaded whole walls nearly black under the eaves), and the Medium and Low renderers,
/// which had no feature, get one while the game runs; below High it works at half resolution.</para>
/// <para>Nothing is written to the renderer assets: in the editor, whatever was added or switched is put back when play
/// stops.</para>
/// </remarks>
public static class AmbientOcclusion
{
    private const float Radius = 1.2f;
    // the strength setting's 0 to 1 is an intensity of 0 to 2; direct sunlight is hardly darkened, so a lit wall or
    // the ground in the sun never goes black
    private const float MaxIntensity = 2f;
    private const float DirectLighting = 0.12f;
    private const float Falloff = 400f;

    private static readonly FieldInfo RendererDataList = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo DefaultRendererIndex = typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly List<(ScriptableRendererData data, ScriptableRendererFeature feature)> added = new List<(ScriptableRendererData, ScriptableRendererFeature)>();
    private static readonly Dictionary<ScriptableRendererFeature, bool> wasActive = new Dictionary<ScriptableRendererFeature, bool>();
    private static RenderPipelineAsset applied;
    private static bool appliedOn;
    private static Volume volume;
    private static ScreenSpaceAmbientOcclusionVolumeOverride look;

    /// <summary>The setting: ambient occlusion on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>The setting: how strong the shading is, 0 to 1.</summary>
    public static float Strength { get; set; } = 0.4f;
    private static float appliedStrength = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        applied = null;
        RenderPipelineManager.beginContextRendering -= BeforeFrame;
        RenderPipelineManager.beginContextRendering += BeforeFrame;
        PlaySession.Ending += () =>
        {
            RenderPipelineManager.beginContextRendering -= BeforeFrame;
            Restore();
        };
    }

    private static void BeforeFrame(ScriptableRenderContext context, List<Camera> cameras)
    {
        var asset = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
        var on = Enabled && !Suppressed;
        if (asset == applied && on == appliedOn && volume && Strength == appliedStrength) return;
        applied = asset;
        appliedOn = on;
        appliedStrength = Strength;

        EnsureVolume();
        look.intensity = Mathf.Clamp01(Strength) * MaxIntensity;
        // half resolution below the highest level
        look.downsample = QualitySettings.GetQualityLevel() < QualitySettings.count - 1;
        volume.enabled = on;

        if (asset is not UniversalRenderPipelineAsset urp || RendererDataList == null) return;
        var list = RendererDataList.GetValue(urp) as ScriptableRendererData[];
        var index = DefaultRendererIndex != null ? (int)DefaultRendererIndex.GetValue(urp) : 0;
        if (list == null || list.Length == 0) return;
        var data = list[Mathf.Clamp(index, 0, list.Length - 1)];
        if (!data) return;

        var found = false;
        foreach (var feature in data.rendererFeatures)
        {
            if (feature is not ScreenSpaceAmbientOcclusion) continue;
            found = true;
            if (!wasActive.ContainsKey(feature)) wasActive[feature] = feature.isActive;
            feature.SetActive(on);
        }
        if (!found && on) Add(data);
    }

    /// <summary>The global volume with the look, made once; not saved and not in the hierarchy.</summary>
    private static void EnsureVolume()
    {
        if (volume) return;
        var go = new GameObject("Ambient Occlusion Volume") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        look = profile.Add<ScreenSpaceAmbientOcclusionVolumeOverride>(false);
        foreach (var parameter in look.parameters) parameter.overrideState = false;
        Override("m_Intensity");
        Override("m_Radius");
        Override("m_FalloffDistance");
        Override("m_DirectLightingStrength");
        Override("m_Downsample");
        look.radius = Radius;
        look.falloffDistance = Falloff;
        look.directLightingStrength = DirectLighting;
        volume.sharedProfile = profile;
    }

    private static void Override(string field)
    {
        var info = typeof(ScreenSpaceAmbientOcclusionVolumeOverride).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        if (info?.GetValue(look) is VolumeParameter parameter) parameter.overrideState = true;
    }

    private static void Add(ScriptableRendererData data)
    {
        var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
        feature.name = "Ambient Occlusion (added while playing)";
        feature.hideFlags = HideFlags.DontSave;
        feature.Create();
        data.rendererFeatures.Add(feature);
        data.SetDirty();
        added.Add((data, feature));
    }

    /// <summary>Takes away what was added and puts the switched features back as they were.</summary>
    private static void Restore()
    {
        foreach (var (data, feature) in added)
        {
            if (data)
            {
                data.rendererFeatures.Remove(feature);
                data.SetDirty();
            }
            if (feature) Object.DestroyImmediate(feature);
        }
        added.Clear();
        foreach (var pair in wasActive) if (pair.Key) pair.Key.SetActive(pair.Value);
        wasActive.Clear();
        if (volume)
        {
            if (volume.sharedProfile) Object.DestroyImmediate(volume.sharedProfile);
            Object.DestroyImmediate(volume.gameObject);
        }
        volume = null;
        look = null;
        applied = null;
    }
}
