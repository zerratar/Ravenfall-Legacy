public class DungeonForce : PacketHandler<Player>
{
    public DungeonForce(
         GameManager game,
         GameServer server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    { }
}
