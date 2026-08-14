using RavenNest.Models;
using System;

public partial class PlayerLogRequestEventHandler : GameEventHandler<PlayerLogRequest>
{
    public override void Handle(GameManager gameManager, PlayerLogRequest data)
    {
        try
        {
            byte[] content = Array.Empty<byte>();
            if (data.Type == PlayerLogRequestType.Current)
            {
                Shinobytes.Debug.Log($"[Server Request] Current in memory log requested by server.");
                // do not await, we dont want to block the game.
                content = Shinobytes.Debug.GetCurrentLogContentAsBytes();
                gameManager.RavenNest.UploadPlayerLogAsync(data.RequestId, content);
                return;
            }

            Shinobytes.Debug.Log($"[Server Request] {data.LogFile} Requested by server.");
            content = Shinobytes.Debug.GetLogFileContentAsBytes(data.LogFile);
            gameManager.RavenNest.UploadPlayerLogAsync(data.RequestId, content);
        }
        catch (Exception ex)
        {
            Shinobytes.Debug.LogError($"Failed to get player log: {ex.Message}");
        }
    }
}