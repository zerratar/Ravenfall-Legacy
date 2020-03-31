using RavenNest.Models;
using UnityEngine;

public class PlayerAddEventHandler : GameEventHandler<PlayerAdd>
{
    protected override async void Handle(GameManager gameManager, PlayerAdd data)
    {
        if (gameManager.Players.Contains(data.UserId))
        {
            return;
        }

        var playerInfo = await gameManager.RavenNest.PlayerJoinAsync(data.UserId, data.UserName);
        if (playerInfo == null)
        {
            return;
        }

        gameManager.SpawnPlayer(playerInfo);

        Debug.Log($"PlayerAddEventHandler " + data.UserId + ", " + data.UserName);
    }
}

