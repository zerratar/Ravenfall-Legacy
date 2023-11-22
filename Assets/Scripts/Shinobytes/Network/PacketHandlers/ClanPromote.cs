public class ClanPromote : ChatBotCommandHandler<Arguments>
{
    public ClanPromote(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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
        var targetPlayer = data.GetArg<User>(0);
        var otherPlayer = PlayerManager.GetPlayer(targetPlayer);
        if (!otherPlayer)
        {
            client.SendReply(gm, "No player with name " + targetPlayer.Username + " is currently playing.");
            return;
        }

        var value = data.GetArg<string>(1);
        var result = await Game.RavenNest.Clan.PromoteMemberAsync(plr.Id, otherPlayer.Id, value);
        if (result.Success)
        {
            otherPlayer.clanHandler.SetRole(result.NewRole);
            client.SendReply(gm, $"Congratulations {otherPlayer.Name}! You have been promoted to {result.NewRole.Name}!");
            return;
        }

        client.SendReply(gm, $"You don't have permission to promote {otherPlayer.Name} or the player is not a member of your clan.");

    }
}
