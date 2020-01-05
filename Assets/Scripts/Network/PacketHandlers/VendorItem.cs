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
        if (item == null || item.Item == null)
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
