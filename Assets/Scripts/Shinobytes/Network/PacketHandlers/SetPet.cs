using RavenNest.Models;

public class SetPet : PacketHandler<SetPetRequest>
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

        var item = player.Inventory.GetInventoryItems(queriedItem.Item.Id);
        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if ((item == null || item.Count == 0))
        {
            if (equippedPet == null || equippedPet.Id != queriedItem.Item.Id)
            {
                client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_OWNED, queriedItem.Item.Name);
                return;
            }
        }

        if (equippedPet == null || equippedPet.Id != queriedItem.Item.Id)
        {
            await player.EquipAsync(queriedItem.Item);
        }

        client.SendMessage(data.Player.Username, Localization.MSG_SET_PET, queriedItem.Item.Name);
    }
}


public class EquipItem : PacketHandler<TradeItemRequest>
{
    public EquipItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(data.ItemQuery))
        {
            return;
        }

        if (data.ItemQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            await player.EquipBestItemsAsync();
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
            queriedItem = itemResolver.Resolve(data.ItemQuery + " pet", parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = player.Inventory.GetInventoryItems(queriedItem.Item.Id);
        if (item == null || item.Count == 0)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.Item))
        {
            client.SendMessage(data.Player.Username, Localization.MSG_EQUIPPED, queriedItem.Item.Name);
        }
    }
}

public class UnequipItem : PacketHandler<TradeItemRequest>
{
    public UnequipItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (data.ItemQuery.Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            await player.UnequipAllItemsAsync();
            client.SendMessage(data.Player.Username, Localization.MSG_UNEQUIPPED_ALL);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.ItemQuery, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
            queriedItem = itemResolver.Resolve(data.ItemQuery + " pet", parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        var item = player.Inventory.GetEquippedItem(queriedItem.Item.Id);
        if (item == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_EQUIPPED, queriedItem.Item.Name);
            return;
        }

        await player.UnequipAsync(queriedItem.Item);
        client.SendMessage(data.Player.Username, Localization.MSG_UNEQUIPPED, queriedItem.Item.Name);
    }
}