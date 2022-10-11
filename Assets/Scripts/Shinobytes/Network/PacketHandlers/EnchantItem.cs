using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;

public class EnchantItem : ChatBotCommandHandler<TradeItemRequest>
{
    public EnchantItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        // Only game admins can use enchant in this version.
        if (!GameVersion.TryParse("0.7.9.2a", out var minVersion))
        {
            return;
        }

        var player = PlayerManager.GetPlayer(data.Player);
        if (GameVersion.GetApplicationVersion() < minVersion || !player || !player.IsGameAdmin) //(/*!Game.Permissions.IsAdministrator ||*/ !player || !player.IsGameAdmin))
        {
            return;
        }

        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.Clan == null || !player.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        if (string.IsNullOrEmpty(data.ItemQuery))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ENCHANT_MISSING_ARGS);
            return;
        }
        var query = data.ItemQuery;
        var isReplace = query.ToLower().IndexOf("replace") >= 0;
        if (isReplace) query = query.Replace("replace", "");

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(query,
            parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);

        //if (queriedItem == null)
        //    queriedItem = itemResolver.Resolve(data.ItemQuery + " pet", parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = player.Inventory.GetInventoryItems(queriedItem.Item.ItemId);
        if (item == null || item.Count == 0)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        var inventoryItem = item[0];

        if (!isReplace && (inventoryItem.Enchantments != null && inventoryItem.Enchantments.Count > 0))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ENCHANT_WARN_REPLACE, inventoryItem.Name, FormatEnchantmentValues(inventoryItem.Enchantments));
            return;
        }

        var result = await Game.RavenNest.Players.EnchantItemAsync(player.UserId, inventoryItem.InstanceId);
        if (result == null || result.Result == ItemEnchantmentResultValue.Error)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ENCHANT_UNKNOWN_ERROR);
            return;
        }

        var cooldown = result.Cooldown.GetValueOrDefault() - System.DateTime.UtcNow;
        var cooldownString = GetCooldownString(cooldown);
        if (!result.Success)
        {

            if (result.Result == ItemEnchantmentResultValue.NotEnchantable)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_NOT_ENCHANTABLE, inventoryItem.Name);
                return;
            }

            if (result.Result == ItemEnchantmentResultValue.Failed)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_FAILED, inventoryItem.Name, cooldownString);
                return;
            }

            client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_COOLDOWN, cooldownString);
            return;
        }

        if (result.GainedExperience > 0 || result.GainedLevels > 0)
        {
            var clanSkill = player.Clan.ClanInfo.ClanSkills.FirstOrDefault(x => x.Name.ToLower() == "enchanting");
            Game.Clans.UpdateClanSkill(player.Clan.ClanInfo.Id, clanSkill.Id, result.GainedLevels, result.GainedExperience);
        }

        var enchantedItem = result.EnchantedItem;
        var oldItemName = result.OldItemStack.Name ?? Game.Items.Get(result.OldItemStack.ItemId)?.Name;
        var addedItem = player.Inventory.AddToBackpack(enchantedItem, 1);

        if (string.IsNullOrEmpty(result.OldItemStack.Enchantment))
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_SUCCESS, oldItemName, addedItem.Name, cooldownString);
        }
        else
        {
            client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_REPLACE, addedItem.Name, cooldownString);
        }

        client.SendFormat(data.Player.Username, Localization.MSG_ENCHANT_STATS, FormatEnchantmentValues(addedItem.Enchantments), addedItem.Name);
    }

    private string FormatEnchantmentValues(IReadOnlyList<ItemEnchantment> enchantments)
    {
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
