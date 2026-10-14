public class ClanLeave : ChatBotCommandHandler<string>
{
    public ClanLeave(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(gm.Sender);
        if (!plr)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.clanHandler == null || !plr.clanHandler.InClan)
        {
            client.SendReply(gm, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clan = plr.clanHandler;
        var result = await Game.RavenNest.Clan.LeaveAsync(plr.Id);
        if (result.Success)
        {
            client.SendReply(gm, Localization.MSG_CLAN_LEFT, clan.ClanInfo.Name);
            clan.Leave();
            return;
        }

        client.SendReply(gm,
            result.ErrorMessage ??
            Localization.MSG_CLAN_LEAVE_FAILED);
    }
}
