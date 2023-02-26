using System;
using System.Linq;
using System.Text;
using RavenNest.Models;

public class Craft : ChatBotCommandHandler<TradeItemRequest>
{
    public const int MaxCraftingCount = 50_000_000;

    public Craft(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.Ferry && player.Ferry.Active)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_FAILED_FERRY);
            return;
        }

        if (string.IsNullOrEmpty(data.ItemQuery))
        {
            // Player perhaps intended to train crafting.
            player.SetTask("crafting", new string[0]);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();

        Item item = null;
        var amountToCraft = 1d;
        var queriedItem = itemResolver.ResolveTradeQuery(data.ItemQuery, parsePrice: false, parseUsername: false);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item != null)
        {
            item = queriedItem.Item;
            amountToCraft = Math.Min(MaxCraftingCount, queriedItem.Count);
        }
        else
        {
            var (category, typeStr) = GetCraftingTarget(data.ItemQuery);

            var type = ItemType.None;

            if (!string.IsNullOrEmpty(typeStr))
            {
                Enum.TryParse(typeStr, true, out type);
            }

            item = Game.Crafting.GetCraftableItemForPlayer(player, category, type);

            if (typeStr == null && category == ItemCategory.Potion)
            {
                if (item != null)
                {
                    client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_ITEM_NOT_FOUND_MEAN, data.ItemQuery, item.Name);
                    return;
                }

                client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_ITEM_NOT_FOUND, data.ItemQuery);
                return;
            }

            if (item == null)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_FAILED, category, type);
                return;
            }

            if (category != ItemCategory.Armor && category != ItemCategory.Weapon)
            {
                client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_ITEM_NOT_FOUND_MEAN, data.ItemQuery, item.Name);
                return; // don't actually craft atm
            }
        }

        var toCraft = amountToCraft > int.MaxValue ? int.MaxValue : (int)amountToCraft;
        var status = Game.Crafting.CanCraftItem(player, item);
        switch (status)
        {
            case CraftValidationStatus.OK:
                await CraftItemAsync(data, client, player, item, toCraft);
                return;
            case CraftValidationStatus.NeedCraftingStation:
                client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_FAILED_STATION);
                return;
            case CraftValidationStatus.NotEnoughSkill:
                client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_FAILED_LEVEL, item.RequiredCraftingLevel);
                return;
            case CraftValidationStatus.NotEnoughResources:
                InsufficientResources(player, data, client, item, toCraft);
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

    private void InsufficientResources(PlayerController player, TradeItemRequest data, GameClient client, Item item, double amount = 1)
    {
        if (item != null)
        {
            var requiredItemsStr = new StringBuilder();
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Wood)} / {Utility.FormatValue(item.WoodCost * amount)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Ore)} / {Utility.FormatValue(item.OreCost * amount)} Ore, ");
            }

            foreach (var req in item.CraftingRequirements)
            {
                var requiredItem = Game.Items.Get(req.ResourceItemId);
                var ownedNumber = 0L;
                var items = player.Inventory.GetInventoryItems(req.ResourceItemId);
                if (items != null)
                {
                    ownedNumber = (long)items.Sum(x => x.Amount);
                }
                requiredItemsStr.Append($"{Utility.FormatValue(ownedNumber)}/{Utility.FormatValue(req.Amount * amount)} {requiredItem.Name}, ");
            }
            if (amount > 1)
            {
                requiredItemsStr.Append("to craft " + (int)amount + "x " + item.Name);
            }
            else
            {
                requiredItemsStr.Append("to craft " + item.Name);
            }
            client.SendMessage(data.Player.Username, requiredItemsStr.ToString());
        }
        else
        {
            client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_FAILED_RES, item.Name);
        }
    }

    private async System.Threading.Tasks.Task<bool> CraftItemAsync(
        TradeItemRequest data,
        GameClient client,
        PlayerController player,
        Item item,
        int amountToCraft)
    {
        try
        {
            var craftResult = await Game.RavenNest.Players.CraftItemsAsync(player.UserId, item.Id, amountToCraft);
            if (craftResult != null)
            {
                if (craftResult.Status == CraftItemResultStatus.InsufficientResources)
                {
                    InsufficientResources(player, data, client, item, amountToCraft);
                    return false;
                }

                if (craftResult.Status == CraftItemResultStatus.LevelTooLow)
                {
                    client.SendMessage(data.Player.Username, "Your crafting level is too low. You need to be level {reqCraftingLevel}. You are currently level {craftingLevel}.",
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
                            client.SendMessage(data.Player.Username, "Problem when trying to add the item after crafting. You can try and !leave !join and see if the crafted item was added properly.", item.Name);
                            return false;
                        }
                    }

                    foreach (var req in item.CraftingRequirements)
                    {
                        var amount = req.Amount * amountToCraft;
                        var stacks = player.Inventory.GetInventoryItems(req.ResourceItemId);
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
                        client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_MANY, amountToCraft.ToString(), item.Name + msgAddS);
                    }
                    else client.SendMessage(data.Player.Username, Localization.MSG_CRAFT, item.Name);
                    return true;
                }
            }

            if (craftResult == null)
            {
                client.SendMessage(data.Player.Username, "Crafting failed. Server did not respond. Try again later");
                return false;
            }

            if (craftResult.Status == CraftItemResultStatus.Error || craftResult.Status == CraftItemResultStatus.UncraftableItem || craftResult.Status == CraftItemResultStatus.UnknownItem)
            {
                client.SendMessage(data.Player.Username, "Server returned an error when trying to craft the item: {serverResponseResult}", craftResult.Status.ToString());
                return false;
            }

            return true;
        }
        catch (System.Exception exc)
        {
            client.SendMessage(data.Player.Username, "Error occurred when handling the response from the server.");

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