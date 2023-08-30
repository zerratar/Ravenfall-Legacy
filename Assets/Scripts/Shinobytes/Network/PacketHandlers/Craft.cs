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
        if (recipe.RequiredLevel > player.Stats.Crafting.Level)
        {
            return Result.NotEnoughSkill;
        }

        // check if we have enough resources
        if (HasMissingIngredients(player.Inventory, recipe.Ingredients))
        {
            return Result.NotEnoughResources;
        }

        player.BeginInterruptableAction(
            action: async () => await ProduceAsync(player, recipe, amount, message, client, new string[] { "craft", "crafted" }),
            onInterrupt: () => client.SendReply(message, Localization.MSG_CRAFT_CANCEL),
            recipe.PreparationTime);

        return Result.OK;
    }
}