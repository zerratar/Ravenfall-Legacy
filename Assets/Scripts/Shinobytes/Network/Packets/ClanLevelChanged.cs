using System;

public class ClanLevelChanged
{
    public Guid ClanId { get; set; }
    public int LevelDelta { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
}
