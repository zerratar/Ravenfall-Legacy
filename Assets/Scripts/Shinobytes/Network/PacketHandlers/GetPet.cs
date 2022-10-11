using RavenNest.Models;

public class GetPet : ChatBotCommandHandler<GetPetRequest>
{
    public GetPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GetPetRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if (equippedPet == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_GET_PET_NO_PET);
            return;
        }

        client.SendMessage(data.Player.Username, Localization.MSG_GET_PET, equippedPet.Name);
    }
}