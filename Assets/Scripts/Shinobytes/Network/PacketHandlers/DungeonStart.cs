public class DungeonStart : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public DungeonStart(
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
