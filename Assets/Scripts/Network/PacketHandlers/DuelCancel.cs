public class DuelCancel : PacketHandler<Player>
{
    public DuelCancel(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        client.SendCommand(data.Username, "duel_failed", "Duel has not been implemented yet.");
    }
}