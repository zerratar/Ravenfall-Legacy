public class PlayerCount : PacketHandler<TwitchPlayerInfo>
{
    public PlayerCount(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var playerCount = PlayerManager.GetPlayerCount();
        client.SendFormat(data.Username, Localization.MSG_PLAYERS_ONLINE, playerCount);
    }
}
