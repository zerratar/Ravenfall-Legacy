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

        var target = plr.Rested.AutoRestTarget;
        var start = plr.Rested.AutoRestStart;

        // stopped is null for both (!auto rest off), and null <= null is false: it said active with the numbers missing
        // ("until you have {autoRestStopMinutes} minutes"), right after being turned off (the user, 2026-11-04)
        if (!target.HasValue || !start.HasValue || target.Value <= 0 || target.Value <= start.Value)
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_INACTIVE);
            return;
        }

        var autoRestStopMinutes = (int)System.Math.Round(target.Value);
        var autoRestStartMinutesLeft = (int)System.Math.Round(start.Value);
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
