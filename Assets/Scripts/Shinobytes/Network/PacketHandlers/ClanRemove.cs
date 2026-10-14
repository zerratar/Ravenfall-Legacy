public class ClanRemove : ChatBotCommandHandler<User>
{
    public ClanRemove(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (await Game.RavenNest.Clan.RemoveMemberAsync(plr.Id, otherPlayer.Id))
        {
            otherPlayer.clanHandler.Leave();
            client.SendReply(gm, Localization.MSG_CLAN_REMOVED, data.Username);
            return;
        }

        client.SendReply(gm, Localization.MSG_CLAN_REMOVE_NO_PERMISSION, otherPlayer.Name);
    }
}
