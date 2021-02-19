public class DuelDecline : PacketHandler<Player>
{
    public DuelDecline(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);

        if (!player.Duel.HasActiveRequest)
        {
            return;
        }

        player.Duel.DeclineDuel();
    }
}