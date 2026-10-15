using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The sky's reflection, kept up to date: a reflection probe that draws only the skybox, 64 pixels a side, every
/// couple of seconds, covering the whole world. Shiny things (the sea, armour, wet rock) then reflect the sky as it
/// is now, the night's sky at night, where the scene's own reflection is whatever the sky was when it was saved.
/// The sea reads it directly as <c>_RavenfallSky</c>.
/// </summary>
/// <remarks>Made in play only, by itself; nothing in the scene. Six skybox draws at 64 x 64 every two seconds.</remarks>
public sealed class SkyReflection : MonoBehaviour
{
    private static readonly int SkyId = Shader.PropertyToID("_RavenfallSky");
    private static readonly int HasSkyId = Shader.PropertyToID("_RavenfallHasSky");

    private const float Interval = 2f;

    private ReflectionProbe probe;
    private float next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<SkyReflection>()) return;
        var go = new GameObject("Sky Reflection") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        go.AddComponent<SkyReflection>();
    }

    private void Awake()
    {
        // realtime probes are off in the quality settings; this is the only one there is
        QualitySettings.realtimeReflectionProbes = true;
        probe = gameObject.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.resolution = 64;
        probe.hdr = true;
        probe.cullingMask = 0;
        probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        probe.boxProjection = false;
        probe.importance = 0;
        probe.size = new Vector3(100000f, 100000f, 100000f);
        probe.nearClipPlane = 0.1f;
        probe.farClipPlane = 10f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= next)
        {
            next = Time.unscaledTime + Interval;
            probe.RenderProbe();
        }
        var sky = probe.realtimeTexture;
        if (sky) Shader.SetGlobalTexture(SkyId, sky);
        Shader.SetGlobalFloat(HasSkyId, sky ? 1f : 0f);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(HasSkyId, 0f);
    }
}
