
public class ReloadGame : ChatBotCommandHandler
{
    public ReloadGame(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
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

public class RestartGame : ChatBotCommandHandler
{
    public RestartGame(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
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
