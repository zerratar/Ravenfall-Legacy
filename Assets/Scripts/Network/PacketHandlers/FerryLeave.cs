public class FerryLeave : PacketHandler<Player>
{
    public FerryLeave(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!player.Ferry)
            return;

        if (player.Ferry.Disembarking)
        {
            client.SendMessage(data.Username, Localization.MSG_DISEMBARK_ALREADY);
            return;
        }

        if (!player.Ferry.Active)
        {
            client.SendMessage(data.Username, Localization.MSG_DISEMBARK_FAIL);
            return;
        }

        player.Ferry.Disembark();        
    }
}
