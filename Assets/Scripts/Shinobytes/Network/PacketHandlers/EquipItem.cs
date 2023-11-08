public class EquipItem : ChatBotCommandHandler<string>
{
    public EquipItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string targetItem, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(targetItem))
        {
            return;
        }

        if (targetItem.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            player.EquipBestItems();
            await Game.RavenNest.Players.EquipBestItemsAsync(player.Id);
            client.SendReply(gm, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveInventoryItem(player, targetItem, 5, EquippedState.NotEquipped);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, targetItem, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, targetItem);
            return;
        }

        if (queriedItem.InventoryItem == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (!queriedItem.InventoryItem.IsEquippableType)
        {
            client.SendReply(gm, "{itemName} is not an equippable item.", queriedItem.InventoryItem.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.InventoryItem))
        {
            client.SendReply(gm, Localization.MSG_EQUIPPED, queriedItem.InventoryItem.Name);
        }
        else
        {
            player.AnnounceLevelToLowToEquip(queriedItem.InventoryItem);
        }
    }
}
