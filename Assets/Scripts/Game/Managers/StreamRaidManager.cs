using RavenNest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StreamRaidManager : MonoBehaviour, IEvent
{
    private readonly object mutex = new object();
    private readonly List<PlayerController> defenders = new List<PlayerController>();
    private readonly List<PlayerController> raiders = new List<PlayerController>();
    private readonly ConcurrentDictionary<Guid, Vector3> oldPlayerPosition = new ConcurrentDictionary<Guid, Vector3>();

    [SerializeField] private StreamRaidNotifications streamRaidNotifications;

    private GameManager gameManager;
    private IslandController raidWarIsland;

    public bool Started;
    public bool IsWar;
    private StreamRaidInfo raiderInfo;

    public StreamRaidInfo Raider => raiderInfo;

    public StreamRaidNotifications Notifications => streamRaidNotifications;

    private void Start()
    {
        if (!streamRaidNotifications) streamRaidNotifications = FindAnyObjectByType<StreamRaidNotifications>();
        gameManager = GetComponent<GameManager>();
    }

    public IReadOnlyList<PlayerController> GetOpposingTeamPlayers(PlayerController playerReference)
    {
        lock (mutex)
        {
            return defenders.Any(x => x.UserId == playerReference.UserId) ? raiders : defenders;
        }
    }

    public void AddToStreamerTeam(PlayerController player)
    {
        if (Started) return;
        lock (mutex)
        {
            defenders.Add(player);
            if (!raidWarIsland) raidWarIsland = gameManager.Islands.Find("war");
            oldPlayerPosition[player.UserId] = player.Position;
            player.teleportHandler.Teleport(raidWarIsland.StreamerSpawningPoint.position);
            player.streamRaidHandler.OnEnter();
        }
    }

    public void AddToRaiderTeam(PlayerController player)
    {
        if (Started || !player) return;
        lock (mutex)
        {
            raiders.Add(player);
            if (!raidWarIsland) raidWarIsland = gameManager.Islands.Find("war");
            oldPlayerPosition[player.UserId] = gameManager.Chunks.GetStarterChunk().GetPlayerSpawnPoint();
            player.teleportHandler.Teleport(raidWarIsland.RaiderSpawningPoint.position);
            player.streamRaidHandler.OnEnter();
        }
    }
    public bool InRaid(PlayerController target)
    {
        lock (mutex)
        {
            return defenders.Any(x => x.UserId == target.UserId) || raiders.Any(x => x.UserId == target.UserId);
        }
    }

    public void ClearTeams()
    {
        lock (mutex)
        {
            oldPlayerPosition.Clear();
            defenders.Clear();
            raiders.Clear();
        }
    }

    public void CheckForWarEnd()
    {
        lock (mutex)
        {
            if (raiders.Count == 0 || defenders.Count == 0)
            {
                EndRaidWar();
            }
        }
    }

    public void RemoveFromRaid(PlayerController playerReference)
    {
        lock (mutex)
        {
            defenders.Remove(playerReference);
            raiders.Remove(playerReference);
            playerReference.streamRaidHandler.OnExit();
        }
    }

    public void OnPlayerDied(PlayerController player)
    {
        if (!IsWar || !InRaid(player))
        {
            return;
        }

        RemoveFromRaid(player);

        if (oldPlayerPosition.TryRemove(player.UserId, out var pos))
        {
            player.teleportHandler.Teleport(pos);
        }

        CheckForWarEnd();
    }

    public void StartRaidWar()
    {
        Started = true;
        gameManager.Music.PlayStreamRaidMusic();
    }

    public void EndRaidWar()
    {
        gameManager.Events.End(this);

        if (!IsWar)
        {
            return;
        }

        IsWar = false;
        Started = false;

        lock (mutex)
        {

            var fallback = gameManager.Chunks.GetStarterChunk().GetPlayerSpawnPoint();
            foreach (var player in defenders)
            {
                player.streamRaidHandler.OnExit();
                TeleportBack(fallback, player);
            }
            foreach (var player in raiders)
            {
                player.streamRaidHandler.OnExit();
                TeleportBack(fallback, player);
            }

            if (defenders.Count >= raiders.Count) AnnounceDefendersWon();
            else AnnounceRaidersWon();

            defenders.Clear();
            raiders.Clear();
            oldPlayerPosition.Clear();


            this.raiderInfo = null;
        }

        gameManager.Music.PlayBackgroundMusic();
    }

    private void TeleportBack(Vector3 fallback, PlayerController player)
    {
        if (oldPlayerPosition.TryGetValue(player.UserId, out var pos))
        {
            player.teleportHandler.Teleport(pos);
        }
        else
        {
            player.teleportHandler.Teleport(fallback);
        }
    }

    public void AnnounceRaid(StreamRaidInfo raidInfo, bool raidWar)
    {
        this.raiderInfo = raidInfo;
        if (raidWar)
        {
            IsWar = true;
            AnnounceWarMessage($"<b>{raidInfo.RaiderUserName}</b> has declared war with an army of <b>{raidInfo.Players.Count}</b>!");
            return;
        }

        AnnounceRaidMessage($"Friendly raid from <b>{raidInfo.RaiderUserName}</b> with <b>{raidInfo.Players.Count}</b> players!");
    }

    private void AnnounceDefendersWon()
    {
        streamRaidNotifications.ShowDefendersWon();
    }

    private void AnnounceRaidersWon()
    {
        streamRaidNotifications.ShowRaidersWon();
    }

    //private void TeleportBack(IEnumerable<PlayerController> players)
    //{
    //    lock (mutex)
    //    {
    //        foreach (var player in players)
    //        {
    //            if (oldPlayerPosition.TryGetValue(player.UserId, out var pos))
    //                player.teleportHandler.Teleport(pos);
    //        }
    //    }

    //    oldPlayerPosition.Clear();
    //}

    private void AnnounceRaidMessage(string msg)
    {
        if (!streamRaidNotifications.gameObject.activeSelf)
            streamRaidNotifications.gameObject.SetActive(true);
        streamRaidNotifications.ShowIncomingRaid(msg);
    }

    private void AnnounceWarMessage(string msg)
    {
        if (!streamRaidNotifications.gameObject.activeSelf)
            streamRaidNotifications.gameObject.SetActive(true);
        streamRaidNotifications.ShowIncomingRaid(msg);
    }
}
