using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A player's own boat out on the sea (docs/own-boats.md, phase 2): it exists only while its owner is aboard. It sails
/// a route from <see cref="SeaChart"/> at the owner's speed (<see cref="SailingSpeeds"/>), turning as a boat does
/// rather than on the spot, and rides the waves (<see cref="Ocean.Float"/>). Going to an island it comes alongside that
/// island's dock point and says so (<see cref="Arrived"/>); wandering, it picks somewhere new each time it gets there.
/// Its oars row while it moves (<see cref="Row"/>), a stroke for every few metres it goes, so the faster the owner
/// sails the faster they row.
/// </summary>
public sealed class OwnBoat : MonoBehaviour
{
    public enum Course { Sailing, Wandering, Anchored, Arrived }

    private static readonly Dictionary<BoatItems.Tier, GameObject> prefabs = new Dictionary<BoatItems.Tier, GameObject>();
    private static readonly System.Random random = new System.Random();

    public PlayerController Owner { get; private set; }
    public BoatItems.Tier Tier { get; private set; }
    public Transform Seat { get; private set; }
    public Course State { get; private set; }
    /// <summary>The island it sails for; null while wandering or anchored.</summary>
    public string Destination { get; private set; }
    public bool Moving => (State == Course.Sailing || State == Course.Wandering) && route != null;
    /// <summary>The owner's speed, and a little more for each guest at the oars (within the boat's cap).</summary>
    public float Speed => SailingSpeeds.Boat(Tier, Owner ? Owner.Stats.Sailing.Level : 1, guests.Count);

    /// <summary>The wind's share of its speed: 1, or a gust or a lull while racing the regatta (Regatta).</summary>
    [NonSerialized] public float Pace = 1f;

    /// <summary>Held at the regatta's start line, sails furled, until the start (Regatta).</summary>
    [NonSerialized] public bool Held;

    /// <summary>
    /// Metres to starboard of its course it sails (the regatta's lanes, so racers sail beside each other and not through
    /// each other): the boat steers its course as ever and sits this far to the side of it, turned a little into a
    /// lane change. The regatta eases it, and brings it back to 0 before the dock.
    /// </summary>
    [NonSerialized] public float Lane;

    /// <summary>Racing the regatta: put away at the finish it lies at the dock only a moment (FerryHandler).</summary>
    [NonSerialized] public bool Raced;

    /// <summary>Changes whenever it is given a new course or anchors: the regatta drops a racer whose course is not the race's.</summary>
    public int CourseVersion { get; private set; }

    private float lastLane, laneYaw;

    /// <summary>Spinning round after a banana peel or a mine (Regatta): it goes round once and barely moves.</summary>
    public bool Spinning => spinLeft > 0f;
    private float spinLeft, spinSeconds;

    /// <summary>Guests aboard (docs/own-boats.md, "Guests aboard"), each in a seat of their own.</summary>
    public IReadOnlyList<PlayerController> Guests => guests;
    /// <summary>Guests sit at the oars on the small boats; the sloop's stand on its deck.</summary>
    /// <summary>On the small boats guests sit; on the ships (Sloop and up) everyone stands on deck.</summary>
    public bool GuestsSit => Tier == BoatItems.Tier.Rowboat || Tier == BoatItems.Tier.FishingBoat;

    private readonly List<PlayerController> guests = new List<PlayerController>();
    private readonly List<Transform> guestSeats = new List<Transform>();
    private readonly List<GameObject> guestOars = new List<GameObject>();

    /// <summary>Raised once when it comes alongside its destination's dock point.</summary>
    public event Action<OwnBoat> Arrived;

    /// <summary>Roaming (!sail with no island) goes at this share of the boat's speed.</summary>
    private const float WanderPace = 0.45f;

    // rowing: a stroke for every StrokeMetres the boat goes; the blade sweeps SweepDegrees either way of where the kit
    // left it and lifts LiftDegrees out of the water to come forward again
    private const float StrokeMetres = 8f, SweepDegrees = 28f, LiftDegrees = 9f;

    private struct Oar
    {
        public Transform Transform;
        /// <summary>Its turn against the boat as the kit left it, resting.</summary>
        public Quaternion Rest;
        /// <summary>1 on the starboard side, -1 on the port side.</summary>
        public float Side;
    }

    private readonly List<Oar> oars = new List<Oar>();
    private float strokePhase;
    private float rowing;   // 0 resting .. 1 rowing, eased so the oars settle when the boat stops
    private float speedNow; // metres a second it actually went, smoothed

    private BoatSails sails;
    // put away at a dock: it lies there a while, sails furled, then goes (Moor)
    private float mooredFor = -1f;

    private List<Vector3> route;
    private int next;
    private float heading; // degrees, 0 = north (+Z)
    private float length = 5f, width = 2f;
    private float turnRate = 60f;
    private float arrivalHeading;
    private Vector3 flat;  // position on the sea's surface, before the waves

    /// <summary>Puts the boat on the water, facing a way, ready to take its owner aboard.</summary>
    public static OwnBoat Launch(PlayerController owner, BoatItems.Tier tier, Vector3 at, float heading)
    {
        var prefab = Prefab(tier);
        var go = prefab ? Instantiate(prefab) : new GameObject("Own boat");
        go.name = "Own boat (" + (owner ? owner.Name : "?") + ")";
        var boat = go.AddComponent<OwnBoat>();
        boat.Owner = owner;
        boat.Tier = tier;
        boat.Seat = go.transform.Find("Seat") ?? go.transform;
        // GuestSeat1, 2... and the fishing boat's front oars, off until a guest takes them (tools/sailing/make-boat-prefabs.cs)
        for (var i = 1; ; i++)
        {
            var guestSeat = go.transform.Find("GuestSeat" + i);
            if (!guestSeat) break;
            boat.guestSeats.Add(guestSeat);
        }
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "SM_Prop_Oar_01" && !t.gameObject.activeSelf) boat.guestOars.Add(t.gameObject);
            // every oar rows, the guests' too while they are out; each turns at its own origin, the rowlock on the gunwale
            if (t.name.StartsWith("SM_Prop_Oar", StringComparison.Ordinal))
                boat.oars.Add(new Oar { Transform = t, Rest = Quaternion.Inverse(go.transform.rotation) * t.rotation, Side = Mathf.Sign(go.transform.InverseTransformPoint(t.position).x) });
        }
        boat.heading = heading;
        boat.flat = new Vector3(at.x, SeaChart.Sea, at.z);
        boat.State = Course.Anchored;
        // its look, from the boat stack's Tag (docs/own-boats.md, "Customizing boats")
        BoatAssembler.Apply(go, tier, BoatLook.TagOf(owner), owner);
        // a ship comes alongside with its sails furled and sets them as it pulls away (docs/own-boats.md, "Sails in port")
        boat.sails = go.GetComponent<BoatSails>();
        if (boat.sails) boat.sails.Snap(true);
        (boat.length, boat.width, boat.turnRate) = Size(tier);
        boat.Place();
        return boat;
    }

    /// <summary>A boat's length and beam in metres, and how many degrees a second it turns.</summary>
    public static (float Length, float Width, float TurnRate) Size(BoatItems.Tier tier) => tier switch
    {
        BoatItems.Tier.Galleon => (40f, 11f, 22f),
        BoatItems.Tier.Brigantine => (30f, 8f, 28f),
        BoatItems.Tier.Sloop => (17f, 6.6f, 35f),
        BoatItems.Tier.FishingBoat => (8.5f, 3f, 45f),
        _ => (5.4f, 2.2f, 60f),
    };

    private static GameObject Prefab(BoatItems.Tier tier)
    {
        if (prefabs.TryGetValue(tier, out var prefab)) return prefab;
        // made by tools/sailing/make-boat-prefabs.cs, referenced from the scene by OwnBoatPrefabs
        prefab = OwnBoatPrefabs.Get(tier);
        prefabs[tier] = prefab;
        return prefab;
    }

    /// <summary>Sails for an island's dock point; false when the chart has no way there.</summary>
    public bool SailTo(string island)
    {
        if (!SeaChart.TryGetDock(island, out var dock)) return false;
        var way = SeaChart.Route(flat, dock.Position);
        if (way == null) return false;
        Destination = island;
        arrivalHeading = dock.Heading;
        Follow(way, Course.Sailing);
        return true;
    }

    /// <summary>
    /// Sails a course of marks in turn (the regatta round the Mangrove island), the chart's way between each, and
    /// arrives at the last as at a dock: at this island's, landing there (FerryHandler).
    /// </summary>
    public bool SailCourse(IReadOnlyList<Vector3> marks, string finishIsland, float finishHeading)
    {
        if (marks == null || marks.Count == 0) return false;
        var way = new List<Vector3> { flat };
        var from = flat;
        foreach (var mark in marks)
        {
            var leg = SeaChart.Route(from, mark);
            if (leg == null) return false;
            for (var i = 1; i < leg.Count; ++i) way.Add(leg[i]);
            from = mark;
        }
        Destination = finishIsland;
        arrivalHeading = finishHeading;
        Follow(way, Course.Sailing);
        return true;
    }

    /// <summary>How far along its course it is: the point it makes for next, of how many (the regatta's places).</summary>
    public int CourseNext => route != null ? next : 0;
    public int CourseLength => route != null ? route.Count : 0;
    public Vector3 CourseTarget => route != null && next < route.Count ? route[next] : flat;

    /// <summary>Wanders the sea until told where to go.</summary>
    public void Wander()
    {
        Destination = null;
        var way = SeaChart.Wander(flat, random);
        if (way == null) { Anchor(); return; }
        Follow(way, Course.Wandering);
    }

    /// <summary>Lies still where it is (to fish, later).</summary>
    public void Anchor()
    {
        CourseVersion++;
        route = null;
        State = Course.Anchored;
    }

    private void Follow(List<Vector3> way, Course course)
    {
        CourseVersion++;
        route = way;
        next = 1;
        State = course;
    }

    /// <summary>A free seat for a guest, or null when the boat is full.</summary>
    public Transform TakeSeat(PlayerController guest)
    {
        if (!guest) return null;
        if (guests.Contains(guest)) return guestSeats[guests.IndexOf(guest)];
        guests.RemoveAll(g => !g);
        if (guests.Count >= guestSeats.Count) return null;
        guests.Add(guest);
        ShowGuestOars();
        return guestSeats[guests.Count - 1];
    }

    public void FreeSeat(PlayerController guest)
    {
        var at = guests.IndexOf(guest);
        if (at < 0) return;
        guests.RemoveAt(at);
        Reseat();
    }

    /// <summary>The guests left move up a seat, so the seats in use are always the first (the fishing boat's oars).</summary>
    private void Reseat()
    {
        for (var i = 0; i < guests.Count; i++)
        {
            if (!guests[i] || guests[i].transform.parent == guestSeats[i]) continue;
            guests[i].transform.SetParent(guestSeats[i]);
            guests[i].transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        ShowGuestOars();
    }

    private void ShowGuestOars()
    {
        foreach (var oar in guestOars) if (oar) oar.SetActive(guests.Count > 0);
    }

    /// <summary>
    /// Its owner stepped ashore: it lies at the dock with its sails furled for a while, then goes. Its owner sailing again
    /// takes it away at once (FerryHandler), so two of theirs never lie in one place.
    /// </summary>
    public void Moor(float seconds)
    {
        route = null;
        guests.Clear();
        State = Course.Arrived;
        mooredFor = seconds;
        if (sails) sails.Furl(true);
    }

    public bool Moored => mooredFor >= 0f;

    /// <summary>Goes round once over these seconds, as good as stopped (a banana peel, a mine).</summary>
    public void Spin(float seconds)
    {
        if (seconds <= 0f || Spinning) return;
        spinSeconds = spinLeft = seconds;
    }

    /// <summary>Coming in to a dock: the last stretch, in which a ship takes in its sails.</summary>
    private bool NearingPort => State == Course.Sailing && route != null && route.Count > 0
        && Flat(route[route.Count - 1] - flat).magnitude < Mathf.Max(30f, length * 1.5f);

    private void Update()
    {
        var dt = GameTime.deltaTime;
        if (Moored)
        {
            mooredFor -= dt;
            if (mooredFor < 0f || !Owner) { Destroy(gameObject); return; }
            speedNow = Mathf.Lerp(speedNow, 0f, 1f - Mathf.Exp(-dt * 6f));
            Place();
            Row(dt);
            return;
        }
        // its owner gone (left the game): nobody to sail it. Its guests are lifted out first (they would go with it);
        // each lands at the nearest dock on its own (FerryHandler)
        if (!Owner)
        {
            foreach (var guest in guests) if (guest) guest.transform.SetParent(null);
            Destroy(gameObject);
            return;
        }
        if (guests.RemoveAll(g => !g) > 0) Reseat();
        var was = flat;
        if (Spinning)
        {
            // round once, eased in and out, drifting on a little the way it was going
            var before = 1f - spinLeft / spinSeconds;
            spinLeft = Mathf.Max(0f, spinLeft - dt);
            var after = 1f - spinLeft / spinSeconds;
            heading += (Mathf.SmoothStep(0f, 1f, after) - Mathf.SmoothStep(0f, 1f, before)) * 360f;
            flat += Quaternion.Euler(0f, heading, 0f) * Vector3.forward * (Speed * 0.08f * dt);
        }
        else if (Moving && !Held) Steer(dt);
        if (dt > 0f) speedNow = Mathf.Lerp(speedNow, Flat(flat - was).magnitude / dt, 1f - Mathf.Exp(-dt * 6f));
        Place();
        Row(dt);
        // under sail on the open sea; furled at anchor, at the start line and coming in to a dock
        if (sails) sails.Furl(!Moving || Held || NearingPort);
    }

    /// <summary>
    /// The oars row as fast as the boat goes: the blade sweeps aft in the water (the drive), lifts and swings forward
    /// again (the recovery). Stopped, they ease back to where they rest. Turned about the boat's own up and fore and
    /// aft axes at each oar's rowlock, mirrored for the port side.
    /// </summary>
    private void Row(float dt)
    {
        if (oars.Count == 0 || dt <= 0f) return;
        rowing = Mathf.MoveTowards(rowing, speedNow > 0.5f ? 1f : 0f, dt * 2f);
        strokePhase = (strokePhase + speedNow / StrokeMetres * dt) % 1f;
        if (rowing <= 0f && strokePhase == 0f) return;
        var angle = strokePhase * Mathf.PI * 2f;
        // sin: blade forward at the catch (-1) to aft at the release (1); cos > 0 while it drives through the water
        var sweep = Mathf.Sin(angle) * SweepDegrees * rowing;
        var lift = Mathf.Max(0f, -Mathf.Cos(angle)) * LiftDegrees * rowing;
        foreach (var oar in oars)
        {
            if (!oar.Transform || !oar.Transform.gameObject.activeInHierarchy) continue;
            // about +Y a starboard blade (+X) swings aft as the angle grows, a port one forward; about +Z it rises
            var yaw = Quaternion.AngleAxis(sweep * oar.Side, Vector3.up);
            var roll = Quaternion.AngleAxis(lift * oar.Side, Vector3.forward);
            oar.Transform.rotation = transform.rotation * yaw * roll * oar.Rest;
        }
    }

    private void Steer(float dt)
    {
        if (dt <= 0f) return;
        // past points close enough to count as reached; the last one must be reached properly
        while (next < route.Count - 1 && Flat(route[next] - flat).magnitude < Math.Max(8f, length))
            next++;

        var target = route[next];
        var toTarget = Flat(target - flat);
        var distance = toTarget.magnitude;
        var last = next == route.Count - 1;

        if (last && distance < 1.5f)
        {
            flat = new Vector3(target.x, flat.y, target.z);
            route = null;
            if (State == Course.Wandering) { Wander(); return; }
            heading = arrivalHeading;
            State = Course.Arrived;
            Arrived?.Invoke(this);
            return;
        }

        var wanted = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        var turn = Mathf.DeltaAngle(heading, wanted);
        heading += Mathf.Clamp(turn, -turnRate * dt, turnRate * dt);

        // roaming with nowhere to be, it takes its time (decided 2026-10-28); bound for an island it goes at full speed
        var speed = Speed * Pace;
        if (State == Course.Wandering) speed *= WanderPace;
        // slower through a sharp turn and coming alongside, so it never overshoots or circles its mark
        speed *= Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(Mathf.Abs(turn) / 90f));
        // a mark inside the turning circle is never reached at speed: at the fishing boat's top speed (36 m/s) and 45
        // degrees a second the circle is 46 m across, and a high level owner circled a point by the dock for good.
        // Slow until the circle fits in half the distance to it
        if (Mathf.Abs(turn) > 15f) speed = Mathf.Min(speed, Mathf.Max(2f, turnRate * Mathf.Deg2Rad * distance * 0.5f));
        if (last) speed = Mathf.Min(speed, Mathf.Max(2f, distance * 0.8f));
        var forward = Quaternion.Euler(0, heading, 0) * Vector3.forward;
        // close in, steer straight at the mark so the last metres are never missed
        var step = last && distance < speed * dt * 4f ? toTarget.normalized : forward;
        flat += step * Mathf.Min(speed * dt, distance);
    }

    /// <summary>
    /// Where it will be after this many metres more along its course, round its next turns, in its lane (the regatta's
    /// kegs land in a racer's way). Straight ahead when it has no course.
    /// </summary>
    public Vector3 Ahead(float metres)
    {
        var forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        if (route == null) return SeaPosition + forward * metres;
        var at = flat;
        var left = metres;
        for (var i = next; i < route.Count && left > 0f; ++i)
        {
            var leg = Flat(route[i] - at);
            var length = leg.magnitude;
            if (length < 0.01f) continue;
            forward = leg / length;
            var step = Mathf.Min(length, left);
            at += forward * step;
            left -= step;
        }
        return at + Vector3.Cross(Vector3.up, forward) * Lane;
    }

    /// <summary>Where it sits: its course, and its lane to starboard of it (<see cref="Lane"/>).</summary>
    private Vector3 Laned => Lane == 0f ? flat : flat + Quaternion.Euler(0f, heading, 0f) * Vector3.right * Lane;

    private void Place()
    {
        // a lane change turns the bow a little the way it moves
        var dt = GameTime.deltaTime;
        if (dt > 0f)
        {
            var sideways = (Lane - lastLane) / dt;
            laneYaw = Mathf.Lerp(laneYaw, Mathf.Atan2(sideways, Mathf.Max(4f, speedNow)) * Mathf.Rad2Deg, 1f - Mathf.Exp(-dt * 5f));
        }
        lastLane = Lane;
        var rotation = Quaternion.Euler(0, heading + laneYaw, 0);
        var position = Laned;
        // the waves only near the camera (and not in potato mode): far off nobody sees a boat bob
        var potato = GameManager.Instance && GameManager.Instance.PotatoMode;
        var camera = Camera.main;
        var near = !camera || (camera.transform.position - position).sqrMagnitude < 300f * 300f;
        if (!potato && near && Ocean.Float(position, rotation * Vector3.forward, rotation * Vector3.right, length, width, out var heave, out var up))
        {
            position.y += heave * 0.9f;
            rotation = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, up, 0.9f)) * rotation;
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Where it is on the water, for saving and for a restart.</summary>
    public Vector3 SeaPosition => Laned;
    public float Heading => heading;
    public float Length => length;
    public float Width => width;

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
