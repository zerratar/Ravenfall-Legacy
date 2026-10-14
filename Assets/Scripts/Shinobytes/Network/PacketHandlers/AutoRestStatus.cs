public class AutoRestStatus : ChatBotCommandHandler
{
    public AutoRestStatus(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var autoRestStopMinutes = plr.Rested.AutoRestTarget;
        var autoRestStartMinutesLeft = plr.Rested.AutoRestStart;

        if (autoRestStopMinutes <= autoRestStartMinutesLeft)
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_INACTIVE);
            return;
        }

        if (autoRestStartMinutesLeft == 0)
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_ACTIVE_WHEN_OUT, autoRestStopMinutes);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_ACTIVE, autoRestStopMinutes, autoRestStartMinutesLeft);
        }
    }
}
