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

        // Anywhere after the name, not only at the end: "abraxas armor set 5" is a set too.
        if (ItemSetRequest.IsSetQuery(inputQuery))
        {
            await GiftItemSetAsync(inputQuery, gm, client, player);
            return;
        }

        if (inputQuery.Contains(" "))
        {
            var parts = inputQuery.Split(" ");
            if (parts.Length == 3 && inputQuery.Contains(" coins ", System.StringComparison.OrdinalIgnoreCase) || inputQuery.Contains(" coin ", System.StringComparison.OrdinalIgnoreCase))
            {
                var amountString = parts.LastOrDefault();

                var resolved = itemResolver.ResolveTradeQuery(inputQuery,
                    parsePrice: false,
                    parseUsername: true,
                    parseAmount: true);

                if (resolved.Player == null)
                {
                    client.SendReply(gm, Localization.MSG_GIFT_COINS_PLAYER_NOT_FOUND, resolved.PlayerName);
                    return;
                }

                if (resolved.Player.Id == player.Id)
                {
                    client.SendReply(gm, Localization.MSG_GIFT_COINS_SELF);
                    return;
                }

                if (resolved.Count <= 0)
                {
                    client.SendReply(gm, Localization.MSG_GIFT_COINS_NEGATIVE);
                    return;
                }

                var result = await Game.RavenNest.Players.SendCoinsAsync(player.Id, resolved.Player.Id, resolved.Count);
                if (result > 0)
                {
                    resolved.Player.AddResource(Resource.Currency, result);
                    player.RemoveResource(Resource.Currency, result);
                    if (result > 1)
                        client.SendReply(gm, Localization.MSG_GIFT_COINS, resolved.PlayerName, result);
                    else
                        client.SendReply(gm, Localization.MSG_GIFT_COINS_SINGLE, resolved.PlayerName, result);
                    return;
                }

                if (result == -1 || result == -2)
                {
                    client.SendReply(gm, Localization.MSG_GIFT_COINS_FAILED, resolved.PlayerName);
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_GIFT_COINS_ERROR);
                }
                return;
            }
        }

        await GiftItemAsync(inputQuery, gm, client, player);
    }

    private async Task GiftItemSetAsync(string inputQuery, GameMessage gm, GameClient client, PlayerController player)
    {
        // "!gift zerratar abraxas armor set 5": who, which set, which part of it, how many of each piece.
        var request = ItemSetRequest.Parse(inputQuery);
        if (request == null)
        {
            client.SendReply(gm, Localization.MSG_GIFT_SET_HELP);
            return;
        }

        var targetPlayer = base.PlayerManager.GetPlayerByName(request.Target);
        if (targetPlayer == null)
        {
            client.SendReply(player, Localization.MSG_GIFT_PLAYER_NOT_FOUND, request.Target);
            return;
        }

        var setType = request.SetType;
        var itemSet = ItemSetFilter.Apply(itemResolver.GetItemSet(setType), request.Only);
        if (itemSet.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NOT_FOUND, setType);
            return;
        }

        // Taken before anything is given: the backpack list changes under us as each gift lands.
        var backpack = player.Inventory.GetBackpackItems().ToList();
        if (!itemSet.Any(i => backpack.Any(x => !x.Soulbound && x.Item.Id == i.Id)))
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NONE_OWNED, setType);
            return;
        }

        var given = new List<(string Name, long Amount)>();
        var piecesGiven = 0L;
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
                    var result = await Game.RavenNest.Players.GiftItemAsync(player.Id, targetPlayer.Id, stack.InstanceId, left);
                    if (result.Status != RavenNest.Models.GiftItemStatus.OK) continue;

                    // Update game client with the changes
                    // this is done locally to avoid sending additional data from server to client and visa versa.
                    targetPlayer.Inventory.AddOrSetInventoryItem(result.StackToIncrement);
                    player.Inventory.RemoveOrSetInventoryItem(result.StackToDecrement);
                    left -= result.Amount;
                    sentOfThis += result.Amount;
                }
                if (sentOfThis > 0)
                {
                    given.Add((i.Name, sentOfThis));
                    piecesGiven += sentOfThis;
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Error when '" + player.Name + "' tried to gift " + request.Count + "x '" + setType + "' item set to '" + targetPlayer.Id + "'. " + piecesGiven + " out of " + (itemSet.Count * request.Count) + " pieces were gifted. Exception: " + exc);
        }

        if (given.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_SET_NONE_OWNED, setType);
            return;
        }

        var itemList = ItemSetRequest.ListOf(given, request.Count);
        var wanted = itemSet.Count * request.Count;
        if (piecesGiven != wanted)
        {
            client.SendReply(gm, Localization.MSG_GIFT_SET_PARTIAL, piecesGiven, wanted, setType, targetPlayer.Name, itemList);
        }
        else if (request.Count > 1)
        {
            client.SendReply(gm, Localization.MSG_GIFT_SETS, request.Count, setType, targetPlayer.Name, itemList);
        }
        else
        {
            var format = Localization.MSG_GIFT_SET_A;
            if (setType[0] == 'a' || setType[0] == 'e' || setType[0] == 'i' || setType[0] == 'u' || setType[0] == 'o') format = Localization.MSG_GIFT_SET_AN;
            client.SendReply(gm, format,
                setType, targetPlayer.Name, itemList);
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

        var result = await Game.RavenNest.Players.GiftItemAsync(player.Id, item.Player.Id, item.InventoryItem.InstanceId, amount);
        if (result.Status == RavenNest.Models.GiftItemStatus.OK)
        {
            // Update game client with the changes
            // this is done locally to avoid sending additional data from server to client and visa versa.
            var targetPlayer = item.Player;
            targetPlayer.Inventory.AddOrSetInventoryItem(result.StackToIncrement);
            player.Inventory.RemoveOrSetInventoryItem(result.StackToDecrement);
            client.SendReply(gm, Localization.MSG_GIFT, result.Amount, item.Item.Name, item.Player.PlayerName);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_GIFT_ERROR, item.Count, item.Item.Name, item.Player.PlayerName);
        }
    }
}
