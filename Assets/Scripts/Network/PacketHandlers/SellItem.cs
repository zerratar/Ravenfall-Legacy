using RavenNest.Models;

public class SellItem : PacketHandler<TradeItemRequest>
{
    public SellItem(
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
                "You have to play the game to sell items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendCommand(player.PlayerName, "item_trade_result", "Unable to sell items right now. Unknown error");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);

        if (item == null)
        {
            client.SendCommand(player.PlayerName, "item_trade_result", "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        var result = await Game.RavenNest.Marketplace.SellItemAsync(player.UserId, item.Item.Id, item.Amount, item.PricePerItem);
        switch (result.State)
        {
            case ItemTradeState.DoesNotExist:
                client.SendCommand(player.PlayerName, "item_trade_result", "Could not find an item matching the query '" + data.ItemQuery + "'");
                break;
            case ItemTradeState.DoesNotOwn:
                client.SendCommand(player.PlayerName, "item_trade_result", $"You do not have any {item.Item.Name} in your inventory.");
                break;
            case ItemTradeState.Failed:
                client.SendCommand(player.PlayerName, "item_trade_result", $"Error accessing marketplace right now.");
                break;
            case ItemTradeState.Success:
                player.Inventory.Remove(item.Item, item.Amount);
                client.SendCommand(player.PlayerName, "item_trade_result", $"{item.Amount}x {item.Item.Name} was put in the marketplace listing for {Utility.FormatValue(item.PricePerItem)} per item.");
                break;
        }
    }
}
