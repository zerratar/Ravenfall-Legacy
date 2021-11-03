using System;
using System.Linq;
using RavenNest.Models;

public class BuyItem : PacketHandler<TradeItemRequest>
{
    public BuyItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }
        try
        {
            var amount = item.Amount;
            if (amount > long.MaxValue)
                amount = long.MaxValue;

            var pricePerItem = item.PricePerItem;
            if (pricePerItem > long.MaxValue)
                pricePerItem = long.MaxValue;

            var result = await Game.RavenNest.Marketplace.BuyItemAsync(player.UserId, item.Item.Id, (long)amount, (long)pricePerItem);

            switch (result.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_NOT_IN_MARKET, item.Item.Name);
                    break;
                case ItemTradeState.InsufficientCoins:
                    client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_INSUFFICIENT_COIN, item.Item.Name);
                    break;

                case ItemTradeState.Failed:
                    client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_MARKETPLACE_ERROR);
                    break;

                case ItemTradeState.RequestToLow:

                    var cheapest = result.CostPerItem
                        .OrderBy(x => x)
                        .FirstOrDefault();

                    client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_TOO_LOW, item.Item.Name, Utility.FormatValue(cheapest));
                    break;

                case ItemTradeState.Success:
                    player.Inventory.Add(item.Item, item.Amount);
                    var msg = "";
                    var arg0 = "";
                    var arg1 = "";
                    var arg2 = "";

                    if (result.TotalAmount > 1)
                    {
                        arg0 = result.TotalAmount.ToString();
                        arg1 = item.Item.Name;
                        arg2 = Utility.FormatValue(result.TotalCost);
                        msg = "{totalAmount}x ";
                    }
                    else
                    {
                        arg0 = item.Item.Name;
                        arg1 = Utility.FormatValue(result.TotalCost);
                    }

                    msg += "{itemName} was bought ";

                    if (player.EquipIfBetter(item.Item))
                    {
                        msg += "and equipped for {price} coins.";
                    }
                    else
                    {
                        msg += "for {price} coins.";
                    }

                    client.SendMessage(player.PlayerName, msg, arg0, arg1, arg2);
                    break;
            }

        }
        catch
        {
            client.SendMessage(player.PlayerName, Localization.MSG_BUY_ITEM_ERROR, item.Item.Name);
        }
    }
}