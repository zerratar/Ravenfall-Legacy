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

        if (plr.PatreonTier <= 0)
        {
            client.SendReply(gm, Localization.MSG_PATREON_ONLY);
            return;
        }

        var autoRestStopMinutes = plr.Rested.AutoRestTarget;
        var autoRestStartMinutesLeft = plr.Rested.AutoRestStart;

        if (autoRestStopMinutes <= autoRestStartMinutesLeft)
        {
            client.SendReply(gm, "Auto rest is currently inactive. Use !auto rest <start minute> <stop minute> to activate.");
            return;
        }

        if (autoRestStartMinutesLeft == 0)
        {
            client.SendReply(gm, "Auto rest is currently active. You will auto rest until you have {autoRestStopMinutes} minutes of rested as soon as you run out of rested time.", autoRestStopMinutes);
        }
        else
        {
            client.SendReply(gm, "Auto rest is currently active. You will auto rest until you have {autoRestStopMinutes} minutes of rested. You will start resting when you have {autoRestStartMinutesLeft} minutes of rested left.", autoRestStopMinutes, autoRestStartMinutesLeft);
        }
    }
}
