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
    {
        if (Game.StreamRaid.IsWar)
        {
            client.SendCommand(data.Username, "message", "Unable to start a dungeon during a war. Please wait for it to be over.");
            return;
        }

        if (Game.Dungeons && !Game.Dungeons.Started)
        {
            Game.Dungeons.ActivateDungeon();
        }
    }
}
