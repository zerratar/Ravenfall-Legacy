using System;

public class UsedCommandInfo
{
    public UsedCommandInfo(string command, DateTime lastUsed, TimeSpan cooldown)
    {
        Command = command;
        LastUsed = lastUsed;
        Cooldown = cooldown;
    }

    public string Command { get; }
    public DateTime LastUsed { get; set; }
    public TimeSpan Cooldown { get; set; }
}
