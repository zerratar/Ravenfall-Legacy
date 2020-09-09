
public class ReloadGame : PacketHandler<Player>
{
    public ReloadGame(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendCommand(data.Username, "message", "You are not currently playing. Use !join to start playing!");
            return;
        }

        var canReloadGame = player.IsGameAdmin || player.IsGameModerator || player.IsModerator || player.IsBroadcaster;
        if (!canReloadGame)
        {
            return;
        }

        //client.SendCommand(data.Username, "message", "Game is being reloaded.. Everyone please wait.");
        Game.ReloadGame();
    }
}
