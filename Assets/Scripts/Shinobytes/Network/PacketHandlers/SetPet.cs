using RavenNest.Models;

public class SetPet : ChatBotCommandHandler<SetPetRequest>
{
    public SetPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(SetPetRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.Pet, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            queriedItem = itemResolver.Resolve(data.Pet + " pet", parsePrice: false, parseUsername: false, parseAmount: false);
        }

        if (queriedItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.Pet);
            return;
        }

        if (queriedItem.Item.Type != ItemType.Pet)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_PET, queriedItem.Item.Name);
            return;
        }

        var item = player.Inventory.GetInventoryItems(queriedItem.Id);
        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if ((item == null || item.Count == 0))
        {
            if (equippedPet == null || equippedPet.ItemId != queriedItem.Item.ItemId)
            {
                client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_OWNED, queriedItem.Item.Name);
                return;
            }
        }

        if (equippedPet == null || equippedPet.ItemId != queriedItem.Item.ItemId)
        {
            await player.EquipAsync(queriedItem.Item.Item);
        }

        client.SendMessage(data.Player.Username, Localization.MSG_SET_PET, queriedItem.Item.Name);
    }
}
