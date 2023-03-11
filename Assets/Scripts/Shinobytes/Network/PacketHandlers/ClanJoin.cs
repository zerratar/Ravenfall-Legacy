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

        if (plr.Clan != null && plr.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_ALREADY_IN_CLAN);
            return;
        }

        var ownerUserId = Game.RavenNest.UserId;
        if (!string.IsNullOrEmpty(data))
        {
            var otherPlayer = PlayerManager.GetPlayerByName(data);
            if (otherPlayer != null && otherPlayer.Clan.InClan && otherPlayer.Clan.ClanInfo != null)
            {
                ownerUserId = otherPlayer.Clan.ClanInfo.OwnerUserId;
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
            plr.Clan.Join(result.Clan, result.Role);

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

            client.SendReply(gm, $"You have joined {result.Clan.Name} as a {result.Role.Name}!");
            return;
        }

        client.SendReply(gm, "You are not able to join that clan at this moment.");
    }
}
