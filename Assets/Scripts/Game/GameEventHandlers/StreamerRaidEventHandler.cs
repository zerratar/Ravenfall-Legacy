public class StreamerRaidEventHandler : RaidEventHandler
{
    public override void Handle(GameManager gameManager, StreamRaidInfo data)
    {
        OnStreamerRaid(gameManager, data, false);
    }
}
