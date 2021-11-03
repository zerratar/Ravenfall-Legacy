public class ToggleDiaperMode : PacketHandler<TwitchPlayerInfo>
{
    public ToggleDiaperMode(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (player == null || !player)
        {
            client.SendMessage(data, Localization.MSG_NOT_PLAYING);
            return;
        }

        player.ToggleDiaperMode();
        client.SendMessage(data, player.IsDiaperModeEnabled ? Localization.MSG_DIAPER_ON : Localization.MSG_DIAPER_OFF);
    }
}
