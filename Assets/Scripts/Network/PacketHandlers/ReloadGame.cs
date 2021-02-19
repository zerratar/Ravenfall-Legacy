
public class ReloadGame : PacketHandler<Player>
{
    public ReloadGame(
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

        var canReloadGame = player.IsGameAdmin || player.IsGameModerator || player.IsModerator || player.IsBroadcaster;
        if (!canReloadGame)
        {
            return;
        }

        Game.ReloadGame();
    }
}
