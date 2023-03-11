public class SetTimeOfDayRequest
{
    public SetTimeOfDayRequest(int totalTime, int freezeTime)
    {
        this.TotalTime = totalTime;
        this.FreezeTime = freezeTime;
    }

    public int TotalTime { get; }
    public int FreezeTime { get; }
}