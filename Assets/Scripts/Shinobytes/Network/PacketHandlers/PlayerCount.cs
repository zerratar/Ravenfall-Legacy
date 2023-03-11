public class PlayerCount : ChatBotCommandHandler<User>
{
    public PlayerCount(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        var playerCount = PlayerManager.GetPlayerCount();
        client.SendReply(gm, Localization.MSG_PLAYERS_ONLINE, playerCount);
    }
}
