
public class UpdateGame : ChatBotCommandHandler
{
    public UpdateGame(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var user = gm.Sender;
        if (user.IsGameAdministrator || user.IsGameModerator)
        {
            Game.UpdateGame();
            return;
        }

        if (!user.IsModerator && !user.IsBroadcaster)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                return;
            }
            if (!player.IsGameAdmin && !player.IsGameModerator)
            {
                return;
            }
        }

        Game.UpdateGame();
    }
}
