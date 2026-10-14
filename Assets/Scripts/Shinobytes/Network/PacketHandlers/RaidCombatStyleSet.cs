public class RaidCombatStyleSet : ChatBotCommandHandler<string>
{
    public RaidCombatStyleSet(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
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
            client.SendReply(gm, Localization.MSG_RAID_SKILL_MISSING);
            return;
        }

        var targetSkill = SkillUtilities.ParseSkill(data);
        if (!targetSkill.IsCombatSkill())
        {
            client.SendReply(gm, Localization.MSG_COMBAT_SKILL_NOT_FOUND, data);
            return;
        }

        var skillName = Localization.SkillName(targetSkill);
        if (targetSkill == RavenNest.Models.Skill.Health || targetSkill == RavenNest.Models.Skill.Melee)
        {
            skillName = Localization.UI_SKILL_ALL;
        }

        player.raidHandler.SetSkill(targetSkill);
        client.SendReply(gm, Localization.MSG_RAID_SKILL_SET, skillName);
    }
}
