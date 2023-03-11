
public class TicTacToeActivate : ChatBotCommandHandler
{
    public TicTacToeActivate(
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
            if (Game.Tavern.TicTacToe.Started)
            {
                client.SendReply(gm, Localization.MSG_TICTACTOE_PLAY);
                return;
            }

            Game.Tavern.TicTacToe.Activate();
            client.SendReply(gm, Localization.MSG_TICTACTOE_STARTED);
        }
    }
}
