public class CycleEquippedPet : PacketHandler<Player>
{
    public CycleEquippedPet(
        GameManager game,
        GameServer server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override async void Handle(Player data, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(data);
        if (!targetPlayer)
        {
            client.SendCommand(data.Username, "message", "You are not currently playing. Use !join to start playing!");
            return;
        }

        //var equippedPet = targetPlayer.Inventory.GetEquipmentOfType(RavenNest.Models.ItemCategory.Pet, RavenNest.Models.ItemType.Pet);
        //if (equippedPet == null)
        //{
        //    client.SendCommand(data.Username, "message", "You do not own any pets.");
        //    return;
        //}

        var result = await targetPlayer.CycleEquippedPetAsync();
        if (result == null)
        {
            client.SendCommand(data.Username, "message", "You have no more pets to cycle between.");//"Oh no, this is a bug. Failed to cycle pet.");
            return;
        }

        client.SendCommand(data.Username, "message", "You have changed your active pet to " + result.Item.Name);
    }
}