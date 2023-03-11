public class ArenaAdd : ChatBotCommandHandler<User>
{
    public ArenaAdd(
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