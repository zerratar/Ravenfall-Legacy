public class PlayerBeginRestEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        if (player.ferryHandler.Embarking)
        {
            player.ferryHandler.Cancel();
        }

        // in a tower town: its campfire (the same rest)
        if (player.towerHandler != null && player.towerHandler.InTown)
        {
            player.towerHandler.StartResting();
            return;
        }

        if (player.duelHandler.InDuel || player.arenaHandler.InArena || player.streamRaidHandler.InWar || player.InRavebridge
            || player.IsAway || player.raidHandler.InRaid || player.onsenHandler.InOnsen)
        {
            return;
        }

        gameManager.Onsen.Join(player);
    }
}
