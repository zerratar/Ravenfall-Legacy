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

        if (await Game.RavenNest.Clan.RemoveMemberAsync(plr.Id, otherPlayer.Id))
        {
            otherPlayer.Clan.Leave();
            client.SendReply(gm, data.Username + " have been removed from the clan.");
            return;
        }

        client.SendReply(gm, $"You don't have permission to remove {otherPlayer.Name} or the player is not a member of your clan.");
    }
}
