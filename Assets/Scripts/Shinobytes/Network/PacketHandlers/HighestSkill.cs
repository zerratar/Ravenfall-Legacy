using System;
using System.Linq;
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
        var isCombatSkill = false;
        var anySkill = true;
        var skillName = data.Skill;

        if (skillName.Equals("all", StringComparison.OrdinalIgnoreCase) || skillName.Equals("overall", StringComparison.OrdinalIgnoreCase))
        {
            var highestAll = players.OrderByDescending(x => x.Stats.LevelList.Sum()).FirstOrDefault();
            if (highestAll != null)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_TOTAL,
                    highestAll.PlayerName,
                    highestAll.Stats.LevelList.Sum());
                return;
            }
        }

        if (skillName.Equals("combat", StringComparison.OrdinalIgnoreCase) || skillName.Equals("level", StringComparison.OrdinalIgnoreCase))
        {
            var highestAll = players.OrderByDescending(x => x.Stats.CombatLevel).FirstOrDefault();
            if (highestAll != null)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_HIGHEST_COMBAT,
                    highestAll.PlayerName,
                    highestAll.Stats.CombatLevel);
                return;
            }
        }

        if (Enum.TryParse<CombatSkill>(skillName, true, out var combat))
        {
            isCombatSkill = true;
        }

        if (Enum.TryParse<TaskSkill>(skillName, true, out var skill))
        {
            anySkill = false;
        }

        // get the player with highest exp on any skill.
        SkillStat highest = null;
        PlayerController p = null;
        foreach (var player in players)
        {
            if (isCombatSkill)
            {
                var combatSkill = player.GetCombatSkill(combat);
                if (highest == null || (highest.Level <= combatSkill.Level && highest.Experience < combatSkill.Experience))
                {
                    p = player;
                    highest = combatSkill;
                }
            }
            else if (!anySkill)
            {
                var secondary = player.GetSkill(skill);
                if (highest == null || (highest.Level <= secondary.Level && highest.Experience < secondary.Experience))
                {
                    p = player;
                    highest = secondary;
                }
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