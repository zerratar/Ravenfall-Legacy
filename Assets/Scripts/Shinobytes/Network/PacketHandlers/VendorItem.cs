public class VendorItem : ChatBotCommandHandler<TradeItemRequest>
{
    public VendorItem(
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
            client.SendMessage(data.Player, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveTradeQuery(data.ItemQuery, parsePrice: false, playerToSearch: player);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendMessage(player, Localization.MSG_VENDOR_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendMessage(player, Localization.MSG_VENDOR_ITEM_NOT_OWNED, item.Item.Name);
            return;
        }

        var toVendor = item.Count >= int.MaxValue ? int.MaxValue : (int)item.Count;
        var vendorCount = await Game.RavenNest.Players.VendorInventoryItemAsync(player.UserId, item.InventoryItem.InstanceId, toVendor);
        if (vendorCount > 0)
        {
            if (item.InventoryItem.InventoryItem.Equipped)
            {
                player.Inventory.Unequip(item.InventoryItem);
            }

             player.Inventory.Remove(item.InventoryItem, vendorCount); 
            
            client.SendFormat(player.PlayerName, Localization.MSG_VENDOR_ITEM,
                vendorCount,
                item.Item.Name,
                Utility.FormatValue(item.Item.ShopSellPrice * vendorCount));
        }
        else
        {
            client.SendFormat(player.PlayerName, Localization.MSG_VENDOR_ITEM_FAILED,
                item.Count,
                item.Item.Name);
        }
    }
}
