public class TownResources : ChatBotCommandHandler
{
    public TownResources(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var res = Game.Village.TownHall;
        client.SendReply(gm, Localization.MSG_TOWN_RESOURCES,
            Utility.FormatAmount(res.Wood),
            Utility.FormatAmount(res.Ore),
            Utility.FormatAmount(res.Fish),
            Utility.FormatAmount(res.Wheat),
            Utility.FormatAmount(res.Coins));
    }
}
