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

    /// <summary>Guests aboard (docs/own-boats.md, "Guests aboard"), each in a seat of their own.</summary>
    public IReadOnlyList<PlayerController> Guests => guests;
    /// <summary>Guests sit at the oars on the small boats; the sloop's stand on its deck.</summary>
    public bool GuestsSit => Tier != BoatItems.Tier.Sloop;

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
        (boat.length, boat.width, boat.turnRate) = tier switch
        {
            BoatItems.Tier.Sloop => (16f, 5f, 35f),
            BoatItems.Tier.FishingBoat => (8.5f, 3f, 45f),
            _ => (5.4f, 2.2f, 60f),
        };
        boat.Place();
        return boat;
    }

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
        route = null;
        State = Course.Anchored;
    }

    private void Follow(List<Vector3> way, Course course)
    {
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

    private void Update()
    {
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
        var dt = GameTime.deltaTime;
        if (Moving) Steer(dt);
        if (dt > 0f) speedNow = Mathf.Lerp(speedNow, Flat(flat - was).magnitude / dt, 1f - Mathf.Exp(-dt * 6f));
        Place();
        Row(dt);
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
        var speed = Speed;
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

    private void Place()
    {
        var rotation = Quaternion.Euler(0, heading, 0);
        var position = flat;
        // the waves only near the camera (and not in potato mode): far off nobody sees a boat bob
        var potato = GameManager.Instance && GameManager.Instance.PotatoMode;
        var camera = Camera.main;
        var near = !camera || (camera.transform.position - flat).sqrMagnitude < 300f * 300f;
        if (!potato && near && Ocean.Float(flat, rotation * Vector3.forward, rotation * Vector3.right, length, width, out var heave, out var up))
        {
            position.y += heave * 0.9f;
            rotation = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, up, 0.9f)) * rotation;
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Where it is on the water, for saving and for a restart.</summary>
    public Vector3 SeaPosition => flat;
    public float Heading => heading;

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
