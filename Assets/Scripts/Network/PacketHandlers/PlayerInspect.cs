public class PlayerInspect : PacketHandler<Player>
{
    public PlayerInspect(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
         : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (player == null)
        {
            return;
        }

        client.SendMessage(data, Localization.MSG_PLAYER_INSPECT_URL, player.Id.ToString());
    }
}
