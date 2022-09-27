public class ServerMessageEventHandler : GameEventHandler<ServerMessage>
{
    public override void Handle(GameManager gameManager, ServerMessage data)
    {
        Shinobytes.Debug.Log($"Message from server: " + data.Message);
        gameManager.ServerNotifications.EnqueueServerMessage(data);
    }
}
