public class DungeonCombatStyleSet : ChatBotCommandHandler<string>
{
    public DungeonCombatStyleSet(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (string.IsNullOrEmpty(data))
        {
            client.SendReply(gm, Localization.MSG_DUNGEON_SKILL_MISSING);
            return;
        }

        // try get the Skill from the data string
        // if its an invalid one, reply with "no such skill"
        var targetSkill = SkillUtilities.ParseSkill(data);
        if (!targetSkill.IsCombatSkill())
        {
            client.SendReply(gm, Localization.MSG_COMBAT_SKILL_NOT_FOUND, data);
            return;
        }

        player.dungeonHandler.SetSkill(targetSkill);

        var skillName = Localization.SkillName(targetSkill);
        if (targetSkill == RavenNest.Models.Skill.Health || targetSkill == RavenNest.Models.Skill.Melee)
        {
            skillName = Localization.UI_SKILL_ALL;
        }

        client.SendReply(gm, Localization.MSG_DUNGEON_SKILL_SET, skillName);
    }
}
