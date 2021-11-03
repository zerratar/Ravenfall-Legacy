public class GiftItem : PacketHandler<TradeItemRequest>
{
    public GiftItem(
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
        var item = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: true);

        if (item == null || item.Item == null)
        {
            client.SendMessage(player, Localization.MSG_GIFT_PLAYER_NOT_FOUND, data.ItemQuery);
            return;
        }

        if (item.Player == null)
        {
            client.SendMessage(player, Localization.MSG_GIFT_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var amount = item.Amount;
        if (amount > long.MaxValue)
            amount = long.MaxValue;

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.UserId, item.Player.UserId, item.Item.Id, (long)amount);
        if (giftCount > 0)
        {
            item.Player.Inventory.Add(item.Item, item.Amount);
            player.Inventory.Remove(item.Item, item.Amount, true);
            client.SendFormat(player.PlayerName, Localization.MSG_GIFT, giftCount, item.Item.Name, item.Player.PlayerName);
        }
        else
        {
            client.SendFormat(player.PlayerName, Localization.MSG_GIFT_ERROR, item.Amount, item.Item.Name, item.Player.PlayerName);
        }
    }
}
