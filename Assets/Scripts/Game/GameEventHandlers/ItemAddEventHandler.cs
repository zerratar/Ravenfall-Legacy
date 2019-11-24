using RavenNest.Models;
using UnityEngine;

public class ItemAddEventHandler : GameEventHandler<ItemAdd>
{
    protected override void Handle(GameManager gameManager, ItemAdd data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (!player)
        {
            Debug.Log("No player with userid " + data.UserId + " when adding item.");
            return;
        }
        var item = gameManager.Items.GetItem(data.ItemId);
        if (item == null)
        {
            Debug.Log("No item with id " + data.ItemId + " was found.");
            return;
        }

        player.AddItem(item, false);

        gameManager.Server.Client.SendCommand(player.PlayerName, "item_pickup", $"You found a {item.Name}!");
    }
}
