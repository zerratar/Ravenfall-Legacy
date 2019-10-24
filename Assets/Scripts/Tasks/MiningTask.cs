using System;
using System.Linq;
using UnityEngine;

public class MiningTask : ChunkTask
{
    private readonly Func<RockController[]> lazyRock;

    public MiningTask(Func<RockController[]> lazyRock)
    {
        this.lazyRock = lazyRock;
    }

    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var rock = target.GetComponent<RockController>();
        if (!rock) return true;
        return false;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var rocks = lazyRock();

        return rocks
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

        var miningSpot = target.GetComponent<RockController>();
        if (!miningSpot)
        {
            return false;
        }

        return player.Mine(miningSpot);
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

        var possibleTargets = lazyRock();
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