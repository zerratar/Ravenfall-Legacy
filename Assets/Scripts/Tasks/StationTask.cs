using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StationTask : ChunkTask
{
    private readonly CraftingStationType type;
    private readonly Func<IReadOnlyList<CraftingStation>> lazyCraftingStations;

    public StationTask(CraftingStationType type, Func<IReadOnlyList<CraftingStation>> lazyCraftingStations)
    {
        this.lazyCraftingStations = lazyCraftingStations;
        this.type = type;
    }

    public override bool IsCompleted(PlayerController player, object target)
    {
        var craftingStation = target as CraftingStation;
        if (!craftingStation || craftingStation.StationType != type || craftingStation.IsInvalid) return true;
        return false;
    }

    public override object GetTarget(PlayerController player)
    {
        var all = lazyCraftingStations();
        return all.FirstOrDefault(x => x.StationType == type && !x.IsInvalid);
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
        switch (type)
        {
            case CraftingStationType.Cooking:
                return player.Cook(craftingStation);
            case CraftingStationType.Crafting:
                return player.Craft(craftingStation);
            case CraftingStationType.Brewing:
                return player.Brew(craftingStation);
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

        var station = target as CraftingStation;
        if (!station)
        {
            return false;
        }

        if (station.Island != player.Island || station.IsInvalid)
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

    internal override void SetTargetInvalid(object target)
    {
        if (target is CraftingStation station)
        {
            station.IsInvalid = true;
            //Shinobytes.Debug.LogError(station.name + " is unreachable. Marked invalid to avoid being selected in the future.");
        }
    }
}
