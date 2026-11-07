using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     The tower (docs/tower-design.md): loads a floor scene next to MainWorld when the first player enters with
///     !tower, moves the players who join into it and back, counts the floor's quests, runs boss attempts, and unloads
///     the floor when nobody has been in it for a while. MainWorld is never unloaded: the players who did not join keep
///     training as if nothing happened. While the camera is over the floor, the floor's light and fog replace the
///     world's, and the world-wide systems that follow the camera (sea, clouds, cloud shadows, weather) are switched off.
///     The stream's progress (its floor, the quests, clears) is the server's (api/tower); this keeps a copy.
/// </summary>
public sealed class TowerManager : MonoBehaviour
{
    public enum TowerPhase
    {
        /// <summary>No floor loaded.</summary>
        Closed,
        Loading,
        /// <summary>Players train on the floor and work on its quests.</summary>
        Open,
        /// <summary>A boss attempt was called: players join with !tower.</summary>
        Countdown,
        Fight,
    }

    public static TowerManager Instance { get; private set; }

    /// <summary>Seconds the floor stays loaded after the last player left.</summary>
    public const float EmptyFloorSeconds = 90f;

    /// <summary>Seconds between kill reports to the server.</summary>
    public const float ReportSeconds = 8f;

    /// <summary>Seconds between the boss's slam warning and the slam.</summary>
    public const float SlamWarning = 1.6f;

    /// <summary>What the last load and unload measured, for the tests.</summary>
    public string Report => report.ToString();

    public bool Loading { get; private set; }
    public string LoadedScene { get; private set; }
    public TowerPhase Phase { get; private set; }

    /// <summary>The stream's progress as the server last answered, or null before it did.</summary>
    public TowerState State { get; private set; }

    /// <summary>The floor that is loaded (its number), 0 when none.</summary>
    public int CurrentFloor { get; private set; }

    public TowerFloorDefinition Definition => Tower.Get(CurrentFloor);

    /// <summary>Seconds left of the countdown or of the attempt.</summary>
    public float PhaseSecondsLeft => Mathf.Max(0f, phaseEnds - Time.time);

    /// <summary>Seconds until the next attempt can start after a lost one.</summary>
    public float RetrySecondsLeft => Mathf.Max(0f, retryAt - Time.time);

    public IReadOnlyCollection<PlayerController> Participants => participants;

    private readonly StringBuilder report = new StringBuilder();
    private readonly List<PlayerController> pendingJoins = new List<PlayerController>();
    private readonly HashSet<PlayerController> participants = new HashSet<PlayerController>();
    private readonly HashSet<string> bossVotes = new HashSet<string>();
    private readonly Dictionary<string, int> pendingKills = new Dictionary<string, int>();
    private GameManager game;
    private bool stateRequested;
    private bool reporting;
    private float nextReport;
    private float emptySince = -1f;
    private float phaseEnds;
    private float retryAt;
    private float voteStarted;
    private float nextSlam;
    private float slamAt = -1f;
    private GameObject slamRing;

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

    private GameManager Game => game ? game : (game = FindAnyObjectByType<GameManager>());

    // ---- The server's state ---------------------------------------------------------------------------------

    public async void RefreshState()
    {
        var g = Game;
        if (!g || g.RavenNest == null || !g.RavenNest.SessionStarted) return;
        stateRequested = true;
        try
        {
            var state = await g.RavenNest.Tower.GetAsync();
            if (state != null) SetState(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.RefreshState: " + exc.Message);
        }
    }

    private void SetState(TowerState state)
    {
        var wasOpen = State != null && State.BossOpen && State.Floor == state.Floor;
        State = state;
        if (state.BossOpen && !wasOpen && Phase == TowerPhase.Open && CurrentFloor == state.Floor && Definition != null)
            Announce(Localization.MSG_TOWER_BOSS_OPEN, CurrentFloor, Definition.Boss.Name);
    }

    /// <summary>The floor the stream works on: the lowest not cleared, or the top floor when all are.</summary>
    public int StreamFloor => State == null ? 1 : Mathf.Clamp(State.Floor, 1, Tower.TopFloor);

    /// <summary>The stream has cleared every floor that exists so far.</summary>
    public bool AllCleared => State != null && State.Floor > Tower.TopFloor;

    /// <summary>"Defeat 30 Feral Bunnies (12/30), Defeat 20 Moss Goblins (done)"</summary>
    public string QuestText()
    {
        var floor = Tower.Get(StreamFloor);
        if (floor == null) return "";
        if (AllCleared) return Localization.MSG_TOWER_QUESTS_CLEARED;
        var parts = new List<string>();
        foreach (var q in floor.Quests)
        {
            var count = State?.Quests?.FirstOrDefault(x => x.Id == q.Id)?.Count ?? 0;
            parts.Add(q.Text + (count >= q.Amount ? " (done)" : " (" + count + "/" + q.Amount + ")"));
        }
        return string.Join(", ", parts);
    }

    // ---- Joining and leaving --------------------------------------------------------------------------------

    public enum JoinResult { Joined, Loading, Busy, AlreadyIn, FightUnderWay, NotReady }

    /// <summary>
    ///     !tower: into the floor, loading it first if nobody is in the tower. Players can join while it trains and during
    ///     a boss countdown, not during the fight.
    /// </summary>
    public JoinResult Join(PlayerController player)
    {
        if (!player || player.towerHandler == null) return JoinResult.NotReady;
        if (player.towerHandler.InTower || pendingJoins.Contains(player)) return JoinResult.AlreadyIn;
        if (Phase == TowerPhase.Fight) return JoinResult.FightUnderWay;
        if (!player.towerHandler.CanEnter) return JoinResult.Busy;

        if (Phase == TowerPhase.Open || Phase == TowerPhase.Countdown)
        {
            var floor = TowerFloor.Loaded;
            if (!floor || !player.towerHandler.Enter(floor)) return JoinResult.Busy;
            if (Phase == TowerPhase.Countdown) participants.Add(player);
            emptySince = -1f;
            return JoinResult.Joined;
        }

        if (State == null)
        {
            if (!stateRequested) RefreshState();
            return JoinResult.NotReady;
        }

        pendingJoins.Add(player);
        if (Phase == TowerPhase.Closed) LoadFloor(StreamFloor);
        return JoinResult.Loading;
    }

    /// <summary>Moves a player into the loaded floor; false when they cannot go (busy, away, dead) or nothing is loaded.</summary>
    public bool Enter(PlayerController player)
    {
        var floor = TowerFloor.Loaded;
        return floor && player && player.towerHandler != null && player.towerHandler.Enter(floor);
    }

    public void Leave(PlayerController player)
    {
        pendingJoins.Remove(player);
        if (player && player.towerHandler != null) player.towerHandler.Exit();
    }

    public List<PlayerController> Players()
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower) list.Add(p);
        return list;
    }

    // ---- Loading ----------------------------------------------------------------------------------------------

    /// <summary>The scene of a layout: its path in the editor, its name in a build (it must be in the build's scene list).</summary>
    public static string ScenePath(string layout)
    {
#if UNITY_EDITOR
        return "Assets/Scenes/Tower/" + layout + ".unity";
#else
        return layout;
#endif
    }

    public void LoadFloor(int number)
    {
        var floor = Tower.Get(number);
        if (floor == null || Loading || TowerFloor.Loaded) return;
        CurrentFloor = number;
        Phase = TowerPhase.Loading;
        StartCoroutine(LoadRoutine(ScenePath(floor.Layout)));
    }

    /// <summary>Loads a floor scene (its path in the project, or its name when it is in the build) next to MainWorld.</summary>
    public void Load(string scene)
    {
        if (Loading || TowerFloor.Loaded) return;
        Phase = TowerPhase.Loading;
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
        // in the editor the floor scenes do not have to be in the build's scene list
        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Additive));
#else
        op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
#endif
        if (op == null)
        {
            report.AppendLine("could not start loading " + scene);
            Loading = false;
            Phase = TowerPhase.Closed;
            CurrentFloor = 0;
            pendingJoins.Clear();
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
        if (floor && CurrentFloor > 0) floor.Apply(Tower.Get(CurrentFloor));
        Phase = floor ? TowerPhase.Open : TowerPhase.Closed;

        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("loaded " + scene + " in " + loadedIn.ToString("0.00", CultureInfo.InvariantCulture) + " s over " + frames + " frames, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory +" + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB, floor "
            + (floor ? floor.name + " with " + floor.Enemies.Count + " enemies" : "missing"));

        // everyone who asked while it loaded goes in now
        var joined = 0;
        foreach (var p in pendingJoins)
            if (p && !p.isDestroyed && floor && p.towerHandler.Enter(floor)) joined++;
        pendingJoins.Clear();
        emptySince = -1f;
        var def = Definition;
        if (joined > 0 && def != null) Announce(Localization.MSG_TOWER_OPENED, CurrentFloor, def.Name, joined, QuestText());
    }

    /// <summary>Everyone back to the world, then the floor scene goes.</summary>
    public void Unload()
    {
        if (Loading || string.IsNullOrEmpty(LoadedScene)) return;
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator UnloadRoutine()
    {
        EndAttempt();
        foreach (var p in Players()) p.towerHandler.Exit();
        Phase = TowerPhase.Closed;
        FlushKills();
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
        var cleanup = UnityEngine.Resources.UnloadUnusedAssets();
        while (!cleanup.isDone) yield return null;
        GC.Collect();
        yield return null;
        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("unloaded in " + (Time.realtimeSinceStartup - started).ToString("0.00", CultureInfo.InvariantCulture) + " s, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory " + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + " MB");
        LoadedScene = null;
        CurrentFloor = 0;
    }

    // ---- Quests ----------------------------------------------------------------------------------------------

    /// <summary>A monster on the floor fell to a player in the tower (PlayerCombat.Defeated).</summary>
    public void EnemyDefeated(EnemyController enemy)
    {
        var floor = TowerFloor.Loaded;
        if (!floor || CurrentFloor != StreamFloor || AllCleared) return;
        var kind = floor.KindOf(enemy);
        var def = Definition;
        if (kind == null || def == null) return;
        foreach (var q in def.Quests)
        {
            if (q.Target != kind.Id) continue;
            pendingKills.TryGetValue(q.Id, out var n);
            pendingKills[q.Id] = n + 1;
        }
    }

    private async void FlushKills()
    {
        if (pendingKills.Count == 0 || reporting) return;
        var g = Game;
        if (!g || !g.RavenNest.SessionStarted) return;
        var counts = string.Join(";", pendingKills.Select(kv => kv.Key + ":" + kv.Value));
        var floor = CurrentFloor;
        pendingKills.Clear();
        reporting = true;
        try
        {
            var state = await g.RavenNest.Tower.ProgressAsync(floor, counts);
            if (state != null) SetState(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.FlushKills: " + exc.Message);
        }
        finally
        {
            reporting = false;
        }
    }

    // ---- Boss attempts -------------------------------------------------------------------------------------

    public enum BossCallResult { Started, Voted, NotOpen, NotLoaded, UnderWay, Regrouping, NotInTower }

    /// <summary>
    ///     !tower boss: the streamer or a moderator starts the countdown at once; anyone else in the tower votes, and
    ///     three votes (or everyone in the tower, when fewer are in it) start it. <paramref name="votesNeeded"/> is what
    ///     is still missing after a vote.
    /// </summary>
    public BossCallResult CallBoss(PlayerController player, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight) return BossCallResult.UnderWay;
        if (Phase != TowerPhase.Open || !TowerFloor.Loaded) return BossCallResult.NotLoaded;
        if (State == null || !State.BossOpen || CurrentFloor != StreamFloor || AllCleared) return BossCallResult.NotOpen;
        if (RetrySecondsLeft > 0) return BossCallResult.Regrouping;

        if (!canForce)
        {
            if (!player || player.towerHandler == null || !player.towerHandler.InTower) return BossCallResult.NotInTower;
            if (Time.time - voteStarted > 120f) bossVotes.Clear();
            if (bossVotes.Count == 0) voteStarted = Time.time;
            bossVotes.Add(player.Name);
            var needed = Mathf.Min(3, Mathf.Max(1, Players().Count));
            votesNeeded = Mathf.Max(0, needed - bossVotes.Count);
            if (votesNeeded > 0) return BossCallResult.Voted;
        }

        StartCountdown();
        return BossCallResult.Started;
    }

    private void StartCountdown()
    {
        bossVotes.Clear();
        participants.Clear();
        foreach (var p in Players()) participants.Add(p);
        Phase = TowerPhase.Countdown;
        phaseEnds = Time.time + Tower.BossCountdownSeconds;
        Announce(Localization.MSG_TOWER_BOSS_COUNTDOWN, CurrentFloor, Tower.BossCountdownSeconds, Definition.Boss.Name);
    }

    private async void StartFight()
    {
        var floor = TowerFloor.Loaded;
        var def = Definition;
        participants.RemoveWhere(p => !p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower);
        if (!floor || def == null || participants.Count == 0)
        {
            Phase = TowerPhase.Open;
            return;
        }

        var boss = floor.SpawnBoss();
        if (!boss)
        {
            Shinobytes.Debug.LogError("TowerManager: floor " + CurrentFloor + " has no template for the boss " + def.Boss.Model);
            Phase = TowerPhase.Open;
            return;
        }

        Phase = TowerPhase.Fight;
        phaseEnds = Time.time + Tower.BossTimeLimitSeconds;
        nextSlam = Time.time + SlamInterval(boss);
        slamAt = -1f;
        foreach (var p in participants) p.towerHandler.BossFightStarted(boss);
        Announce(Localization.MSG_TOWER_BOSS_START, def.Boss.Name, participants.Count, def.Boss.Line ?? "");

        var g = Game;
        try
        {
            var state = await g.RavenNest.Tower.BossStartedAsync(CurrentFloor);
            if (state != null) SetState(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.BossStarted: " + exc.Message);
        }
    }

    private float SlamInterval(EnemyController boss)
    {
        var def = Definition;
        if (def == null || def.Boss.SlamEvery <= 0) return float.MaxValue;
        return (float)(def.Boss.SlamEvery * Math.Max(0.5, boss.CombatDefinition?.AttackInterval ?? 2));
    }

    private void UpdateFight()
    {
        var floor = TowerFloor.Loaded;
        var boss = floor ? floor.Boss : null;
        var def = Definition;
        if (!boss || def == null) { EndAttempt(); Phase = TowerPhase.Open; return; }

        if (boss.Stats.IsDead)
        {
            Won(def);
            return;
        }

        var standing = participants.Count(p => p && !p.isDestroyed && p.towerHandler.InTower && !p.Stats.IsDead);
        if (standing == 0) { Lost(def, Localization.MSG_TOWER_BOSS_LOST_FALLEN); return; }
        if (PhaseSecondsLeft <= 0f) { Lost(def, Localization.MSG_TOWER_BOSS_LOST_TIME); return; }

        // the slam: a ring on the ground first, then everyone inside it is hit
        var radius = SlamRadius(boss);
        if (slamAt < 0f && Time.time >= nextSlam)
        {
            slamAt = Time.time + SlamWarning;
            ShowRing(boss.transform.position, radius);
        }
        else if (slamAt >= 0f && Time.time >= slamAt)
        {
            slamAt = -1f;
            HideRing();
            nextSlam = Time.time + SlamInterval(boss);
            var share = (float)def.Boss.SlamShare;
            foreach (var p in participants.ToList())
            {
                if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower) continue;
                var d = p.Position - boss.transform.position;
                d.y = 0f;
                if (d.magnitude > radius) continue;
                var damage = Mathf.Max(1, (int)(GameMath.CalculateMeleeDamage(boss, p) * share));
                p.TakeDamage(boss, damage);
            }
        }
    }

    private static float SlamRadius(EnemyController boss) => 4f + 1.5f * boss.transform.localScale.x;

    private async void Won(TowerFloorDefinition def)
    {
        var floorNumber = CurrentFloor;
        var ids = participants.Where(p => p && !p.isDestroyed).Select(p => p.Id).Distinct().ToArray();
        var players = ids.Length;
        EndAttempt();
        Phase = TowerPhase.Open;
        Announce(Localization.MSG_TOWER_BOSS_DOWN, def.Boss.Name, floorNumber);

        var g = Game;
        TowerClearResult result = null;
        try
        {
            result = await g.RavenNest.Tower.ClearedAsync(floorNumber, ids);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.Cleared: " + exc.Message);
        }

        if (result == null || !result.Success)
        {
            Announce(Localization.MSG_TOWER_CLEAR_FAILED, result?.Message ?? "no answer from the server");
            if (result?.State != null) SetState(result.State);
            return;
        }

        if (result.WorldFirst) Announce(Localization.MSG_TOWER_WORLD_FIRST, floorNumber);
        Announce(Localization.MSG_TOWER_CLEARED, floorNumber, players, result.Coins.ToString("N0", CultureInfo.InvariantCulture), result.Rewards?.Length ?? 0);
        if (result.Rewards != null && result.Rewards.Length > 0) g.AddItems(result.Rewards);
        if (result.State != null) SetState(result.State);

        // the next floor: the same layout fills again for it, a new one loads after the players come back
        var next = Tower.Get(StreamFloor);
        if (AllCleared || next == null)
        {
            Announce(Localization.MSG_TOWER_TOP, floorNumber);
            return;
        }
        var floor = TowerFloor.Loaded;
        if (floor && next.Layout == def.Layout)
        {
            CurrentFloor = next.Number;
            floor.Apply(next);
            foreach (var p in Players()) p.towerHandler.FloorChanged(floor);
            Announce(Localization.MSG_TOWER_NEXT_FLOOR, next.Number, next.Name, QuestText());
        }
        else
        {
            Announce(Localization.MSG_TOWER_NEXT_FLOOR_LATER, next.Number, next.Name);
        }
    }

    private void Lost(TowerFloorDefinition def, string message)
    {
        EndAttempt();
        Phase = TowerPhase.Open;
        retryAt = Time.time + Tower.BossRetrySeconds;
        Announce(message, def.Boss.Name, Tower.BossRetrySeconds);
    }

    private void EndAttempt()
    {
        HideRing();
        slamAt = -1f;
        var floor = TowerFloor.Loaded;
        if (floor) floor.DespawnBoss();
        foreach (var p in participants) if (p && p.towerHandler != null) p.towerHandler.BossFightEnded();
        participants.Clear();
    }

    private void ShowRing(Vector3 centre, float radius)
    {
        if (!slamRing)
        {
            slamRing = new GameObject("Tower Slam Warning");
            var line = slamRing.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = 48;
            line.widthMultiplier = 0.5f;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            line.material = new Material(shader);
            line.material.color = new Color(1f, 0.25f, 0.1f, 1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var lr = slamRing.GetComponent<LineRenderer>();
        for (var i = 0; i < lr.positionCount; ++i)
        {
            var a = i / (float)lr.positionCount * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.4f, Mathf.Sin(a) * radius));
        }
        slamRing.transform.position = centre;
        slamRing.SetActive(true);
    }

    private void HideRing()
    {
        if (slamRing) slamRing.SetActive(false);
    }

    private void Announce(string message, params object[] args)
    {
        var g = Game;
        if (!g || g.RavenBot == null) return;
        g.RavenBot.Announce(message, args.Select(a => a?.ToString() ?? "").ToArray());
    }

    // ---- Every frame -----------------------------------------------------------------------------------------

    private void Update()
    {
        var g = Game;
        if (!g) return;
        if (!stateRequested && g.RavenNest != null && g.RavenNest.SessionStarted) RefreshState();

        if (Phase == TowerPhase.Countdown && PhaseSecondsLeft <= 0f) StartFight();
        else if (Phase == TowerPhase.Fight) UpdateFight();

        if (Time.time >= nextReport)
        {
            nextReport = Time.time + ReportSeconds;
            FlushKills();
        }

        // nobody in it for a while: the floor goes
        if (Phase == TowerPhase.Open && !string.IsNullOrEmpty(LoadedScene))
        {
            if (Players().Count > 0) emptySince = -1f;
            else if (emptySince < 0f) emptySince = Time.time;
            else if (Time.time - emptySince > EmptyFloorSeconds) Unload();
        }
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
