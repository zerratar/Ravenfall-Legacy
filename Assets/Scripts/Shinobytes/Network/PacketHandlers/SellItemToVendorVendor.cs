
public class SellItemToVendorVendor : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public SellItemToVendorVendor(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var item = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false, playerToSearch: player);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(player, Localization.MSG_VENDOR_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendReply(player, Localization.MSG_VENDOR_ITEM_NOT_OWNED, item.Item.Name);
            return;
        }

        var toVendor = item.Count >= int.MaxValue ? int.MaxValue : (int)item.Count;
        var vendorCount = await Game.RavenNest.Players.VendorInventoryItemAsync(player.Id, item.InventoryItem.InstanceId, toVendor);
        if (vendorCount > 0)
        {
            if (player.Inventory.IsEquipped(item.InventoryItem))
            {
                player.Inventory.Unequip(item.InventoryItem);
            }

            player.Inventory.Remove(item.InventoryItem, vendorCount);

            client.SendReply(gm, Localization.MSG_VENDOR_ITEM,
                vendorCount,
                item.Item.Name,
                Utility.FormatAmount(item.Item.ShopSellPrice * vendorCount));
        }
        else
        {
            client.SendReply(gm, Localization.MSG_VENDOR_ITEM_FAILED,
                item.Count,
                item.Item.Name);
        }
    }
}
