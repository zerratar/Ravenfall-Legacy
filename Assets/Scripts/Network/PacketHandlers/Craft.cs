using System;
using System.Linq;
using System.Text;
using RavenNest.Models;

public class Craft : PacketHandler<CraftRequest>
{
    public Craft(GameManager game, GameServer server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(CraftRequest data, GameClient client)
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

        var categories = Enum.GetNames(typeof(ItemCategory));
        if (Enum.TryParse<ItemCategory>(data.Category, true, out var category))
        {
            Enum.TryParse<ItemType>(data.Type, true, out var type);

            var item = Game.Crafting.GetCraftableItemForPlayer(player, category, type);
            if (item == null)
            {
                client.SendCommand(data.Player.Username, "craft_failed", $"{category} {type} cannot be crafted right now.");
                return;
            }

            var status = Game.Crafting.CanCraftItem(player, item);
            switch (status)
            {
                case CraftValidationStatus.OK:
                    await CraftItemAsync(data, client, player, item);
                    return;
                case CraftValidationStatus.NeedCraftingStation:
                    client.SendCommand(data.Player.Username, "craft_failed", "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting");
                    return;
                case CraftValidationStatus.NotEnoughResources:
                    InsufficientResources(player, data, client, item);
                    return;
            }
            return;
        }

        client.SendCommand(data.Player.Username, "craft_failed", $"{data.Category} is not a craftable item type. Supported types are {string.Join(", ", categories)}");
    }

    private void InsufficientResources(PlayerController player, CraftRequest data, GameClient client, Item item)
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
                requiredItemsStr.Append($"{Utility.FormatValue(ownedNumber)} / {Utility.FormatValue(req.Amount)} {requiredItem.Name}, ");
            }

            requiredItemsStr.Append("to craft " + item.Name);
            client.SendCommand(data.Player.Username, "craft_failed", requiredItemsStr.ToString());
        }
        else
        {
            client.SendCommand(data.Player.Username, "craft_failed", $"Insufficient resources to craft " + item.Name);
        }
    }

    private async System.Threading.Tasks.Task CraftItemAsync(CraftRequest data, GameClient client, PlayerController player, Item item)
    {
        var craftResult = await Game.RavenNest.Players.CraftItemAsync(player.UserId, item.Id);

        if (craftResult == AddItemResult.Failed)
        {
            InsufficientResources(player, data, client, item);
            return;
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
    }
}