public class PetRacingPlay : PacketHandler<TwitchPlayerInfo>
{
    public PetRacingPlay(
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

        //if (!Game.Tavern.PetRacing.Started)
        //{
        //    Game.Tavern.PetRacing.Activate();
        //}

        //var player = PlayerManager.GetPlayer(data);
        //if (player == null)
        //{
        //    client.SendMessage(data, Localization.MSG_NOT_PLAYING);
        //    return;
        //}

        //Game.Tavern.PetRacing.Play(player);
    }
}

