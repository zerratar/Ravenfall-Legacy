using System;
using System.Linq;
using UnityEngine;

public class FarmingTask : ChunkTask
{
    private readonly Func<FarmController[]> lazyTargets;

    public FarmingTask(Func<FarmController[]> lazyTargets)
    {
        this.lazyTargets = lazyTargets;
    }
    public override bool IsCompleted(PlayerController player, object target)
    {
        var farm = target as FarmController;
        if (!farm) return true;
        return false;
    }
    public override object GetTarget(PlayerController player)
    {
        var farmingSpots = lazyTargets();

        return farmingSpots
            //.Where(x => !x.IsDepleted)
            .OrderBy(x => UnityEngine.Random.value)
            //.ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .FirstOrDefault();
    }

    public override bool Execute(PlayerController player, object target)
    {
        var farmingPatch = target as FarmController;
        if (target == null || !farmingPatch || !player)
        {
            return false;
        }

        return player.Farm(farmingPatch);
    }

    public override bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        var farm = target as FarmController;

        if (!farm || !player || player.Stats.IsDead || !player.IsReadyForAction || target == null)
        {
            return false;
        }

        if (farm.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (Vector3.Distance(player.transform.position, farm.transform.position) >= farm.MaxActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        var tar = target as FarmController;
        if (!tar)
        {
            return false;
        }

        var possibleTargets = lazyTargets();
        return possibleTargets.Any(x => x.GetInstanceID() == tar.GetInstanceID());
    }
}
