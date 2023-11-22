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
            client.SendReply(gm, "No player with name {playerName} is currently playing.", data.Username);
            return;
        }

        if (await Game.RavenNest.Clan.InvitePlayerAsync(plr.Id, otherPlayer.Id))
        {
            client.SendReply(gm, "Invite was successfully sent. {playerName}, you may use the command '!clan accept' if you wish to join the clan {clanName}. Or '!clan decline' to decline.", otherPlayer.Name, plr.clanHandler.ClanInfo.Name);
            return;
        }

        client.SendReply(gm, "{playerName} could not be invited right now.", otherPlayer.Name);
    }
}
