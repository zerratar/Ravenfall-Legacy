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

            if (player.Duel.InDuel || player.Arena.InArena || player.StreamRaid.InWar
                || player.Dungeon.InDungeon || player.Raid.InRaid || player.Onsen.InOnsen)
            {
                return;
            }

            Game.Onsen.Join(player);
        }
        catch { }
    }
}
