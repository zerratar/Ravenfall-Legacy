public class DuelAccept : PacketHandler<Player>
{
    public DuelAccept(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);

        if (player.Ferry.OnFerry)
        {
            client.SendMessage(data.Username, Localization.MSG_DUEL_ACCEPT_FERRY);
            return;
        }

        if (player.Duel.InDuel)
        {
            client.SendMessage(data.Username, Localization.MSG_DUEL_ACCEPT_IN_DUEL);
            return;
        }

        if (player.Arena.InArena)
        {
            client.SendMessage(data.Username, Localization.MSG_DUEL_ACCEPT_IN_ARENA);
            return;
        }

        if (player.Raid.InRaid)
        {
            client.SendMessage(data.Username, Localization.MSG_DUEL_ACCEPT_IN_RAID);
            return;
        }

        if (!player.Duel.HasActiveRequest)
        {
            client.SendMessage(data.Username, Localization.MSG_DUEL_ACCEPT_NO_REQ);
            return;
        }

        player.Duel.AcceptDuel();
    }
}