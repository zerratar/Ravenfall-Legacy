using RavenNest.Models;

public class SellItem : ChatBotCommandHandler<string>
{
    public SellItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            var ioc = Game.gameObject.GetComponent<IoCContainer>();
            var itemResolver = ioc.Resolve<IItemResolver>();
            var item = itemResolver.ResolveTradeQuery(inputQuery, playerToSearch: player);
            
            if (item.SuggestedItemNames.Length > 0)
            {
                client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
                return;
            }

            if (item.Item == null)
            {
                client.SendReply(gm, Localization.MSG_SELL_ITEM_NOT_FOUND, inputQuery);
                return;
            }

            if (item.Count >= long.MaxValue)
            {
                client.SendReply(gm, Localization.MSG_SELL_TOO_MANY,
                    item.Count,
                    item.Item.Name,
                    long.MaxValue);
                return;
            }

            var itemAmount = item.Count;
            var pricePerItem = item.Price;

            if (itemAmount > long.MaxValue)
            {
                itemAmount = long.MaxValue;
            }

            if (pricePerItem > long.MaxValue)
            {
                pricePerItem = long.MaxValue;
            }

            if (item.Item?.Soulbound ?? false)
            {
                client.SendReply(gm, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
                return;
            }


            if (item.InventoryItem == null)
            {
                client.SendReply(player, Localization.MSG_SELL_ITEM_NOT_OWNED, item.Item.Name);
                return;
            }

            var sellResult = await Game.RavenNest.Marketplace.SellItemAsync(player.Id, item.Id, (long)itemAmount, (long)pricePerItem);

            if (sellResult == null)
            {
                client.SendReply(gm, Localization.MSG_SELL_MARKETPLACE_ERROR);
            }

            switch (sellResult.State)
            {
                case ItemTradeState.DoesNotExist:
                    client.SendReply(gm, Localization.MSG_SELL_ITEM_NOT_FOUND, inputQuery);
                    break;
                case ItemTradeState.DoesNotOwn:
                    client.SendReply(gm, Localization.MSG_SELL_ITEM_NOT_OWNED, item.Item.Name);
                    break;
                case ItemTradeState.Failed:
                    client.SendReply(gm, Localization.MSG_SELL_MARKETPLACE_ERROR);
                    break;
                case ItemTradeState.Success:
                    player.Inventory.Remove(item.InventoryItem, item.Count);
                    client.SendReply(gm, Localization.MSG_SELL,
                        item.Count.ToString(),
                        item.Item.Name,
                        Utility.FormatExp(item.Price));
                    break;
            }
        }
        catch (System.Exception err)
        {
            Shinobytes.Debug.LogError("SellItem threw an exception: " + err);
        }
    }
}
