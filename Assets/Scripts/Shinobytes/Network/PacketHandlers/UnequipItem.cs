public class UnequipItem : ChatBotCommandHandler<TradeItemRequest>
{
    public UnequipItem(
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
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (data.ItemQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            await player.UnequipAllItemsAsync();
            client.SendMessage(data.Player.Username, Localization.MSG_UNEQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }


        if (queriedItem.InventoryItem == null || !queriedItem.InventoryItem.InventoryItem.Equipped)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_EQUIPPED, queriedItem.Item.Name);
            return;
        }

        await player.UnequipAsync(queriedItem.InventoryItem);
        client.SendMessage(data.Player.Username, Localization.MSG_UNEQUIPPED, queriedItem.Item.Name);
    }
}