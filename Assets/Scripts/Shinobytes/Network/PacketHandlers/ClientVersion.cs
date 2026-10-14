public class ClientVersion : ChatBotCommandHandler<User>
{
    public ClientVersion(GameManager game, RavenBotConnection server, PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        client.SendReply(gm, Localization.MSG_CLIENT_VERSION, Ravenfall.Version);
    }
}