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

        if (!player.Ferry)
        {
            return;
        }

        if (player.Ferry.Embarking)
        {
            client.SendReply(gm, Localization.MSG_FERRY_ALREADY_WAITING);
            return;
        }
        if (player.Ferry.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_FERRY_ALREADY_ON);
            return;
        }

        player.Ferry.Embark();
        player.ClearTask();
        client.SendReply(gm, Localization.MSG_FERRY_TRAIN_SAIL);
    }
}
