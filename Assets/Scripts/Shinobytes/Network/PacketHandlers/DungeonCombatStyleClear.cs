public class DungeonCombatStyleClear : ChatBotCommandHandler<string>
{
    public DungeonCombatStyleClear(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        player.dungeonHandler.SetCombatStyle(null);
        client.SendReply(gm, "Your combat style has been cleared.");
    }
}
