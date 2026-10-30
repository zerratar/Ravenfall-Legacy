using RavenNest.Models;
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
            //client.SendReply(player, Localization.MSG_SEND_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        // Anywhere after the character, not only at the end: "rune set 5" is a set too.
        if (ItemSetRequest.IsSetQuery(inputQuery))
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

        var request = ItemSetRequest.Parse(inputQuery);
        if (request == null)
        {
            client.SendReply(gm, Localization.MSG_SEND_SET_HELP);
            return;
        }

        // resolve target player
        var targetPlayer = request.Target;
        if (string.IsNullOrEmpty(targetPlayer))
        {
            return;
        }

        if (targetPlayer == (player.CharacterIndex + 1).ToString() || targetPlayer.Equals(player.Identifier, System.StringComparison.OrdinalIgnoreCase))
        {
            client.SendReply(gm, Localization.MSG_SEND_SELF);
            return;
        }

        var setType = request.SetType;
        var itemSet = ItemSetFilter.Apply(itemResolver.GetItemSet(setType), request.Only);
        if (itemSet.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NOT_FOUND, setType);
            return;
        }

        // Taken before anything is sent: the backpack list changes under us as each one leaves.
        var backpack = player.Inventory.GetBackpackItems().ToList();
        if (!itemSet.Any(i => backpack.Any(x => !x.Soulbound && x.Item.Id == i.Id)))
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NONE_OWNED, setType);
            return;
        }

        var sent = new List<(string Name, long Amount)>();
        var piecesSent = 0L;
        try
        {
            foreach (var i in itemSet)
            {
                // As many of this piece as were asked for, from as many stacks as it takes.
                var left = request.Count;
                var sentOfThis = 0L;
                foreach (var stack in backpack.Where(x => !x.Soulbound && x.Item.Id == i.Id))
                {
                    if (left <= 0) break;
                    var result = await Game.RavenNest.Players.SendItemAsync(player.Id, targetPlayer, stack.InstanceId, left);
                    if (result.Status != RavenNest.Models.GiftItemStatus.OK) continue;

                    // Update game client with the changes
                    // this is done locally to avoid sending additional data from server to client and visa versa.
                    player.Inventory.RemoveOrSetInventoryItem(result.StackToDecrement);
                    left -= result.Amount;
                    sentOfThis += result.Amount;
                }
                if (sentOfThis > 0)
                {
                    sent.Add((i.Name, sentOfThis));
                    piecesSent += sentOfThis;
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Error when '" + player.Name + "' tried to send " + request.Count + "x '" + setType + "' item set to '" + targetPlayer + "'. " + piecesSent + " out of " + (itemSet.Count * request.Count) + " pieces were sent. Exception: " + exc);
        }

        if (sent.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NONE_OWNED, setType);
            return;
        }

        var itemList = ItemSetRequest.ListOf(sent, request.Count);
        var wanted = itemSet.Count * request.Count;
        if (piecesSent != wanted)
        {
            client.SendReply(gm, Localization.MSG_SEND_SET_PARTIAL, piecesSent, wanted, setType, targetPlayer, itemList);
        }
        else if (request.Count > 1)
        {
            client.SendReply(gm, Localization.MSG_SEND_SETS, request.Count, setType, targetPlayer, itemList);
        }
        else
        {
            var format = Localization.MSG_SEND_SET_A;
            if (setType[0] == 'a' || setType[0] == 'e' || setType[0] == 'i' || setType[0] == 'u' || setType[0] == 'o') format = Localization.MSG_SEND_SET_AN;
            client.SendReply(gm, format,
                setType, targetPlayer, itemList);
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
            client.SendReply(gm, Localization.MSG_SEND_SELF);
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
