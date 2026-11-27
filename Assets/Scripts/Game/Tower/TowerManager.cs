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
///     The tower (docs/tower-design.md, and docs/tower-worlds-proposal.md section 12 for its shape): players see floors,
///     each a world with a town, and the parts of a floor. In this code a "floor" is still one part (a
///     <see cref="TowerFloorDefinition"/>, a layout scene filled for it) and its number is the part's step, counted
///     through every floor; RavenNest.Combat.Tower maps steps to floors and parts, and <see cref="TowerWords"/> says
///     them the players' way. Loads a part's scene next to MainWorld when the first player enters with
///     !tower, moves the players who join into it and back, counts the part's objectives (kills, hidden things found;
///     gathering is the server's), runs boss attempts against its guardian (a mini boss, or the floor boss on a floor's
///     last part), scales the fights by the group's pressure, and unloads the part when nobody has been in it for a while. MainWorld is never unloaded: the players who did not join keep
///     training as if nothing happened. While the camera is over the floor, the floor's light and fog replace the
///     world's, and the world-wide systems that follow the camera (sea, clouds, cloud shadows, weather) are switched off.
///     The stream's progress (its floor, the quests, clears) is the server's (api/tower); this keeps a copy.
///     Up to three parts are loaded at once, one in potato mode (<see cref="Area"/>, docs/tower-worlds-proposal.md
///     section 13): the part the climb is on, where its objectives count and its guardian is fought, and cleared parts
///     that a player went to by themselves to train (!tower part 3). Each stands in a place of its own, 1.5 km from
///     the next, and goes when nobody has been on it for a while.
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

    /// <summary>How long a spit's glob flies (it lands as the hit comes), and how far a slam pushes the players it hits.</summary>
    public const float SpitFlight = 0.8f, SlamKnockback = 1.1f;

    /// <summary>Seconds after a won fight before the players climb to a floor of another layout.</summary>
    public const float ClimbDelay = 12f;

    /// <summary>What the last load and unload measured, for the tests.</summary>
    public string Report => report.ToString();

    /// <summary>A fighting area is loading.</summary>
    public bool Loading
    {
        get
        {
            foreach (var a in areas) if (a.Loading) return true;
            return false;
        }
    }

    /// <summary>The scene of the part the climb is on, while it is loaded.</summary>
    public string LoadedScene => PushArea?.ScenePath;

    /// <summary>
    ///     Where the climb is: no part of it loaded, loading, open (players train on it and work on its objectives), or
    ///     a boss attempt called (the countdown) or fought. The cleared parts players train on beside it
    ///     (<see cref="Areas"/>) have no phase of their own.
    /// </summary>
    public TowerPhase Phase
    {
        get
        {
            if (attempt != TowerPhase.Closed) return attempt;
            var area = PushArea ?? clearing;
            if (area == null) return TowerPhase.Closed;
            return area.Loading ? TowerPhase.Loading : TowerPhase.Open;
        }
    }

    // the boss attempt: Countdown or Fight, Closed while there is none, and the area it is fought on
    private TowerPhase attempt = TowerPhase.Closed;
    private Area fightArea;

    // the area whose guardian just fell, until everyone on it is in the town and it is made ready for the next part
    private Area clearing;

    /// <summary>The stream's progress as the server last answered, or null before it did.</summary>
    public TowerState State { get; private set; }

    /// <summary>The step of the climb's part while it is loaded (or wrapped up after its clear), 0 when it is not.</summary>
    public int CurrentFloor => (PushArea ?? clearing)?.Step ?? 0;

    public TowerFloorDefinition Definition => Tower.Get(CurrentFloor);

    /// <summary>Seconds left of the countdown or of the attempt.</summary>
    public float PhaseSecondsLeft => Mathf.Max(0f, phaseEnds - Time.time);

    /// <summary>Seconds until the next attempt can start after a lost one.</summary>
    public float RetrySecondsLeft => Mathf.Max(0f, retryAt - Time.time);

    public IReadOnlyCollection<PlayerController> Participants => participants;

    private readonly StringBuilder report = new StringBuilder();
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
    private float phaseEnds;
    private float retryAt;
    private float nextSlam;
    private float slamAt = -1f;
    // the warnings on the ground (TowerBossFx.Telegraph), when each began, and whether the boss has swung and spat yet
    private GameObject slamRing, spitRing;
    private Material slamMark, spitMark;
    private float slamFrom, spitFrom;
    private bool slamSwung, spitSwung, spitThrown;
    private Vector3 slamCentre;
    private EnemyMovementController fightMovement;
    private float nextSpit;
    private float spitAt = -1f;
    private Vector3 spitSpot;
    private int addsCalled;
    private bool enraged;
    private float fightStarted;

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
        go.AddComponent<TowerLook>();
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
        TowerPlace best = null;
        var nearest = float.MaxValue;
        var floors = TowerFloor.All;
        for (var i = 0; i < floors.Count; i++)
        {
            var d = Reach(floors[i], eye, margin);
            if (d >= 0f && d < nearest) { nearest = d; best = floors[i]; }
        }
        var town = Reach(TowerTown.Loaded, eye, margin);
        if (town >= 0f && town < nearest) best = TowerTown.Loaded;
        return best;
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
        var push = AreaAt(StreamFloor);
        if (state.BossOpen && !wasOpen && attempt == TowerPhase.Closed && !AllCleared && push != null && !push.Loading && push.Definition != null)
            Announce(Localization.MSG_TOWER_BOSS_OPEN, TowerWords.Floor(push.Step), TowerWords.Part(push.Step), TowerWords.BossKind(push.Step), push.Definition.Boss.Name);
    }

    /// <summary>How far one of the stream's part's objectives is, as the server last said.</summary>
    public int Progress(string objective)
    {
        if (State?.Objectives == null) return 0;
        foreach (var p in State.Objectives) if (p.Id == objective) return p.Count;
        return 0;
    }

    /// <summary>The floor the stream works on: the lowest not cleared, or the top floor when all are.</summary>
    public int StreamFloor => State == null ? 1 : Mathf.Clamp(State.Floor, 1, Tower.TopFloor);

    /// <summary>The stream has cleared every floor that exists so far.</summary>
    public bool AllCleared => State != null && State.Floor > Tower.TopFloor;

    /// <summary>"Defeat 300 Feral Bunnies (120/300), Gather 250 timber (done), Find 3 Stolen Boots (1/3)": the part's counted objectives.</summary>
    public string QuestText()
    {
        var floor = Tower.Get(StreamFloor);
        if (floor == null) return "";
        if (AllCleared) return Localization.MSG_TOWER_QUESTS_CLEARED;
        var parts = new List<string>();
        foreach (var q in floor.Objectives)
        {
            if (!q.Counted) continue;
            var count = Progress(q.Id);
            parts.Add(q.Text + (count >= q.Amount ? " (done)" : " (" + count.ToString("N0", CultureInfo.InvariantCulture) + "/" + q.Amount.ToString("N0", CultureInfo.InvariantCulture) + ")"));
        }
        return string.Join(", ", parts);
    }

    // ---- Areas: the parts that are loaded -------------------------------------------------------------------

    /// <summary>
    ///     One fighting place of the tower that is loaded (docs/tower-worlds-proposal.md section 13): a layout scene
    ///     filled for one part. The part the climb is on has one while anyone pushes there; a part already cleared has
    ///     one while someone trains on it.
    /// </summary>
    public sealed class Area
    {
        /// <summary>The part it is filled for (its step).</summary>
        public int Step { get; internal set; }

        /// <summary>
        ///     Where it stands (<see cref="SlotOffsets"/>): every layout scene is built on the same spot, so each loaded
        ///     one is moved to a place of its own. -1 until it has one.
        /// </summary>
        public int Slot { get; internal set; } = -1;

        public string ScenePath { get; internal set; }
        public Scene Scene { get; internal set; }
        public TowerFloor Floor { get; internal set; }
        public bool Loading { get; internal set; }
        public bool Unloading { get; internal set; }

        /// <summary>On its way out: nobody new goes in, and it goes as soon as it is empty.</summary>
        public bool Closing { get; internal set; }

        public TowerFloorDefinition Definition => Tower.Get(Step);

        /// <summary>The average strength of the players on it beyond fresh level-synced players: 1 to 1.5.</summary>
        public double GroupStrength { get; internal set; } = 1;

        /// <summary>Its pressure: the players on it against the group the part is made for, times their strength.</summary>
        public double Pressure { get; internal set; } = 1;

        // everyone who asked to go there while it loads
        internal readonly List<PlayerController> Pending = new List<PlayerController>();

        /// <summary>This player asked to go there and waits for it to load.</summary>
        public bool Waits(PlayerController player) => Pending.Contains(player);

        /// <summary>
        ///     Damage, healing and damage taken on this part since its players arrived (or since it was counted anew):
        ///     the meters window shows it when the tower panel's Meters button asks. A boss fight has its own
        ///     (<see cref="CombatMeter.Tower"/>).
        /// </summary>
        public readonly CombatMeter Meter = new CombatMeter();
        internal float EmptySince = -1f;
        internal float NextPressure;

        // its navmesh, when TowerManager had to add it (a second copy of a layout in the editor)
        internal UnityEngine.AI.NavMeshDataInstance Nav;

        // the heads on it at each look of the last seconds (TowerPace.Heads), for what a kill there is worth
        internal readonly Queue<(float At, double Heads)> HeadLog = new Queue<(float, double)>();

        /// <summary>
        ///     The most heads that were on it in the last <see cref="HeadsMemorySeconds"/> seconds (a player counts as
        ///     one, one far below the part as a share): what its kills are reported with.
        /// </summary>
        public double RecentHeads
        {
            get
            {
                var most = 0.0;
                foreach (var entry in HeadLog) if (entry.Heads > most) most = entry.Heads;
                return most;
            }
        }
    }

    /// <summary>
    ///     Seconds a part remembers the heads that were on it. A kill counts for more with few players on the part and
    ///     for less with many (RavenNest.Combat.TowerPace; the server works it out from the heads the game reports with
    ///     the kills). The heads are the most there were in these seconds, so stepping off a part for the moment a
    ///     monster falls changes nothing: whoever leaves counts until they have been gone this long, and whoever comes
    ///     counts at once.
    /// </summary>
    public const float HeadsMemorySeconds = 20f;

    /// <summary>
    ///     How many fighting areas are loaded at once beside the town: the part the climb is on and two more, opened
    ///     by whoever asks first. One in potato mode (each costs about 20 MB and its monsters' updates).
    /// </summary>
    public const int MaxAreas = 3;
    public const int PotatoAreas = 1;

    /// <summary>How many areas may be open now: <see cref="MaxAreas"/>, or one in potato mode.</summary>
    public int AreaLimit => Game && Game.PotatoMode ? PotatoAreas : MaxAreas;

    /// <summary>
    ///     Where each loaded area stands, from the spot every layout scene was built on ((0, 0, -6000)): the first stays
    ///     there, the others stand 1.5 km apart further west, past the camera's reach, so no area is ever seen from
    ///     another, and two parts that share a layout can be open at once (the scene is loaded a second time and moved).
    ///     One place more than <see cref="MaxAreas"/>: a part opens beside the full ones before the one it replaces is gone.
    /// </summary>
    private static readonly Vector3[] SlotOffsets =
    {
        Vector3.zero, new Vector3(-1500f, 0f, 0f), new Vector3(-3000f, 0f, 0f), new Vector3(-4500f, 0f, 0f),
    };

    private readonly List<Area> areas = new List<Area>();

    /// <summary>The fighting areas that are loaded or loading.</summary>
    public IReadOnlyList<Area> Areas => areas;

    /// <summary>The loaded (or loading) area of a part, or null. One on its way out does not count.</summary>
    public Area AreaAt(int step)
    {
        foreach (var a in areas) if (a.Step == step && !a.Unloading && !a.Closing) return a;
        return null;
    }

    public Area AreaOf(TowerFloor floor)
    {
        if (!floor) return null;
        foreach (var a in areas) if (a.Floor == floor) return a;
        return null;
    }

    /// <summary>The area a player fights on, or null (in the town, in the world).</summary>
    public Area AreaOf(PlayerController player) =>
        player && player.towerHandler != null && player.towerHandler.InTower ? AreaOf(player.towerHandler.Floor) : null;

    /// <summary>
    ///     The area of the part the climb is on, while it is loaded: where the objectives count and the guardian is
    ///     fought. !tower goes there.
    /// </summary>
    public Area PushArea => fightArea ?? (State == null ? null : AreaAt(StreamFloor));

    /// <summary>The floor (the scene's root) of the part the climb is on, while it is loaded.</summary>
    public TowerFloor PushFloor => PushArea?.Floor;

    /// <summary>This area is the climb's part (not a cleared part someone trains on).</summary>
    public bool IsClimb(Area area) => area != null && (area == fightArea || area == clearing || (State != null && area.Step == StreamFloor));

    /// <summary>The player the stream is watching, or null (the free camera).</summary>
    public PlayerController Watched
    {
        get
        {
            var g = Game;
            return g && g.Camera && g.Camera.Observer ? g.Camera.Observer.ObservedPlayer : null;
        }
    }

    /// <summary>The part the stream is looking at: the one the watched player is on, else the climb's.</summary>
    public Area WatchedArea => AreaOf(Watched) ?? CameraArea ?? PushArea ?? clearing;

    /// <summary>The players on an area.</summary>
    public List<PlayerController> PlayersOn(Area area)
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g || area == null || !area.Floor) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower && p.towerHandler.Floor == area.Floor) list.Add(p);
        return list;
    }

    /// <summary>How many players are on an area.</summary>
    public int CountOn(Area area)
    {
        var g = Game;
        if (!g || area == null || !area.Floor) return 0;
        var count = 0;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower && p.towerHandler.Floor == area.Floor) count++;
        return count;
    }

    /// <summary>How many players are on a part right now (0 when it is not loaded): the floor map shows it.</summary>
    public int CountOnPart(int step) => CountOn(AreaAt(step));

    /// <summary>Everyone on a part of the tower, whichever it is (not the town).</summary>
    public List<PlayerController> FightingPlayers()
    {
        var list = new List<PlayerController>();
        var g = Game;
        if (!g) return list;
        foreach (var p in g.Players.GetAllPlayers())
            if (p && p.towerHandler != null && p.towerHandler.InTower && p.towerHandler.Floor) list.Add(p);
        return list;
    }

    // ---- Meters (the dungeon's meters window, for the tower) ---------------------------------------------------

    /// <summary>The area the last boss fight was fought on: its meter (<see cref="CombatMeter.Tower"/>) shows while the camera is there.</summary>
    public Area MeterArea { get; private set; }

    /// <summary>The loaded part the game's camera is at, or null (in the town, in the world).</summary>
    public Area CameraArea
    {
        get
        {
            var camera = Camera.main;
            return camera ? AreaOf(PlaceNear(camera.transform.position, LookMargin) as TowerFloor) : null;
        }
    }

    /// <summary>The town the game's camera is at, or null (on a part, in the world).</summary>
    public TowerTown CameraTown
    {
        get
        {
            var camera = Camera.main;
            return camera ? PlaceNear(camera.transform.position, LookMargin) as TowerTown : null;
        }
    }

    /// <summary>The game's camera is at a place of the tower (a part or the town).</summary>
    public bool CameraInTower
    {
        get
        {
            var camera = Camera.main;
            return camera && PlaceNear(camera.transform.position, LookMargin);
        }
    }

    /// <summary>
    ///     The meter a player's blows count on now: the boss fight's for everyone on its part while it is fought
    ///     (also one who fell and got up again, whose blows still land on the boss), else their part's own; null
    ///     outside the tower's parts. On the hit path of every player: a few comparisons.
    /// </summary>
    public static CombatMeter MeterOf(PlayerController player)
    {
        var tower = Instance;
        if (!tower || !player || player.towerHandler == null || !player.towerHandler.InTower) return null;
        var area = tower.AreaOf(player.towerHandler.Floor);
        if (area == null) return null;
        if (tower.attempt == TowerPhase.Fight && area == tower.fightArea) return CombatMeter.Tower;
        return area.Meter.Running ? area.Meter : null;
    }

    /// <summary>A player's blow took <paramref name="took"/> health off a monster (EnemyController.TakeDamage): onto their meter.</summary>
    public static void MeterHit(PlayerController player, EnemyController enemy, int took)
    {
        if (took <= 0) return;
        var meter = MeterOf(player);
        if (meter == null) return;
        meter.AddDamage(player, took);
        var tower = Instance;
        if (meter == CombatMeter.Tower && tower.fightArea != null && tower.fightArea.Floor && tower.fightArea.Floor.Boss == enemy) meter.TargetDamage += took;
    }

    // a part's own meter starts anew: its first players arrived, or it became another part
    private static void BeginStretch(Area area)
    {
        var def = area.Definition;
        area.Meter.Begin();
        area.Meter.Title = def != null ? string.Format(Localization.UI_TOWER_METER_TITLE, TowerWords.Floor(area.Step), TowerWords.Part(area.Step), def.Name) : null;
        area.Meter.Caption = Localization.UI_METER_TRAINING;
    }

    /// <summary>The part's own meter counts from now (the meters window's button).</summary>
    public void ResetStretch(Area area)
    {
        if (area != null && areas.Contains(area)) BeginStretch(area);
    }

    // those who went to a cleared part by themselves (!tower part 3)
    private readonly HashSet<PlayerController> ownWay = new HashSet<PlayerController>();

    /// <summary>
    ///     Whether a player went to a cleared part by themselves (!tower part 3) and has not joined the climb again:
    ///     !tower all leaves them where they are, as it leaves the players in the town. Not in potato mode, where the
    ///     one open part is everyone's.
    /// </summary>
    public bool OnOwnWay(PlayerController player) =>
        AreaLimit > 1 && player && ownWay.Contains(player) && player.towerHandler != null && player.towerHandler.InTower;

    /// <summary>
    ///     The climbing group: everyone on a part of the tower but those who went to one by themselves. !tower all
    ///     moves them, and only they vote for it.
    /// </summary>
    public List<PlayerController> GroupPlayers()
    {
        var list = FightingPlayers();
        for (var i = list.Count - 1; i >= 0; i--) if (OnOwnWay(list[i])) list.RemoveAt(i);
        return list;
    }

    // nobody on it, nobody on the way, nothing going on: it may give its place to another part
    private bool Idle(Area a) =>
        !a.Loading && !a.Unloading && a != fightArea && a != clearing && a.Pending.Count == 0 && CountOn(a) == 0;

    /// <summary>
    ///     Whether a part that is not loaded can open now. The climb's part always has its place; the cleared parts
    ///     share what is left (two of three; in potato mode there is one place for all). An area nobody is in gives way
    ///     (<paramref name="evict"/>).
    /// </summary>
    private bool Room(int step, out Area evict)
    {
        evict = null;
        var limit = AreaLimit;
        var climb = State != null && step == StreamFloor;
        var scene = ScenePath(LayoutFor(Tower.Get(step)));
        int live = 0, others = 0;
        Area idleOther = null, idleAny = null;
        foreach (var a in areas)
        {
            if (a.Unloading || a.Closing) continue;
            live++;
            var isClimb = State != null && a.Step == StreamFloor;
            if (!isClimb) others++;
            if (!Idle(a)) continue;
            // rather one of the same layout: it is filled again where it stands, with nothing to load
            if (idleAny == null || (a.ScenePath == scene && idleAny.ScenePath != scene)) idleAny = a;
            if (!isClimb && (idleOther == null || (a.ScenePath == scene && idleOther.ScenePath != scene))) idleOther = a;
        }
        var otherLimit = Mathf.Max(1, limit - 1);
        if (live < limit && (climb || others < otherLimit)) return true;
        evict = climb || others < otherLimit ? idleAny : idleOther;
        if (evict == null) return false;
        var evictIsOther = !(State != null && evict.Step == StreamFloor);
        return live - 1 < limit && (climb || others - (evictIsOther ? 1 : 0) < otherLimit);
    }

    private int FreeSlot()
    {
        for (var i = 0; i < SlotOffsets.Length; i++)
        {
            var taken = false;
            foreach (var a in areas) if (a.Slot == i) { taken = true; break; }
            if (!taken) return i;
        }
        return -1;
    }

    // a part opens: its scene loads, and everyone who asks meanwhile waits in its list
    private Area Open(int step)
    {
        var area = new Area { Step = step, Loading = true, ScenePath = ScenePath(LayoutFor(Tower.Get(step))) };
        areas.Add(area);
        StartCoroutine(LoadRoutine(area));
        return area;
    }

    // a loaded area becomes another part of the same layout where it stands: filled again, nothing to load
    private void Retarget(Area area, int step)
    {
        area.Step = step;
        area.EmptySince = -1f;
        if (!area.Floor) return;
        area.Floor.Apply(Tower.Get(step));
        BeginStretch(area);
        // anyone on it finds themselves on the new part (on a part of rooms: back at its first room)
        foreach (var p in PlayersOn(area)) p.towerHandler.FloorChanged(area.Floor);
        RefreshPressure(area, true);
    }

    // an area nobody is in gives its place to another part
    private Area Replace(Area old, int step)
    {
        if (!old.Loading && old.Floor && old.ScenePath == ScenePath(LayoutFor(Tower.Get(step))))
        {
            Retarget(old, step);
            return old;
        }
        StartCoroutine(UnloadRoutine(old));
        return Open(step);
    }

    private enum GoResult { Joined, Loading, Here, Busy, Full }

    /// <summary>
    ///     A player goes to a part: at once when its area is loaded, when it has loaded otherwise (they stay where they
    ///     are meanwhile). Opens the area if there is room for it. From the town or another part the way back to the
    ///     world is kept.
    /// </summary>
    private GoResult GoTo(PlayerController player, int step, out Area area)
    {
        area = AreaAt(step);
        var handler = player.towerHandler;
        if (area != null && handler.InTower && handler.Floor && handler.Floor == area.Floor) return GoResult.Here;
        if (area != null && area.Pending.Contains(player)) return GoResult.Loading;
        if (handler.InTower ? handler.InBossFight || player.Stats.IsDead : !handler.CanEnter) return GoResult.Busy;
        if (area == null)
        {
            if (Room(step, out var evict)) area = evict != null ? Replace(evict, step) : Open(step);
            else
            {
                // from one cleared part to another: the part they leave does not count. Alone there (the last of a
                // crowd) it becomes the new part where it stands when it has the same layout; else the new part opens
                // one over the limit for a moment and the old one goes as soon as its last player has left
                // (UpdateAreas). A crowd on a cleared part could not move on while three were open, even its last
                // player (playtest 2026-11-26)
                var from = LeavingCleared(player);
                if (from == null) return GoResult.Full;
                if (CountOn(from) == 1 && from.Pending.Count == 0 && from.Floor && from.ScenePath == ScenePath(LayoutFor(Tower.Get(step))))
                {
                    Retarget(from, step);
                    return GoResult.Joined;
                }
                if (FreeSlot() < 0 || LiveAreas() > AreaLimit) return GoResult.Full;
                area = Open(step);
            }
        }
        return Send(player, area);
    }

    // the cleared part (not the climb's, nothing going on there) this player is on, or null
    private Area LeavingCleared(PlayerController player)
    {
        var handler = player.towerHandler;
        if (AreaLimit <= 1 || handler == null || !handler.InTower || !handler.Floor) return null;
        var from = AreaOf(handler.Floor);
        if (from == null || from.Loading || from.Unloading || from == fightArea || from == clearing) return null;
        return State != null && from.Step == StreamFloor ? null : from;
    }

    private int LiveAreas()
    {
        var live = 0;
        foreach (var a in areas) if (!a.Unloading && !a.Closing) live++;
        return live;
    }

    // into an area, or onto its list while it loads
    private GoResult Send(PlayerController player, Area area)
    {
        var handler = player.towerHandler;
        // with the group again, whoever sends them (Visit marks its own right after)
        ownWay.Remove(player);
        foreach (var a in areas) if (a != area) a.Pending.Remove(player);
        pendingTownJoins.Remove(player);
        // off to another part during a countdown: out of the attempt
        if (fightArea != null && area != fightArea) participants.Remove(player);
        if (area.Loading || !area.Floor)
        {
            if (!area.Pending.Contains(player)) area.Pending.Add(player);
            return GoResult.Loading;
        }
        var first = CountOn(area) == 0;
        if (!handler.Enter(area.Floor)) return GoResult.Busy;
        if (first || !area.Meter.Running) BeginStretch(area);
        area.EmptySince = -1f;
        if (area == fightArea && attempt == TowerPhase.Countdown) participants.Add(player);
        return GoResult.Joined;
    }

    // ---- Joining and leaving --------------------------------------------------------------------------------

    public enum JoinResult { Joined, Loading, Busy, AlreadyIn, FightUnderWay, NotReady, ToTown, Elsewhere }

    /// <summary>
    ///     !tower: to the part the climb is on, loading it first if nobody is there; from the world, from the town or
    ///     from a cleared part the player trains on. Players can join while it trains and during a boss countdown,
    ///     not during the fight. <see cref="JoinResult.Elsewhere"/>: no place was free for the climb's part (potato
    ///     mode keeps one part open), and they joined the part that is open.
    /// </summary>
    public JoinResult Join(PlayerController player)
    {
        if (!player || player.towerHandler == null) return JoinResult.NotReady;
        var handler = player.towerHandler;
        var push = PushArea;
        // there already, or on the way
        if (push != null && ((handler.InTower && handler.Floor && handler.Floor == push.Floor) || push.Pending.Contains(player))) return JoinResult.AlreadyIn;
        if (attempt == TowerPhase.Fight) return JoinResult.FightUnderWay;
        if (handler.InTower ? handler.InBossFight || player.Stats.IsDead : !handler.CanEnter) return JoinResult.Busy;
        // the first time the stream opens the tower: its cutscene first, then they go in
        if (!handler.InTower && WaitForOpening(player, false, null)) return JoinResult.Loading;
        if (State == null)
        {
            if (!stateRequested) RefreshState();
            return JoinResult.NotReady;
        }

        // between parts everyone waits in the town: a newcomer goes to them (!tower again goes up alone); so does one
        // who comes while the part just cleared is wrapped up
        if (!handler.InTower && attempt != TowerPhase.Countdown)
        {
            var nobodyUp = push == null || (CountOn(push) == 0 && push.Pending.Count == 0);
            if (clearing != null || (nobodyUp && TowerTown.Loaded && !TownLoading && TownPlayers().Count > 0))
            {
                var town = JoinTown(player);
                if (town == TownJoinResult.Joined || town == TownJoinResult.Loading) return JoinResult.ToTown;
            }
        }

        switch (GoTo(player, StreamFloor, out _))
        {
            case GoResult.Joined: return JoinResult.Joined;
            case GoResult.Loading: return JoinResult.Loading;
            case GoResult.Here: return JoinResult.AlreadyIn;
            case GoResult.Full:
                // no place for the climb's part: to the part the group is on (else where most players are)
                Area open = null;
                var most = -1;
                var group = GroupPlayers();
                foreach (var a in areas)
                {
                    if (a.Unloading || a.Closing) continue;
                    var weight = CountOn(a);
                    foreach (var p in group) if (a.Floor && p.towerHandler.Floor == a.Floor) weight += 1000;
                    if (weight > most) { most = weight; open = a; }
                }
                if (open == null) return JoinResult.Busy;
                var there = Send(player, open);
                return there == GoResult.Joined || there == GoResult.Loading ? JoinResult.Elsewhere : JoinResult.Busy;
            default: return JoinResult.Busy;
        }
    }

    public enum VisitResult { Joined, Loading, Here, Busy, Full, Locked, NotReady }

    /// <summary>
    ///     !tower part 3: only this player goes to a part the stream has cleared, to train there, like sailing to an
    ///     island (docs/tower-worlds-proposal.md section 13); the climb goes on elsewhere. Opens the part if a place is
    ///     free for it (<see cref="VisitResult.Full"/> when none is). The climb's own part is <see cref="Join"/>.
    /// </summary>
    public VisitResult Visit(PlayerController player, int step, out Area area)
    {
        area = null;
        if (!player || player.towerHandler == null) return VisitResult.NotReady;
        if (State == null)
        {
            if (!stateRequested) RefreshState();
            return VisitResult.NotReady;
        }
        if (Tower.Get(step) == null || step < 1 || step > StreamFloor) return VisitResult.Locked;
        // the tower's first opening plays: a moment
        if (Opening) return VisitResult.NotReady;
        var target = AreaAt(step);
        // the part whose guardian just fell is being wrapped up
        if (target != null && target == clearing) return VisitResult.Busy;
        var went = GoTo(player, step, out area);
        // by themselves on a cleared part: the group's moves pass them by until they join the climb again (!tower)
        if ((went == GoResult.Joined || went == GoResult.Loading) && step != StreamFloor) ownWay.Add(player);
        switch (went)
        {
            case GoResult.Joined: return VisitResult.Joined;
            case GoResult.Loading: return VisitResult.Loading;
            case GoResult.Here: return VisitResult.Here;
            case GoResult.Full: return VisitResult.Full;
            default: return VisitResult.Busy;
        }
    }

    /// <summary>Takes a player off the lists of those waiting to be let in; true when they were on one.</summary>
    public bool CancelWaiting(PlayerController player)
    {
        var waiting = false;
        foreach (var a in areas) waiting |= a.Pending.Remove(player);
        waiting |= pendingTownJoins.Remove(player);
        waiting |= waitingForOpening.RemoveAll(w => w.Player == player) > 0;
        return waiting;
    }

    // someone waits for a part to load
    private bool AnyPending()
    {
        foreach (var a in areas) if (a.Pending.Count > 0) return true;
        return false;
    }

    public void Leave(PlayerController player)
    {
        // out of the attempt too: someone who joined the countdown and left still got the clear's coins and chest
        participants.Remove(player);
        fallen.Remove(player);
        foreach (var a in areas) a.Pending.Remove(player);
        waitingForOpening.RemoveAll(w => w.Player == player);
        pendingTownJoins.Remove(player);
        ownWay.Remove(player);
        if (player && player.towerHandler != null) player.towerHandler.Exit();
    }

    /// <summary>The players on the part the climb is on (or the one being wrapped up after its clear).</summary>
    public List<PlayerController> Players() => PlayersOn(PushArea ?? clearing);

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
            foreach (var a in areas) a.Pending.Remove(player);
            participants.Remove(player);
            ownWay.Remove(player);
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

    /// <summary>
    ///     A layout scene to use for every floor instead of the floors' own, or null (always null for players): the
    ///     admin debug menu's way to try a layout that is not in the climb yet, such as the band 4 great hall
    ///     (docs/tower-design.md section 22). The floor's monsters, quests and boss stay the stream's floor's.
    /// </summary>
    public static string LayoutOverride { get; private set; }

    /// <summary>The scene a floor loads: its own layout, or the debug menu's <see cref="LayoutOverride"/>.</summary>
    public static string LayoutFor(TowerFloorDefinition floor) => !string.IsNullOrEmpty(LayoutOverride) ? LayoutOverride : floor?.Layout;

    /// <summary>
    ///     The debug menu: every part loads <paramref name="layout"/> from now on (null: their own again). A part that is
    ///     loaded opens again in it beside the old one, and everyone on it goes over when it is ready (not during a
    ///     boss attempt).
    /// </summary>
    public bool PreviewLayout(string layout)
    {
        if (attempt != TowerPhase.Closed || clearing != null) return false;
        foreach (var a in areas) if (a.Loading || a.Unloading) return false;
        LayoutOverride = string.IsNullOrEmpty(layout) ? null : layout;
        foreach (var area in areas.ToArray())
        {
            if (area.ScenePath == ScenePath(LayoutFor(area.Definition))) continue;
            area.Closing = true;
            var fresh = Open(area.Step);
            foreach (var p in PlayersOn(area)) fresh.Pending.Add(p);
            foreach (var p in area.Pending) if (!fresh.Pending.Contains(p)) fresh.Pending.Add(p);
            area.Pending.Clear();
        }
        return true;
    }

    /// <summary>Loads a part's area when there is room for it and it is not loaded (the tests; players come by <see cref="Join"/> and <see cref="Visit"/>).</summary>
    public void LoadFloor(int number)
    {
        if (Tower.Get(number) == null || AreaAt(number) != null || !Room(number, out var evict)) return;
        if (evict != null) Replace(evict, number);
        else Open(number);
    }

    // one scene loads at a time: the scene that arrives is that area's (two parts can share a layout, so its path
    // does not say whose it is)
    private bool sceneLoading;

    private static bool IsScene(Scene scene, string path) => scene.path == path || scene.name == path;

    // A loaded layout scene moved to its area's place. Its objects woke up where the scene was built, so the root is
    // switched off for the move: what registered itself by its place (the navmesh, the grass, roads, light shafts)
    // registers again where it stands now. The first place is where the scenes are built: nothing to do there.
    private static void Place(Scene scene, int slot)
    {
        var offset = slot >= 0 && slot < SlotOffsets.Length ? SlotOffsets[slot] : Vector3.zero;
        if (offset == Vector3.zero) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            var active = root.activeSelf;
            if (active) root.SetActive(false);
            root.transform.position += offset;
            if (active) root.SetActive(true);
        }
    }

    // the navmesh of each layout, kept from the first time its scene was loaded
    private readonly Dictionary<string, UnityEngine.AI.NavMeshData> navMeshes = new Dictionary<string, UnityEngine.AI.NavMeshData>();

    // Two parts that share a layout are two copies of its scene, and both use the scene's one baked navmesh. A
    // surface adds it where it stands, so in a build each copy has its own. In the editor a surface whose navmesh
    // another loaded surface already has drops it when it loads (the editor's guard against copied surfaces), and the
    // second copy had no navmesh at all: nobody on it could walk. So it is added here for a copy whose surface has none.
    private void AddNavMesh(Area area, TowerFloor floor)
    {
        var surface = floor.GetComponentInChildren<Unity.AI.Navigation.NavMeshSurface>(true);
        if (!surface) return;
        if (surface.navMeshData)
        {
            navMeshes[area.ScenePath] = surface.navMeshData;
            return;
        }
        if (navMeshes.TryGetValue(area.ScenePath, out var data) && data)
            area.Nav = UnityEngine.AI.NavMesh.AddNavMeshData(data, surface.transform.position, surface.transform.rotation);
    }

    /// <summary>Loads an area's layout scene next to MainWorld, moves it to its place and fills it for its part.</summary>
    private IEnumerator LoadRoutine(Area area)
    {
        while (sceneLoading) yield return null;
        sceneLoading = true;
        // every place taken by areas on their way out: a moment
        while ((area.Slot = FreeSlot()) < 0) yield return null;
        report.Clear();
        var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        var started = Time.realtimeSinceStartup;
        var worst = 0f;
        var frames = 0;
        var scene = area.ScenePath;

        // told as the scene arrives, after its objects woke up and before their first frame: it is moved then
        var arrived = default(Scene);
        var got = false;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (s, mode) =>
        {
            if (got || !IsScene(s, scene)) return;
            got = true;
            arrived = s;
            Place(s, area.Slot);
        };
        SceneManager.sceneLoaded += onLoaded;

        AsyncOperation op;
#if UNITY_EDITOR
        // in the editor the floor scenes do not have to be in the build's scene list
        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Additive));
#else
        op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
#endif
        if (op == null)
        {
            SceneManager.sceneLoaded -= onLoaded;
            sceneLoading = false;
            report.AppendLine("could not start loading " + scene);
            area.Loading = false;
            area.Pending.Clear();
            areas.Remove(area);
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
        SceneManager.sceneLoaded -= onLoaded;
        if (!got)
        {
            // not told: the newest loaded scene of that layout that no area has
            for (var i = SceneManager.sceneCount - 1; i >= 0 && !got; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded || !IsScene(s, scene)) continue;
                var taken = false;
                foreach (var a in areas) if (a != area && a.Scene == s) { taken = true; break; }
                if (taken) continue;
                got = true;
                arrived = s;
                Place(s, area.Slot);
            }
        }
        sceneLoading = false;

        TowerFloor floor = null;
        if (got)
        {
            foreach (var root in arrived.GetRootGameObjects())
            {
                floor = root.GetComponentInChildren<TowerFloor>(true);
                if (floor) break;
            }
        }
        area.Scene = arrived;
        area.Floor = floor;
        area.Loading = false;
        if (floor) AddNavMesh(area, floor);
        if (floor && area.Step > 0) floor.Apply(Tower.Get(area.Step));

        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("loaded " + scene + " in " + loadedIn.ToString("0.00", CultureInfo.InvariantCulture) + " s over " + frames + " frames, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory +" + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture) + " MB, place " + area.Slot + ", floor "
            + (floor ? floor.name + " with " + floor.Enemies.Count + " enemies" : "missing"));

        // it was asked to go while it loaded, or its scene has no floor: it goes again
        if (!floor || area.Unloading)
        {
            area.Pending.Clear();
            if (!area.Unloading) StartCoroutine(UnloadRoutine(area));
            yield break;
        }

        // everyone who asked while it loaded goes in now
        var joined = 0;
        foreach (var p in area.Pending.ToArray())
            if (p && !p.isDestroyed && p.towerHandler != null && p.towerHandler.Enter(floor)) joined++;
        area.Pending.Clear();
        area.EmptySince = -1f;
        BeginStretch(area);
        var def = area.Definition;
        // the climb's part opening is told to everyone; a cleared part someone went to train on is their own business
        if (joined > 0 && def != null && State != null && area.Step == StreamFloor)
            Announce(Localization.MSG_TOWER_OPENED, TowerWords.Floor(area.Step), TowerWords.Part(area.Step), def.Name, joined, QuestText());
        RefreshPressure(area, true);
    }

    /// <summary>Everyone in a fighting area back to the world, then every area's scene goes (the tests).</summary>
    public void Unload()
    {
        foreach (var a in areas.ToArray()) if (!a.Unloading) StartCoroutine(UnloadRoutine(a));
    }

    /// <summary>Whoever is still on an area goes back to the world, then its scene goes.</summary>
    private IEnumerator UnloadRoutine(Area area)
    {
        if (area.Unloading) yield break;
        area.Unloading = true;
        // a load under way finishes first (LoadRoutine sees that it is to go)
        while (area.Loading) yield return null;
        if (area == fightArea) EndAttempt();
        if (area == clearing) clearing = null;
        foreach (var p in PlayersOn(area)) p.towerHandler.Exit();
        area.Pending.Clear();
        FlushKills();
        if (area.Nav.valid) area.Nav.Remove();
        var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        var started = Time.realtimeSinceStartup;
        var worst = 0f;
        if (area.Scene.IsValid() && area.Scene.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(area.Scene);
            while (op != null && !op.isDone)
            {
                yield return null;
                worst = Mathf.Max(worst, Time.unscaledDeltaTime);
            }
        }
        areas.Remove(area);
        var cleanup = UnityEngine.Resources.UnloadUnusedAssets();
        while (!cleanup.isDone) yield return null;
        GC.Collect();
        yield return null;
        var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        report.AppendLine("unloaded " + area.ScenePath + " in " + (Time.realtimeSinceStartup - started).ToString("0.00", CultureInfo.InvariantCulture) + " s, worst frame "
            + (worst * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, memory " + ((memoryAfter - memoryBefore) / (1024f * 1024f)).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + " MB");
    }

    // nobody on an area for a while: it goes. At once when it is on its way out or more are open than there is room
    // for (potato mode switched on, a part opened beside the full ones); never the one a boss attempt runs on.
    private void UpdateAreas()
    {
        if (ownWay.Count > 0) ownWay.RemoveWhere(Forgotten);
        if (areas.Count == 0) return;
        var limit = AreaLimit;
        var live = 0;
        foreach (var a in areas) if (!a.Unloading) live++;
        for (var i = areas.Count - 1; i >= 0; i--)
        {
            var a = areas[i];
            if (a.Unloading || a.Loading) continue;
            if (!Idle(a))
            {
                a.EmptySince = -1f;
                continue;
            }
            if (a.EmptySince < 0f) a.EmptySince = Time.time;
            if (!a.Closing && live <= limit && Time.time - a.EmptySince <= EmptyFloorSeconds) continue;
            StartCoroutine(UnloadRoutine(a));
            live--;
        }
    }

    // gone, or out of the tower and not waiting for a part to open: no longer on a way of their own
    private bool Forgotten(PlayerController player)
    {
        if (!player || player.isDestroyed || player.towerHandler == null) return true;
        if (player.towerHandler.InTower) return false;
        foreach (var a in areas) if (a.Pending.Contains(player)) return false;
        return true;
    }

    // ---- Quests ----------------------------------------------------------------------------------------------

    // the part the kills that wait to be reported were made on (the climb's part when they fell)
    private int pendingStep;

    // the heads on the part as each of those kills fell, added up, and how many kills that is
    private double pendingHeads;
    private int pendingHeadKills;

    /// <summary>What the last report to the server said ("defeat-hall-cow:3;kills:5;heads:500"); the tests read it.</summary>
    public static string LastReport { get; private set; } = "";

    /// <summary>A monster on a part fell to a player in the tower (PlayerCombat.Defeated).</summary>
    public void EnemyDefeated(EnemyController enemy, PlayerController by)
    {
        // on the part the player fights on (else on whichever loaded part the monster is one of)
        var floor = by && by.towerHandler != null ? by.towerHandler.Floor : null;
        var kind = floor ? floor.KindOf(enemy) : null;
        if (kind == null)
        {
            floor = null;
            foreach (var f in TowerFloor.All)
            {
                kind = f.KindOf(enemy);
                if (kind == null) continue;
                floor = f;
                break;
            }
        }
        if (kind == null || !floor) return;

        // the player's own quests (Mossgate's chains) count kills on any part
        if (by && by.UserId != Guid.Empty)
        {
            var key = (by.Id, kind.Id);
            characterKills.TryGetValue(key, out var mine);
            characterKills[key] = mine + 1;
        }

        // the stream's objectives count on the part the climb is on only
        var def = floor.Definition;
        if (def == null || State == null || def.Number != StreamFloor || AllCleared) return;
        if (pendingStep != def.Number)
        {
            pendingKills.Clear();
            pendingStep = def.Number;
            pendingHeads = 0;
            pendingHeadKills = 0;
        }
        // the heads on the part as this monster fell: the server counts the kill for more or less by them
        var area = AreaOf(floor);
        var heads = area != null ? area.RecentHeads : 0;
        if (heads <= 0) heads = TowerPace.Heads(StrengthsOf(PlayersOn(area), def.Number));
        pendingHeads += Math.Max(TowerPace.MinHeads, heads);
        pendingHeadKills++;
        var gathers = false;
        foreach (var q in def.Objectives)
        {
            if (q.Kind == TowerObjectiveKind.Gather) gathers = true;
            if (q.Kind != TowerObjectiveKind.Kill || q.Target != kind.Id) continue;
            pendingKills.TryGetValue(q.Id, out var n);
            pendingKills[q.Id] = n + 1;
        }
        // a gather objective counts every monster defeated on the part too (what it carried), so a stream with nobody
        // gathering is never stuck: the server adds them from this count
        if (gathers)
        {
            pendingKills.TryGetValue(Tower.AllKills, out var all);
            pendingKills[Tower.AllKills] = all + 1;
        }
    }

    // ---- Hidden things (find objectives) -----------------------------------------------------------------------

    /// <summary>
    ///     A find objective's hidden thing that is being looked for now and how long it has been searched. Things are
    ///     found one at a time; which one is next is the server's count.
    /// </summary>
    private sealed class Hidden
    {
        public int Index = -1;
        public float Searched;
        public bool Reported;
        public float ReportedAt;
    }

    private readonly Dictionary<string, Hidden> hidden = new Dictionary<string, Hidden>();
    private float nextSearch;

    /// <summary>Seconds between two looks at who stands where (the search counts in steps of this).</summary>
    private const float SearchStep = 1f;

    /// <summary>How far along the search for an objective's next hidden thing is, 0 to 1 (the panel does not show it; the tests do).</summary>
    public float SearchShare(string objective) =>
        hidden.TryGetValue(objective, out var h) && h.Index >= 0 ? Mathf.Clamp01(h.Searched / Mathf.Max(1f, Tower.FindSeconds(StreamFloor))) : 0f;

    /// <summary>Whether the last look found someone searching where the monsters are (the tests read it).</summary>
    public bool SearchingNear { get; private set; }

    /// <summary>
    ///     The hidden things of the stream's part (find objectives): they lie among the part's monsters, and players
    ///     who train there find them by themselves. A second of searching counts in full while someone stands where
    ///     the monsters are (within Tower.FindNearMeters of one of the part's monster spots: on a part of rooms, of the
    ///     rooms the group has reached) and a quarter while the players on the part are away from them (at the
    ///     arrival, waiting for a boss, getting back up), so a thing takes a fighting group the minutes it is made for
    ///     (Tower.FindSeconds: 4 on floor 1, 5 on floor 2, 6 on floor 3). Nobody types or clicks anything. Found: a
    ///     word over the finder, and the server counts it with the next report.
    ///     Before, each thing lay at one spot picked for it, and only standing near that spot counted in full. A group
    ///     roams all over a part (five players were 30 to 60 m from the spot at any look), so in play a thing took 11
    ///     to 16 minutes instead of 4.
    /// </summary>
    private void UpdateFinds()
    {
        if (Time.time < nextSearch) return;
        nextSearch = Time.time + SearchStep;
        var area = attempt == TowerPhase.Closed && State != null && clearing == null ? AreaAt(StreamFloor) : null;
        var floor = area != null && !area.Loading ? area.Floor : null;
        var def = area?.Definition;
        if (!floor || def == null || AllCleared)
        {
            if (hidden.Count > 0) hidden.Clear();
            return;
        }

        List<PlayerController> players = null;
        PlayerController finder = null;
        var near = false;
        var looked = false;
        foreach (var q in def.Objectives)
        {
            if (q.Kind != TowerObjectiveKind.Find) continue;
            var found = Progress(q.Id);
            if (found >= q.Amount) { hidden.Remove(q.Id); continue; }
            if (!hidden.TryGetValue(q.Id, out var h)) hidden[q.Id] = h = new Hidden();
            if (h.Index != found)
            {
                // the next one
                h.Index = found;
                h.Searched = 0f;
                h.Reported = false;
            }
            // told to the server and not counted yet: asked again after a while (a report can be lost or come too soon)
            if (h.Reported)
            {
                if (Time.time - h.ReportedAt > 30f) { h.Reported = false; h.Searched = Tower.FindSeconds(def.Number); }
                else continue;
            }

            // who searches, and whether anyone does so where the monsters are: looked at once for all the part's things
            if (!looked)
            {
                looked = true;
                players = PlayersOn(area);
                finder = Searcher(floor, players, out near);
                SearchingNear = near;
            }
            if (!finder) continue;
            h.Searched += SearchStep * (near ? 1f : (float)Tower.FindFarShare);
            if (h.Searched < Tower.FindSeconds(def.Number)) continue;

            h.Reported = true;
            h.ReportedAt = Time.time;
            if (pendingStep != def.Number)
            {
                pendingKills.Clear();
                pendingStep = def.Number;
            }
            pendingKills[q.Id] = 1;
            CombatPaths.Word(finder, string.Format(Localization.UI_TOWER_FOUND, q.Target), new Color(1f, 0.9f, 0.5f));
            FlushKills();
        }
    }

    /// <summary>
    ///     The player on the part who stands nearest to one of its monster spots (the one who finds a hidden thing), and
    ///     whether that is near enough to be searching where the monsters are. On a part of rooms only the rooms the
    ///     group has reached count: nothing is found through a shut door.
    /// </summary>
    private static PlayerController Searcher(TowerFloor floor, List<PlayerController> players, out bool near)
    {
        near = false;
        PlayerController best = null;
        var nearest = float.MaxValue;
        var route = floor.Route && floor.Route.Valid ? floor.Route : null;
        foreach (var p in players)
        {
            if (!p || p.isDestroyed || p.Stats.IsDead) continue;
            var position = p.Position;
            if (route)
            {
                for (var z = 0; z < route.Zones.Length && z <= route.Active; z++)
                {
                    var zone = route.Zones[z];
                    if (!zone || !zone.Spawns) continue;
                    for (var i = 0; i < zone.Spawns.childCount; ++i) Look(zone.Spawns.GetChild(i).position);
                }
            }
            else if (floor.Spawns)
            {
                for (var i = 0; i < floor.Spawns.childCount; ++i) Look(floor.Spawns.GetChild(i).position);
            }
            // nobody near a monster yet: still someone to find it at the slow rate
            if (!best) best = p;

            void Look(Vector3 spot)
            {
                var d = position - spot;
                d.y = 0f;
                if (d.sqrMagnitude >= nearest) return;
                nearest = d.sqrMagnitude;
                best = p;
            }
        }
        near = best && nearest <= Tower.FindNearMeters * Tower.FindNearMeters;
        return best;
    }

    // ---- Pressure ----------------------------------------------------------------------------------------------

    /// <summary>Seconds between two looks at the group on the part.</summary>
    private const float PressureSeconds = 4f;

    /// <summary>The average strength of the players on the climb's part beyond fresh level-synced players: 1 to 1.5.</summary>
    public double GroupStrength => (PushArea ?? clearing)?.GroupStrength ?? 1;

    /// <summary>The pressure of the climb's part: the players on it against the group it is made for, times their strength.</summary>
    public double PartPressure => (PushArea ?? clearing)?.Pressure ?? 1;

    /// <summary>A player's strength on a part beyond a fresh synced player: skill trees, gems and enchantments (RavenNest.Combat.TowerPressure).</summary>
    public static double StrengthOf(PlayerController player, int step)
    {
        if (!player || !CombatRules.Enabled) return 1;
        var sheet = CombatRules.Sheet(player);
        if (sheet == null) return 1;
        var style = player.TrainingRanged ? AttackStyle.Ranged : player.TrainingMagic ? AttackStyle.Magic : player.TrainingHealing ? AttackStyle.Healing : AttackStyle.Melee;
        var actives = player.SkillBuild != null ? player.SkillBuild.Loadout.Count : 0;
        return TowerPressure.Strength(sheet, style, step, actives);
    }

    /// <summary>
    ///     Every player's strength on a part, for the group's pressure (TowerPressure.Of): a veteran counts for more
    ///     than one fresh player, someone far below the part for less than a head.
    /// </summary>
    public static List<double> StrengthsOf(IEnumerable<PlayerController> players, int step)
    {
        var list = new List<double>();
        foreach (var p in players)
            if (p && !p.isDestroyed) list.Add(StrengthOf(p, step));
        return list;
    }

    /// <summary>
    ///     A part's monsters follow the group on it (docs/tower-worlds-proposal.md section 12), each loaded part by
    ///     itself: looked at every few seconds, and the monsters take the new numbers as they stand untouched or come
    ///     back. Not during a boss attempt on that part.
    /// </summary>
    private void RefreshPressure(bool now = false)
    {
        foreach (var a in areas) if (a != fightArea) RefreshPressure(a, now);
    }

    private void RefreshPressure(Area area, bool now)
    {
        if (area == null || area.Loading || area.Unloading || !area.Floor) return;
        if (!now && Time.time < area.NextPressure) return;
        area.NextPressure = Time.time + PressureSeconds;
        var def = area.Definition;
        if (def == null) return;
        var players = PlayersOn(area);
        // an empty part waits as it is for whoever comes next
        if (players.Count + area.Pending.Count == 0) return;
        var strengths = StrengthsOf(players, def.Number);
        // the heads on the part now, kept for a while (what its kills are worth)
        while (area.HeadLog.Count > 0 && Time.time - area.HeadLog.Peek().At > HeadsMemorySeconds) area.HeadLog.Dequeue();
        area.HeadLog.Enqueue((Time.time, TowerPace.Heads(strengths)));
        // those still on their way in count as fresh players until they are here
        for (var i = 0; i < area.Pending.Count; i++) strengths.Add(1);
        area.GroupStrength = TowerPressure.GroupStrength(strengths);
        area.Pressure = TowerPressure.Of(strengths, def.GroupSize);
        area.Floor.SetPressure(area.Pressure);
    }

    /// <summary>The pressure of a boss attempt: its participants against the group the guardian is made for, times their strength.</summary>
    private double BossPressure(TowerFloorDefinition def)
    {
        var fighters = participants.Where(p => p && !p.isDestroyed && p.towerHandler != null && p.towerHandler.InTower);
        return TowerPressure.Of(StrengthsOf(fighters, def.Number), Tower.BossGroup(def));
    }

    // ---- The last blow -----------------------------------------------------------------------------------------

    /// <summary>Blows that land within this many seconds of the one a floor boss falls to are a tie: they share its last hit.</summary>
    public const float LastBlowTie = 0.1f;

    private readonly List<(Guid Player, float At)> blows = new List<(Guid, float)>();
    private Guid killingBlow;

    // every blow on the attempt's boss: the last few are kept, and the one it fell to
    private void BossStruck(EnemyController boss, PlayerController player, bool fell)
    {
        if (!player) return;
        blows.Add((player.Id, Time.time));
        if (blows.Count > 16) blows.RemoveAt(0);
        if (fell) killingBlow = player.Id;
    }

    /// <summary>Whose blows landed as the boss fell: the killing blow first, then any within <see cref="LastBlowTie"/> of it.</summary>
    private Guid[] LastBlows()
    {
        var result = new List<Guid>();
        if (killingBlow == Guid.Empty || blows.Count == 0) return result.ToArray();
        result.Add(killingBlow);
        var at = blows[blows.Count - 1].At;
        for (var i = blows.Count - 1; i >= 0; --i)
        {
            if (at - blows[i].At > LastBlowTie) break;
            if (!result.Contains(blows[i].Player)) result.Add(blows[i].Player);
        }
        return result.ToArray();
    }

    /// <summary>A player was knocked out during a boss attempt (reported with the kills, for a hidden achievement).</summary>
    public void PlayerFell(PlayerController player, int floor)
    {
        if (player && attempt == TowerPhase.Fight)
        {
            fallen.Add(player);
            // said over them, so it reads as what it is: knocked out, and out of this fight once back up
            player.towerHandler.SitOut();
            CombatPaths.Word(player, Localization.UI_TOWER_KNOCKED_OUT, new Color(1f, 0.45f, 0.4f));
            // the boss forgets them: once back up they sit the attempt out, and it does not come after them
            var boss = fightArea != null && fightArea.Floor ? fightArea.Floor.Boss : null;
            if (boss) boss.RemoveAttacker(player);
        }
        if (player && player.UserId != Guid.Empty && attempt == TowerPhase.Fight)
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
        // with the heads that were on the part as the kills fell, on average, in hundredths
        if (pendingHeadKills > 0) counts += ";" + Tower.Heads + ":" + Mathf.RoundToInt((float)(100 * pendingHeads / pendingHeadKills)).ToString(CultureInfo.InvariantCulture);
        pendingHeads = 0;
        pendingHeadKills = 0;
        var floor = pendingStep;
        LastReport = counts;
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

        /// <summary>When its time is up (Time.time).</summary>
        public float Ends;

        /// <summary>Only a majority passes it, never the time alone (closing the tower sends everyone out).</summary>
        public bool MajorityOnly;

        /// <summary>A vote of the climbing group (where it goes): those on a part of their own are not counted.</summary>
        public bool GroupOnly;
    }

    /// <summary>
    ///     Seconds a vote runs from its first voice. One voice is enough (the user, 2026-11-21): when the time is up the
    ///     vote passes, and a majority of the players who are here passes it at once.
    /// </summary>
    public const float VoteSeconds = 60f;

    /// <summary>A player who typed no command for this long is away from the keyboard and counts toward no majority.</summary>
    public const double VoteAwayMinutes = 15;

    /// <summary>More than half of <paramref name="active"/> players, at least one.</summary>
    public static int Majority(int active) => Mathf.Max(1, active / 2 + 1);

    /// <summary>The players in the tower who are here: those who typed a command in the last <see cref="VoteAwayMinutes"/>.</summary>
    public int ActivePlayers(bool groupOnly = false)
    {
        var now = System.DateTime.UtcNow;
        var count = 0;
        foreach (var p in AllPlayers())
            if ((now - p.LastChatCommandUtc).TotalMinutes <= VoteAwayMinutes && !(groupOnly && OnOwnWay(p))) count++;
        return count;
    }

    /// <summary>The voices a vote still needs to pass at once: a majority of those who are here (its voters are).</summary>
    private int Missing(Vote vote) => Mathf.Max(0, Majority(Mathf.Max(ActivePlayers(vote.GroupOnly), vote.Voters.Count)) - vote.Voters.Count);

    /// <summary>Whether the last vote cast opened a new vote (announced to everyone) and did not join one.</summary>
    public bool VoteJustOpened { get; private set; }

    /// <summary>The key of a vote for everyone to go to a part (by its step): "part 7".</summary>
    public static string PartVote(int step) => "part " + step.ToString(CultureInfo.InvariantCulture);

    /// <summary>Seconds an open vote ("boss", "climb", "close", "part 7") has left, 0 when there is none.</summary>
    public int VoteSecondsLeft(string key) =>
        votes.TryGetValue(key, out var vote) ? Mathf.Max(0, Mathf.CeilToInt(vote.Ends - Time.time)) : 0;

    /// <summary>
    ///     The open vote that ends first, for the panel: what it is for ("boss", "climb", "close", "part 7"), the seconds
    ///     left and the voices still missing for it to pass at once.
    /// </summary>
    public bool TryGetOpenVote(out string key, out int secondsLeft, out int missing, out bool majorityOnly)
    {
        key = null; secondsLeft = 0; missing = 0; majorityOnly = false;
        Vote first = null;
        foreach (var kv in votes)
        {
            if (Time.time >= kv.Value.Ends || (first != null && kv.Value.Ends >= first.Ends)) continue;
            first = kv.Value;
            key = kv.Key;
        }
        if (first == null) return false;
        secondsLeft = Mathf.Max(0, Mathf.CeilToInt(first.Ends - Time.time));
        missing = Missing(first);
        majorityOnly = first.MajorityOnly;
        return true;
    }

    /// <summary>
    ///     A player in the tower votes for <paramref name="key"/> ("boss", "climb", "close", "part 7"): the voices still
    ///     missing for it to pass at once, 0 when this one decided it (the vote is then over). The first voice opens the
    ///     vote for <see cref="VoteSeconds"/>; when they are up it passes with the voices it has (<see cref="UpdateVotes"/>),
    ///     unless it is <paramref name="majorityOnly"/>.
    /// </summary>
    private int AddVote(string key, PlayerController player, bool majorityOnly = false, bool groupOnly = false)
    {
        VoteJustOpened = false;
        if (!votes.TryGetValue(key, out var vote) || Time.time >= vote.Ends)
        {
            votes[key] = vote = new Vote { Ends = Time.time + VoteSeconds, MajorityOnly = majorityOnly, GroupOnly = groupOnly };
            VoteJustOpened = true;
        }
        vote.Voters.Add(player.Id.ToString());
        var missing = Missing(vote);
        if (missing == 0)
        {
            votes.Remove(key);
            VoteJustOpened = false;
        }
        return missing;
    }

    private readonly List<string> endedVotes = new List<string>();

    /// <summary>Votes whose time is up: they pass with the voices they have, unless another open vote has more.</summary>
    private void UpdateVotes()
    {
        if (votes.Count == 0) return;
        endedVotes.Clear();
        foreach (var kv in votes)
            if (Time.time >= kv.Value.Ends) endedVotes.Add(kv.Key);
        foreach (var key in endedVotes)
        {
            if (!votes.TryGetValue(key, out var vote)) continue; // one that passed before it cleared the rest
            votes.Remove(key);
            if (vote.MajorityOnly) continue;
            var beaten = false;
            foreach (var other in votes.Values)
                if (!other.MajorityOnly && other.Voters.Count > vote.Voters.Count) { beaten = true; break; }
            if (!beaten) PassVote(key);
        }
    }

    /// <summary>Does what a vote was for, as the streamer would, and tells everyone (the boss countdown announces itself).</summary>
    private void PassVote(string key)
    {
        if (key == "boss")
        {
            CallBoss(null, true, out _);
        }
        else if (key == "climb")
        {
            var town = CurrentTown;
            var floor = Tower.Get(StreamFloor);
            if (Climb(null, true, out _) == ClimbResult.Climbing)
                Announce(Localization.MSG_TOWER_CLIMB_STARTED, town != null ? town.Name : "", TowerWords.Floor(StreamFloor), TowerWords.Part(StreamFloor), floor != null ? floor.Name : "");
        }
        else if (key.StartsWith("part ") && int.TryParse(key.Substring(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            var def = Tower.Get(number);
            if (def == null || RequestMove(null, number, true, out _) != MoveResult.Moved) return;
            if (number == StreamFloor) Announce(Localization.MSG_TOWER_FLOOR_BACK, TowerWords.Floor(number), TowerWords.Part(number), def.Name);
            else Announce(Localization.MSG_TOWER_FLOOR_TRAIN, TowerWords.Floor(number), TowerWords.Part(number), def.Name, TowerWords.Floor(StreamFloor), TowerWords.Part(StreamFloor));
            votes.Clear();
        }
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
        // a vote that came in as the climb was passed: everyone in the town is already on the way up while the part
        // loads. It opened a second climb vote, announced in chat, that was left open on the part (playtest 2026-11-26)
        var push = PushArea;
        if (push != null && inTown.TrueForAll(p => push.Pending.Contains(p))) return ClimbResult.Climbing;
        if (State == null) { if (!stateRequested) RefreshState(); return ClimbResult.NotReady; }
        if (!canForce)
        {
            if (!CanVote(player)) return ClimbResult.NotInTower;
            votesNeeded = AddVote("climb", player);
            if (votesNeeded > 0)
            {
                if (VoteJustOpened) Announce(Localization.MSG_TOWER_CLIMB_VOTE_OPENED, player.PlayerName, TowerWords.Floor(StreamFloor), TowerWords.Part(StreamFloor), (int)VoteSeconds, votesNeeded);
                return ClimbResult.Voted;
            }
        }
        votes.Remove("climb");
        foreach (var p in inTown) Join(p);
        return ClimbResult.Climbing;
    }

    /// <summary>
    ///     The streamer switched "Tower open to chat" off: everyone inside goes back to what they were doing. Before, the
    ///     switch only refused commands, so those inside could not climb, fight or close, and nobody could get them out.
    /// </summary>
    public void ClosedByStreamer()
    {
        if (Close(null, true, out _) == CloseResult.Closed) Announce(Localization.MSG_TOWER_CLOSED_BY_STREAMER);
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
        if (everyone.Count == 0 && !AnyPending() && pendingTownJoins.Count == 0) return CloseResult.Empty;
        if (!canForce)
        {
            if (!CanVote(player)) return CloseResult.NotInTower;
            if (Phase == TowerPhase.Countdown || Phase == TowerPhase.Fight) return CloseResult.FightUnderWay;
            // a majority only: one voice and a minute must not send everyone out (!tower leave takes one player out)
            votesNeeded = AddVote("close", player, true);
            if (votesNeeded > 0)
            {
                if (VoteJustOpened) Announce(Localization.MSG_TOWER_CLOSE_VOTE_OPENED, player.PlayerName, votesNeeded, (int)VoteSeconds);
                return CloseResult.Voted;
            }
        }
        votes.Clear();
        if (attempt != TowerPhase.Closed) EndAttempt();
        foreach (var a in areas) a.Pending.Clear();
        pendingTownJoins.Clear();
        foreach (var p in everyone) Leave(p);
        return CloseResult.Closed;
    }

    // ---- Boss attempts -------------------------------------------------------------------------------------

    public enum BossCallResult { Started, Voted, NotOpen, NotLoaded, UnderWay, Regrouping, NotInTower }

    /// <summary>
    ///     !tower boss: the streamer or a moderator starts the countdown at once; anyone else in the tower votes (see
    ///     <see cref="AddVote"/>). <paramref name="votesNeeded"/> is what is still missing after a vote for it to start now.
    /// </summary>
    public BossCallResult CallBoss(PlayerController player, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        if (attempt != TowerPhase.Closed) return BossCallResult.UnderWay;
        // the guardian is fought on the part the climb is on, while it is loaded
        var area = State == null ? null : AreaAt(StreamFloor);
        if (area == null || area.Loading || !area.Floor || clearing != null) return BossCallResult.NotLoaded;
        if (!State.BossOpen || AllCleared) return BossCallResult.NotOpen;
        if (RetrySecondsLeft > 0) return BossCallResult.Regrouping;

        if (!canForce)
        {
            if (!CanVote(player)) return BossCallResult.NotInTower;
            votesNeeded = AddVote("boss", player);
            if (votesNeeded > 0)
            {
                if (VoteJustOpened) Announce(Localization.MSG_TOWER_BOSS_VOTE_OPENED, player.PlayerName, TowerWords.BossKind(area.Step), TowerWords.Floor(area.Step), TowerWords.Part(area.Step), (int)VoteSeconds, votesNeeded);
                return BossCallResult.Voted;
            }
        }

        StartCountdown(area);
        return BossCallResult.Started;
    }

    private void StartCountdown(Area area)
    {
        LastLoss = null;
        votes.Remove("boss");
        participants.Clear();
        fallen.Clear();
        fightArea = area;
        foreach (var p in PlayersOn(area)) participants.Add(p);
        attempt = TowerPhase.Countdown;
        phaseEnds = Time.time + Tower.BossCountdownSeconds;
        // a floor of rooms: the way to the boss's room opens and everyone walks there during the countdown
        var routed = area.Floor ? area.Floor.Route : null;
        if (routed && routed.Valid) routed.OpenToBoss();
        // told to everyone: players in the town and on other parts can join with !tower
        Announce(Localization.MSG_TOWER_BOSS_COUNTDOWN, TowerWords.Floor(area.Step), TowerWords.Part(area.Step), Tower.BossCountdownSeconds, TowerWords.BossKind(area.Step), area.Definition.Boss.Name);
    }

    // who is in the attempt: in the tower and on the part it is fought on (someone who went to the town or to another
    // part during the countdown is out of it)
    private void TrimParticipants()
    {
        var floor = fightArea != null ? fightArea.Floor : null;
        participants.RemoveWhere(p => !p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower || p.towerHandler.Floor != floor);
    }

    private void StartFight()
    {
        var floor = fightArea != null ? fightArea.Floor : null;
        var def = fightArea?.Definition;
        TrimParticipants();
        if (!floor || def == null || participants.Count == 0)
        {
            EndAttempt();
            if (def != null) Announce(Localization.MSG_TOWER_BOSS_CALLED_OFF, def.Boss.Name);
            return;
        }

        // the guardian comes with the numbers the group asks for (scaling by pressure)
        var boss = floor.SpawnBoss(BossPressure(def));
        if (!boss)
        {
            Shinobytes.Debug.LogError("TowerManager: " + Tower.Label(def.Number) + " has no template for the boss " + def.Boss.Model);
            EndAttempt();
            Announce(Localization.MSG_TOWER_BOSS_CALLED_OFF, def.Boss.Name);
            return;
        }
        blows.Clear();
        killingBlow = Guid.Empty;
        boss.Struck = BossStruck;

        // the first time the stream fights this boss: its intro first (still the countdown: a join meanwhile counts)
        if (PlayBossIntro(boss, def, floor, false)) return;
        BeginFight(boss);
    }

    private async void BeginFight(EnemyController boss)
    {
        var area = fightArea;
        var floor = area != null ? area.Floor : null;
        var def = area?.Definition;
        TrimParticipants();
        if (!floor || def == null || !boss || floor.Boss != boss || participants.Count == 0)
        {
            EndAttempt();
            return;
        }

        // someone joined or left during the countdown or the intro: the numbers for the group that fights
        floor.RescaleBoss(BossPressure(def));
        fightStrength = TowerPressure.GroupStrength(StrengthsOf(participants, def.Number));
        attempt = TowerPhase.Fight;
        phaseEnds = Time.time + Tower.BossTimeLimitSeconds;
        fightStarted = Time.time;
        // the meters window: the fight from here (after the intro) to its end
        var meter = CombatMeter.Tower;
        meter.Begin();
        meter.Title = string.Format(Localization.UI_TOWER_METER_TITLE, TowerWords.Floor(area.Step), TowerWords.Part(area.Step), def.Boss.Name);
        meter.Caption = TowerWords.BossKind(area.Step).ToUpperInvariant();
        MeterArea = area;
        nextSlam = Time.time + SlamInterval(boss);
        slamAt = -1f;
        nextSpit = Time.time + SpitInterval(boss) * 0.5f;
        spitAt = -1f;
        addsCalled = 0;
        enraged = false;
        // a humanoid guardian stands ready for the whole fight, never back in its plain idle between swings
        fightMovement = boss.GetComponent<EnemyMovementController>();
        if (fightMovement) fightMovement.CombatStance = true;
        foreach (var p in participants) p.towerHandler.BossFightStarted(boss);
        Announce(Localization.MSG_TOWER_BOSS_START, def.Boss.Name, participants.Count, def.Boss.Line ?? "");

        var g = Game;
        var step = area.Step;
        try
        {
            var state = await g.RavenNest.Tower.BossStartedAsync(step);
            if (state != null) SetState(state);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.BossStarted: " + exc.Message);
        }
    }

    // the group's strength when the fight began, for the record of the clear
    private double fightStrength = 1;

    private float SlamInterval(EnemyController boss)
    {
        var def = fightArea?.Definition;
        if (def == null || def.Boss.SlamEvery <= 0) return float.MaxValue;
        return (float)(def.Boss.SlamEvery * Math.Max(0.5, boss.CombatDefinition?.AttackInterval ?? 2));
    }

    private void UpdateFight()
    {
        var floor = fightArea != null ? fightArea.Floor : null;
        var boss = floor ? floor.Boss : null;
        var def = fightArea?.Definition;
        if (!boss || def == null) { EndAttempt(); return; }

        if (boss.Stats.IsDead)
        {
            Won(def, boss);
            return;
        }

        var standing = participants.Count(p => p && !p.isDestroyed && p.towerHandler.InTower && !p.Stats.IsDead && !fallen.Contains(p));
        if (standing == 0) { Lost(def, Localization.MSG_TOWER_BOSS_LOST_FALLEN); return; }
        if (PhaseSecondsLeft <= 0f) { Lost(def, Localization.MSG_TOWER_BOSS_LOST_TIME); return; }

        UpdateKit(boss, def);

        // the slam: a ring on the ground first that fills to its edge while the boss stands and winds up (its attack, or
        // a jump), then the blow: dust to the ring's edge, the ground shakes, everyone inside is hit and pushed back.
        // Not while a spit is on its way (one move at a time reads better)
        var radius = SlamRadius(boss);
        if (slamAt < 0f && spitAt < 0f && Time.time >= nextSlam)
        {
            slamAt = Time.time + SlamWarning;
            slamFrom = Time.time;
            slamSwung = false;
            slamCentre = boss.transform.position;
            ClearMark(ref slamRing, ref slamMark);
            slamRing = TowerBossFx.Telegraph(slamCentre, radius, TowerBossFx.SlamColor, out slamMark);
            // it stands for the windup; its own swings wait
            boss.StunnedUntil = Mathf.Max(boss.StunnedUntil, slamAt + 0.1f);
            if (fightMovement) fightMovement.Stop();
        }
        else if (slamAt >= 0f)
        {
            TowerBossFx.Fill(slamMark, (Time.time - slamFrom) / SlamWarning);
            if (!slamSwung && fightMovement && Time.time >= slamAt - fightMovement.SwingLead(true))
            {
                slamSwung = true;
                fightMovement.PlaySwing(true);
            }
            if (Time.time >= slamAt)
            {
                slamAt = -1f;
                TowerBossFx.Land(slamRing, slamMark);
                slamRing = null;
                slamMark = null;
                TowerBossFx.SlamImpact(slamCentre, radius, TowerBossFx.DustColor(Tower.FloorInfo(def.Floor)?.Biome));
                nextSlam = Time.time + SlamInterval(boss);
                // a breath before a spit follows
                nextSpit = Mathf.Max(nextSpit, Time.time + 1.5f);
                var share = (float)def.Boss.SlamShare;
                var group = participants.ToList();
                foreach (var p in group)
                {
                    if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || fallen.Contains(p)) continue;
                    var d = p.Position - slamCentre;
                    d.y = 0f;
                    if (d.magnitude > radius) continue;
                    // Substitution (docs/substitution-escape-idea.md): a rare power may have them out of the ring as it lands
                    if (Substitutions.TryEscape(p, slamCentre, radius, group, spitAt >= 0f ? spitSpot : (Vector3?)null, SpitRadius)) continue;
                    var damage = Mathf.Max(1, (int)(GameMath.CalculateMeleeDamage(boss, p) * share));
                    p.TakeDamage(boss, damage);
                    if (!p.Stats.IsDead) TowerBossFx.Knock(p, slamCentre, SlamKnockback);
                }
            }
        }
    }

    private static void ClearMark(ref GameObject mark, ref Material material)
    {
        TowerBossFx.Remove(mark, material);
        mark = null;
        material = null;
    }

    // where a spit leaves the boss: high on its front, by its size
    private static Vector3 Mouth(EnemyController boss, Vector3 toward)
    {
        var t = boss.transform;
        var bounds = new Bounds(t.position, Vector3.zero);
        var any = false;
        foreach (var r in boss.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        var flat = toward - t.position;
        flat.y = 0f;
        flat = flat.sqrMagnitude > 0.01f ? flat.normalized : t.forward;
        var height = Mathf.Max(1f, bounds.max.y - t.position.y);
        return t.position + Vector3.up * (height * 0.75f) + flat * Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.6f;
    }

    private static float SlamRadius(EnemyController boss) => 4f + 1.5f * boss.transform.localScale.x;

    /// <summary>How far from the guardian a player who sits the fight out stands at least: clear of its slam.</summary>
    public static float WatchDistance(EnemyController boss) => SlamRadius(boss) + 5f;

    /// <summary>The guardian being fought, or null.</summary>
    public EnemyController FightBoss => attempt == TowerPhase.Fight && fightArea != null && fightArea.Floor ? fightArea.Floor.Boss : null;

    /// <summary>
    ///     A knocked out player has recovered (TowerHandler, after TowerRecovery's seconds): back into the fight, at
    ///     a share of their health. The guardian may come after them again, and its slams reach them again.
    /// </summary>
    public void Rejoin(PlayerController player)
    {
        if (!player || player.towerHandler == null) return;
        var boss = FightBoss;
        if (!boss || boss.Stats.IsDead || !fallen.Remove(player))
        {
            player.towerHandler.BossFightEnded();
            return;
        }
        player.towerHandler.Recovered(boss);
        CombatPaths.Word(player, Localization.UI_TOWER_BACK_IN, new Color(0.6f, 1f, 0.6f));
    }

    /// <summary>How the last attempt on the climb's part was lost, until the next one is called or the part is cleared: the panel says it while the group regroups.</summary>
    public sealed class LossInfo
    {
        public int Step;
        /// <summary>The guardian's short name ("Viridia").</summary>
        public string Boss;
        /// <summary>"Everyone was knocked out" or "Time ran out".</summary>
        public string Reason;
        /// <summary>The share of its health the guardian had left, in percent (1 to 100).</summary>
        public int PercentLeft;
    }

    public LossInfo LastLoss { get; private set; }

    /// <summary>A guardian's name without its byname: "Viridia, the Verdant Wyrm" is "Viridia".</summary>
    public static string ShortName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var comma = name.IndexOf(',');
        return comma > 0 ? name.Substring(0, comma) : name;
    }

    /// <summary>The area a boss attempt (its countdown or its fight) is on, or null.</summary>
    public Area FightArea => fightArea;

    /// <summary>
    ///     In a fight: the seconds until the next knocked out player is back in it (the soonest of those recovering), or
    ///     0 when nobody is recovering.
    /// </summary>
    public float NextBackSeconds()
    {
        if (attempt != TowerPhase.Fight) return 0f;
        var next = float.MaxValue;
        foreach (var p in fallen)
        {
            if (!p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower || !p.towerHandler.SatOut) continue;
            var left = p.towerHandler.RecoverSecondsLeft;
            if (left > 0f && left < next) next = left;
        }
        return next == float.MaxValue ? 0f : next;
    }

    /// <summary>In a fight: how many of its players still stand, and how many went in.</summary>
    public bool FightCount(out int standing, out int total)
    {
        standing = 0; total = 0;
        if (attempt != TowerPhase.Fight) return false;
        foreach (var p in participants)
        {
            if (!p || p.isDestroyed || !p.towerHandler.InTower) continue;
            total++;
            if (!p.Stats.IsDead && !fallen.Contains(p)) standing++;
        }
        return true;
    }

    /// <summary>
    ///     !tower boss now (the streamer or a moderator; the panel's Start now button): the countdown is cut short and
    ///     the fight begins with everyone who has joined. The guardian's first-time intro still plays, and a !tower
    ///     during it still counts. False when no countdown is running.
    /// </summary>
    public bool StartBossNow()
    {
        if (attempt != TowerPhase.Countdown || introBoss || PhaseSecondsLeft <= 0f) return false;
        var def = fightArea?.Definition;
        TrimParticipants();
        phaseEnds = Time.time;
        Announce(Localization.MSG_TOWER_BOSS_NOW, def != null ? def.Boss.Name : "", participants.Count);
        return true;
    }

    private float SpitInterval(EnemyController boss)
    {
        var def = fightArea?.Definition;
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
        var floor = fightArea.Floor;

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
        if (spitAt < 0f && slamAt < 0f && Time.time >= nextSpit)
        {
            var target = SpitTarget(boss);
            if (!target)
            {
                nextSpit = Time.time + 2f;
                return;
            }
            // the spot is marked in the spit's colour, and the boss turns to it and spits a glob that lands as the hit comes
            spitSpot = target.Position;
            spitAt = Time.time + SpitWarning;
            spitFrom = Time.time;
            spitSwung = false;
            spitThrown = false;
            ClearMark(ref spitRing, ref spitMark);
            spitRing = TowerBossFx.Telegraph(spitSpot, SpitRadius, TowerBossFx.ElementColor(SpitElement(boss)), out spitMark);
            boss.StunnedUntil = Mathf.Max(boss.StunnedUntil, spitAt - SpitFlight + 0.2f);
            if (fightMovement) fightMovement.Stop();
        }
        else if (spitAt >= 0f && Time.time < spitAt)
        {
            TowerBossFx.Fill(spitMark, (Time.time - spitFrom) / SpitWarning);
            var launch = spitAt - SpitFlight;
            if (!spitSwung && Time.time >= launch - (fightMovement ? fightMovement.SwingLead(false) : 0f))
            {
                spitSwung = true;
                var face = spitSpot - boss.transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f) boss.transform.rotation = Quaternion.LookRotation(face.normalized);
                // a humanoid throws it; a beast with no attack clip just turns and spits
                if (fightMovement && fightMovement.HasAttackClip) fightMovement.PlaySwing(false);
            }
            if (!spitThrown && Time.time >= launch)
            {
                spitThrown = true;
                TowerBossFx.Spit(Mouth(boss, spitSpot), spitSpot, Mathf.Max(0.2f, spitAt - Time.time), TowerBossFx.ElementColor(SpitElement(boss)),
                    Mathf.Clamp(0.3f * boss.transform.localScale.x, 0.5f, 1.4f));
            }
        }
        else if (spitAt >= 0f)
        {
            spitAt = -1f;
            TowerBossFx.Land(spitRing, spitMark, 0.2f);
            nextSlam = Mathf.Max(nextSlam, Time.time + 1.5f);
            spitRing = null;
            spitMark = null;
            // with the effects off no glob flew to splash: the splash's sound alone
            if (!SettingsApplier.BossEffects) TowerBossFx.Splash(spitSpot, SpitRadius, Color.white);
            nextSpit = Time.time + SpitInterval(boss);
            var group = participants.ToList();
            foreach (var p in group)
            {
                if (!p || p.isDestroyed || p.Stats.IsDead || !p.towerHandler.InTower || fallen.Contains(p)) continue;
                var d = p.Position - spitSpot;
                d.y = 0f;
                if (d.magnitude > SpitRadius) continue;
                // Substitution: out of the splash as it lands (the slam's ring is never down at the same time)
                if (Substitutions.TryEscape(p, spitSpot, SpitRadius, group)) continue;
                var damage = Mathf.Max(1, (int)(GameMath.CalculateMeleeDamage(boss, p) * (float)kit.SpitShare));
                p.TakeDamage(boss, damage);
            }
        }
    }

    // the element a boss spits: its own, or its band's when it has none (the runtime numbers carry it)
    private static RavenNest.Combat.Element SpitElement(EnemyController boss) =>
        boss.CombatDefinition != null ? boss.CombatDefinition.Element : RavenNest.Combat.Element.Physical;

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
        var area = fightArea;
        var floorNumber = area.Step;
        var floorBoss = def.IsFloorBoss;
        var ids = participants.Where(p => p && !p.isDestroyed).Select(p => p.Id).Distinct().ToArray();
        var names = participants.Where(p => p && !p.isDestroyed).GroupBy(p => p.Id).ToDictionary(x => x.Key, x => x.First().PlayerName);
        var players = ids.Length;
        var standing = participants.Where(p => p && !p.isDestroyed && !p.Stats.IsDead && p.towerHandler.InTower).ToList();
        // everyone who fought in it, knocked out or not, for the Slayer reward once the server has the clear
        var fought = participants.Where(p => p && !p.isDestroyed).ToList();
        var bossLevel = boss ? boss.Stats.CombatLevel : 0;
        var report = new TowerClearReport { Characters = ids, LastHits = LastBlows(), Strength = fightStrength };
        // the part is wrapped up: nobody new comes onto it, and it stays loaded until everyone is in the town
        clearing = area;
        LastLoss = null;
        // the meters' result: won, and on a floor boss whose blows felled it (the server's word replaces it below)
        CombatMeter.Tower.Outcome = Localization.UI_METER_VICTORY;
        if (floorBoss)
        {
            CombatMeter.Tower.MarkText = Localization.UI_METER_LAST_BLOW;
            foreach (var id in report.LastHits) CombatMeter.Tower.Marked.Add(id);
        }
        // the body stays for its fall (slow motion when the cutscene plays), then goes
        EndAttempt((floorBoss ? FloorBossFallSeconds : BossFallSeconds) + 1.5f);
        if (floorBoss) Announce(Localization.MSG_TOWER_FLOOR_BOSS_DOWN, def.Boss.Name, TowerWords.Floor(floorNumber), TowerWords.FloorName(floorNumber));
        else Announce(Localization.MSG_TOWER_BOSS_DOWN, def.Boss.Name, TowerWords.Floor(floorNumber), TowerWords.Part(floorNumber));
        PlayBossFall(boss, def, floorNumber);
        foreach (var p in standing) p.towerHandler.Celebrate();
        // a floor boss: the look at the world tower follows its fall at once (WorldTowerMoments), while the players
        // go to the new floor's town behind it; it does not wait for the server's answer
        if (floorBoss)
        {
            MovingToNewTown = true;
            dueArrival = null;
            WorldTower.FloorBossFell(TowerWords.Floor(floorNumber));
        }

        var g = Game;
        TowerClearResult result = null;
        try
        {
            result = await g.RavenNest.Tower.ClearedAsync(floorNumber, report);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.Cleared: " + exc.Message);
        }

        if (result == null || !result.Success)
        {
            Announce(Localization.MSG_TOWER_CLEAR_FAILED, result?.Message ?? "no answer from the server");
            if (result?.State != null) SetState(result.State);
            if (clearing == area) clearing = null;
            if (floorBoss) WorldTower.FloorBossRefused();
            MovingToNewTown = false;
            return;
        }

        PaySlayer(fought, def, bossLevel);

        // the world first is the floor boss's; a part's first clear in the world is only said with its coins
        if (result.WorldFirst) Announce(Localization.MSG_TOWER_WORLD_FIRST, def.Boss.Name, TowerWords.Floor(floorNumber), TowerWords.FloorName(floorNumber));
        Announce(result.PartFirst && !result.WorldFirst ? Localization.MSG_TOWER_CLEARED_FIRST : Localization.MSG_TOWER_CLEARED,
            TowerWords.Floor(floorNumber), TowerWords.Part(floorNumber), players, result.Coins.ToString("N0", CultureInfo.InvariantCulture), result.Rewards?.Length ?? 0);
        // the last blow on a floor boss, as the server decided it: a title (several names when blows landed together)
        if (result.LastHitBy != null && result.LastHitBy.Length > 0)
        {
            var hitters = result.LastHitBy.Select(id => names.TryGetValue(id, out var n) ? n : null).Where(n => !string.IsNullOrEmpty(n)).ToList();
            // the meters' result names who the server gave the last blow (unless another fight has begun since)
            if (!CombatMeter.Tower.Running && MeterArea == area)
            {
                CombatMeter.Tower.Marked.Clear();
                foreach (var id in result.LastHitBy) CombatMeter.Tower.Marked.Add(id);
            }
            if (hitters.Count > 0) Announce(Localization.MSG_TOWER_LAST_HIT, string.Join(", ", hitters), def.Boss.Name, Localization.UI_TITLE_FINISHER_1);
        }
        if (result.Rewards != null && result.Rewards.Length > 0) AnnounceDrops(g, g.AddItems(result.Rewards));
        if (result.State != null) SetState(result.State);

        // the next part: after a moment to celebrate, everyone goes down to the town to rest and train until the
        // stream goes on (!tower climb); after a floor boss that is the new floor's town
        var next = Tower.Get(StreamFloor);
        if (AllCleared || next == null)
        {
            Announce(Localization.MSG_TOWER_TOP);
            if (clearing == area) clearing = null;
            MovingToNewTown = false;
            return;
        }
        var town = StreamTown;
        if (town == null)
        {
            // no town for it (should not happen): straight on, as before the towns
            if (clearing == area) clearing = null;
            MovingToNewTown = false;
            if (area.Floor && !area.Unloading && LayoutFor(next) == LayoutFor(def)) Retarget(area, next.Number);
            else foreach (var p in PlayersOn(area)) GoTo(p, next.Number, out _);
            return;
        }
        if (floorBoss) Announce(Localization.MSG_TOWER_TO_NEW_TOWN, TowerWords.Floor(next.Number), TowerWords.FloorName(next.Number), (int)ClimbDelay, town.Name, next.Name);
        else Announce(Localization.MSG_TOWER_TO_TOWN, (int)ClimbDelay, town.Name, TowerWords.Floor(next.Number), TowerWords.Part(next.Number), next.Name);
        StartCoroutine(AfterClearRoutine(area, def, next, town));
    }

    /// <summary>A floor boss pays this many times a mini boss's Slayer reward (the owner, 2026-11-27).</summary>
    public const double FloorBossSlayerShare = 2.5;

    /// <summary>
    ///     A won boss attempt pays Slayer experience, as a dungeon boss does (DungeonController.AddExperienceReward: the
    ///     same factor from the boss's combat level, and its small bonus in the trained skill); a floor boss 2.5 times
    ///     that. Everyone who fought in the attempt gets it, knocked out ones included; whoever sat it out gets nothing.
    ///     The floor's experience bonus (Tower.ExpBonus) applies to both. The players are still on the part here, so a
    ///     combat skill or Healing already gets that bonus from GetExperience and is not given it twice.
    /// </summary>
    private void PaySlayer(List<PlayerController> fought, TowerFloorDefinition def, int bossLevel)
    {
        if (fought == null || def == null) return;
        var g = Game;
        var dungeonFactor = g && g.SessionSettings != null ? Math.Max(1, g.SessionSettings.DungeonExpFactor) : 1;
        var share = def.IsFloorBoss ? FloorBossSlayerShare : 1;
        var slayer = Mathf.Max(bossLevel / 200, 70) * 0.125 * dungeonFactor;
        var skill = Math.Max(5, slayer * 0.5) * share;
        slayer *= share;
        var bonus = 1d + Tower.ExpBonus(def.Floor);
        foreach (var p in fought)
        {
            if (!p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower) continue;
            try
            {
                p.Progression.AddReward(Skill.Slayer, slayer * bonus);
                var trained = p.ActiveSkill;
                if (trained == Skill.None || trained == Skill.Slayer) continue;
                var given = trained.IsCombatSkill() ? 1d + p.towerHandler.ExpBonus : 1d;
                p.Progression.AddReward(trained, skill * bonus / given);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("TowerManager.PaySlayer: " + exc.Message);
            }
        }
    }

    // Who got what from the chest, as after a dungeon (the same filter): the clear only gave a count, and nobody knew
    // what they had won.
    private static async void AnnounceDrops(GameManager g, PlayerItemDropText drops)
    {
        try
        {
            if (drops == null || drops.Messages == null || !g || g.RavenBot == null || g.HasMessageFilter("ItemDrop")) return;
            for (var i = 0; i < drops.Messages.Count; i++)
            {
                if (i > 0) await System.Threading.Tasks.Task.Delay(100);
                g.RavenBot.Announce(drops.Messages[i]);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerManager.AnnounceDrops: " + exc.Message);
        }
    }

    /// <summary>
    ///     After a clear: the players on the floor go to the town of the next floor's band (the new band's town after a
    ///     tenth floor), and the floor gets ready for the next one: the same layout fills for it at once, another goes
    ///     now and loads when they climb (!tower climb).
    /// </summary>
    private IEnumerator AfterClearRoutine(Area area, TowerFloorDefinition cleared, TowerFloorDefinition next, TowerTownDefinition town)
    {
        yield return new WaitForSeconds(ClimbDelay);
        var moved = 0;
        foreach (var p in PlayersOn(area))
        {
            var r = JoinTown(p, town);
            // another town is open with people in it: they go there instead, to be with the others
            if (r == TownJoinResult.OtherOpen) r = JoinTown(p);
            if (r == TownJoinResult.Joined || r == TownJoinResult.Loading) moved++;
        }
        while (TownLoading) yield return null;
        // someone knocked out at that moment cannot be moved yet (they are up again after a few seconds): tried again
        // for a while. The floor stayed as it was while anyone was on it, the server was already on the next one, and
        // !tower climb then took everyone back to the cleared floor, where nothing counted.
        var waited = 0f;
        while (CountOn(area) > 0 && waited < 20f && !area.Unloading)
        {
            yield return new WaitForSeconds(1f);
            waited += 1f;
            foreach (var p in PlayersOn(area))
            {
                var r = JoinTown(p, town);
                if (r == TownJoinResult.OtherOpen) r = JoinTown(p);
                if (r == TownJoinResult.Joined || r == TownJoinResult.Loading) moved++;
            }
            while (TownLoading) yield return null;
        }
        // the area gets ready for the next part whoever is still on it (they find themselves on the new part, or back
        // in the world when its layout is another): the same layout is filled again where it stands, another goes now
        // and the next part loads when they climb (!tower climb)
        if (clearing == area) clearing = null;
        if (area.Floor && !area.Unloading && areas.Contains(area))
        {
            if (LayoutFor(next) == LayoutFor(cleared) && AreaAt(next.Number) == null) Retarget(area, next.Number);
            else yield return UnloadRoutine(area);
        }
        // a new band's town, the first time the stream gets there: a fly-over with its name. While the look at the
        // world tower plays (or waits to) it follows that look straight out of its fade, never under it
        var arrived = TowerTown.Loaded;
        if (moved > 0 && arrived && arrived.Town == town && Tower.TownFor(cleared.Number) != town)
        {
            if (WorldTower.HoldsArrival) dueArrival = arrived;
            else PlayArrival(arrived, false);
        }
        if (moved > 0) Announce(Localization.MSG_TOWER_IN_TOWN, TowerTown.Loaded && TowerTown.Loaded.Town != null ? TowerTown.Loaded.Town.Name : town.Name, TowerWords.Floor(next.Number), TowerWords.Part(next.Number), next.Name);
        MovingToNewTown = false;
    }

    /// <summary>
    ///     A floor boss fell and its players are on their way to the new floor's town, until they are there (or the
    ///     move is given up): the look at the world tower waits for it in the dark before it shows the town.
    /// </summary>
    public bool MovingToNewTown { get; private set; }

    // a new town's fly-over kept for the end of the look at the world tower (WorldTowerMoments)
    private TowerTown dueArrival;

    /// <summary>A new town's fly-over waits for the look at the world tower to end.</summary>
    public bool ArrivalDue => dueArrival && TowerTown.Loaded == dueArrival;

    /// <summary>The fly-over that waited, now (the look at the world tower has given the camera back): true when it plays.</summary>
    public bool PlayDueArrival()
    {
        var town = dueArrival;
        dueArrival = null;
        return town && TowerTown.Loaded == town && PlayArrival(town, false);
    }

    public enum MoveResult { Moved, Same, Locked, Busy, Voted, NotInTower, Nobody, OwnWay }

    /// <summary>
    ///     !tower all part 3 from chat (<paramref name="step"/> is the part's step; the climb's own step for !tower all
    ///     alone): the climbing group goes to that part together, as !tower part 3 did before each player could go by
    ///     themselves. The streamer or a moderator at once; anyone else of the group votes for it. Players in the town
    ///     stay there, and so do those who went to a cleared part by themselves (<see cref="OnOwnWay"/>): they hear of
    ///     a boss countdown like everyone and come back with !tower. In potato mode the one open part is everyone's.
    /// </summary>
    public MoveResult RequestMove(PlayerController player, int step, bool canForce, out int votesNeeded)
    {
        votesNeeded = 0;
        var def = Tower.Get(step);
        if (def == null || step < 1 || State == null || step > StreamFloor) return MoveResult.Locked;
        if (attempt != TowerPhase.Closed || clearing != null) return MoveResult.Busy;
        // the streamer or a moderator who is on a part of their own takes the group along and goes with it
        if (canForce && player && OnOwnWay(player)) ownWay.Remove(player);
        var group = GroupPlayers();
        if (group.Count == 0) return MoveResult.Nobody;
        var target = AreaAt(step);
        if (target != null && target.Floor)
        {
            var all = true;
            foreach (var p in group) if (p.towerHandler.Floor != target.Floor) { all = false; break; }
            if (all) return MoveResult.Same;
        }
        if (!canForce)
        {
            if (!CanVote(player)) return MoveResult.NotInTower;
            // on a part of their own: where the group goes is the group's to say
            if (OnOwnWay(player) && player.towerHandler.Floor) return MoveResult.OwnWay;
            votesNeeded = AddVote(PartVote(step), player, false, true);
            if (votesNeeded > 0)
            {
                if (VoteJustOpened) Announce(Localization.MSG_TOWER_FLOOR_VOTE_OPENED, player.PlayerName, TowerWords.Floor(step), TowerWords.Part(step), def.Name, (int)VoteSeconds, votesNeeded);
                return MoveResult.Voted;
            }
        }
        votes.Remove(PartVote(step));
        return MoveGroup(step, group) ? MoveResult.Moved : MoveResult.Busy;
    }

    // The climbing group goes to one part. When no place is free for it, a loaded area of the same layout that only
    // the group is on becomes it where it stands (nothing to load); else it opens beside the others (one more than
    // the limit for a moment), the group goes over when it has loaded, and the area it left goes at once
    // (UpdateAreas). Nobody steps out of the tower meanwhile. False: every place is taken, by a move still under way.
    private bool MoveGroup(int step, List<PlayerController> group)
    {
        var target = AreaAt(step);
        if (target == null)
        {
            if (Room(step, out var evict)) target = evict != null ? Replace(evict, step) : Open(step);
            else
            {
                var scene = ScenePath(LayoutFor(Tower.Get(step)));
                Area same = null;
                foreach (var a in areas)
                {
                    if (a.Unloading || a.Closing || a.Loading || !a.Floor || a.ScenePath != scene || a == fightArea || a == clearing) continue;
                    if (!OnlyOn(a, group)) continue;
                    // rather a cleared part than the climb's
                    if (same == null || (State != null && same.Step == StreamFloor)) same = a;
                }
                if (same != null)
                {
                    Retarget(same, step);
                    target = same;
                }
                else if (FreeSlot() < 0) return false;
                else target = Open(step);
            }
        }
        List<PlayerController> down = null;
        foreach (var p in group)
        {
            if (!p || p.isDestroyed || p.towerHandler == null || p.towerHandler.Floor == target.Floor) continue;
            if (p.towerHandler.InBossFight) continue;
            // knocked out as the group goes: they follow when they are back up. Left behind, they stayed alone on the
            // old part, which kept a second part open in potato mode (playtest 2026-11-26)
            if (p.Stats.IsDead) { (down ??= new List<PlayerController>()).Add(p); continue; }
            Send(p, target);
        }
        if (down != null) StartCoroutine(FollowWhenUp(down, target));
        return true;
    }

    // Group players who were knocked out when the group moved go after it once they are up (for half a minute; one
    // who meanwhile went elsewhere, to the town or a part of their own, or left the tower, stays where they are).
    private IEnumerator FollowWhenUp(List<PlayerController> players, Area target)
    {
        var floorWas = new Dictionary<PlayerController, TowerFloor>();
        foreach (var p in players) floorWas[p] = p.towerHandler.Floor;
        var until = Time.time + 30f;
        while (players.Count > 0 && Time.time < until)
        {
            yield return new WaitForSeconds(1f);
            if (!areas.Contains(target) || target.Unloading || target.Closing) yield break;
            for (var i = players.Count - 1; i >= 0; i--)
            {
                var p = players[i];
                if (!p || p.isDestroyed || p.towerHandler == null || !p.towerHandler.InTower || p.towerHandler.InTown || OnOwnWay(p)
                    || p.towerHandler.Floor != floorWas[p])
                {
                    players.RemoveAt(i);
                    continue;
                }
                if (p.Stats.IsDead) continue;
                if (!target.Pending.Contains(p)) Send(p, target);
                players.RemoveAt(i);
            }
        }
    }

    // nobody but these players is on the area or on the way to it
    private bool OnlyOn(Area area, List<PlayerController> players)
    {
        foreach (var p in PlayersOn(area)) if (!players.Contains(p)) return false;
        foreach (var p in area.Pending) if (!players.Contains(p)) return false;
        return true;
    }

    private void Lost(TowerFloorDefinition def, string message)
    {
        CombatMeter.Tower.Outcome = Localization.UI_METER_DEFEAT;
        // said on screen as a clear is, short and without the camera, and kept for the panel while the group
        // regroups: someone who only looks at the game has to see that the attempt was lost, why, and what comes next
        var guardian = fightArea != null && fightArea.Floor ? fightArea.Floor.Boss : null;
        var left = guardian && guardian.Stats.Health.MaxValue > 0
            ? Mathf.Clamp(Mathf.CeilToInt(100f * guardian.Stats.Health.CurrentValue / guardian.Stats.Health.MaxValue), 1, 100) : 100;
        LastLoss = new LossInfo
        {
            Step = fightArea != null ? fightArea.Step : StreamFloor,
            Boss = ShortName(def.Boss.Name),
            Reason = message == Localization.MSG_TOWER_BOSS_LOST_TIME ? Localization.UI_TOWER_LOST_TIME : Localization.UI_TOWER_LOST_FALLEN,
            PercentLeft = left
        };
        Shinobytes.UI.TowerHudView.ShowDefeat(Localization.UI_TOWER_DEFEATED, LastLoss.Reason, string.Format(Localization.UI_TOWER_LOST_LEFT, LastLoss.Boss, left));
        EndAttempt();
        retryAt = Time.time + Tower.BossRetrySeconds;
        Announce(message, def.Boss.Name, Tower.BossRetrySeconds);
    }

    /// <param name="bossDelay">Seconds before the boss goes: its fall is shown after a win.</param>
    private void EndAttempt(float bossDelay = 0f)
    {
        ClearMark(ref slamRing, ref slamMark);
        ClearMark(ref spitRing, ref spitMark);
        slamAt = -1f;
        spitAt = -1f;
        fightMovement = null;
        StopBossIntro();
        var floor = fightArea != null ? fightArea.Floor : null;
        if (floor) floor.DespawnBoss(bossDelay);
        if (floor && floor.Route) floor.Route.EndRally();
        foreach (var p in participants) if (p && p.towerHandler != null) p.towerHandler.BossFightEnded();
        foreach (var p in fallen) if (p && p.towerHandler != null) p.towerHandler.BossFightEnded();
        participants.Clear();
        fallen.Clear();
        attempt = TowerPhase.Closed;
        fightArea = null;
        CombatMeter.Tower.End();
    }

    /// <summary>The tower's last announcements as chat would read them, newest last (the tests read them; chat may be muted).</summary>
    public static readonly List<string> Said = new List<string>();

    private void Announce(string message, params object[] args)
    {
        var texts = args.Select(a => a?.ToString() ?? "").ToArray();
        Said.Add(Rendered(message, texts));
        if (Said.Count > 40) Said.RemoveAt(0);
        var g = Game;
        if (!g || g.RavenBot == null) return;
        g.RavenBot.Announce(message, texts);
    }

    /// <summary>A chat message with its named places filled in order, the way the bot does it.</summary>
    public static string Rendered(string message, object[] args)
    {
        var i = 0;
        var text = System.Text.RegularExpressions.Regex.Replace(message ?? "", @"\{[a-zA-Z]\w*\}", m => args != null && i < args.Length ? (args[i++]?.ToString() ?? "") : m.Value);
        // without the category and tags in front ("%[category:tower]%%[tags:info]%")
        var meta = text.LastIndexOf("]%", System.StringComparison.Ordinal);
        return text.StartsWith("%[") && meta > 0 ? text.Substring(meta + 2) : text;
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
        var band = Tower.FloorInfo(definition.Band);
        var line = band != null ? string.Format(Localization.UI_TOWER_TOWN_LINE, band.Number, band.Name) : "";
        // at the end the camera goes to someone in the town
        return TowerCutscene.Play(this, town.CutscenePoses(false), ArrivalSeconds, definition.Name, line, town.TitleColor, town.TitleOutline,
            () => TownPlayers().FirstOrDefault(), () =>
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            });
    }

    // ---- The bosses' cutscenes: the intro the first time, the fall every time

    /// <summary>
    ///     How long a mini boss's intro and its fall take, and how long their names show at the end: short, so the climb
    ///     is not interrupted (docs/tower-worlds-proposal.md section 12).
    /// </summary>
    public const float BossIntroSeconds = 5f, BossIntroTitleSeconds = 3.2f, BossFallSeconds = 3.2f, BossFallTitleSeconds = 2.4f;

    /// <summary>The floor boss's: the full ones, for the big fight that opens the next floor.</summary>
    public const float FloorBossIntroSeconds = 9f, FloorBossIntroTitleSeconds = 5f, FloorBossFallSeconds = 6.5f, FloorBossFallTitleSeconds = 4.6f;

    /// <summary>
    ///     How long a boss's name and line stay up in its intro: long enough to read the line (about 15 letters a second
    ///     after a moment to notice it), never shorter than <paramref name="least"/>, at most 10 s. "You defeated my little
    ///     brother. Prepare to be nibbled." stays about 5 s, where the 3.2 s of a mini boss cut it off (the owner,
    ///     2026-11-26). The intro grows by as much, so the camera does not cut away under it.
    /// </summary>
    public static float LineSeconds(string line, float least) => Mathf.Clamp(1.4f + 0.068f * (line?.Length ?? 0), least, 10f);

    /// <summary>The size of a mini boss's name and of a floor boss's, in its intro and at its fall.</summary>
    public const int BossTitleSize = 76, FloorBossTitleSize = 104;

    /// <summary>The earliest the boss taunts in its intro, and how slow its fall is.</summary>
    public const float TauntAt = 1.1f, FallSpeed = 0.35f;

    /// <summary>The boss's name in its intro: a menacing red.</summary>
    public static readonly Color BossColor = new Color(1f, 0.42f, 0.32f), BossOutline = new Color(0.24f, 0.03f, 0.02f);

    /// <summary>"Part 7 cleared" after a mini boss: silver, where a cleared floor is gold.</summary>
    public static readonly Color PartColor = new Color(0.86f, 0.9f, 0.95f), PartOutline = new Color(0.1f, 0.13f, 0.18f);

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
    private bool PlayBossIntro(EnemyController boss, TowerFloorDefinition def, TowerFloor on, bool replay)
    {
        if (!boss || def == null || introBoss || !SettingsApplier.Cutscenes) return false;
        var key = BossKey(def.Number);
        if (!replay && PlayerPrefs.GetInt(key, 0) == 1) return false;
        var (positions, forwards) = BossShot(boss, false);
        // a mini boss: who it is and where; a floor boss: that it is the floor's, then what it has to say
        var floorBoss = def.IsFloorBoss;
        var who = floorBoss
            ? string.Format(Localization.UI_TOWER_FLOOR_BOSS_LINE, def.Floor, TowerWords.FloorName(def.Number))
            : string.Format(Localization.UI_TOWER_BOSS_FLOOR, def.Floor, def.Part);
        var line = string.IsNullOrEmpty(def.Boss.Line) ? who
            : floorBoss ? who + "\n" + string.Format(Localization.UI_TOWER_BOSS_QUOTE, def.Boss.Line)
            : string.Format(Localization.UI_TOWER_BOSS_QUOTE, def.Boss.Line);
        // a long line stays up longer, and the intro with it: the camera comes in at its usual pace and waits under the
        // boss's face for the rest
        var baseTitle = floorBoss ? FloorBossIntroTitleSeconds : BossIntroTitleSeconds;
        var titleSeconds = LineSeconds(line, baseTitle);
        var baseSeconds = floorBoss ? FloorBossIntroSeconds : BossIntroSeconds;
        var seconds = baseSeconds + (titleSeconds - baseTitle);
        var pathSeconds = baseSeconds + 0.5f * (titleSeconds - baseTitle);
        var model = def.Boss.Model;
        var taunted = false;
        // timed to end as the camera settles under its face and its name shows (a loop, the cow eating, from early on)
        var entry = TowerBossTaunts.Get() ? TowerBossTaunts.Get().For(model) : null;
        var tauntAt = entry != null && !entry.Loops ? Mathf.Max(TauntAt, seconds - entry.Seconds - 0.4f) : TauntAt;
        introBoss = boss;
        introPlaying = TowerCutscene.Play(this, positions, forwards, seconds, def.Boss.Name, line, BossColor, BossOutline,
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
                if (replay)
                {
                    if (on && on.Boss == wasFor && attempt == TowerPhase.Closed) on.DespawnBoss();
                    return;
                }
                if (attempt == TowerPhase.Countdown) BeginFight(wasFor);
            }, titleSeconds, floorBoss ? FloorBossTitleSize : BossTitleSize, t =>
            {
                // played through: back to its own idle
                if (taunt != null && taunt.Done) StopTaunt();
                if (taunted || t < tauntAt) return;
                taunted = true;
                taunt = TowerBossTaunts.Play(boss, model);
            }, top: true, pathSeconds: pathSeconds);
        if (!introPlaying)
        {
            introBoss = null;
            return false;
        }
        // the countdown shows the time the intro takes (a !tower now answers with it)
        if (attempt == TowerPhase.Countdown) phaseEnds = Time.time + seconds;
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
    ///     The guardian has fallen: a few seconds round it while it falls in slow motion. A mini boss: a short moment,
    ///     "Part 7 cleared" in silver. The floor boss: the long one, "Floor 1 cleared" in gold with the floor's name.
    ///     Every clear, when cutscenes are on and the camera is free; the players cheer meanwhile.
    /// </summary>
    private bool PlayBossFall(EnemyController boss, TowerFloorDefinition def, int floorNumber)
    {
        if (!boss || def == null || !SettingsApplier.Cutscenes) return false;
        var (positions, forwards) = BossShot(boss, true);
        var floorBoss = def.IsFloorBoss;
        var seconds = floorBoss ? FloorBossFallSeconds : BossFallSeconds;
        var title = floorBoss
            ? string.Format(Localization.UI_TOWER_FLOOR_CLEARED, TowerWords.Floor(floorNumber))
            : string.Format(Localization.UI_TOWER_PART_CLEARED, TowerWords.Part(floorNumber));
        var line = floorBoss
            ? string.Format(Localization.UI_TOWER_FLOOR_BOSS_FALLS, def.Boss.Name, TowerWords.FloorName(floorNumber))
            : string.Format(Localization.UI_TOWER_BOSS_FALLS, def.Boss.Name);
        var played = TowerCutscene.Play(this, positions, forwards, seconds, title, line,
            floorBoss ? CutsceneScreen.Gold : PartColor, floorBoss ? CutsceneScreen.GoldOutline : PartOutline, SomeoneToWatch, null,
            floorBoss ? FloorBossFallTitleSeconds : BossFallTitleSeconds, floorBoss ? FloorBossTitleSize : BossTitleSize, top: true);
        // its death from the start, slowly, held at the end (it goes with the body)
        if (played && TowerBossTaunts.Fall(boss, FallSpeed, seconds - 0.8f) == null)
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
        return PlayersOn(fightArea ?? clearing ?? PushArea).FirstOrDefault(p => p && !p.isDestroyed && !p.Stats.IsDead);
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
        var area = WatchedArea;
        var floor = area != null ? area.Floor : null;
        var def = floor ? floor.Definition : null;
        if (!floor || def == null || attempt != TowerPhase.Closed || clearing != null || TowerCutscene.Playing || introBoss || floor.Boss) return false;
        var boss = floor.SpawnBoss();
        if (!boss) return false;
        if (PlayBossIntro(boss, def, floor, true)) return true;
        floor.DespawnBoss();
        return false;
    }

    /// <summary>The debug menu's "Boss Falls": the watched part's boss steps out and falls at once, as after a clear.</summary>
    public bool ReplayBossFall()
    {
        var area = WatchedArea;
        var floor = area != null ? area.Floor : null;
        var def = floor ? floor.Definition : null;
        if (!floor || def == null || attempt != TowerPhase.Closed || clearing != null || TowerCutscene.Playing || introBoss || floor.Boss) return false;
        var boss = floor.SpawnBoss();
        if (!boss) return false;
        var movement = boss.GetComponent<EnemyMovementController>();
        if (movement) movement.Die();
        PlayBossFall(boss, def, def.Number);
        floor.DespawnBoss((def.IsFloorBoss ? FloorBossFallSeconds : BossFallSeconds) + 1.5f);
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

    private bool hudAdded;
    private float nextHudTry;

    private void Update()
    {
        var g = Game;
        if (!g) return;
        if (!stateRequested && g.RavenNest != null && g.RavenNest.SessionStarted) RefreshState();
        // the tower panel in the top status column (floor, quests, countdown, boss): added once the column is there
        if (!hudAdded && Time.time >= nextHudTry)
        {
            nextHudTry = Time.time + 2f;
            hudAdded = Shinobytes.UI.TowerHudView.Ensure();
        }

        if (attempt == TowerPhase.Countdown && PhaseSecondsLeft <= 0f && !introBoss) StartFight();
        else if (attempt == TowerPhase.Fight) UpdateFight();
        UpdateVotes();
        UpdateFinds();
        RefreshPressure();
        // a floor of rooms gathers everyone at the boss only while an attempt is called or fought there
        foreach (var a in areas)
        {
            var route = a.Floor ? a.Floor.Route : null;
            if (route && route.Rallying && a != fightArea) route.EndRally();
        }

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

        // nobody on a part for a while: it goes
        UpdateAreas();
    }

    /// <summary>
    ///     The look of the place the camera is at (light, fog, haze, no sea, sky clouds or weather), set every frame by
    ///     <see cref="TowerLook"/> after every camera has moved (a late LateUpdate) and before anything is drawn. As
    ///     this object's own LateUpdate it ran before the game's camera moved: on the frame the camera cut from an
    ///     island to a tower place the place was drawn in the island's light, and the island in the place's on the
    ///     cut back.
    /// </summary>
    public void ApplyLook()
    {
        var camera = Camera.main;
        // the place the camera is at, or near enough to look at from outside (a wide shot): the nearest loaded one
        TowerPlace floor = null;
        if (camera) floor = PlaceNear(camera.transform.position, LookMargin);
        var over = floor;
        SetLook(over);
        UpdateHaze(over, camera);
        SkyClouds.Place = null;
        Ambience.Place = null;
        WeatherEffects.PlaceStorm = null;
        WeatherEffects.PlaceRain = null;
        if (!over) return;
        WeatherEffects.PlaceStorm = over.Storm;
        WeatherEffects.PlaceRain = over.Rain;
        Ambience.Place = new Ambience.Life { Snow = over.Snow, Fireflies = over.Fireflies, Butterflies = over.Butterflies, Gulls = over.Gulls };

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
            // a stormy place's lightning lights the land for a moment
            sun.intensity = floor.SunIntensity + (floor.Storm > 0f ? Weather.Flash * 5f : 0f);
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
