public class ClanJoin : ChatBotCommandHandler<string>
{
    public ClanJoin(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (plr.clanHandler != null && plr.clanHandler.InClan)
        {
            client.SendReply(gm, Localization.MSG_ALREADY_IN_CLAN);
            return;
        }

        var ownerUserId = Game.RavenNest.UserId;
        // the bot sends "-" for no name; a name it can't place was the streamer's clan without a word, now it says so
        data = data?.Trim();
        if (!string.IsNullOrEmpty(data) && data != "-")
        {
            var otherPlayer = PlayerManager.GetPlayerByName(data);
            if (otherPlayer != null && otherPlayer.clanHandler.InClan && otherPlayer.clanHandler.ClanInfo != null)
            {
                ownerUserId = otherPlayer.clanHandler.ClanInfo.OwnerUserId;
            }
            else
            {
                var clan = Game.Clans.GetByName(data);
                if (clan != null)
                {
                    ownerUserId = clan.OwnerUserId;
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_CLAN_NOT_FOUND, data);
                    return;
                }
            }
        }

        var result = await Game.RavenNest.Clan.JoinAsync(ownerUserId, plr.Id);
        if (result.Success)
        {
            plr.clanHandler.Join(result.Clan, result.Role);

            if (!string.IsNullOrEmpty(result.WelcomeMessage))
            {
                client.SendReply(gm,
                    result.WelcomeMessage
                    .Replace("{ClanName}", result.Clan.Name)
                    .Replace("{PlayerName}", plr.Name)
                    .Replace("{RoleName}", result.Role.Name)
                );
                return;
            }

            client.SendReply(gm, Localization.MSG_CLAN_JOINED, result.Clan.Name, result.Role.Name);
            return;
        }

        // clans that take members on request or by invite (RavenNest's join policy, the website's clan pages)
        if (result.RequestSent)
        {
            client.SendReply(gm, Localization.MSG_CLAN_JOIN_REQUESTED, result.ClanName);
            return;
        }

        if (result.Failure == RavenNest.Models.JoinClanFailure.InviteOnly)
        {
            client.SendReply(gm, Localization.MSG_CLAN_INVITE_ONLY, result.ClanName);
            return;
        }

        if (result.Failure == RavenNest.Models.JoinClanFailure.AlreadyInClan)
        {
            client.SendReply(gm, Localization.MSG_ALREADY_IN_CLAN);
            return;
        }

        client.SendReply(gm, Localization.MSG_CLAN_JOIN_FAILED);
    }
}
