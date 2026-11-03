using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The regatta's camera (docs/banana-island.md, "The regatta"): a race director that cuts between shots the way an AI
/// against AI race in Mario Kart is shown (the user, 2026-11-03), or a race on TV. The start line from the side while
/// the boats wait; then mostly a chase camera high behind a boat, looking down the course ahead of it; a racer who
/// fires a cannonball or uses a pickup, a new leader taking the lead, a boat that was just hit, the Kraken; two boats
/// close together from abeam; a drone looking down on the leader and what lies ahead of it, as high as that takes; a
/// fixed camera at each buoy as the leader comes round it, and at the end a camera behind the finish that watches them
/// come in. GameCamera.FollowRace turns it on; the setting (Race camera) or any camera key turns it off.
/// When a boat's sails would fill the middle of a shot (one passing the lens, the camera close behind another), it
/// swings up and looks down more steeply, and back down once the view is clear.
/// The streamer can take it by hand without leaving the race (the user, 2026-11-03): moving or turning it as the free
/// camera does (right mouse, WASD or the arrows, Q and E, the wheel, or holding the free camera key) and the race's UI
/// stays; the race camera key, the button under the standings, or a while without touching it (a countdown shows)
/// gives it back to the director, which flies back to its shot.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RaceCamera : MonoBehaviour
{
    private enum Shot { Start, Pack, Chase, Battle, Drone, Buoy, Incident, Action, Kraken, Finish }

    private const float MinShot = 6f, MaxShot = 10f;
    private const float IncidentShot = 3.5f, ActionShot = 3.5f, LeadShot = 4.5f;
    /// <summary>An action or a new leader does not cut away from a shot younger than this (or from another such cut).</summary>
    private const float EventRest = 2.5f;
    /// <summary>
    /// A hit is shown close up at most this often, and only in the front of the race or on the boat already shown:
    /// the reefs, wreckage and kegs hit someone every few seconds, and the race was all close-ups (2026-11-03).
    /// </summary>
    private const float IncidentRest = 8f, IncidentPlaces = 5;
    private const float BuoyApproach = 100f;
    /// <summary>The swing from the start's front view round behind the pack: slower and over the top of it.</summary>
    private const float SwingSeconds = 2.5f, SwingRise = 18f;
    private const float FinishApproach = 130f;
    /// <summary>
    /// A boat in the way: another boat whose box on screen covers this share of it over the middle, or a shot's own
    /// boat this much (it is meant to be seen, but not as a wall of sail), for BlockedFor seconds. Up at LiftAngle
    /// degrees while it lasts, held LiftHold seconds after so the camera does not bob, swinging over LiftSeconds.
    /// </summary>
    private const float OtherCover = 0.25f, OwnCover = 0.55f, BlockedFor = 0.3f;
    private const float LiftAngle = 50f, LiftHold = 2f, LiftSeconds = 1.2f, LiftDistance = 40f;
    /// <summary>The drone looks this far ahead of its boat along the course, from this steep, between these heights.</summary>
    private const float DroneAhead = 110f, DroneAngle = 58f, DroneNearest = 45f, DroneFarthest = 150f;

    /// <summary>By hand: after this long untouched a countdown of ReturnCountdown seconds starts, then the director has it back.</summary>
    private const float IdleBeforeCountdown = 3f, ReturnCountdown = 10f;
    private const float HandSpeed = 50f, HandLook = 0.25f, HandWheel = 12f;

    private Regatta race;
    private Shot shot;
    private float shotStarted, shotEnds;
    private OwnBoat subject, other;
    private int buoy = -1;
    private readonly HashSet<int> buoysShown = new HashSet<int>();
    private Vector3 fixedAt;
    private Vector3 beast; // where the Kraken rose, kept: the race's last incident may be another by the next frame
    private float lastIncident, lastAction, lastIncidentCut = -100f;
    private OwnBoat lastLeader;
    private Vector3 position, look;
    private bool cut;
    private float lift, liftUntil, blocked;
    private float droneDistance;
    private float swingUntil;
    private float idle;
    private Vector3 lastMouse;
    private Camera lens;
    private readonly List<OwnBoat> boats = new List<OwnBoat>();
    private readonly List<Vector3> ahead = new List<Vector3>();
    // each boat's box in its own space, from its renderers; measured again now and then, as the sails set and furl
    private readonly Dictionary<OwnBoat, Bounds> shapes = new Dictionary<OwnBoat, Bounds>();
    private float shapesAt;

    public Regatta Race => race;

    /// <summary>Taken by hand: the streamer moves and turns it, the director waits.</summary>
    public bool Manual { get; private set; }

    /// <summary>Seconds until it goes back to the director by itself, or below 0 while the streamer is still at it.</summary>
    public float ReturnIn => Manual && idle > IdleBeforeCountdown ? Mathf.Max(0f, IdleBeforeCountdown + ReturnCountdown - idle) : -1f;

    /// <summary>The free camera key: by hand, from where it is now.</summary>
    public void TakeByHand()
    {
        Manual = true;
        idle = 0f;
    }

    /// <summary>Back to the director, flying from where the streamer left it to the shot it is on.</summary>
    public void ReturnToRace()
    {
        if (!Manual) return;
        Manual = false;
        position = transform.position;
        look = transform.position + transform.forward * 60f;
        cut = false;
        swingUntil = Time.unscaledTime + SwingSeconds;
    }

    public void Follow(Regatta regatta)
    {
        race = regatta;
        Manual = false;
        buoysShown.Clear();
        lastIncident = regatta ? regatta.IncidentAt : 0f;
        lastAction = regatta ? regatta.ActionAt : 0f;
        lastLeader = null;
        Cut(Shot.Start, null, float.MaxValue);
    }

    private void Cut(Shot next, OwnBoat boat, float seconds, OwnBoat second = null)
    {
        shot = next;
        subject = boat;
        other = second;
        shotStarted = Time.unscaledTime;
        shotEnds = seconds >= float.MaxValue ? float.MaxValue : shotStarted + seconds;
        cut = true;
        liftUntil = 0f;
        blocked = 0f;
        droneDistance = 0f;
    }

    private void OnDisable() => Manual = false;

    private void LateUpdate()
    {
        if (!race) return;
        // the director goes on choosing its shots while the streamer has it, so it comes back to the race as it is
        Direct(Time.unscaledTime);
        var dt = Time.unscaledDeltaTime;
        if (ByHand(dt)) return;
        if (!Frame(dt, out var wantPosition, out var wantLook, out var focus)) return;

        // judged on the shot as framed, not as lifted: from up there nothing is in the way, and it would drop again
        if (Liftable(shot) && Blocked(wantPosition, wantLook))
        {
            blocked += dt;
            if (blocked >= BlockedFor || cut) liftUntil = Time.unscaledTime + LiftHold;
        }
        else blocked = 0f;
        var liftWanted = Time.unscaledTime < liftUntil ? 1f : 0f;
        lift = cut ? liftWanted : Mathf.MoveTowards(lift, liftWanted, dt / LiftSeconds);
        if (lift > 0f) wantPosition = Vector3.Lerp(wantPosition, Lifted(wantPosition, wantLook), Mathf.SmoothStep(0f, 1f, lift));

        if (cut)
        {
            position = wantPosition;
            look = wantLook;
            cut = false;
        }
        else
        {
            // the fixed shots only turn; the moving ones follow, a little behind so the boat leads the frame; the swing
            // round the start goes slower, and up over the boats rather than through their sails
            var swing = Mathf.Clamp01((swingUntil - Time.unscaledTime) / SwingSeconds);
            if (swing > 0f) wantPosition.y += SwingRise * swing;
            position = Vector3.Lerp(position, wantPosition, 1f - Mathf.Exp(-dt * (swing > 0f ? 1.3f : 3f)));
            look = Vector3.Lerp(look, wantLook, 1f - Mathf.Exp(-dt * (swing > 0f ? 2f : 5f)));
        }
        position = AboveGround(position);
        var shaken = position + Shake(race.Rumble);
        var forward = look - shaken;
        if (forward.sqrMagnitude > 0.01f) transform.SetPositionAndRotation(shaken, Quaternion.LookRotation(forward));
        TiltShift.FocusPoint = focus;
    }

    // ---- What to show ------------------------------------------------------------------------------

    private void Direct(float now)
    {
        if (!race.Started)
        {
            if (shot != Shot.Start) Cut(Shot.Start, null, float.MaxValue);
            return;
        }

        var leader = race.Leader;
        // the boats sail at the start's camera, bows on; when they are halfway to it, it swings up over them and round
        // behind the pack (the user, 2026-11-03), and the race goes on from there
        if (shot == Shot.Start && leader)
        {
            lastLeader = leader;
            var gap = race.StartWidth * 0.8f + 50f;
            if (race.SinceStart < 12f && Flat(race.PackCentre() - position).magnitude > gap * 0.5f) return;
            Cut(Shot.Pack, leader, MaxShot);
            cut = false;
            swingUntil = now + SwingSeconds;
            return;
        }
        if (shot == Shot.Pack && now < shotEnds && subject) return;

        // the Kraken goes before everything: from abeam, the boat and the beast in one picture
        if (race.IncidentAt > lastIncident && race.IncidentBig && race.IncidentBoat)
        {
            lastIncident = race.IncidentAt;
            beast = race.IncidentPoint;
            Cut(Shot.Kraken, race.IncidentBoat, 5.2f);
            return;
        }
        if (shot == Shot.Kraken && now < shotEnds && subject) return;

        // the finish: once the leader is close, the camera by the way in stays while boats come in; when the next one
        // is still far out on the course, back to the race until it comes near
        if (shot == Shot.Finish)
        {
            if (!leader || race.DistanceToFinish(leader) < FinishApproach * 1.5f)
            {
                if (leader) subject = leader;
                return;
            }
            shotEnds = 0f;
            shot = Shot.Chase;
        }
        if (!leader)
        {
            if (race.Finished) { fixedAt = default; Cut(Shot.Finish, null, float.MaxValue); }
            return;
        }
        if (race.DistanceToFinish(leader) < FinishApproach)
        {
            fixedAt = FinishSpot(race.FinishPoint, leader.SeaPosition);
            Cut(Shot.Finish, leader, float.MaxValue);
            return;
        }

        // someone was hit: a close look, now and then, at the front of the race or the boat on screen
        var settled = now - shotStarted > EventRest;
        if (race.IncidentAt > lastIncident)
        {
            lastIncident = race.IncidentAt;
            var hit = race.IncidentBoat;
            if (hit && shot != Shot.Buoy && (hit == subject || (settled && now - lastIncidentCut > IncidentRest && race.PlaceOf(hit) <= IncidentPlaces)))
            {
                lastIncidentCut = now;
                Cut(Shot.Incident, hit, IncidentShot);
                return;
            }
        }
        if (shot == Shot.Incident && now < shotEnds && subject) return;

        // a racer uses what it picked up: a cannonball with its target in the picture, the rest from behind
        if (race.ActionAt > lastAction)
        {
            lastAction = race.ActionAt;
            var actor = race.ActionBoat;
            if (actor && settled && shot != Shot.Buoy && (actor == subject || race.PlaceOf(actor) <= 4))
            {
                var target = race.ActionTarget;
                if (target) Cut(Shot.Battle, actor, ActionShot, target);
                else Cut(Shot.Action, actor, ActionShot);
                return;
            }
        }

        // a new leader: the two of them if they are close, else the one in front
        if (leader != lastLeader)
        {
            var old = lastLeader;
            lastLeader = leader;
            if (old && settled && shot != Shot.Buoy)
            {
                var close = Flat(old.SeaPosition - leader.SeaPosition).magnitude < Mathf.Max(30f, leader.Length * 3f);
                if (close) Cut(Shot.Battle, leader, LeadShot, old);
                else Cut(Shot.Chase, leader, LeadShot);
                return;
            }
        }
        if ((shot == Shot.Action || shot == Shot.Battle) && now < shotEnds && subject && (shot != Shot.Battle || other)) return;

        // the leader comes up to a buoy: a camera at the buoy watches them round it, then holds a moment
        var mark = race.NextMark(leader);
        if (shot == Shot.Buoy)
        {
            if (mark > buoy && shotEnds == float.MaxValue) shotEnds = now + 2.5f;
            if (now < shotEnds) { subject = leader; return; }
        }
        else if (mark >= 0 && mark < race.Marks.Count - 1 && !buoysShown.Contains(mark) && now - shotStarted > 3f
            && Flat(race.Marks[mark] - leader.SeaPosition).magnitude < BuoyApproach)
        {
            buoy = mark;
            buoysShown.Add(mark);
            var markAt = race.Marks[mark];
            var outward = Flat(markAt - race.IslandCentre).normalized;
            var across = Vector3.Cross(Vector3.up, outward);
            fixedAt = markAt + outward * 40f + across * 12f + Vector3.up * 8f;
            Cut(Shot.Buoy, leader, float.MaxValue);
            return;
        }

        if (now < shotEnds && subject) return;

        // a new shot: mostly the leader from behind, a close battle when there is one, the drone, now and then another
        var roll = Random.value;
        if (roll < 0.25f && race.Battle(out var a, out var b)) { Cut(Shot.Battle, a, Random.Range(MinShot, MaxShot), b); return; }
        roll = Random.value;
        if (roll < 0.5f) Cut(Shot.Chase, leader, Random.Range(MinShot, MaxShot));
        else if (roll < 0.8f) Cut(Shot.Drone, Random.value < 0.75f ? leader : race.SomeRacer() ?? leader, Random.Range(MinShot, MaxShot));
        else Cut(Shot.Chase, race.SomeRacer() ?? leader, Random.Range(MinShot, MaxShot));
    }

    // ---- By hand -----------------------------------------------------------------------------------

    /// <summary>
    /// The streamer's own angle: any of the free camera's moves takes it, and it moves as the free camera does. True
    /// while it is by hand (the director's framing is skipped).
    /// </summary>
    private bool ByHand(float dt)
    {
        var mouse = Input.mousePosition;
        var turned = mouse - lastMouse;
        lastMouse = mouse;
        if (Keybinds.IsTyping) return Manual;

        var overUi = UIUtility.IsPointerOverAnyUI();
        var turning = Input.GetMouseButton(1) && (Manual || !overUi) && turned.sqrMagnitude > 0.25f;
        var sideways = Input.GetAxisRaw("Horizontal");
        var forward = Input.GetAxisRaw("Vertical");
        var rise = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        var wheel = overUi ? 0f : Input.mouseScrollDelta.y;
        var touched = turning || sideways != 0f || forward != 0f || rise != 0f || wheel != 0f;

        if (touched && !Manual) TakeByHand();
        if (!Manual) return false;
        idle = touched ? 0f : idle + dt;
        if (idle >= IdleBeforeCountdown + ReturnCountdown)
        {
            ReturnToRace();
            return false;
        }

        var t = transform;
        if (turning)
        {
            var look = HandLook * CameraSettings.LookSensitivity;
            var pitch = CameraSettings.InvertLook ? turned.y : -turned.y;
            var euler = t.eulerAngles;
            t.eulerAngles = new Vector3(euler.x + pitch * look, euler.y + turned.x * look, 0f);
        }
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var speed = CameraSettings.FreeCameraSpeed.GetValueOrDefault(HandSpeed) * (shift ? 5f : 1f);
        var move = (t.forward * forward + t.right * sideways + t.up * rise) * (speed * dt) + t.forward * (wheel * HandWheel);
        t.position = AboveGround(t.position + move);
        TiltShift.FocusPoint = t.position + t.forward * 60f;
        return true;
    }

    // ---- Where the camera goes ---------------------------------------------------------------------

    private bool Frame(float dt, out Vector3 at, out Vector3 lookAt, out Vector3 focus)
    {
        at = lookAt = focus = default;
        var t = Time.unscaledTime - shotStarted;
        switch (shot)
        {
            case Shot.Start:
            {
                // from ahead of the line and off to one side, every bow in the picture, drifting slowly across
                var forward = Quaternion.Euler(0f, race.StartHeading, 0f) * Vector3.forward;
                var side = Vector3.Cross(Vector3.up, forward);
                var centre = race.StartPoint;
                var reach = race.StartWidth * 0.5f;
                at = centre + forward * (reach * 1.6f + 50f) + side * (reach * 0.25f + 10f - t * 0.8f) + Vector3.up * (18f + reach * 0.3f);
                lookAt = centre + Vector3.up * 3f;
                focus = centre;
                return true;
            }
            case Shot.Pack:
            {
                // behind the leading pack and above it, looking on down the course the way they sail
                var pack = race.PackCentre();
                var forward = subject ? Flat(subject.transform.forward).normalized : Quaternion.Euler(0f, race.StartHeading, 0f) * Vector3.forward;
                var spread = race.StartWidth * 0.5f;
                // high and well back: the boats behind the leaders sail between it and them
                at = pack - forward * (spread + 70f) + Vector3.up * (38f + spread * 0.4f);
                lookAt = pack + forward * 45f;
                focus = pack;
                return true;
            }
            case Shot.Chase:
            case Shot.Action:
            {
                // high behind, the boat low in the picture and the course ahead of it above, round its next turns
                if (!subject) return false;
                var forward = Flat(subject.transform.forward).normalized;
                var length = subject.Length;
                var p = subject.SeaPosition;
                var near = shot == Shot.Action;
                at = p - forward * (length * (near ? 0.9f : 1.1f) + (near ? 12f : 16f)) + Vector3.up * (length * 0.65f + (near ? 6f : 8f));
                lookAt = subject.Ahead(length + (near ? 15f : 30f)) + Vector3.up * 1f;
                focus = p;
                return true;
            }
            case Shot.Incident:
            {
                if (!subject) return false;
                var forward = Flat(subject.transform.forward).normalized;
                var side = Vector3.Cross(Vector3.up, forward);
                var length = subject.Length;
                var p = subject.SeaPosition;
                at = p - forward * (length * 1.0f + 7f) + Vector3.up * (length * 0.35f + 4f) + side * (length * 0.3f);
                lookAt = p + forward * (length * 0.7f) + Vector3.up * 2f;
                focus = p;
                return true;
            }
            case Shot.Kraken:
            {
                if (!subject) return false;
                var p = subject.SeaPosition;
                var forward = Flat(beast - p).normalized;
                if (forward.sqrMagnitude < 0.5f) forward = Flat(subject.transform.forward).normalized;
                var side = Vector3.Cross(Vector3.up, forward);
                var mid = (p + beast) * 0.5f;
                // seaward of the course, where the other racers do not sail past the lens (they cut in towards the island)
                if (Vector3.Dot(side, Flat(mid - race.IslandCentre)) < 0f) side = -side;
                var apart = Flat(beast - p).magnitude;
                // from ahead on the quarter and above: the arms in front with the boat behind them, and the ones beside
                // it arching over its deck; far enough out for thirty metre arms and a galleon, above the smaller sails
                var across = (side * 0.75f + forward * 0.65f).normalized;
                at = mid + across * (apart * 0.5f + subject.Length * 0.8f + 50f) + Vector3.up * (24f + subject.Length * 0.2f);
                lookAt = mid + Vector3.up * 10f;
                focus = mid;
                return true;
            }
            case Shot.Battle:
            {
                if (!subject || !other) return false;
                var mid = (subject.SeaPosition + other.SeaPosition) * 0.5f;
                var forward = Flat(subject.transform.forward).normalized;
                var side = Vector3.Cross(Vector3.up, forward);
                var apart = Flat(subject.SeaPosition - other.SeaPosition).magnitude;
                var length = Mathf.Max(subject.Length, other.Length);
                at = mid + side * (apart * 0.6f + 22f + length) - forward * 6f + Vector3.up * (8f + length * 0.35f);
                lookAt = mid + Vector3.up * 2f;
                focus = mid;
                return true;
            }
            case Shot.Drone:
            {
                if (!subject) return false;
                Drone(dt, out at, out lookAt);
                focus = subject.SeaPosition;
                return true;
            }
            case Shot.Buoy:
            {
                var markAt = race.Marks[Mathf.Clamp(buoy, 0, race.Marks.Count - 1)];
                at = fixedAt;
                lookAt = subject ? Vector3.Lerp(markAt, subject.SeaPosition, 0.5f) + Vector3.up * 2f : markAt;
                focus = lookAt;
                return true;
            }
            case Shot.Finish:
            {
                var finish = race.FinishPoint;
                if (fixedAt == default) fixedAt = FinishSpot(finish, race.StartPoint);
                at = fixedAt;
                lookAt = subject ? Vector3.Lerp(subject.SeaPosition, finish, 0.3f) + Vector3.up * 2f : finish;
                focus = lookAt;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Looking down on a boat from behind and above, high enough to take in what matters now: the course ahead of it
    /// round its next turns, the bottles, kegs, peels, reefs and wreckage on the way, its next buoy and the racers
    /// around it. Further out when a lot lies ahead, closer when it is open water; the height eases, never jumps.
    /// </summary>
    private void Drone(float dt, out Vector3 at, out Vector3 lookAt)
    {
        var p = subject.SeaPosition;
        race.Upcoming(subject, DroneAhead, ahead);
        ahead.Add(p);
        ahead.Add(subject.Ahead(DroneAhead * 0.45f));
        ahead.Add(subject.Ahead(DroneAhead * 0.8f));
        race.Boats(boats);
        foreach (var boat in boats)
            if (boat && boat != subject && Flat(boat.SeaPosition - p).sqrMagnitude < 45f * 45f) ahead.Add(boat.SeaPosition);

        var min = ahead[0];
        var max = ahead[0];
        foreach (var point in ahead)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        var centre = Flat((min + max) * 0.5f);
        centre.y = SeaChart.Sea;
        var radius = 10f;
        foreach (var point in ahead) radius = Mathf.Max(radius, Flat(point - centre).magnitude + 8f);

        // far enough for that circle to fill the picture's height (the camera's own view, 60 degrees)
        if (!lens) lens = GetComponent<Camera>();
        var half = Mathf.Max(10f, lens.fieldOfView * 0.5f) * Mathf.Deg2Rad;
        var wanted = Mathf.Clamp(radius / Mathf.Tan(half), DroneNearest, DroneFarthest);
        droneDistance = droneDistance <= 0f ? wanted : Mathf.Lerp(droneDistance, wanted, 1f - Mathf.Exp(-dt * 0.8f));

        var forward = Flat(subject.transform.forward).normalized;
        var angle = DroneAngle * Mathf.Deg2Rad;
        at = centre - forward * (droneDistance * Mathf.Cos(angle)) + Vector3.up * (droneDistance * Mathf.Sin(angle));
        lookAt = centre;
    }

    /// <summary>
    /// Out on the water beside the way in to the dock, a little seaward of it: the boats come in across the picture.
    /// The side that is open water, or the higher spot when neither is.
    /// </summary>
    private static Vector3 FinishSpot(Vector3 finish, Vector3 from)
    {
        var inward = Flat(finish - from).normalized;
        if (inward.sqrMagnitude < 0.5f) inward = Vector3.forward;
        var side = Vector3.Cross(Vector3.up, inward);
        var right = finish - inward * 35f + side * 45f;
        var left = finish - inward * 35f - side * 45f;
        var at = SeaChart.IsWater(right) || !SeaChart.IsWater(left) ? right : left;
        at.y = SeaChart.Sea + 14f;
        return at;
    }

    // ---- Sails in the way ---------------------------------------------------------------------------

    /// <summary>
    /// The start, the Kraken and the drone are framed from high or far already; the finish watches every boat come in
    /// to the one dock point, so they are always in the middle of it.
    /// </summary>
    private static bool Liftable(Shot s) => s == Shot.Chase || s == Shot.Action || s == Shot.Incident || s == Shot.Battle || s == Shot.Buoy;

    /// <summary>The same spot from more steeply above, at least LiftDistance away, from the side the shot looked from.</summary>
    private static Vector3 Lifted(Vector3 at, Vector3 lookAt)
    {
        var offset = at - lookAt;
        var flat = Flat(offset);
        var away = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.back;
        var distance = Mathf.Max(offset.magnitude, LiftDistance);
        var angle = Mathf.Max(LiftAngle * Mathf.Deg2Rad, Mathf.Asin(Mathf.Clamp(offset.y / distance, -1f, 1f)));
        return lookAt + away * (distance * Mathf.Cos(angle)) + Vector3.up * (distance * Mathf.Sin(angle));
    }

    /// <summary>Would a boat fill the middle of the picture from here: one in front of what is looked at, or the shot's own?</summary>
    private bool Blocked(Vector3 at, Vector3 lookAt)
    {
        var forward = lookAt - at;
        if (forward.sqrMagnitude < 0.01f) return false;
        if (!lens) lens = GetComponent<Camera>();
        if (Time.unscaledTime - shapesAt > 3f)
        {
            shapes.Clear();
            shapesAt = Time.unscaledTime;
        }
        // the view as the camera would have it there (Unity's cameras look down -z)
        var view = Matrix4x4.TRS(at, Quaternion.LookRotation(forward), new Vector3(1f, 1f, -1f)).inverse;
        var toClip = lens.projectionMatrix * view;
        var depth = forward.magnitude;
        race.Boats(boats);
        // a shot's own boat only counts from behind it (the chase): a close-up or a fixed camera is meant to be full of it
        var ownCounts = shot == Shot.Chase || shot == Shot.Action;
        foreach (var boat in boats)
        {
            var own = boat == subject || boat == other;
            if (own && !ownCounts) continue;
            var cover = Cover(boat, at, toClip, own ? float.MaxValue : depth);
            if (cover >= (own ? OwnCover : OtherCover)) return true;
        }
        return false;
    }

    /// <summary>
    /// How much of the picture the boat's box covers, 0 to 1, counted only when it reaches over the middle and its
    /// nearest corner is closer than <paramref name="before"/>; 1 when the lens is inside it. Corners behind the lens
    /// are left out: a boat beside or behind the camera is not in the way.
    /// </summary>
    private float Cover(OwnBoat boat, Vector3 at, Matrix4x4 toClip, float before)
    {
        var shape = Shape(boat);
        var inside = shape;
        inside.Expand(2f);
        if (inside.Contains(boat.transform.InverseTransformPoint(at))) return 1f;
        var toWorld = boat.transform.localToWorldMatrix;
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        var nearest = float.MaxValue;
        var seen = 0;
        for (var i = 0; i < 8; ++i)
        {
            var w = toWorld.MultiplyPoint3x4(Corner(shape, i));
            var clip = toClip * new Vector4(w.x, w.y, w.z, 1f);
            if (clip.w <= 0.5f) continue;
            ++seen;
            nearest = Mathf.Min(nearest, clip.w);
            var ndc = new Vector2(clip.x / clip.w, clip.y / clip.w);
            min = Vector2.Min(min, ndc);
            max = Vector2.Max(max, ndc);
        }
        if (seen < 4 || nearest > before) return 0f;
        min = Vector2.Max(min, -Vector2.one);
        max = Vector2.Min(max, Vector2.one);
        if (max.x <= min.x || max.y <= min.y) return 0f;
        // over the middle: the box reaches into the centre third of the picture
        if (max.x < -0.35f || min.x > 0.35f || max.y < -0.35f || min.y > 0.35f) return 0f;
        return (max.x - min.x) * (max.y - min.y) * 0.25f;
    }

    /// <summary>
    /// The boat's box in its own space: its renderers, kept to its hull's length and beam (a wake or a flag's long
    /// bounds would make it a wall of water).
    /// </summary>
    private Bounds Shape(OwnBoat boat)
    {
        if (shapes.TryGetValue(boat, out var shape)) return shape;
        var toLocal = boat.transform.worldToLocalMatrix;
        var any = false;
        foreach (var r in boat.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            var local = r.localBounds;
            var m = toLocal * r.transform.localToWorldMatrix;
            for (var i = 0; i < 8; ++i)
            {
                var c = m.MultiplyPoint3x4(Corner(local, i));
                if (!any) { shape = new Bounds(c, Vector3.zero); any = true; }
                else shape.Encapsulate(c);
            }
        }
        var most = new Vector3(boat.Width * 0.75f, boat.Length * 1.1f, boat.Length * 0.6f);
        if (!any) shape = new Bounds(Vector3.up * boat.Length * 0.4f, new Vector3(boat.Width, boat.Length * 0.8f, boat.Length));
        else shape.SetMinMax(Vector3.Max(shape.min, new Vector3(-most.x, -2f, -most.z)), Vector3.Min(shape.max, most));
        shapes[boat] = shape;
        return shape;
    }

    private static Vector3 Corner(Bounds b, int i) =>
        b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));

    /// <summary>Never under an island's ground or the sea.</summary>
    private static Vector3 AboveGround(Vector3 at)
    {
        var lowest = SeaChart.Sea + 2f;
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (!terrain) continue;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (at.x < origin.x || at.z < origin.z || at.x > origin.x + size.x || at.z > origin.z + size.z) continue;
            lowest = Mathf.Max(lowest, terrain.SampleHeight(at) + origin.y + 3f);
        }
        if (at.y < lowest) at.y = lowest;
        return at;
    }

    /// <summary>The Kraken's roar shakes the picture.</summary>
    private static Vector3 Shake(float rumble)
    {
        if (rumble <= 0f || !SettingsApplier.ScreenShake) return Vector3.zero;
        var t = Time.unscaledTime * 18f;
        return new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, 5.1f) - 0.5f) * (2.4f * rumble);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
