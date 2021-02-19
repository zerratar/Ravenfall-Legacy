using System;

public interface ITwitchUser
{
    string Name { get; }
    string Alias { get; }
    long Credits { get; }
    bool CanAfford(long cost);
    void RemoveCredits(long amount);
    void AddCredits(long amount);

    bool CanUseCommand(string command);
    void UseCommand(string command, TimeSpan cooldown);
    TimeSpan GetCooldown(string command);
}
