using System;
using System.Collections.Generic;
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
        if (player.TrainingHealing)
        {
            var plr = target.GetComponent<PlayerController>();
            if (!plr)
            {
                return true;
            }

            var hp = plr.GetStats().Health;
            if (hp.CurrentValue >= hp.Level)
            {
                var newTarget = GetTarget(player);
                return newTarget != target;
            }

            return false;
        }
        else
        {
            var enemy = target.GetComponent<EnemyController>();
            if (!enemy)
            {
                return true;
            }
            return enemy.Stats.IsDead;
        }
    }

    public override Transform GetTarget(PlayerController player)
    {
        if (player.TrainingHealing)
        {
            if (player.Duel.InDuel || player.Arena.InArena)
            {
                return player.Transform;
            }

            IReadOnlyList<PlayerController> players = null;

            if (!player.Island)
            {
                players = player.Game.Players
                    .GetAllPlayers()
                    .Where(x => (!x.Island || x.Island == null) && !x.Duel.InDuel && !x.Arena.InArena)
                    .ToList();
            }
            else
            {
                players = player.Island
                    .GetPlayers()
                    .Where(x => !x.Stats.IsDead && !x.Duel.InDuel && !x.Arena.InArena)
                    .ToList();
            }

            if (players.Count == 0)
                return player.transform;

            var targetPlayer = players
                .Where(x => !x.Stats.IsDead && x.InCombat)
                .OrderByDescending(x => x.Stats.Health.Level - x.Stats.Health.CurrentValue)
                .FirstOrDefault();

            if (!targetPlayer)
                targetPlayer = player;

            return targetPlayer.transform;
        }

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
                        .ThenBy(x => Vector3.Distance(x.transform.position, player.transform.position))
                        .ThenBy(x => x.Stats.Health.CurrentValue)
                        .ThenBy(x => x.Attackers.Count)
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
        if (player.TrainingHealing)
        {
            var tar = target.GetComponent<PlayerController>();
            if (!tar)
            {
                return false;
            }

            return player.Heal(tar);
        }

        var enemy = target.GetComponent<EnemyController>();
        if (!enemy)
        {
            return false;
        }

        return player.Attack(enemy);
    }

    public override void TargetAcquired(PlayerController player, Transform target)
    {
        if (player.TrainingHealing)
        {
            return;
        }

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

        if (!target || target == null)
            return false;

        if (!player.IsReadyForAction)
        {
            return false;
        }

        if (player.TrainingHealing)
        {
            var tar = target.GetComponent<PlayerController>();
            if (!tar)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }

            if (tar.Stats.IsDead)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }
            //var possibleTargets = player.Island?.GetPlayers();
            //if (possibleTargets == null || !possibleTargets.FirstOrDefault(x => x != null && x && x.transform != null && target && target != null && x.transform == target))
            //{
            //    reason = TaskExecutionStatus.InvalidTarget;
            //    return false;
            //}
        }
        else
        {
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
        }

        var range = player.GetAttackRange();
        var collider = target.GetComponent<SphereCollider>();
        if (collider && collider.radius > range)
        {
            range = collider.radius;
        }

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