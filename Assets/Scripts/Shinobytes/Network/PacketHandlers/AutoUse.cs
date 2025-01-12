public class AutoUse : ChatBotCommandHandler<Arguments>
{
    public AutoUse(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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
        if (plr.PatreonTier <= 0)
        {
            client.SendReply(gm, Localization.MSG_PATREON_ONLY);
            return;
        }

        // !auto <use|eat|consume|drink> <itemName> (optional: <maxCount>) (optional: <interval>)
        //  itemName: item to use, this cannot be a tome of teleportation
        //  maxCount: how many you want to use as max, this can be formatted with a prefix (x) to help identify as an amount
        //  interval: can be formatted in various ways, but when numbered it defaults to seconds
        //      format option A: 10m (every 10 minutes), 10s (every 10s), 1h (every hour)
        //      format option B: cronjob


    }
}

public class AutoUseStop : ChatBotCommandHandler
{
    public AutoUseStop(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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
    }
}

public class AutoUseStatus : ChatBotCommandHandler
{
    public AutoUseStatus(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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
    }
}
