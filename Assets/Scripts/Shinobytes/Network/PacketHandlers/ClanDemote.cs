public class ClanDemote : ChatBotCommandHandler<Arguments>
{
    public ClanDemote(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(Arguments data, GameMessage gm, GameClient client)
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

        var target = data.GetArg<User>(0);
        var otherPlayer = PlayerManager.GetPlayer(target);
        if (!otherPlayer)
        {
            client.SendReply(gm, Localization.MSG_CLAN_PLAYER_NOT_PLAYING, target.Username);
            return;
        }

        var value = data.GetArg<string>(1);
        var result = await Game.RavenNest.Clan.DemoteMemberAsync(plr.Id, otherPlayer.Id, value);
        if (result.Success)
        {
            otherPlayer.clanHandler.SetRole(result.NewRole);
            client.SendReply(gm, Localization.MSG_CLAN_DEMOTED, otherPlayer.Name, result.NewRole.Name);
            return;
        }

        client.SendReply(gm, Localization.MSG_CLAN_DEMOTE_NO_PERMISSION, otherPlayer.Name);
    }
}
