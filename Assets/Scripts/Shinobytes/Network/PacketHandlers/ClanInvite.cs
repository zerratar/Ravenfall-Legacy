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

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var otherPlayer = PlayerManager.GetPlayer(data);
        if (!otherPlayer)
        {
            client.SendReply(gm, "No player with name " + data.Username + " is currently playing.");
            return;
        }

        if (await Game.RavenNest.Clan.InvitePlayerAsync(plr.Id, otherPlayer.Id))
        {
            client.SendReply(gm, $"Invite was successfully sent. {otherPlayer.Name}, you may use the command '!clan accept' if you wish to join the clan {plr.Clan.ClanInfo.Name}. Or '!clan decline' to decline.");
            return;
        }

        client.SendReply(gm, otherPlayer.Name + " could not be invited right now.");
    }
}
