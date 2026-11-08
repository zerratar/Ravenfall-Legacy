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

            if (player.ferryHandler.Embarking)
            {
                player.ferryHandler.Cancel();
            }

            // in the tower, the town's campfire is the place to rest (from a floor: down to the town first)
            if (player.towerHandler != null && player.towerHandler.InTower && TowerManager.Instance)
            {
                TowerCommand.Rest(TowerManager.Instance, player, gm, client);
                return;
            }

            if (player.duelHandler.InDuel || player.arenaHandler.InArena || player.streamRaidHandler.InWar || player.InRavebridge
                || player.IsAway || player.raidHandler.InRaid || player.onsenHandler.InOnsen)
            {
                return;
            }

            player.onsenHandler.IsAutoResting = false;
            Game.Onsen.Join(player);
        }
        catch { }
    }
}
