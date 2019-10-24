public class PlayerCount : PacketHandler<Player>
{
    public PlayerCount(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var playerCount = PlayerManager.GetPlayerCount();
        client.SendCommand(data.Username, "message", $"There are currently {playerCount} players in the game.");
    }
}
