public class PlayerResources : PacketHandler<Player>
{
    public PlayerResources(
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
            client.SendCommand(data.Username, "player_resources", "You are not currently playing. Use !join to start playing!");
            return;
        }

        var res = player.Resources;

        client.SendCommand(data.Username,
            "player_resources",
            $"Wood {Utility.FormatValue(res.Wood)}, " +
            $"Ore {Utility.FormatValue(res.Ore)}, " +
            $"Fish {Utility.FormatValue(res.Fish)}, " +
            $"Wheat {Utility.FormatValue(res.Wheat)}, " +
            $"Coin {Utility.FormatValue(res.Coins)}");
    }
}