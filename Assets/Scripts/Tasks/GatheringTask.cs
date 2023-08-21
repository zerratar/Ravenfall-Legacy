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
        var gather = target as GatherController;
        if (!gather || gather.IsDepleted) return true;
        return false;
    }
    public override object GetTarget(PlayerController player)
    {
        var gatherSpots = lazyTargets();

        return gatherSpots
            .Where(x => !x.IsDepleted)
            //.OrderBy(x => UnityEngine.Random.value)
            .OrderBy(x => Vector3.Distance(player.transform.position, x.transform.position))
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

    public override bool CanExecute(PlayerController player, object targetObject, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        var target = targetObject as GatherController;

        if (!target || !player || player.Stats.IsDead || !player.IsReadyForAction || targetObject == null)
        {
            return false;
        }

        if (target.IsDepleted)
        {
            reason = TaskExecutionStatus.NotReady;
            return false;
        }

        if (target.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        target.AddGatherer(player);

        if (Vector3.Distance(player.transform.position, target.transform.position) >= target.MaxActionDistance)
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