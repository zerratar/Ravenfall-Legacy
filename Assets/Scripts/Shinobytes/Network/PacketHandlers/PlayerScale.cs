public class PlayerScale : PacketHandler<SetScaleRequest>
{
    public PlayerScale(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(SetScaleRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.DisplayName, Localization.MSG_NOT_PLAYING);
            return;
        }

        player.SetScale(data.Scale);
    }
}
