using RavenNest.Models;
using Shinobytes.Linq;

public class ClanInfoHandler : ChatBotCommandHandler<string>
{
    public ClanInfoHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clanInfo = await Game.RavenNest.Clan.GetClanInfoAsync(plr.Id);
        if (clanInfo == null)
        {
            client.SendReply(gm, Localization.MSG_CLAN_INFO_UNKNOWN_ERROR);
            return;
        }

        client.SendReply(gm, GenerateStringPresentation(clanInfo));
    }

    private string GenerateStringPresentation(ClanInfo data)
    {
        var msg = $"Your clan is {data.Name} led by {data.OwnerName}. There are currently ";
        var totalMemberCount = data.Roles.Sum(x => x.MemberCount);
        msg += string.Join(", ", data.Roles.OrderByDescending(x => x.Level).Select(x =>
        {
            var str = x.MemberCount + " " + x.Name;
            if (x.MemberCount > 1) str += "s";
            return str;
        }));

        msg += ". A total of " + totalMemberCount + " members.";
        return msg;
    }
}
