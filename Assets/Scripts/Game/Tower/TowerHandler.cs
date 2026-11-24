using UnityEngine;

/// <summary>
///     A player in the tower (docs/tower-design.md, section 8): enters a loaded floor, fights its enemies (or heals the
///     others in it when training Healing), or strolls about a tower town (<see cref="TowerTown"/>), and goes back to
///     what they were doing when they leave or fall. Moving between the town and a floor keeps the way back. Works like
///     the dungeon's handler and shares its way back (<see cref="WorldReturn"/>).
/// </summary>
public class TowerHandler : IAwayHandler
{
    private readonly PlayerController player;
    private readonly WorldReturn back = new WorldReturn();
    private TowerFloor floor;
    private TowerTown town;
    private float nextStroll;
    private EnemyController enemyTarget;
    private PlayerController healTarget;
    private EnemyController boss;

    public bool InTower { get; private set; }

    public AwayPlace Place => AwayPlace.Tower;
    public bool IsAway => InTower;
    public WorldReturn Return => back;
    public Vector3 SpawnPosition => floor ? floor.Spawn : town ? town.Spawn : player.Position;
    public TowerFloor Floor => floor;
    public TowerTown Town => town;

    /// <summary>
    ///     The floor this player is level synced to (docs/tower-design.md section 12), or 0 off the floors (towns too).
    /// </summary>
    public int SyncFloor => InTower && floor && floor.Definition != null ? floor.Definition.Number : 0;

    /// <summary>
    ///     How much of the real health pool one point of synced health is: damage and healing taken on a floor are
    ///     multiplied by it, so a big health bar loses the same share as the synced pool would. 1 off the floors.
    /// </summary>
    public float SyncHealthScale
    {
        get
        {
            var synced = SyncFloor;
            if (synced <= 0 || player.Stats?.Health == null) return 1f;
            var real = player.Stats.Health.MaxValue;
            var level = RavenNest.Combat.Tower.SyncLevel(synced);
            var pool = Mathf.Min(real, CombatRules.PlayerMaxHealth(level) + RavenNest.Combat.Tower.SyncGearHealth(player.Stats.Health.GearHealth, player.Stats.Health.ArmorHealth, level));
            return pool > 0 ? Mathf.Max(1f, real / (float)pool) : 1f;
        }
    }

    /// <summary>In a tower town (not on a floor).</summary>
    public bool InTown => InTower && town;

    /// <summary>Resting at the town's resting place (!tower rest): rested as in the Onsen, no training meanwhile.</summary>
    public bool Resting { get; private set; }

    /// <summary>Rest as soon as the town is reached (!tower rest from outside it, while the town loads).</summary>
    public bool RestOnArrival;

    private Transform restSpot;
    private bool seated;
    private float seatBy;

    // training in town: the work place for the skill (TowerTown.Work), what it looks like there
    private string workKind;
    private Transform workSpot;
    private Vector3 workOffset;
    private bool working;
    private float workBy;
    private float nextWork;

    /// <summary>The stand at a town work place this player has, or null.</summary>
    public Transform WorkSpot => workSpot;

    /// <summary>
    ///     The island's experience is paid here (PlayerProgression.TrainAway): in a town while not resting, and on a floor
    ///     for a skill the floor's fights do not train (a woodcutter fighting along keeps their woodcutting). Combat skills
    ///     and Healing earn on a floor by fighting and healing.
    /// </summary>
    public bool TrainsAway
    {
        get
        {
            if (!InTower) return false;
            if (town) return !Resting;
            var skill = player.ActiveSkill;
            return floor && skill != RavenNest.Models.Skill.None && skill != RavenNest.Models.Skill.Healing && !skill.IsCombatSkill();
        }
    }

    public enum RestResult { Resting, AlreadyResting, NotInTown, Full }

    /// <summary>!tower rest in a town: off to a free spot at the resting place, to sit, meditate or sleep there.</summary>
    public RestResult StartResting()
    {
        if (!InTown) return RestResult.NotInTown;
        if (Resting) return RestResult.AlreadyResting;
        var spot = town.TakeRestSpot(player);
        if (!spot) return RestResult.Full;
        StopWorking();
        restSpot = spot;
        Resting = true;
        seated = false;
        seatBy = Time.time + 25f;
        player.InterruptAction();
        player.taskTarget = null;
        player.SetDestination(spot.position);
        return RestResult.Resting;
    }

    /// <summary>Up again: a new task, the floor, or leaving the tower.</summary>
    public void StopResting() => StopResting(true);

    private void StopResting(bool standUp)
    {
        if (!Resting) return;
        Resting = false;
        RestOnArrival = false;
        var spot = restSpot;
        restSpot = null;
        if (town && spot) town.ReleaseRestSpot(spot, player);
        if (!seated || !player) return;
        seated = false;
        player.Animations.ClearOnsenAnimations();
        if (standUp && spot)
        {
            var at = spot.position - spot.forward * 1.2f;
            if (UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) at = hit.position;
            player.teleportHandler.Teleport(at, true);
        }
        player.Movement.Unlock();
    }

    private void UpdateResting()
    {
        if (seated || !restSpot) return;
        var flat = restSpot.position - player.Position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 1.2f * 1.2f && Time.time < seatBy)
        {
            if (!player.Movement.IsMoving) player.SetDestination(restSpot.position);
            return;
        }

        // seated as in the Onsen: locked in place, the spot's pose
        player.Movement.Lock();
        player.transform.SetPositionAndRotation(restSpot.position, restSpot.rotation);
        player.InCombat = false;
        player.ClearTarget();
        var name = restSpot.name;
        if (name.StartsWith("Sleep")) player.Animations.Sleep();
        else if (name.StartsWith("Meditate")) player.Animations.Meditate();
        else player.Animations.Sit();
        seated = true;
    }

    public TowerHandler(PlayerController player)
    {
        this.player = player;
    }

    /// <summary>Not away, not in a raid, duel, arena, war or Ravebridge, and standing.</summary>
    public bool CanEnter => player && !player.isDestroyed && !player.Removed && !InTower && !player.IsAway
        && !player.raidHandler.InRaid && !player.duelHandler.InDuel && !player.arenaHandler.InArena
        && !player.streamRaidHandler.InWar && !player.InRavebridge && !player.Stats.IsDead;

    /// <summary>In a boss attempt: the boss is the only target.</summary>
    public bool InBossFight => boss;

    public bool Enter(TowerFloor target) => EnterPlace(target, null);

    public bool Enter(TowerTown target) => EnterPlace(null, target);

    /// <summary>
    ///     Into a floor or a town. From the other one (the town to a floor, or back) the way back to the world is kept;
    ///     not during a boss attempt.
    /// </summary>
    private bool EnterPlace(TowerFloor targetFloor, TowerTown targetTown)
    {
        TowerPlace place = targetFloor ? (TowerPlace)targetFloor : targetTown;
        if (!place) return false;
        var moving = InTower && !boss && player && !player.isDestroyed && !player.Stats.IsDead;
        if (!moving && !CanEnter) return false;
        if (moving && (place == floor || place == town)) return false;

        if (!moving) back.Leave(player);
        var restAfter = RestOnArrival && targetTown;
        RestOnArrival = false;
        StopResting(false);
        StopWorking();
        // off to another place while recovering from a boss fight: up, and no longer out
        EndWatching();
        enemyTarget = null;
        healTarget = null;
        player.attackTarget = null;
        floor = targetFloor;
        town = targetTown;
        InTower = true;
        nextStroll = Time.time + Random.Range(2f, 6f);

        player.InterruptAction();
        var offset = Random.insideUnitCircle * 3f;
        var spot = place.Spawn + new Vector3(offset.x, 0f, offset.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        player.teleportHandler.Teleport(spot);
        player.Stats.Health.Reset();
        player.Island = null;
        player.taskTarget = null;
        if (restAfter) StartResting();
        return true;
    }

    public void Exit()
    {
        if (!InTower || !player || !player.Movement) return;
        player.Movement.EnableLocalAvoidance();
        Clear();
        back.Back(player, "the tower");
    }

    /// <summary>
    ///     Knocked out in the tower: they get back up at the place's arrival (<see cref="RespawnSpot"/>), as on an
    ///     island, instead of being sent out of the tower (most viewers do not watch chat, and would quietly drop out of
    ///     the stream's climb). Out of a boss attempt for good: they sit the rest of it out on the floor's monsters.
    /// </summary>
    public void Died()
    {
        if (boss && floor && floor.Definition != null && player) TowerManager.Instance?.PlayerFell(player, floor.Definition.Number);
        saidOut = false;
        StandFromWatching();
        boss = null;
        enemyTarget = null;
        healTarget = null;
        if (player) player.attackTarget = null;
        StopResting(false);
        StopWorking();
    }

    /// <summary>Where a knocked out player gets back up: by the arrival of the floor or town they are in.</summary>
    public bool RespawnSpot(out Vector3 spot)
    {
        spot = default;
        TowerPlace place = floor ? (TowerPlace)floor : town;
        if (!InTower || !place) return false;
        var offset = Random.insideUnitCircle * 3f;
        spot = place.Spawn + new Vector3(offset.x, 0f, offset.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        return true;
    }

    public void BossFightStarted(EnemyController target)
    {
        boss = target;
        enemyTarget = null;
        player.attackTarget = null;
    }

    public void BossFightEnded()
    {
        boss = null;
        enemyTarget = null;
        // the attempt ended while they recovered: whole again, like everyone who goes on (not at the sliver of health
        // the recovery had reached, among the part's monsters)
        if (SatOut && player && !player.Stats.IsDead)
        {
            player.Stats.Health.Reset();
            if (player.HealthBar) player.HealthBar.UpdateHealth();
        }
        EndWatching();
    }

    /// <summary>
    ///     Knocked out in a boss attempt and recovering: up again by the arrival, they recover there
    ///     (<see cref="UpdateWatching"/>) for <see cref="RavenNest.Combat.TowerRecovery.Seconds"/> and
    ///     then walk back into the fight by themselves, at a share of their health. It has to read at a glance as
    ///     "knocked out and recovering", not as players who stand about and do not help (the owner, 2026-11-24).
    /// </summary>
    public bool SatOut { get; private set; }

    // this recovery's time (one fixed length, whatever the floor and however often they fell)
    private float recoverFrom, recoverUntil;

    /// <summary>Seconds until a recovering player goes back into the fight (0 when not recovering).</summary>
    public float RecoverSecondsLeft => SatOut ? Mathf.Max(0f, recoverUntil - Time.time) : 0f;

    private bool saidOut;
    private float nextCheer;
    private float nextWatchStep;
    // how this one waits, picked once when they get up: 0 sits, 1 sits cross-legged, 2 stands, 3 stands and cheers
    private int watchKind;
    private Vector3 watchSpot;
    private bool watchSpotSet;
    private bool watchSettled;
    private float nextWatchWord;

    /// <summary>TowerManager.PlayerFell: this player fell in the fight and sits the rest of it out.</summary>
    public void SitOut()
    {
        StandFromWatching();
        SatOut = true;
        saidOut = false;
        watchSpotSet = false;
        nextWatchStep = 0f;
        recoverFrom = Time.time;
        recoverUntil = Time.time + (float)RavenNest.Combat.TowerRecovery.Seconds;
    }

    /// <summary>TowerManager.Rejoin: recovered, up and back into the fight at a share of their health.</summary>
    public void Recovered(EnemyController guardian)
    {
        EndWatching();
        SetRecoveryHealth(1f);
        BossFightStarted(guardian);
    }

    // the health bar is the recovery's clock: it fills to the share they come back with as the time passes
    private void SetRecoveryHealth(float share)
    {
        var health = player.Stats.Health;
        var want = Mathf.Max(1, Mathf.RoundToInt(health.MaxValue * (float)RavenNest.Combat.TowerRecovery.ReturnHealth * Mathf.Clamp01(share)));
        if (want == health.CurrentValue) return;
        health.Add(want - health.CurrentValue);
        if (player.HealthBar) player.HealthBar.UpdateHealth();
    }

    // up from the ground (or just free to walk again): the attempt ended, or they have to move
    private void StandFromWatching()
    {
        if (!watchSettled) return;
        watchSettled = false;
        if (!player || player.isDestroyed) return;
        player.Animations.ClearOnsenAnimations();
        player.Movement.Unlock();
    }

    // no longer out: the attempt ended, or they left the part (the town, another part, the world)
    private void EndWatching()
    {
        StandFromWatching();
        SatOut = false;
    }

    /// <summary>
    ///     Out of the fight and back up: off to a place of one's own beside the arrival (to both sides of the way
    ///     in, so the doorway and the walk to the guardian stay clear), turned to the fight, and down on the ground
    ///     there. Most sit, some sit cross-legged, some stand, a few cheer the others on now and then; each the same
    ///     way for the whole attempt. Never within the guardian's slam: when it comes near, up and further back.
    /// </summary>
    private void UpdateWatching()
    {
        var tower = TowerManager.Instance;
        var guardian = tower ? tower.FightBoss : null;
        if (!saidOut)
        {
            saidOut = true;
            CombatPaths.Word(player, Localization.UI_TOWER_RECOVERING, new Color(0.8f, 0.85f, 0.95f));
            player.InterruptAction();
            player.attackTarget = null;
            var pick = (player.Id.GetHashCode() & 0x7fffffff) % 10;
            watchKind = pick < 5 ? 0 : pick < 7 ? 1 : pick < 9 ? 2 : 3;
            nextCheer = Time.time + Random.Range(3f, 7f);
        }
        if (!guardian || guardian.Stats.IsDead) return;
        // recovered: back into the fight
        if (Time.time >= recoverUntil)
        {
            tower.Rejoin(player);
            return;
        }
        SetRecoveryHealth(Mathf.InverseLerp(recoverFrom, recoverUntil, Time.time));
        var bossAt = guardian.transform.position;
        var safe = TowerManager.WatchDistance(guardian);
        var fromBoss = watchSpot - bossAt;
        fromBoss.y = 0f;
        if (!watchSpotSet || fromBoss.magnitude < safe)
        {
            if (watchSpotSet && Time.time < nextWatchStep) return;
            nextWatchStep = Time.time + 1.5f;
            watchSpot = WatchSpot(bossAt, safe);
            watchSpotSet = true;
            StandFromWatching();
        }

        var to = watchSpot - player.Position;
        to.y = 0f;
        if (to.sqrMagnitude > 1.2f * 1.2f)
        {
            StandFromWatching();
            if (!player.Movement.IsMoving || Time.time >= nextStroll)
            {
                nextStroll = Time.time + 2f;
                if (!player.SetDestination(watchSpot)) player.Movement.DisableLocalAvoidance();
            }
            return;
        }

        var look = bossAt - player.Position;
        look.y = 0f;
        if (!watchSettled)
        {
            // there: turned to the fight, and down
            watchSettled = true;
            player.Movement.Lock();
            player.InCombat = false;
            if (look.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(look.normalized);
            if (watchKind == 0) player.Animations.Sit();
            else if (watchKind == 1) player.Animations.Meditate();
            nextWatchWord = Time.time + Random.Range(6f, 10f);
            return;
        }
        // said again now and then: someone who looks in mid fight sees why these players do not help
        if (Time.time >= nextWatchWord)
        {
            nextWatchWord = Time.time + Random.Range(8f, 12f);
            CombatPaths.Word(player, string.Format(Localization.UI_TOWER_RECOVERING_LEFT, Mathf.CeilToInt(RecoverSecondsLeft)), new Color(0.8f, 0.85f, 0.95f));
        }
        if (watchKind < 2) return;
        // standing: an eye on the guardian, and a few cheer the others on
        if (look.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(look.normalized), Mathf.Clamp01(Time.deltaTime * 4f));
        if (watchKind == 3 && Time.time >= nextCheer)
        {
            nextCheer = Time.time + Random.Range(5f, 9f);
            player.Cheer();
        }
    }

    // This player's own place to recover: beside the arrival, 2.5 to 7.5 m to one side of the line from the arrival
    // to the guardian and up to 4 m along it, by their id (the same place all attempt); clear of the guardian's slam.
    private Vector3 WatchSpot(Vector3 bossAt, float safe)
    {
        var home = floor.Spawn;
        var ahead = bossAt - home;
        ahead.y = 0f;
        ahead = ahead.sqrMagnitude > 1f ? ahead.normalized : floor.transform.forward;
        var side = Vector3.Cross(Vector3.up, ahead);
        var h = player.Id.GetHashCode() & 0x7fffffff;
        var lateral = (2.5f + ((h >> 4) & 15) / 15f * 5f) * (((h >> 3) & 1) == 0 ? -1f : 1f);
        var along = -0.5f + ((h >> 9) & 15) / 15f * 4.5f;
        var spot = home + side * lateral + ahead * along;
        var away = spot - bossAt;
        away.y = 0f;
        if (away.magnitude < safe + 2f) spot = bossAt + (away.sqrMagnitude > 0.01f ? away.normalized : -ahead) * (safe + 2f);
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        return spot;
    }

    /// <summary>The floor was filled for the next floor (the boss fell): new monsters to fight.</summary>
    public void FloorChanged(TowerFloor target)
    {
        EndWatching();
        floor = target;
        enemyTarget = null;
        player.attackTarget = null;
        player.Stats.Health.Reset();
        // a floor of rooms starts again at its first room, behind shut gates: back to its arrival
        if (target && target.Route && target.Route.Valid && player && !player.Stats.IsDead)
        {
            player.InterruptAction();
            var offset = Random.insideUnitCircle * 3f;
            var spot = target.Spawn + new Vector3(offset.x, 0f, offset.y);
            if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
            player.teleportHandler.Teleport(spot);
        }
    }

    private void Clear()
    {
        StopResting(false);
        StopWorking();
        enemyTarget = null;
        healTarget = null;
        boss = null;
        EndWatching();
        floor = null;
        town = null;
        InTower = false;
        player.attackTarget = null;
        player.taskTarget = null;
    }

    public void Update()
    {
        if (!InTower) return;
        // knocked out: the body lies where it fell until it is back up (PlayerController.Respawn). Without this a
        // body was still given orders: on a part of rooms the walk to its place in the guardian's room, elsewhere
        // the way to the next monster, and it slid there lying down (the owner, 2026-11-24)
        if (player.Stats.IsDead) return;
        if (town)
        {
            UpdateTown();
            return;
        }
        if (!floor)
        {
            // the floor (or town) went away under the player (unloaded): back to the world
            Exit();
            return;
        }

        // out of the boss attempt: watching it from the arrival
        if (SatOut)
        {
            UpdateWatching();
            return;
        }

        // a floor of rooms (TowerRoute): when the boss is called, everyone walks to the boss's room and waits for it there
        var route = floor.Route;
        if (route && route.Valid && route.Rallying && !boss)
        {
            UpdateRally(route);
            return;
        }

        if (player.TrainingHealing)
        {
            UpdateHealing();
            return;
        }

        // in an attempt the boss's helpers come first, then the boss
        if (boss && !boss.Stats.IsDead)
        {
            var add = floor.NearestAdd(player.Position);
            if (!enemyTarget || enemyTarget.Stats.IsDead || (enemyTarget != boss && !IsAdd(enemyTarget)) || (enemyTarget == boss && add))
                enemyTarget = add ? add : boss;
        }
        if (enemyTarget && (enemyTarget.Stats.IsDead || (enemyTarget.IsUnreachable && enemyTarget != boss))) enemyTarget = null;
        if (!enemyTarget) enemyTarget = floor.NearestEnemy(player.Position);
        if (!enemyTarget) return;

        // a big boss is reached at its edge: its hit range grows with its size
        var range = player.GetAttackRange();
        var scale = enemyTarget.transform.lossyScale.x;
        range = Mathf.Max(range, enemyTarget.GetHitRange() * scale, enemyTarget == boss ? range + scale : range);
        // around a boss everyone takes their own place on a ring at its edge, instead of all on one spot, and fights
        // from there (or from wherever they stand once that place is reached)
        var destination = enemyTarget.transform.position;
        var distance = Vector3.Distance(player.Position, enemyTarget.transform.position);
        var inReach = distance <= range;
        if (enemyTarget == boss)
        {
            var angle = (player.Id.GetHashCode() & 1023) / 1024f * Mathf.PI * 2f;
            var place = destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (range * 0.8f);
            if (UnityEngine.AI.NavMesh.SamplePosition(place, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                destination = hit.position;
                inReach = inReach && (Vector3.Distance(player.Position, destination) < 1.5f || distance < range * 0.5f);
            }
        }

        if (inReach)
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }
            player.Attack(enemyTarget);
            return;
        }

        if (!player.SetDestination(destination))
        {
            player.Movement.DisableLocalAvoidance();
            if (enemyTarget == boss) return;
            enemyTarget.IsUnreachable = true;
            enemyTarget = null;
        }
    }

    /// <summary>The boss is called on a floor of rooms: off to a place of one's own in the boss's room, and wait there.</summary>
    private void UpdateRally(TowerRoute route)
    {
        if (enemyTarget || player.attackTarget)
        {
            enemyTarget = null;
            healTarget = null;
            player.InterruptAction();
            player.attackTarget = null;
        }
        var spot = route.RallyPoint(player);
        var flat = spot - player.Position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1.5f * 1.5f)
        {
            if (player.Movement.IsMoving) player.Movement.Lock();
            return;
        }
        if (!player.Movement.IsMoving || Time.time >= nextStroll)
        {
            nextStroll = Time.time + 2f;
            if (!player.SetDestination(spot)) player.Movement.DisableLocalAvoidance();
        }
    }

    private bool IsAdd(EnemyController enemy)
    {
        foreach (var a in floor.Adds) if (a == enemy) return true;
        return false;
    }

    /// <summary>
    ///     In a town: off to the work place of the skill being trained and at it there (the experience is the island's,
    ///     see <see cref="TrainsAway"/>), or, with no place for it, wander to a spot now and then and linger there.
    /// </summary>
    private void UpdateTown()
    {
        if (Resting) { UpdateResting(); return; }
        var kind = TowerTown.WorkKind(player.ActiveSkill);
        if (!town.HasWork(kind)) kind = null;
        if (kind != workKind)
        {
            StopWorking();
            workKind = kind;
            if (kind != null)
            {
                workSpot = town.TakeWorkSpot(player, kind, out var shared);
                // a stand others have: a little to one side of them
                workOffset = shared && workSpot && kind != "Sailing" ? workSpot.right * Random.Range(-1.1f, 1.1f) - workSpot.forward * Random.Range(0f, 0.8f) : Vector3.zero;
                workBy = Time.time + 30f;
                player.InterruptAction();
                player.taskTarget = null;
                if (workSpot) player.SetDestination(workSpot.position + workOffset);
            }
        }
        if (workSpot)
        {
            UpdateWorking();
            return;
        }

        if (Time.time < nextStroll) return;
        nextStroll = Time.time + Random.Range(12f, 30f);
        var spot = town.RandomStroll();
        var offset = Random.insideUnitCircle * 1.5f;
        spot += new Vector3(offset.x, 0f, offset.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        player.SetDestination(spot);
    }

    private void UpdateWorking()
    {
        var stand = workSpot.position + workOffset;
        if (!working)
        {
            var flat = stand - player.Position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.6f * 0.6f && Time.time < workBy)
            {
                if (!player.Movement.IsMoving) player.SetDestination(stand);
                return;
            }
            // stuck on the way (a crowd, a corner): there at once
            if (flat.sqrMagnitude > 3f * 3f) player.teleportHandler.Teleport(stand, true);
            player.Movement.Lock();
            player.InCombat = false;
            player.ClearTarget();
            player.transform.rotation = Quaternion.Euler(0f, workSpot.eulerAngles.y, 0f);
            // a seat (a bench by the boat): on it, as resting does
            if (Seated) player.transform.SetPositionAndRotation(workSpot.position, Quaternion.Euler(0f, workSpot.eulerAngles.y, 0f));
            working = true;
            StartWork();
            nextWork = Time.time + Random.Range(0.6f, 1.6f);
            return;
        }
        if (Time.time < nextWork) return;
        player.transform.rotation = Quaternion.Euler(0f, workSpot.eulerAngles.y, 0f);
        nextWork = Time.time + DoWork();
    }

    private Vector3 WorkTarget => workSpot.position + workSpot.forward * 1.5f + Vector3.up;

    // the work done sitting (its stands are seats, placed at the sitting height)
    private bool Seated => workKind == "Sailing";

    private void StartWork()
    {
        var animations = player.Animations;
        var equipment = player.Equipment;
        switch (workKind)
        {
            case "Woodcutting": equipment.ShowHatchet(); animations.StartWoodcutting(); break;
            case "Mining": equipment.ShowPickAxe(); animations.StartMining(); break;
            case "Fishing": equipment.ShowFishingRod(); animations.StartFishing(); break;
            case "Farming": equipment.ShowRake(); animations.StartFarming(); break;
            case "Cooking": equipment.HideEquipments(); animations.StartCooking(); break;
            case "Crafting": equipment.ShowHammer(); animations.StartCrafting(); break;
            case "Alchemy": equipment.HideEquipments(); animations.StartBrewing(); break;
            case "Gathering": equipment.HideEquipments(); animations.StartGathering(true); break;
            case "Taming": equipment.HideEquipments(); animations.StartGathering(false); break;
            case "Sailing": equipment.HideEquipments(); animations.Sit(); break;
            case "Melee":
            case "Ranged":
            case "Magic":
            case "Healing":
                var type = WorkAttackType;
                equipment.ShowWeapon(type);
                animations.StartCombat(WorkWeaponAnimation(type), equipment.HasShield);
                break;
        }
    }

    /// <summary>One stroke of the work (a chop, a cast); the seconds until the next.</summary>
    private float DoWork()
    {
        var animations = player.Animations;
        var at = WorkTarget;
        switch (workKind)
        {
            case "Woodcutting":
                animations.Chop(0);
                GameSounds.Play(SoundKind.WoodChop, at, 0.5f, 1f, player);
                return 2f;
            case "Mining":
                animations.Mine();
                GameSounds.Play(SoundKind.RockImpact, at, 0.5f, 0.35f, player);
                return 2f;
            case "Fishing":
                animations.Fish();
                if (Random.value < 0.4f) GameSounds.Play(SoundKind.WaterSplash, at, 0.4f, 0.7f, player);
                return 3f;
            case "Farming":
                GameSounds.Play(SoundKind.Soil, at, 0.4f, 0.3f, player);
                return 3f;
            case "Cooking":
                GameSounds.Play(SoundKind.Sizzle, at, 0.35f, 0.2f, player);
                return 3f;
            case "Crafting":
                animations.Craft();
                GameSounds.Play(Random.value < 0.6f ? SoundKind.Hammer : SoundKind.Saw, at, 0.45f, 0.3f, player);
                return 3f;
            case "Alchemy":
                animations.Brew();
                GameSounds.Play(SoundKind.Bubbles, at, 0.35f, 0.2f, player);
                return 3f;
            case "Gathering":
                animations.Gather(true);
                GameSounds.Play(SoundKind.Foliage, at, 0.35f, 0.4f, player);
                return 3f;
            case "Taming":
                return 6f;
            case "Sailing":
                return 10f;
            default:
                var type = WorkAttackType;
                var weapon = WorkWeaponAnimation(type);
                var count = type == AttackType.Melee ? (player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Weapon)?.GetAnimationCount() ?? 4) : 0;
                animations.Attack(weapon, Random.Range(0, Mathf.Max(1, count)), player.Equipment.HasShield);
                return type == AttackType.Healing ? 2.2f : Random.Range(1.5f, 2f);
        }
    }

    private AttackType WorkAttackType => workKind == "Ranged" ? AttackType.Ranged : workKind == "Magic" ? AttackType.Magic : workKind == "Healing" ? AttackType.Healing : AttackType.Melee;

    // the animator's weapon numbers, as PlayerCombat's
    private int WorkWeaponAnimation(AttackType type)
    {
        switch (type)
        {
            case AttackType.Healing: return 6;
            case AttackType.Magic: return 4;
            case AttackType.Ranged: return 3;
            default: return player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Weapon)?.GetAnimation() ?? 0;
        }
    }

    /// <summary>Done at the work place (another skill, resting, leaving the town): up and free again.</summary>
    private void StopWorking()
    {
        var spot = workSpot;
        var kind = workKind;
        workSpot = null;
        workKind = null;
        if (town && spot) town.ReleaseWorkSpot(spot, player);
        if (!working || !player) return;
        working = false;
        player.Animations.ResetAnimationStates();
        if (kind == "Sailing") player.Animations.ClearOnsenAnimations();
        player.Equipment.HideEquipments();
        player.Movement.Unlock();
    }

    private void UpdateHealing()
    {
        if (!healTarget || healTarget.Stats.IsDead || !healTarget.towerHandler.InTower || healTarget.Stats.HealthPercent > 0.9f)
        {
            healTarget = null;
            var lowest = 0.9f;
            foreach (var p in player.GameManager.Players.GetAllPlayers())
            {
                if (!p || p.Stats.IsDead || !p.towerHandler.InTower || p.towerHandler.Floor != floor) continue;
                if (p.Stats.HealthPercent < lowest)
                {
                    lowest = p.Stats.HealthPercent;
                    healTarget = p;
                }
            }
            if (!healTarget) healTarget = player;
        }

        if (Vector3.Distance(player.Position, healTarget.Position) <= player.GetAttackRange())
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }
            player.Heal(healTarget);
            return;
        }

        if (!player.SetDestination(healTarget.Position)) player.Movement.DisableLocalAvoidance();
    }
}
