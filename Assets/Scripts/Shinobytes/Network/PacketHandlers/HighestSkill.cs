using Shinobytes.Linq;
using System;
public class HighestSkill : PacketHandler<HighestSkillRequest>
{
    public HighestSkill(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(HighestSkillRequest data, GameClient client)
    {
        var players = PlayerManager.GetAllPlayers();
        if (players.Count == 0)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_SKILL_NO_PLAYERS);
            return;
        }

        var skillName = data.Skill;
        if (skillName.Equals("all", StringComparison.OrdinalIgnoreCase) || skillName.Equals("overall", StringComparison.OrdinalIgnoreCase))
        {
            var highestAll = players.Highest(x => x.Stats.GetLevelList().Sum());
            if (highestAll != null)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_TOTAL,
                    highestAll.PlayerName,
                    highestAll.Stats.GetLevelList().Sum());
                return;
            }
        }

        if (skillName.Equals("combat", StringComparison.OrdinalIgnoreCase) || skillName.Equals("level", StringComparison.OrdinalIgnoreCase))
        {
            var highestAll = players.Highest(x => x.Stats.CombatLevel);
            if (highestAll != null)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_COMBAT,
                    highestAll.PlayerName,
                    highestAll.Stats.CombatLevel);
                return;
            }
        }

        var s = SkillUtilities.ParseSkill(skillName);
        if (s == Skill.None) return;

        // get the player with highest exp on any skill.
        SkillStat highest = null;
        PlayerController p = null;
        foreach (var player in players)
        {
            var skill = player.GetSkill(s);
            if (highest == null || (highest.Level <= skill.Level && highest.Experience < skill.Experience))
            {
                p = player;
                highest = skill;
            }
        }

        if (p == null || highest == null)
        {
            return;
        }

        client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_SKILL,
            p.PlayerName,
            skillName,
            highest.Level);
    }
}