public class DuelDecline : PacketHandler<Player>
{
    public DuelDecline(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);

        if (!player.Duel.HasActiveRequest)
        {
            client.SendCommand(data.Username, "duel_failed",
                "You do not have any pending duel requests to decline.");
            return;
        }

        player.Duel.DeclineDuel();
    }
}