public class FerryEnter : ChatBotCommandHandler
{
    public FerryEnter(
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
        {
            return;
        }

        if (player.ferryHandler.Embarking)
        {
            client.SendReply(gm, Localization.MSG_FERRY_ALREADY_WAITING);
            return;
        }
        if (player.ferryHandler.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_FERRY_ALREADY_ON);
            return;
        }

        player.ferryHandler.Embark();
        player.ClearTask();
        client.SendReply(gm, Localization.MSG_FERRY_TRAIN_SAIL);
    }
}
