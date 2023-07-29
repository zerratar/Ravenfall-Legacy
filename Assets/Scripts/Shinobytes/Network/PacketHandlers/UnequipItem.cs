public class UnequipItem : ChatBotCommandHandler<string>
{
    public UnequipItem(
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

        if (inputQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            await player.UnequipAllItemsAsync();
            client.SendReply(gm, Localization.MSG_UNEQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();

        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery,
            parsePrice: false,
            parseAmount: false,
            playerToSearch: player,
            equippedState: EquippedState.Equipped);

        //var queriedItem = itemResolver.ResolveInventoryItem(player, inputQuery, 5, EquippedState.Equipped);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }


        if (queriedItem.InventoryItem == null || !player.Inventory.IsEquipped(queriedItem.InventoryItem))
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_EQUIPPED, queriedItem.Item.Name);
            return;
        }

        await player.UnequipAsync(queriedItem.InventoryItem);
        client.SendReply(gm, Localization.MSG_UNEQUIPPED, queriedItem.Item.Name);
    }
}