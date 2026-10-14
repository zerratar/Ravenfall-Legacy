public class DungeonCombatStyleClear : ChatBotCommandHandler
{
    public DungeonCombatStyleClear(GameManager game, RavenBotConnection server, PlayerManager playerManager) 
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        player.dungeonHandler.SetSkill(null);
        client.SendReply(gm, Localization.MSG_DUNGEON_SKILL_RESET);
    }
}
