public class DuelAccept : PacketHandler<Player>
{
    public DuelAccept(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);

        if (player.Ferry.OnFerry)
        {
            client.SendCommand(data.Username, "duel_failed", $"You cannot accept a duel while on the ferry.");
            return;
        }

        if (player.Duel.InDuel)
        {
            client.SendCommand(data.Username, "duel_failed", "You cannot accept the duel of another player as you are already in a duel.");
            return;
        }

        if (player.Arena.InArena)
        {
            client.SendCommand(data.Username, "duel_failed", "You cannot accept the duel of another player as you are participating in the Arena.");
            return;
        }

        if (player.Raid.InRaid)
        {
            client.SendCommand(data.Username, "duel_failed", "You cannot accept the duel of another player as you are participating in a Raid.");
            return;
        }

        if (!player.Duel.HasActiveRequest)
        {
            client.SendCommand(data.Username, "duel_failed",
                "You do not have any pending duel requests to accept.");
            return;
        }

        player.Duel.AcceptDuel();
    }
}