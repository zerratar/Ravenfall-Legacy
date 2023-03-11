public class TicTacToePlay : ChatBotCommandHandler<int>
{
    public TicTacToePlay(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }
    public override void Handle(int data, GameMessage gm, GameClient client)
    {        
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!Game.Tavern.IsActivated)
        {
            return;
        }

        if (!Game.Tavern.TicTacToe.Started)
        {
            Game.Tavern.TicTacToe.Activate();
        }

        Game.Tavern.TicTacToe.Play(player, data);
    }
}

