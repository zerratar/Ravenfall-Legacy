﻿using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;

public class DisenchantItem : ChatBotCommandHandler<string>
{
    public DisenchantItem(
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
            client.SendReply(gm, Localization.MSG_ENCHANT_MISSING_ARGS);
            return;
        }

        var query = inputQuery;
        var isReplace = query.ToLower().IndexOf("replace") >= 0;
        if (isReplace) query = query.Replace("replace", "");

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveInventoryItem(player, query);

        if (queriedItem == null || queriedItem.Item == null)
        {
            if (queriedItem != null && queriedItem.SuggestedItemNames != null && queriedItem.SuggestedItemNames.Length > 0)
            {
                client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", queriedItem.SuggestedItemNames));
                return;
            }
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        var item = queriedItem.InventoryItem;
        if (item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        var inventoryItem = item;
        if ((inventoryItem.Enchantments == null || inventoryItem.Enchantments.Count == 0) || string.IsNullOrEmpty(inventoryItem.InventoryItem.Enchantment))
        {
            client.SendReply(gm, Localization.MSG_DISENCHANT_NOT_ENCHANTED, inventoryItem.Name);
            return;
        }

        var result = await Game.RavenNest.Players.DisenchantInventoryItemAsync(player.Id, inventoryItem.InstanceId);
        if (result == null || result.Result == ItemEnchantmentResultValue.Error || !result.Success)
        {
            client.SendReply(gm, Localization.MSG_DISENCHANT_UNKNOWN_ERROR);
            return;
        }

        var enchantedItem = result.EnchantedItem;
        var oldItemName = result.OldItemStack.Name ?? Game.Items.Get(result.OldItemStack.ItemId)?.Name;

        var inventory = player.Inventory;
        var wasEquipped = inventory.IsEquipped(inventoryItem);
        if (wasEquipped)
        {
            player.Unequip(inventoryItem);
        }

        inventory.RemoveByInventoryId(result.OldItemStack.Id, 1);
        var addedItem = inventory.AddToBackpack(enchantedItem, 1);

        if (wasEquipped)
        {
            player.Equip(addedItem, false);
        }

        client.SendReply(gm, Localization.MSG_DISENCHANT_SUCCESS, oldItemName);
    }
}
