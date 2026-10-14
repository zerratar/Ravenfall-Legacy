public class ClanInvite : ChatBotCommandHandler<User>
{
    public ClanInvite(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(User data, GameMessage gm, GameClient client)
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

        var otherPlayer = PlayerManager.GetPlayer(data);
        if (!otherPlayer)
        {
            client.SendReply(gm, Localization.MSG_CLAN_PLAYER_NOT_PLAYING, data.Username);
            return;
        }

        if (await Game.RavenNest.Clan.InvitePlayerAsync(plr.Id, otherPlayer.Id))
        {
            client.SendReply(gm, Localization.MSG_CLAN_INVITE_SENT, otherPlayer.Name, plr.clanHandler.ClanInfo.Name);
            return;
        }

        client.SendReply(gm, Localization.MSG_CLAN_INVITE_FAILED, otherPlayer.Name);
    }
}
