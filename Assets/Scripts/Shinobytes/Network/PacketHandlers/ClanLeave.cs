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

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clan = plr.Clan;
        var result = await Game.RavenNest.Clan.LeaveAsync(plr.Id);
        if (result.Success)
        {
            client.SendReply(gm, $"You have departed your clan and are no longer a member of {clan.ClanInfo.Name}.");
            clan.Leave();
            return;
        }

        client.SendReply(gm,
            result.ErrorMessage ??
            "Unable to leave your clan right now, unknown error. Please try again later.");
    }
}
