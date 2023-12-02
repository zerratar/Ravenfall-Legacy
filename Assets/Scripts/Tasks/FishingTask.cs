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

    public override bool IsCompleted(PlayerController player, object target)
    {
        var fishingSpot = target as FishingController;
        if (!fishingSpot || fishingSpot.IsInvalid) return true;
        return false;
    }

    public override object GetTarget(PlayerController player)
    {
        var fishingSpots = lazyFishes();

        return fishingSpots
                //.Where(x => !x.IsDepleted)
                .OrderBy(x => UnityEngine.Random.value)
                .ThenBy(x => Vector3.Distance(player.transform.position, x.transform.position))
                .FirstOrDefault(x => !x.IsInvalid);
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

        var fishingSpot = target as FishingController;
        if (!fishingSpot)
        {
            return false;
        }

        return player.Fish(fishingSpot);
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

        //var possibleTargets = lazyFishes();
        //if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        //{
        //    reason = TaskExecutionStatus.InvalidTarget;
        //    return false;
        //}

        var fish = target as FishingController;
        if (!fish)
        {
            return false;
        }

        if (fish.IsInvalid)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (Vector3.Distance(player.transform.position, fish.transform.position) >= fish.MaxActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        var tar = target as FishingController;
        if (!tar)
        {
            return false;
        }

        var possibleTargets = lazyFishes();
        return possibleTargets.Any(x => x.GetInstanceID() == tar.GetInstanceID());
    }


    internal override void SetTargetInvalid(object target)
    {
        if (target is FishingController entity)
        {
            entity.IsInvalid = true;
            //Shinobytes.Debug.LogError(station.name + " is unreachable. Marked invalid to avoid being selected in the future.");
        }
    }
}