public class ArenaEnd : PacketHandler<Player>
{
    public ArenaEnd(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
    }
}