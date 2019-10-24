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
        return islands.FirstOrDefault(x => x.Identifier.StartsWith(islandName, StringComparison.OrdinalIgnoreCase));
    }

    public IslandController FindPlayerIsland(PlayerController player)
    {
        return All.FirstOrDefault(x => x.InsideIsland(player.transform.position));
    }

    public IslandController FindIsland(Vector3 position)
    {
        return All.FirstOrDefault(x => x.InsideIsland(position));
    }
}
