using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// The regatta round the Mangrove island (docs/banana-island.md): players with an own boat join with !regatta, which
/// opens the sign-up the first time; after it (or when the broadcaster says !regatta start) their boats come up at a
/// start line south of the island, sails furled, and on the count of three race round the buoys to its jetty, where
/// they land. Each boat goes at its own speed (Sailing level and boat, capped: SailingSpeeds), and gusts and lulls
/// through the race keep it open.
/// </summary>
/// <remarks>
/// On the way (docs/banana-island.md, "Pickups, hazards and meddling"): rows of mystery bottles across each leg give
/// what a racer needs for where they are (a tailwind or a cannonball for those behind, a barnacle shield or a banana
/// peel for the leaders); reefs and floating wreckage lie on some legs, and the better sailors steer round them more
/// often. The racers keep lanes, so they sail beside each other and swerve for bottles and away from trouble. Viewers
/// meddle from chat (MangroveCommand): a powder keg in front of a racer, wind in a racer's sails, and once a race the
/// streamer can wake the Kraken. A hit boat cannot be hit again for a few seconds. The race camera (RaceCamera) shows
/// it all when the setting allows. The places pay coins (the server) and Sailing exp (the game); wins and best times
/// go on the board by the jetty.
/// </remarks>
public sealed class Regatta : MonoBehaviour
{
    public const float SignupSeconds = 90f;
    public const float Cooldown = 600f;
    public const int MostRacers = 12;
    public const float GiveUpAfter = 300f;
    private const float Radius = 150f;
    /// <summary>The gap between hulls on the start line.</summary>
    private const float StartGap = 4f;
    private const float BuoysStay = 15f;
    /// <summary>The boats lie at the line this long, then comes the count of three.</summary>
    private const float LineUpSeconds = 5f, CountSeconds = 3f;
    /// <summary>The race camera stays on the finish this long after the race.</summary>
    private const float CameraAfter = 6f;
    /// <summary>Racers keep up to this far either side of the course, and move between lanes this fast.</summary>
    private const float LaneWidth = 15f, LaneSpeed = 7f;
    /// <summary>A buoy is rounded when a boat comes this close to it (its lane keeps it off the buoy itself).</summary>
    private const float MarkReached = 35f;
    /// <summary>After a hit a boat cannot be hit again for this long; a viewer's aim waits for it too.</summary>
    public const float HitImmunity = 4f;
    /// <summary>A racer aimed at by a viewer's keg is not aimed at again this soon.</summary>
    private const float AimedRest = 8f;
    /// <summary>Twice round the island: about a minute to a minute and a half with the race speeds below.</summary>
    public const int Laps = 2;
    /// <summary>A taken bottle shows again this soon; each racer takes each bottle once a lap.</summary>
    private const float BottleBack = 1.5f;
    /// <summary>A bottle is picked up this close to a boat's middle, whatever the hull (a galleon's beam caught more).</summary>
    private const float BottleReach = 4f;
    /// <summary>The winner is in: the rest have this long, then the race ends and they are placed as they lie.</summary>
    private const float AfterWinner = 25f;
    /// <summary>Viewers' wind: a racer gets it at most this often, and this many times a race.</summary>
    private const float WindRest = 8f;
    private const int MostWindsPerRacer = 2;
    /// <summary>The back third's cannonball now and then is a chain shot at the leader instead of the boat ahead.</summary>
    private const float ChainShotChance = 0.15f;
    private const float KegStays = 30f, PeelStays = 30f;
    private const float ShieldSeconds = 15f;
    private const float TailwindPace = 1.5f, TailwindSeconds = 4f;
    /// <summary>The back third's tailwind blows harder and longer, so those behind catch up.</summary>
    private const float BackTailwindPace = 1.7f, BackTailwindSeconds = 5f;
    /// <summary>The Sailing exp of a place, as a share of what the racer's next level takes.</summary>
    private static readonly float[] ExpShare = { 0.05f, 0.035f, 0.025f };
    private const float ExpShareOthers = 0.015f;

    public enum Phase { Signup, Countdown, Racing, Done }
    public enum JoinResult { Joined, Already, NotOpen, NoBoat, Busy, Full, Underway, Cooldown }
    public enum Item { None, Tailwind, Cannon, Shield, Peel }

    /// <summary>
    /// A boat's speed in the race: its own (12 for a new rowboat to 70 for a master's galleon) pressed into 30 to 44.5,
    /// so the order still follows the boats and the sailors, but by half again at most, not almost six times; a
    /// pickup or a hit can turn a race again. Out of the race every boat goes at its own speed.
    /// </summary>
    public static float RaceSpeed(float own) => 30f + (Mathf.Max(12f, own) - 12f) * 0.25f;
    public enum Hit { Cannon, Keg, Peel, Reef, Wreck, Kraken }
    public enum Meddling { Keg, Wind, Kraken }
    public enum MeddleResult { Done, NoRace, Racing, Raced, NoTarget, Finished, Protected, Nobody, KrakenUsed, NoKegs, NoWinds, WindRest }

    private sealed class Racer
    {
        public PlayerController Player;
        public OwnBoat Boat;
        public float Time = -1f;
        public int Place;
        public bool Out;
        public float NextGust;
        public float Gust = 1f;
        /// <summary>The buoys rounded so far: the next is marks[Mark].</summary>
        public int Mark;
        /// <summary>Its boat's course when the race began: another means it sailed off (!sail, !disembark, !fish).</summary>
        public int Course;
        public float Lane, LaneWanted, NextLane;
        public float Boost = 1f, BoostUntil;
        public float Slow = 1f, SlowUntil;
        public float ShieldUntil, SafeUntil, AimedUntil;
        /// <summary>Its own speed pressed into the race's (RaceSpeed), as the boat's pace.</summary>
        public float Base = 1f;
        /// <summary>Viewers' winds this race, and when the next may come.</summary>
        public int Winds;
        public float WindRestUntil;
        /// <summary>Still sailing when the time after the winner ran out: placed where it lay, no time.</summary>
        public bool Unfinished;
        public Item Item;
        public bool ChainShot;
        public float UseAt;
        public GameObject Aura;
        /// <summary>The things on the water it already tried (or failed) to steer round, this lap (JudgedLap).</summary>
        public readonly HashSet<Thing> Judged = new HashSet<Thing>();
        public int JudgedLap;
        public bool Racing => !Out && Time < 0f && Player && Boat;
        public string Name => Player ? Player.Name : "?";
    }

    private enum Kind { Bottle, Keg, Peel, Reef, Wreck }

    private sealed class Thing
    {
        public Kind Kind;
        public Vector3 At;
        public GameObject Go;
        public float Radius;
        public float Until = float.MaxValue;
        public float ArmedAt;
        /// <summary>A taken bottle shows again at this time; 0 while it is there.</summary>
        public float BackAt;
        /// <summary>The peel's dropper sails clear of it first; the keg's thrower is a viewer's name.</summary>
        public Racer Owner;
        public float OwnerClearAt;
        public string By;
        /// <summary>A bottle: the racers who took it, by lap (each takes it once a lap).</summary>
        public readonly HashSet<(Racer, int)> TakenBy = new HashSet<(Racer, int)>();
        public bool Live(float now) => BackAt <= 0f && now >= ArmedAt;
    }

    private sealed class Ball
    {
        public Racer From, To;
        public Vector3 Start;
        public float At;
        public GameObject Go;
    }

    private sealed class Kraken
    {
        public Vector3 At;
        public float Born;
        public Racer Target;
        public bool Struck;
        public int Stage = -1;
        public readonly List<Arm> Arms = new List<Arm>();
    }

    private sealed class Arm
    {
        public KrakenTentacle Tentacle;
        public Vector3 At;
        public float Delay;
        public float Depth;
        /// <summary>How far it curls over while it holds the boat.</summary>
        public float Reach;
        /// <summary>The boat it holds on to, and where it is and which way it faces from the boat; null stays put.</summary>
        public OwnBoat Holds;
        public Vector3 Offset;
        public Quaternion Turn;
    }

    // the Kraken: its arms up in a second, it roars, holds the boat, waves, and is gone again after KrakenGone
    private const float KrakenUp = 1f;
    private const float KrakenRoar = 1f;
    private const float KrakenSinks = 4.2f;
    private const float KrakenGone = 5.4f;

    private static Regatta current;
    private static float nextAllowed;

    private readonly List<Racer> racers = new List<Racer>();
    private readonly List<GameObject> buoys = new List<GameObject>();
    private readonly List<Thing> things = new List<Thing>();
    private readonly List<Ball> balls = new List<Ball>();
    private readonly List<Kraken> krakens = new List<Kraken>();
    private IslandController island;
    private List<Vector3> marks;
    private Vector3 start;
    private float startHeading;
    private Phase phase;
    private float signupEnds;
    private float startWidth;
    private float goAt;
    private float startedAt;
    private int finished;
    private bool forced;
    private bool krakenWoken;
    private int viewerKegs;
    private int perLap;
    private float winnerAt = -1f;
    private long pot;
    private readonly Dictionary<Guid, int> aimedBy = new Dictionary<Guid, int>();
    private bool filmed;
    private float releaseCameraAt = -1f;
    private AudioSource horn;

    public static bool Running => current && current.phase != Phase.Done;
    public static float CooldownLeft => Mathf.Max(0f, nextAllowed - Time.time);

    /// <summary>Signed up, lining up or still racing (not out, not finished): raids' and dungeons' auto-join leave them be.</summary>
    public static bool InRace(PlayerController player)
    {
        if (!current || !player || current.phase == Phase.Done) return false;
        var r = current.racers.FirstOrDefault(x => x.Player == player);
        return r != null && (current.phase == Phase.Signup || r.Racing);
    }

    /// <summary>The kegs viewers may throw in one race: a couple, plus one per racer, so a big chat cannot bury it.</summary>
    private int MostViewerKegs => 2 + racers.Count;

    // ---- !regatta ----------------------------------------------------------------------------------

    public static JoinResult Join(PlayerController player)
    {
        var island = Mangrove();
        if (!island || island.Locked) return JoinResult.NotOpen;
        if (current && current.phase != Phase.Signup) return JoinResult.Underway;
        if (current && current.racers.Any(r => r.Player == player)) return JoinResult.Already;
        if (!player.ferryHandler.HasOwnBoat) return JoinResult.NoBoat;
        if (Busy(player)) return JoinResult.Busy;
        if (!current)
        {
            if (Time.time < nextAllowed) return JoinResult.Cooldown;
            Open(island);
        }
        if (current.racers.Count >= MostRacers) return JoinResult.Full;
        current.racers.Add(new Racer { Player = player });
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_JOINED, player.Name, current.racers.Count), "");
        return JoinResult.Joined;
    }

    /// <summary>!regatta start (the broadcaster or a moderator): opens it if need be and starts it now, alone if only one has joined.</summary>
    public static bool StartNow()
    {
        if (!current || current.phase != Phase.Signup) return false;
        current.forced = true;
        current.signupEnds = Time.time;
        return true;
    }

    public static bool Leave(PlayerController player)
    {
        if (!current || current.phase != Phase.Signup) return false;
        return current.racers.RemoveAll(r => r.Player == player) > 0;
    }

    private static bool Busy(PlayerController p) =>
        !p || p.Controlled || p.InRavebridge || p.Stats.IsDead || p.raidHandler.InRaid || p.dungeonHandler.InDungeon
        || p.duelHandler.InDuel || p.arenaHandler.InArena || p.ferryHandler.IsGuest || (p.ferryHandler.OnFerry && !p.ferryHandler.OnOwnBoat);

    private static IslandController Mangrove() =>
        GameManager.Instance ? GameManager.Instance.Islands.Get(IslandController.MangroveIsland) : null;

    private static void Open(IslandController island)
    {
        var go = new GameObject("Regatta");
        go.transform.SetParent(island.transform, false);
        current = go.AddComponent<Regatta>();
        current.island = island;
        current.phase = Phase.Signup;
        current.signupEnds = Time.time + SignupSeconds;
        GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_OPEN, Mathf.RoundToInt(SignupSeconds));
    }

    // ---- Meddling from chat (MangroveCommand) ------------------------------------------------------

    /// <summary>
    /// A viewer meddles: a powder keg in front of a racer, wind in a racer's sails, or the Kraken in front of the
    /// leader. <paramref name="targetName"/> aims it; without one a racer is picked (for a keg, one not just hit). With
    /// <paramref name="onlyCheck"/> nothing happens: it only says whether it would, and at whom.
    /// </summary>
    public static MeddleResult Meddle(PlayerController viewer, Meddling kind, string targetName, bool onlyCheck, out string target, out int wait)
    {
        target = null;
        wait = 0;
        if (!current || current.phase != Phase.Racing) return MeddleResult.NoRace;
        // racers keep out of it until the race is over: dropped out (!sail) or finished too, else last place drops
        // out and kegs the leader
        var own = current.racers.FirstOrDefault(r => r.Player == viewer);
        if (own != null) return own.Racing ? MeddleResult.Racing : MeddleResult.Raced;
        if (kind == Meddling.Kraken && current.krakenWoken) return MeddleResult.KrakenUsed;
        if (kind == Meddling.Keg && current.viewerKegs >= current.MostViewerKegs) return MeddleResult.NoKegs;

        var now = Time.time;
        Racer racer;
        if (kind == Meddling.Kraken)
        {
            racer = current.Ordered().FirstOrDefault(r => r.Racing);
            if (racer == null) return MeddleResult.Nobody;
        }
        else if (!string.IsNullOrWhiteSpace(targetName))
        {
            var name = targetName.Trim().TrimStart('@');
            racer = current.racers.FirstOrDefault(r => r.Player && string.Equals(r.Player.Name, name, StringComparison.OrdinalIgnoreCase));
            target = racer != null ? racer.Name : name;
            if (racer == null || racer.Out || !racer.Player) return MeddleResult.NoTarget;
            if (racer.Time >= 0f) return MeddleResult.Finished;
            if (!racer.Boat) return MeddleResult.NoTarget;
            var free = Mathf.Max(racer.SafeUntil, racer.AimedUntil);
            if (kind == Meddling.Keg && now < free)
            {
                wait = Mathf.CeilToInt(free - now);
                return MeddleResult.Protected;
            }
            // a favourite's sails can be filled a couple of times a race, not all the way round (coins bought wins)
            if (kind == Meddling.Wind && racer.Winds >= MostWindsPerRacer) return MeddleResult.NoWinds;
            if (kind == Meddling.Wind && now < racer.WindRestUntil)
            {
                wait = Mathf.CeilToInt(racer.WindRestUntil - now);
                return MeddleResult.WindRest;
            }
        }
        else
        {
            var open = current.racers.Where(r => r.Racing && (kind == Meddling.Keg
                ? now >= Mathf.Max(r.SafeUntil, r.AimedUntil)
                : r.Winds < MostWindsPerRacer && now >= r.WindRestUntil)).ToList();
            if (open.Count == 0) return MeddleResult.Nobody;
            racer = open[UnityEngine.Random.Range(0, open.Count)];
        }
        target = racer.Name;
        if (onlyCheck) return MeddleResult.Done;

        switch (kind)
        {
            case Meddling.Keg:
                // aimed or not, the racer gets a rest from viewers' kegs: free random ones piled up on one boat,
                // and all but the first went off against its immunity
                racer.AimedUntil = now + AimedRest;
                current.viewerKegs++;
                current.ThrowKeg(racer, viewer.Name);
                break;
            case Meddling.Wind:
                racer.Winds++;
                racer.WindRestUntil = now + WindRest;
                current.Tailwind(racer, TailwindPace, TailwindSeconds);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_WIND, viewer.Name, racer.Name), "");
                break;
            case Meddling.Kraken:
                current.krakenWoken = true;
                current.WakeKraken(racer);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_KRAKEN, viewer.Name), "");
                break;
        }
        return MeddleResult.Done;
    }

    /// <summary>
    /// What aiming at a racer costs this viewer now: 2000 coins, doubling with each aimed one this race (4000, then 8000
    /// at most), so a rich friend cannot fill a favourite's sails the whole way round.
    /// </summary>
    public static long AimedPrice(Guid viewer)
    {
        var count = current && current.aimedBy.TryGetValue(viewer, out var n) ? n : 0;
        return RegattaMeddling.AimedPrice << Mathf.Min(2, count);
    }

    /// <summary>A viewer paid for a meddle: aimed ones raise their next price; half of every paid one goes in the prize pot.</summary>
    public static void Paid(Guid viewer, long coins, bool aimed)
    {
        if (!current) return;
        if (aimed) current.aimedBy[viewer] = (current.aimedBy.TryGetValue(viewer, out var n) ? n : 0) + 1;
        current.pot += coins / 2;
    }

    // ---- The race ----------------------------------------------------------------------------------

    private void Update()
    {
        switch (phase)
        {
            case Phase.Signup:
                racers.RemoveAll(r => !r.Player || Busy(r.Player));
                if (Time.time < signupEnds) return;
                if (racers.Count >= 2 || (forced && racers.Count >= 1)) Begin();
                else Cancel();
                return;

            case Phase.Countdown:
                DropOuts();
                if (Time.time >= goAt) Go();
                return;

            case Phase.Racing:
                Race();
                if (racers.All(r => r.Out || r.Time >= 0f) || Time.time - startedAt > GiveUpAfter
                    || (winnerAt > 0f && Time.time - winnerAt > AfterWinner)) Finish();
                return;

            case Phase.Done:
                // a Kraken woken just before the end still lets go and sinks (and stops shaking the camera)
                Rise(Time.time);
                if (releaseCameraAt > 0f && Time.time >= releaseCameraAt)
                {
                    releaseCameraAt = -1f;
                    var camera = GameManager.Instance ? GameManager.Instance.Camera : null;
                    // unless a newer race has the camera by now
                    var filming = camera ? camera.GetComponent<RaceCamera>() : null;
                    if (camera && camera.State == GameCameraType.Race && filming && filming.Race == this) camera.StopRace();
                }
                return;
        }
    }

    /// <summary>
    /// Left the game, the boat was put away (a raid or dungeon took them), or it was sent off the course (!sail,
    /// !disembark, !fish): out of the race, so nobody sails straight to the jetty past the buoys.
    /// </summary>
    private void DropOuts()
    {
        foreach (var r in racers)
        {
            if (r.Out || r.Time >= 0f) continue;
            if (r.Player && r.Boat && r.Player.ferryHandler.OwnBoat == r.Boat && r.Boat.CourseVersion == r.Course) continue;
            r.Out = true;
            ClearAura(r);
            if (!r.Boat) continue;
            r.Boat.Arrived -= Arrived;
            r.Boat.Held = false;
            r.Boat.Pace = 1f;
            r.Boat.Lane = 0f;
            r.Boat.Raced = false;
        }
    }

    private void Begin()
    {
        marks = Course(island, out start, out startHeading, out var finishHeading, out perLap);
        if (marks == null)
        {
            Shinobytes.Debug.LogWarning("The regatta found no course round the Mangrove island.");
            Cancel();
            return;
        }
        phase = Phase.Countdown;
        goAt = Time.time + LineUpSeconds + CountSeconds;
        PlaceBuoys();

        // line abreast across the start, facing the first buoy, a gap between the hulls, sails furled until the start
        var forward = Quaternion.Euler(0f, startHeading, 0f) * Vector3.forward;
        var side = Vector3.Cross(Vector3.up, forward);
        var beams = racers.Select(r =>
        {
            var item = r.Player.Inventory.GetEquipmentBySlot(ItemSlot.Boat);
            return item != null ? OwnBoat.Size(BoatItems.TierOf(item.Item)).Width : 3f;
        }).ToList();
        startWidth = beams.Sum() + StartGap * Mathf.Max(0, racers.Count - 1);
        var across = -startWidth * 0.5f;
        for (var i = 0; i < racers.Count; ++i)
        {
            var r = racers[i];
            var at = start + side * (across + beams[i] * 0.5f);
            across += beams[i] + StartGap;
            if (!SeaChart.IsWater(at)) at = start - forward * (i * 45f);
            at.y = SeaChart.Sea;
            r.Boat = r.Player.ferryHandler.RaceOwnBoat(at, startHeading, marks, island, finishHeading);
            if (!r.Boat) { r.Out = true; continue; }
            r.Boat.Held = true;
            r.Boat.Raced = true;
            r.Boat.Arrived += Arrived;
            r.Course = r.Boat.CourseVersion;
        }
        PlaceBottles();
        PlaceHazards();

        var racing = racers.Count(r => !r.Out);
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_LINEUP, racing), "");
        GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_START, racing);
        var camera = GameManager.Instance.Camera;
        filmed = camera && camera.FollowRace(this);
    }

    /// <summary>The count of three is done: the horn, and every boat sets sail at once.</summary>
    private void Go()
    {
        phase = Phase.Racing;
        startedAt = Time.time;
        foreach (var r in racers)
        {
            if (!r.Racing) continue;
            r.Boat.Held = false;
            r.NextGust = Time.time + UnityEngine.Random.Range(2f, 6f);
            r.NextLane = Time.time + UnityEngine.Random.Range(3f, 7f);
        }
        MangroveFeed.Say(Localization.UI_REGATTA_GO, "");
        PlayHorn();
    }

    private void PlayHorn()
    {
        var props = RegattaProps.Get();
        Aloud(props ? props.Horn : null, start, 0.8f);
    }

    /// <summary>A sound for the whole race (the horn, the Kraken's roar), heard by whoever watches it, not across the world.</summary>
    private void Aloud(AudioClip clip, Vector3 where, float volume)
    {
        var camera = Camera.main;
        if (!clip || GameSounds.Volume <= 0.001f) return;
        if (!filmed && (!camera || Flat(camera.transform.position - where).magnitude > 400f)) return;
        if (!horn)
        {
            horn = gameObject.AddComponent<AudioSource>();
            horn.playOnAwake = false;
            horn.spatialBlend = 0f;
        }
        horn.PlayOneShot(clip, GameSounds.Volume * volume);
    }

    private void Race()
    {
        DropOuts();
        var now = Time.time;
        var dt = GameTime.deltaTime;
        var order = Ordered().Where(r => r.Racing).ToList();
        for (var rank = 0; rank < order.Count; ++rank)
        {
            var r = order[rank];
            if (now >= r.NextGust)
            {
                r.NextGust = now + UnityEngine.Random.Range(5f, 11f);
                r.Gust = UnityEngine.Random.Range(0.82f, 1.18f);
            }
            var own = r.Boat.Speed;
            r.Base = own > 0.01f ? RaceSpeed(own) / own : 1f;
            r.Boat.Pace = r.Base * r.Gust * (now < r.BoostUntil ? r.Boost : 1f) * (now < r.SlowUntil ? r.Slow : 1f);
            if (r.Mark < marks.Count - 1 && Flat(marks[r.Mark] - r.Boat.SeaPosition).magnitude < MarkReached) r.Mark++;
            if (r.Item != Item.None && now >= r.UseAt) Use(r, rank, order);
            if (r.Aura && now >= Mathf.Max(r.ShieldUntil, r.BoostUntil)) ClearAura(r);
            Steer(r, now, dt);
        }
        Touch(now);
        Fly(now);
        Rise(now);
    }

    /// <summary>
    /// A racer's lane: now and then a new one, often towards a bottle ahead; away from a reef, wreckage, a keg or a peel
    /// in its way when it sees it in time (the better the sailor, the more often).
    /// </summary>
    private void Steer(Racer r, float now, float dt)
    {
        var boat = r.Boat;
        var p = boat.SeaPosition;
        var forward = Quaternion.Euler(0f, boat.Heading, 0f) * Vector3.forward;
        var right = Vector3.Cross(Vector3.up, forward);
        var clearance = boat.Width * 0.5f + 3f;
        // each lap a new try at the reefs and wreckage: one missed on the first lap was hit on every lap after
        var lap = Lap(r);
        if (r.JudgedLap != lap)
        {
            r.Judged.Clear();
            r.JudgedLap = lap;
        }
        var look = Mathf.Max(70f, boat.Speed * boat.Pace * 2.5f);
        // on the last leg the lanes close up towards the dock point, where the race ends
        var widest = r.Mark >= marks.Count - 1
            ? Mathf.Clamp((Flat(FinishPoint - p).magnitude - 25f) * 0.15f, 0f, LaneWidth + 6f)
            : LaneWidth + 6f;

        if (now >= r.NextLane)
        {
            r.NextLane = now + UnityEngine.Random.Range(3f, 7f);
            Thing bottle = null;
            if (r.Item == Item.None && UnityEngine.Random.value < 0.65f)
            {
                var best = float.MaxValue;
                foreach (var t in things)
                {
                    if (t.Kind != Kind.Bottle || !t.Live(now)) continue;
                    var to = Flat(t.At - p);
                    var ahead = Vector3.Dot(to, forward);
                    if (ahead < 15f || ahead > look * 2f || Mathf.Abs(Vector3.Dot(to, right)) > LaneWidth * 2f) continue;
                    var score = ahead + UnityEngine.Random.Range(0f, 25f);
                    if (score < best) { best = score; bottle = t; }
                }
            }
            r.LaneWanted = bottle != null
                ? Mathf.Clamp(r.Lane + Vector3.Dot(Flat(bottle.At - p), right), -LaneWidth - 4f, LaneWidth + 4f)
                : UnityEngine.Random.Range(-LaneWidth, LaneWidth);
        }

        foreach (var t in things)
        {
            if (t.Kind == Kind.Bottle || !t.Live(now) || r.Judged.Contains(t)) continue;
            if (t.Kind == Kind.Peel && t.Owner == r) continue;
            var to = Flat(t.At - p);
            var ahead = Vector3.Dot(to, forward);
            if (ahead < 0f || ahead > look) continue;
            var lateral = Vector3.Dot(to, right);
            var need = t.Radius + clearance;
            if (Mathf.Abs(lateral) > need) continue;
            r.Judged.Add(t);
            // a viewer's keg is harder to see coming: they get their money's worth
            var chance = Dodge(r) * (t.Kind == Kind.Keg ? 0.5f : 1f);
            if (UnityEngine.Random.value > chance) continue;
            var shift = lateral - Mathf.Sign(lateral == 0f ? 1f : lateral) * need;
            if (Mathf.Abs(r.Lane + shift) > LaneWidth + 6f) shift = lateral + Mathf.Sign(lateral == 0f ? 1f : lateral) * need;
            r.LaneWanted = Mathf.Clamp(r.Lane + shift, -LaneWidth - 6f, LaneWidth + 6f);
            r.NextLane = now + UnityEngine.Random.Range(3f, 5f);
            if (t.Kind == Kind.Keg && !string.IsNullOrEmpty(t.By))
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_DODGE, r.Name, t.By), "");
        }

        // never through another racer: alongside or overlapping, each moves over
        foreach (var o in racers)
        {
            if (o == r || !o.Racing) continue;
            var to = Flat(o.Boat.SeaPosition - p);
            var ahead = Vector3.Dot(to, forward);
            var lateral = Vector3.Dot(to, right);
            var beam = (boat.Width + o.Boat.Width) * 0.5f + 2f;
            if (Mathf.Abs(ahead) > (boat.Length + o.Boat.Length) * 0.5f + 3f || Mathf.Abs(lateral) > beam) continue;
            var away = lateral > 0f || (lateral == 0f && racers.IndexOf(r) < racers.IndexOf(o)) ? -1f : 1f;
            r.LaneWanted = r.Lane + away * (beam + 1f - Mathf.Abs(lateral)) * 0.6f;
        }

        r.LaneWanted = Mathf.Clamp(r.LaneWanted, -widest, widest);
        r.Lane = Mathf.MoveTowards(r.Lane, r.LaneWanted, LaneSpeed * dt);
        boat.Lane = r.Lane;
    }

    /// <summary>
    /// How often a racer steers round trouble it sees: a fifth of the time at Sailing level 1, rising as the boat's
    /// speed does with the level (SailingSpeeds.Reach: half way at about 100) to four in five.
    /// </summary>
    private static float Dodge(Racer r)
    {
        int level = r.Player && r.Player.Stats != null && r.Player.Stats.Sailing != null ? (int)r.Player.Stats.Sailing.Level : 1;
        return 0.2f + SailingSpeeds.Reach(level) * 0.6f;
    }

    /// <summary>What the boats sail into: bottles picked up, kegs and peels set off, reefs and wreckage scraped.</summary>
    private void Touch(float now)
    {
        for (var i = things.Count - 1; i >= 0; --i)
        {
            var t = things[i];
            if (t.BackAt > 0f && now >= t.BackAt)
            {
                t.BackAt = 0f;
                if (t.Go) t.Go.SetActive(true);
            }
            if (now >= t.Until)
            {
                if (t.Go) Destroy(t.Go);
                things.RemoveAt(i);
                continue;
            }
            if (!t.Live(now)) continue;
            foreach (var r in racers)
            {
                if (!r.Racing) continue;
                var reach = t.Kind == Kind.Bottle ? BottleReach : t.Radius + r.Boat.Width * 0.4f;
                if (Flat(t.At - r.Boat.SeaPosition).magnitude > reach) continue;
                if (t.Kind == Kind.Bottle)
                {
                    // once a lap each: the first boats through a row used to take every bottle in it
                    if (r.Item != Item.None || !t.TakenBy.Add((r, Lap(r)))) continue;
                    Give(r);
                    t.BackAt = now + BottleBack;
                    if (t.Go) t.Go.SetActive(false);
                    break;
                }
                if (t.Kind == Kind.Peel && t.Owner == r && now < t.OwnerClearAt) continue;
                // a boat still reeling from a hit sails through a keg or a peel, which waits for the next one: it used to
                // go off against the immunity, and a paid keg did nothing
                if ((t.Kind == Kind.Keg || t.Kind == Kind.Peel) && now < r.SafeUntil) continue;
                if (t.Kind == Kind.Reef || t.Kind == Kind.Wreck)
                {
                    Strike(r, t.Kind == Kind.Reef ? Hit.Reef : Hit.Wreck, null);
                    continue;
                }
                // a keg or a peel goes off once, whether or not a shield took it
                Strike(r, t.Kind == Kind.Keg ? Hit.Keg : Hit.Peel, t.Kind == Kind.Keg ? t.By : t.Owner?.Name);
                if (t.Kind == Kind.Keg) Effect(RegattaProps.Get()?.Blast, t.At, 5f);
                if (t.Go) Destroy(t.Go);
                things.RemoveAt(i);
                break;
            }
        }
    }

    // ---- Bottles and what is in them ---------------------------------------------------------------

    /// <summary>Something for where the racer is: those behind get speed and cannonballs, the leaders shields and peels.</summary>
    private void Give(Racer r)
    {
        var order = Ordered().Where(x => x.Racing).ToList();
        var rank = order.IndexOf(r);
        var share = order.Count > 1 ? rank / (float)(order.Count - 1) : 0f;
        var roll = UnityEngine.Random.value;
        if (share < 0.34f) r.Item = roll < 0.4f ? Item.Shield : roll < 0.85f ? Item.Peel : Item.Tailwind;
        else if (share < 0.67f) r.Item = roll < 0.3f ? Item.Tailwind : roll < 0.6f ? Item.Cannon : roll < 0.8f ? Item.Peel : Item.Shield;
        else r.Item = roll < 0.45f ? Item.Tailwind : roll < 0.85f ? Item.Cannon : Item.Shield;
        // the back third's tailwind blows harder (Use), and its cannon now and then reaches the leader
        r.ChainShot = r.Item == Item.Cannon && share >= 0.67f && UnityEngine.Random.value < ChainShotChance;
        // a shield goes up at once; the rest a moment later, as if they were deciding
        r.UseAt = Time.time + (r.Item == Item.Shield ? 0.3f : UnityEngine.Random.Range(0.6f, 2.5f));
        GameSounds.Play(SoundKind.ItemPickup, r.Boat.SeaPosition, 0.8f);
        Effect(RegattaProps.Get()?.Sparkle, r.Boat.SeaPosition + Vector3.up * 2f, 2f);
    }

    private void Use(Racer r, int rank, List<Racer> order)
    {
        var item = r.Item;
        r.Item = Item.None;
        switch (item)
        {
            case Item.Tailwind:
                var behind = order.Count > 2 && rank / (float)(order.Count - 1) >= 0.67f;
                Tailwind(r, behind ? BackTailwindPace : TailwindPace, behind ? BackTailwindSeconds : TailwindSeconds);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_TAILWIND, r.Name), "");
                break;

            case Item.Shield:
                r.ShieldUntil = Time.time + ShieldSeconds;
                SetAura(r, RegattaProps.Get()?.Bubbles);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_SHIELD, r.Name), "");
                break;

            case Item.Peel:
                DropPeel(r);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_PEEL, r.Name), "");
                break;

            case Item.Cannon:
                // a chain shot at the leader
                if (r.ChainShot && rank > 1 && order[0].Racing)
                {
                    r.ChainShot = false;
                    Fire(r, order[0]);
                    MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_CHAIN, r.Name, order[0].Name), "");
                    break;
                }
                r.ChainShot = false;
                // at the boat ahead; the leader fires back at the one behind
                var target = rank > 0 ? order[rank - 1] : rank + 1 < order.Count ? order[rank + 1] : null;
                if (target == null || !target.Racing)
                {
                    MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_CANNON_MISS, r.Name), "");
                    break;
                }
                Fire(r, target);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_CANNON, r.Name, target.Name), "");
                break;
        }
    }

    private void Tailwind(Racer r, float pace, float seconds)
    {
        r.Boost = pace;
        r.BoostUntil = Time.time + seconds;
        SetAura(r, RegattaProps.Get()?.Sparkle);
        GameSounds.Play(SoundKind.WindGust, r.Boat.SeaPosition, 1f);
    }

    private void DropPeel(Racer r)
    {
        var boat = r.Boat;
        var forward = Quaternion.Euler(0f, boat.Heading, 0f) * Vector3.forward;
        var at = boat.SeaPosition - forward * (boat.Length * 0.5f + 3f);
        Add(Kind.Peel, at, 2.2f, Time.time + PeelStays, RegattaProps.Get()?.Peel, owner: r);
    }

    private void ThrowKeg(Racer r, string by)
    {
        var boat = r.Boat;
        // it falls for 0.7 s, then the racer needs a moment to see it: about 2.5 s of sailing ahead, on its course and in
        // its lane, which it holds until then (unless it sees the keg in time and steers round it)
        var ahead = Mathf.Clamp(boat.Speed * boat.Pace * 2.5f, 25f, 220f);
        var at = boat.Ahead(ahead);
        if (!SeaChart.IsWater(at)) at = boat.Ahead(ahead * 0.5f);
        r.LaneWanted = r.Lane;
        r.NextLane = Time.time + 3.5f;
        // lobbed in from the shore: it lands with a splash a moment later and is live from then
        var keg = Add(Kind.Keg, at, 2.6f, Time.time + KegStays, RegattaProps.Get()?.Keg, by: by, dropSeconds: 0.7f);
        keg.ArmedAt = Time.time + 0.7f;
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_KEG, by, r.Name), "");
    }

    private void Fire(Racer from, Racer to)
    {
        var props = RegattaProps.Get();
        var at = from.Boat.SeaPosition + Vector3.up * 2.5f;
        Effect(props ? props.CannonShot : null, at, 3f);
        GameSounds.Play(SoundKind.RockImpact, at, 1f);
        var ball = new Ball { From = from, To = to, Start = at, At = Time.time };
        if (props && props.Cannonball) ball.Go = Instantiate(props.Cannonball, at, Quaternion.identity);
        balls.Add(ball);
    }

    /// <summary>Cannonballs in the air, curving onto their target wherever it has got to.</summary>
    private void Fly(float now)
    {
        const float seconds = 1.2f;
        for (var i = balls.Count - 1; i >= 0; --i)
        {
            var b = balls[i];
            var t = Mathf.Clamp01((now - b.At) / seconds);
            var end = b.To.Boat ? b.To.Boat.SeaPosition + Vector3.up * 1.5f : b.Start;
            var p = Vector3.Lerp(b.Start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 12f);
            if (b.Go) b.Go.transform.position = p;
            if (t < 1f) continue;
            if (b.Go) Destroy(b.Go);
            balls.RemoveAt(i);
            Effect(RegattaProps.Get()?.Splash, end, 4f);
            GameSounds.Play(SoundKind.WaterSplash, end, 1f);
            if (b.To.Racing) Strike(b.To, Hit.Cannon, b.From.Name);
        }
    }

    // ---- The Kraken ------------------------------------------------------------------------------

    private void WakeKraken(Racer r)
    {
        var props = RegattaProps.Get();
        var boat = r.Boat;
        var forward = Quaternion.Euler(0f, boat.Heading, 0f) * Vector3.forward;
        var at = boat.SeaPosition + forward * (boat.Length * 0.5f + 14f);
        var k = new Kraken { At = at, Born = Time.time, Target = r };
        if (props && props.Tentacle)
        {
            // the beast stays under; its three biggest arms burst up in front of the bow and lean over it
            var side = Vector3.Cross(Vector3.up, forward);
            AddArm(k, props.Tentacle, at + forward * 5f, boat.SeaPosition, 1.4f, 0f, 0.6f);
            AddArm(k, props.Tentacle, at + side * 9f, boat.SeaPosition, 1.2f, 0.12f, 0.8f);
            AddArm(k, props.Tentacle, at - side * 9f, boat.SeaPosition, 1.2f, 0.24f, 0.8f);

            // smaller ones come up on both sides of the boat and curl over its deck; a rowboat gets one pair
            var scale = Mathf.Clamp(0.4f + boat.Length * 0.025f, 0.5f, 1f);
            var apart = boat.Width * 0.5f + 2f + 4f * scale;
            var delay = 0.4f;
            foreach (var along in boat.Length < 10f ? new[] { 0f } : new[] { -0.3f, 0.2f })
            {
                var abeam = boat.SeaPosition + forward * (boat.Length * along);
                foreach (var sign in new[] { 1f, -1f })
                {
                    AddArm(k, props.Tentacle, abeam + side * (sign * apart), abeam, scale, delay, 1f, boat);
                    delay += 0.15f;
                }
            }
        }
        krakens.Add(k);
        Effect(props ? props.Splash : null, at, 4f);
        Effect(props ? props.Bubbles : null, at, 4f);
        GameSounds.Play(SoundKind.WaterSplash, at, 1f);
    }

    /// <summary>
    /// One of the Kraken's arms, deep under the water at a spot, to curl towards a point when it is up; one that holds a
    /// boat keeps its place beside it while it drifts.
    /// </summary>
    private static void AddArm(Kraken k, GameObject prefab, Vector3 spot, Vector3 towards, float scale, float delay, float reach, OwnBoat holds = null)
    {
        // it curls towards its own -X
        var to = Flat(towards - spot);
        if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
        var depth = 30f * scale;
        var facing = Quaternion.LookRotation(Vector3.Cross(-to.normalized, Vector3.up));
        var go = Instantiate(prefab, spot + Vector3.down * depth, facing);
        go.transform.localScale = Vector3.one * scale;
        var tentacle = go.GetComponent<KrakenTentacle>();
        if (!tentacle)
        {
            Destroy(go);
            return;
        }
        var arm = new Arm { Tentacle = tentacle, At = spot, Delay = delay, Depth = depth, Reach = reach, Holds = holds };
        if (holds)
        {
            var boatTurn = Quaternion.Euler(0f, holds.Heading, 0f);
            var back = Quaternion.Inverse(boatTurn);
            arm.Offset = back * (spot - holds.SeaPosition);
            arm.Offset.y = 0f;
            arm.Turn = back * facing;
        }
        k.Arms.Add(arm);
    }

    /// <summary>How much the Kraken's roar shakes the race camera, 0 to 1.</summary>
    public float Rumble { get; private set; }

    /// <summary>
    /// Its arms burst out of the sea in a second, in front of the boat and round it; it roars from below with them
    /// curled over the boat, holds it while they wave about, and they sink back.
    /// </summary>
    private void Rise(float now)
    {
        Rumble = 0f;
        var props = RegattaProps.Get();
        for (var i = krakens.Count - 1; i >= 0; --i)
        {
            var k = krakens[i];
            var age = now - k.Born;
            if (!k.Struck)
            {
                k.Struck = true;
                if (k.Target.Racing) Strike(k.Target, Hit.Kraken, null);
                IncidentPoint = k.At;
            }

            var stage = age < KrakenRoar ? 0 : age < KrakenRoar + 1.6f ? 1 : age < KrakenSinks ? 2 : 3;
            if (stage != k.Stage)
            {
                k.Stage = stage;
                if (stage == 1)
                {
                    Aloud(props ? props.Roar : null, k.At, 1f);
                    Effect(props ? props.Splash : null, k.At, 5f);
                    foreach (var a in k.Arms) Effect(props ? props.Splash : null, a.At, 3f);
                }
            }
            // the roar shakes the picture: hard at first, settling
            if (stage == 1) Rumble = Mathf.Max(Rumble, 1f - (age - KrakenRoar) / 1.6f);

            foreach (var a in k.Arms)
            {
                if (!a.Tentacle) continue;
                if (a.Holds)
                {
                    var boatTurn = Quaternion.Euler(0f, a.Holds.Heading, 0f);
                    a.At = a.Holds.SeaPosition + boatTurn * a.Offset;
                    a.At.y = k.At.y;
                    a.Tentacle.transform.rotation = boatTurn * a.Turn;
                }
                var armAge = age - a.Delay;
                var armUp = armAge < 0f ? 0f
                    : armAge < KrakenUp * 0.8f ? Mathf.SmoothStep(0f, 1f, armAge / (KrakenUp * 0.8f))
                    : age < KrakenSinks ? 1f
                    : 1f - Mathf.SmoothStep(0f, 1f, (age - KrakenSinks - a.Delay * 0.5f) / (KrakenGone - KrakenSinks - 0.4f));
                // only its root stays under the water (the clear sea shows what is below)
                a.Tentacle.transform.position = a.At + Vector3.down * Mathf.Lerp(a.Depth, a.Depth * 0.1f, armUp);
                // up straight and thrashing, curled right over the boat while it roars, then waving over it
                a.Tentacle.Curl = a.Reach * (stage == 0 ? 0.2f * armUp
                    : stage == 1 ? 0.9f
                    : stage == 2 ? 0.55f + Mathf.Sin(age * 2.2f + a.Delay * 9f) * 0.35f
                    : 0.3f);
                a.Tentacle.Wave = stage == 1 ? 0.5f : 1f;
            }

            if (age < KrakenGone) continue;
            foreach (var a in k.Arms) if (a.Tentacle) Destroy(a.Tentacle.gameObject);
            krakens.RemoveAt(i);
        }
    }

    // ---- Hits --------------------------------------------------------------------------------------

    public OwnBoat IncidentBoat { get; private set; }
    public float IncidentAt { get; private set; } = -1f;
    /// <summary>The last incident was the Kraken: the camera shows it whatever else is going on.</summary>
    public bool IncidentBig { get; private set; }
    public Vector3 IncidentPoint { get; private set; }

    /// <summary>A boat is hit, unless it was just hit or its shield takes it (the Kraken minds neither).</summary>
    private void Strike(Racer r, Hit hit, string by)
    {
        var now = Time.time;
        if (!r.Racing) return;
        if (hit != Hit.Kraken && now < r.SafeUntil) return;
        if (hit != Hit.Kraken && now < r.ShieldUntil)
        {
            r.ShieldUntil = 0f;
            ClearAura(r);
            Effect(RegattaProps.Get()?.Bubbles, r.Boat.SeaPosition + Vector3.up, 2f);
            GameSounds.Play(SoundKind.Bubbles, r.Boat.SeaPosition, 1f);
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BLOCKED, r.Name), "");
            return;
        }
        r.SafeUntil = now + HitImmunity;
        var at = r.Boat.SeaPosition;
        switch (hit)
        {
            case Hit.Cannon:
                SlowDown(r, 0.45f, 2f);
                break;
            case Hit.Keg:
                r.Boat.Spin(1.8f);
                SlowDown(r, 0.35f, 2.6f);
                GameSounds.Play(SoundKind.RockImpact, at, 1f);
                GameSounds.Play(SoundKind.WaterSplash, at, 1f);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, string.IsNullOrEmpty(by) ? Localization.UI_REGATTA_HIT_KEG : Localization.UI_REGATTA_HIT_KEG_BY, r.Name, by), "");
                break;
            case Hit.Peel:
                r.Boat.Spin(1.4f);
                SlowDown(r, 0.5f, 1.4f);
                GameSounds.Play(SoundKind.WaterSplash, at, 0.7f);
                // a peel of its own from the lap before: it says so
                MangroveFeed.Say(by == r.Name
                    ? string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_HIT_OWN_PEEL, r.Name)
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_HIT_PEEL, r.Name, by ?? "?"), "");
                break;
            case Hit.Reef:
                SlowDown(r, 0.4f, 1.6f);
                GameSounds.Play(SoundKind.RockImpact, at, 0.9f);
                Effect(RegattaProps.Get()?.Splash, at, 3f);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_HIT_REEF, r.Name), "");
                break;
            case Hit.Wreck:
                SlowDown(r, 0.6f, 1.2f);
                GameSounds.Play(SoundKind.WoodChop, at, 0.8f);
                Effect(RegattaProps.Get()?.Splash, at, 3f);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_HIT_WRECK, r.Name), "");
                break;
            case Hit.Kraken:
                // held until its arms let go and sink
                SlowDown(r, 0.15f, KrakenSinks + 0.3f);
                MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_HIT_KRAKEN, r.Name), "");
                break;
        }
        IncidentBoat = r.Boat;
        IncidentBig = hit == Hit.Kraken;
        IncidentPoint = at;
        IncidentAt = Time.unscaledTime;
    }

    /// <summary>A hit slows the boat for a while; a tailwind it has keeps blowing (a viewer paid for it).</summary>
    private static void SlowDown(Racer r, float pace, float seconds)
    {
        r.Slow = pace;
        r.SlowUntil = Time.time + seconds;
    }

    private void SetAura(Racer r, GameObject prefab)
    {
        ClearAura(r);
        if (!prefab || Potato || !r.Boat) return;
        r.Aura = Instantiate(prefab, r.Boat.transform);
        r.Aura.transform.localPosition = Vector3.up * 1.5f;
    }

    private static void ClearAura(Racer r)
    {
        if (r.Aura) Destroy(r.Aura);
        r.Aura = null;
    }

    private static bool Potato => GameManager.Instance && GameManager.Instance.PotatoMode;

    /// <summary>A one-off effect (a splash, a blast); none in potato mode.</summary>
    private static void Effect(GameObject prefab, Vector3 at, float seconds)
    {
        if (!prefab || Potato) return;
        var go = Instantiate(prefab, at, Quaternion.identity);
        Destroy(go, seconds);
    }

    // ---- The end -----------------------------------------------------------------------------------

    private void Arrived(OwnBoat boat)
    {
        boat.Arrived -= Arrived;
        boat.Pace = 1f;
        boat.Lane = 0f;
        var r = racers.FirstOrDefault(x => x.Boat == boat);
        if (r == null || r.Time >= 0f || r.Out || phase != Phase.Racing) return;
        r.Time = Time.time - startedAt;
        r.Place = ++finished;
        if (r.Place == 1)
        {
            winnerAt = Time.time;
            if (racers.Any(x => x.Racing)) MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_CLOCK, Mathf.RoundToInt(AfterWinner)), "");
        }
        ClearAura(r);
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_FINISHED, r.Name, Ordinal(r.Place), Seconds(r.Time)), r.Place == 1 ? "" : "");
    }

    private void Cancel()
    {
        MangroveFeed.Say(Localization.UI_REGATTA_CANCELLED, "");
        phase = Phase.Done;
        current = null;
        nextAllowed = Time.time + 60f;
        Destroy(gameObject);
    }

    private void Finish()
    {
        // those still sailing when the time after the winner ran out are placed as they lie, without a time
        foreach (var r in Ordered().Where(x => x.Racing).ToList())
        {
            r.Unfinished = true;
            r.Place = ++finished;
        }
        phase = Phase.Done;
        current = null;
        nextAllowed = Time.time + Cooldown;
        releaseCameraAt = filmed ? Time.time + CameraAfter : -1f;
        foreach (var r in racers)
        {
            ClearAura(r);
            if (!r.Boat) continue;
            r.Boat.Arrived -= Arrived;
            r.Boat.Pace = 1f;
            r.Boat.Held = false;
            r.Boat.Lane = 0f;
            r.Boat.Raced = false;
        }

        var placed = racers.Where(r => (r.Time >= 0f || r.Unfinished) && r.Player).OrderBy(r => r.Place).ToList();
        if (placed.Count > 0)
        {
            // the Sailing exp is the game's to give; the coins the server's
            for (var i = 0; i < placed.Count; ++i)
            {
                var sailing = placed[i].Player.GetSkill(RavenNest.Models.Skill.Sailing);
                var share = i < ExpShare.Length ? ExpShare[i] : ExpShareOthers;
                if (sailing != null) sailing.AddExp(GameMath.ExperienceForLevel(sailing.Level + 1) * share);
            }
            // alone (a forced start) it is a time trial: the best time counts, not a win
            RegattaRecords.Add(placed.Select(r => (r.Player.Id, r.Player.Name, r.Unfinished ? -1f : r.Time)).ToList(), racers.Count(r => r.Boat) >= 2);
            RegattaRecords.UpdateBoard(island);
            var winner = placed[0];
            var line = string.Join(", ", placed.Skip(1).Take(2).Select(r => Ordinal(r.Place) + " " + r.Player.Name));
            GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_WON, winner.Player.Name, Seconds(winner.Time), line.Length > 0 ? line : "-");
            // the server shares the pot out to the podium (half of every paid meddle)
            if (pot > 0 && placed.Count >= 2) GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_POT, pot);
            Pay(placed.Where(r => !r.Player.IsBot).Select(r => r.Player.Id).ToArray());
        }
        else MangroveFeed.Say(Localization.UI_REGATTA_NOBODY, "");
        Destroy(gameObject, BuoysStay);
    }

    private static async void Pay(Guid[] ids)
    {
        var game = GameManager.Instance;
        if (ids.Length == 0 || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        try
        {
            var rewards = await game.RavenNest.Mangrove.RegattaAsync(ids);
            MangroveRewards.Apply(game, rewards, Localization.UI_REGATTA_PAID, null);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The regatta's prizes could not be paid: " + exc.Message);
        }
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        foreach (var b in buoys) if (b) Destroy(b);
        foreach (var t in things) if (t.Go) Destroy(t.Go);
        foreach (var b in balls) if (b.Go) Destroy(b.Go);
        foreach (var k in krakens)
            foreach (var a in k.Arms)
                if (a.Tentacle) Destroy(a.Tentacle.gameObject);
        foreach (var r in racers)
        {
            ClearAura(r);
            if (r.Boat) r.Boat.Held = false;
        }
    }

    // ---- For the race camera (RaceCamera) ----------------------------------------------------------

    /// <summary>The order now: the finished by place, then who is furthest round the buoys.</summary>
    private IEnumerable<Racer> Ordered() => racers.Where(r => !r.Out && r.Player && (r.Boat || r.Time >= 0f))
        .OrderBy(r => r.Time >= 0f ? r.Place : 1000)
        .ThenByDescending(Progress);

    private float Progress(Racer r) =>
        r.Boat && marks != null ? r.Mark * 10000f - Flat(marks[Mathf.Min(r.Mark, marks.Count - 1)] - r.Boat.SeaPosition).magnitude : -1e9f;

    /// <summary>The start is over: the boats are racing (or done).</summary>
    public bool Started => phase == Phase.Racing || phase == Phase.Done;
    public bool Finished => phase == Phase.Done;
    public float SinceStart => phase == Phase.Racing || phase == Phase.Done ? Time.time - startedAt : 0f;
    public Vector3 StartPoint => start;
    public float StartHeading => startHeading;
    public float StartWidth => startWidth;
    public IReadOnlyList<Vector3> Marks => marks;
    public Vector3 FinishPoint => marks[marks.Count - 1];
    public Vector3 IslandCentre => island ? island.SpawnPosition : start;

    /// <summary>The boat in front that is still racing.</summary>
    public OwnBoat Leader
    {
        get
        {
            var r = Ordered().FirstOrDefault(x => x.Racing);
            return r != null ? r.Boat : null;
        }
    }

    /// <summary>The buoy a boat makes for next (the last mark is the finish).</summary>
    public int NextMark(OwnBoat boat)
    {
        var r = racers.FirstOrDefault(x => x.Boat == boat);
        return r != null ? r.Mark : -1;
    }

    /// <summary>How far a boat is from the finish once on the last leg; far off before it.</summary>
    public float DistanceToFinish(OwnBoat boat)
    {
        var r = racers.FirstOrDefault(x => x.Boat == boat);
        if (r == null || !boat || r.Mark < marks.Count - 1) return float.MaxValue;
        return Flat(FinishPoint - boat.SeaPosition).magnitude;
    }

    /// <summary>The frontmost two racers close enough together to be a battle.</summary>
    public bool Battle(out OwnBoat a, out OwnBoat b)
    {
        a = b = null;
        var order = Ordered().Where(r => r.Racing).ToList();
        for (var i = 0; i + 1 < order.Count; ++i)
        {
            var x = order[i].Boat;
            var y = order[i + 1].Boat;
            if (Flat(x.SeaPosition - y.SeaPosition).magnitude > Mathf.Max(25f, Mathf.Max(x.Length, y.Length) * 2.5f)) continue;
            a = x;
            b = y;
            return true;
        }
        return false;
    }

    public OwnBoat SomeRacer()
    {
        var open = racers.Where(r => r.Racing).ToList();
        return open.Count > 0 ? open[UnityEngine.Random.Range(0, open.Count)].Boat : null;
    }

    /// <summary>The middle of the leading pack (the first four).</summary>
    public Vector3 PackCentre()
    {
        var pack = Ordered().Where(r => r.Racing).Take(4).Select(r => r.Boat.SeaPosition).ToList();
        if (pack.Count == 0) return start;
        var sum = Vector3.zero;
        foreach (var p in pack) sum += p;
        return sum / pack.Count;
    }

    // ---- The course --------------------------------------------------------------------------------

    /// <summary>
    /// Round the island clockwise from a start south of it: buoys west, north west, north east, east and south east,
    /// each on open water at the course's radius or further out, <see cref="Laps"/> times, then the jetty's dock point.
    /// </summary>
    private static List<Vector3> Course(IslandController island, out Vector3 start, out float startHeading, out float finishHeading, out int perLap)
    {
        start = default; startHeading = 0f; finishHeading = 0f; perLap = 0;
        if (!SeaChart.TryGetDock(island.Identifier, out var dock)) return null;
        var centre = island.SpawnPosition;
        Vector3 OnWater(float angle)
        {
            for (var r = Radius; r < Radius + 120f; r += 10f)
            {
                var at = centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * r;
                at.y = SeaChart.Sea;
                if (SeaChart.IsWater(at)) return at;
            }
            return Vector3.zero;
        }
        start = OnWater(195f);
        if (start == Vector3.zero) return null;
        var course = new List<Vector3>();
        foreach (var angle in new[] { 255f, 315f, 25f, 90f, 150f })
        {
            var mark = OnWater(angle);
            if (mark != Vector3.zero) course.Add(mark);
        }
        if (course.Count < 3) return null;
        var first = course[0] - start;
        startHeading = Mathf.Atan2(first.x, first.z) * Mathf.Rad2Deg;
        // round the same buoys again for each lap after the first
        perLap = course.Count;
        var loop = course.ToList();
        for (var lap = 1; lap < Laps; ++lap) course.AddRange(loop);
        course.Add(dock.Position);
        finishHeading = dock.Heading;
        return course;
    }

    private void PlaceBuoys()
    {
        var prefab = Resources.Load<GameObject>("Mangrove/Regatta Buoy");
        if (!prefab) return;
        for (var i = 0; i < perLap; ++i)
        {
            var buoy = Instantiate(prefab, marks[i], Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            buoy.name = "Regatta Buoy " + (i + 1);
            buoy.AddComponent<Floater>();
            buoys.Add(buoy);
        }
    }

    /// <summary>A point part way along the chart's way from one place to another, and the way it runs there.</summary>
    private static bool Along(Vector3 from, Vector3 to, float share, out Vector3 at, out Vector3 direction)
    {
        at = direction = default;
        var way = SeaChart.Route(from, to);
        if (way == null || way.Count < 2) return false;
        var total = 0f;
        for (var i = 1; i < way.Count; ++i) total += Flat(way[i] - way[i - 1]).magnitude;
        var left = total * share;
        for (var i = 1; i < way.Count; ++i)
        {
            var leg = Flat(way[i] - way[i - 1]);
            var length = leg.magnitude;
            if (left > length && i < way.Count - 1) { left -= length; continue; }
            direction = length > 0.01f ? leg / length : Vector3.forward;
            at = way[i - 1] + direction * Mathf.Min(left, length);
            at.y = SeaChart.Sea;
            return true;
        }
        return false;
    }

    /// <summary>
    /// A row of mystery bottles across the middle of each leg of a lap (the later laps sail through the same rows); not
    /// on the way into the harbour.
    /// </summary>
    private void PlaceBottles()
    {
        var prefab = RegattaProps.Get()?.Bottle;
        var from = start;
        for (var leg = 0; leg < perLap; ++leg)
        {
            var to = marks[leg];
            if (Along(from, to, 0.5f, out var at, out var direction))
            {
                var across = Vector3.Cross(Vector3.up, direction);
                foreach (var offset in new[] { -12f, -4f, 4f, 12f })
                {
                    var p = at + across * offset;
                    if (SeaChart.IsWater(p)) Add(Kind.Bottle, p, 2.2f, float.MaxValue, prefab, spin: 40f);
                }
            }
            from = to;
        }
    }

    /// <summary>Two to four reefs or patches of wreckage on the legs round the island, never on a row of bottles.</summary>
    private void PlaceHazards()
    {
        var props = RegattaProps.Get();
        var legs = Enumerable.Range(1, Mathf.Max(0, perLap - 1)).OrderBy(_ => UnityEngine.Random.value).ToList();
        var count = Mathf.Min(legs.Count, UnityEngine.Random.Range(2, 5));
        for (var i = 0; i < count; ++i)
        {
            var leg = legs[i];
            var share = UnityEngine.Random.value < 0.5f ? UnityEngine.Random.Range(0.25f, 0.38f) : UnityEngine.Random.Range(0.62f, 0.78f);
            if (!Along(marks[leg - 1], marks[leg], share, out var at, out var direction)) continue;
            var p = at + Vector3.Cross(Vector3.up, direction) * UnityEngine.Random.Range(-10f, 10f);
            if (!SeaChart.IsWater(p)) continue;
            var reef = UnityEngine.Random.value < 0.6f;
            var go = Add(reef ? Kind.Reef : Kind.Wreck, p, reef ? 4f : 3.5f, float.MaxValue, reef ? props?.Reef : props?.Wreck, floats: !reef).Go;
            if (go) go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        }
    }

    private Thing Add(Kind kind, Vector3 at, float radius, float until, GameObject prefab, Racer owner = null, string by = null,
        float spin = 0f, bool floats = true, float dropSeconds = 0f)
    {
        at.y = SeaChart.Sea;
        var t = new Thing { Kind = kind, At = at, Radius = radius, Until = until, Owner = owner, By = by, OwnerClearAt = Time.time + 3f };
        if (prefab)
        {
            t.Go = Instantiate(prefab, at, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            if (floats)
            {
                var f = t.Go.AddComponent<Floater>();
                f.Spin = spin;
                f.DropSeconds = dropSeconds;
            }
        }
        things.Add(t);
        return t;
    }

    /// <summary>
    /// Rides the swell a little (a buoy, a bottle, a keg); a bottle turns slowly to catch the eye, and a keg lobbed in
    /// falls first and lands with a splash.
    /// </summary>
    private sealed class Floater : MonoBehaviour
    {
        public float Spin;
        public float DropSeconds;
        private Vector3 rest;
        private float phase, born, yaw;
        private bool landed;

        private void Start()
        {
            rest = transform.position;
            phase = UnityEngine.Random.Range(0f, 6f);
            born = Time.time;
            yaw = transform.eulerAngles.y;
            landed = DropSeconds <= 0f;
        }

        private void Update()
        {
            if (!landed)
            {
                var t = Mathf.Clamp01((Time.time - born) / DropSeconds);
                transform.position = rest + Vector3.up * (18f * (1f - t * t));
                if (t < 1f) return;
                landed = true;
                Effect(RegattaProps.Get()?.Splash, rest, 3f);
                GameSounds.Play(SoundKind.WaterSplash, rest, 1f);
            }
            var s = Time.time + phase;
            yaw += Spin * Time.deltaTime;
            transform.position = rest + Vector3.up * (Mathf.Sin(s * 1.3f) * 0.18f);
            transform.rotation = Quaternion.Euler(Mathf.Sin(s * 0.9f) * 6f, yaw, Mathf.Cos(s * 1.1f) * 6f);
        }
    }

    /// <summary>The lap a racer is on, from 0.</summary>
    private int Lap(Racer r) => perLap > 0 ? Mathf.Min(Laps - 1, r.Mark / perLap) : 0;

    /// <summary>One line of the standings on screen (MangroveView).</summary>
    public sealed class Standing
    {
        public int Place;
        public string Name;
        /// <summary>"Lap 2/2", or the finish time.</summary>
        public string Detail;
        /// <summary>Icons for what it has: a held item, its shield, a tailwind; empty for none.</summary>
        public string Glyphs;
        public bool Done;
    }

    /// <summary>Every racer in order while the race runs, for the standings on screen.</summary>
    public static List<Standing> Standings()
    {
        var race = current;
        if (!race || race.phase != Phase.Racing) return null;
        var now = Time.time;
        var list = new List<Standing>();
        var place = 0;
        foreach (var r in race.Ordered())
        {
            var s = new Standing { Place = ++place, Name = r.Name, Done = r.Time >= 0f };
            s.Detail = s.Done
                ? Seconds(r.Time) + " s"
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_LAP, race.Lap(r) + 1, Laps);
            var glyphs = "";
            if (!s.Done)
            {
                if (now < r.ShieldUntil) glyphs += "";
                if (now < r.BoostUntil) glyphs += "";
                switch (r.Item)
                {
                    case Item.Tailwind: glyphs += ""; break;
                    case Item.Cannon: glyphs += ""; break;
                    case Item.Shield: glyphs += ""; break;
                    case Item.Peel: glyphs += ""; break;
                }
            }
            s.Glyphs = glyphs;
            list.Add(s);
        }
        return list;
    }

    // ---- The bar -----------------------------------------------------------------------------------

    public static Shinobytes.UI.MangroveView.Status CurrentStatus()
    {
        if (!current) return null;
        if (current.phase == Phase.Signup)
        {
            var left = Mathf.Max(0, Mathf.CeilToInt(current.signupEnds - Time.time));
            return new Shinobytes.UI.MangroveView.Status
            {
                Glyph = "",
                Title = string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BAR_SIGNUP, left),
                Line = string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BAR_SIGNUP_LINE, current.racers.Count, MostRacers),
                Progress = 1f - left / SignupSeconds,
            };
        }
        if (current.phase == Phase.Countdown)
        {
            var left = current.goAt - Time.time;
            return new Shinobytes.UI.MangroveView.Status
            {
                Glyph = "",
                Title = left > CountSeconds
                    ? Localization.UI_REGATTA_BAR_LINEUP
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BAR_COUNT, Mathf.CeilToInt(left)),
                Line = Localization.UI_REGATTA_BAR_HINT,
                Progress = 1f - Mathf.Clamp01(left / (LineUpSeconds + CountSeconds)),
            };
        }
        if (current.phase != Phase.Racing) return null;
        // the order is in the standings beside it; the bar keeps the time, the leader's lap and how to meddle
        var leader = current.Ordered().FirstOrDefault(r => r.Racing);
        var lap = leader != null ? current.Lap(leader) + 1 : Laps;
        return new Shinobytes.UI.MangroveView.Status
        {
            Glyph = "",
            Title = string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BAR_RACING, Seconds(Time.time - current.startedAt))
                + "  " + string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_LAP, lap, Laps),
            Line = Localization.UI_REGATTA_BAR_HINT,
        };
    }

    public static string Seconds(float s) => s.ToString("0.0", CultureInfo.InvariantCulture);

    public static string Ordinal(int place) => place switch
    {
        1 => Localization.UI_REGATTA_1ST,
        2 => Localization.UI_REGATTA_2ND,
        3 => Localization.UI_REGATTA_3RD,
        _ => string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_NTH, place),
    };

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}

/// <summary>
/// Viewers' meddling in the regatta (docs/banana-island.md, "Pickups, hazards and meddling"): a free keg or wind now
/// and then, at a random racer; before the free one is back, or aimed at a racer by name, it costs coins, and the
/// first time the viewer is told the price and sends the command again to pay.
/// </summary>
public static class RegattaMeddling
{
    public const float FreeEvery = 60f;
    public const long RandomPrice = 500;
    public const long AimedPrice = 2000;
    public const float ConfirmSeconds = 30f;

    private static readonly Dictionary<Guid, float> freeAt = new Dictionary<Guid, float>();
    private static readonly Dictionary<Guid, (string What, float Until)> pending = new Dictionary<Guid, (string, float)>();

    /// <summary>Seconds until the viewer's next free one; 0 when it is ready.</summary>
    public static float FreeIn(Guid viewer) => freeAt.TryGetValue(viewer, out var at) ? Mathf.Max(0f, at - Time.time) : 0f;

    public static void UsedFree(Guid viewer) => freeAt[viewer] = Time.time + FreeEvery;

    /// <summary>True when the viewer sends the same paid command a second time in time: go ahead and pay.</summary>
    public static bool Confirmed(Guid viewer, string what)
    {
        if (pending.TryGetValue(viewer, out var p) && p.What == what && Time.time < p.Until)
        {
            pending.Remove(viewer);
            return true;
        }
        pending[viewer] = (what, Time.time + ConfirmSeconds);
        return false;
    }
}

/// <summary>
/// The regatta's records, kept by the game (states/mangrove-regatta.json): each racer's wins and best time, shown on the
/// board by the Mangrove island's jetty ("Regatta Board", made by tools/mangrove/mangrove-monkeys.cs).
/// </summary>
public static class RegattaRecords
{
    public class Record
    {
        public Guid UserId;
        public string Name;
        public int Wins;
        public int Races;
        public float Best;
    }

    private static JsonStore<List<Record>> store;
    private static List<Record> records;

    private static List<Record> Records
    {
        get
        {
            if (records != null) return records;
            store = JsonStore<List<Record>>.Create("mangrove-regatta");
            try { records = store.Get() ?? new List<Record>(); }
            catch { records = new List<Record>(); }
            return records;
        }
    }

    public static void Add(List<(Guid Id, string Name, float Time)> placed, bool race = true)
    {
        for (var i = 0; i < placed.Count; ++i)
        {
            var (id, name, time) = placed[i];
            var r = Records.FirstOrDefault(x => x.UserId == id);
            if (r == null) Records.Add(r = new Record { UserId = id, Best = float.MaxValue });
            r.Name = name;
            r.Races++;
            if (i == 0 && race) r.Wins++;
            // an unfinished racer (time -1) counts a race but no time
            if (time > 0f && (time < r.Best || r.Best <= 0f)) r.Best = time;
        }
        try { store.Save(Records); }
        catch (Exception exc) { Shinobytes.Debug.LogWarning("The regatta's records could not be saved: " + exc.Message); }
    }

    /// <summary>Most wins first, then the best time.</summary>
    public static IEnumerable<Record> Top(int count) => Records.OrderByDescending(r => r.Wins).ThenBy(r => r.Best).Take(count);

    public static void UpdateBoard(IslandController island)
    {
        if (!island) return;
        var board = island.transform.Find("Regatta Board");
        var text = board ? board.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
        if (!text) return;
        var lines = new List<string> { "<b>" + Localization.UI_REGATTA_BOARD + "</b>" };
        var n = 0;
        foreach (var r in Top(5))
            lines.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BOARD_LINE, ++n, r.Name, r.Wins, r.Best < 1e6f ? Regatta.Seconds(r.Best) : "-"));
        if (n == 0) lines.Add(Localization.UI_REGATTA_BOARD_EMPTY);
        text.text = string.Join("\n", lines);
    }
}
