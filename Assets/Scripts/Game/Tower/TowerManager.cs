using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;

/// <summary>
///     Loads a tower floor scene next to MainWorld, moves the players who join into it and back, and unloads it when
///     the last one leaves (docs/tower-design.md, sections 8 and 9). MainWorld is never unloaded: the players who did
///     not join keep training as if nothing happened. While the camera is over the floor, the floor's light and fog
///     replace the world's, and the world-wide systems that follow the camera (sea, clouds, cloud shadows, weather)
///     are switched off.
///     The first version is the plausibility test: it measures how long loading stalls frames and what it costs in
///     memory (<see cref="Report"/>).
/// </summary>
public sealed class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }

    /// <summary>What the last load and unload measured, for the test.</summary>
    public string Report => report.ToString();

    public bool Loading { get; private set; }
    public string LoadedScene { get; private set; }

    private readonly StringBuilder report = new StringBuilder();
    private GameManager game;

    // the look: what was on before the camera came over the floor
    private bool lookOn;
    private bool fogWas;
    private Color fogColorWas;
    private float fogDensityWas;
    private FogMode fogModeWas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying || Instance) return;
        var go = new GameObject("Tower Manager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<TowerManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Loads a floor scene (its path in the project, or its name when it is in the build) next to MainWorld.</summary>
    public void Load(string scene)
    {
        if (Loading || TowerFloor.Loaded) return;
        StartCoroutine(LoadRoutine(scene));
    }

    private IEnumerator LoadRoutine(string scene)
    {
        Loading = true;
        report.Clear();
        var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        var started = Time.realtimeSinceStartup;
        var worst = 0f;
        var frames = 0;

        AsyncOperation op;
#if UNITY_EDITOR
        // in the editor the test scene does not have to be in the build's scene list
        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Additive));
#else
        op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
#endif
        if (op == null)
        {
            report.AppendLine("could not start loading " + scene);
            Loading = false;
            yield break;
        }

        // the frame time is measured from the frame after the call: the call itself is in this frame
        while (!op.isDone)
        {
            yield return null;
            frames++;
            worst = Mathf.Max(worst, Time.unscaledDeltaTime);
        }
        // the frame in which the scene's objects woke up is the one after isDone
        yield return null;
        worst = Mathf.Max(worst, Time.unscaledDeltaTime);
        var loadedIn = Time.realtimeSinceStartup - started;

        LoadedScene = scene;
        Loading = false;
        var floor = TowerFloor.Loaded;
        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("loaded " + scene + " in " + loadedIn.ToString("0.00", CultureInfo.InvariantCulture) + " s over " + frames + " frames, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory +" + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB, floor "
            + (floor ? floor.name + " with " + floor.Enemies.Count + " enemies" : "missing"));
    }

    /// <summary>Moves a player into the loaded floor; false when they cannot go (busy, away, dead) or nothing is loaded.</summary>
    public bool Join(PlayerController player)
    {
        var floor = TowerFloor.Loaded;
        return floor && player && player.towerHandler != null && player.towerHandler.Enter(floor);
    }

    public void Leave(PlayerController player)
    {
        if (player && player.towerHandler != null) player.towerHandler.Exit();
    }

    public List<PlayerController> Players()
    {
        var list = new List<PlayerController>();
        if (!game) game = FindAnyObjectByType<GameManager>();
        if (!game) return list;
        foreach (var p in game.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower) list.Add(p);
        return list;
    }

    /// <summary>Everyone back to the world, then the floor scene goes.</summary>
    public void Unload()
    {
        if (Loading || string.IsNullOrEmpty(LoadedScene)) return;
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator UnloadRoutine()
    {
        foreach (var p in Players()) p.towerHandler.Exit();
        SetLook(false);
        var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        var started = Time.realtimeSinceStartup;
        var worst = 0f;
        var scene = SceneManager.GetSceneByPath(LoadedScene);
        if (!scene.IsValid()) scene = SceneManager.GetSceneByName(LoadedScene);
        var op = SceneManager.UnloadSceneAsync(scene);
        while (op != null && !op.isDone)
        {
            yield return null;
            worst = Mathf.Max(worst, Time.unscaledDeltaTime);
        }
        var cleanup = Resources.UnloadUnusedAssets();
        while (!cleanup.isDone) yield return null;
        System.GC.Collect();
        yield return null;
        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("unloaded in " + (Time.realtimeSinceStartup - started).ToString("0.00", CultureInfo.InvariantCulture) + " s, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory " + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + " MB");
        LoadedScene = null;
    }

    private void LateUpdate()
    {
        var floor = TowerFloor.Loaded;
        var camera = Camera.main;
        var over = floor && camera && floor.Contains(camera.transform.position);
        SetLook(over);
        if (!over) return;

        // GameManager sets these from potato mode every frame, so they are switched off every frame while the camera is
        // over the floor; when it leaves, the next frame puts them back as the settings say
        Ocean.Suppressed = true;
        SkyClouds.Suppressed = true;
        CloudShadows.Suppressed = true;
        Weather.Suppressed = true;

        // after the day and night cycle has set this frame's light: the floor's own
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = floor.SkyAmbient;
        RenderSettings.ambientEquatorColor = floor.EquatorAmbient;
        RenderSettings.ambientGroundColor = floor.GroundAmbient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = floor.FogColor;
        RenderSettings.fogDensity = floor.FogDensity;
        var sun = RenderSettings.sun;
        if (sun)
        {
            sun.color = floor.SunColor;
            sun.intensity = floor.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(floor.SunAngles);
        }
    }

    private void SetLook(bool on)
    {
        if (on == lookOn) return;
        lookOn = on;
        if (on)
        {
            fogWas = RenderSettings.fog; fogColorWas = RenderSettings.fogColor; fogDensityWas = RenderSettings.fogDensity; fogModeWas = RenderSettings.fogMode;
        }
        else
        {
            // the world's fog comes back as it was; the day and night cycle sets the sun and the ambient again on the
            // next frame
            RenderSettings.fog = fogWas;
            RenderSettings.fogColor = fogColorWas;
            RenderSettings.fogDensity = fogDensityWas;
            RenderSettings.fogMode = fogModeWas;
        }
    }
}
