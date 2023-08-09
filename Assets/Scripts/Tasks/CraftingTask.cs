using System;
using System.Collections.Generic;

public class CraftingTask : StationTask
{
    public CraftingTask(Func<IReadOnlyList<CraftingStation>> lazyCraftingStations)
      : base(CraftingStationType.Crafting, lazyCraftingStations)
    {
    }
}