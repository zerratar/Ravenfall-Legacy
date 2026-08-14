using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TownStats : ChatBotCommandHandler
{
    public TownStats(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var townHall = Game.Village.TownHall;
        var experience = Game.Village.TownHall.Experience;
        var level = Game.Village.TownHall.Level;
        var nextLevel = level + 1;
        var nextLevelExperience = GameMath.ExperienceForLevel(nextLevel);
        var remainingExp = nextLevelExperience - experience;
        client.SendReply(gm, "Village is level {townHallLevel}, it needs {remainingExp} xp to level up.", level, remainingExp);
    }
}

public class TownHouses : ChatBotCommandHandler
{
    public TownHouses(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var townHall = Game.Village.TownHall;

        var assignedPlayers = Game.Village.GetAssignedPlayersGroupedByType();

        var sBuilder = new StringBuilder();
        var variables = new List<object>();

        var typeIndex = 0;
        var playerNameIndex = 0;
        var playerStatsIndex = 0;
        foreach (var ap in assignedPlayers)
        {
            var type = ap.Key;
            var players = ap.Value;
            if (typeIndex > 0)
            {
                sBuilder.Append(", ");
            }
            sBuilder.Append("{type" + (typeIndex++) + "}: ");
            variables.Add(type.ToString());
            var skillType = GameMath.GetHouseSkillType(type);


            foreach (var player in players.OrderByDescending(x => x.GetSkill(skillType).Level))
            {
                if (playerNameIndex > 0)
                {
                    sBuilder.Append(", ");
                }

                var skill = player.GetSkill(skillType);
                var bonus = GameMath.CalculateHouseExpBonus(skill);
                if (bonus >= GameMath.MaxExpBonusPerSlot)
                {
                    sBuilder.Append("{playerName" + (playerNameIndex++) + "}");
                    variables.Add(player.Name);
                }
                else
                {
                    sBuilder.Append("{playerName" + (playerNameIndex++) + "} ({playerStats" + (playerStatsIndex++) + "})");
                    variables.Add(player.Name);
                    variables.Add(skill.Level);
                }
            }
        }
        if (sBuilder.Length > 0)
        {
            client.SendReply(gm, sBuilder.ToString(), variables.ToArray());
            return;
        }

        client.SendReply(gm, "No players assigned to houses.");
    }


}