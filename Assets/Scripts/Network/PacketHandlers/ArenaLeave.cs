public class ArenaLeave : PacketHandler<Player>
{
    public ArenaLeave(
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