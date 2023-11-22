public class DuelDecline : ChatBotCommandHandler
{
    public DuelDecline(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);

        if (!player.duelHandler.HasActiveRequest)
        {
            return;
        }

        player.duelHandler.DeclineDuel();
    }
}