using System;
using System.Collections.Generic;
using UnityEngine;

public class WoodcuttingTask : ChunkTask
{
    private readonly Func<IReadOnlyList<ResourceTarget>> lazyTrees;

    public WoodcuttingTask(Func<IReadOnlyList<ResourceTarget>> lazyTrees)
    {
        this.lazyTrees = lazyTrees;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        return target is not ResourceTarget tree || !tree.IsAvailable;
    }

    public override object GetTarget(PlayerController player)
    {
        return ResourceTarget.PickFor(player, lazyTrees());
    }

    public override bool Execute(PlayerController player, object target)
    {
        return target is ResourceTarget tree && player.Cut(tree);
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
        if (target == null)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (target is not ResourceTarget tree)
        {
            return false;
        }

        if (tree.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        // targeting is enough to share in the exp when it falls
        tree.AddTargeter(player);

        if (!player.IsReadyForAction)
        {
            return false;
        }

        var distance = Vector3.Distance(player.Position, tree.Position);
        if (distance >= tree.ActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        if (target is not ResourceTarget tree)
        {
            return false;
        }

        var possibleTargets = lazyTrees();
        for (var i = 0; i < possibleTargets.Count; ++i)
        {
            if (possibleTargets[i] == tree) return true;
        }
        return false;
    }

    internal override void SetTargetInvalid(object target)
    {
        if (target is ResourceTarget tree && badPathReported.Add(tree))
        {
            Shinobytes.Debug.LogError(tree.Name + " is unreachable.");
        }
    }

    private readonly HashSet<ResourceTarget> badPathReported = new();
}
