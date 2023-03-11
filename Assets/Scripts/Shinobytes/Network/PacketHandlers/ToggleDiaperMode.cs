public class ToggleDiaperMode : ChatBotCommandHandler
{
    public ToggleDiaperMode(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null || !player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        player.ToggleDiaperMode();
        client.SendReply(gm, player.IsDiaperModeEnabled ? Localization.MSG_DIAPER_ON : Localization.MSG_DIAPER_OFF);
    }
}
