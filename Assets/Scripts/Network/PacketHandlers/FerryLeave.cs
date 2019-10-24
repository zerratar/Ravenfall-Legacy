public class FerryLeave : PacketHandler<Player>
{
    public FerryLeave(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendCommand(data.Username, "ferry_leave_failed", $"You are not currently playing. Type !join to start playing.");
            return;
        }

        if (!player.Ferry)
        {
            client.SendCommand(data.Username, "ferry_leave_failed", $"Uh oh! A bug! Player doesnt know where the ferry is.");
            return;
        }

        if (player.Ferry.Disembarking)
        {
            client.SendCommand(data.Username, "ferry_leave_failed", $"You're already disembarking the ferry.");
            return;
        }

        if (!player.Ferry.Active)
        {
            client.SendCommand(data.Username, "ferry_leave_failed", $"You're not on the ferry.");
            return;
        }

        player.Ferry.Disembark();        
    }
}
