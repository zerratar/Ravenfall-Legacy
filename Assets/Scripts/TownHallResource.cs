using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
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

    private long lastCoins = -1;
    private long lastWood = -1;
    private long lastWheat = -1;
    private long lastOre = -1;
    private long lastFish = -1;

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

        if (lastCoins != manager.Coins)
        {
            SetPile(manager.Coins, lastCoins, coins);
            lastCoins = manager.Coins;
        }

        if (lastWood != manager.Wood)
        {
            SetPile(manager.Wood, lastWood, wood);
            lastWood = manager.Wood;
        }

        if (lastOre != manager.Ore)
        {
            SetPile(manager.Ore, lastOre, ore);
            lastOre = manager.Ore;
        }

        if (lastWheat != manager.Wheat)
        {
            SetPile(manager.Wheat, lastWheat, wheat);
            lastWheat = manager.Wheat;
        }

        if (lastFish != manager.Fish)
        {
            SetPile(manager.Fish, lastFish, fish);
            lastFish = manager.Fish;
        }
    }

    private void SetPile(long amount, long lastValue, GameObject[] pileSizes)
    {
        var oldPileIndex = GetPileIndex(lastValue);
        var newIndex = GetPileIndex(amount);

        if (lastValue < 0 || oldPileIndex != newIndex)
        {
            foreach (var ps in pileSizes)
            {
                ps.SetActive(false);
            }

            if (amount == 0 || newIndex < 0)
            {
                return;
            }

            pileSizes[newIndex].SetActive(true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    private int GetPileIndex(long amount)
    {
        if (amount <= 0)
        {
            return -1;
        }

        if (amount >= 10_000)
            return 3;
        else if (amount >= 5_000)
            return 2;
        else if (amount >= 1_000)
            return 1;

        return 0;
    }
}
