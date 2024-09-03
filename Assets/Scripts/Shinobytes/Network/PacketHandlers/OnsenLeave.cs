public class OnsenLeave : ChatBotCommandHandler
{
    public OnsenLeave(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override void Handle(GameMessage gm, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (!player.onsenHandler.InOnsen)
            {
                return;
            }

            if (player.duelHandler.InDuel)
            {
                return;
            }

            if (player.arenaHandler.InArena)
            {
                return;
            }

            if (player.dungeonHandler.InDungeon)
            {
                return;
            }

            if (player.raidHandler.InRaid)
            {
                return;
            }

            if (player.streamRaidHandler.InWar)
            {
                return;
            }

            player.onsenHandler.Exit();

            //Game.RavenBot.SendReply(player, Localization.MSG_ONSEN_LEFT);
        }
        catch { }
    }
}
