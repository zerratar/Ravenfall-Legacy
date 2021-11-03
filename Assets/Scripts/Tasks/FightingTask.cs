using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FightingTask : ChunkTask
{
    private readonly Func<EnemyController[]> lazyEnemies;

    //private readonly Func<EnemyController[]> lazyEnemies;
    private EnemyController[] allEnemies;
    private Dictionary<int, EnemyController> instanceLookup;
    public FightingTask(Func<EnemyController[]> lazyEnemies)
    {
        this.lazyEnemies = lazyEnemies;
    }

    private EnemyController[] GetEnemies(bool reload = false)
    {
        if (this.allEnemies != null && !reload)
        {
            return this.allEnemies;
        }

        this.allEnemies = lazyEnemies();
        if (this.allEnemies == null || this.allEnemies.Length == 0)
        {
            this.instanceLookup = new Dictionary<int, EnemyController>();
        }
        else
        {
            this.instanceLookup = this.allEnemies.ToDictionary(x => x.GetInstanceID());
        }
        return allEnemies;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        if (player.TrainingHealing)
        {
            var plr = target as PlayerController;
            return plr == null || !plr || plr.Stats.IsDead || plr.TrainingHealing || plr.Stats.HealthPercent >= 0.9;
        }
        else
        {
            var enemy = target as EnemyController;
            return enemy == null || !enemy || enemy.Stats.Health.CurrentValue <= 0;
        }
    }

    public override object GetTarget(PlayerController player)
    {
        if (player.TrainingHealing)
        {
            if (player.Duel.InDuel || player.Arena.InArena)
            {
                return player;
            }

            var players = player.Island.GetPlayers();
            var playerCount = players.Count;
            PlayerController targetPlayer = null;
            var healthDif = 0;
            for (var i = 0; i < playerCount; ++i)
            {
                var x = players[i];
                if (x.Stats.Health.CurrentValue <= 0)
                {
                    continue;
                }

                var hd = x.Stats.Health.Level - x.Stats.Health.CurrentValue;
                if (hd >= healthDif)
                {
                    healthDif = hd;
                    targetPlayer = x;
                }
            }

            if (!targetPlayer)
            {
                targetPlayer = player;
            }

            return targetPlayer;
        }

        var enemies = GetEnemies(); //allEnemies;//lazyEnemies();
        var attackers = player.GetAttackers();
        try
        {
            if (attackers.Count > 0)
            {
                var e = attackers.GetEnumerator();
                while (e.MoveNext())
                {
                    var attacker = e.Current;
                    if (attacker == null || !attacker.Transform)
                    {
                        continue;
                    }

                    var enemyController = attacker as EnemyController;
                    if (enemyController == null || !enemyController)
                    {
                        continue;
                    }
                    if (instanceLookup.TryGetValue(enemyController.GetInstanceID(), out var existingTarget) && enemyController && !enemyController.Stats.IsDead)
                    {
                        return enemyController;
                    }
                }
            }

            float maxDist = float.MaxValue;
            EnemyController enemy = null;
            for (var i = 0; i < enemies.Length; ++i)
            {
                try
                {
                    var x = enemies[i];
                    if (x == null || !x || x.gameObject == null || x.Stats.Health.CurrentValue <= 0)
                        continue;

                    var dist = Vector3.Distance(x.Position, player.Position) + UnityEngine.Random.value;
                    if (dist < maxDist)
                    {
                        maxDist = dist;
                        enemy = x;
                    }
                }
                catch (Exception exc)
                {
#if DEBUG
                    GameManager.LogError(exc);
#endif
                    continue;
                }
            }

            //if (!enemy || enemy == null)
            //{
            //    GetEnemies(true);
            //    UnityEngine.Debug.LogError("All enemies are dead, cannot find new target");
            //}

            return enemy;
        }
        catch (Exception exc)
        {
#if DEBUG
            GameManager.LogError(exc);
#endif
            return null;
        }
    }

    public override bool Execute(PlayerController player, object target)
    {
        if (player.TrainingHealing)
        {
            var tar = target as PlayerController;
            if (!tar)
            {
                return false;
            }

            return player.Heal(tar);
        }

        var enemy = target as EnemyController;
        if (!enemy)
        {
            return false;
        }

        return player.Attack(enemy);
    }

    public override void TargetAcquired(PlayerController player, object target)
    {
        if (player.TrainingHealing)
        {
            return;
        }

        var enemy = target as EnemyController;
        if (!enemy)
        {
            return;
        }

        enemy.AddAttacker(player);
    }

    public override bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        var attackable = target as IAttackable;
        reason = TaskExecutionStatus.NotReady;
        if (!player)
        {
            return false;
        }
        if (attackable == null)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }
        if (target == null || attackable.Transform == null)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (!player.IsReadyForAction)
        {
            reason = TaskExecutionStatus.NotReady;
            return false;
        }

        if (player.TrainingHealing)
        {
            var tar = target as PlayerController;
            if (!tar)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }

            if (tar.Island != player.Island)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }

            if (tar.Stats.Health.CurrentValue <= 0)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }
        }
        else
        {
            var enemy = target as EnemyController;
            if (!enemy)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }

            if (enemy.Island != player.Island)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }

            if (enemy.Stats.Health.CurrentValue <= 0)
            {
                reason = TaskExecutionStatus.InvalidTarget;
                return false;
            }
        }

        var range = player.GetAttackRange();
        var hitRange = attackable.GetHitRange();
        if (hitRange > range)
        {
            range = hitRange;
        }

        var distance = Vector3.Distance(player.Position, attackable.Position);
        if (distance > range)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        return true;
    }
}