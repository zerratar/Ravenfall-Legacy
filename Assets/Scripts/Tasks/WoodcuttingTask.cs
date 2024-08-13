using Shinobytes.Linq;
using System;
//using System.Linq;
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

        var totalCount = trees.Length;
        var availableTargets = trees.AsList(x => !x.IsStump);
        var left = (float)availableTargets.Count / totalCount;
        // if less than 25% of the trees are available, decrease respawn time
        if (left <= 0.25)
        {
            foreach (var t in trees) t.DecreaseRespawnTime();
        }
        // if more than 50% of the trees are available, increase respawn time
        else if (left >= 0.5)
        {
            foreach (var t in trees) t.IncreaseRespawnTime();
        }

        return availableTargets
            .OrderBy(x => Vector3.Distance(player.Position, x.transform.position) + (UnityEngine.Random.value * 15f))
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
        if (target == null)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

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

        tree.AddWoodcutter(player);

        if (!player.IsReadyForAction)
        {
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