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

        if (item == null || item.Item == null)
        {
            client.SendMessage(player, "Could not find an item or player matching the query '" + data.ItemQuery + "'");
            return;
        }

        if (item.Player == null)
        {
            client.SendMessage(player, "Could not find a matching the query '" + data.ItemQuery + "'");
            return;
        }

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.UserId, item.Player.UserId, item.Item.Id, (int)item.Amount);
        if (giftCount > 0)
        {
            player.Inventory.Remove(item.Item, item.Amount, true);
            client.SendMessage(player, $"You gifted {giftCount}x {item.Item.Name} to {item.Player.PlayerName}! PogChamp");
        }
        else
        {
            client.SendMessage(player, $"Error gifting {item.Amount}x {item.Item.Name} to {item.Player.PlayerName}. FeelsBadMan");
        }
    }
}
