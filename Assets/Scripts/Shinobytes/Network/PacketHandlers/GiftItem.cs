public class GiftItem : ChatBotCommandHandler<string>
{
    public GiftItem(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false, parseUsername: true, parseAmount: true, playerToSearch: player);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.Player == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_PLAYER_NOT_FOUND, inputQuery);
            return;
        }

        if (item.Item?.Soulbound ?? false)
        {
            client.SendReply(player, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
            return;
        }

        var amount = item.Count;
        if (amount > long.MaxValue)
            amount = long.MaxValue;

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.Id, item.Player.Id, item.InventoryItem.InstanceId, (long)amount);
        if (giftCount > 0)
        {
            // Update game client with the changes
            // this is done locally to avoid sending additional data from server to client and visa versa.
            item.Player.Inventory.AddToBackpack(item.Item, item.Count);
            player.Inventory.Remove(item.InventoryItem, item.Count, true);
            client.SendReply(gm, Localization.MSG_GIFT, giftCount, item.Item.Name, item.Player.PlayerName);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_GIFT_ERROR, item.Count, item.Item.Name, item.Player.PlayerName);
        }
    }
}
