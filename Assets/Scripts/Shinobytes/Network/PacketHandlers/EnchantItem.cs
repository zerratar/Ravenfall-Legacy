using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;

public class EnchantItem : ChatBotCommandHandler<string>
{
    public EnchantItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        //// Only game admins can use enchant in this version.
        //if (!GameVersion.TryParse("0.7.9.2a", out var minVersion))
        //{
        //    return;
        //}

        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.Clan == null || !player.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        if (string.IsNullOrEmpty(inputQuery))
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_MISSING_ARGS);
            return;
        }

        var query = inputQuery;

        // For later
        var checkForCost = query.Contains("requirement") || query.IndexOf(" req ", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (checkForCost) query = query.Replace("requirement", "").Replace(" req ", " ", System.StringComparison.OrdinalIgnoreCase).Replace("  ", " ");

        var isReplace = query.ToLower().IndexOf("replace") >= 0;
        if (isReplace) query = query.Replace("replace", "");

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(query, parsePrice: false, parseAmount: false, playerToSearch: player);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, query, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, query);
            return;
        }

        if (checkForCost)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_COST_NO_REQ, queriedItem.Item.Name);
            return;
        }

        var item = queriedItem.InventoryItem;
        if (item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        var inventoryItem = item;
        if ((inventoryItem.Enchantments != null && inventoryItem.Enchantments.Count > 0) || !string.IsNullOrEmpty(inventoryItem.InventoryItem.Enchantment))
        {
            if (!isReplace)
            {
                client.SendReply(gm, Localization.MSG_ENCHANT_WARN_REPLACE, inventoryItem.Name, FormatEnchantmentValues(inventoryItem.Enchantments));
                return;
            }
        }

        var result = await Game.RavenNest.Players.EnchantInventoryItemAsync(player.Id, inventoryItem.InstanceId);
        if (result == null || result.Result == ItemEnchantmentResultValue.Error)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_UNKNOWN_ERROR);
            return;
        }

        var cooldown = result.Cooldown.GetValueOrDefault() - System.DateTime.UtcNow;
        var cooldownString = GetCooldownString(cooldown);
        if (!result.Success)
        {

            if (result.Result == ItemEnchantmentResultValue.NotAvailable)
            {
                client.SendReply(gm, Localization.MSG_ENCHANT_NOT_AVAILABLE);
                return;
            }

            if (result.Result == ItemEnchantmentResultValue.NotEnchantable)
            {
                client.SendReply(gm, Localization.MSG_ENCHANT_NOT_ENCHANTABLE, inventoryItem.Name);
                return;
            }

            if (result.Result == ItemEnchantmentResultValue.Failed)
            {
                client.SendReply(gm, Localization.MSG_ENCHANT_FAILED, inventoryItem.Name, cooldownString);
                return;
            }

            client.SendReply(gm, Localization.MSG_ENCHANT_COOLDOWN, cooldownString);
            return;
        }

        var enchantedItem = result.EnchantedItem;
        var oldItemName = result.OldItemStack.Name ?? Game.Items.Get(result.OldItemStack.ItemId)?.Name;

        GameInventoryItem addedItem = null;

        var inventory = player.Inventory;
        var wasEquipped = inventory.IsEquipped(inventoryItem);
        if (wasEquipped)
        {
            player.Unequip(inventoryItem);
        }

        if (result.OldItemStack.Id != enchantedItem.Id)
        {
            inventory.RemoveByInventoryId(result.OldItemStack.Id, 1);
            addedItem = inventory.AddToBackpack(enchantedItem, 1);
        }
        else
        {
            addedItem = inventory.ApplyEnchantment(enchantedItem);
        }

        if (wasEquipped)
        {
            player.Equip(addedItem, false);
        }

        inventory.UpdateEquipmentEffect();

        if (string.IsNullOrEmpty(result.OldItemStack.Enchantment))
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_SUCCESS, oldItemName, addedItem.Name, cooldownString);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_REPLACE, oldItemName, addedItem.Name, cooldownString);
        }

        client.SendReply(gm, Localization.MSG_ENCHANT_STATS, FormatEnchantmentValues(addedItem.Enchantments), addedItem.Name);
    }

    private string FormatEnchantmentValues(IReadOnlyList<ItemEnchantment> enchantments)
    {
        if (enchantments == null) return string.Empty;
        return string.Join(", ", enchantments.Select(x => x.Name + " +" + (x.ValueType == AttributeValueType.Percent ? (int)(x.Value * 100) + "%" : x.Value.ToString())));
    }

    private string GetCooldownString(System.TimeSpan cooldown)
    {
        if (cooldown.Hours > 0)
        {
            return (int)cooldown.TotalHours + " hours";
        }

        if (cooldown.Minutes > 0)
        {
            return (int)cooldown.TotalMinutes + " minutes";
        }

        if (cooldown.Seconds > 0)
        {
            return (int)cooldown.TotalSeconds + " seconds";
        }

        return "a moment";
    }
}
