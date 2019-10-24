using System;
using RavenNest.Models;

public class Craft : PacketHandler<CraftRequest>
{
    public Craft(GameManager game, GameServer server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(CraftRequest data, GameClient client)
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

            var status = Game.Crafting.CanCraftItem(player, category, type);
            switch (status)
            {
                case CraftValidationStatus.OK:
                    var item = Game.Crafting.CraftItem(player, category, type);
                    if (item != null)
                    {
                        player.AddItem(item);

                        if (player.EquipIfBetter(item))
                        {
                            client.SendCommand(data.Player.Username, "craft_success", $"You crafted and equipped a {item.Name}!");
                        }
                        else
                        {
                            client.SendCommand(data.Player.Username, "craft_success", $"You crafted a {item.Name}!");
                        }
                        return;
                    }
                    else
                    {
                        client.SendCommand(data.Player.Username, "craft_failed", "Craft failed, you found a bug. :(");
                    }
                    break;
                case CraftValidationStatus.NotCraftable:
                    client.SendCommand(data.Player.Username, "craft_failed", $"{category} {type} cannot be crafted right now.");
                    return;
                case CraftValidationStatus.NeedCraftingStation:
                    client.SendCommand(data.Player.Username, "craft_failed", "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting");
                    return;
                case CraftValidationStatus.NotEnoughSkill:
                    {

                        client.SendCommand(data.Player.Username, "craft_failed", "You can't craft anything better right now. !train crafting to improve your crafting level.");
                    }
                    return;
                case CraftValidationStatus.NotEnoughResources:
                    //var cost = Game.Crafting.GetRREQ(player, category, type);
                    //client.SendCommand(data.Player.Username, "craft_failed", $"You need to have at least {cost.Wood} wood and {cost.Ore} ores to craft this item.");

                    var craftingItem = Game.Crafting.GetCraftableItemForPlayer(player, category, type);
                    if (craftingItem != null)
                    {
                        client.SendCommand(data.Player.Username, "craft_failed",
                            $"You need to have at least {Utility.FormatValue(craftingItem.WoodCost)} wood and {Utility.FormatValue(craftingItem.OreCost)} ores to craft this item.");
                    }
                    else
                    {
                        client.SendCommand(data.Player.Username, "craft_failed", $"Insufficient resources to craft this item.");
                    }
                    return;
            }
            return;
        }

        client.SendCommand(data.Player.Username, "craft_failed", $"{data.Category} is not a craftable item type. Supported types are {string.Join(", ", categories)}");
    }
}