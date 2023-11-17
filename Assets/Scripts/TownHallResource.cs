using Sirenix.OdinInspector;
using System;
using UnityEngine;
public class TownHallResource : MonoBehaviour
{
    [SerializeField] private TownHallController townhall;
    [SerializeField] private GameManager gameManager;

    [Header("Coins")]
    [SerializeField] private GameObject[] coins;

    [Header("Wood")]
    [SerializeField] private GameObject[] wood;

    [Header("Ore")]
    [SerializeField] private GameObject[] ore;

    [Header("Fish")]
    [SerializeField] private GameObject[] fish;

    [Header("Wheat")]
    [SerializeField] private GameObject[] wheat;

    [Button("Assign resource piles")]
    private void AssignPiles()
    {
        townhall = GetComponentInParent<TownHallController>(true);
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

        if (coins.Length == 0) coins = FindPile("Coins");
        if (wood.Length == 0) wood = FindPile("Wood");
        if (ore.Length == 0) ore = FindPile("Ore");
        if (fish.Length == 0) fish = FindPile("Fish");
        if (wheat.Length == 0) wheat = FindPile("Wheat");

        if (townhall)
        {
            townhall.SetTownHallResourceController(this);
        }
    }

    private GameObject[] FindPile(string name)
    {
        var root = transform.Find(name);
        return new GameObject[]
        {
            root.Find("Low")?.gameObject,
            root.Find("Medium")?.gameObject,
            root.Find("High")?.gameObject,
            root.Find("ShitTon")?.gameObject,
        };
    }

    internal void ResourcesUpdated(TownHallManager manager)
    {
        // determine which of the 4 objects to show
        // based on the amaount of resources available.
        SetPile(manager.Coins, coins);
        SetPile(manager.Wood, wood);
        SetPile(manager.Ore, ore);
        SetPile(manager.Wheat, wheat);
        SetPile(manager.Fish, fish);
    }

    private void SetPile(long amount, GameObject[] pileSizes)
    {
        foreach (var ps in pileSizes)
        {
            ps.SetActive(false);
        }

        if (amount == 0)
        {
            return;
        }

        if (amount >= 10_000)
            pileSizes[3].SetActive(true);
        else if (amount >= 5_000)
            pileSizes[2].SetActive(true);
        else if (amount >= 1_000)
            pileSizes[1].SetActive(true);
        else if (amount > 0)
            pileSizes[0].SetActive(true);
    }
}
