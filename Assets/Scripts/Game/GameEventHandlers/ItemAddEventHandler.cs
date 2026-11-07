using Assets.Scripts;
using RavenNest.Models;
using UnityEngine;

public class ItemAddEventHandler : GameEventHandler<ItemAdd>
{
    public override void Handle(GameManager gameManager, ItemAdd data)
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

        if (player.Inventory.FedByServer)
        {
            // the feed brings the stack (server authority); gear goes on once it is here
            if (item.Category != ItemCategory.Resource) player.EquipIfBetterWhenArrived(data.InventoryItemId, equipAll: true);
            return;
        }

        var result = player.Inventory.AddToBackpack(data);

        if (item.Category != ItemCategory.Resource)
        {
            player.EquipIfBetter(result);
            player.Inventory.EquipAll();
        }
    }
}
