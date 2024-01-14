
public class UpdateGame : ChatBotCommandHandler
{
    public UpdateGame(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    private bool isFirstTime = true;

    public override void Handle(GameMessage gm, GameClient client)
    {
        var user = gm.Sender;
        if (user.IsGameAdministrator || user.IsGameModerator)
        {
            // check if there is a new update, otherwise no need to reload if this was by accident.
            if (!Game.NewUpdateAvailable && isFirstTime)
            {
                isFirstTime = false;
                client.SendReply(gm, "There are no new version available. If you want to forcibly update then use the command one more time.");
                return;
            }

            isFirstTime = true;
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

        // check if there is a new update, otherwise no need to reload if this was by accident.
        if (!Game.NewUpdateAvailable && isFirstTime)
        {
            isFirstTime = false;
            client.SendReply(gm, "There are no new version available. If you want to forcibly update then use the command one more time.");
            return;
        }

        isFirstTime = true;
        Game.UpdateGame();
    }
}
