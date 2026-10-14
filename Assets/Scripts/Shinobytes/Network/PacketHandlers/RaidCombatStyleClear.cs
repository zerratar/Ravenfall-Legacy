public class RaidCombatStyleClear : ChatBotCommandHandler
{
    public RaidCombatStyleClear(GameManager game, RavenBotConnection server, PlayerManager playerManager) 
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        player.raidHandler.SetSkill(null);
        client.SendReply(gm, Localization.MSG_RAID_SKILL_RESET);
    }
}
