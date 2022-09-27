public class ServerTimeEventHandler : GameEventHandler<ServerTime>
{
    public override void Handle(GameManager gameManager, ServerTime data)
    {
        gameManager.UpdateServerTime(data.TimeUtc);
    }
}
