using System;
using System.Linq;
using UnityEngine;

public class FishingTask : ChunkTask
{
    private readonly Func<FishingController[]> lazyFishes;

    public FishingTask(Func<FishingController[]> lazyFishes)
    {
        this.lazyFishes = lazyFishes;
    }

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var fishingSpot = target.GetComponent<FishingController>();
        if (!fishingSpot) return true;
        return false;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var fishingSpots = lazyFishes();

        return fishingSpots
                //.Where(x => !x.IsDepleted)
                .OrderBy(x => UnityEngine.Random.value)
                .ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
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

        var fishingSpot = target.GetComponent<FishingController>();
        if (!fishingSpot)
        {
            return false;
        }

        return player.Fish(fishingSpot);
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

        var possibleTargets = lazyFishes();
        if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        var collider = target.GetComponent<CapsuleCollider>();
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