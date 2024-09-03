using System;

public class PlayerTravelEventHandler : GameEventHandler<RavenNest.Models.PlayerTravel>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerTravel data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player) return;

        if (player.streamRaidHandler.InWar || player.arenaHandler.InArena || player.duelHandler.InDuel
            || player.dungeonHandler.InDungeon)
        {
            return;
        }

        if (player.onsenHandler.InOnsen)
        {
            player.onsenHandler.Exit();
        }

        if (player.raidHandler.InRaid)
        {
            gameManager.Raid.Leave(player);
        }

        if (string.IsNullOrEmpty(data.Island) || data.Island.Equals("ferry", StringComparison.OrdinalIgnoreCase))
        {
            SailForever(player);
            return;
        }

        var islandName = data.Island;
        var island = gameManager.Islands.Find(islandName);
        if (!island || !island.Sailable || island == player.Island)
        {
            return;
        }

        player.ferryHandler.Embark(island);
    }

    private void SailForever(PlayerController player)
    {
        if (player.ferryHandler.Embarking || player.ferryHandler.OnFerry)
        {
            player.ferryHandler.ClearDestination();
            return;
        }

        player.ClearTask();
        player.ferryHandler.Embark();
    }
}
