using System;

public class PlayerLogUpload : ChatBotCommandHandler
{
    public PlayerLogUpload(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        if (!gm.Sender.IsGameAdministrator && !gm.Sender.IsGameModerator &&
            !gm.Sender.IsBroadcaster && !gm.Sender.IsModerator)
        {
            if (!TryGetPlayer(gm, client, out var player) || (!player.IsGameAdmin && !player.IsModerator && !player.IsBroadcaster && !player.IsGameModerator))
            {
                client.SendReply(gm, "Only the Broadcaster and Moderators can use this command.");
                return;
            }
        }

        try
        {
            byte[] content = Array.Empty<byte>();
            Shinobytes.Debug.Log($"[Bot Request] Uploading current in memory log to server.");
            // do not await, we dont want to block the game.
            content = Shinobytes.Debug.GetCurrentLogContentAsBytes();
            Game.RavenNest.UploadPlayerLogAsync(Guid.NewGuid(), content, msg =>
            {
                client.SendReply(gm, msg);
            });
            return;
        }
        catch (Exception ex)
        {
            Shinobytes.Debug.LogError($"Failed to get player log: {ex.Message}");
        }
    }
}
