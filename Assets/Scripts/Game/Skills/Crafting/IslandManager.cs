using RavenNest.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private GameManager game;

    private IslandController[] islands;

    // todo: make this list dynamic
    public static Dictionary<Island, int> IslandLevelRangeMin = new Dictionary<Island, int>
    {
        { Island.Home, 1 },
        { Island.Away, 50 },
        { Island.Ironhill, 100 },
        { Island.Kyo, 200 },
        { Island.Heim, 300 },
        { Island.Atria, 500 },
        { Island.Eldara, 700 },
    };

    // todo: make this list dynamic
    public static Dictionary<Island, int> IslandLevelRangeMax = new Dictionary<Island, int>
    {
        { Island.Home, 99 },
        { Island.Away, 150 },
        { Island.Ironhill, 300 },
        { Island.Kyo, 400 },
        { Island.Heim, 700 },
        { Island.Atria, 900 },
        { Island.Eldara, 1000 },
    };

    // todo: make this list dynamic
    public static Dictionary<Island, int> IslandMaxEffect = new Dictionary<Island, int>
    {
        { Island.Home, 50 },
        { Island.Away, 100 },
        { Island.Ironhill, 200 },
        { Island.Kyo, 300 },
        { Island.Heim, 500 },
        { Island.Atria, 700 },
        { Island.Eldara, 1000 },
    };

    public static Island GetSuitableIsland(int level)
    {
        foreach (var island in IslandLevelRangeMin.Keys)
        {
            if (level >= IslandLevelRangeMin[island] && level <= IslandMaxEffect[island])
            {
                return island;
            }

            if (level >= IslandLevelRangeMin[island] && level <= IslandLevelRangeMax[island])
            {
                return island;
            }
        }
        return Island.Home;
    }

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
