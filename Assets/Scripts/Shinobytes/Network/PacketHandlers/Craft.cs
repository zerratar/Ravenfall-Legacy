using RavenNest.Models;

public class Craft : ProduceItemCommand
{
    public Craft(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(TaskType.Crafting, game, server, playerManager)
    {
    }

    protected override Result TryProduceItem(PlayerController player, Item item, int amount, ItemRecipe recipe, GameMessage message, GameClient client)
    {
        // check if our level is high enough
        if (recipe.RequiredLevel > player.Stats.Crafting.MaxLevel)
        {
            return Result.NotEnoughSkill;
        }

        // check if we have enough resources
        if (HasMissingIngredients(player.Inventory, recipe.Ingredients))
        {
            return Result.NotEnoughResources;
        }

        // note: we should not be producing all items at once. We should chain the interruptable action
        //       so that the item we produced is finished we will go on with the next. However this will spam
        //       the chat unless we decide to give a result after "cancel" or "finished". But for now, finish all at once.
        //       it will be more beneficial for players to craft many at a time to save time for now.
        // plus.. We wont have these preparationtime in place yet. It will be later.

        // TODO:
        // but we can keep the state so we can keep track on how things are going and report back to the player with the result.


        player.BeginInterruptableAction(
            new ItemProductionState(player, recipe, amount, message, client),
            // action: async state => { if (state.Continue) { await ProduceAsync(state, ...) } else { report result }  }
            //         in the future, we can make it so that items that take less than a second to make can be combined to
            //         be done together so there are not just 1 request per item being made.
            action: async state => await ProduceAsync(state, new string[] { "craft", "crafted" }),
            onInterrupt: state => client.SendReply(message, Localization.MSG_CRAFT_CANCEL),
            recipe.PreparationTime,
            "Crafting " + recipe.Name,
            TaskType.Crafting);

        return Result.OK;
    }
}