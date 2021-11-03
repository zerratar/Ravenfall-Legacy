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
    public override bool IsCompleted(PlayerController player, object target)
    {
        var craftingStation = target as CraftingStation;
        if (!craftingStation || craftingStation.StationType != CraftingStationType.Cooking) return true;
        return false;
    }

    public override object GetTarget(PlayerController player)
    {
        var all = lazyCraftingStations();
        return all.FirstOrDefault(x => x.StationType == CraftingStationType.Cooking);
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

        var craftingStation = target as CraftingStation;
        if (!craftingStation)
        {
            return false;
        }

        return player.Cook(craftingStation);
    }

    public override bool CanExecute(
        PlayerController player,
        object target,
        out TaskExecutionStatus reason)
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

        //var possibleTargets = lazyCraftingStations();
        //if (!possibleTargets.FirstOrDefault(x => x.transform == target))
        //{
        //    reason = TaskExecutionStatus.InvalidTarget;
        //    return false;
        //}

        var station = target as CraftingStation;
        if (!station)
        {
            return false;
        }

        if (station.Island != player.Island)
        {
            reason = TaskExecutionStatus.InvalidTarget;
            return false;
        }

        if (Vector3.Distance(player.transform.position, station.transform.position) >= station.MaxActionDistance)
        {
            reason = TaskExecutionStatus.OutOfRange;
            return false;
        }

        reason = TaskExecutionStatus.Ready;
        return true;
    }

    internal override bool TargetExistsImpl(object target)
    {
        var station = target as CraftingStation;
        if (!station)
        {
            return false;
        }

        var possibleTargets = lazyCraftingStations();
        return possibleTargets.Any(x => x.GetInstanceID() == station.GetInstanceID());
    }
}