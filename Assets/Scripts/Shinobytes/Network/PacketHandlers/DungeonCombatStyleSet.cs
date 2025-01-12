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
            client.SendReply(gm, "You need to specify a combat skill to set. Example: !dungeon skill magic");
            return;
        }

        // try get the Skill from the data string
        // if its an invalid one, reply with "no such skill"
        var targetSkill = SkillUtilities.ParseSkill(data);
        if (!targetSkill.IsCombatSkill())
        {
            client.SendReply(gm, "No such combat skill: {skill}", data);
            return;
        }

        player.dungeonHandler.SetSkill(targetSkill);

        var skillName = targetSkill.ToString();
        if (targetSkill == RavenNest.Models.Skill.Health)
        {
            skillName = "All";
        }

        client.SendReply(gm, "Your combat skill during dungeons has been set to {skill}.", skillName);
    }
}
