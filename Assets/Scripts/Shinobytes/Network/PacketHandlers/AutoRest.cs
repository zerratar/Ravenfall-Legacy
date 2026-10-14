public class AutoRest : ChatBotCommandHandler<Arguments>
{
    public AutoRest(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(Arguments data, GameMessage gm, GameClient client)
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

        var autoJoinCost = Game.SessionSettings.AutoRestCost;
        var autoRestStart = int.Parse(data.Values[0]?.ToString());
        var autoRestStop = int.Parse(data.Values[1]?.ToString());
        if (!plr.onsenHandler.SetAutoRest(autoRestStart, autoRestStop))
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_SET_FAILED);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_AUTOREST_ACTIVATED, autoJoinCost);
        }
    }
}
