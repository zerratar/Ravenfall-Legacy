public class ArenaAdd : ChatBotCommandHandler<ArenaAddRequest>
{
    public ArenaAdd(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(ArenaAddRequest data, GameClient client)
    {
        
    }
}