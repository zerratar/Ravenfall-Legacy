public class CycleEquippedPet : ChatBotCommandHandler
{
    public CycleEquippedPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager) 
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(gm.Sender);
        if (!targetPlayer)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var result = await targetPlayer.CycleEquippedPetAsync();
        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_TOGGLE_PET_NO_PET);
            return;
        }

        client.SendReply(gm, Localization.MSG_TOGGLE_PET, result.Item.Name);
    }
}