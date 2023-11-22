using RavenNest.Models;
using UnityEngine;

public class PlayerJoinRaidEventHandler : GameEventHandler<PlayerId>
{
    public override void Handle(GameManager gameManager, PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player)
            return;

        if (player.streamRaidHandler.InWar)
            return;

        //if (player.ferryHandler.OnFerry)
        //    return;

        //if (player.ferryHandler.Active)
        //    player.ferryHandler.Disembark();

        if (player.dungeonHandler.InDungeon)
            return;

        if (player.arenaHandler.InArena && gameManager.Arena.Started)
            return;

        if (player.duelHandler.InDuel)
            return;

        var result = gameManager.Raid.CanJoin(player);
        switch (result)
        {
            case RaidJoinResult.NoActiveRaid:
                //client.SendCommand(data.Username, "raid_join_failed", $"There are no active raids at the moment.");
                return;
            case RaidJoinResult.AlreadyJoined:
                //client.SendCommand(data.Username, "raid_join_failed", $"You have already joined the raid.");
                return;
            case RaidJoinResult.MinHealthReached:
                //client.SendCommand(data.Username, "raid_join_failed", $"You can no longer join the raid.");
                return;
        }

        gameManager.Arena.Leave(player);
        gameManager.Raid.Join(player);
        Shinobytes.Debug.Log($"PlayerJoinRaidEventHandler " + data.Id);
    }
}

