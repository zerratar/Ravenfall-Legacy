using RavenNest.Models;
using UnityEngine;

public class PlayerAddEventHandler : GameEventHandler<PlayerAdd>
{
    protected override async void Handle(GameManager gameManager, PlayerAdd data)
    {
        if (gameManager.Players.Contains(data.UserId))
        {
            var existing = gameManager.Players.GetPlayerByUserId(data.UserId);
            if (existing != null)
            {
                gameManager.RemovePlayer(existing, false);
            }
            //return;
        }

        var playerInfo = await gameManager.RavenNest.PlayerJoinAsync(
            new PlayerJoinData
            {
                UserId = data.UserId,
                UserName = data.UserName,
                Identifier = data.Identifier,
                CharacterId = data.CharacterId
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

        gameManager.SpawnPlayer(playerInfo.Player);
        gameManager.SavePlayerStates();

        Shinobytes.Debug.Log($"PlayerAddEventHandler " + data.UserId + ", " + data.UserName);
    }
}

