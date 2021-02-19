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
            client.SendFormat(data.Player.DisplayName, "Player is not currently in the game.");
            return;
        }

        player.SetScale(data.Scale);
    }
}
