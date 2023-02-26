using RavenNest.Models;

public class SellItem : ChatBotCommandHandler<TradeItemRequest>
{
    public SellItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        try
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
            
            if (item.SuggestedItemNames.Length > 0)
            {
                client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", item.SuggestedItemNames));
                return;
            }

            if (item.Item == null)
            {
                client.SendMessage(player.PlayerName, Localization.MSG_SELL_ITEM_NOT_FOUND, data.ItemQuery);
                return;
            }

            if (item.Count >= long.MaxValue)
            {
                client.SendFormat(player.PlayerName, Localization.MSG_SELL_TOO_MANY,
                    item.Count,
                    item.Item.Name,
                    long.MaxValue);
                return;
            }

            var itemAmount = item.Count;
            var pricePerItem = item.Price;

            if (itemAmount > long.MaxValue)
            {
                itemAmount = long.MaxValue;
            }

            if (pricePerItem > long.MaxValue)
            {
                pricePerItem = long.MaxValue;
            }

            if (item.Item?.Soulbound ?? false)
            {
                client.SendMessage(player.PlayerName, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
                return;
            }

            var sellResult = await Game.RavenNest.Marketplace.SellItemAsync(player.UserId, item.Id, (long)itemAmount, (long)pricePerItem);

            if (sellResult == null)
            {
                client.SendMessage(player.PlayerName, Localization.MSG_SELL_MARKETPLACE_ERROR);
            }

            switch (sellResult.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendMessage(player.PlayerName, Localization.MSG_SELL_ITEM_NOT_FOUND, data.ItemQuery);
                    break;
                case ItemTradeState.DoesNotOwn:
                    client.SendMessage(player.PlayerName, Localization.MSG_SELL_ITEM_NOT_OWNED, item.Item.Name);
                    break;
                case ItemTradeState.Failed:
                    client.SendMessage(player.PlayerName, Localization.MSG_SELL_MARKETPLACE_ERROR);
                    break;
                case ItemTradeState.Success:
                    player.Inventory.Remove(item.InventoryItem, item.Count);
                    client.SendMessage(player.PlayerName, Localization.MSG_SELL,
                        item.Count.ToString(),
                        item.Item.Name,
                        Utility.FormatValue(item.Price));
                    break;
            }
        }
        catch (System.Exception err)
        {
            Shinobytes.Debug.LogError("SellItem threw an exception: " + err);
        }
    }
}
