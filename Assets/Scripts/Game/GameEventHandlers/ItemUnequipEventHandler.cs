using RavenNest.Models;

public class ItemUnequipEventHandler : GameEventHandler<ItemEquip>
{
    public override void Handle(GameManager gameManager, ItemEquip data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player)
        {
            return;
        }

        player.Inventory.Unequip(data.InventoryItemId, true);
    }
}