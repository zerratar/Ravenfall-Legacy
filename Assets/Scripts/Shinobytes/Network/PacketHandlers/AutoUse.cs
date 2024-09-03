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
