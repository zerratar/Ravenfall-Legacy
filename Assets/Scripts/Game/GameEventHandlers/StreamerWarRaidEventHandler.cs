public class StreamerWarRaidEventHandler : RaidEventHandler
{
    public override void Handle(GameManager gameManager, StreamRaidInfo data)
    {
        OnStreamerRaid(gameManager, data, true);
    }
}
