public class ToggleHelmetVisibility : ChatBotCommandHandler
{
    public ToggleHelmetVisibility(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(gm.Sender);
        if (!targetPlayer)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.RavenNest.Players.ToggleHelmetAsync(targetPlayer.Id);

        var appearance = targetPlayer.Appearance;
        appearance.ToggleHelmVisibility();

        // said back, as for !toggle cape: it gave no answer at all (playtest 2026-11-26)
        if (!appearance.HasHelmet)
        {
            client.SendReply(gm, appearance.HelmetVisible ? Localization.MSG_HELMET_NONE_SHOWN : Localization.MSG_HELMET_NONE_HIDDEN);
            return;
        }
        client.SendReply(gm, appearance.HelmetVisible ? Localization.MSG_HELMET_SHOWN : Localization.MSG_HELMET_HIDDEN);
    }
}