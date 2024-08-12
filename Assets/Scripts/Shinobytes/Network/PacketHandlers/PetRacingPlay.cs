public class PetRacingPlay : ChatBotCommandHandler
{
    public PetRacingPlay(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!Game.Tavern.PetRacing || !Game.Tavern.IsActivated)
        {
            return;
        }

        if (!Game.Tavern.PetRacing.Started)
        {
            Game.Tavern.PetRacing.Activate();
        }

        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.Tavern.PetRacing.Play(player);
    }
}

