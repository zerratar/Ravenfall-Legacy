using RavenNest.Models;
using UnityEngine;

public class ItemSellEventHandler : GameEventHandler<ItemTradeUpdate>
{
    protected override void Handle(GameManager gameManager, ItemTradeUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.SellerId);
        if (player)
        {
            var coinsBefore = player.Resources.Coins;
            player.AddResource(Resource.Currency, data.Cost, false);
            var coinsChange = coinsBefore - player.Resources.Coins;
            Debug.Log("Coins change after item sell: " + coinsChange);
        }
    }
}
