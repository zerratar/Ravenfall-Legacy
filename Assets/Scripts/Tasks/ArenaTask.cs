using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArenaTask : ChunkTask
{
    private readonly ArenaController arena;

    public ArenaTask(ArenaController arena)
    {
        this.arena = arena;
    }

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        if (player.transform == target)
        {
            return true;
        }

        if (player.Stats.IsDead)
        {
            return true;
        }

        if (!target)
        {
            return true;
        }

        var targetPlayer = target.GetComponent<PlayerController>();
        return !targetPlayer || targetPlayer.Stats.IsDead;
    }

    public override Transform GetTarget(PlayerController player)
    {
        if (!arena.Started)
        {
            return player.transform;
        }

        var attackers = player.GetAttackers();
        var attackerTarget = attackers
            .Cast<PlayerController>()
            .Where(atk => !atk.GetStats().IsDead)
            .OrderBy(atk => Vector3.Distance(player.transform.position, atk.transform.position))
            .FirstOrDefault();

        if (attackerTarget && arena.AvailablePlayers.Contains(attackerTarget))
        {
            return attackerTarget.transform;
        }

        var target = arena
            .AvailablePlayers
            .Except(player)
            .OrderBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .FirstOrDefault()?
            .transform;

        if (!target)
        {
            arena.End();
        }

        return target;
    }

    public override bool Execute(PlayerController player, Transform target)
    {
        if (!arena.Started)
        {
            return false;
        }

        var targetPlayer = target.GetComponent<PlayerController>();
        if (target == player)
        {
            return false;
        }

        return player.Attack(targetPlayer);
    }

    public override bool CanExecute(PlayerController player, Transform target, out TaskExecutionStatus reason)
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

        if (player.Stats.IsDead)
        {
            return false;
        }

        if (!target)
        {
            return false;
        }

        if (!arena.AvailablePlayers.FirstOrDefault(x => x.transform == target))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (!arena.InsideArena(target))
        {
            return false;
        }

        if (!arena.InsideArena(player.transform))
        {
            return false;
        }

        if (Vector3.Distance(player.transform.position, target.position) > player.AttackRange)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        return true;
    }
}