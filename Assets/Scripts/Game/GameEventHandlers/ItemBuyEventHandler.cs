using RavenNest.Models;
using UnityEngine;

public class ItemBuyEventHandler : GameEventHandler<ItemTradeUpdate>
{
    protected override void Handle(GameManager gameManager, ItemTradeUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.BuyerId);
        if (player)
        {
            var coinsBefore = player.Resources.Coins;
            player.RemoveResource(Resource.Currency, data.Cost);
            var coinsChange = coinsBefore - player.Resources.Coins;
            GameManager.Log("Coins change after item buy: " + coinsChange);
        }
    }
}