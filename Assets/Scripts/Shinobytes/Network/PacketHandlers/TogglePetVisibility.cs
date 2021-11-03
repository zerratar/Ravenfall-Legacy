public class CycleEquippedPet : PacketHandler<TwitchPlayerInfo>
{
    public CycleEquippedPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(data);
        if (!targetPlayer)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }


        var result = await targetPlayer.CycleEquippedPetAsync();
        if (result == null)
        {
            client.SendMessage(data.Username, Localization.MSG_TOGGLE_PET_NO_PET);
            return;
        }

        client.SendMessage(data.Username, Localization.MSG_TOGGLE_PET, result.Item.Name);
    }
}