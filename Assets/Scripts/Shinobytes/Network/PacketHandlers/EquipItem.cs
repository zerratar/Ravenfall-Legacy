public class EquipItem : ChatBotCommandHandler<TradeItemRequest>
{
    public EquipItem(
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

        if (string.IsNullOrEmpty(data.ItemQuery))
        {
            return;
        }

        if (data.ItemQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            player.EquipBestItems();
            await Game.RavenNest.Players.EquipBestItemsAsync(player.UserId);
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);

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

        if (queriedItem.InventoryItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.InventoryItem))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED, queriedItem.InventoryItem.Name);
        }
    }
}
