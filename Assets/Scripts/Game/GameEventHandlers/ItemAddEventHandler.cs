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

        var result = player.Inventory.AddToBackpack(data);

        if (item.Category != ItemCategory.Resource)
        {
            player.EquipIfBetter(result);
            player.Inventory.EquipAll();
        }
    }
}
