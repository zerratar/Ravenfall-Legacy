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

        var playerInfo = await gameManager.RavenNest.PlayerJoinAsync(
            new PlayerJoinData
            {
                UserId = data.UserId,
                UserName = data.UserName,
                Identifier = "1",
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

        Debug.Log($"PlayerAddEventHandler " + data.UserId + ", " + data.UserName);
    }
}

