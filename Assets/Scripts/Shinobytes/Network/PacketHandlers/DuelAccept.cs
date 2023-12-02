
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

        if (player.ferryHandler.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_FERRY);
            return;
        }

        if (player.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_DUEL);
            return;
        }

        if (player.arenaHandler.InArena)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_ARENA);
            return;
        }

        if (player.raidHandler.InRaid)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_IN_RAID);
            return;
        }

        if (!player.duelHandler.HasActiveRequest)
        {
            client.SendReply(gm, Localization.MSG_DUEL_ACCEPT_NO_REQ);
            return;
        }

        player.duelHandler.AcceptDuel();
    }
}