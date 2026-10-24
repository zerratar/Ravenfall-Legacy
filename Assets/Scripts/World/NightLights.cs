using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The world's torches and braziers by night: after dark they reach further and flicker, growing in with the night,
/// in a warm but natural firelight. By day they are as the scene set them.
/// </summary>
/// <remarks>
/// They are the scene's own point lights on torches and braziers outside the dungeons (which DungeonMood lights),
/// set with a 10 m reach: from the game camera that was a small pool round each torch with the dark in between.
/// Many are set very orange (about 1700 K), which by night made a hot red pool round each fire (found on stream,
/// October 2026), so after dark they go no warmer than <see cref="TorchTemperature"/> and reach wider instead of
/// burning brighter. No new lights; each gets back what it had when this stops.
/// </remarks>
public sealed class NightLights : MonoBehaviour
{
    // the torches by night: further, flickering, no redder than this
    private const float TorchReach = 2.2f;
    private const float TorchBoost = 1f;
    private const float TorchTemperature = 2600f;
    // new torches (a town hall tier, build mode) are picked up this often
    private const float TorchScanEvery = 10f;

    private DayNightCycle dayNight;
    private readonly List<(Light light, float intensity, float range, float temperature, float seed)> torches = new List<(Light, float, float, float, float)>();
    private readonly HashSet<Light> known = new HashSet<Light>();
    private float nextTorchScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying || FindAnyObjectByType<NightLights>()) return;
        var go = new GameObject("Night Lights") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<NightLights>();
    }

    private void LateUpdate()
    {
        if (!dayNight) dayNight = FindAnyObjectByType<DayNightCycle>();
        if (Time.unscaledTime >= nextTorchScan)
        {
            nextTorchScan = Time.unscaledTime + TorchScanEvery;
            FindTorches();
        }

        var night = Night();
        var time = Time.time;
        foreach (var (light, intensity, range, temperature, seed) in torches)
        {
            if (!light) continue;
            light.range = range * Mathf.Lerp(1f, TorchReach, night);
            light.colorTemperature = Mathf.Lerp(temperature, Mathf.Max(temperature, TorchTemperature), night);
            var flicker = 1f - night * (0.1f - 0.06f * Mathf.Sin(time * 8.3f + seed) - 0.04f * Mathf.PerlinNoise(time * 3.7f, seed));
            light.intensity = intensity * Mathf.Lerp(1f, TorchBoost, night) * flicker;
        }
    }

    /// <summary>How much it is night, 0 by day to 1 in the dark, from the cycle's own daylight.</summary>
    private float Night()
    {
        if (!dayNight) return 0f;
        // dark below 15 % of the day's light, none above 55 % (as the fireflies)
        var t = Mathf.Clamp01((dayNight.Daylight - 0.15f) / 0.4f);
        return 1f - t * t * (3f - 2f * t);
    }

    /// <summary>The point lights on torches and braziers outside the dungeons, remembering how each was set.</summary>
    private void FindTorches()
    {
        torches.RemoveAll(x => !x.light);
        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (light.type != LightType.Point || known.Contains(light)) continue;
            if (!OnTorch(light.transform) || light.GetComponentInParent<DungeonController>(true)) continue;
            known.Add(light);
            torches.Add((light, light.intensity, light.range, light.colorTemperature, Random.value * 100f));
        }
    }

    private static bool OnTorch(Transform t)
    {
        for (var i = 0; t && i < 3; ++i, t = t.parent)
        {
            var name = t.name;
            if (name.IndexOf("Torch", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Brazier", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        foreach (var (light, intensity, range, temperature, _) in torches)
        {
            if (!light) continue;
            light.intensity = intensity;
            light.range = range;
            light.colorTemperature = temperature;
        }
        torches.Clear();
        known.Clear();
    }
}
