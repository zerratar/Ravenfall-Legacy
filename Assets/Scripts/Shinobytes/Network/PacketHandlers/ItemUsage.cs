using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;

public class ItemUsage : ChatBotCommandHandler<string>
{
    public ItemUsage(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
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
            client.SendReply(gm, Localization.MSG_GENERIC_ITEM_NOT_FOUND, inputQuery);
            return;
        }
        var matchingRecipes = new List<ItemRecipe>();
        var recipes = Game.Items.GetItemRecipes();
        foreach (var recipe in recipes)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.ItemId == item.Id)
                {
                    matchingRecipes.Add(recipe);
                }
            }
        }

        if (matchingRecipes.Count == 0)
        {
            client.SendReply(gm, "{itemName} is not used in any recipes.", item.Item.Name);
            return;
        }

        var list = string.Join(", ", matchingRecipes.Select(x => x.Name));
        client.SendReply(gm, "{name} is used for: {list}", item.Item.Name, list);
    }
}