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

		if (data.Amount > 1)
		{
			gameManager.Server.Client.SendCommand(player.PlayerName, "item_pickup", $"You received {data.Amount} {item.Name}s!");
		}
		else
		{
			gameManager.Server.Client.SendCommand(player.PlayerName, "item_pickup", $"You received {Utility.GetDescriber(item.Name)} {item.Name}!");
		}
	}
}
