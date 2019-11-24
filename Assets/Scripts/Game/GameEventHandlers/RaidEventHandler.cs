using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RaidEventHandler : GameEventHandler<StreamRaidInfo>
{
    protected async void OnStreamerRaid(GameManager gameManager, StreamRaidInfo raidInfo, bool raidWar)
    {
        var players = gameManager.Players.GetAllPlayers();
        if (raidInfo.Players.Count == 0)
        {
            gameManager.Server.Client?.SendCommand("", "message", raidInfo.RaiderUserName + " raided but without any players. Kappa");
            return;
        }

        // only one active raid war at a time.
        raidWar = raidWar && !gameManager.StreamRaid.Started && !gameManager.StreamRaid.IsWar;
        gameManager.StreamRaid.AnnounceRaid(raidInfo, raidWar && players.Count > 0);

        if (players.Count == 0 && raidWar)
        {
            gameManager.Server.Client?.SendCommand("", "message",
                raidInfo.RaiderUserName + " raided with intent of war but we don't have any players. FeelsBadMan");
        }

        gameManager.StreamRaid.ClearTeams();

        if (!gameManager.StreamRaid.Started && raidWar)
        {
            foreach (var player in players)
            {
                if (raidInfo.Players.Contains(player.UserId)) continue;
                gameManager.StreamRaid.AddToStreamerTeam(player);
            }
        }

        var raiders = new List<PlayerController>();
        foreach (var user in raidInfo.Players)
        {
            var existingPlayer = gameManager.Players.GetPlayerByUserId(user);
            if (existingPlayer)
            {
                gameManager.RemovePlayer(existingPlayer);
            }

            var player = await gameManager.AddPlayerByUserIdAsync(user, raidInfo);
            if (player && raidWar)
            {
                raiders.Add(player);
            }
        }

        if (raidWar)
        {
            gameManager.StartCoroutine(StartRaidWar(gameManager, raiders));
        }
    }

    private IEnumerator StartRaidWar(GameManager gameManager, IReadOnlyList<PlayerController> raiders)
    {
        yield return new WaitForEndOfFrame();
        foreach (var raider in raiders)
        {
            gameManager.StreamRaid.AddToRaiderTeam(raider);
            yield return new WaitForEndOfFrame();
        }

        gameManager.StreamRaid.StartRaidWar();
    }
}
