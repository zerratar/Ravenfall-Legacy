using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FightingTask : ChunkTask
{
    private readonly Func<EnemyController[]> lazyEnemies;

    //private readonly Func<EnemyController[]> lazyEnemies;
    private EnemyController[] allEnemies;
    private Dictionary<EntityId, EnemyController> instanceLookup;
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
            this.instanceLookup = new Dictionary<EntityId, EnemyController>();
        }
        else
        {
            this.instanceLookup = this.allEnemies.ToDictionary(x => x.GetEntityId());
        }
        return allEnemies;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        if (player.TrainingHealing)
        {
            var plr = target as PlayerController;
            if (plr == null || !plr || plr.Stats.IsDead) return true;

            // the healer itself: kept, even at full health, until someone it may heal needs it more (the owner,
            // 2026-11-28). With nobody hurt it heals itself and earns Healing experience as usual. Who else needs
            // healing is looked at only when the next heal is due, not every frame (it was dropped and sought again
            // every frame before)
            if (plr == player)
            {
                if (player.duelHandler.InDuel || player.arenaHandler.InArena || !player.IsReadyForAction) return false;
                var neediest = FindHealTarget(player);
                return neediest && neediest != player;
            }

            // other healers are left to heal themselves
            return plr.TrainingHealing || plr.Stats.HealthPercent >= 0.9;
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
            if (player.duelHandler.InDuel || player.arenaHandler.InArena)
            {
                return player;
            }

            // nobody else in need: the healer heals itself (IsCompleted keeps it until someone needs healing)
            var neediest = FindHealTarget(player);
            return neediest ? neediest : player;
        }

        var enemies = GetEnemies(); //allEnemies;//lazyEnemies();
        var attackers = player.GetAttackers();
        try
        {
            for (var i = 0; i < attackers.Count; ++i)
            {
                var attacker = attackers[i];
                if (attacker == null || !attacker.Transform)
                {
                    continue;
                }

                var enemyController = attacker as EnemyController;
                if (enemyController == null || !enemyController)
                {
                    continue;
                }
                if (instanceLookup.ContainsKey(enemyController.GetEntityId()) && !enemyController.Stats.IsDead)
                {
                    return enemyController;
                }
            }

            float maxDist = float.MaxValue;
            EnemyController enemy = null;
            // taming: the creatures being tamed come first, the others only when none is left
            var taming = player.BattlePets.Taming != null;
            for (var i = 0; i < enemies.Length; ++i)
            {
                try
                {
                    var x = enemies[i];
                    if (x.Removed || x.Stats.Health.CurrentValue <= 0)
                        continue;

                    var dist = Vector3.Distance(x.Position, player.Position) + UnityEngine.Random.value;
                    if (taming && !player.BattlePets.IsTamingTarget(x)) dist += 1000f;
                    if (dist < maxDist)
                    {
                        maxDist = dist;
                        enemy = x;
                    }
                }
                catch (Exception exc)
                {
#if DEBUG
                    Shinobytes.Debug.LogError("FightingTask.GetTarget: " + exc);
#endif
                    continue;
                }
            }

            //if (!enemy || enemy == null)
            //{
            //    GetEnemies(true);
            //    Shinobytes.Debug.LogError("All enemies are dead, cannot find new target");
            //}

            return enemy;
        }
        catch (Exception exc)
        {
#if DEBUG
            Shinobytes.Debug.LogError("FightingTask.GetTarget: " + exc);
#endif
            return null;
        }
    }

    /// <summary>
    ///     The one a healer heals: the one most in need below 90% health, by share of health, not by health lost (a 999
    ///     health healer scratched for 6 healed itself before a 186 health hero half down, the user, 2026-11-04).
    ///     Ravebridge's hero comes first unless someone else is in real danger. Other healers are not healed by
    ///     healers; the healer itself is. Null when nobody on the island is below 90%.
    /// </summary>
    private static PlayerController FindHealTarget(PlayerController player)
    {
        var players = player.Island ? player.Island.GetPlayers() : null;
        if (players == null) return null;
        var playerCount = players.Count;
        var hero = player.InRavebridge && RavebridgeManager.Instance ? RavebridgeManager.Instance.Hero : null;
        PlayerController targetPlayer = null;
        var bestNeed = 0.9f;
        for (var i = 0; i < playerCount; ++i)
        {
            var x = players[i];
            // only players in the same place (the islands, the dungeon, the tower)
            if (!x || x.Stats.Health.CurrentValue <= 0 || x.ferryHandler.OnFerry || x.onsenHandler.InOnsen || player.Away != x.Away)
            {
                continue;
            }

            if (x.TrainingHealing && x != player) continue;
            var need = x.Stats.HealthPercent - (x == hero ? 0.25f : 0f);
            if (need < bestNeed)
            {
                bestNeed = need;
                targetPlayer = x;
            }
        }

        return targetPlayer;
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

            if (tar.ferryHandler.OnFerry || player.ferryHandler.OnFerry)
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



    internal override void SetTargetInvalid(object target)
    {
        if (target != null && target is MonoBehaviour mb)
        {
            var path = mb.GetHierarchyPath();
            if (badPathReported.Add(path))
            {
                Shinobytes.Debug.LogError(path + " is unreachable.");
            }
        }
    }

    private System.Collections.Generic.HashSet<string> badPathReported = new();
}