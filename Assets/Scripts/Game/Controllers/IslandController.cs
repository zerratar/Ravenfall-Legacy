using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    public RavenNest.Models.Island Island;

    public string Identifier;
    public DockController DockingArea;
    public Transform SpawnPositionTransform;

    public bool AllowRaidWar;
    public Transform RaiderSpawningPoint;
    public Transform StreamerSpawningPoint;

    private SphereCollider sphereCollider;
    private float radius;

    //private int ferryArriveCount;
    private int raidBossCount;

    //private readonly Dictionary<Guid, PlayerController> players = new Dictionary<Guid, PlayerController>();
    private readonly List<PlayerController> players = new List<PlayerController>();
    public Vector3 SpawnPosition => SpawnPositionTransform ? SpawnPositionTransform.position : transform.position;

    private PlayerManager playerManager;

    public bool Sailable => this.DockingArea != null;

    public bool InsideIsland(Vector3 position)
    {
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        var pos = sphereCollider.center + transform.position;
        return Vector3.Distance(position, pos) <= radius;
    }

    public void Awake()
    {
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        radius = sphereCollider.radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        var raidBoss = other.gameObject.GetComponent<RaidBossController>();
        if (raidBoss)
        {
            raidBoss.IslandEnter(this);
            ++this.raidBossCount;
            return;
        }

        //var ferry = other.gameObject.GetComponent<FerryController>();
        //if (ferry)
        //{
        //    ferry.IslandEnter(this);
        //    ++this.ferryArriveCount;
        //    return;
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        var raidBoss = other.gameObject.GetComponent<RaidBossController>();
        if (raidBoss)
        {
            raidBoss.IslandExit();
            return;
        }

        //var ferry = other.gameObject.GetComponent<FerryController>();
        //if (ferry)
        //{
        //    ferry.IslandExit();
        //    return;
        //}
    }
    public IReadOnlyList<PlayerController> GetPlayers()
    {
        // we have to verify that all players are correctly assigned on an island
        // if not, we have to rebuild this list.

        foreach (var p in players)
        {
            if (p.Ferry.OnFerry || p.Dungeon.InDungeon)
            {
                return RebuildPlayerList();
            }
        }

        return players;//players.Values.ToList();
    }

    private IReadOnlyList<PlayerController> RebuildPlayerList()
    {
        Shinobytes.Debug.Log("Rebuilding player list for island: " + this.Island + " as one or more players were on ferry or in dungeon.");
        if (!playerManager)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        players.Clear();
        foreach (var p in playerManager.GetAllPlayers())
        {
            if (p.Ferry.OnFerry || p.Dungeon.InDungeon)
                continue;

            if (InsideIsland(p.transform.position))
            {
                players.Add(p);
            }
        }

        return players;
    }

    internal void AddPlayer(PlayerController playerController)
    {
        players.Add(playerController);
        //players[playerController.Id] = playerController;
    }

    internal void RemovePlayer(PlayerController playerController)
    {
        players.Remove(playerController);
        //players.Remove(playerController.Id);
    }

    internal int GetPlayerCount()
    {
        return players.Count;
    }
}
