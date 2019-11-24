
public class RaidForce : PacketHandler<Player>
{
    public RaidForce(
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
            client.SendCommand(data.Username, "message", "Unable to start a raid during a war. Please wait for it to be over.");
            return;
        }

        if (Game.Raid && !Game.Raid.Started && !Game.Raid.Boss)
        {
            Game.Raid.StartRaid(data.Username);
        }
    }
}