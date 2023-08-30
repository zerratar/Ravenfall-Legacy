using RavenNest.Models;

public class Cook : ProduceItemCommand
{
    public Cook(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(TaskType.Cooking, game, server, playerManager)
    {
    }

    protected override Result TryProduceItem(PlayerController player, Item item, int amount, ItemRecipe recipe, GameMessage message, GameClient client)
    {
        // check if our level is high enough
        if (recipe.RequiredLevel > player.Stats.Cooking.Level)
        {
            return Result.NotEnoughSkill;
        }

        // check if we have enough resources

        if (HasMissingIngredients(player.Inventory, recipe.Ingredients))
        {
            return Result.NotEnoughResources;
        }

        player.BeginInterruptableAction(
            action: async () => await ProduceAsync(player, recipe, amount, message, client, new string[] { "cook", "cooked" }),
            onInterrupt: () => client.SendReply(message, Localization.MSG_COOK_CANCEL),
            recipe.PreparationTime);

        return Result.OK;
    }
}