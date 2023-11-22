public class FerryLeave : ChatBotCommandHandler
{
    public FerryLeave(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!player.ferryHandler)
            return;

        if (player.ferryHandler.Disembarking)
        {
            client.SendReply(gm, Localization.MSG_DISEMBARK_ALREADY);
            return;
        }

        if (!player.ferryHandler.Active)
        {
            client.SendReply(gm, Localization.MSG_DISEMBARK_FAIL);
            return;
        }

        player.ferryHandler.BeginDisembark();
    }
}
