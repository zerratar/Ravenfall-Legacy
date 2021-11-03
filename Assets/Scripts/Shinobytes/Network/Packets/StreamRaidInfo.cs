using System;
using System.Collections.Generic;

public class StreamRaidInfo
{
    public string RaiderUserName { get; set; }
    public string RaiderUserId { get; set; }
    public List<UserCharacter> Players { get; set; }
}

public class UserCharacter
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public Guid CharacterId { get; set; }
}
