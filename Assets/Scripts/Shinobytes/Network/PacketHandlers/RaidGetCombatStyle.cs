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

        if (player.RaidSkill == null)
        {
            client.SendReply(gm, Localization.MSG_RAID_SKILL_NOT_SET);
            return;
        }

        client.SendReply(gm, Localization.MSG_RAID_SKILL_CURRENT, Localization.SkillName(player.RaidSkill.Value));
    }
}
