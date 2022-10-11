public class TicTacToeReset : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public TicTacToeReset(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        if (!Game.Tavern.IsActivated)
        {
            return;
        }

        var player = PlayerManager.GetPlayer(data);
        if (player != null)
        {
            if (player.IsModerator || player.IsGameAdmin || player.IsGameModerator || player.IsBroadcaster || Game.Tavern.TicTacToe.State == TavernGameState.GameOver)
                Game.Tavern.TicTacToe.ResetGame();
        }
    }
}

