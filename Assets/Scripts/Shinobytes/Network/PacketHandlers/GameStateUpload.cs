using System;

public class GameStateUpload : ChatBotCommandHandler
{
    public GameStateUpload(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        Shinobytes.Debug.Log($"[Bot Request] Uploading game state to server.");

        if (!gm.Sender.IsGameAdministrator && !gm.Sender.IsGameModerator &&
            !gm.Sender.IsBroadcaster && !gm.Sender.IsModerator)
        {
            if (!TryGetPlayer(gm, client, out var player) || (!player.IsGameAdmin && !player.IsModerator && !player.IsBroadcaster && !player.IsGameModerator))
            {
                client.SendReply(gm, "Only the Broadcaster and Moderators can use this command.");
                return;
            }
        }

        var dc = Game.DeltaClient;
        if (!dc)
        {
            return;
        }
        try
        {
            var content = dc.SaveStateToDisk();
            Game.RavenNest.UploadStateDataAsync(Guid.NewGuid(), content,
                msg =>
                {
                    client.SendReply(gm, msg);
                });
        }
        catch (Exception ex)
        {
            Shinobytes.Debug.LogError($"Failed to save game state: {ex.Message}");
        }
    }
}
