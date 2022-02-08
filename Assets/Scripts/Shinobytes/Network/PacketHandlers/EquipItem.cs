public class EquipItem : PacketHandler<TradeItemRequest>
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
            await player.EquipBestItemsAsync();
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);

        if (queriedItem == null)
            queriedItem = itemResolver.Resolve(data.ItemQuery + " pet", parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = player.Inventory.GetInventoryItems(queriedItem.Id);
        if (item == null || item.Count == 0)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.Item.Item))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED, queriedItem.Item.Name);
        }
    }
}
