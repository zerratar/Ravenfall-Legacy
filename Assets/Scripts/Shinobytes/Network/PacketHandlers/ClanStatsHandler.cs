using RavenNest.Models;
using Shinobytes.Linq;

public class ClanStatsHandler : ChatBotCommandHandler<string>
{
    public ClanStatsHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var clanData = await Game.RavenNest.Clan.GetClanStatsAsync(plr.Id);
        if (clanData == null)
        {
            client.SendReply(gm, Localization.MSG_CLAN_STATS_UNKNOWN_ERROR);
            return;
        }

        client.SendReply(gm, GenerateStringPresentation(clanData));
    }

    private string GenerateStringPresentation(ClanStats data)
    {
        var msg = $"Your clan is {data.Name} led by {data.OwnerName} and is currently level {data.Level}.";
        if (data.ClanSkills != null)
        {
            if (data.ClanSkills.Count == 1)
            {
                var ench = data.ClanSkills[0];
                return msg + $" Your clan has an enchanting level of {ench.Level}.";
            }

            var skills = string.Join(", ", data.ClanSkills.Select(x => x.Name + " level " + x.Level));

            return msg + $" Your clan has the following skills {skills}.";
        }
        return msg;
    }
}
