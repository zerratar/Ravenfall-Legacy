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

        client.SendReply(gm, "Your dungeon skill is set to: {skillName}", player.DungeonCombatStyle);
    }
}