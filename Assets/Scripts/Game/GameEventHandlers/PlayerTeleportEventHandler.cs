using RavenNest.Models;

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

            if (player.Ferry.OnFerry)
            {
                player.Ferry.BeginDisembark();
            }

            if (player.Raid.InRaid)
            {
                gameManager.Raid.Leave(player);
            }
            if (player.Dungeon.InDungeon)
            {
                gameManager.Dungeons.Remove(player);
            }
            player.Teleporter.Teleport(island);
        }
    }
}
