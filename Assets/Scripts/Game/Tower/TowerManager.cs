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
///     Beside the floor, the band's town (<see cref="TowerTown"/>) loads the same way when someone goes there with
///     !tower town, and goes when it has been empty a while. When the next floor is a different layout (the forest, the
///     marsh, the crypt), the players climb to it: the old scene goes and the new one loads with them in it.
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

    /// <summary>Seconds between a spit's mark and the hit, and how far the spit splashes.</summary>
    public const float SpitWarning = 1.4f;
    public const float SpitRadius = 2.6f;

    /// <summary>Seconds after a won fight before the players climb to a floor of another layout.</summary>
    public const float ClimbDelay = 12f;

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
    private readonly Dictionary<(Guid Character, string Kind), int> characterKills = new Dictionary<(Guid, string), int>();
    private readonly List<TowerCharacterFall> characterFalls = new List<TowerCharacterFall>();
    private readonly List<PlayerController> pendingTownJoins = new List<PlayerController>();
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
    private GameObject spitRing;
    private float nextSpit;
    private float spitAt = -1f;
    private Vector3 spitSpot;
    private int addsCalled;
    private bool enraged;
    private float fightStarted;
    private bool climbing;

    // the town: loaded beside the floor
    public bool TownLoading { get; private set; }
    public string TownScene { get; private set; }
    private float townEmptySince = -1f;

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
        // level sync on the floors (docs/tower-design.md section 12)
        CombatRules.SyncFloor = f => f is PlayerController p && p.towerHandler != null ? p.towerHandler.SyncFloor : 0;
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
        // from the town up to the floor keeps the way back; otherwise only from the world
        var fromTown = player.towerHandler.InTown;
        if ((player.towerHandler.InTower && !fromTown) || pendingJoins.Contains(player)) return JoinResult.AlreadyIn;
        if (Phase == TowerPhase.Fight) return JoinResult.FightUnderWay;
        if (!fromTown && !player.towerHandler.CanEnter) return JoinResult.Busy;

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
        pendingTownJoins.Remove(player);
        if (player && player.towerHandler != null) player.towerHandler.Exit();
    }

    public List<PlayerController> Players()
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower && !p.towerHandler.InTown) list.Add(p);
        return list;
    }

    /// <summary>The players in the town.</summary>
    public List<PlayerController> TownPlayers()
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTown) list.Add(p);
        return list;
    }

    // ---- The town ---------------------------------------------------------------------------------------------

    public enum TownJoinResult { Joined, Loading, Busy, AlreadyIn, NoTown, Locked, OtherOpen }

    /// <summary>The town the stream can visit now: the one of the band its floor is in.</summary>
    public TowerTownDefinition StreamTown => Tower.TownFor(StreamFloor);

    /// <summary>A town the stream has reached: its band's or one below (Mossgate stays open for its chains).</summary>
    public bool TownUnlocked(TowerTownDefinition town) => town != null && StreamTown != null && town.Band <= StreamTown.Band;

    /// <summary>The town loaded now, or loading, or the one !tower town would open: the stream's band's.</summary>
    public TowerTownDefinition CurrentTown => TownLoading && loadingTown != null ? loadingTown : TowerTown.Loaded && TowerTown.Loaded.Town != null ? TowerTown.Loaded.Town : StreamTown;

    private TowerTownDefinition loadingTown;

    /// <summary>
    ///     !tower town: into a town, loading it first; from a floor too (not during a boss attempt). Without a name: the
    ///     town that is open (players go where the others are), else the stream's band's. With one (!tower town mossgate):
    ///     that town if the stream has reached it. One town is loaded at a time; another opens once the open one empties.
    /// </summary>
    public TownJoinResult JoinTown(PlayerController player, TowerTownDefinition wanted = null)
    {
        if (!player || player.towerHandler == null) return TownJoinResult.Busy;
        if (wanted != null && !TownUnlocked(wanted)) return TownJoinResult.Locked;
        var definition = wanted ?? CurrentTown;
        if (definition == null) return TownJoinResult.NoTown;
        if ((player.towerHandler.InTown && TowerTown.Loaded && TowerTown.Loaded.Town == definition) || pendingTownJoins.Contains(player)) return TownJoinResult.AlreadyIn;
        if (player.towerHandler.InBossFight) return TownJoinResult.Busy;
        if (!player.towerHandler.CanEnter && !player.towerHandler.InTower) return TownJoinResult.Busy;

        var town = TowerTown.Loaded;
        if (TownLoading && loadingTown != null && loadingTown != definition) return TownJoinResult.OtherOpen;
        if (town && !TownLoading)
        {
            if (town.Town != definition)
            {
                // another town is open: it gives way only when nobody is in it
                foreach (var p in TownPlayers()) if (p != player) return TownJoinResult.OtherOpen;
                if (pendingTownJoins.Count > 0) return TownJoinResult.OtherOpen;
                if (player.towerHandler.InTown) player.towerHandler.Exit();
                pendingTownJoins.Add(player);
                StartCoroutine(SwitchTownRoutine(definition));
                return TownJoinResult.Loading;
            }
            if (!player.towerHandler.Enter(town)) return TownJoinResult.Busy;
            pendingJoins.Remove(player);
            townEmptySince = -1f;
            return TownJoinResult.Joined;
        }

        pendingTownJoins.Add(player);
        if (!TownLoading)
        {
            loadingTown = definition;
            StartCoroutine(LoadTownRoutine(ScenePath(definition.Layout)));
        }
        return TownJoinResult.Loading;
    }

    // the open town is empty and another was asked for: it goes, the other comes
    private IEnumerator SwitchTownRoutine(TowerTownDefinition next)
    {
        TownLoading = true;
        loadingTown = next;
        yield return UnloadTownRoutine();
        TownLoading = false;
        yield return LoadTownRoutine(ScenePath(next.Layout));
    }

    private IEnumerator LoadTownRoutine(string scene)
    {
        TownLoading = true;
        AsyncOperation op;
#if UNITY_EDITOR
        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Additive));
#else
        op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
#endif
        if (op == null)
        {
            TownLoading = false;
            pendingTownJoins.Clear();
            yield break;
        }
        while (!op.isDone) yield return null;
        yield return null;
        TownScene = scene;
        TownLoading = false;
        loadingTown = null;
        var town = TowerTown.Loaded;
        foreach (var p in pendingTownJoins)
            if (p && !p.isDestroyed && town) p.towerHandler.Enter(town);
        pendingTownJoins.Clear();
        townEmptySince = -1f;
    }

    private IEnumerator UnloadTownRoutine()
    {
        var name = TownScene;
        TownScene = null;
        foreach (var p in TownPlayers()) p.towerHandler.Exit();
        var scene = SceneManager.GetSceneByPath(name);
        if (!scene.IsValid()) scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid()) yield break;
        var op = SceneManager.UnloadSceneAsync(scene);
        while (op != null && !op.isDone) yield return null;
    }

    /// <summary>
    ///     !tower talk: the town's person says their piece (their quest chain's step for this player, or one of their
    ///     lines) in a speech bubble; the chat reply is the quest command's.
    /// </summary>
    public IslandNpc Person(string key)
    {
        var town = TowerTown.Loaded;
        return town ? town.Person(key) : null;
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
    public void EnemyDefeated(EnemyController enemy, PlayerController by)
    {
        var floor = TowerFloor.Loaded;
        if (!floor) return;
        var kind = floor.KindOf(enemy);
        if (kind == null) return;

        // the player's own quests (Mossgate's chains) count kills on any floor
        if (by && by.UserId != Guid.Empty)
        {
            var key = (by.Id, kind.Id);
            characterKills.TryGetValue(key, out var mine);
            characterKills[key] = mine + 1;
        }

        var def = Definition;
        if (CurrentFloor != StreamFloor || AllCleared || def == null) return;
        foreach (var q in def.Quests)
        {
            if (q.Target != kind.Id) continue;
            pendingKills.TryGetValue(q.Id, out var n);
            pendingKills[q.Id] = n + 1;
        }
    }

    /// <summary>A player was knocked out during a boss attempt (reported with the kills, for a hidden achievement).</summary>
    public void PlayerFell(PlayerController player, int floor)
    {
        if (player && player.UserId != Guid.Empty && Phase == TowerPhase.Fight)
            characterFalls.Add(new TowerCharacterFall { CharacterId = player.Id, Floor = floor });
    }

    private async void FlushCharacterKills()
    {
        if (characterKills.Count == 0 && characterFalls.Count == 0) return;
        var g = Game;
        if (!g || g.RavenNest == null || !g.RavenNest.SessionStarted) return;
        var report = new TowerKillReport
        {
            Kills = characterKills.Select(kv => new TowerCharacterKills { CharacterId = kv.Key.Character, Kind = kv.Key.Kind, Count = kv.Value }).ToArray(),
            Falls = characterFalls.ToArray(),
        };
        characterKills.Clear();
        characterFalls.Clear();
        try
        {
            await g.RavenNest.Tower.KillsAsync(report);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.FlushCharacterKills: " + exc.Message);
        }
    }

    private async void FlushKills()
    {
        FlushCharacterKills();
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
        fightStarted = Time.time;
        nextSlam = Time.time + SlamInterval(boss);
        slamAt = -1f;
        nextSpit = Time.time + SpitInterval(boss) * 0.5f;
        spitAt = -1f;
        addsCalled = 0;
        enraged = false;
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

        UpdateKit(boss, def);

        // the slam: a ring on the ground first, then everyone inside it is hit
        var radius = SlamRadius(boss);
        if (slamAt < 0f && Time.time >= nextSlam)
        {
            slamAt = Time.time + SlamWarning;
            ShowRing(ref slamRing, "Tower Slam Warning", boss.transform.position, radius, new Color(1f, 0.25f, 0.1f, 1f), 0.5f);
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

    private float SpitInterval(EnemyController boss)
    {
        var def = Definition;
        if (def == null || def.Boss.SpitEvery <= 0) return float.MaxValue;
        return (float)(def.Boss.SpitEvery * Math.Max(0.5, boss.CombatDefinition?.AttackInterval ?? 2));
    }

    /// <summary>
    ///     The boss's other moves: help at shares of its health, enrage after a while, and a spit at someone who is not
    ///     holding it (the farthest from it), marked on the ground before it lands.
    /// </summary>
    private void UpdateKit(EnemyController boss, TowerFloorDefinition def)
    {
        var kit = def.Boss;
        var floor = TowerFloor.Loaded;

        var share = boss.Stats.HealthPercent;
        if (kit.AddsAt != null && addsCalled < kit.AddsAt.Length && share <= kit.AddsAt[addsCalled])
        {
            addsCalled++;
            var made = floor.SpawnAdds(kit.AddsCount);
            if (made > 0) Announce(Localization.MSG_TOWER_BOSS_ADDS, kit.Name, made, def.Enemies[0].Name);
        }

        if (!enraged && kit.EnrageSeconds > 0 && Time.time - fightStarted >= kit.EnrageSeconds)
        {
            enraged = true;
            boss.Enrage(kit.EnrageInterval, kit.EnragePower);
            nextSlam = Mathf.Min(nextSlam, Time.time + SlamInterval(boss));
            Announce(Localization.MSG_TOWER_BOSS_ENRAGED, kit.Name);
        }

        if (kit.SpitEvery <= 0) return;
        if (spitAt < 0f && Time.time >= nextSpit)
        {
            var target = SpitTarget(boss);
            if (!target)
            {
                nextSpit = Time.time + 2f;
                return;
            }
            spitSpot = target.Position;
            spitAt = Time.time + SpitWarning;
            ShowRing(ref spitRing, "Tower Spit Warning", spitSpot, SpitRadius, new Color(0.55f, 1f, 0.2f, 1f), 0.3f);
        }
        else if (spitAt >= 0f && Time.time >= spitAt)
        {
            spitAt = -1f;
            if (spitRing) spitRing.SetActive(false);
            nextSpit = Time.time + SpitInterval(boss);
            foreach (var p in participants.ToList())
            {
                if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower) continue;
                var d = p.Position - spitSpot;
                d.y = 0f;
                if (d.magnitude > SpitRadius) continue;
                var damage = Mathf.Max(1, (int)(GameMath.CalculateMeleeDamage(boss, p) * (float)kit.SpitShare));
                p.TakeDamage(boss, damage);
            }
        }
    }

    /// <summary>Someone the boss is not fighting: the one standing farthest from it (healers stand back).</summary>
    private PlayerController SpitTarget(EnemyController boss)
    {
        PlayerController best = null;
        var bestDistance = -1f;
        var holding = boss.TargetPlayer;
        foreach (var p in participants)
        {
            if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || p == holding) continue;
            var d = Vector3.Distance(p.Position, boss.transform.position);
            if (d > bestDistance) { bestDistance = d; best = p; }
        }
        return best;
    }

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
            Announce(Localization.MSG_TOWER_CLIMB, next.Number, next.Name, (int)ClimbDelay);
            StartCoroutine(ClimbRoutine(next.Number));
        }
    }

    /// <summary>
    ///     The next floor is another layout: after a moment to celebrate, the players on the floor climb to it. The old
    ///     scene goes (they step back to the world for the moment it takes) and the new one loads with them in it.
    /// </summary>
    private IEnumerator ClimbRoutine(int next)
    {
        climbing = true;
        yield return new WaitForSeconds(ClimbDelay);
        var climbers = Players();
        foreach (var p in climbers) if (!pendingJoins.Contains(p)) pendingJoins.Add(p);
        yield return UnloadRoutine();
        LoadFloor(next);
        climbing = false;
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
        spitAt = -1f;
        if (spitRing) spitRing.SetActive(false);
        var floor = TowerFloor.Loaded;
        if (floor) floor.DespawnBoss();
        foreach (var p in participants) if (p && p.towerHandler != null) p.towerHandler.BossFightEnded();
        participants.Clear();
    }

    private static void ShowRing(ref GameObject ring, string name, Vector3 centre, float radius, Color color, float width)
    {
        if (!ring)
        {
            ring = new GameObject(name);
            var line = ring.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = 48;
            line.widthMultiplier = width;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            line.material = new Material(shader);
            line.material.color = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var lr = ring.GetComponent<LineRenderer>();
        for (var i = 0; i < lr.positionCount; ++i)
        {
            var a = i / (float)lr.positionCount * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.4f, Mathf.Sin(a) * radius));
        }
        ring.transform.position = centre;
        ring.SetActive(true);
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

        // nobody in the town for a while: it goes
        if (!TownLoading && !string.IsNullOrEmpty(TownScene))
        {
            if (TownPlayers().Count > 0 || pendingTownJoins.Count > 0) townEmptySince = -1f;
            else if (townEmptySince < 0f) townEmptySince = Time.time;
            else if (Time.time - townEmptySince > EmptyFloorSeconds) StartCoroutine(UnloadTownRoutine());
        }

        // nobody in it for a while: the floor goes
        if (Phase == TowerPhase.Open && !climbing && !string.IsNullOrEmpty(LoadedScene))
        {
            if (Players().Count > 0) emptySince = -1f;
            else if (emptySince < 0f) emptySince = Time.time;
            else if (Time.time - emptySince > EmptyFloorSeconds) Unload();
        }
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        TowerPlace floor = null;
        if (camera)
        {
            var cam = camera.transform.position;
            if (TowerFloor.Loaded && TowerFloor.Loaded.Contains(cam)) floor = TowerFloor.Loaded;
            else if (TowerTown.Loaded && TowerTown.Loaded.Contains(cam)) floor = TowerTown.Loaded;
        }
        var over = floor;
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
