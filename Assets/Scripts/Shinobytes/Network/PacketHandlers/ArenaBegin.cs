public class ArenaBegin : PacketHandler<TwitchPlayerInfo>
{
    public ArenaBegin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
    }
}