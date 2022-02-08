using System;
using System.Linq;
using UnityEngine;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private GameManager game;

    private IslandController[] islands;
    private void Start()
    {
        islands = GetComponentsInChildren<IslandController>();
    }

    public IslandController[] All => islands;
    public IslandController Find(string islandName)
    {
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
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].InsideIsland(player.Position)) return All[i];
        }

        return null;
    }

    public IslandController FindIsland(Vector3 position)
    {
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].InsideIsland(position)) return All[i];
        }

        return null;
    }
}
