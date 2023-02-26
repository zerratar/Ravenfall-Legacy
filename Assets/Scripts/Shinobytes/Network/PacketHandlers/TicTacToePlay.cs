﻿public class TicTacToePlay : ChatBotCommandHandler<PlayerAndNumber>
{
    public TicTacToePlay(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }
    public override void Handle(PlayerAndNumber data, GameClient client)
    {        
        var player = PlayerManager.GetPlayer(data.Player);
        if (player == null)
        {
            client.SendMessage(data.Player, Localization.MSG_NOT_PLAYING);
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

        Game.Tavern.TicTacToe.Play(player, data.Number);
    }
}

