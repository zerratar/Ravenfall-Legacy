public class SetTimeOfDayRequest
{
    public SetTimeOfDayRequest(TwitchPlayerInfo player, int totalTime, int freezeTime)
    {
        this.Player = player;
        this.TotalTime = totalTime;
        this.FreezeTime = freezeTime;
    }

    public int TotalTime { get; }
    public int FreezeTime { get; }
    public TwitchPlayerInfo Player { get; }
}