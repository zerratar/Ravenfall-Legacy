public class ConnectionPing : ChatBotCommandHandler<Empty>
{
    public ConnectionPing(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(Empty _, GameMessage gm, GameClient client)
    {
        client.SendPong(gm.CorrelationId);

        Game.RavenBotController.State = BotState.Ready;
    }
}

