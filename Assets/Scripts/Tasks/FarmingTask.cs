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

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var farm = target.GetComponent<FarmController>();
        if (!farm) return true;
        return false;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var farmingSpots = lazyTargets();

        return farmingSpots
            //.Where(x => !x.IsDepleted)
            .OrderBy(x => UnityEngine.Random.value)
            //.ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .FirstOrDefault()?
            .transform;
    }

    public override bool Execute(PlayerController player, Transform target)
    {
        if (!target)
        {
            return false;
        }

        if (!player)
        {
            return false;
        }

        var farmingPatch = target.GetComponent<FarmController>();
        if (!farmingPatch)
        {
            return false;
        }

        return player.Farm(farmingPatch);
    }

    public override bool CanExecute(PlayerController player, Transform target, out TaskExecutionStatus reason)
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

        if (!target)
        {
            return false;
        }

        var possibleTargets = lazyTargets();
        if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        var collider = target.GetComponent<SphereCollider>();
        if (!collider)
        {
            return false;
        }

        if (Vector3.Distance(player.transform.position, target.transform.position) >= collider.radius)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }
}