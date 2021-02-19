public class ArenaBegin : PacketHandler<Player>
{
    public ArenaBegin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
    }
}