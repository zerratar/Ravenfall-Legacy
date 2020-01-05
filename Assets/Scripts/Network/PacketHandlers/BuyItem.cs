using System;
using System.Linq;
using RavenNest.Models;

public class BuyItem : PacketHandler<TradeItemRequest>
{
    public BuyItem(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(
                data.Player.Username, "item_trade_result",
                "You have to play the game to buy items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendCommand(player.PlayerName, "item_trade_result", "Unable to buy items right now. Unknown error");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendCommand(player.PlayerName, "message", "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }
        try
        {
            var result = await Game.RavenNest.Marketplace.BuyItemAsync(player.UserId, item.Item.Id, item.Amount, item.PricePerItem);

            switch (result.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendCommand(player.PlayerName, "item_trade_result", $"Could not find any {item.Item.Name} in the marketplace.");
                    break;
                case ItemTradeState.InsufficientCoins:
                    client.SendCommand(player.PlayerName, "item_trade_result", $"You do not have enough coins to buy the {item.Item.Name}.");
                    break;

                case ItemTradeState.Failed:
                    client.SendCommand(player.PlayerName, "item_trade_result", $"Error accessing marketplace right now.");
                    break;

                case ItemTradeState.RequestToLow:

                    var cheapest = result.CostPerItem
                        .OrderBy(x => x)
                        .FirstOrDefault();

                    client.SendCommand(player.PlayerName, "item_trade_result", $"Unable to buy any {item.Item.Name}, the cheapest asking price is {Utility.FormatValue(cheapest)}.");
                    break;

                case ItemTradeState.Success:
                    player.Inventory.Add(item.Item, item.Amount);
                    var amountStr = "";
                    if (result.TotalAmount > 1) amountStr = $"{result.TotalAmount}x ";
                    client.SendCommand(player.PlayerName,
                        "item_trade_result",
                        player.EquipIfBetter(item.Item)
                            ? $"{amountStr}{item.Item.Name} was bought and equipped for {Utility.FormatValue(result.TotalCost)} coins."
                            : $"{amountStr}{item.Item.Name} was bought for {Utility.FormatValue(result.TotalCost)} coins.");
                    break;
            }

        }
        catch (Exception exc)
        {
            client.SendCommand(player.PlayerName, "item_trade_result", $"Error buying {item.Item.Name}. Server returned an error. :( Try !leave and then !join to see if buying it was successeful or not.");
        }
    }
}