public class PlayerCount : PacketHandler<Player>
{
    public PlayerCount(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(Player data, GameClient client)
    {
        var playerCount = PlayerManager.GetPlayerCount();
        client.SendFormat(data.Username, Localization.MSG_PLAYERS_ONLINE, playerCount);
    }
}
