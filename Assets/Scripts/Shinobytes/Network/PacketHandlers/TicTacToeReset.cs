public class TicTacToeReset : ChatBotCommandHandler
{
    public TicTacToeReset(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!Game.Tavern.IsActivated)
        {
            return;
        }

        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player != null)
        {
            if (player.IsModerator || player.IsGameAdmin || player.IsGameModerator || player.IsBroadcaster || Game.Tavern.TicTacToe.State == TavernGameState.GameOver)
                Game.Tavern.TicTacToe.ResetGame();
        }
    }
}

