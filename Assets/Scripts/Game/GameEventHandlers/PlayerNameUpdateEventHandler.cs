using RavenNest.Models;
using UnityEngine;

public class PlayerNameUpdateEventHandler : GameEventHandler<PlayerNameUpdate>
{
    protected override void Handle(GameManager gameManager, PlayerNameUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player) return;
        player.PlayerName = data.Name;
        GameManager.Log($"PlayerNameUpdateEventHandler " + data.UserId);
    }
}

