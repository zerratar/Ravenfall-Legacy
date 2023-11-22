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

        if (player.duelHandler.InDuel || player.arenaHandler.InArena || player.streamRaidHandler.InWar
            || player.dungeonHandler.InDungeon || player.raidHandler.InRaid || player.onsenHandler.InOnsen)
        {
            return;
        }

        gameManager.Onsen.Join(player);
    }
}
