public class ToggleHelmetVisibility : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public ToggleHelmetVisibility(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(data);
        if (!targetPlayer)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.RavenNest.Players.ToggleHelmetAsync(targetPlayer.UserId);

        targetPlayer.Appearance.ToggleHelmVisibility();
    }
}