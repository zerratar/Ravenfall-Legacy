using System;
using System.Linq;
using UnityEngine;

public class WoodcuttingTask : ChunkTask
{
    private readonly Func<TreeController[]> lazyTrees;

    public WoodcuttingTask(Func<TreeController[]> lazyTrees)
    {
        this.lazyTrees = lazyTrees;
    }
    public override bool IsCompleted(PlayerController player, object target)
    {
        var tree = target as TreeController;
        if (!tree)
        {
            return true;
        }

        return tree.IsStump;
    }
    public override object GetTarget(PlayerController player)
    {
        var trees = lazyTrees();

        return trees
            .Where(x => !x.IsStump)
            .OrderBy(x => Vector3.Distance(player.Position, x.transform.position) + UnityEngine.Random.value * 10f)
            .FirstOrDefault();
    }

    public override bool Execute(PlayerController player, object target)
    {
        var tree = target as TreeController;
        return player.Cut(tree);
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
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        //var possibleTargets = lazyTrees();
        //if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        //{
        //    reason = TaskExecutionStatus.InvalidTarget;
        //    return false;
        //}

        var tree = target as TreeController;
        if (!tree)
        {
            return false;
        }
        if (tree.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }
        var distance = Vector3.Distance(player.Position, tree.transform.position);
        if (distance >= tree.MaxActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        var tar = target as TreeController;
        if (!tar)
        {
            return false;
        }

        var possibleTargets = lazyTrees();
        return possibleTargets.Any(x => x.GetInstanceID() == tar.GetInstanceID());
    }
}