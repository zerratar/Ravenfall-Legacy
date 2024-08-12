using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    public RavenNest.Models.Island Island;

    public string Identifier;
    public int LevelRequirement;

    public DockController DockingArea;
    public Transform SpawnPositionTransform;
    public Transform CameraPanTarget;

    public bool AllowRaidWar;
    public Transform RaiderSpawningPoint;
    public Transform StreamerSpawningPoint;

    private SphereCollider sphereCollider;
    private float radius;
    private Chunk[] chunks;

    //private int ferryArriveCount;
    private int raidBossCount;

    //private readonly Dictionary<Guid, PlayerController> players = new Dictionary<Guid, PlayerController>();
    private readonly List<PlayerController> players = new List<PlayerController>();
    public Vector3 SpawnPosition => SpawnPositionTransform ? SpawnPositionTransform.position : transform.position;

    private PlayerManager playerManager;
    private Transform _transform;

    public bool Sailable => this.DockingArea != null;

    public IslandStatistics Statistics = new IslandStatistics();

    public bool InsideIsland(Vector3 position)
    {
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        var pos = sphereCollider.center + _transform.position;
        return Vector3.Distance(position, pos) <= radius;
    }

    public void Awake()
    {
        this._transform = transform;
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        radius = sphereCollider.radius;
        this.chunks = GetComponentsInChildren<Chunk>();
    }

    public IReadOnlyList<Chunk> TrainingAreas => chunks;

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

        //foreach (var p in players)
        //{
        //    if (p.Ferry.OnFerry || p.Dungeon.InDungeon)
        //    {
        //        return RebuildPlayerList();
        //    }
        //}

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

            if (p.ferryHandler.OnFerry || p.dungeonHandler.InDungeon)
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
        if (!players.Contains(playerController))
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

public class IslandStatistics
{
    public long MonstersDefeated;
    public long PlayersKilled;
    public long RaidBossesSpawned;
    public long ItemsGathered;
    public long TreesCutDown;
    public long RocksMined;
    public long FishCaught;
    public long CropsHarvested;
    public long FoodCooked;
    public long ItemsCrafted;
    public long PotionsBrewed;
}
