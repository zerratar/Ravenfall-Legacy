using System;

public class ClanSkillLevelChanged
{
    public Guid ClanId { get; set; }
    public Guid SkillId { get; set; }
    public int LevelDelta { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
}