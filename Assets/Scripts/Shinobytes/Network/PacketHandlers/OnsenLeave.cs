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

            if (!player.Onsen.InOnsen)
            {
                return;
            }

            if (player.Duel.InDuel)
            {
                return;
            }

            if (player.Arena.InArena)
            {
                return;
            }

            if (player.Dungeon.InDungeon)
            {
                return;
            }

            if (player.Raid.InRaid)
            {
                return;
            }

            if (player.StreamRaid.InWar)
            {
                return;
            }

            Game.Onsen.Leave(player);

            //Game.RavenBot.SendReply(player, Localization.MSG_ONSEN_LEFT);
        }
        catch { }
    }
}
