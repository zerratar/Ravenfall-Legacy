public class RaidGetCombatStyle : ChatBotCommandHandler
{
    public RaidGetCombatStyle(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (player.RaidCombatStyle == null)
        {
            client.SendReply(gm, "Your raid skill is not set. Use !raid skill <skill> to set it.");
            return;
        }

        client.SendReply(gm, "Your raid skill is set to " + player.RaidCombatStyle);
    }
}
