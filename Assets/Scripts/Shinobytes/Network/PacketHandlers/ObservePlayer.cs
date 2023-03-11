public class ObservePlayer : ChatBotCommandHandler
{
    public ObservePlayer(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.Camera.ObservePlayer(player);
    }
}
