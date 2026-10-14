using System;
using System.Collections.Generic;
using UnityEngine;

public class GatheringTask : ChunkTask
{
    private readonly Func<IReadOnlyList<ResourceTarget>> lazyTargets;

    public GatheringTask(Func<IReadOnlyList<ResourceTarget>> lazyTargets)
    {
        this.lazyTargets = lazyTargets;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        return target is not ResourceTarget spot || !spot.IsAvailable;
    }

    public override object GetTarget(PlayerController player)
    {
        return ResourceTarget.PickFor(player, lazyTargets());
    }

    public override bool Execute(PlayerController player, object target)
    {
        if (target is not ResourceTarget spot || !player)
        {
            return false;
        }

        return player.Gather(spot);
    }

    public override bool CanExecute(PlayerController player, object targetObject, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        if (targetObject is not ResourceTarget target)
        {
            return false;
        }

        if (!player || player.Stats.IsDead || !player.IsReadyForAction)
        {
            return false;
        }

        if (!target.IsAvailable)
        {
            reason = TaskExecutionStatus.NotReady;
            return false;
        }

        if (target.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        // targeting is enough to share in the exp when it is gathered
        target.AddTargeter(player);

        if (Vector3.Distance(player.Position, target.Position) >= target.ActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        if (target is not ResourceTarget spot)
        {
            return false;
        }

        var possibleTargets = lazyTargets();
        for (var i = 0; i < possibleTargets.Count; ++i)
        {
            if (possibleTargets[i] == spot) return true;
        }
        return false;
    }

    internal override void SetTargetInvalid(object target)
    {
        if (target is ResourceTarget spot && badPathReported.Add(spot))
        {
            Shinobytes.Debug.LogError(spot.Name + " is unreachable.");
        }
    }

    private readonly HashSet<ResourceTarget> badPathReported = new();
}
