public class ConnectionPing : PacketHandler<PlayerAndNumber>
{
    public ConnectionPing(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(PlayerAndNumber data, GameClient client)
    {
        var correlationId = data.Number;
        client.SendPong(correlationId);

        Game.RavenBotController.State = BotState.Ready;
    }
}

