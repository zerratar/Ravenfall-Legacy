using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A player's own boat out on the sea (docs/own-boats.md, phase 2): it exists only while its owner is aboard. It sails
/// a route from <see cref="SeaChart"/> at the owner's speed (<see cref="SailingSpeeds"/>), turning as a boat does
/// rather than on the spot, and rides the waves (<see cref="Ocean.Float"/>). Going to an island it comes alongside that
/// island's dock point and says so (<see cref="Arrived"/>); wandering, it picks somewhere new each time it gets there.
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
    public float Speed => SailingSpeeds.Boat(Tier, Owner ? Owner.Stats.Sailing.Level : 1);

    /// <summary>Raised once when it comes alongside its destination's dock point.</summary>
    public event Action<OwnBoat> Arrived;

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

    private void Update()
    {
        // its owner gone (left the game): nobody to sail it
        if (!Owner) { Destroy(gameObject); return; }
        if (Moving) Steer(GameTime.deltaTime);
        Place();
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

        // slower through a sharp turn and coming alongside, so it never overshoots or circles its mark
        var speed = Speed;
        speed *= Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(Mathf.Abs(turn) / 90f));
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
