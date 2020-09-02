using System;
using System.Linq;
using UnityEngine;

public class FightingTask : ChunkTask
{
    private readonly Func<EnemyController[]> lazyEnemies;

    public FightingTask(Func<EnemyController[]> lazyEnemies)
    {
        this.lazyEnemies = lazyEnemies;
    }

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var enemy = target.GetComponent<EnemyController>();
        if (!enemy)
        {
            return true;
        }

        return enemy.Stats.IsDead;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var enemies = lazyEnemies();
        var attackers = player.GetAttackers();
        try
        {
            foreach (var attacker in attackers.Where(x => !x.GetStats().IsDead))
            {
                if (!attacker.Transform)
                {
                    continue;
                }

                var enemyController = attacker.Transform.GetComponent<EnemyController>();
                if (!enemyController)
                {
                    continue;
                }

                var targetEnemy = enemies.FirstOrDefault(x => x.GetInstanceID() == enemyController.GetInstanceID());
                if (targetEnemy) return targetEnemy.transform;
            }

            var enemy = enemies
                        .Where(x => !x.Stats.IsDead)
                        //.OrderByDescending(x => x.Attackers.Count)
                        //.ThenBy(x => Math.Abs(player.Stats.CombatLevel - x.Stats.CombatLevel))
                        .OrderBy(x => Math.Abs(player.Stats.CombatLevel - x.Stats.CombatLevel))
                        .ThenBy(x => x.Attackers.Count)
                        .ThenBy(x => Vector3.Distance(x.transform.position, player.transform.position))
                        .ThenBy(x => UnityEngine.Random.value)
                        .FirstOrDefault();

            return enemy?.transform;
        }
        catch
        {
            return null;
        }
    }

    public override bool Execute(PlayerController player, Transform target)
    {
        var enemy = target.GetComponent<EnemyController>();
        if (!enemy)
        {
            return false;
        }

        return player.Attack(enemy);
    }

    public override void TargetAcquired(PlayerController player, Transform target)
    {
        var enemy = target.GetComponent<EnemyController>();
        if (!enemy)
        {
            return;
        }

        enemy.AddAttacker(player);
    }

    public override bool CanExecute(
        PlayerController player, Transform target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        if (!player)
        {
            return false;
        }

        if (!player.IsReadyForAction)
        {
            return false;
        }

        var enemy = target.GetComponent<EnemyController>();
        if (!enemy)
        {
            return false;
        }

        if (enemy.Stats.IsDead)
        {
            return false;
        }

        var possibleTargets = lazyEnemies();
        if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        var collider = target.GetComponent<SphereCollider>();
        if (!collider)
        {
            Debug.LogError("Target enemy does not have a sphere collider to mark max distance.");
            return false;
        }

        //var range = collider.radius;        
        //if (player.TrainingRanged) range = player.RangedAttackRange;
        //if (player.TrainingMagic) range = player.MagicAttackRange;

        var range = player.GetAttackRange();
        if (collider.radius > range)
            range = collider.radius;

        var distance = Vector3.Distance(player.transform.position, target.position);
        if (distance > range)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }
}