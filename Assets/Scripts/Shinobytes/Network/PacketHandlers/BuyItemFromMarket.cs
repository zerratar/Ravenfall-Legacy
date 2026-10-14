using System;
using System.Linq;
using RavenNest.Models;

public class BuyItemFromMarket : ChatBotCommandHandler<string>
{
    public BuyItemFromMarket(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveTradeQuery(inputQuery);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        try
        {
            var amount = item.Count;
            if (amount > long.MaxValue)
                amount = long.MaxValue;

            var pricePerItem = item.Price;
            if (pricePerItem > long.MaxValue)
                pricePerItem = long.MaxValue;

            var result = await Game.RavenNest.Marketplace.BuyItemAsync(player.Id, item.Id, (long)amount, (long)pricePerItem);

            switch (result.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_IN_MARKET, item.Item.Name);
                    break;
                case ItemTradeState.InsufficientCoins:
                    client.SendReply(gm, Localization.MSG_BUY_ITEM_INSUFFICIENT_COIN, item.Item.Name);
                    break;

                case ItemTradeState.Failed:
                    client.SendReply(gm, Localization.MSG_BUY_ITEM_MARKETPLACE_ERROR);
                    break;

                case ItemTradeState.RequestToLow:

                    var cheapest = result.CostPerItem
                        .OrderBy(x => x)
                        .FirstOrDefault();

                    client.SendReply(gm, Localization.MSG_BUY_ITEM_TOO_LOW, item.Item.Name, Utility.FormatAmount(cheapest));
                    break;

                case ItemTradeState.Success:
                    var instance = player.Inventory.AddToBackpack(item.Item, item.Count);
                    var equipped = player.EquipIfBetter(instance);
                    var totalCost = Utility.FormatAmount(result.TotalCost);

                    if (result.TotalAmount > 1)
                    {
                        client.SendReply(gm, equipped ? Localization.MSG_MARKET_BOUGHT_MANY_EQUIPPED : Localization.MSG_MARKET_BOUGHT_MANY,
                            result.TotalAmount.ToString(), item.Item.Name, totalCost);
                    }
                    else
                    {
                        client.SendReply(gm, equipped ? Localization.MSG_MARKET_BOUGHT_EQUIPPED : Localization.MSG_MARKET_BOUGHT,
                            item.Item.Name, totalCost);
                    }
                    break;
            }

        }
        catch
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_ERROR, item.Item.Name);
        }
    }
}