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

        if (player.arenaHandler.InArena || player.raidHandler.InRaid || player.IsAway || player.duelHandler.InDuel || player.onsenHandler.InOnsen)
        {
            if (player.arenaHandler.InArena) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_ARENA);
            if (player.raidHandler.InRaid) client.SendReply(gm, Localization.MSG_GENERIC_FAILED_RAID);
            if (player.IsAway) client.SendReply(gm, player.AwayReply(Localization.MSG_GENERIC_FAILED_DUNGEON));
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
        if (target == null || target.Item == null || target.SuggestedItemNames.Length > 0)
        {
            // nothing by that name: only things this skill makes are worth suggesting. !cook 18 cod answered "did you
            // mean Ham, Cat, Sand, Coal, Rope?" (any item with a short name), and !cook cat then "cannot be created".
            ReplyNotFound(query, gm, client);
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
        if (result == Result.NotEnoughResources)
        {
            // the game's copy of the inventory may be missing something the server has: check before saying no
            _ = RetryWithServerInventoryAsync(player, target.Item, amount, recipe, name, gm, client);
            return;
        }

        Reply(result, player, recipe, name, gm, client);
    }

    private void Reply(Result result, PlayerController player, ItemRecipe recipe, string name, GameMessage gm, GameClient client)
    {
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

    // when each character's inventory was last fetched for a failed craft: asking again and again does not ask the
    // server each time
    private static readonly Dictionary<Guid, DateTime> lastInventoryCheck = new Dictionary<Guid, DateTime>();
    private static readonly TimeSpan InventoryCheckInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The game said there was not enough of an ingredient. Its copy of the inventory can miss an item the server
    /// added (seen on stream: Phantom Ore 0/1 while the player had one, and !leave and !join fixed it), so the
    /// inventory is fetched from the server, the game's copy made to match, and the craft tried once more before
    /// the player is told no.
    /// </summary>
    private async Task RetryWithServerInventoryAsync(PlayerController player, Item item, int amount, ItemRecipe recipe, string name, GameMessage gm, GameClient client)
    {
        var now = DateTime.UtcNow;
        var checkedRecently = lastInventoryCheck.TryGetValue(player.Id, out var last) && now - last < InventoryCheckInterval;
        if (!checkedRecently)
        {
            lastInventoryCheck[player.Id] = now;
            try
            {
                var fresh = await Game.RavenNest.Players.GetPlayerAsync(player.Id);
                if (player && fresh?.InventoryItems != null)
                {
                    var changed = player.Inventory.SyncWithServer(fresh.InventoryItems);
                    if (changed > 0)
                    {
                        Shinobytes.Debug.LogWarning("Crafting found " + player.Name + "'s inventory out of step with the server: " + changed + " stack(s) repaired.");
                        var again = TryProduceItem(player, item, amount, recipe, gm, client);
                        if (again != Result.NotEnoughResources)
                        {
                            Reply(again, player, recipe, name, gm, client);
                            return;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Could not check " + player.Name + "'s inventory with the server: " + exc.Message);
            }
        }

        if (player) Reply(Result.NotEnoughResources, player, recipe, name, gm, client);
    }

    private void ReplyNotFound(string query, GameMessage gm, GameClient client)
    {
        // what was asked for, without the amount ("18 cod", "cod x18")
        var words = (query ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !long.TryParse(w.Trim('x', 'X'), out _)).ToArray();
        var asked = words.Length > 0 ? string.Join(" ", words) : (query ?? "").Trim();
        var verb = itemProductionSkill == TaskType.Cooking ? "cook" : itemProductionSkill == TaskType.Alchemy ? "brew" : "craft";

        var skill = GetSkill(itemProductionSkill);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recipes = Game.Items.GetItemRecipes();
        if (recipes != null)
        {
            foreach (var r in recipes)
            {
                if (r == null || r.RequiredSkill != skill) continue;
                var made = Game.Items.Get(r.ItemId);
                if (made != null && !string.IsNullOrEmpty(made.Name)) names.Add(made.Name);
            }
        }

        var key = asked.ToLowerInvariant();
        var close = names
            .Select(n => new { Name = n, Score = Closeness(key, n.ToLowerInvariant()) })
            .Where(x => x.Score < 100)
            .OrderBy(x => x.Score).ThenBy(x => x.Name.Length)
            .Take(5).Select(x => x.Name).ToArray();

        if (close.Length > 0) client.SendReply(gm, Localization.MSG_PRODUCE_NOT_FOUND_SUGGEST, asked, verb, string.Join(", ", close));
        else client.SendReply(gm, Localization.MSG_PRODUCE_NOT_FOUND, asked, verb);
    }

    // how near a name is to what was typed: 0 the same word in it, then the letters that differ; 100 not near at all
    private static int Closeness(string asked, string name)
    {
        if (asked.Length == 0) return 100;
        if (name.Contains(asked)) return 0;
        var best = 100;
        foreach (var word in name.Split(' '))
        {
            if (word.Length == 0) continue;
            if (asked.Contains(word) && word.Length >= 3) return 1;
            var d = EditDistance(asked, word);
            if (d <= Math.Max(1, Math.Min(asked.Length, word.Length) / 3)) best = Math.Min(best, 1 + d);
        }
        return best;
    }

    private static int EditDistance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
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

        // Without the server's inventory feed the game takes the ingredients itself, by what the recipe costs. That can
        // be wrong (the server may have taken them from the stash); with the feed the server's own numbers arrive.
        var fed = player.Inventory.FedByServer;
        foreach (var req in fed ? Enumerable.Empty<ItemRecipeIngredient>() : recipe.Ingredients)
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

            if (!fed) player.Inventory.AddToBackpack(producedItem.InventoryItemId, item, producedItem.Amount);
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
            if (fed) continue;

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
