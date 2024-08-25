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

        player.dungeonHandler.SetCombatStyle(null);
        client.SendReply(gm, "The skill used for dungeons has been reset.");
    }
}
