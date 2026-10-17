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
        if (!Game.RavenNest.ServerReady)
        {
            client.SendReply(gm, Localization.MSG_PRODUCE_NOT_CONNECTED);
            return;
        }

        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (player.ferryHandler && player.ferryHandler.Active)
        {
            client.SendReply(gm, Localization.MSG_GENERIC_FAILED_FERRY);
            return;
        }

        if (player.arenaHandler.InArena || player.raidHandler.InRaid || player.dungeonHandler.InDungeon || player.duelHandler.InDuel || player.onsenHandler.InOnsen)
        {
            if (player.arenaHandler.InArena) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_ARENA);
            if (player.raidHandler.InRaid) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_RAID);
            if (player.dungeonHandler.InDungeon) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_DUNGEON);
            if (player.duelHandler.InDuel) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_DUEL);
            if (player.onsenHandler.InOnsen) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_RESTING);
            return;
        }

        var q = query?.ToLower().Trim();
        if (q == "status")
        {
            // if we have an active scheduled action for this skill, return the status.
            if (player.ScheduledAction != null)
            {
                if (player.ScheduledAction.State is not ItemProductionState state)
                {
                    return;
                }

                // TODO: include details such as state.AmountLeftToCraft, expected total time, time for next item, etc.

                var timeLeft = player.ScheduledAction.ExecuteTime - DateTime.UtcNow;
                if (timeLeft.TotalSeconds <= 0)
                {
                    client.SendReply(gm, Localization.MSG_PRODUCE_STATUS_ALMOST_DONE);
                    return;
                }

                client.SendReply(gm, Localization.MSG_PRODUCE_STATUS, Utility.FormatTime(timeLeft));
                return;
            }

            client.SendReply(gm, Localization.MSG_PRODUCE_NO_ACTION);
            return;
        }

        if (q == "cancel" || q == "abort")
        {
            if (player.ScheduledAction == null)
            {
                client.SendReply(gm, Localization.MSG_PRODUCE_NO_ACTION_TO_CANCEL);
                return;
            }

            if (player.ScheduledAction.State is not ItemProductionState state)
            {
                // we should not interrupt a non item production action.
                return;
            }

            player.InterruptAction();
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

        // unique case, we should allow cooking raw ingredients such as fish, meat, etc.
        if (recipe == null && itemProductionSkill == TaskType.Cooking)
        {
            // does not necessarily have to be only one recipe, but in this game it is. luckily :)
            recipe = Game.Items.GetRecipeWithSingleIngredient(target.Item);
        }

        if (recipe == null || recipe.RequiredSkill != GetSkill(itemProductionSkill))
        {
            // item does not have a recipe or is not for cooking. 
            // "{itemName} is not an item that can be created."
            // "{itemName} is not an item that can be created using {skillName}. It requires {requiredSkillName}"

            if (recipe == null)
            {
                client.SendReply(gm, Localization.MSG_PRODUCE_NOT_CREATABLE, target.Item.Name);
                return;
            }

            client.SendReply(gm, Localization.MSG_PRODUCE_WRONG_SKILL,
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
                client.SendReply(gm, Localization.MSG_PRODUCE_LEVEL_TOO_LOW,
                    name, recipe.RequiredLevel.ToString(), recipe.RequiredSkill.ToString());
                break;

            case Result.NotEnoughResources:
                client.SendReply(gm, Localization.MSG_PRODUCE_NOT_ENOUGH_RESOURCES, name, GetRecipeIngredientsString(player.Inventory, recipe));
                break;
            case Result.ServerError:
                client.SendReply(gm, Localization.MSG_PRODUCE_SERVER_ERROR);
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

    //         in the future, we can make it so that items that take less than a second to make can be combined to
    //         be done together so there are not just 1 request per item being made.
    //   Example: recipe.PreparationTime<1, Amount = Min(Amount, 1/recipe.PreparationTime)


    public static string GetQuantityForm(long amount, string name)
    {
        var uncountableItems = new HashSet<string>
        {
            "yarrow", "cacao", "yeast", "burned perch", "burned beef",
            "burned bass", "turmeric", "lemon balm", "wormwood", "garlic",
            "sand", "sage", "coriander", "sugar", "wood pulp", "water",
            "mugwort", "chocolate", "cinnamon", "cheese", "onion",
            "black pepper", "butter", "flour", "bread", "resin",
            "lavender", "tomato", "coal", "wood plank"
        };

        string itemNameLower = name.ToLower().Trim();

        if (amount == 1)
        {
            if (uncountableItems.Contains(itemNameLower)) return string.Empty;

            switch (itemNameLower[0])
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    return Localization.MSG_PRODUCE_QUANTITY_AN;
                default:
                    return Localization.MSG_PRODUCE_QUANTITY_A;
            }
        }
        return amount.ToString();
    }

    // The verbs are read when they are used, since Languages replaces the Localization fields
    // after this dictionary has been created. Each term is { verb, past participle }.
    public static readonly Dictionary<string, Func<string[]>> CustomTerms = new Dictionary<string, Func<string[]>>
    {
        { "butter", () => Localization.MSG_PRODUCE_TERMS_CHURN },
        { "red wine", () => Localization.MSG_PRODUCE_TERMS_FERMENT },
        { "cheese", () => Localization.MSG_PRODUCE_TERMS_MAKE },
        { "yarrow", () => Localization.MSG_PRODUCE_TERMS_PICK },
        { "cacao", () => Localization.MSG_PRODUCE_TERMS_HARVEST },
        { "yeast", () => Localization.MSG_PRODUCE_TERMS_CULTIVATE },
        { "turmeric", () => Localization.MSG_PRODUCE_TERMS_GRIND },
        { "lemon balm", () => Localization.MSG_PRODUCE_TERMS_PLUCK },
        { "wormwood", () => Localization.MSG_PRODUCE_TERMS_GATHER },
        { "garlic", () => Localization.MSG_PRODUCE_TERMS_HARVEST },
        { "sand", () => Localization.MSG_PRODUCE_TERMS_COLLECT },
        { "sugar", () => Localization.MSG_PRODUCE_TERMS_REFINE },
        { "wood pulp", () => Localization.MSG_PRODUCE_TERMS_PROCESS },
        { "water", () => Localization.MSG_PRODUCE_TERMS_FETCH },
        { "chocolate", () => Localization.MSG_PRODUCE_TERMS_MOLD },
        { "cinnamon", () => Localization.MSG_PRODUCE_TERMS_GRIND },
        { "onion", () => Localization.MSG_PRODUCE_TERMS_CHOP },
        { "black pepper", () => Localization.MSG_PRODUCE_TERMS_GRIND },
        { "flour", () => Localization.MSG_PRODUCE_TERMS_MILL },
        { "bread", () => Localization.MSG_PRODUCE_TERMS_BAKE },
        { "resin", () => Localization.MSG_PRODUCE_TERMS_EXTRACT },
        { "lavender", () => Localization.MSG_PRODUCE_TERMS_PICK },
        { "tomato", () => Localization.MSG_PRODUCE_TERMS_HARVEST },
        { "coal", () => Localization.MSG_PRODUCE_TERMS_MINE },
        { "wood plank", () => Localization.MSG_PRODUCE_TERMS_CUT },
        { "tome of teleportation", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of home", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of away", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of ironhill", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of kyo", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of heim", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of atria", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE },
        { "tome of eldara", () => Localization.MSG_PRODUCE_TERMS_INSCRIBE }
    };

    public async Task ProduceAsync(ItemProductionState state)
    {
        var player = state.Player;
        var recipe = state.Recipe;
        var amount = state.Amount;
        var message = state.Message;
        var client = state.Client;

        string[] terms;
        if (CustomTerms.ContainsKey(recipe.Name.ToLower()))
        {
            terms = CustomTerms[recipe.Name.ToLower()]();
        }
        else if (state.Recipe.RequiredSkill == Skill.Cooking)
        {
            terms = Localization.MSG_PRODUCE_TERMS_COOK;
        }
        else if (state.Recipe.RequiredSkill == Skill.Alchemy)
        {
            terms = Localization.MSG_PRODUCE_TERMS_BREW;
        }
        else
        {
            terms = Localization.MSG_PRODUCE_TERMS_CRAFT; // default
        }

        var result = await Game.RavenNest.Players.ProduceItemAsync(player.Id, recipe.Id, amount);

        if (!result.Success)
        {
            state.Continue = false;
            client.SendReply(message, Localization.MSG_PRODUCE_FAILED_SERVER, terms[0], recipe.Name);
            return;
        }

        if (result.Items == null || result.Items.Count == 0)
        {
            state.Continue = false;
            client.SendReply(message, Localization.MSG_PRODUCE_FAILED_INGREDIENTS, terms[0], recipe.Name);
            return;
        }

        // get total amount of items that was crafted.
        var producedItemAmount = result.Items.Sum(x => x.Amount);

        // TODO: This may cause a bug where inventory is out of sync if we remove items based on what it should cost
        //       instead of what the server changed. Server may have updated the stash and not removed items from inventory.
        //       This is a rare case, but it can happen.
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

        state.AmountLeftToCraft -= producedItemAmount;
        state.ProducedItems.AddRange(result.Items);
        state.Continue = state.AmountLeftToCraft > 0;

        if (player.Island)
        {
            var count = result.Items.Where(x => x.Success).Sum(x => x.Amount);
            switch (state.Recipe.RequiredSkill)
            {
                case Skill.Cooking:
                    player.Island.Statistics.FoodCooked += count;
                    break;
                case Skill.Alchemy:
                    player.Island.Statistics.PotionsBrewed += count;
                    break;
                default:
                    player.Island.Statistics.ItemsCrafted += count;
                    break;
            }
        }

        // Display the result, if we only have one result that means we either failed or succeeded. 
        if (result.Items.Count == 1)
        {
            var producedItem = result.Items[0];
            var item = Game.Items.Get(producedItem.ItemId);
            if (producedItem.Success)
            {
                if (recipe.Name.ToLower() == item.Name.ToLower())
                {
                    client.SendReply(message, Localization.MSG_PRODUCE_SUCCESS, terms[1], GetQuantityForm(producedItem.Amount, item.Name), item.Name);
                }
                else
                {
                    client.SendReply(message, Localization.MSG_PRODUCE_SUCCESS_RECEIVED,
                            terms[1], recipe.Name, GetQuantityForm(producedItem.Amount, item.Name), item.Name);
                }
            }
            else
            {
                client.SendReply(message, Localization.MSG_PRODUCE_FAILED,
                    terms[0], recipe.Name, GetQuantityForm(producedItem.Amount, item.Name), item.Name);
            }

            player.Inventory.AddToBackpack(producedItem.InventoryItemId, item, producedItem.Amount);
            return;
        }

        // we have more than 1 items, that means we both succeeded and failed. But it could also be a suprise item, so we will join all messages
        // to simplify things, lets give a generic message.

        var receivedItems = new List<string>();

        var inventory = player.Inventory;

        for (int i = 0; i < result.Items.Count; i++)
        {
            var producedItem = result.Items[i];
            var item = Game.Items.Get(producedItem.ItemId);
            receivedItems.Add((GetQuantityForm(producedItem.Amount, item.Name) + " " + item.Name).Trim());

            var existing = inventory.GetInventoryItem(producedItem.InventoryItemId);
            if (existing != null)
            {
                existing.Amount += producedItem.Amount;
                continue;
            }

            inventory.AddOrSetInventoryItem(new InventoryItem
            {
                Id = producedItem.InventoryItemId,
                Amount = producedItem.StackAmount,
                ItemId = producedItem.ItemId
            });
        }

        client.SendReply(message, Localization.MSG_PRODUCE_MIXED_RESULT,
            terms[1], GetQuantityForm(producedItemAmount, recipe.Name), recipe.Name, string.Join(", ", receivedItems));
    }

    protected bool HasMissingIngredients(Inventory inventory, List<ItemRecipeIngredient> ingredients)
    {
        foreach (var ingredient in ingredients)
        {
            var stack = inventory.GetInventoryItemsByItemId(ingredient.ItemId);
            var ownedAmount = 0L;
            if (stack != null && stack.Count > 0) ownedAmount = stack.Sum(x => x.Amount);
            if (ownedAmount < ingredient.Amount) return true;
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
