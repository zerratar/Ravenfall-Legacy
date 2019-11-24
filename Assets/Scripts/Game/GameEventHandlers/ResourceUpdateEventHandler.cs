using RavenNest.Models;
using UnityEngine;

public class ResourceUpdateEventHandler : GameEventHandler<ResourceUpdate>
{
    protected override void Handle(GameManager gameManager, ResourceUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Debug.Log("No player with userid " + data.UserId + " when updating resources.");
            return;
        }

        player.Resources.Fish = data.FishAmount;
        player.Resources.Ore = data.OreAmount;
        player.Resources.Wood = data.WoodAmount;
        player.Resources.Wheat = data.WheatAmount;
        player.Resources.Coins = data.CoinsAmount;
    }
}
