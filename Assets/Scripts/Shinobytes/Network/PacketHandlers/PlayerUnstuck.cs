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

        var result = player.Unstuck();
        if (!result)
        {
            client.SendReply(gm, "Unstuck command can only be used once per minute/character.");
        }
    }
}
