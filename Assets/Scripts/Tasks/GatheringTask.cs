using System;
using System.Linq;
using UnityEngine;

public class GatheringTask : ChunkTask
{
    private readonly Func<GatherController[]> lazyTargets;

    public GatheringTask(Func<GatherController[]> lazyTargets)
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
        var gatherSpots = lazyTargets();

        return gatherSpots
            .Where(x => !x.IsDepleted)
            .OrderBy(x => UnityEngine.Random.value)
            //.ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .FirstOrDefault();
    }

    public override bool Execute(PlayerController player, object target)
    {
        var gatherSpot = target as GatherController;
        if (target == null || !gatherSpot || !player)
        {
            return false;
        }

        return player.Gather(gatherSpot);
    }

    public override bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        var farm = target as GatherController;

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
        var tar = target as GatherController;
        if (!tar)
        {
            return false;
        }

        var possibleTargets = lazyTargets();
        return possibleTargets.Any(x => x.GetInstanceID() == tar.GetInstanceID());
    }
}