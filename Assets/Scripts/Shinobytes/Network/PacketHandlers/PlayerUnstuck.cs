public class PlayerUnstuck : ChatBotCommandHandler
{
    public PlayerUnstuck(
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
            return;
        }

        player.Unstuck();
    }
}
