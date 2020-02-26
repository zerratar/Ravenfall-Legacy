using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageManager : MonoBehaviour
{

    [SerializeField] private TownHallManager townHallManager;
    [SerializeField] private TownHouseManager townHouseManager;

    public TownHallManager TownHall => townHallManager;
    public TownHouseManager TownHouses => townHouseManager;

    // Start is called before the first frame update
    void Start()
    {
        if (!townHallManager) townHallManager = FindObjectOfType<TownHallManager>();
        if (!townHouseManager) townHouseManager = FindObjectOfType<TownHouseManager>();
    }

    public void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
    {
        // do something
        townHouseManager.SetHouses(houses);
    }

    public void SetSlotCount(int count)
    {
        townHouseManager.SetSlotCount(count);
    }

    public void SetTierByLevel(int level)
    {
        townHallManager.SetTierByLevel(level);
    }

    public void SetTier(int tier)
    {
        townHallManager.SetTier(tier);
    }
}
