using System;
using System.Linq;

public class HighestSkill : PacketHandler<HighestSkillRequest>
{
    public HighestSkill(
        GameManager game,
        GameServer server,
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
                client.SendCommand(data.Player.Username, "highest_skill",
                    $"{highestAll.PlayerName} has the highest total level with {highestAll.Stats.LevelList.Sum()}.");
                return;
            }
        }

        if (skillName.Equals("combat", StringComparison.OrdinalIgnoreCase) || skillName.Equals("level", StringComparison.OrdinalIgnoreCase))
        {
            var highestAll = players.OrderByDescending(x => x.Stats.CombatLevel).FirstOrDefault();
            if (highestAll != null)
            {
                client.SendCommand(data.Player.Username, "highest_skill",
                    $"{highestAll.PlayerName} has the highest combat level with {highestAll.Stats.CombatLevel}.");
                return;
            }
        }

        if (Enum.TryParse<CombatSkill>(skillName, true, out var combat))
        {
            isCombatSkill = true;
        }

        if (Enum.TryParse<Skill>(skillName, true, out var skill))
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
                if (highest == null || highest.Experience < combatSkill.Experience)
                {
                    p = player;
                    highest = combatSkill;
                }
            }
            else if (!anySkill)
            {
                var secondary = player.GetSkill(skill);
                if (highest == null || highest.Experience < secondary.Experience)
                {
                    p = player;
                    highest = secondary;
                }
            }
        }

        if (p == null || highest == null)
        {
            client.SendCommand(data.Player.Username,
                "highest_skill",
                $"No player could be found that has a skill named {skillName}");
            return;
        }

        client.SendCommand(data.Player.Username,
            "highest_skill",
            $"{p.PlayerName} has the highest level {skillName} with level {highest.Level}.");
    }
}