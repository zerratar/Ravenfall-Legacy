public class DungeonGetCombatStyle : ChatBotCommandHandler
{
    public DungeonGetCombatStyle(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (player.DungeonSkill == null)
        {
            client.SendReply(gm, Localization.MSG_DUNGEON_SKILL_NOT_SET);
            return;
        }

        client.SendReply(gm, Localization.MSG_DUNGEON_SKILL_CURRENT, Localization.SkillName(player.DungeonSkill.Value));
    }
}