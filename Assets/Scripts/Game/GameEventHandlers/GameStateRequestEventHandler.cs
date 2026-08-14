using RavenNest.Models;
using System;

public class GameStateRequestEventHandler : GameEventHandler<GameStateRequest>
{
    public override void Handle(GameManager gameManager, GameStateRequest data)
    {
        Shinobytes.Debug.Log($"[Server Request] Game state requested by server.");

        var dc = gameManager.DeltaClient;
        if (!dc)
        {
            return;
        }
        try
        {

            var content = dc.SaveStateToDisk();
            gameManager.RavenNest.UploadStateDataAsync(data.RequestId, content);
        }
        catch (Exception ex)
        {
            Shinobytes.Debug.LogError($"Failed to save game state: {ex.Message}");
        }
    }
}