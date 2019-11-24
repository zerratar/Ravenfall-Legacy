public class StreamerWarRaidEventHandler : RaidEventHandler
{
    protected override void Handle(GameManager gameManager, StreamRaidInfo data)
    {
        OnStreamerRaid(gameManager, data, true);
    }
}
