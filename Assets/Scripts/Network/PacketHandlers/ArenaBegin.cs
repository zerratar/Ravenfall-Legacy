public class ArenaBegin : PacketHandler<Player>
{
    public ArenaBegin(
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