public class ObservePlayer : PacketHandler<Player>
{
    public ObservePlayer(
    GameManager game,
    GameServer server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            return;
        }

        Game.Camera.ObservePlayer(player);
    }
}
