using Shinobytes.Linq;

public class ItemCount : ChatBotCommandHandler<string>
{
    public ItemCount(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(inputQuery))
        {
            client.SendReply(gm, Localization.MSG_ITEM_COUNT_MISSING_ARGS);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery,
            parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);


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

        var item = queriedItem.InventoryItem;
        if (item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        var itemAmount = item.Amount;
        if (item.Enchantments?.Count == 0)
        {
            // good, no enchantments, then we should check all our stacks of this type
            itemAmount = player.Inventory.GetInventoryItemsByItemId(item.Item.Id).AsList(x => x.Enchantments?.Count == 0).Sum(x => x.Amount);
        }

        client.SendReply(gm, "You have {itemCount}x {itemName}(s).", itemAmount, item.Name);
    }
}