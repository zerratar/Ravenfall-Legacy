using RavenNest.Models;

public class Brew : ProduceItemCommand
{
    public Brew(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(TaskType.Alchemy, game, server, playerManager)
    {
    }

    protected override Result TryProduceItem(PlayerController player, Item item, int amount, ItemRecipe recipe, GameMessage message, GameClient client)
    {
        // check if our level is high enough
        if (recipe.RequiredLevel > player.Stats.Alchemy.Level)
        {
            return Result.NotEnoughSkill;
        }

        // check if we have enough resources
        if (HasMissingIngredients(player.Inventory, recipe.Ingredients))
        {
            return Result.NotEnoughResources;
        }

        player.BeginInterruptableAction(
            action: async () => await ProduceAsync(player, recipe, amount, message, client, new string[] { "brew", "brewed" }),
            onInterrupt: () => client.SendReply(message, Localization.MSG_BREW_CANCEL),
            recipe.PreparationTime);

        return Result.OK;
    }
}