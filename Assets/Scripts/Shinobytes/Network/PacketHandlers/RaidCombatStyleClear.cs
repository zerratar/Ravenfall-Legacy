public class RaidCombatStyleClear : ChatBotCommandHandler
{
    public RaidCombatStyleClear(GameManager game, RavenBotConnection server, PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        player.raidHandler.SetCombatStyle(null);
        client.SendReply(gm, "Your combat style has been cleared.");
    }
}
