using Shinobytes.Linq;

public class ItemCount : ChatBotCommandHandler<TradeItemRequest>
{
    public ItemCount(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(data.ItemQuery))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_COUNT_MISSING_ARGS);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(data.ItemQuery,
            parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);


        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = queriedItem.InventoryItem;
        if (item == null)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        var itemAmount = item.Amount;
        if (item.Enchantments?.Count == 0)
        {
            // good, no enchantments, then we should check all our stacks of this type
            itemAmount = player.Inventory.GetInventoryItems(item.Item.Id).AsList(x => x.Enchantments?.Count == 0).Sum(x => x.Amount);
        }

        client.SendFormat(data.Player.Username, "You have {itemCount}x {itemName}(s).", itemAmount, item.Name);
    }
}