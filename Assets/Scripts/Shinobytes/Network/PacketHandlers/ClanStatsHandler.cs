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

        if (plr.clanHandler == null || !plr.clanHandler.InClan)
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
        var msg = new ReplyMessageBuilder();
        msg.Add(Localization.MSG_CLAN_STATS, data.Name, data.OwnerName, data.Level);
        if (data.ClanSkills != null)
        {
            if (data.ClanSkills.Count == 1)
            {
                var ench = data.ClanSkills[0];
                msg.Add(Localization.MSG_CLAN_STATS_ENCHANTING, ench.Level);
            }
            else
            {
                var skills = string.Join(", ", data.ClanSkills.Select(x =>
                    Localization.MSG_CLAN_STATS_SKILL.Replace("{skillName}", x.Name).Replace("{level}", x.Level.ToString())));

                msg.Add(Localization.MSG_CLAN_STATS_SKILLS, skills);
            }
        }

        client.SendReply(gm, msg.ToString(), msg.Arguments);
    }
}
