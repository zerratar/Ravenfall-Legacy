using RavenNest.Models;
using UnityEngine;

public class ItemSellEventHandler : GameEventHandler<ItemTradeUpdate>
{
    public override void Handle(GameManager gameManager, ItemTradeUpdate data)
    {
        var player = gameManager.Players.GetPlayerById(data.SellerPlayerId);
        if (player)
        {
            var coinsBefore = player.Resources.Coins;
            player.AddResource(Resource.Currency, data.Cost, false);
            var coinsChange = coinsBefore - player.Resources.Coins;
            Shinobytes.Debug.Log("Coins change after item sell: " + coinsChange);
        }
    }
}
