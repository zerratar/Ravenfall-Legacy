public class FerryEnter : PacketHandler<Player>
{
    public FerryEnter(
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
            client.SendCommand(data.Username, "ferry_enter_failed", $"You are not currently playing. Type !join to start playing.");
            return;
        }

        if (!player.Ferry)
        {
            client.SendCommand(data.Username, "ferry_enter_failed", $"Uh oh! A bug! Player doesnt know where the ferry is.");
            return;
        }

        if (player.Ferry.Embarking)
        {
            client.SendCommand(data.Username, "ferry_enter_failed", $"You're already waiting for the ferry.");
            return;
        }
        if (player.Ferry.OnFerry)
        {
            client.SendCommand(data.Username, "ferry_enter_failed", $"You're already on the ferry.");
            return;
        }

        player.Ferry.Embark();
    }
}
