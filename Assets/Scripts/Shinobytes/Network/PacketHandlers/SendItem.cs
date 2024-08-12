using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SendItem : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public SendItem(
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
            client.SendReply(player, Localization.MSG_SEND_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (inputQuery.EndsWith(" set", System.StringComparison.OrdinalIgnoreCase) || inputQuery.EndsWith(" sets", System.StringComparison.OrdinalIgnoreCase))
        {
            await SendItemSetAsync(inputQuery, gm, client, player);
            return;
        }

        await SendItemAsync(inputQuery, gm, client, player);
    }

    private async Task SendItemSetAsync(string inputQuery, GameMessage gm, GameClient client, PlayerController player)
    {
        // resolve set type
        // so we can take for instance !send brute black set xAmount
        // for now, we will just replace the word " set " with various item types
        // first extract the types of a known set:

        var queryParts = inputQuery.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower()).ToArray();
        if (queryParts.Length < 3)
        {
            client.SendReply(gm, "When sending a whole item set, please include target player name, item type, word set. You can only send one set at a time and only unequipped items will be sent. example !send characterNumber rune set");
            return;
        }

        // resolve target player
        var targetPlayer = queryParts[0];
        if (string.IsNullOrEmpty(targetPlayer))
        {
            return;
        }

        if (targetPlayer == (player.CharacterIndex + 1).ToString() || targetPlayer.Equals(player.Identifier, System.StringComparison.OrdinalIgnoreCase))
        {
            client.SendReply(gm, "You cannot send items to yourself.");
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
        var itemsToSend = new List<GameInventoryItem>();

        // now select the matches.
        foreach (var i in itemSet)
        {
            var existingStack = unequippedItems.FirstOrDefault(x => !x.Soulbound && x.Item.Id == i.Id);
            if (existingStack != null)
            {
                itemsToSend.Add(existingStack);
            }
        }

        // targetPlayer
        if (itemsToSend.Count > 0)
        {
            var sentItems = new List<GameInventoryItem>();
            try
            {
                foreach (var i in itemsToSend)
                {
                    var result = await Game.RavenNest.Players.SendItemAsync(player.Id, targetPlayer, i.InstanceId, 1);
                    if (result.Status == RavenNest.Models.GiftItemStatus.OK)
                    {
                        // Update game client with the changes
                        // this is done locally to avoid sending additional data from server to client and visa versa.

                        player.Inventory.RemoveOrSetInventoryItem(result.StackToDecrement);

                        sentItems.Add(i);
                    }
                }
            }
            catch (System.Exception exc)
            {
                Shinobytes.Debug.LogError("Error when '" + player.Name + "' tried to send an '" + setType + "' item set to '" + targetPlayer + "'. " + sentItems.Count + " out of " + itemsToSend.Count + " was sent. Exception: " + exc);
            }
            var itemList = Utility.ReplaceLastOccurrence(string.Join(", ", sentItems.Select(x => x.Name).ToArray()), ", ", " and ");
            if (sentItems.Count != itemSet.Count)
            {
                client.SendReply(gm, "You have sent {itemCount} out of {itemSetCount} from the {setType} set to {targetPlayerName}! The items sent was {itemList}", sentItems.Count, itemSet.Count, setType, targetPlayer, itemList);
            }
            else
            {
                var a = "a";
                if (setType[0] == 'a' || setType[0] == 'e' || setType[0] == 'i' || setType[0] == 'u' || setType[0] == 'o') a = "an";
                client.SendReply(gm, "You have sent " + a + " {setType} set to {targetPlayerName}! The items sent was {itemList}",
                    setType, targetPlayer, itemList);
            }
        }
        else
        {
            client.SendReply(gm, "You do not have any of the items in the set '{query}'", setType);
        }
    }

    private async Task SendItemAsync(string inputQuery, GameMessage gm, GameClient client, PlayerController player)
    {
        var item = itemResolver.ResolveTradeQuery(inputQuery,
            parsePrice: false,
            parseUsername: true,
            parseAmount: true,
            playerToSearch: player);

        if (item.PlayerName == (player.CharacterIndex + 1).ToString() || item.PlayerName.Equals(player.Identifier, System.StringComparison.OrdinalIgnoreCase))
        {
            client.SendReply(gm, "You cannot send items to yourself.");
            return;
        }

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(player, Localization.MSG_SEND_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendReply(player, Localization.MSG_SEND_ITEM_NOT_OWNED, item.Item.Name);
            return;
        }

        if (item.PlayerName == null)
        {
            client.SendReply(player, Localization.MSG_SEND_PLAYER_NOT_FOUND, inputQuery);
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

        var result = await Game.RavenNest.Players.SendItemAsync(player.Id, item.PlayerName, item.InventoryItem.InstanceId, amount);
        if (result.Status == RavenNest.Models.GiftItemStatus.OK)
        {
            // Update game client with the changes
            // this is done locally to avoid sending additional data from server to client and visa versa.
            var targetPlayer = item.Player;
            if (targetPlayer)
            {
                targetPlayer.Inventory.AddOrSetInventoryItem(result.StackToIncrement);
            }
            player.Inventory.RemoveOrSetInventoryItem(result.StackToDecrement);
            client.SendReply(gm, Localization.MSG_SEND, result.Amount, item.Item.Name, item.PlayerName);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_SEND_ERROR, item.Count, item.Item.Name, item.PlayerName);
        }
    }
}
