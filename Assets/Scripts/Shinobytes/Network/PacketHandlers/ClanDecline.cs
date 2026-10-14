public class ClanDecline : ChatBotCommandHandler<string>
{
    public ClanDecline(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var result = await Game.RavenNest.Clan.DeclineInviteAsync(plr.Id, data);
        if (result.Success)
        {
            client.SendReply(gm, Localization.MSG_CLAN_DECLINED);
            return;
        }

        client.SendReply(gm,
            result.ErrorMessage ??
            Localization.MSG_CLAN_DECLINE_FAILED);
        return;
    }
}
