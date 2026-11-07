using RavenNest.Models;
using UnityEngine;

public class ItemSellEventHandler : GameEventHandler<ItemTradeUpdate>
{
    public override void Handle(GameManager gameManager, ItemTradeUpdate data)
    {
        // nothing to change here: the server's coin update (ResourceUpdate) brings the new amount; changing it here too raced that update
        var player = gameManager.Players.GetPlayerById(data.SellerPlayerId);
        if (player)
        {
            Shinobytes.Debug.Log(player.Name + " sold for " + data.Cost + " coins");
        }
    }
}
