using RavenNest.Models;
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

            client.SendReply(gm, "{itemName} is not an item that can be created.", item.Item.Name);
            return;
        }

        var name = GetSuitableName(recipe, item.Item);

        var skillname = recipe.RequiredSkill.ToString();
        var skilllevel = recipe.RequiredLevel.ToString();

        client.SendReply(gm, "{name} requires level {level} {skillname} and {requirements}", name, skilllevel, skillname, GetRecipeIngredientsString(player.Inventory, recipe));
    }

    private string GetSuitableName(ItemRecipe recipe, Item item)
    {
        if (string.IsNullOrEmpty(recipe.Name)) return item.Name;
        return recipe.Name;
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
