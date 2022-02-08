using RavenNest.Models;
using UnityEngine;

public class ItemAddEventHandler : GameEventHandler<ItemAdd>
{
    protected override void Handle(GameManager gameManager, ItemAdd data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Shinobytes.Debug.Log("No player with userid " + data.UserId + " when adding item.");
            return;
        }

        var item = gameManager.Items.GetItem(data.ItemId);
        if (item == null)
        {
            Shinobytes.Debug.Log("No item with id " + data.ItemId + " was found.");
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
