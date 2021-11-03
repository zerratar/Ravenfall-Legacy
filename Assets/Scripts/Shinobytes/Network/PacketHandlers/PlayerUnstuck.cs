public class PlayerUnstuck : PacketHandler<TwitchPlayerInfo>
{
    public PlayerUnstuck(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            return;
        }

        player.Unstuck();
    }
}
