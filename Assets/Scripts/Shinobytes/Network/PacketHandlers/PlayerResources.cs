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

        client.SendReply(gm, Localization.MSG_PLAYER_RESOURCES,
            Utility.FormatAmount(res.Wood),
            Utility.FormatAmount(res.Ore),
            Utility.FormatAmount(res.Fish),
            Utility.FormatAmount(res.Wheat),
            Utility.FormatAmount(res.Coins));
    }
}