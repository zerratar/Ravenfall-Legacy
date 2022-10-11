public class ObservePlayer : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public ObservePlayer(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.DisplayName, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.Camera.ObservePlayer(player);
    }
}
