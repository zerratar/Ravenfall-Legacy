using System;
using System.Linq;
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
