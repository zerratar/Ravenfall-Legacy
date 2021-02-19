public class ServerTimeEventHandler : GameEventHandler<ServerTime>
{
    protected override void Handle(GameManager gameManager, ServerTime data)
    {
        gameManager.UpdateServerTime(data.TimeUtc);
    }
}
