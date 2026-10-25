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

    /// <summary>Open to every level, with the full island experience factor for everyone (the mangrove island, Ravebridge).</summary>
    public bool OpenToAllLevels;

    /// <summary>Only an event takes players here (Ravebridge): left out of the island tour while nobody is on it.</summary>
    public bool EventOnly;

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

    public bool Sailable => this.DockingArea != null && !Locked;

    /// <summary>
    /// The mangrove island ((Island)8, which the game's older RavenNest.Models has no name for) opens per stream with
    /// the streamer's story "Insufficient bananas" (StoryLog, island_mangrove). Until the log is loaded it counts as
    /// open: the server refuses gathering there anyway where it is not unlocked.
    /// </summary>
    public bool Locked => Island == MangroveIsland && StoryLog.Latest != null && !StoryLog.HasUnlock("island_mangrove");

    public const RavenNest.Models.Island MangroveIsland = (RavenNest.Models.Island)8;
    public const RavenNest.Models.Island RavebridgeIsland = (RavenNest.Models.Island)9;

    public IslandStatistics Statistics = new IslandStatistics();

    /// <summary>
    /// The navmesh area of an island with a navmesh of its own (the mangrove island's "Mangrove"), or 0 for the world's.
    /// </summary>
    public int NavMeshArea;

    public bool InsideIsland(Vector3 position)
    {
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        if (!_transform) _transform = transform;
        // the sphere's centre turns with the island (the mangrove island's root is turned)
        var pos = _transform.TransformPoint(sphereCollider.center);
        return Vector3.Distance(position, pos) <= radius;
    }

    public void Awake()
    {
        this._transform = transform;
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        radius = sphereCollider.radius;
        this.chunks = GetComponentsInChildren<Chunk>();
        if (NavMeshArea != 0) NavMeshHelper.AddOwnArea(_transform.TransformPoint(sphereCollider.center), radius, NavMeshArea);
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

    public IReadOnlyList<PlayerController> RebuildPlayerList()
    {
#if DEBUG
        Shinobytes.Debug.Log("Rebuilding player list for island: " + this.Island + " as one or more players were on ferry or in dungeon.");
#endif

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
                p.Island = this;
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
