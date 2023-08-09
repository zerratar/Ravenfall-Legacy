using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GiftItem : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public GiftItem(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        inputQuery = inputQuery?.Trim();
        if (string.IsNullOrEmpty(inputQuery))
        {
            client.SendReply(player, Localization.MSG_GIFT_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (inputQuery.EndsWith(" set", System.StringComparison.OrdinalIgnoreCase) || inputQuery.EndsWith(" sets", System.StringComparison.OrdinalIgnoreCase))
        {
            await GiftItemSetAsync(inputQuery, gm, client, player);
            return;
        }

        await GiftItemAsync(inputQuery, gm, client, player);
    }

    private async Task GiftItemSetAsync(string inputQuery, GameMessage gm, GameClient client, PlayerController player)
    {
        // resolve set type
        // so we can take for instance !gift zerratar black set xAmount
        // for now, we will just replace the word " set " with various item types

        // first extract the types of a known set:

        var queryParts = inputQuery.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower()).ToArray();
        if (queryParts.Length < 3)
        {
            client.SendReply(gm, "When gifting a whole item set, please include target player name, item type, word set. You can only gift one set at a time and only unequipped items will be gifted. example !gift playerName rune set");
            return;
        }

        // resolve target player
        var targetPlayer = base.PlayerManager.GetPlayerByName(queryParts[0]);
        if (targetPlayer == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_PLAYER_NOT_FOUND, queryParts[0]);
            return;
        }

        var setType = "";
        if (queryParts[1] == "elder")
        {
            // then combine type
            setType = $"{queryParts[1]} {queryParts[2]}";
        }
        else
        {
            setType = queryParts[1];
        }

        var itemSet = itemResolver.GetItemSet(setType);
        if (itemSet.Count == 0)
        {
            client.SendReply(gm, "No item sets could be found matching the name {query}", setType);
            return;
        }

        // now we have all items, we should compare and see if the player has any of these items in their inventories
        var unequippedItems = player.Inventory.GetBackpackItems();
        var itemsToGift = new List<GameInventoryItem>();

        // now select the matches.
        foreach (var i in itemSet)
        {
            var existingStack = unequippedItems.FirstOrDefault(x => !x.Soulbound && x.Item.Id == i.Id);
            if (existingStack != null)
            {
                itemsToGift.Add(existingStack);
            }
        }

        // targetPlayer
        if (itemsToGift.Count > 0)
        {
            var giftedItems = new List<GameInventoryItem>();
            foreach (var i in itemsToGift)
            {
                var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.Id, targetPlayer.Id, i.InstanceId, 1);
                if (giftCount > 0)
                {
                    // Update game client with the changes
                    // this is done locally to avoid sending additional data from server to client and visa versa.
                    targetPlayer.Inventory.AddToBackpack(i.Item, 1);
                    player.Inventory.Remove(i, 1);
                    giftedItems.Add(i);
                }
            }

            var itemList = Utility.ReplaceLastOccurrence(string.Join(", ", giftedItems.Select(x => x.Name).ToArray()), ", ", " and ");
            if (giftedItems.Count != itemSet.Count)
            {
                client.SendReply(gm, "You have gifted {itemCount} out of {itemSetCount} from the {setType} set to {targetPlayerName}! The items gifted was {itemList}", giftedItems.Count, itemSet.Count, setType, targetPlayer.Name, itemList);
            }
            else
            {
                var a = "a";
                if (setType[0] == 'a' || setType[0] == 'e' || setType[0] == 'i' || setType[0] == 'u' || setType[0] == 'o') a = "an";
                client.SendReply(gm, "You have gifted " + a + " {setType} set to {targetPlayerName}! The items gifted was {itemList}", giftedItems.Count, itemSet.Count, setType, targetPlayer.Name, itemList);
            }
        }
        else
        {
            client.SendReply(gm, "You do not have any of the items in the set '{query}'", setType);
        }
    }

    private async Task GiftItemAsync(string inputQuery, GameMessage gm, GameClient client, PlayerController player)
    {
        var item = itemResolver.ResolveTradeQuery(inputQuery,
            parsePrice: false,
            parseUsername: true,
            parseAmount: true,
            playerToSearch: player);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_ITEM_NOT_OWNED, item.Item.Name);
            return;
        }

        if (item.Player == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_PLAYER_NOT_FOUND, inputQuery);
            return;
        }

        if (item.Item?.Soulbound ?? false)
        {
            client.SendReply(player, Localization.MSG_ITEM_SOULBOUND, item.Item.Name);
            return;
        }

        var amount = item.Count;
        if (amount > long.MaxValue)
            amount = long.MaxValue;

        var giftCount = await Game.RavenNest.Players.GiftItemAsync(player.Id, item.Player.Id, item.InventoryItem.InstanceId, (long)amount);
        if (giftCount > 0)
        {
            // Update game client with the changes
            // this is done locally to avoid sending additional data from server to client and visa versa.
            item.Player.Inventory.AddToBackpack(item.Item, item.Count);
            player.Inventory.Remove(item.InventoryItem, item.Count, true);
            client.SendReply(gm, Localization.MSG_GIFT, giftCount, item.Item.Name, item.Player.PlayerName);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_GIFT_ERROR, item.Count, item.Item.Name, item.Player.PlayerName);
        }
    }
}
