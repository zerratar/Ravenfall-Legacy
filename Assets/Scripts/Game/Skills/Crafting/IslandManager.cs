using RavenNest.Models;
using System;
using UnityEngine;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private GameManager game;

    private IslandController[] islands;

    public void EnsureIslands()
    {
        if (islands == null || islands.Length == 0)
        {
            islands = GetComponentsInChildren<IslandController>();
        }
    }
    private void Start()
    {
        EnsureIslands();
    }

    public IslandController[] All => islands;

    public IslandController Get(Island targetIsland)
    {
        EnsureIslands();
        foreach (var island in islands)
        {
            if (island.Island == targetIsland)
                return island;
        }
        return null;
    }

    public IslandController FindClosestIsland(Vector3 position)
    {
        EnsureIslands();
        var closest = float.MaxValue;
        IslandController closestIsland = null;
        for (var i = 0; i < All.Length; i++)
        {
            var dist = Vector3.Distance(position, All[i].transform.position);
            if (dist < closest)
            {
                closest = dist;
                closestIsland = All[i];
            }
        }
        return closestIsland;
    }

    public IslandController Find(string islandName)
    {
        EnsureIslands();
        if (islands == null || string.IsNullOrEmpty(islandName)) return null;

        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Identifier.StartsWith(islandName, StringComparison.OrdinalIgnoreCase))
                return All[i];
        }

        return null;
    }

    public IslandController FindPlayerIsland(PlayerController player)
    {
        EnsureIslands();
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].InsideIsland(player.Position)) return All[i];
        }

        return null;
    }

    public IslandController FindIsland(Vector3 position)
    {
        EnsureIslands();
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].InsideIsland(position)) return All[i];
        }

        return null;
    }

}
