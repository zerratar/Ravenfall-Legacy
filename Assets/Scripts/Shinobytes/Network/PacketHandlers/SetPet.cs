using RavenNest.Models;
using System.Linq;

public class SetPet : ChatBotCommandHandler<string>
{
    public SetPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var itemQuery = inputQuery;
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveInventoryItem(player, itemQuery + " pet", 5, EquippedState.NotEquipped);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, itemQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.Item.Type != ItemType.Pet)
        {
            client.SendReply(gm, Localization.MSG_SET_PET_NOT_PET, item.Item.Name);
            return;
        }

        var invItem = item.InventoryItem;
        if (invItem == null)
        {
            client.SendReply(gm, Localization.MSG_SET_PET_NOT_OWNED, item.Item.Name);
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if (equippedPet == null || equippedPet.ItemId != item.Id)
        {
            await player.EquipAsync(item.Item);
        }

        client.SendReply(gm, Localization.MSG_SET_PET, item.Item.Name);
    }
}
