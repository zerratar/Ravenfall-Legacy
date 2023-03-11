using System;

public class VillageHouseInfo
{
    public Guid? OwnerUserId { get; set; }
    public string Owner { get; set; }
    public int Type { get; set; }
    public int Slot { get; set; }
}
