public class ClanAccept : ChatBotCommandHandler<string>
{
    public ClanAccept(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var result = await Game.RavenNest.Clan.AcceptInviteAsync(plr.Id, data);
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

        client.SendReply(gm, "Unable to accept clan invite. Either you don't have any invites or server is bonkers.");
    }
}
