using RavenNest.Models;
using UnityEngine;

public class ResourceUpdateEventHandler : GameEventHandler<ResourceUpdate>
{
    public override void Handle(GameManager gameManager, ResourceUpdate data)
    {
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player)
        {
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
