using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CookingTask : ChunkTask
{
    private readonly Func<IReadOnlyList<CraftingStation>> lazyCraftingStations;

    public CookingTask(Func<IReadOnlyList<CraftingStation>> lazyCraftingStations)
    {
        this.lazyCraftingStations = lazyCraftingStations;
    }
    public override bool IsCompleted(PlayerController player, Transform target)
    {
        var craftingStation = target.GetComponent<CraftingStation>();
        if (!craftingStation || craftingStation.StationType != CraftingStationType.Cooking) return true;
        return false;
    }

    public override Transform GetTarget(PlayerController player)
    {
        var all = lazyCraftingStations();
        return all.OrderBy(x => UnityEngine.Random.value).FirstOrDefault(x => x.StationType == CraftingStationType.Cooking)?.transform;
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

        var craftingStation = target.GetComponent<CraftingStation>();
        if (!craftingStation)
        {
            return false;
        }

        return player.Cook(craftingStation);
    }

    public override bool CanExecute(PlayerController player, Transform target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;

        if (player.Stats.IsDead)
        {
            return false;
        }

        if (!player.IsReadyForAction)
        {
            return false;
        }

        var possibleTargets =lazyCraftingStations();
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