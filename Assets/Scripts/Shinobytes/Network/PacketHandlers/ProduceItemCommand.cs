using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract class ProduceItemCommand : ChatBotCommandHandler<string>
{
    public const int MaxCraftingCount = 50_000_000;
    private readonly TaskType itemProductionSkill;
    protected IItemResolver itemResolver;

    public ProduceItemCommand(
        TaskType itemProductionSkill,
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
        this.itemProductionSkill = itemProductionSkill;
    }

    public override void Handle(string query, GameMessage gm, GameClient client)
    {
        if (!Game.RavenNest.Tcp.IsReady)
        {
            client.SendReply(gm, "This action can only be done while connected to the server.");
            return;
        }

        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (player.Ferry && player.Ferry.Active)
        {
            client.SendReply(gm, Localization.MSG_GENERIC_FAILED_FERRY);
            return;
        }

        if (player.Arena.InArena || player.Raid.InRaid || player.Dungeon.InDungeon || player.Duel.InDuel || player.Onsen.InOnsen)
        {
            if (player.Arena.InArena) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_ARENA);
            if (player.Raid.InRaid) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_RAID);
            if (player.Dungeon.InDungeon) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_DUNGEON);
            if (player.Duel.InDuel) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_DUEL);
            if (player.Onsen.InOnsen) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_RESTING);
            return;
        }

        if (player.GetTask() != itemProductionSkill)
        {
            player.SetTask(itemProductionSkill);
        }

        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        var target = itemResolver.ResolveItemAndAmount(query);
        if (target.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, query, string.Join(", ", target.SuggestedItemNames));
            return;
        }

        if (target.Item == null)
        {
            client.SendReply(gm, Localization.MSG_GENERIC_ITEM_NOT_FOUND, query);
            return;
        }

        var amountToCraft = Math.Min(MaxCraftingCount, target.Count);
        var amount = amountToCraft > int.MaxValue ? int.MaxValue : (int)amountToCraft;

        var recipe = Game.Items.GetItemRecipe(target.Item);
        if (recipe == null || recipe.RequiredSkill != GetSkill(itemProductionSkill))
        {
            // item does not have a recipe or is not for cooking. 
            // "{itemName} is not an item that can be created."
            // "{itemName} is not an item that can be created using {skillName}. It requires {requiredSkillName}"

            if (recipe == null)
            {
                client.SendReply(gm, "{itemName} is not an item that can be created.", target.Item.Name);
                return;
            }

            client.SendReply(gm, "{itemName} is not an item that can be created using {skillName}. It requires level {requiredSkillLevel} {requiredSkill}",
                target.Item.Name,
                itemProductionSkill.ToString(),
                recipe.RequiredLevel.ToString(),
                recipe.RequiredSkill.ToString());

            return;
        }


        var result = TryProduceItem(player, target.Item, amount, recipe, gm, client);
        var name = GetSuitableName(recipe, target.Item);
        switch (result)
        {
            case Result.NotEnoughSkill:
                client.SendReply(gm, "{name} requires level {requiredSkillLevel} {requiredSkill}",
                    name, recipe.RequiredLevel.ToString(), recipe.RequiredSkill.ToString());
                break;

            case Result.NotEnoughResources:
                client.SendReply(gm, "You do not have enough resources for creating {name}. You need {requirements}", name, GetRecipeIngredientsString(player.Inventory, recipe));
                break;
            case Result.ServerError:
                client.SendReply(gm, "Server returned an error, please try again later.");
                break;
        }
    }

    private string GetSuitableName(ItemRecipe recipe, Item item)
    {
        if (string.IsNullOrEmpty(recipe.Name)) return item.Name;
        return recipe.Name;
    }

    private Skill GetSkill(TaskType itemProductionSkill)
    {
        switch (itemProductionSkill)
        {
            case TaskType.Alchemy: return Skill.Alchemy;
            case TaskType.Cooking: return Skill.Cooking;
            case TaskType.Crafting: return Skill.Crafting;
        }
        return Skill.None;
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

    protected string GetQuantityForm(int amount, string name)
    {
        var c = name.ToLower().Trim()[0];
        var grammarForm = "a";
        if (c == 'a' || c == 'i' || c == 'u' || c == 'e' || c == 'o') grammarForm = "an";
        if (amount > 1)
        {
            grammarForm = amount.ToString();
        }
        return grammarForm;
    }

    protected async Task ProduceAsync(PlayerController player, ItemRecipe recipe, int amount, GameMessage message, GameClient client, string[] terms)
    {
        ItemProductionResult result = await Game.RavenNest.Players.ProduceItemAsync(player.Id, recipe.Id, amount);

        if (!result.Success)
        {
            client.SendReply(message, "Unable to " + terms[0] + " {recipeName}. Server returned an error. Please try again later.", recipe.Name);
            return;
        }

        if (result.Items == null || result.Items.Count == 0)
        {
            client.SendReply(message, "Unable to " + terms[0] + " {recipeName}. Could you be missing some ingredients?", recipe.Name);
            return;
        }

        // get total amount of items that was crafted.
        var producedItemAmount = result.Items.Sum(x => x.Amount);

        // remove the ingredients from the player inventory
        foreach (var req in recipe.Ingredients)
        {
            var a = req.Amount * producedItemAmount;
            var stacks = player.Inventory.GetInventoryItemsByItemId(req.ItemId);

            foreach (GameInventoryItem stack in stacks)
            {
                if (stack.Amount < a)
                {
                    var toRemove = a - stack.Amount;
                    player.Inventory.Remove(stack, toRemove);
                    a -= (int)toRemove;
                }

                if (stack.Amount >= a)
                {
                    player.Inventory.Remove(stack, a);
                }
            }
        }

        // Display the result, if we only have one result that means we either failed or succeeded. 
        if (result.Items.Count == 1)
        {
            var producedItem = result.Items[0];
            var item = Game.Items.Get(producedItem.ItemId);
            if (producedItem.Success)
            {
                client.SendReply(message, "You have successfully " + terms[1] + " the {recipeName}! You've received {quantity} {itemName}",
                        recipe.Name, GetQuantityForm(producedItem.Amount, item.Name), item.Name);
            }
            else
            {
                client.SendReply(message, "Oh no! You have failed to " + terms[0] + " the {recipeName}! You've received {quantity} {itemName}",
                    recipe.Name, GetQuantityForm(producedItem.Amount, item.Name), item.Name);
            }

            player.Inventory.AddToBackpack(item, producedItem.Amount);
            return;
        }

        // we have more than 1 items, that means we both succeeded and failed. But it could also be a suprise item, so we will join all messages
        // to simplify things, lets give a generic message.

        var msg = "You have " + terms[1] + " {quantity} {recipeName} and received";
        var args = new List<object>
        {
            GetQuantityForm(producedItemAmount, recipe.Name), recipe.Name
        };

        for (int i = 0; i < result.Items.Count; i++)
        {
            ItemProductionResultItem producedItem = result.Items[i];
            var item = Game.Items.Get(producedItem.ItemId);
            msg = msg.Trim();
            msg += " {quantity" + i + "} {itemName" + i + "},";
            args.Add(GetQuantityForm(producedItem.Amount, item.Name));
            args.Add(item.Name);

            player.Inventory.AddToBackpack(item, producedItem.Amount);
        }

        msg = msg.Trim(',');
        client.SendReply(message, msg, args.ToArray());
    }

    protected bool HasMissingIngredients(Inventory inventory, List<ItemRecipeIngredient> ingredients)
    {
        foreach (var ingredient in ingredients)
        {
            if (!inventory.Contains(ingredient.ItemId, ingredient.Amount))
            {
                return true;
            }
        }

        return false;
    }

    protected abstract Result TryProduceItem(PlayerController player, Item item, int amount, ItemRecipe recipe, GameMessage message, GameClient client);

    public enum Result
    {
        OK,
        NotEnoughSkill,
        NotEnoughResources,
        ServerError
    }
}
