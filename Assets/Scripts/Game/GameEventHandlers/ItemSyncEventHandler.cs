using RavenNest.Models;

public class ItemSyncEventHandler : GameEventHandler<ItemSync>
{
    public override void Handle(GameManager gameManager, ItemSync data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player)
        {
            return;
        }

        player.Inventory.SyncItems(data);

        //if (item.Category != ItemCategory.Resource)
        //{
        //    player.EquipIfBetter(result);
        //    player.Inventory.EquipAll();
        //}
    }
}
