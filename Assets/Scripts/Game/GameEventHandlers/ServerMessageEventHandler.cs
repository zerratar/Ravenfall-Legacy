public class ServerMessageEventHandler : GameEventHandler<ServerMessage>
{
    protected override void Handle(GameManager gameManager, ServerMessage data)
    {
        UnityEngine.Debug.Log($"Message from server: " + data.Message);
    }
}
