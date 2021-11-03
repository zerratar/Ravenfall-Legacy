public class ArenaKick : PacketHandler<ArenaKickRequest>
{
    public ArenaKick(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(ArenaKickRequest data, GameClient client)
    {
    }
}