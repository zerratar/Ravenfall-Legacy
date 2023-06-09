using RavenNest.Models;
using UnityEngine;

public class PlayerAddEventHandler : GameEventHandler<PlayerAdd>
{
    public override async void Handle(GameManager gameManager, PlayerAdd data)
    {
        if (gameManager.Players.Contains(data.UserId))
        {
            var existing = gameManager.Players.GetPlayerByUserId(data.UserId);
            if (existing != null)
            {
                gameManager.RemovePlayer(existing, false);
            }
        }

        var playerInfo = await gameManager.RavenNest.PlayerJoinAsync(
            new PlayerJoinData
            {
                UserId = data.UserId,
                UserName = data.UserName,
                Identifier = data.Identifier,
                CharacterId = data.CharacterId,
                PlatformId = data.PlatformId,
                Platform = data.Platform,
            });

        if (playerInfo == null)
        {
            Shinobytes.Debug.LogError("Unable to add player: " + data.UserName + " " + (playerInfo != null ? playerInfo.ErrorMessage : ""));
            return;
        }

        if (!playerInfo.Success)
        {
            Shinobytes.Debug.LogError("Unable to add player: " + data.UserName + " " + (playerInfo != null ? playerInfo.ErrorMessage : ""));
            return;
        }

        var user = new User(playerInfo.Player, gameManager.RavenNest.UserId);
        user.Platform = data.Platform;
        user.PlatformId = data.PlatformId;

        gameManager.SpawnPlayer(playerInfo.Player, user);
        gameManager.SaveStateFile();

        Shinobytes.Debug.Log($"PlayerAddEventHandler " + data.UserId + ", " + data.UserName);
    }
}

