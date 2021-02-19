using System;
using System.Linq;
using System.Text;
using RavenNest.Models;

public class Craft : PacketHandler<TradeItemRequest>
{
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

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();

        Item item = null;
        var amountToCraft = 1m;
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
                client.SendFormat(data.Player.Username, Localization.MSG_CRAFT_FAILED, category, type);
                return;
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
            client.SendMessage(data.Player.Username, requiredItemsStr.ToString());
        }
        else
        {
            client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_FAILED_RES, item.Name);
        }
    }

    private async System.Threading.Tasks.Task<bool> CraftItemAsync(TradeItemRequest data, GameClient client, PlayerController player, Item item, int amountToCraft)
    {
        var craftResult = await Game.RavenNest.Players.CraftItemAsync(player.UserId, item.Id);
        if (craftResult == AddItemResult.Failed)
        {
            InsufficientResources(player, data, client, item);
            return false;
        }

        for (var i = 0; i < amountToCraft; ++i)
        {
            player.AddItem(item, false);
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
                    player.Inventory.Remove(stack.Item, toRemove);
                    amount -= (int)toRemove;
                }

                if (stack.Amount >= amount)
                {
                    player.Inventory.Remove(stack.Item, amount);
                }
            }
        }

        player.RemoveResource(Resource.Woodcutting, item.WoodCost * amountToCraft);
        player.RemoveResource(Resource.Mining, item.OreCost * amountToCraft);

        switch (craftResult)
        {
            case AddItemResult.AddedAndEquipped:
            //player.EquipIfBetter(item);
            //client.SendMessage(data.Player.Username, Localization.MSG_CRAFT_EQUIPPED, item.Name);
            //break;
            case AddItemResult.Added:
                if (amountToCraft > 1)
                    client.SendMessage(data.Player.Username, 
                        Localization.MSG_CRAFT_MANY, 
                        item.Name, 
                        amountToCraft.ToString());
                else
                    client.SendMessage(data.Player.Username, Localization.MSG_CRAFT, item.Name);
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