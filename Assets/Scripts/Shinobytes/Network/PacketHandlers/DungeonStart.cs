public class DungeonStart : ChatBotCommandHandler<User>
{
    public DungeonStart(
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
