using Shinobytes.Linq;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class ArenaTask : ChunkTask
{
    private readonly ArenaController arena;
    public ArenaTask(ArenaController arena)
    {
        this.arena = arena;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        var targetPlayer = target as PlayerController;
        if (targetPlayer == null)
        {
            return true;
        }

        if (player.Id == targetPlayer.Id)
        {
            return true;
        }

        if (player.Stats.IsDead)
        {
            return true;
        }

        return !targetPlayer || targetPlayer.Stats.IsDead;
    }

    public override object GetTarget(PlayerController player)
    {
        if (!arena.Started)
        {
            return player;
        }

        var attackers = player.GetAttackers();
        var attackerTarget = attackers
            .OfType<PlayerController>()
            .Where(atk => !atk.GetStats().IsDead)
            .OrderBy(atk => Vector3.Distance(player.Position, atk.Position))
            .FirstOrDefault();

        if (attackerTarget && arena.AvailablePlayers.Contains(attackerTarget))
        {
            return attackerTarget;
        }

        var target = arena
            .AvailablePlayers
            .Except(player)
            .OrderBy(x => Vector3.Distance(player.Position, x.Position))
            .FirstOrDefault();

        if (!target)
        {
            arena.End();
        }

        return target;
    }

    public override bool Execute(PlayerController player, object target)
    {
        if (!arena.Started)
        {
            return false;
        }

        var targetPlayer = target as PlayerController;
        if (target == player)
        {
            return false;
        }

        return player.Attack(targetPlayer);
    }

    public override bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        var attackable = target as IAttackable;
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

        if (target == null || attackable == null)
        {
            return false;
        }

        if (!arena.AvailablePlayers.FirstOrDefault(x => x.transform.GetInstanceID() == attackable.Transform.GetInstanceID()))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (!arena.InsideArena(attackable))
        {
            return false;
        }

        if (!arena.InsideArena(player.transform))
        {
            return false;
        }

        if (Vector3.Distance(player.transform.position, attackable.Position) > player.AttackRange)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        // We do the check in canexecute already.
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

    private HashSet<string> badPathReported = new HashSet<string>();
}