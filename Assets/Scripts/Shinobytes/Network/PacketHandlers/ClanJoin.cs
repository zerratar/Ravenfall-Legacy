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
        if (!string.IsNullOrEmpty(data))
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

            client.SendReply(gm, "You have joined {clanName} as a {roleName}!", result.Clan.Name, result.Role.Name);
            return;
        }

        client.SendReply(gm, "You are not able to join that clan at this moment.");
    }
}
