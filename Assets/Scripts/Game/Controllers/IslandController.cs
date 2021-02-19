using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    public string Identifier;
    public DockController DockingArea;
    public Transform SpawnPositionTransform;

    public bool AllowRaidWar;
    public Transform RaiderSpawningPoint;
    public Transform StreamerSpawningPoint;

    private SphereCollider sphereCollider;
    private float radius;
    private readonly ConcurrentDictionary<Guid, PlayerController> players
        = new ConcurrentDictionary<Guid, PlayerController>();

    public Vector3 SpawnPosition => SpawnPositionTransform ? SpawnPositionTransform.position : transform.position;

    public bool InsideIsland(Vector3 position)
    {
        var pos = sphereCollider.center + transform.position;
        return Vector3.Distance(position, pos) <= radius;
    }

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        radius = sphereCollider.radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        var raidBoss = other.gameObject.GetComponent<RaidBossController>();
        if (raidBoss)
        {
            raidBoss.IslandEnter(this);
            return;
        }

        var ferry = other.gameObject.GetComponent<FerryController>();
        if (ferry)
        {
            ferry.IslandEnter(this);
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var raidBoss = other.gameObject.GetComponent<RaidBossController>();
        if (raidBoss)
        {
            raidBoss.IslandExit();
            return;
        }

        var ferry = other.gameObject.GetComponent<FerryController>();
        if (ferry)
        {
            ferry.IslandExit();
            return;
        }
    }

    public IReadOnlyList<PlayerController> GetPlayers() => players.Values.ToList();

    internal void AddPlayer(PlayerController playerController)
    {
        players[playerController.Id] = playerController;
    }

    internal void RemovePlayer(PlayerController playerController)
    {
        players.TryRemove(playerController.Id, out _);
    }
}
