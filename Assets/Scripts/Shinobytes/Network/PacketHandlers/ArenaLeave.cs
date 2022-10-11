public class ArenaLeave : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public ArenaLeave(
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