public class FerryEnter : PacketHandler<TwitchPlayerInfo>
{
    public FerryEnter(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!player.Ferry)
        {
            return;
        }

        if (player.Ferry.Embarking)
        {
            client.SendMessage(data.Username, Localization.MSG_FERRY_ALREADY_WAITING);
            return;
        }
        if (player.Ferry.OnFerry)
        {
            client.SendMessage(data.Username, Localization.MSG_FERRY_ALREADY_ON);
            return;
        }

        player.Ferry.Embark();
        client.SendMessage(data.Username, Localization.MSG_FERRY_TRAIN_SAIL);
    }
}
