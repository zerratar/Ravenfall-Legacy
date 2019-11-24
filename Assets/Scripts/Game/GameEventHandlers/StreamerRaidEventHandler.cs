public class StreamerRaidEventHandler : RaidEventHandler
{
    protected override void Handle(GameManager gameManager, StreamRaidInfo data)
    {
        OnStreamerRaid(gameManager, data, false);
    }
}
