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
///     After a clear everyone on the floor goes down to the town to rest and train, and goes on up when the streamer, a
///     moderator or a chat vote says so (!tower climb); every choice the streamer makes can be made by a vote, so a
///     tower never waits on an away streamer (docs/tower-design.md, section 16).
///     The first time a stream opens the tower, and the first time it reaches a new band's town, a cutscene plays
///     (<see cref="TowerCutscene"/>); joins wait for the first. The first time a stream fights a boss, a short intro shows
///     it taunting before the fight starts, and every boss falls in slow motion with the floor's number.
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

    /// <summary>Participants knocked out during the attempt: they get back up in the tower but sit the rest of it out.</summary>
    private readonly HashSet<PlayerController> fallen = new HashSet<PlayerController>();
    private readonly Dictionary<string, Vote> votes = new Dictionary<string, Vote>();
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
        // the tower stands far out at sea, but its floors and towns have no sea: a camera anywhere near one (a wide
        // shot from outside its reach too) sees none
        Ocean.NoSeaAt = NearPlace;
    }

    /// <summary>How far past a place's own reach the sea stays hidden: the islands are kilometres away.</summary>
    private const float NoSeaMargin = 400f;

    /// <summary>
    ///     How far past a place's own reach the camera still sees its look (light, fog, no sky clouds or weather): a
    ///     wide shot from outside showed MainWorld's sky and sea round the floor. The towns stand 600 m from the floors,
    ///     so the nearest loaded place wins.
    /// </summary>
    private const float LookMargin = 200f;

    private static bool NearPlace(Vector3 eye) => PlaceNear(eye, NoSeaMargin);

    /// <summary>The nearest loaded floor or town within its reach plus a margin, or null.</summary>
    private static TowerPlace PlaceNear(Vector3 eye, float margin)
    {
        // called every frame and for every camera that draws: no allocations
        var floor = Reach(TowerFloor.Loaded, eye, margin);
        var town = Reach(TowerTown.Loaded, eye, margin);
        if (floor < 0f && town < 0f) return null;
        if (town < 0f) return TowerFloor.Loaded;
        if (floor < 0f) return TowerTown.Loaded;
        return floor <= town ? (TowerPlace)TowerFloor.Loaded : TowerTown.Loaded;
    }

    /// <summary>The ground distance from a place within its reach plus a margin, or -1.</summary>
    private static float Reach(TowerPlace place, Vector3 eye, float margin)
    {
        if (!place) return -1f;
        var d = eye - place.Centre;
        d.y = 0f;
        var distance = d.magnitude;
        return distance <= place.Radius + margin ? distance : -1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Ocean.NoSeaAt == (System.Func<Vector3, bool>)NearPlace) Ocean.NoSeaAt = null;
        if (hazeMaterial) Destroy(hazeMaterial);
        if (hazeBand) Destroy(hazeBand);
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

    public enum JoinResult { Joined, Loading, Busy, AlreadyIn, FightUnderWay, NotReady, ToTown }

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
        // the first time the stream opens the tower: its cutscene first, then they go in
        if (!fromTown && WaitForOpening(player, false, null)) return JoinResult.Loading;

        // between floors everyone waits in the town: a newcomer goes to them (!tower again goes up alone)
        if (!fromTown && Phase != TowerPhase.Countdown && Players().Count == 0 && TowerTown.Loaded && !TownLoading && TownPlayers().Count > 0)
        {
            var town = JoinTown(player);
            if (town == TownJoinResult.Joined || town == TownJoinResult.Loading) return JoinResult.ToTown;
        }

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
        waitingForOpening.RemoveAll(w => w.Player == player);
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

    /// <summary>Everyone in the tower: on the floor and in the town.</summary>
    public List<PlayerController> AllPlayers()
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower) list.Add(p);
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
        if (!player.towerHandler.InTower && WaitForOpening(player, true, wanted)) return TownJoinResult.Loading;

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
        if (player && Phase == TowerPhase.Fight)
        {
            fallen.Add(player);
            // the boss forgets them: once back up they sit the attempt out, and it does not come after them
            var boss = TowerFloor.Loaded ? TowerFloor.Loaded.Boss : null;
            if (boss) boss.RemoveAttacker(player);
        }
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

    // ---- Votes ----------------------------------------------------------------------------------------------

    private sealed class Vote
    {
        public readonly HashSet<string> Voters = new HashSet<string>();
        public float Started;
    }

    /// <summary>Seconds a vote stays open after its first voice.</summary>
    public const float VoteSeconds = 120f;

    /// <summary>
    ///     Voices that decide a vote (docs/tower-design.md, section 16): a quarter of everyone in the tower, at least three
    ///     (everyone, when fewer are in it): 3 for up to 12 players, 5 for 20, 10 for 40.
    /// </summary>
    public static int VotesNeeded(int inTower) => inTower <= 0 ? 1 : Mathf.Min(inTower, Mathf.Max(3, Mathf.CeilToInt(inTower * 0.25f)));

    /// <summary>
    ///     A player in the tower votes for <paramref name="key"/> ("boss", "climb", "close", "floor 7"): the voices still
    ///     missing, 0 when this one decided it (the vote is then over).
    /// </summary>
    private int AddVote(string key, PlayerController player)
    {
        if (!votes.TryGetValue(key, out var vote) || Time.time - vote.Started > VoteSeconds)
            votes[key] = vote = new Vote { Started = Time.time };
        vote.Voters.Add(player.Id.ToString());
        var missing = Mathf.Max(0, VotesNeeded(AllPlayers().Count) - vote.Voters.Count);
        if (missing == 0) votes.Remove(key);
        return missing;
    }

    private static bool CanVote(PlayerController player) => player && player.towerHandler != null && player.towerHandler.InTower;

    // ---- Climbing on and closing -----------------------------------------------------------------------------

    public enum ClimbResult { Climbing, Voted, NotInTower, NobodyInTown, FightUnderWay, NotReady }

    /// <summary>
    ///     !tower climb: everyone in the town goes up to the stream's floor (loading it when it is not). The streamer or a
    ///     moderator at once; anyone in the tower votes for it otherwise.
    /// </summary>
    public ClimbResult Climb(PlayerController player, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        var inTown = TownPlayers();
        if (inTown.Count == 0) return ClimbResult.NobodyInTown;
        if (Phase == TowerPhase.Fight) return ClimbResult.FightUnderWay;
        if (State == null) { if (!stateRequested) RefreshState(); return ClimbResult.NotReady; }
        if (!canForce)
        {
            if (!CanVote(player)) return ClimbResult.NotInTower;
            votesNeeded = AddVote("climb", player);
            if (votesNeeded > 0) return ClimbResult.Voted;
        }
        votes.Remove("climb");
        foreach (var p in inTown) Join(p);
        return ClimbResult.Climbing;
    }

    public enum CloseResult { Closed, Voted, NotInTower, Empty, FightUnderWay }

    /// <summary>
    ///     !tower close: everyone in the tower goes back to what they were doing (the floor and the town unload once
    ///     empty, the stream's progress stays). The streamer or a moderator at once, also during a boss attempt; anyone in
    ///     the tower votes for it otherwise, not during an attempt.
    /// </summary>
    public CloseResult Close(PlayerController player, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        var everyone = AllPlayers();
        if (everyone.Count == 0 && pendingJoins.Count == 0 && pendingTownJoins.Count == 0) return CloseResult.Empty;
        if (!canForce)
        {
            if (!CanVote(player)) return CloseResult.NotInTower;
            if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight) return CloseResult.FightUnderWay;
            votesNeeded = AddVote("close", player);
            if (votesNeeded > 0) return CloseResult.Voted;
        }
        votes.Clear();
        if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight)
        {
            EndAttempt();
            Phase = TowerPhase.Open;
        }
        pendingJoins.Clear();
        pendingTownJoins.Clear();
        foreach (var p in everyone) Leave(p);
        return CloseResult.Closed;
    }

    // ---- Boss attempts -------------------------------------------------------------------------------------

    public enum BossCallResult { Started, Voted, NotOpen, NotLoaded, UnderWay, Regrouping, NotInTower }

    /// <summary>
    ///     !tower boss: the streamer or a moderator starts the countdown at once; anyone else in the tower votes, and
    ///     <see cref="VotesNeeded"/> votes start it. <paramref name="votesNeeded"/> is what is still missing after a vote.
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
            if (!CanVote(player)) return BossCallResult.NotInTower;
            votesNeeded = AddVote("boss", player);
            if (votesNeeded > 0) return BossCallResult.Voted;
        }

        StartCountdown();
        return BossCallResult.Started;
    }

    private void StartCountdown()
    {
        votes.Remove("boss");
        participants.Clear();
        fallen.Clear();
        foreach (var p in Players()) participants.Add(p);
        Phase = TowerPhase.Countdown;
        phaseEnds = Time.time + Tower.BossCountdownSeconds;
        Announce(Localization.MSG_TOWER_BOSS_COUNTDOWN, CurrentFloor, Tower.BossCountdownSeconds, Definition.Boss.Name);
    }

    private void StartFight()
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

        // the first time the stream fights this boss: its intro first (still the countdown: a join meanwhile counts)
        if (PlayBossIntro(boss, def, false)) return;
        BeginFight(boss);
    }

    private async void BeginFight(EnemyController boss)
    {
        var floor = TowerFloor.Loaded;
        var def = Definition;
        participants.RemoveWhere(p => !p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower);
        if (!floor || def == null || !boss || floor.Boss != boss || participants.Count == 0)
        {
            EndAttempt();
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
            Won(def, boss);
            return;
        }

        var standing = participants.Count(p => p && !p.isDestroyed && p.towerHandler.InTower && !p.Stats.IsDead && !fallen.Contains(p));
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
                if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || fallen.Contains(p)) continue;
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
                if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || fallen.Contains(p)) continue;
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
            if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || p == holding || fallen.Contains(p)) continue;
            var d = Vector3.Distance(p.Position, boss.transform.position);
            if (d > bestDistance) { bestDistance = d; best = p; }
        }
        return best;
    }

    private async void Won(TowerFloorDefinition def, EnemyController boss)
    {
        var floorNumber = CurrentFloor;
        var ids = participants.Where(p => p && !p.isDestroyed).Select(p => p.Id).Distinct().ToArray();
        var players = ids.Length;
        var standing = participants.Where(p => p && !p.isDestroyed && !p.Stats.IsDead && p.towerHandler.InTower).ToList();
        // the body stays for its fall (slow motion when the cutscene plays), then goes
        EndAttempt(BossFallSeconds + 1.5f);
        Phase = TowerPhase.Open;
        Announce(Localization.MSG_TOWER_BOSS_DOWN, def.Boss.Name, floorNumber);
        PlayBossFall(boss, def, floorNumber);
        foreach (var p in standing) p.Cheer();

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

        // the next floor: after a moment to celebrate, everyone goes down to the town to rest and train until the
        // stream goes on (!tower climb)
        var next = Tower.Get(StreamFloor);
        if (AllCleared || next == null)
        {
            Announce(Localization.MSG_TOWER_TOP, floorNumber);
            return;
        }
        var town = StreamTown;
        if (town == null)
        {
            // no town for it (should not happen): straight on, as before the towns
            StartCoroutine(ClimbRoutine(next.Number));
            return;
        }
        Announce(Localization.MSG_TOWER_TO_TOWN, (int)ClimbDelay, town.Name, next.Number, next.Name);
        StartCoroutine(AfterClearRoutine(def, next, town));
    }

    /// <summary>
    ///     After a clear: the players on the floor go to the town of the next floor's band (the new band's town after a
    ///     tenth floor), and the floor gets ready for the next one: the same layout fills for it at once, another goes
    ///     now and loads when they climb (!tower climb).
    /// </summary>
    private IEnumerator AfterClearRoutine(TowerFloorDefinition cleared, TowerFloorDefinition next, TowerTownDefinition town)
    {
        climbing = true;
        yield return new WaitForSeconds(ClimbDelay);
        var moved = 0;
        foreach (var p in Players())
        {
            if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight) break;
            var r = JoinTown(p, town);
            // another town is open with people in it: they go there instead, to be with the others
            if (r == TownJoinResult.OtherOpen) r = JoinTown(p);
            if (r == TownJoinResult.Joined || r == TownJoinResult.Loading) moved++;
        }
        while (TownLoading) yield return null;
        var floor = TowerFloor.Loaded;
        if (floor && Players().Count == 0)
        {
            if (next.Layout == cleared.Layout)
            {
                CurrentFloor = next.Number;
                floor.Apply(next);
            }
            else
            {
                yield return UnloadRoutine();
            }
        }
        climbing = false;
        // a new band's town, the first time the stream gets there: a fly-over with its name
        var arrived = TowerTown.Loaded;
        if (moved > 0 && arrived && arrived.Town == town && Tower.TownFor(cleared.Number) != town) PlayArrival(arrived, false);
        if (moved > 0) Announce(Localization.MSG_TOWER_IN_TOWN, TowerTown.Loaded && TowerTown.Loaded.Town != null ? TowerTown.Loaded.Town.Name : town.Name, next.Number, next.Name, VotesNeeded(AllPlayers().Count));
    }

    public enum FloorChangeResult { Changed, Climbing, Same, Locked, Busy, Voted, NotInTower }

    /// <summary>
    ///     !tower floor N from chat: the streamer or a moderator changes the floor at once (<see cref="ChangeFloor"/>);
    ///     anyone else in the tower votes for that floor.
    /// </summary>
    public FloorChangeResult RequestFloor(PlayerController player, int number, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        var def = Tower.Get(number);
        if (def == null || number < 1 || number > StreamFloor) return FloorChangeResult.Locked;
        if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight || Phase == TowerPhase.Loading || climbing || Loading) return FloorChangeResult.Busy;
        if (TowerFloor.Loaded && number == CurrentFloor) return FloorChangeResult.Same;
        if (!canForce)
        {
            if (!CanVote(player)) return FloorChangeResult.NotInTower;
            votesNeeded = AddVote("floor " + number, player);
            if (votesNeeded > 0) return FloorChangeResult.Voted;
        }
        return ChangeFloor(number);
    }

    /// <summary>A floor below the stream's, where the players train (its quests and boss count only on the stream's floor).</summary>
    public bool Training => CurrentFloor > 0 && CurrentFloor != StreamFloor;

    /// <summary>
    ///     !tower floor N (the streamer and moderators): the players on the floor go to floor N, one the stream has
    ///     reached, to train there (docs/tower-design.md, training on cleared floors); !tower floor alone takes them back
    ///     to the stream's floor. The same layout fills again at once; another one is a climb like after a clear. One
    ///     floor is loaded at a time, so everyone in the tower goes. Not during a boss countdown or fight.
    /// </summary>
    public FloorChangeResult ChangeFloor(int number)
    {
        var def = Tower.Get(number);
        if (def == null || number < 1 || number > StreamFloor) return FloorChangeResult.Locked;
        if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight || Phase == TowerPhase.Loading || climbing || Loading) return FloorChangeResult.Busy;
        var floor = TowerFloor.Loaded;
        if (floor && number == CurrentFloor) return FloorChangeResult.Same;
        if (!floor)
        {
            // nobody in the tower: the floor loads now and !tower joins it
            LoadFloor(number);
            return FloorChangeResult.Changed;
        }
        if (floor.Definition != null && def.Layout == floor.Definition.Layout)
        {
            CurrentFloor = number;
            floor.Apply(def);
            foreach (var p in Players()) p.towerHandler.FloorChanged(floor);
            return FloorChangeResult.Changed;
        }
        StartCoroutine(ClimbRoutine(number));
        return FloorChangeResult.Climbing;
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

    /// <param name="bossDelay">Seconds before the boss goes: its fall is shown after a win.</param>
    private void EndAttempt(float bossDelay = 0f)
    {
        HideRing();
        slamAt = -1f;
        spitAt = -1f;
        if (spitRing) spitRing.SetActive(false);
        StopBossIntro();
        var floor = TowerFloor.Loaded;
        if (floor) floor.DespawnBoss(bossDelay);
        foreach (var p in participants) if (p && p.towerHandler != null) p.towerHandler.BossFightEnded();
        participants.Clear();
        fallen.Clear();
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

    // ---- Cutscenes --------------------------------------------------------------------------------------------

    /// <summary>How long the opening (over Mossgate and up the tower) and a town's arrival take, the name included.</summary>
    public const float OpeningSeconds = 20f, ArrivalSeconds = 15f;

    /// <summary>The tower's first opening is playing: joins wait for it (as for Ravebridge's rising).</summary>
    public bool Opening { get; private set; }

    private readonly List<(PlayerController Player, bool Town, TowerTownDefinition Wanted)> waitingForOpening = new List<(PlayerController, bool, TowerTownDefinition)>();
    private bool openingTried;

    // what the stream has seen: kept on this PC, per streamer (as Ravebridge's rising)
    private string StreamKey => Game && Game.RavenNest != null ? Game.RavenNest.UserId.ToString() : "local";
    private string OpeningKey => "tower.opened." + StreamKey;
    private string ArrivalKey(TowerTownDefinition town) => "tower.arrived." + town.Key + "." + StreamKey;

    /// <summary>
    ///     A join from the world while the opening is to play or playing: it waits (true) and goes through after it. The
    ///     opening starts with the first join of a stream that has not seen it, once per session at most (it is skipped
    ///     when the camera is busy or cutscenes are off, and tried again next session).
    /// </summary>
    private bool WaitForOpening(PlayerController player, bool town, TowerTownDefinition wanted)
    {
        if (!Opening)
        {
            if (openingTried || State == null || !SettingsApplier.Cutscenes || PlayerPrefs.GetInt(OpeningKey, 0) == 1) return false;
            var g = Game;
            if (!g || !g.Camera || !g.Camera.CinematicFree || TowerCutscene.Playing || RavebridgeReveal.Playing) return false;
            // Mossgate is where it starts: not while another town is open (somebody is already in the tower)
            if (TownLoading || (TowerTown.Loaded && TowerTown.Loaded.Town != Tower.TownFor(1))) return false;
            openingTried = true;
            Opening = true;
            StartCoroutine(OpeningRoutine());
        }
        if (!waitingForOpening.Any(w => w.Player == player)) waitingForOpening.Add((player, town, wanted));
        return true;
    }

    private IEnumerator OpeningRoutine()
    {
        var played = false;
        var foot = Tower.TownFor(1);
        // Mossgate, loaded for it when it is not (nobody walks in; it goes again when nobody comes)
        if (foot != null && !TowerTown.Loaded && !TownLoading)
        {
            loadingTown = foot;
            yield return LoadTownRoutine(ScenePath(foot.Layout));
        }
        var town = TowerTown.Loaded;
        if (town && town.Town == foot)
        {
            var finished = false;
            played = TowerCutscene.Play(this, town.CutscenePoses(true), OpeningSeconds, Localization.UI_TOWER_OPENING_TITLE, Localization.UI_TOWER_OPENING_LINE,
                town.TitleColor, town.TitleOutline, null, () => finished = true);
            while (played && !finished) yield return null;
        }
        if (played)
        {
            PlayerPrefs.SetInt(OpeningKey, 1);
            PlayerPrefs.Save();
        }
        Opening = false;
        var waiting = waitingForOpening.ToArray();
        waitingForOpening.Clear();
        foreach (var (player, toTown, wanted) in waiting)
        {
            if (!player || player.isDestroyed) continue;
            if (toTown) JoinTown(player, wanted);
            else Join(player);
        }
    }

    /// <summary>The town's fly-over with its name, the first time the stream gets there (or again when <paramref name="replay"/>).</summary>
    private bool PlayArrival(TowerTown town, bool replay)
    {
        var definition = town ? town.Town : null;
        if (definition == null || !SettingsApplier.Cutscenes) return false;
        var key = ArrivalKey(definition);
        if (!replay && PlayerPrefs.GetInt(key, 0) == 1) return false;
        var band = Tower.Bands.FirstOrDefault(b => b.Number == definition.Band);
        var line = band != null ? string.Format(Localization.UI_TOWER_TOWN_LINE, band.FirstFloor, band.LastFloor, band.Name) : "";
        // at the end the camera goes to someone in the town
        return TowerCutscene.Play(this, town.CutscenePoses(false), ArrivalSeconds, definition.Name, line, town.TitleColor, town.TitleOutline,
            () => TownPlayers().FirstOrDefault(), () =>
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            });
    }

    // ---- The bosses' cutscenes: the intro the first time, the fall every time

    /// <summary>How long a boss's intro and its fall take, and how long their names show at the end.</summary>
    public const float BossIntroSeconds = 7f, BossIntroTitleSeconds = 4.2f, BossFallSeconds = 4.5f, BossFallTitleSeconds = 3.4f;

    /// <summary>The earliest the boss taunts in its intro, and how slow its fall is.</summary>
    public const float TauntAt = 1.1f, FallSpeed = 0.35f;

    /// <summary>The boss's name in its intro: a menacing red.</summary>
    public static readonly Color BossColor = new Color(1f, 0.42f, 0.32f), BossOutline = new Color(0.24f, 0.03f, 0.02f);

    private EnemyController introBoss;
    private bool introPlaying;
    private TowerBossTaunts.Playing taunt;

    private string BossKey(int floor) => "tower.boss." + floor + "." + StreamKey;

    /// <summary>
    ///     The boss's intro, the first time the stream fights it (or again when <paramref name="replay"/>): the camera comes
    ///     in from the players' side to below its face while it taunts (a cheer, a roar, the cow eats), with its name and
    ///     its line. The fight starts after it (<see cref="BeginFight"/>); a replay sends the boss away again. False when
    ///     it does not play: seen, cutscenes off, the camera busy.
    /// </summary>
    private bool PlayBossIntro(EnemyController boss, TowerFloorDefinition def, bool replay)
    {
        if (!boss || def == null || introBoss || !SettingsApplier.Cutscenes) return false;
        var key = BossKey(def.Number);
        if (!replay && PlayerPrefs.GetInt(key, 0) == 1) return false;
        var (positions, forwards) = BossShot(boss, false);
        var line = string.IsNullOrEmpty(def.Boss.Line)
            ? string.Format(Localization.UI_TOWER_BOSS_FLOOR, def.Number)
            : string.Format(Localization.UI_TOWER_BOSS_QUOTE, def.Boss.Line);
        var model = def.Boss.Model;
        var taunted = false;
        // timed to end as the camera settles under its face and its name shows (a loop, the cow eating, from early on)
        var entry = TowerBossTaunts.Get() ? TowerBossTaunts.Get().For(model) : null;
        var tauntAt = entry != null && !entry.Loops ? Mathf.Max(TauntAt, BossIntroSeconds - entry.Seconds - 0.4f) : TauntAt;
        introBoss = boss;
        introPlaying = TowerCutscene.Play(this, positions, forwards, BossIntroSeconds, def.Boss.Name, line, BossColor, BossOutline,
            SomeoneToWatch, () =>
            {
                StopTaunt();
                var wasFor = introBoss;
                introBoss = null;
                introPlaying = false;
                if (!wasFor) return; // the attempt ended meanwhile
                if (!replay)
                {
                    PlayerPrefs.SetInt(key, 1);
                    PlayerPrefs.Save();
                }
                var floor = TowerFloor.Loaded;
                if (replay)
                {
                    if (floor && floor.Boss == wasFor && Phase == TowerPhase.Open) floor.DespawnBoss();
                    return;
                }
                if (Phase == TowerPhase.Countdown) BeginFight(wasFor);
            }, BossIntroTitleSeconds, 96, t =>
            {
                // played through: back to its own idle
                if (taunt != null && taunt.Done) StopTaunt();
                if (taunted || t < tauntAt) return;
                taunted = true;
                taunt = TowerBossTaunts.Play(boss, model);
            }, top: true);
        if (!introPlaying)
        {
            introBoss = null;
            return false;
        }
        // the countdown shows the time the intro takes (a !tower now answers with it)
        if (Phase == TowerPhase.Countdown) phaseEnds = Time.time + BossIntroSeconds;
        return true;
    }

    private void StopTaunt()
    {
        taunt?.Stop();
        taunt = null;
    }

    /// <summary>The attempt ended during the intro (the tower closed): it stops, and no fight follows.</summary>
    private void StopBossIntro()
    {
        StopTaunt();
        if (!introBoss) return;
        introBoss = null;
        if (introPlaying) TowerCutscene.Stop();
        introPlaying = false;
    }

    /// <summary>
    ///     The boss has fallen: a few seconds round it while it falls in slow motion, "Floor 7 cleared". Every clear, when
    ///     cutscenes are on and the camera is free; the players cheer meanwhile.
    /// </summary>
    private bool PlayBossFall(EnemyController boss, TowerFloorDefinition def, int floorNumber)
    {
        if (!boss || def == null || !SettingsApplier.Cutscenes) return false;
        var (positions, forwards) = BossShot(boss, true);
        var played = TowerCutscene.Play(this, positions, forwards, BossFallSeconds, string.Format(Localization.UI_TOWER_FLOOR_CLEARED, floorNumber),
            string.Format(Localization.UI_TOWER_BOSS_FALLS, def.Boss.Name), CutsceneScreen.Gold, CutsceneScreen.GoldOutline, SomeoneToWatch, null,
            BossFallTitleSeconds, 100, top: true);
        // its death from the start, slowly, held at the end (it goes with the body)
        if (played && TowerBossTaunts.Fall(boss, FallSpeed, BossFallSeconds - 0.8f) == null)
        {
            var animator = boss.GetComponentInChildren<Animator>();
            if (animator) animator.speed = FallSpeed;
        }
        return played;
    }

    /// <summary>Who the camera goes to after a boss's cutscene: someone in the fight, or anyone on the floor.</summary>
    private PlayerController SomeoneToWatch()
    {
        foreach (var p in participants) if (p && !p.isDestroyed && !p.Stats.IsDead && p.towerHandler.InTower) return p;
        return Players().FirstOrDefault(p => p && !p.isDestroyed && !p.Stats.IsDead);
    }

    /// <summary>
    ///     The camera's path round a boss, sized to it: the intro comes in from the players' side, high and wide, to low
    ///     under its face; the fall circles slowly round it from one side to the other. Every pose is pulled in front of
    ///     a wall between it and the boss, and kept off the ground.
    /// </summary>
    private static (Vector3[] Positions, Vector3[] Forwards) BossShot(EnemyController boss, bool fall)
    {
        var t = boss.transform;
        var centre = t.position;
        var bounds = new Bounds(centre, Vector3.zero);
        var any = false;
        foreach (var r in boss.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        // room to grow: a taunt stands it up or rears it (the bunny's ears left the shot)
        var height = Mathf.Clamp(1.25f * (bounds.max.y - centre.y), 1.2f, 25f);
        // how far away: by its height, and some of its length (a dragon's tail should not push the camera out of the room),
        // and never right in its face
        var size = Mathf.Clamp(Mathf.Max(height, 0.45f * Mathf.Max(bounds.size.x, bounds.size.z)), 3f, 25f);
        var front = t.forward;
        front.y = 0f;
        front = front.sqrMagnitude > 1e-4f ? front.normalized : Vector3.forward;
        var up = Vector3.up;

        (Vector3, Vector3)[] Poses(Vector3 f)
        {
            var r2 = Vector3.Cross(Vector3.up, f);
            return fall
                ? new[]
                {
                    (centre + f * 1.9f * size + r2 * 1.3f * size + up * 0.75f * height, centre + up * 0.4f * height),
                    (centre + f * 1.6f * size - r2 * 0.2f * size + up * 0.5f * height, centre + up * 0.3f * height),
                    (centre + f * 1.2f * size - r2 * 1.1f * size + up * 0.35f * height, centre + up * 0.25f * height),
                }
                : new[]
                {
                    (centre + f * 3.2f * size + r2 * 1.6f * size + up * 1.4f * height, centre + up * 0.55f * height),
                    (centre + f * 2.0f * size + r2 * 0.55f * size + up * 0.55f * height, centre + up * 0.65f * height),
                    (centre + f * 1.1f * size - r2 * 0.15f * size + up * 0.18f * height, centre + up * 0.62f * height),
                };
        }

        // from its front if nothing is in the way, else turned round it to the clearest side (a wall, a pillar, a ruin in
        // front of the boss's spot hid it): every pose is looked at from the boss's middle and from its feet
        var feet = centre + up * 0.15f * height;
        (Vector3, Vector3)[] poses = null;
        var best = int.MaxValue;
        foreach (var angle in new[] { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 140f, -140f, 180f })
        {
            var candidate = Poses(Quaternion.AngleAxis(angle, Vector3.up) * front);
            var blocked = 0;
            foreach (var (at, look) in candidate) blocked += (Blocked(look, at) ? 1 : 0) + (Blocked(feet, at) ? 1 : 0);
            if (blocked >= best) continue;
            best = blocked;
            poses = candidate;
            if (blocked == 0) break;
        }
        var positions = new Vector3[poses.Length];
        var forwards = new Vector3[poses.Length];
        for (var i = 0; i < poses.Length; i++)
        {
            var (at, look) = poses[i];
            at = Clear(look, at);
            positions[i] = at;
            forwards[i] = (look - at).normalized;
        }
        return (positions, forwards);
    }

    /// <summary>
    ///     <paramref name="at"/> pulled in front of anything solid between it and <paramref name="look"/> (a wall, a
    ///     pillar; not a character), and at least a little over the ground under it.
    /// </summary>
    private static Vector3 Clear(Vector3 look, Vector3 at)
    {
        var d = at - look;
        var distance = d.magnitude;
        if (distance > 0.01f)
        {
            var nearest = distance;
            foreach (var hit in Physics.RaycastAll(look, d / distance, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= nearest || Character(hit.collider)) continue;
                nearest = hit.distance;
            }
            if (nearest < distance) at = look + d / distance * Mathf.Max(0.5f, nearest - 0.6f);
        }
        foreach (var hit in Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Character(hit.collider)) continue;
            if (at.y < hit.point.y + 0.7f && hit.point.y < at.y + 3f) at.y = hit.point.y + 0.7f;
        }
        return at;
    }

    /// <summary>
    ///     Something solid (not a character, not the ground) on or near the line between <paramref name="from"/> and
    ///     <paramref name="to"/>: a pillar beside the line still fills a third of the shot, so it is a wide sweep.
    /// </summary>
    private static bool Blocked(Vector3 from, Vector3 to)
    {
        var d = to - from;
        var distance = d.magnitude;
        if (distance < 0.01f) return false;
        var radius = Mathf.Clamp(0.06f * distance, 0.3f, 1.5f);
        foreach (var hit in Physics.SphereCastAll(from, radius, d / distance, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            // touching at the start (the floor round its feet), the ground and floors, characters: not in the way
            if (hit.distance <= 0f || hit.normal.y > 0.6f || hit.collider is TerrainCollider || Character(hit.collider)) continue;
            return true;
        }
        return false;
    }

    private static bool Character(Collider c) =>
        c.GetComponentInParent<EnemyController>() || c.GetComponentInParent<PlayerController>() || c.GetComponentInParent<BattlePetController>();

    /// <summary>The cutscenes play again (the settings' "Introductions").</summary>
    public void ForgetCutscenesSeen()
    {
        PlayerPrefs.DeleteKey(OpeningKey);
        foreach (var town in Tower.Towns) PlayerPrefs.DeleteKey(ArrivalKey(town));
        for (var floor = 1; floor <= Tower.TopFloor; floor++) PlayerPrefs.DeleteKey(BossKey(floor));
        PlayerPrefs.Save();
        openingTried = false;
    }

    /// <summary>
    ///     The debug menu's "Boss Intro": the loaded floor's boss steps out and plays its intro, then goes again. Only
    ///     while the floor trains (no attempt).
    /// </summary>
    public bool ReplayBossIntro()
    {
        var floor = TowerFloor.Loaded;
        var def = floor ? floor.Definition : null;
        if (!floor || def == null || Phase != TowerPhase.Open || TowerCutscene.Playing || introBoss || floor.Boss) return false;
        var boss = floor.SpawnBoss();
        if (!boss) return false;
        if (PlayBossIntro(boss, def, true)) return true;
        floor.DespawnBoss();
        return false;
    }

    /// <summary>The debug menu's "Boss Falls": the loaded floor's boss steps out and falls at once, as after a clear.</summary>
    public bool ReplayBossFall()
    {
        var floor = TowerFloor.Loaded;
        var def = floor ? floor.Definition : null;
        if (!floor || def == null || Phase != TowerPhase.Open || TowerCutscene.Playing || introBoss || floor.Boss) return false;
        var boss = floor.SpawnBoss();
        if (!boss) return false;
        var movement = boss.GetComponent<EnemyMovementController>();
        if (movement) movement.Die();
        PlayBossFall(boss, def, def.Number);
        floor.DespawnBoss(BossFallSeconds + 1.5f);
        return true;
    }

    /// <summary>The debug menu's "Tower Opens": the opening again, now (Mossgate loads for it when no town is open).</summary>
    public bool ReplayOpening()
    {
        if (Opening || TowerCutscene.Playing || TownLoading || (TowerTown.Loaded && TowerTown.Loaded.Town != Tower.TownFor(1))) return false;
        PlayerPrefs.DeleteKey(OpeningKey);
        openingTried = true;
        Opening = true;
        StartCoroutine(OpeningRoutine());
        return true;
    }

    /// <summary>The debug menu's "Town Arrival": the open town's fly-over (the stream's town loads for it when none is open).</summary>
    public void ReplayArrival()
    {
        if (Opening || TowerCutscene.Playing) return;
        StartCoroutine(ReplayArrivalRoutine());
    }

    private IEnumerator ReplayArrivalRoutine()
    {
        var town = StreamTown;
        if (!TowerTown.Loaded && !TownLoading && town != null)
        {
            loadingTown = town;
            yield return LoadTownRoutine(ScenePath(town.Layout));
        }
        while (TownLoading) yield return null;
        PlayArrival(TowerTown.Loaded, true);
    }

    // ---- Every frame -----------------------------------------------------------------------------------------

    private void Update()
    {
        var g = Game;
        if (!g) return;
        if (!stateRequested && g.RavenNest != null && g.RavenNest.SessionStarted) RefreshState();

        if (Phase == TowerPhase.Countdown && PhaseSecondsLeft <= 0f && !introBoss) StartFight();
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
        // the place the camera is at, or near enough to look at from outside (a wide shot): the nearest loaded one
        TowerPlace floor = null;
        if (camera) floor = PlaceNear(camera.transform.position, LookMargin);
        var over = floor;
        SetLook(over);
        UpdateHaze(over, camera);
        SkyClouds.Place = null;
        if (!over) return;

        // GameManager sets these from potato mode every frame, so they are switched off every frame while the camera is
        // over the floor; when it leaves, the next frame puts them back as the settings say
        Ocean.Suppressed = true;
        // the world's clouds stay only as a place's own cloud sea (and only if the setting and potato mode allow them);
        // its low-poly clouds stand in while they are off
        if (over.CloudSea)
        {
            var c = over.Centre.y;
            SkyClouds.Place = new SkyClouds.Layer
            {
                Floor = c + over.CloudSeaBottom,
                Ceiling = c + over.CloudSeaTop,
                Coverage = over.CloudSeaCoverage,
                PuffSize = over.CloudSeaPuff,
                Dark = over.CloudSeaDark,
                Brightness = over.CloudSeaBrightness,
                // the far cloud melts into the place's fog, as its ground does
                HazeStart = 100f,
                HazeRate = Mathf.Max(0.002f, over.FogDensity * 0.45f),
                HazeColor = over.FogColor
            };
        }
        else SkyClouds.Suppressed = true;
        if (over.MeshClouds) over.MeshClouds.SetActive(!(over.CloudSea && SkyClouds.Active));
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

    /// <summary>How far down under a place the haze floor lies: below its lowest ground.</summary>
    private const float HazeDepth = 3f;

    /// <summary>The ring round the camera that hides the horizon past the sheet (inside the camera's 1300 m reach).</summary>
    private const float HazeRing = 1000f;

    private GameObject haze;
    private GameObject hazeRing;
    private Material hazeMaterial;
    private Mesh hazeBand;

    /// <summary>
    ///     A wide flat sheet in the place's fog colour under the camera while its look is on: from high up (a cutscene, a
    ///     wide shot) the ground of a town or an open floor ends a hundred metres out, and past it the sky's dark lower
    ///     half showed as a blue sea round a floating plateau. The fog turns the sheet into haze out to the horizon, and a
    ///     low ring of the same colour round the camera covers the last strip below the horizon, where the sheet is too far
    ///     to draw.
    /// </summary>
    private void UpdateHaze(TowerPlace place, Camera camera)
    {
        if (!place || !camera)
        {
            if (haze && haze.activeSelf) haze.SetActive(false);
            if (hazeRing && hazeRing.activeSelf) hazeRing.SetActive(false);
            return;
        }
        if (!haze)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) return;
            hazeMaterial = new Material(shader) { name = "Tower Haze" };
            // the ring is seen from inside
            if (hazeMaterial.HasProperty("_Cull")) hazeMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            GameObject Part(PrimitiveType type, string name)
            {
                var go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.hideFlags = HideFlags.DontSave;
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = hazeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return go;
            }
            haze = Part(PrimitiveType.Quad, "Tower Haze");
            haze.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            haze.transform.localScale = new Vector3(2f * HazeRing + 200f, 2f * HazeRing + 200f, 1f);
            // an open band one high and two across (a cylinder's lid would cover the sky)
            hazeRing = Part(PrimitiveType.Quad, "Tower Haze Ring");
            hazeRing.GetComponent<MeshFilter>().sharedMesh = hazeBand = Band(64);
        }
        if (!haze.activeSelf) haze.SetActive(true);
        if (!hazeRing.activeSelf) hazeRing.SetActive(true);
        var eye = camera.transform.position;
        var floorY = place.Centre.y - Mathf.Max(HazeDepth, place.HazeDepth);
        haze.transform.position = new Vector3(eye.x, floorY, eye.z);
        // from the sheet up to the camera's height: its top edge is the horizon (higher, it stood as a wall over the hills)
        var top = Mathf.Max(eye.y, floorY + 1f);
        hazeRing.transform.position = new Vector3(eye.x, floorY, eye.z);
        hazeRing.transform.localScale = new Vector3(HazeRing, top - floorY, HazeRing);
        if (hazeMaterial.HasProperty("_BaseColor")) hazeMaterial.SetColor("_BaseColor", place.FogColor);
        else hazeMaterial.color = place.FogColor;
    }

    /// <summary>A ring of <paramref name="segments"/> quads, radius 1, from 0 up to 1 high, with no top or bottom.</summary>
    private static Mesh Band(int segments)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var triangles = new int[segments * 6];
        for (var i = 0; i <= segments; i++)
        {
            var a = i * Mathf.PI * 2f / segments;
            vertices[i * 2] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            vertices[i * 2 + 1] = new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a));
            if (i == segments) break;
            var t = i * 6;
            triangles[t] = i * 2; triangles[t + 1] = i * 2 + 1; triangles[t + 2] = i * 2 + 2;
            triangles[t + 3] = i * 2 + 1; triangles[t + 4] = i * 2 + 3; triangles[t + 5] = i * 2 + 2;
        }
        var mesh = new Mesh { name = "Tower Haze Band", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 2f));
        return mesh;
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
