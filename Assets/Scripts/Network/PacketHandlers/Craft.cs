using System;
using System.Linq;
using System.Text;
using RavenNest.Models;

public class Craft : PacketHandler<TradeItemRequest>
{
    public Craft(GameManager game, GameServer server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(data.Player.Username, "craft_failed", "You need to !join the game before you can can craft.");
            return;
        }

        if (player.Ferry && player.Ferry.Active)
        {
            client.SendCommand(data.Player.Username, "craft_failed", "You cannot craft while on the ferry");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to gift the item right now.");
            return;
        }
        Item item = null;
        var amountToCraft = 1m;
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false);
        if (queriedItem != null && queriedItem.Item != null)
        {
            item = queriedItem.Item;
            amountToCraft = queriedItem.Amount;
        }
        else
        {
            var (category, typeStr) = GetCraftingTarget(data.ItemQuery);
            Enum.TryParse<ItemType>(typeStr, true, out var type);
            item = Game.Crafting.GetCraftableItemForPlayer(player, category, type);
            if (item == null)
            {
                client.SendCommand(data.Player.Username, "craft_failed", $"{category} {type} cannot be crafted right now.");
                return;
            }
        }

        var status = Game.Crafting.CanCraftItem(player, item);
        switch (status)
        {
            case CraftValidationStatus.OK:
                await CraftItemAsync(data, client, player, item, amountToCraft);
                return;
            case CraftValidationStatus.NeedCraftingStation:
                client.SendCommand(data.Player.Username, "craft_failed", "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting");
                return;
            case CraftValidationStatus.NotEnoughSkill:
                client.SendCommand(data.Player.Username, "craft_failed", $"You can't craft this item, it requires level {item.RequiredCraftingLevel} crafting.");
                return;
            case CraftValidationStatus.NotEnoughResources:
                InsufficientResources(player, data, client, item);
                return;
        }
    }

    private (ItemCategory, string) GetCraftingTarget(string categoryAndType)
    {
        var types = categoryAndType.Split(' ');
        var categories = Enum.GetNames(typeof(CraftableCategory));

        if (categories.Any(x => x.Equals(categoryAndType, StringComparison.InvariantCultureIgnoreCase)))
        {
            if (Enum.TryParse<CraftableCategory>(categoryAndType, true, out var item))
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

        return (ItemCategory.Armor, "");
    }

    private void InsufficientResources(PlayerController player, TradeItemRequest data, GameClient client, Item item)
    {
        if (item != null)
        {
            var requiredItemsStr = new StringBuilder();
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {

                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Wood)} / {Utility.FormatValue(item.WoodCost)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Ore)} / {Utility.FormatValue(item.OreCost)} Ore, ");
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
                requiredItemsStr.Append($"{Utility.FormatValue(ownedNumber)}/{Utility.FormatValue(req.Amount)} {requiredItem.Name}, ");
            }

            requiredItemsStr.Append("to craft " + item.Name);
            client.SendCommand(data.Player.Username, "craft_failed", requiredItemsStr.ToString());
        }
        else
        {
            client.SendCommand(data.Player.Username, "craft_failed", $"Insufficient resources to craft " + item.Name);
        }
    }

    private async System.Threading.Tasks.Task<bool> CraftItemAsync(TradeItemRequest data, GameClient client, PlayerController player, Item item, decimal amountToCraft)
    {
        //for (var i = 0; i < amountToCraft; ++i)
        //{
        var craftResult = await Game.RavenNest.Players.CraftItemAsync(player.UserId, item.Id);
        if (craftResult == AddItemResult.Failed)
        {
            InsufficientResources(player, data, client, item);
            return false;
        }

        player.AddItem(item, false);

        foreach (var req in item.CraftingRequirements)
        {
            var amount = req.Amount;
            var stacks = player.Inventory.GetInventoryItems(req.ResourceItemId);
            foreach (GameInventoryItem stack in stacks)
            {
                if (stack.Amount < amount)
                {
                    var toRemove = amount - stack.Amount;
                    player.Inventory.Remove(stack.Item, toRemove);
                    amount -= (int)toRemove;
                }

                if (stack.Amount >= amount)
                {
                    player.Inventory.Remove(stack.Item, amount);
                }
            }
        }

        player.RemoveResource(Resource.Woodcutting, item.WoodCost);
        player.RemoveResource(Resource.Mining, item.OreCost);

        switch (craftResult)
        {
            case AddItemResult.AddedAndEquipped:
                player.EquipIfBetter(item);
                client.SendCommand(data.Player.Username, "craft_success", $"You crafted and equipped a {item.Name}!");
                break;
            case AddItemResult.Added:
                client.SendCommand(data.Player.Username, "craft_success", $"You crafted a {item.Name}!");
                break;
        }
        //}

        return true;
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