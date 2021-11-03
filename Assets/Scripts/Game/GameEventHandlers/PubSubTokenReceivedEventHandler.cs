public class PubSubTokenReceivedEventHandler : GameEventHandler<PubSubToken>
{
    protected override void Handle(GameManager gameManager, PubSubToken data)
    {
        if (gameManager.RavenBot.IsConnectedToLocal)
        {
            gameManager.RavenBot.SendPubSubToken(
                gameManager.RavenNest.TwitchUserId,
                gameManager.RavenNest.TwitchUserName,
                data.Token);
        }
    }
}