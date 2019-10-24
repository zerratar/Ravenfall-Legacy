public class ToggleHelmetVisibility : PacketHandler<Player>
{
    public ToggleHelmetVisibility(
        GameManager game,
        GameServer server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(data);
        if (!targetPlayer)
        {
            client.SendCommand(data.Username, "toggle_helmet", "You are not currently playing. Use !join to start playing!");
            return;
        }

        targetPlayer.Appearance.ToggleHelmVisibility();
    }
}