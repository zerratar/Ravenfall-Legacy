﻿using RavenNest.Models;

public class ItemRemoveEventHandler : GameEventHandler<ItemRemove>
{
    protected override void Handle(GameManager gameManager, ItemRemove data)
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

        player.Inventory.RemoveByInventoryId(data.InventoryItemId, data.Amount);
    }
}