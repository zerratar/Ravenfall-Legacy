public class ObservePlayer : PacketHandler<Player>
{
    public ObservePlayer(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendFormat(data.DisplayName, "Player is not currently in the game.");
            return;
        }

        Game.Camera.ObservePlayer(player);
    }
}
