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
        var item = itemResolver.Resolve(data.ItemQuery, parsePrice: false, playerToSearch: player);
        if (item == null || item.Item == null)
        {
            client.SendMessage(player, Localization.MSG_VENDOR_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var toVendor = item.Amount >= int.MaxValue ? int.MaxValue : (int)item.Amount;
        var vendorCount = await Game.RavenNest.Players.VendorItemAsync(player.UserId, item.Id, toVendor);
        if (vendorCount > 0)
        {
            client.SendFormat(player.PlayerName, Localization.MSG_VENDOR_ITEM,
                vendorCount,
                item.Item.Name,
                Utility.FormatValue(item.Item.Item.ShopSellPrice * vendorCount));
        }
        else
        {
            client.SendFormat(player.PlayerName, Localization.MSG_VENDOR_ITEM_FAILED,
                item.Amount,
                item.Item.Name);
        }
    }
}
