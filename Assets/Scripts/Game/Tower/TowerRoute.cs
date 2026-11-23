using UnityEngine;

/// <summary>
///     A floor laid out as a route of rooms instead of one arena (docs/tower-design.md section 22): the group fights in
///     one room at a time; when its monsters have all fallen, its gate opens and the players walk on to the next room's
///     monsters. The last room with monsters (the hold) refills as a one-room floor does, so the floor stays open for
///     training and its quests; the room after it is the boss's, opened when a boss attempt is called (everyone gathers
///     there during the countdown). A floor without a route works as before.
///     Only a presentation of the floor in the game: the server still decides the quests, the boss and the clear.
/// </summary>
public sealed class TowerRoute : MonoBehaviour
{
    /// <summary>The rooms in the order the group goes through them; the last is the boss's.</summary>
    public TowerZone[] Zones = new TowerZone[0];

    /// <summary>
    ///     Seconds a room may go without a kill (with someone fighting in it) before its gate opens anyway: a monster that
    ///     cannot be reached must never shut the floor.
    /// </summary>
    public float StallSeconds = 90f;

    /// <summary>The room the group fights in now.</summary>
    public int Active { get; private set; }

    /// <summary>A boss attempt is called or under way: the way to the boss is open and everyone gathers there.</summary>
    public bool Rallying { get; private set; }

    /// <summary>The last room with monsters: it refills, and the boss's room comes after it.</summary>
    public int Hold => Mathf.Max(0, Zones.Length - 2);

    /// <summary>The rooms with monsters (the boss's not counted), for "Room 2 of 4".</summary>
    public int FightingRooms => Zones.Length <= 1 ? Zones.Length : Zones.Length - 1;

    public TowerZone BossZone => Zones.Length > 0 ? Zones[Zones.Length - 1] : null;

    public bool Valid => Zones != null && Zones.Length > 0;

    /// <summary>Where a player arrives or gets back up: the room the group is in, the boss's room while gathering.</summary>
    public Vector3 Arrival => !Valid ? transform.position : Rallying && BossZone ? BossZone.Arrival : Zones[Mathf.Clamp(Active, 0, Zones.Length - 1)].Arrival;

    private TowerFloor floor;
    private float lastProgress;
    private int lastAlive = -1;

    private void Awake()
    {
        floor = GetComponent<TowerFloor>();
    }

    /// <summary>Back to the first room, every gate shut (the floor was filled for a floor).</summary>
    public void ResetRoute()
    {
        Active = 0;
        Rallying = false;
        lastAlive = -1;
        lastProgress = Time.time;
        foreach (var z in Zones) if (z && z.Exit) z.Exit.Close(true);
    }

    /// <summary>Monsters of this room may be fought now (it is the group's room, or one already cleared).</summary>
    public bool Open(int zone) => zone <= Active;

    /// <summary>A boss attempt was called: every gate up to the boss's room opens and everyone gathers there.</summary>
    public void OpenToBoss()
    {
        if (!Valid) return;
        Rallying = true;
        foreach (var z in Zones) if (z && z.Exit) z.Exit.Open();
    }

    /// <summary>The attempt is over: back to the floor's monsters (the gates stay open).</summary>
    public void EndRally()
    {
        Rallying = false;
        lastProgress = Time.time;
    }

    /// <summary>
    ///     Where a player waits for the boss: a place of their own on an arc in front of the boss room's arrival, so a
    ///     crowd does not stand on one spot.
    /// </summary>
    public Vector3 RallyPoint(PlayerController player)
    {
        var zone = BossZone;
        if (!zone) return transform.position;
        var entry = zone.Entry ? zone.Entry : zone.transform;
        var h = player ? (player.Id.GetHashCode() & 1023) / 1023f : 0.5f;
        var ring = 2f + (player ? ((player.Id.GetHashCode() >> 10) & 3) : 0) * 1.6f;
        var angle = Mathf.Lerp(-70f, 70f, h);
        var spot = entry.position + Quaternion.Euler(0f, angle, 0f) * entry.forward * ring;
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        return spot;
    }

    private void Update()
    {
        if (!Valid || !floor || floor.Definition == null || Active >= Hold) return;
        var alive = floor.AliveIn(Active);
        if (alive != lastAlive)
        {
            lastAlive = alive;
            lastProgress = Time.time;
        }
        // nobody fighting here: the stall clock waits
        if (!floor.AnyoneOn()) lastProgress = Time.time;
        if (alive > 0 && Time.time - lastProgress < StallSeconds) return;
        Advance();
    }

    private void Advance()
    {
        var zone = Zones[Active];
        if (zone && zone.Exit) zone.Exit.Open();
        Active++;
        lastAlive = -1;
        lastProgress = Time.time;
        floor.ZoneOpened(Active);
    }
}
