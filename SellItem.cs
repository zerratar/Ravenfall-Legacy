using RavenNest.Models;

public class VendorItem : PacketHandler<TradeItemRequest>
{
    public VendorItem(
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
            client.SendMessage(
                data.Player, "You have to play the game to sell items to vendor. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to sell the item right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery, parsePrice: false);
        if (item == null || item.Player == null || item.Item == null)
        {
            client.SendMessage(player, "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        var vendorCount = await Game.RavenNest.Players.VendorItemAsync(player.UserId, item.Item.Id, (int)item.Amount);
        if (vendorCount > 0)
        {
            client.SendMessage(player, $"You sold {vendorCount}x {item.Item.Name} to the vendor for {Utility.FormatValue(item.Item.ShopSellPrice * vendorCount)} coins! PogChamp");
        }
        else
        {
            client.SendMessage(player, $"Error selling {item.Amount}x {item.Item.Name} to the vendor. FeelsBadMan");
        }
    }
}

public class GiftItem : PacketHandler<TradeItemRequest>
{
    public GiftItem(
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
            client.SendMessage(
                data.Player, "You have to play the game to gift items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to gift the item right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: true);
        if (item == null || item.Player == null || item.Item == null)
        {
            client.SendMessage(player, "Could not find an item or player matching the query '" + data.ItemQuery + "'");
            return;
        }

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.UserId, item.Player.UserId, item.Item.Id, (int)item.Amount);
        if (giftCount > 0)
        {
            client.SendMessage(player, $"You gifted {giftCount}x {item.Item.Name} to {item.Player.PlayerName}! PogChamp");
        }
        else
        {
            client.SendMessage(player, $"Error gifting {item.Amount}x {item.Item.Name} to {item.Player.PlayerName}. FeelsBadMan");
        }
    }
}

public class ValueItem : PacketHandler<TradeItemRequest>
{
    public ValueItem(
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
            client.SendMessage(
                data.Player, "You have to play the game to valuate items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to valuate the item right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false);
        if (item == null)
        {
            client.SendMessage(player, "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        client.SendMessage(player, $"{item.Item.Name} can be sold for {Utility.FormatValue(item.Item.ShopSellPrice)} coins in the !vendor");
    }
}

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