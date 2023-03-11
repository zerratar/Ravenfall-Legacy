using RavenNest.Models;

public class GetPet : ChatBotCommandHandler<string>
{
    public GetPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if (equippedPet == null)
        {
            client.SendReply(gm, Localization.MSG_GET_PET_NO_PET);
            return;
        }

        client.SendReply(gm, Localization.MSG_GET_PET, equippedPet.Name);
    }
}