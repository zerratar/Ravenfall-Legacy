﻿using RavenNest.Models;

public class ItemRemoveEventHandler : GameEventHandler<ItemRemove>
{
    public override void Handle(GameManager gameManager, ItemRemove data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Shinobytes.Debug.Log("No player with userid " + data.UserId + " when removing item. (" + data.ItemId + ", " + data.Amount + ")");
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

public class ItemEquipEventHandler : GameEventHandler<ItemEquip>
{
    public override void Handle(GameManager gameManager, ItemEquip data)
    {

        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Shinobytes.Debug.Log("No player with userid " + data.UserId + " when trying to equip item. (" + data.InventoryItemId + ")");
            return;
        }

        player.Inventory.Equip(data.InventoryItemId);
    }
}

public class ItemUnequipEventHandler : GameEventHandler<ItemEquip>
{
    public override void Handle(GameManager gameManager, ItemEquip data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Shinobytes.Debug.Log("No player with userid " + data.UserId + " when trying to unequip item. (" + data.InventoryItemId + ")");
            return;
        }

        player.Inventory.Unequip(data.InventoryItemId);
    }
}