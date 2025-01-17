﻿public class RaidCombatStyleSet : ChatBotCommandHandler<string>
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
            client.SendReply(gm, "You need to specify a combat skill to set. Example: !raid skill magic");
            return;
        }

        var targetSkill = SkillUtilities.ParseSkill(data);
        if (!targetSkill.IsCombatSkill())
        {
            client.SendReply(gm, "No such combat skill: {skill}", data);
            return;
        }

        var skillName = targetSkill.ToString();
        if (targetSkill == RavenNest.Models.Skill.Health)
        {
            skillName = "All";
        }

        player.raidHandler.SetSkill(targetSkill);
        client.SendReply(gm, "Your combat skill during raids has been set to {skill}.", skillName);
    }
}
