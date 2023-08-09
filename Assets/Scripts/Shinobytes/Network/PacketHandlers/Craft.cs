﻿using System;
using System.Linq;
using System.Text;
using RavenNest.Models;

public class Craft : ChatBotCommandHandler<string>
{
    public const int MaxCraftingCount = 50_000_000;

    public Craft(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (player.Ferry && player.Ferry.Active)
        {
            client.SendReply(gm, Localization.MSG_CRAFT_FAILED_FERRY);
            return;
        }

        if (string.IsNullOrEmpty(inputQuery))
        {
            // Player perhaps intended to train crafting.
            player.SetTask("crafting");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();

        Item item = null;
        var amountToCraft = 1d;
        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item != null)
        {
            item = queriedItem.Item;
            amountToCraft = Math.Min(MaxCraftingCount, queriedItem.Count);
        }
        else
        {
            var (category, typeStr) = GetCraftingTarget(inputQuery);

            var type = ItemType.None;

            if (!string.IsNullOrEmpty(typeStr))
            {
                Enum.TryParse(typeStr, true, out type);
            }

            item = Game.Crafting.GetCraftableItemForPlayer(player, category, type);

            if (item != null)
            {
                client.SendReply(gm, Localization.MSG_CRAFT_ITEM_NOT_FOUND_MEAN, inputQuery, item.Name);
                return;
            }

            client.SendReply(gm, Localization.MSG_CRAFT_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        var toCraft = amountToCraft > int.MaxValue ? int.MaxValue : (int)amountToCraft;
        var status = Game.Crafting.CanCraftItem(player, item);
        switch (status)
        {
            case CraftValidationStatus.OK:
                await CraftItemAsync(inputQuery, gm, client, player, item, toCraft);
                return;
            case CraftValidationStatus.NeedCraftingStation:
                client.SendReply(gm, Localization.MSG_CRAFT_FAILED_STATION);
                return;
            case CraftValidationStatus.NotEnoughSkill:
                client.SendReply(gm, Localization.MSG_CRAFT_FAILED_LEVEL, item.RequiredCraftingLevel);
                return;
            case CraftValidationStatus.NotCraftable:
                client.SendReply(gm, Localization.MSG_CRAFT_FAILED_NOT_CRAFTABLE, item.Name);
                return;
            case CraftValidationStatus.NotEnoughResources:
                InsufficientResources(player, gm, inputQuery, client, item, toCraft);
                return;
        }
    }

    private (ItemCategory, string) GetCraftingTarget(string categoryAndType)
    {
        if (!string.IsNullOrEmpty(categoryAndType))
        {
            var types = categoryAndType.Split(' ');
            var categories = Enum.GetNames(typeof(CraftableCategory));

            for (var i = 0; i < types.Length; ++i)
            {
                if (categories.Any(x => x.Equals(types[i], StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (Enum.TryParse<CraftableCategory>(types[i], true, out var item))
                    {
                        var category = ItemCategory.Weapon;
                        switch ((CraftableCategory)item)
                        {
                            case CraftableCategory.Weapon:
                                category = ItemCategory.Weapon;
                                break;

                            case CraftableCategory.Armor:
                            case CraftableCategory.Helm:
                            case CraftableCategory.Chest:
                            case CraftableCategory.Gloves:
                            case CraftableCategory.Leggings:
                            case CraftableCategory.Boots:
                                category = ItemCategory.Armor;
                                break;

                            case CraftableCategory.Ring:
                                category = ItemCategory.Ring;
                                break;

                            case CraftableCategory.Amulet:
                                category = ItemCategory.Amulet;
                                break;
                        }

                        return (category, categoryAndType);
                    }
                    else if (Enum.TryParse<ItemCategory>(categoryAndType, true, out var weapon))
                    {
                        return (weapon, "");
                    }
                }
            }
        }
        return (ItemCategory.Potion, null);
    }

    private void InsufficientResources(
        PlayerController player,
        GameMessage gm,
        string inputQuery,
        GameClient client, Item item, double amount = 1)
    {
        if (item != null)
        {
            var requiredItemsStr = new StringBuilder();
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatAmount(player.Resources.Wood)} / {Utility.FormatAmount(item.WoodCost * amount)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatAmount(player.Resources.Ore)} / {Utility.FormatAmount(item.OreCost * amount)} Ore, ");
            }

            foreach (var req in item.CraftingRequirements)
            {
                var requiredItem = Game.Items.Get(req.ResourceItemId);
                var ownedNumber = 0L;
                var items = player.Inventory.GetInventoryItemsByItemId(req.ResourceItemId);
                if (items != null)
                {
                    ownedNumber = (long)items.Sum(x => x.Amount);
                }
                requiredItemsStr.Append($"{Utility.FormatAmount(ownedNumber)}/{Utility.FormatAmount(req.Amount * amount)} {requiredItem.Name}, ");
            }
            if (amount > 1)
            {
                requiredItemsStr.Append("to craft " + (int)amount + "x " + item.Name);
            }
            else
            {
                requiredItemsStr.Append("to craft " + item.Name);
            }
            client.SendReply(gm, requiredItemsStr.ToString());
        }
        else
        {
            client.SendReply(gm, Localization.MSG_CRAFT_FAILED_RES, item.Name);
        }
    }

    private async System.Threading.Tasks.Task<bool> CraftItemAsync(
        string inputQuery,
        GameMessage gm,
        GameClient client,
        PlayerController player,
        Item item,
        int amountToCraft)
    {
        try
        {
            var craftResult = await Game.RavenNest.Players.CraftItemsAsync(player.Id, item.Id, amountToCraft);
            if (craftResult != null)
            {
                if (craftResult.Status == CraftItemResultStatus.InsufficientResources)
                {
                    InsufficientResources(player, gm, inputQuery, client, item, amountToCraft);
                    return false;
                }

                if (craftResult.Status == CraftItemResultStatus.LevelTooLow)
                {
                    client.SendReply(gm, "Your crafting level is too low. You need to be level {reqCraftingLevel}. You are currently level {craftingLevel}.",
                        item.RequiredCraftingLevel.ToString(), player.Stats.Crafting.Level.ToString());
                    return false;
                }

                if (craftResult.Status == CraftItemResultStatus.Success || craftResult.Status == CraftItemResultStatus.PartialSuccess)
                {
                    amountToCraft = craftResult.Value;

                    var existingStack = player.Inventory.GetAllItems().FirstOrDefault(x => x.InstanceId == craftResult.InventoryItemId);

                    if (existingStack != null)
                    {
                        existingStack.Amount += amountToCraft;
                    }
                    else
                    {
                        var added = player.Inventory.AddToBackpack(craftResult);
                        if (added == null)
                        {
                            client.SendReply(gm, "Problem when trying to add the item after crafting. You can try and !leave !join and see if the crafted item was added properly.", item.Name);
                            return false;
                        }
                    }

                    foreach (var req in item.CraftingRequirements)
                    {
                        var amount = req.Amount * amountToCraft;
                        var stacks = player.Inventory.GetInventoryItemsByItemId(req.ResourceItemId);
                        foreach (GameInventoryItem stack in stacks)
                        {
                            if (stack.Amount < amount)
                            {
                                var toRemove = amount - stack.Amount;
                                player.Inventory.Remove(stack, toRemove);
                                amount -= (int)toRemove;
                            }

                            if (stack.Amount >= amount)
                            {
                                player.Inventory.Remove(stack, amount);
                            }
                        }
                    }

                    if (item.WoodCost > 0) player.RemoveResource(Resource.Woodcutting, item.WoodCost * amountToCraft);
                    if (item.OreCost > 0) player.RemoveResource(Resource.Mining, item.OreCost * amountToCraft);

                    if (amountToCraft > 1)
                    {
                        var msgAddS = item.Name.EndsWith("s") ? "" : "s";
                        client.SendReply(gm, Localization.MSG_CRAFT_MANY, amountToCraft.ToString(), item.Name + msgAddS);
                    }
                    else client.SendReply(gm, Localization.MSG_CRAFT, item.Name);
                    return true;
                }
            }

            if (craftResult == null)
            {
                client.SendReply(gm, "Crafting failed. Server did not respond. Try again later");
                return false;
            }

            if (craftResult.Status == CraftItemResultStatus.Error || craftResult.Status == CraftItemResultStatus.UncraftableItem || craftResult.Status == CraftItemResultStatus.UnknownItem)
            {
                client.SendReply(gm, "Server returned an error when trying to craft the item: {serverResponseResult}", craftResult.Status.ToString());
                return false;
            }

            return true;
        }
        catch (System.Exception exc)
        {
            client.SendReply(gm, "Error occurred when handling the response from the server.");

            Game.RavenNest.Game.ReportExceptionAsync($"{player.UserId}, {item.Name} ({item.Id}), {amountToCraft}", exc);

            Shinobytes.Debug.LogError("Error when trying to craft an item: " + exc);
            return false;
        }
    }

    public enum CraftableCategory
    {
        Weapon,
        Armor,
        Helm,
        Chest,
        Gloves,
        Leggings,
        Boots,
        Ring,
        Amulet
    }
}