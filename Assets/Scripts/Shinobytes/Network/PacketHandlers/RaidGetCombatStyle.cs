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

        client.SendReply(gm, "Your raid skill is set to: {skillName}", player.RaidCombatStyle);
    }
}
