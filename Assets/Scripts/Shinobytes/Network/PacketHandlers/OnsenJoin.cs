public class OnsenJoin : ChatBotCommandHandler
{
    public OnsenJoin(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

            if (player.Ferry.Embarking)
            {
                player.Ferry.Cancel();

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

            if (player.Onsen.InOnsen)
            {
                // Send Rested Status?
                return;
            }

            if (Game.Onsen.Join(player))
            {
                //Game.RavenBot.SendReply(player, Localization.MSG_ONSEN_ENTRY);
            }
        }
        catch { }
    }
}
