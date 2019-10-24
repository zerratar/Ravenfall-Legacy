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

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var tree = target.GetComponent<TreeController>();
        if (!tree)
        {
            return true;
        }

        return tree.IsStump;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var trees = lazyTrees();

        return trees
            .Where(x => !x.IsStump)
            .OrderBy(x => Vector3.Distance(player.transform.position, x.transform.position))
            .ThenBy(x => UnityEngine.Random.value)
            .FirstOrDefault()?
            .transform;
    }

    public override bool Execute(PlayerController player, Transform target)
    {
        var tree = target.GetComponent<TreeController>();
        return player.Cut(tree);
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

        var possibleTargets = lazyTrees();
        if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        var tree = target.GetComponent<TreeController>();
        if (!tree)
        {
            return false;
        }

        var collider = target.GetComponent<SphereCollider>();
        if (!collider)
        {
            return false;
        }

        var distance = Vector3.Distance(player.transform.position, target.position);
        if (distance >= collider.radius)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }
}