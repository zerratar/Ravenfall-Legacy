using RavenNest.Models;

public class ItemEquipEventHandler : GameEventHandler<ItemEquip>
{
    public override void Handle(GameManager gameManager, ItemEquip data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player)
        {
            return;
        }

        player.Inventory.Equip(data.InventoryItemId);
    }
}
