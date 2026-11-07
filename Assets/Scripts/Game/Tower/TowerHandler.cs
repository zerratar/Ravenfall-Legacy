using UnityEngine;

/// <summary>
///     A player in the tower (docs/tower-design.md, section 8): enters a loaded floor, fights its enemies (or heals the
///     others in it when training Healing), and goes back to what they were doing when they leave or fall. Works like
///     the dungeon's handler and shares its way back (<see cref="WorldReturn"/>).
/// </summary>
public class TowerHandler : IAwayHandler
{
    private readonly PlayerController player;
    private readonly WorldReturn back = new WorldReturn();
    private TowerFloor floor;
    private EnemyController enemyTarget;
    private PlayerController healTarget;

    public bool InTower { get; private set; }

    public AwayPlace Place => AwayPlace.Tower;
    public bool IsAway => InTower;
    public WorldReturn Return => back;
    public Vector3 SpawnPosition => floor ? floor.Spawn : player.Position;
    public TowerFloor Floor => floor;

    public TowerHandler(PlayerController player)
    {
        this.player = player;
    }

    public bool Enter(TowerFloor target)
    {
        if (InTower || !target || !player || player.isDestroyed || player.Removed) return false;
        if (player.IsAway || player.raidHandler.InRaid || player.duelHandler.InDuel || player.arenaHandler.InArena
            || player.streamRaidHandler.InWar || player.InRavebridge || player.Stats.IsDead)
            return false;

        back.Leave(player);
        floor = target;
        InTower = true;

        player.InterruptAction();
        var offset = Random.insideUnitCircle * 3f;
        var spot = floor.Spawn + new Vector3(offset.x, 0f, offset.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
        player.teleportHandler.Teleport(spot);
        player.Stats.Health.Reset();
        player.Island = null;
        player.taskTarget = null;
        return true;
    }

    public void Exit()
    {
        if (!InTower || !player || !player.Movement) return;
        player.Movement.EnableLocalAvoidance();
        Clear();
        back.Back(player, "the tower");
    }

    public void Died() => Exit();

    private void Clear()
    {
        enemyTarget = null;
        healTarget = null;
        floor = null;
        InTower = false;
        player.attackTarget = null;
        player.taskTarget = null;
    }

    public void Update()
    {
        if (!InTower) return;
        if (!floor)
        {
            // the floor went away under the player (unloaded): back to the world
            Exit();
            return;
        }

        if (player.TrainingHealing)
        {
            UpdateHealing();
            return;
        }

        if (enemyTarget && (enemyTarget.Stats.IsDead || enemyTarget.IsUnreachable)) enemyTarget = null;
        if (!enemyTarget) enemyTarget = floor.NearestEnemy(player.Position);
        if (!enemyTarget) return;

        var distance = Vector3.Distance(player.Position, enemyTarget.transform.position);
        if (distance <= player.GetAttackRange())
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }
            player.Attack(enemyTarget);
            return;
        }

        if (!player.SetDestination(enemyTarget.transform.position))
        {
            player.Movement.DisableLocalAvoidance();
            enemyTarget.IsUnreachable = true;
            enemyTarget = null;
        }
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
