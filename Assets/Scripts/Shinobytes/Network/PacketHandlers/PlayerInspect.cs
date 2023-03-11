public class PlayerInspect : ChatBotCommandHandler
{
    public PlayerInspect(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
         : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null)
        {
            return;
        }

        client.SendReply(gm, Localization.MSG_PLAYER_INSPECT_URL, player.Id.ToString());
    }
}
