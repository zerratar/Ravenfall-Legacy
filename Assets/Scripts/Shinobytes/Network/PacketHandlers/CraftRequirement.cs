using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;


public class CraftRequirement : ChatBotCommandHandler<string>
{
    public CraftRequirement(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(inputQuery);

        if (item.Item == null && item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_CRAFT_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(gm, Localization.MSG_CRAFT_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        // 
        var i = item.Item;
        var recipe = Game.Items.GetItemRecipe(item.Item);
        if (recipe == null)
        {
            // check if it can be be dropped
            var drop = Game.Items.GetResourceDrop(item.Item);
            if (drop != null)
            {
                client.SendReply(gm, "{itemName} requires level {level} {skillName}.", item.Item.Name, drop.LevelRequirement.ToString(), drop.RequiredSkill.ToString());
                return;
            }

            if (EquipmentLevelRequirement(i) > 0)
            {
                client.SendReply(gm, "{itemName} requires {equipRequirements} to equip. This item cannot be created.", i.Name, GetEquipRequirements(i));
            }
            else
            {
                client.SendReply(gm, "{itemName} is not an item that can be created.", item.Item.Name);
            }
            return;
        }

        var name = GetSuitableName(recipe, item.Item);

        var skillname = recipe.RequiredSkill.ToString();
        var skilllevel = recipe.RequiredLevel.ToString();


        // check if its an equipable. if so, then we want to include it in the string.
        if (EquipmentLevelRequirement(i) > 0)
        {
            client.SendReply(gm, "{name} requires level {level} {skillname} and {requirements}. To equip this item you require {equipRequirements}", name, skilllevel, skillname,
                GetRecipeIngredientsString(player.Inventory, recipe), GetEquipRequirements(i));
        }
        else
        {
            client.SendReply(gm, "{name} requires level {level} {skillname} and {requirements}", name, skilllevel, skillname, GetRecipeIngredientsString(player.Inventory, recipe));
        }
    }

    private int EquipmentLevelRequirement(Item item)
    {
        return item.RequiredAttackLevel + item.RequiredSlayerLevel + item.RequiredDefenseLevel
            + item.RequiredAttackLevel + item.RequiredRangedLevel
            + item.RequiredMagicLevel;
    }

    private string GetSuitableName(ItemRecipe recipe, Item item)
    {
        if (string.IsNullOrEmpty(recipe.Name)) return item.Name;
        return recipe.Name;
    }

    private string GetEquipRequirements(Item item)
    {
        var requirements = new List<string>();
        if (item.RequiredAttackLevel > 0)
        {
            requirements.Add("Level " + item.RequiredAttackLevel + " Attack");
        }
        if (item.RequiredDefenseLevel > 0)
        {
            requirements.Add("Level " + item.RequiredDefenseLevel + " Defense");
        }
        if (item.RequiredRangedLevel > 0)
        {
            requirements.Add("Level " + item.RequiredRangedLevel + " Ranged");
        }
        if (item.RequiredMagicLevel > 0)
        {
            requirements.Add("Level " + item.RequiredMagicLevel + " Magic or Healing");
        }
        if (item.RequiredSlayerLevel > 0)
        {
            requirements.Add("Level " + item.RequiredSlayerLevel + " Slayer");
        }
        return string.Join(", ", requirements);
    }

    private string GetRecipeIngredientsString(Inventory inventory, ItemRecipe recipe)
    {
        return string.Join(", ", recipe.Ingredients.Select(x =>
        {
            var targetItem = Game.Items.Get(x.ItemId);
            var itemName = targetItem.Name;
            var stack = inventory.GetInventoryItemsByItemId(x.ItemId);
            var ownedAmount = 0L;
            if (stack != null && stack.Count > 0)
            {
                ownedAmount = stack.Sum(x => x.Amount);
            }

            return itemName + " " + ownedAmount + "/" + x.Amount;
        }));
    }
}
