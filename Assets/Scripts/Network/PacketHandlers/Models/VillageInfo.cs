using System.Collections.Generic;

public class VillageInfo
{
    public int Level { get; set; }
    public long Experience { get; set; }
    public string Name { get; set; }
    public IReadOnlyList<VillageHouseInfo> Houses { get; set; }
}