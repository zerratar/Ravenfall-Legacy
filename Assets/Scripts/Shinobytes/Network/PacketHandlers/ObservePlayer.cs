using System;

public class ObservePlayer : ChatBotCommandHandler
{
    public ObservePlayer(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        // no need to test for mod powers as the bot will already have done that filtering.
        if (gm.Sender.Username.Equals("islands", StringComparison.OrdinalIgnoreCase))
        {
            Game.Camera.ObserveNextIsland();
            return;
        }

        if (gm.Sender.Username.Equals("players", StringComparison.OrdinalIgnoreCase))
        {
            Game.Camera.ObserveNextPlayer();
            return;
        }

        // ObservePlayer is a bit different as it can actually be used for ObserveIsland as well
        // so we will try and check if the gm.Sender username is the name of an island or not.
        // before we try to observe the player that used the command.
        var island = Game.Islands.Find(gm.Sender.Username);
        if (island)
        {
            Game.Camera.ObserveIsland(island);
            return;
        }

        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.Camera.ObservePlayer(player);
    }
}
