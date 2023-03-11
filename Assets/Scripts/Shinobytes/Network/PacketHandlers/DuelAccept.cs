public class DuelAccept : ChatBotCommandHandler
{
    public DuelAccept(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);

        if (player.Ferry.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_FERRY);
            return;
        }

        if (player.Duel.InDuel)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_DUEL);
            return;
        }

        if (player.Arena.InArena)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_ARENA);
            return;
        }

        if (player.Raid.InRaid)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_RAID);
            return;
        }

        if (!player.Duel.HasActiveRequest)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_NO_REQ);
            return;
        }

        player.Duel.AcceptDuel();
    }
}