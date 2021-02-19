public class DuelCancel : PacketHandler<Player>
{
    public DuelCancel(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
    }
}