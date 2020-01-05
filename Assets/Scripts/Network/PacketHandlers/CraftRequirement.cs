using RavenNest.Models;
using System.Linq;
using System.Text;

public class CraftRequirement : PacketHandler<TradeItemRequest>
{
    public CraftRequirement(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(
                data.Player, "You have to play the game to valuate items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to valuate the item right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendMessage(player, "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        var msg = GetItemCraftingRequirements(player, client, item.Item);
        client.SendMessage(player, msg);
    }

    private string GetItemCraftingRequirements(PlayerController player, GameClient client, Item item)
    {
        var requiredItemsStr = new StringBuilder();
        if (item != null)
        {
            requiredItemsStr.Append("You need ");
            if (item.WoodCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Wood)}/{Utility.FormatValue(item.WoodCost)} Wood, ");
            }

            if (item.OreCost > 0)
            {
                requiredItemsStr.Append($"{Utility.FormatValue(player.Resources.Ore)}/{Utility.FormatValue(item.OreCost)} Ore, ");
            }

            foreach (var req in item.CraftingRequirements)
            {
                var requiredItem = Game.Items.Get(req.ResourceItemId);
                var ownedNumber = 0L;
                var items = player.Inventory.GetInventoryItems(req.ResourceItemId);
                if (items != null)
                {
                    ownedNumber = (long)items.Sum(x => x.Amount);
                }
                requiredItemsStr.Append($"{Utility.FormatValue(ownedNumber)}/{Utility.FormatValue(req.Amount)} {requiredItem.Name}, ");
            }
            requiredItemsStr.Append($"crafting level {item.RequiredCraftingLevel} ");
            requiredItemsStr.Append("to craft " + item.Name);
        }
        return requiredItemsStr.ToString();
    }
}
