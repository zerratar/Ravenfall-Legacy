using System;
using System.Linq;
using UnityEngine;

public class MiningTask : ChunkTask
{
    private readonly Func<RockController[]> lazyRock;

    public MiningTask(Func<RockController[]> lazyRock)
    {
        this.lazyRock = lazyRock;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        var rock = target as RockController;
        if (!rock) return true;
        return false;
    }

    public override object GetTarget(PlayerController player)
    {
        var rocks = lazyRock();

        return rocks
            .OrderBy(x => UnityEngine.Random.value)
            .ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .FirstOrDefault();
    }

    public override bool Execute(PlayerController player, object target)
    {
        if (target == null)
        {
            return false;
        }

        if (!player)
        {
            return false;
        }

        var miningSpot = target as RockController;
        if (!miningSpot)
        {
            return false;
        }

        return player.Mine(miningSpot);
    }

    public override bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;

        if (!player)
        {
            return false;
        }

        if (player.Stats.IsDead)
        {
            return false;
        }

        if (!player.IsReadyForAction)
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        //var possibleTargets = lazyRock();
        //if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        //{
        //    reason = TaskExecutionStatus.InvalidTarget;
        //    return false;
        //}
        var rock = target as RockController;
        if (!rock)
        {
            return false;
        }
        if (rock.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }
        if (Vector3.Distance(player.transform.position, rock.transform.position) >= rock.MaxActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        var tar = target as RockController;
        if (!tar)
        {
            return false;
        }

        var possibleTargets = lazyRock();
        return possibleTargets.Any(x => x.GetInstanceID() == tar.GetInstanceID());
    }
}