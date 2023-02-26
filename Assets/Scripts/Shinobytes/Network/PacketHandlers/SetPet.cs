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
        var query = (data.Pet + " pet").ToLower().Replace(" pet pet", " pet");
        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false, parseUsername: false, parseAmount: false);
       
        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, query, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
           client.SendMessage(data.Player.Username, Localization.MSG_ITEM_NOT_FOUND, data.Pet);
            return;
        }

        if (item.InventoryItem == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_OWNED, item.Item.Name);
            return;
        }

        if (item.Item.Type != ItemType.Pet)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_PET, item.Item.Name);
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        var invItem = item.InventoryItem;
        if (invItem == null)
        {
            if (equippedPet == null || equippedPet.ItemId != item.Id)
            {
                client.SendMessage(data.Player.Username, Localization.MSG_SET_PET_NOT_OWNED, item.Item.Name);
                return;
            }
        }

        if (equippedPet == null || equippedPet.ItemId != item.Id)
        {
            await player.EquipAsync(item.Item);
        }

        client.SendMessage(data.Player.Username, Localization.MSG_SET_PET, item.Item.Name);
    }
}
