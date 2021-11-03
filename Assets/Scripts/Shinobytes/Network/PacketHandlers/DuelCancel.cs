public class DuelCancel : PacketHandler<TwitchPlayerInfo>
{
    public DuelCancel(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
    }
}