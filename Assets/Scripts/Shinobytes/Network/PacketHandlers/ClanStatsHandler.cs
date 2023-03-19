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

        GenerateStringPresentation(client, gm, clanData);
    }

    private void GenerateStringPresentation(GameClient client, GameMessage gm, ClanStats data)
    {
        var msg = "Your clan is {clanName} led by {ownerName} and is currently level {clanLevel}.";
        if (data.ClanSkills != null)
        {
            if (data.ClanSkills.Count == 1)
            {
                var ench = data.ClanSkills[0];
                msg += $" Your clan has an enchanting level of {ench.Level}.";
            }
            else
            {
                var skills = string.Join(", ", data.ClanSkills.Select(x => x.Name + " level " + x.Level));

                msg += $" Your clan has the following skills {skills}.";
            }
        }

        client.SendReply(gm, msg, data.Name, data.OwnerName, data.Level);
    }
}
