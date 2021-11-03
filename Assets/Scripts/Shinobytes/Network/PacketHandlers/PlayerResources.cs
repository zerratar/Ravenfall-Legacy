public class PlayerResources : PacketHandler<TwitchPlayerInfo>
{
    public PlayerResources(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var res = player.Resources;

        client.SendMessage(data.Username, Localization.MSG_RESOURCES,
            Utility.FormatValue(res.Wood),
            Utility.FormatValue(res.Ore),
            Utility.FormatValue(res.Fish),
            Utility.FormatValue(res.Wheat),
            Utility.FormatValue(res.Coins));
    }
}