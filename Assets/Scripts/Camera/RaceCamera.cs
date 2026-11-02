using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The regatta's camera (docs/banana-island.md, "The regatta"): a race director that cuts between shots the way a
/// broadcast does. The start line from the side while the boats wait; then the leader from behind, two boats close
/// together from abeam, a wide look over the leading pack, now and then another racer; a fixed camera at each buoy as
/// the leader comes round it, a close look at a boat that was just hit, and at the end a camera behind the finish that
/// watches them come in. GameCamera.FollowRace turns it on; the setting (Race camera) or any camera key turns it off.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RaceCamera : MonoBehaviour
{
    private enum Shot { Start, Chase, Battle, Wide, Buoy, Incident, Kraken, Finish }

    private const float MinShot = 6f, MaxShot = 10f;
    private const float IncidentShot = 3.5f;
    private const float BuoyApproach = 100f;
    private const float FinishApproach = 130f;

    private Regatta race;
    private Shot shot;
    private float shotStarted, shotEnds;
    private OwnBoat subject, other;
    private int buoy = -1;
    private readonly HashSet<int> buoysShown = new HashSet<int>();
    private Vector3 fixedAt;
    private Vector3 beast; // where the Kraken rose, kept: the race's last incident may be another by the next frame
    private float orbit;
    private float lastIncident;
    private Vector3 position, look;
    private bool cut;

    public Regatta Race => race;

    public void Follow(Regatta regatta)
    {
        race = regatta;
        buoysShown.Clear();
        lastIncident = regatta ? regatta.IncidentAt : 0f;
        Cut(Shot.Start, null, float.MaxValue);
    }

    private void Cut(Shot next, OwnBoat boat, float seconds, OwnBoat second = null)
    {
        shot = next;
        subject = boat;
        other = second;
        shotStarted = Time.unscaledTime;
        shotEnds = seconds >= float.MaxValue ? float.MaxValue : shotStarted + seconds;
        orbit = Random.Range(0f, 360f);
        cut = true;
    }

    private void LateUpdate()
    {
        if (!race) return;
        Direct(Time.unscaledTime);
        if (!Frame(out var wantPosition, out var wantLook, out var focus)) return;

        var dt = Time.unscaledDeltaTime;
        if (cut)
        {
            position = wantPosition;
            look = wantLook;
            cut = false;
        }
        else
        {
            // the fixed shots only turn; the moving ones follow, a little behind so the boat leads the frame
            position = Vector3.Lerp(position, wantPosition, 1f - Mathf.Exp(-dt * 3f));
            look = Vector3.Lerp(look, wantLook, 1f - Mathf.Exp(-dt * 5f));
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
        if (shot == Shot.Start && race.SinceStart < 4f && leader) return; // a moment for the sails to drop

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

        // someone was hit: a close look, unless one was just shown
        if (race.IncidentAt > lastIncident)
        {
            lastIncident = race.IncidentAt;
            if (race.IncidentBoat && (shot != Shot.Incident || now - shotStarted > 2f))
            {
                Cut(Shot.Incident, race.IncidentBoat, IncidentShot);
                return;
            }
        }
        if (shot == Shot.Incident && now < shotEnds && subject) return;

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

        if (now < shotEnds && subject && (shot != Shot.Battle || other)) return;

        // a new shot: a close battle when there is one, mostly the leader, now and then the pack or someone else
        var roll = Random.value;
        if (roll < 0.45f && race.Battle(out var a, out var b)) { Cut(Shot.Battle, a, Random.Range(MinShot, MaxShot), b); return; }
        roll = Random.value;
        if (roll < 0.55f) Cut(Shot.Chase, leader, Random.Range(MinShot, MaxShot));
        else if (roll < 0.75f) Cut(Shot.Wide, leader, Random.Range(MinShot, MaxShot));
        else Cut(Shot.Chase, race.SomeRacer() ?? leader, Random.Range(MinShot, MaxShot));
    }

    // ---- Where the camera goes ---------------------------------------------------------------------

    private bool Frame(out Vector3 at, out Vector3 lookAt, out Vector3 focus)
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
            case Shot.Chase:
            case Shot.Incident:
            {
                if (!subject) return false;
                var close = shot == Shot.Incident;
                var forward = Flat(subject.transform.forward).normalized;
                var side = Vector3.Cross(Vector3.up, forward);
                var length = subject.Length;
                var p = subject.SeaPosition;
                at = p - forward * (length * (close ? 1.0f : 1.4f) + (close ? 7f : 11f)) + Vector3.up * (length * (close ? 0.35f : 0.45f) + (close ? 4f : 6f)) + side * (length * 0.3f);
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
                at = mid + side * (apart * 0.6f + 22f + length) - forward * 6f + Vector3.up * (6f + length * 0.25f);
                lookAt = mid + Vector3.up * 2f;
                focus = mid;
                return true;
            }
            case Shot.Wide:
            {
                if (!subject) return false;
                var pack = race.PackCentre();
                orbit += Time.unscaledDeltaTime * 5f;
                at = pack + Quaternion.Euler(0f, orbit, 0f) * Vector3.back * 85f + Vector3.up * 42f;
                lookAt = pack;
                focus = pack;
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
