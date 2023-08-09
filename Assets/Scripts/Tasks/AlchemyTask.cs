using System;
using System.Collections.Generic;

public class AlchemyTask : StationTask
{
    public AlchemyTask(Func<IReadOnlyList<CraftingStation>> lazyCraftingStations)
        : base(CraftingStationType.Brewing, lazyCraftingStations)
    {
    }
}
