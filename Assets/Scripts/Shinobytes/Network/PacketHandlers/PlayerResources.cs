public class PlayerResources : ChatBotCommandHandler
{
    public PlayerResources(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var res = player.Resources;

        client.SendReply(gm, Localization.MSG_RESOURCES,
            Utility.FormatValue(res.Wood),
            Utility.FormatValue(res.Ore),
            Utility.FormatValue(res.Fish),
            Utility.FormatValue(res.Wheat),
            Utility.FormatValue(res.Coins));
    }
}