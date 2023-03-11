using RavenNest.Models;
using UnityEngine;

public class ItemBuyEventHandler : GameEventHandler<ItemTradeUpdate>
{
    public override void Handle(GameManager gameManager, ItemTradeUpdate data)
    {
        var player = gameManager.Players.GetPlayerById(data.BuyerPlayerId);
        if (player)
        {
            var coinsBefore = player.Resources.Coins;
            player.RemoveResource(Resource.Currency, data.Cost);
            var coinsChange = coinsBefore - player.Resources.Coins;
            Shinobytes.Debug.Log("Coins change after item buy: " + coinsChange);
        }
    }
}