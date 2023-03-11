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

        targetPlayer.Appearance.ToggleHelmVisibility();
    }
}