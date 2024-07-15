using RavenNest.Models;
using System.Linq;

public class ItemRemoveByCategoryHandler : GameEventHandler<ItemRemoveByCategory>
{
    public override void Handle(GameManager gameManager, ItemRemoveByCategory data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player)
        {
            return;
        }

        var items = player.Inventory.GetInventoryItems();
        foreach (var invItem in items)
        {
            if (data.Exclude != null && data.Exclude.Contains(invItem.Id))
            {
                continue;
            }

            var item = gameManager.Items.Get(invItem.ItemId);
            if (item == null)
            {
                continue;
            }

            if (data.Filter != ItemFilter.All)
            {
                var filter = ItemFilterExtensions.GetItemFilter(item);
                if (filter != data.Filter)
                {
                    continue;
                }
            }

            player.Inventory.RemoveByInventoryId(invItem.Id, invItem.Amount);
        }
    }
}