using RavenNest.Models;
using UnityEngine;

public class ResourceUpdateEventHandler : GameEventHandler<ResourceUpdate>
{
    protected override void Handle(GameManager gameManager, ResourceUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            if (gameManager.Players.TryGetPlayerName(data.UserId, out var name))
            {
                Shinobytes.Debug.LogWarning("Server sent resource update for player: " + name + " (" + data.UserId + ") but the player is no longer in the game.");
            }
            return;
        }

        //Debug.Log("Got resource update for player: " + player.Name);

        player.Resources.Fish = data.FishAmount;
        player.Resources.Ore = data.OreAmount;
        player.Resources.Wood = data.WoodAmount;
        player.Resources.Wheat = data.WheatAmount;
        player.Resources.Coins = data.CoinsAmount;
    }
}
