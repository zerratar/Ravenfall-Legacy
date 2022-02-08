public class UnequipItem : PacketHandler<TradeItemRequest>
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
        var queriedItem = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
            queriedItem = itemResolver.Resolve(data.ItemQuery + " pet", parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = player.Inventory.GetEquippedItem(queriedItem.Id);
        if (item == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_EQUIPPED, queriedItem.Item.Name);
            return;
        }

        await player.UnequipAsync(item);
        client.SendMessage(data.Player.Username, Localization.MSG_UNEQUIPPED, queriedItem.Item.Name);
    }
}