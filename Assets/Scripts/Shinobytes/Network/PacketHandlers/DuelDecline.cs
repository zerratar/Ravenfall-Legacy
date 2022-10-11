public class DuelDecline : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public DuelDecline(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);

        if (!player.Duel.HasActiveRequest)
        {
            return;
        }

        player.Duel.DeclineDuel();
    }
}