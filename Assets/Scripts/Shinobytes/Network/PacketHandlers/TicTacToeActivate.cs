
public class TicTacToeActivate : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public TicTacToeActivate(
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
            if (Game.Tavern.TicTacToe.Started)
            {
                client.SendMessage(data, Localization.MSG_TICTACTOE_PLAY);
                return;
            }

            Game.Tavern.TicTacToe.Activate();
            client.SendMessage(data, Localization.MSG_TICTACTOE_STARTED);
        }
    }
}
