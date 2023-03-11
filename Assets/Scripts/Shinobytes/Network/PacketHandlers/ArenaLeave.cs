public class ArenaLeave : ChatBotCommandHandler<User>
{
    public ArenaLeave(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {

    }
}