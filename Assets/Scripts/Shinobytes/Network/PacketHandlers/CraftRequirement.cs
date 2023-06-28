using RavenNest.Models;
using System.Linq;
using System.Text;

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

        var msg = GetItemCraftingRequirements(player, client, item.Item);
        client.SendReply(gm, msg);
    }

    private string GetItemCraftingRequirements(PlayerController player, GameClient client, Item item)
    {
        var requiredItemsStr = new StringBuilder();
        if (item != null)
        {
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatAmount(player.Resources.Wood)}/{Utility.FormatAmount(item.WoodCost)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatAmount(player.Resources.Ore)}/{Utility.FormatAmount(item.OreCost)} Ore, ");
            }

            foreach (var req in item.CraftingRequirements)
            {
                var requiredItem = Game.Items.Get(req.ResourceItemId);
                var ownedNumber = 0L;
                var items = player.Inventory.GetInventoryItemsByItemId(req.ResourceItemId);
                if (items != null)
                {
                    ownedNumber = (long)items.Sum(x => x.Amount);
                }
                requiredItemsStr.Append($"{Utility.FormatAmount(ownedNumber)}/{Utility.FormatAmount(req.Amount)} {requiredItem.Name}, ");
            }
            requiredItemsStr.Append($"crafting level {item.RequiredCraftingLevel} ");
            requiredItemsStr.Append("to craft " + item.Name);
        }
        return requiredItemsStr.ToString();
    }
}
