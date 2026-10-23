using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dungeons by torchlight: while the camera is down in a running dungeon, the daylight fades to a dim cellar ambient,
/// the rooms' own lights come on (they were all switched off) and their flames flicker. On unless the setting turns
/// it off; potato mode leaves the dungeon in plain daylight (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// It works after the day and night cycle has set the light each frame, easing over about a second, and puts the
/// rooms' lights back as they were when the dungeon ends. About twenty small point lights, unshadowed but one, only
/// while a dungeon is watched.
/// </remarks>
public sealed class DungeonMood : MonoBehaviour
{
    /// <summary>The setting: dungeons by torchlight.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Plain daylight for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>A dungeon to show by torchlight whether or not one runs, for tools and tests; null for the usual.</summary>
    public static DungeonController Preview { get; set; }

    // the camera is down in the dungeon below this height
    private const float Depth = -30f;

    private static readonly Color SkyAmbient = new Color(0.17f, 0.17f, 0.22f);
    private static readonly Color EquatorAmbient = new Color(0.19f, 0.16f, 0.15f);
    private static readonly Color GroundAmbient = new Color(0.09f, 0.08f, 0.07f);
    // what is left of the sky's light down there, the same by day and by night (it used to be a share of the sky's
    // light, so at night, with a dim moon, the dungeons went near black), in a plain colour, not the moon's blue
    private const float DungeonSun = 0.38f;
    private const float DungeonSunTemperature = 5500f;
    // the rooms' lights were set for daylight; by torchlight they burn brighter and reach further
    private const float TorchBoost = 2.6f;
    private const float TorchReach = 1.7f;

    private GameManager game;
    private DungeonController lit;
    private readonly List<(GameObject group, bool wasActive)> groups = new List<(GameObject, bool)>();
    private readonly List<(Light light, float intensity, float range, float seed)> lights = new List<(Light, float, float, float)>();
    private float blend;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("Dungeon Mood") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<DungeonMood>();
    }

    private void LateUpdate()
    {
        if (!game) game = FindAnyObjectByType<GameManager>();
        var dungeons = game ? game.Dungeons : null;
        var camera = Camera.main;
        var dungeon = Preview ? Preview : dungeons ? dungeons.Dungeon : null;
        var down = Preview || (dungeons && dungeons.Started && dungeon && camera && camera.transform.position.y < Depth);
        var target = down && Enabled && !Suppressed ? 1f : 0f;
        blend = Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime * 1.2f);

        if (blend > 0f && dungeon && lit != dungeon) LightUp(dungeon);
        if (blend <= 0f && lit) PutOut();
        if (blend <= 0f) return;

        // the cycle has set this frame's light; it fades toward the cellar's
        var t = blend * blend * (3f - 2f * blend);
        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, SkyAmbient, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, EquatorAmbient, t);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, GroundAmbient, t);
        var sun = RenderSettings.sun;
        if (sun)
        {
            sun.intensity = Mathf.Lerp(sun.intensity, DungeonSun, t);
            sun.colorTemperature = Mathf.Lerp(sun.colorTemperature, DungeonSunTemperature, t);
            sun.color = Color.Lerp(sun.color, Color.white, t);
        }

        // the flames flicker, each in its own time, and grow in with the dark
        var time = Time.time;
        foreach (var (light, intensity, range, seed) in lights)
        {
            if (!light) continue;
            light.range = range * Mathf.Lerp(1f, TorchReach, t);
            var flicker = 0.88f + 0.08f * Mathf.Sin(time * 7.3f + seed) + 0.05f * Mathf.Sin(time * 13.1f + seed * 2.3f) + 0.04f * Mathf.PerlinNoise(time * 3f, seed);
            light.intensity = intensity * TorchBoost * flicker * t;
        }
    }

    /// <summary>The dungeon's room lights on, remembering how each was.</summary>
    private void LightUp(DungeonController dungeon)
    {
        PutOut();
        lit = dungeon;
        foreach (var child in dungeon.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Lights") continue;
            groups.Add((child.gameObject, child.gameObject.activeSelf));
            child.gameObject.SetActive(true);
        }
        foreach (var light in dungeon.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional) continue;
            lights.Add((light, light.intensity, light.range, Random.value * 100f));
        }
    }

    /// <summary>Everything back as the dungeon had it.</summary>
    private void PutOut()
    {
        foreach (var (light, intensity, range, _) in lights)
        {
            if (!light) continue;
            light.intensity = intensity;
            light.range = range;
        }
        foreach (var (group, wasActive) in groups) if (group) group.SetActive(wasActive);
        lights.Clear();
        groups.Clear();
        lit = null;
    }

    private void OnDestroy() => PutOut();
}
