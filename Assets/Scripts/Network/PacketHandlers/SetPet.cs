using RavenNest.Models;

public class SetPet : PacketHandler<SetPetRequest>
{
    public SetPet(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(SetPetRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(data.Player.Username, "set_pet", "You are not currently playing. Use !join to start playing!");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(data.Player, "Change pet right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.Resolve(data.Pet, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem == null)
        {
            queriedItem = itemResolver.Resolve(data.Pet + " pet", parsePrice: false, parseUsername: false, parseAmount: false);
        }

        if (queriedItem == null)
        {
            client.SendCommand(data.Player.Username, "message", "Could not find an item matching the name: " + data.Pet);
            return;
        }

        if (queriedItem.Item.Type != ItemType.Pet)
        {
            client.SendCommand(data.Player.Username, "message", queriedItem.Item.Name + " is not a pet.");
            return;
        }

        var item = player.Inventory.GetInventoryItems(queriedItem.Item.Id);
        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if ((item == null || item.Count == 0))
        {
            if (equippedPet == null || equippedPet.Id != queriedItem.Item.Id)
            {
                client.SendCommand(data.Player.Username, "message", "You do not have any " + queriedItem.Item.Name + ".");
                return;
            }
        }

        if (equippedPet == null || equippedPet.Id != queriedItem.Item.Id)
        {
            await player.EquipAsync(queriedItem.Item);
        }

        client.SendCommand(data.Player.Username, "message", "You have changed your active pet to " + queriedItem.Item.Name);
    }
}
