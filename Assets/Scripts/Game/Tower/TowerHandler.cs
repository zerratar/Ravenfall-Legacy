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

    public void Died()
    {
        if (boss && floor && floor.Definition != null && player) TowerManager.Instance?.PlayerFell(player, floor.Definition.Number);
        Exit();
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
    }

    /// <summary>The floor was filled for the next floor (the boss fell): new monsters to fight.</summary>
    public void FloorChanged(TowerFloor target)
    {
        floor = target;
        enemyTarget = null;
        player.attackTarget = null;
        player.Stats.Health.Reset();
    }

    private void Clear()
    {
        StopResting(false);
        StopWorking();
        enemyTarget = null;
        healTarget = null;
        boss = null;
        floor = null;
        town = null;
        InTower = false;
        player.attackTarget = null;
        player.taskTarget = null;
    }

    public void Update()
    {
        if (!InTower) return;
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
