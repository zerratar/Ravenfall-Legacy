public class AutoRestStop : ChatBotCommandHandler
{
    public AutoRestStop(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(gm.Sender);
        if (!plr)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }
        //if (plr.PatreonTier <= 0)
        //{
        //    client.SendReply(gm, Localization.MSG_PATREON_ONLY);
        //    return;
        //}

        plr.onsenHandler.ClearAutoRest();
        client.SendReply(gm, "Auto rest has been stopped.");
    }
}
