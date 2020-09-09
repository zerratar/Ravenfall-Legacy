using RavenNest.Models;

public class GetPet : PacketHandler<GetPetRequest>
{
    public GetPet(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GetPetRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(data.Player.Username, "set_pet", "You are not currently playing. Use !join to start playing!");
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if (equippedPet == null)
        {
            client.SendCommand(data.Player.Username, "message", "You do not have any pets equipped.");
            return;
        }

        client.SendCommand(data.Player.Username, "message", "You currently have " + equippedPet.Name + " equipped.");
    }
}