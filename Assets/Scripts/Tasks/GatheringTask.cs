using Shinobytes.Linq;
using System;
using System.Collections.Generic;
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
        var totalCount = gatherSpots.Length;
        var availableTargets = gatherSpots.AsList(x => !x.IsDepleted);
        var left = (float)availableTargets.Count / totalCount;
        // if less than 25% of the trees are available, decrease respawn time
        if (left <= 0.25)
        {
            foreach (var t in gatherSpots) t.DecreaseRespawnTime();
        }
        // if more than 50% of the trees are available, increase respawn time
        else if (left >= 0.5)
        {
            foreach (var t in gatherSpots) t.IncreaseRespawnTime();
        }

        return availableTargets
            .OrderBy(x => Vector3.Distance(player.transform.position, x.transform.position) + (UnityEngine.Random.value * 15f))
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

    private System.Collections.Generic.HashSet<string> badPathReported = new ();
}