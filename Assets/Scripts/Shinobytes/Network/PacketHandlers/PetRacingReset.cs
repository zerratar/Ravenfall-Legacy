public class PetRacingReset : PacketHandler<TwitchPlayerInfo>
{
    public PetRacingReset(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        //return;

        //if (!Game.Tavern.IsActivated)
        //{
        //    return;
        //}

        //if (!Game.Tavern.PetRacing.IsGameOver)
        //{
        //    return;
        //}

        //var player = PlayerManager.GetPlayer(data);
        //if (player == null)
        //{
        //    client.SendMessage(data, Localization.MSG_NOT_PLAYING);
        //    return;
        //}

        //Game.Tavern.PetRacing.ResetGame();
    }
}

