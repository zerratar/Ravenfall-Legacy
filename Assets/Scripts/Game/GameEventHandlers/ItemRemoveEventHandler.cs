using RavenNest.Models;

public class ItemRemoveEventHandler : GameEventHandler<ItemRemove>
{
    public override void Handle(GameManager gameManager, ItemRemove data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player)
        {
            return;
        }

        var item = gameManager.Items.Get(data.ItemId);
        if (item == null)
        {
            Shinobytes.Debug.Log("No item with id " + data.ItemId + " was found.");
            return;
        }

        player.Inventory.RemoveByInventoryId(data.InventoryItemId, data.Amount);
    }
}
