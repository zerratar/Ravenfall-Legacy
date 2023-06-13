using System;

public class PlayerTravelEventHandler : GameEventHandler<RavenNest.Models.PlayerTravel>
{
    public override void Handle(GameManager gameManager, RavenNest.Models.PlayerTravel data)
    {
        var player = gameManager.Players.GetPlayerById(data.PlayerId);
        if (!player) return;

        if (player.StreamRaid.InWar || player.Arena.InArena || player.Duel.InDuel
            || player.Dungeon.InDungeon)
        {
            return;
        }

        if (player.Onsen.InOnsen)
        {
            gameManager.Onsen.Leave(player);
        }

        if (player.Raid.InRaid)
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

        player.Ferry.Embark(island);
    }

    private void SailForever(PlayerController player)
    {
        if (player.Ferry.Embarking || player.Ferry.OnFerry)
        {
            player.Ferry.ClearDestination();
            return;
        }

        player.Ferry.Embark();
    }
}
