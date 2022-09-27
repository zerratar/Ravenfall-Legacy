public class ClientVersion : PacketHandler<TwitchPlayerInfo>
{
    public ClientVersion(GameManager game, RavenBotConnection server, PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        client.SendFormat(data.Username, "The streamer is currently running Ravenfall v{version}", Ravenfall.Version);
    }
}