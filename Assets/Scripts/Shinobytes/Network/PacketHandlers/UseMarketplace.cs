using RavenNest.Models;
using Shinobytes.Linq;
using System;
using System.Threading.Tasks;

public class UseMarketplace : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public UseMarketplace(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        if (string.IsNullOrEmpty(data))
        {
            client.SendReply(gm, Localization.MSG_MARKET_MISSING_ARGS);
            return;
        }

        var d = data.Trim();
        var parts = d.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        // we expect first part to be an action.

        if (parts.Length == 0)
        {
            client.SendReply(gm, Localization.MSG_MARKET_MISSING_ARGS);
            return;
        }

        var query = string.Join(" ", parts[1..]);
        switch (parts[0].ToLower())
        {
            case "value":
            case "stock":
                // show market value, lowest selling one, highest selling one, and average selling price.
                // and the total amount on the market. if amount is supplied, the price for buying said amount
                // with the lowest price will be calculated
                await GetMarketValueAsync(query, gm, client);
                break;

            case "buy":
                await BuyFromMarketAsync(query, gm, client);
                break;

            case "sell":
                await SellToMarketAsync(query, gm, client);
                break;

            default:
                client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ACTION, query);
                break;
        }
    }

    private async Task BuyFromMarketAsync(string query, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false);

        if (!ValidateItem(query, gm, client, item))
        {
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
                    var msg = "";
                    var arg0 = "";
                    var arg1 = "";
                    var arg2 = "";

                    if (result.TotalAmount > 1)
                    {
                        arg0 = result.TotalAmount.ToString();
                        arg1 = item.Item.Name;
                        arg2 = Utility.FormatAmount(result.TotalCost);
                        msg = "{totalAmount}x ";
                    }
                    else
                    {
                        arg0 = item.Item.Name;
                        arg1 = Utility.FormatAmount(result.TotalCost);
                    }

                    msg += "{itemName} was bought ";

                    if (player.EquipIfBetter(instance))
                    {
                        msg += "and equipped for {price} coins.";
                    }
                    else
                    {
                        msg += "for {price} coins.";
                    }

                    client.SendReply(gm, msg, arg0, arg1, arg2);
                    break;
            }
        }
        catch (Exception err)
        {
            Shinobytes.Debug.LogError("BuyFromMarketAsync threw an exception: " + err);
            client.SendReply(gm, Localization.MSG_BUY_ITEM_ERROR, item.Item.Name);
        }
        return;
    }

    private async Task SellToMarketAsync(string query, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        var item = itemResolver.ResolveTradeQuery(query, playerToSearch: player);
        if (!ValidateItem(query, gm, client, item))
        {
            return;
        }

        if (item.Count >= int.MaxValue)
        {
            client.SendReply(gm, Localization.MSG_SELL_TOO_MANY,
                item.Count,
                item.Item.Name,
                long.MaxValue);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendReply(player, Localization.MSG_SELL_ITEM_NOT_OWNED, item.Item.Name);
            return;
        }

        if (item.InventoryItem?.Soulbound ?? false)
        {
            client.SendReply(gm, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
            return;
        }
        try
        {
            var itemAmount = item.Count;
            var pricePerItem = item.Price;
            var sellResult = await Game.RavenNest.Marketplace.SellItemAsync(player.Id, item.Id, (long)itemAmount, (long)pricePerItem);
            if (sellResult == null)
            {
                client.SendReply(gm, Localization.MSG_SELL_MARKETPLACE_ERROR);
                return;
            }

            switch (sellResult.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendReply(gm, Localization.MSG_SELL_ITEM_NOT_FOUND, query);
                    break;
                case ItemTradeState.DoesNotOwn:
                    client.SendReply(gm, Localization.MSG_SELL_ITEM_NOT_OWNED, item.Item.Name);
                    break;
                case ItemTradeState.Failed:
                    client.SendReply(gm, Localization.MSG_SELL_MARKETPLACE_ERROR);
                    break;
                case ItemTradeState.Success:
                    player.Inventory.Remove(item.InventoryItem, item.Count);
                    client.SendReply(gm, Localization.MSG_SELL,
                        item.Count.ToString(),
                        item.Item.Name,
                        Utility.FormatAmount(item.Price));
                    break;
            }
        }
        catch (Exception err)
        {
            Shinobytes.Debug.LogError("SellItem threw an exception: " + err);
            client.SendReply(gm, Localization.MSG_SELL_ITEM_ERROR, item.Item.Name);
        }
    }

    private async Task GetMarketValueAsync(string query, GameMessage gm, GameClient client)
    {
        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false);
        if (!ValidateItem(query, gm, client, item))
        {
            return;
        }

        try
        {
            var marketValue = await Game.RavenNest.Marketplace.GetMarketValueAsync(item.Item.Id, item.Count);
            if (marketValue.AvailableAmount == 0)
            {
                client.SendReply(gm, Localization.MSG_MARKET_ITEM_UNAVAILABLE, item.Item.Name);
                return;
            }

            var cost = marketValue.CostForAmount;

            if (item.Count > 1)
            {
                if (marketValue.AvailableAmount < item.Count)
                {
                    var missingCount = item.Count - marketValue.AvailableAmount;
                    cost += marketValue.AvgPrice * missingCount;
                }

                client.SendReply(gm, Localization.MSG_MARKET_VALUE_COUNT,
                        marketValue.AvailableAmount, item.Item.Name,
                        Utility.FormatAmount(marketValue.MinPrice),
                        Utility.FormatAmount(marketValue.MaxPrice),
                        Utility.FormatAmount(marketValue.AvgPrice), item.Count,
                        Utility.FormatAmount(cost)
                    );
                return;
            }

            client.SendReply(gm, Localization.MSG_MARKET_VALUE,
                marketValue.AvailableAmount, item.Item.Name,
                Utility.FormatAmount(marketValue.MinPrice),
                Utility.FormatAmount(marketValue.MaxPrice),
                Utility.FormatAmount(marketValue.AvgPrice));
        }
        catch (Exception err)
        {
            Shinobytes.Debug.LogError("GetMarketValueAsync threw an exception: " + err);
            client.SendReply(gm, Localization.MSG_VALUE_ITEM_ERROR, item.Item.Name);
        }
    }


    private static bool ValidateItem(string query, GameMessage gm, GameClient client, ItemResolveResult item)
    {
        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND_SUGGEST, query,
                string.Join(", ", item.SuggestedItemNames));
            return false;
        }

        if (item.Item == null)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND, query);
            return false;
        }

        return true;
    }
}
