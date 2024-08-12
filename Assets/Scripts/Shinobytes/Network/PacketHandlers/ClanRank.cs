using RavenNest.Models;

public class ClanRank : ChatBotCommandHandler<string>
{
    public ClanRank(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (plr.clanHandler == null || !plr.clanHandler.InClan || plr.clanHandler.Role == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var role = plr.clanHandler.Role;

        GenerateStringPresentation(client, gm, role);
    }

    private void GenerateStringPresentation(GameClient client, GameMessage gm, ClanRole data)
    {
        var msg = "Your clan role is Level {roleLevel} - {roleName}. You joined the clan {date}.";

        client.SendReply(gm, msg, data.Level, data.Name, data.Joined);
    }
}
