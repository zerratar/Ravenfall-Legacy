public class EquipItem : ChatBotCommandHandler<string>
{
    public EquipItem(
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

        if (string.IsNullOrEmpty(inputQuery))
        {
            return;
        }

        if (inputQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            player.EquipBestItems();
            await Game.RavenNest.Players.EquipBestItemsAsync(player.Id);
            client.SendReply(gm, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);

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

        if (queriedItem.InventoryItem == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.InventoryItem))
        {
            client.SendReply(gm, Localization.MSG_EQUIPPED, queriedItem.InventoryItem.Name);
        }
    }
}
