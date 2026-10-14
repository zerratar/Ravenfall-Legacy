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

        if (plr.clanHandler == null || !plr.clanHandler.InClan)
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

        var totalMemberCount = clanInfo.Roles.Sum(x => x.MemberCount);
        client.SendReply(gm, Localization.MSG_CLAN_INFO, clanInfo.Name, clanInfo.OwnerName, GetRoleList(clanInfo), totalMemberCount);
    }

    private string GetRoleList(ClanInfo data)
    {
        return string.Join(", ", data.Roles.OrderByDescending(x => x.Level).Select(x =>
        {
            var format = x.MemberCount > 1 ? Localization.MSG_CLAN_INFO_ROLE_MANY : Localization.MSG_CLAN_INFO_ROLE;
            return format.Replace("{count}", x.MemberCount.ToString()).Replace("{roleName}", x.Name);
        }));
    }
}
