using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// The regatta round the Mangrove island (docs/banana-island.md): players with an own boat join with !regatta, which
/// opens the sign-up the first time; after it (or when the broadcaster says !regatta start) their boats come up at a
/// start line south of the island, race round the buoys and finish at its jetty, where they land. Each boat goes at its
/// own speed (Sailing level and boat, capped: SailingSpeeds), and gusts and lulls through the race keep it open. The
/// places pay coins (the server) and Sailing exp (the game); wins and best times go on the board by the jetty.
/// </summary>
public sealed class Regatta : MonoBehaviour
{
    public const float SignupSeconds = 90f;
    public const float Cooldown = 600f;
    public const int MostRacers = 12;
    public const float GiveUpAfter = 300f;
    private const float Radius = 150f;
    private const float StartSpacing = 10f;
    private const float BuoysStay = 15f;
    /// <summary>The Sailing exp of a place, as a share of what the racer's next level takes.</summary>
    private static readonly float[] ExpShare = { 0.05f, 0.035f, 0.025f };
    private const float ExpShareOthers = 0.015f;

    public enum Phase { Signup, Racing, Done }
    public enum JoinResult { Joined, Already, NotOpen, NoBoat, Busy, Full, Underway, Cooldown }

    private sealed class Racer
    {
        public PlayerController Player;
        public OwnBoat Boat;
        public float Time = -1f;
        public int Place;
        public bool Out;
        public float NextGust;
    }

    private static Regatta current;
    private static float nextAllowed;

    private readonly List<Racer> racers = new List<Racer>();
    private readonly List<GameObject> buoys = new List<GameObject>();
    private IslandController island;
    private List<Vector3> marks;
    private Phase phase;
    private float signupEnds;
    private float startedAt;
    private int finished;
    private bool forced;

    public static bool Running => current && current.phase != Phase.Done;
    public static float CooldownLeft => Mathf.Max(0f, nextAllowed - Time.time);

    // ---- !regatta ----------------------------------------------------------------------------------

    public static JoinResult Join(PlayerController player)
    {
        var island = Mangrove();
        if (!island || island.Locked) return JoinResult.NotOpen;
        if (current && current.phase == Phase.Racing) return JoinResult.Underway;
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

            case Phase.Racing:
                foreach (var r in racers)
                {
                    if (r.Out || r.Time >= 0f) continue;
                    // left the game, or the boat was put away (a raid or dungeon took them): out of the race
                    if (!r.Player || !r.Boat || r.Player.ferryHandler.OwnBoat != r.Boat) { r.Out = true; continue; }
                    if (Time.time >= r.NextGust)
                    {
                        r.NextGust = Time.time + UnityEngine.Random.Range(5f, 11f);
                        r.Boat.Pace = UnityEngine.Random.Range(0.82f, 1.18f);
                    }
                }
                if (racers.All(r => r.Out || r.Time >= 0f) || Time.time - startedAt > GiveUpAfter) Finish();
                return;
        }
    }

    private void Begin()
    {
        marks = Course(island, out var start, out var startHeading, out var finishHeading);
        if (marks == null)
        {
            Shinobytes.Debug.LogWarning("The regatta found no course round the Mangrove island.");
            Cancel();
            return;
        }
        phase = Phase.Racing;
        startedAt = Time.time;
        PlaceBuoys();

        // line abreast across the start, facing the first buoy
        var forward = Quaternion.Euler(0f, startHeading, 0f) * Vector3.forward;
        var side = Vector3.Cross(Vector3.up, forward);
        for (var i = 0; i < racers.Count; ++i)
        {
            var r = racers[i];
            var at = start + side * ((i - (racers.Count - 1) * 0.5f) * StartSpacing);
            if (!SeaChart.IsWater(at)) at = start - forward * (i * StartSpacing);
            at.y = SeaChart.Sea;
            r.Boat = r.Player.ferryHandler.RaceOwnBoat(at, startHeading, marks, island, finishHeading);
            if (!r.Boat) { r.Out = true; continue; }
            r.Boat.Arrived += Arrived;
            r.NextGust = Time.time + UnityEngine.Random.Range(2f, 6f);
        }
        var racing = racers.Count(r => !r.Out);
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_STARTED, racing), "");
        GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_START, racing);
    }

    private void Arrived(OwnBoat boat)
    {
        boat.Arrived -= Arrived;
        boat.Pace = 1f;
        var r = racers.FirstOrDefault(x => x.Boat == boat);
        if (r == null || r.Time >= 0f || r.Out || phase != Phase.Racing) return;
        r.Time = Time.time - startedAt;
        r.Place = ++finished;
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_FINISHED, r.Player ? r.Player.Name : "?", Ordinal(r.Place), Seconds(r.Time)), r.Place == 1 ? "" : "");
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
        phase = Phase.Done;
        current = null;
        nextAllowed = Time.time + Cooldown;
        foreach (var r in racers) if (r.Boat) { r.Boat.Arrived -= Arrived; r.Boat.Pace = 1f; }

        var placed = racers.Where(r => r.Time >= 0f && r.Player).OrderBy(r => r.Place).ToList();
        if (placed.Count > 0)
        {
            // the Sailing exp is the game's to give; the coins the server's
            for (var i = 0; i < placed.Count; ++i)
            {
                var sailing = placed[i].Player.GetSkill(RavenNest.Models.Skill.Sailing);
                var share = i < ExpShare.Length ? ExpShare[i] : ExpShareOthers;
                if (sailing != null) sailing.AddExp(GameMath.ExperienceForLevel(sailing.Level + 1) * share);
            }
            RegattaRecords.Add(placed.Select(r => (r.Player.Id, r.Player.Name, r.Time)).ToList());
            RegattaRecords.UpdateBoard(island);
            var winner = placed[0];
            var line = string.Join(", ", placed.Skip(1).Take(2).Select(r => Ordinal(r.Place) + " " + r.Player.Name));
            GameManager.Instance.RavenBot?.Announce(Localization.MSG_REGATTA_WON, winner.Player.Name, Seconds(winner.Time), line.Length > 0 ? line : "-");
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
    }

    // ---- The course --------------------------------------------------------------------------------

    /// <summary>
    /// Round the island clockwise from a start south of it: buoys west, north west, north east, east and south east,
    /// each on open water at the course's radius or further out, then the jetty's dock point.
    /// </summary>
    private static List<Vector3> Course(IslandController island, out Vector3 start, out float startHeading, out float finishHeading)
    {
        start = default; startHeading = 0f; finishHeading = 0f;
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
        course.Add(dock.Position);
        finishHeading = dock.Heading;
        return course;
    }

    private void PlaceBuoys()
    {
        var prefab = Resources.Load<GameObject>("Mangrove/Regatta Buoy");
        if (!prefab) return;
        for (var i = 0; i < marks.Count - 1; ++i)
        {
            var buoy = Instantiate(prefab, marks[i], Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            buoy.name = "Regatta Buoy " + (i + 1);
            buoy.AddComponent<Bob>();
            buoys.Add(buoy);
        }
    }

    /// <summary>A buoy rides the swell a little.</summary>
    private sealed class Bob : MonoBehaviour
    {
        private Vector3 rest;
        private float phase;
        private void Start() { rest = transform.position; phase = UnityEngine.Random.Range(0f, 6f); }
        private void Update()
        {
            var t = Time.time + phase;
            transform.position = rest + Vector3.up * (Mathf.Sin(t * 1.3f) * 0.18f);
            transform.rotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 6f, transform.eulerAngles.y, Mathf.Cos(t * 1.1f) * 6f);
        }
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
        if (current.phase != Phase.Racing) return null;
        // the order now: who is furthest along the course
        var order = current.racers.Where(r => !r.Out && r.Player)
            .OrderBy(r => r.Time >= 0f ? r.Place : 1000)
            .ThenByDescending(r => r.Boat ? r.Boat.CourseNext - Vector3.Distance(r.Boat.SeaPosition, r.Boat.CourseTarget) / 1000f : -1f)
            .Take(3).Select(r => r.Player.Name);
        return new Shinobytes.UI.MangroveView.Status
        {
            Glyph = "",
            Title = string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BAR_RACING, Seconds(Time.time - current.startedAt)),
            Line = string.Join("  ", order.Select((n, i) => Ordinal(i + 1) + " " + n)),
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

    public static void Add(List<(Guid Id, string Name, float Time)> placed)
    {
        for (var i = 0; i < placed.Count; ++i)
        {
            var (id, name, time) = placed[i];
            var r = Records.FirstOrDefault(x => x.UserId == id);
            if (r == null) Records.Add(r = new Record { UserId = id, Best = float.MaxValue });
            r.Name = name;
            r.Races++;
            if (i == 0) r.Wins++;
            if (time < r.Best || r.Best <= 0f) r.Best = time;
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
            lines.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_BOARD_LINE, ++n, r.Name, r.Wins, Regatta.Seconds(r.Best)));
        if (n == 0) lines.Add(Localization.UI_REGATTA_BOARD_EMPTY);
        text.text = string.Join("\n", lines);
    }
}
