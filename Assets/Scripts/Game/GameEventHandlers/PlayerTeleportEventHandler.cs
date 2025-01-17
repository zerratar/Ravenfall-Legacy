﻿using RavenNest.Models;

public class PlayerTeleportEventHandler : GameEventHandler<PlayerTeleportMessage>
{
    public override void Handle(GameManager gameManager, PlayerTeleportMessage data)
    {
        if (data.Ids == null || data.Ids.Length == 0) return;
        if (data.Island == Island.None) return;

        foreach (var id in data.Ids)
        {
            var player = gameManager.Players.GetPlayerById(id);
            if (!player) continue;

            IslandController island = null;
            if (data.Island == Island.Ferry)
            {
                // not implemented yet, but we could move them to the ferry,.
                continue;
            }
            else if (data.Island == Island.Any)
            {
                // find closest island.
                island = gameManager.Islands.FindClosestIsland(player.transform.position);
            }
            else
            {
                island = gameManager.Islands.Get(data.Island);
            }

            if (!island)
            {
                continue;
            }

            if (player.ferryHandler.OnFerry)
            {
                player.ferryHandler.BeginDisembark();
            }

            if (player.raidHandler.InRaid)
            {
                gameManager.Raid.Leave(player);
            }

            if (player.duelHandler.InDuel)
            {
                player.duelHandler.Died();
            }

            if (player.dungeonHandler.InDungeon)
            {
                gameManager.Dungeons.Remove(player);
            }

            player.teleportHandler.Teleport(island, true);
        }
    }
}
