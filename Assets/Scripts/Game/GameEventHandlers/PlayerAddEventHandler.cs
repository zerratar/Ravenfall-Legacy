using RavenNest.Models;
using UnityEngine;

public class PlayerAddEventHandler : GameEventHandler<PlayerAdd>
{
    protected override async void Handle(GameManager gameManager, PlayerAdd data)
    {
        if (gameManager.Players.Contains(data.UserId))
        {
            var existing = gameManager.Players.GetPlayerByUserId(data.UserId);
            if (existing == null)
            {
                UnityEngine.Debug.LogWarning("Unable to add player: " + data.UserName + " (" + data.UserId + "). A character of the same user is already in the game.");
                return;
            }

            gameManager.RemovePlayer(existing, false);

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
            return;
        }

        if (!playerInfo.Success)
        {
            return;
        }

        gameManager.SpawnPlayer(playerInfo.Player);
        gameManager.SavePlayerStates();

        GameManager.Log($"PlayerAddEventHandler " + data.UserId + ", " + data.UserName);
    }
}

