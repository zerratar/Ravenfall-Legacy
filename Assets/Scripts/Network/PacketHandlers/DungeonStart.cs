public class DungeonStart : PacketHandler<Player>
{
    public DungeonStart(
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
