using RavenNest.Models;
using UnityEngine;

public class ResourceUpdateEventHandler : GameEventHandler<ResourceUpdate>
{
    protected override void Handle(GameManager gameManager, ResourceUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Debug.LogWarning("No player with userid " + data.UserId + " when updating resources.");
            return;
        }
        
        Debug.Log("Got resource update for player: " + player.Name);

        player.Resources.Fish = data.FishAmount;
        player.Resources.Ore = data.OreAmount;
        player.Resources.Wood = data.WoodAmount;
        player.Resources.Wheat = data.WheatAmount;
        player.Resources.Coins = data.CoinsAmount;
    }
}
