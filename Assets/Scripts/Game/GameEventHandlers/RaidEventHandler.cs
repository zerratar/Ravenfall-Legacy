using System;
using System.Collections;
using System.Collections.Generic;
using RavenNest.Models;
using Shinobytes.Linq;
using UnityEngine;

public abstract class RaidEventHandler : GameEventHandler<StreamRaidInfo>
{
    protected async void OnStreamerRaid(GameManager gameManager, StreamRaidInfo raidInfo, bool raidWar)
    {
        var players = gameManager.Players.GetAllPlayers();
        if (raidInfo.Players.Count == 0)
        {
            gameManager.RavenBot.Announce(Localization.MSG_STREAMRAID_NO_PLAYERS, raidInfo.RaiderUserName);
            return;
        }

        var raiderPlayerCount = raidInfo.Players.Count;
        // only one active raid war at a time.
        raidWar = raidWar && !gameManager.StreamRaid.Started && !gameManager.StreamRaid.IsWar;
        gameManager.StreamRaid.AnnounceRaid(raidInfo, raidWar && players.Count > 0);

        if (players.Count == 0 && raidWar)
        {
            gameManager.RavenBot.Announce(Localization.MSG_STREAMRAID_WAR_NO_PLAYERS, raidInfo.RaiderUserName);
        }

        gameManager.StreamRaid.ClearTeams();

        if (raidWar)
        {
            gameManager.RavenBot.Announce("{raiderName} is declaring war with an army of {raiderPlayerCount}!",
                raidInfo.RaiderUserName,
                (object)raiderPlayerCount.ToString());
        }
        else
        {
            gameManager.RavenBot.Announce("{raiderName} is raiding with {raiderPlayerCount} players!",
                raidInfo.RaiderUserName,
                (object)raiderPlayerCount.ToString());
        }

        var raiders = new List<PlayerController>();
        foreach (var user in raidInfo.Players)
        {
            if (user == null || user.UserId == null)
                continue;

            var existingPlayer = gameManager.Players.GetPlayerByUserId(user.UserId);
            if (existingPlayer)
            {
                continue;
                //gameManager.RemovePlayer(existingPlayer);
            }

            var player = await gameManager.AddPlayerByCharacterIdAsync(user.CharacterId, raidInfo);
            if (player != null && player && raidWar)
            {
                raiders.Add(player);
            }
        }

        if (raiders.Count == 0)
        {
            gameManager.RavenBot.Announce("Raid could not be started, as 0 players could be added to the raid.");
            return;
        }

        gameManager.SaveStateFile();

        if (raidWar)
        {
            gameManager.StartCoroutine(StartRaidWar(gameManager, raidInfo, players, raiders));
        }
    }

    private IEnumerator StartRaidWar(GameManager gameManager, StreamRaidInfo raidInfo, IReadOnlyList<PlayerController> players, IReadOnlyList<PlayerController> raiders)
    {
        if (gameManager.Events.TryStart(gameManager.StreamRaid, true))
        {
            var myPlayerCount = gameManager.Players.GetPlayerCount();
            var raiderPlayerCount = raidInfo.Players.Count;
            foreach (var player in players)
            {
                if (raidInfo.Players.Any(x => x.UserId == player.UserId)) continue;
                gameManager.StreamRaid.AddToStreamerTeam(player);
            }

            yield return new WaitForEndOfFrame();
            foreach (var raider in raiders)
            {
                gameManager.StreamRaid.AddToRaiderTeam(raider);
                yield return new WaitForEndOfFrame();
            }

            gameManager.StreamRaid.StartRaidWar();
            gameManager.RavenBot.Announce("The raid war from {raiderName} is starting. {myPlayerCount} vs {raiderPlayerCount}!",
                raidInfo.RaiderUserName,
                myPlayerCount.ToString(),
                (object)raiderPlayerCount.ToString());
        }
        else
        {
            yield return new WaitForSeconds(gameManager.Events.RescheduleTime);
            yield return StartRaidWar(gameManager, raidInfo, players, raiders);
        }
    }
}
