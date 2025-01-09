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
        if (!fishingSpot) return true;
        return false;
    }

    public override object GetTarget(PlayerController player)
    {
        var fishingSpots = lazyFishes();

        return fishingSpots
                //.Where(x => !x.IsDepleted)
                .OrderBy(x => UnityEngine.Random.value)
                .ThenBy(x => Vector3.Distance(player.Position, x.Position))
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

        if (Vector3.Distance(player.Position, fish.Position) >= fish.MaxActionDistance)
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
        if (target != null && target is MonoBehaviour mb)
        {
            var path = mb.GetHierarchyPath();
            if (badPathReported.Add(path))
            {
                Shinobytes.Debug.LogError(path + " is unreachable.");
            }
        }
    }

    private System.Collections.Generic.HashSet<string> badPathReported = new();
}