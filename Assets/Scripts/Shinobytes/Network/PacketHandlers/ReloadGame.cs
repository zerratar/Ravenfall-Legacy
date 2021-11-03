
public class ReloadGame : PacketHandler<TwitchPlayerInfo>
{
    public ReloadGame(
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

        var canReloadGame = player.IsGameAdmin || player.IsGameModerator || player.IsModerator || player.IsBroadcaster;
        if (!canReloadGame)
        {
            return;
        }

        Game.ReloadGame();
    }
}

public class RestartGame : PacketHandler<TwitchPlayerInfo>
{
    public RestartGame(
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

        var canReloadGame = player.IsGameAdmin || player.IsGameModerator || player.IsModerator || player.IsBroadcaster;
        if (!canReloadGame)
        {
            return;
        }

        Game.SaveStateAndShutdownGame();
    }
}
