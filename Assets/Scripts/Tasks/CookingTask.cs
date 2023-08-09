using System;
using System.Collections.Generic;

public class CookingTask : StationTask
{
    public CookingTask(Func<IReadOnlyList<CraftingStation>> lazyCraftingStations)
        : base(CraftingStationType.Cooking, lazyCraftingStations)
    {
    }
}