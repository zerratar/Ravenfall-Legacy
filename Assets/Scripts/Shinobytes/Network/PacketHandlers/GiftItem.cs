public class GiftItem : ChatBotCommandHandler<TradeItemRequest>
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

        if (item.Item?.Soulbound ?? false)
        {
            client.SendMessage(player, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
            return;
        }

        var amount = item.Amount;
        if (amount > long.MaxValue)
            amount = long.MaxValue;

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.UserId, item.Player.UserId, item.Id, (long)amount);
        if (giftCount > 0)
        {
            // Update game client with the changes
            // this is done locally to avoid sending additional data from server to client and visa versa.
            item.Player.Inventory.AddToBackpack(item.Item, item.Amount);
            player.Inventory.Remove(item.Item.Item, item.Amount, true);
            client.SendFormat(player.PlayerName, Localization.MSG_GIFT, giftCount, item.Item.Name, item.Player.PlayerName);
        }
        else
        {
            client.SendFormat(player.PlayerName, Localization.MSG_GIFT_ERROR, item.Amount, item.Item.Name, item.Player.PlayerName);
        }
    }
}
