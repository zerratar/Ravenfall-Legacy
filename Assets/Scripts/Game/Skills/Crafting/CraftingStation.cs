using System;
using UnityEngine;

public class CraftingStation : MonoBehaviour
{
    public int Level = 1;
    public decimal ExpPerResource => 10;
    public CraftingStationType StationType = CraftingStationType.Crafting;

    public decimal GetExperience(PlayerController playerController)
    {
        var rsx = GetCraftingCost(playerController);
        var fishWood = StationType == CraftingStationType.Cooking ? rsx.Fish : rsx.Wood;
        var wheatOre = StationType == CraftingStationType.Cooking ? rsx.Wheat : rsx.Ore;
        return ExpPerResource * fishWood + ExpPerResource * wheatOre;
    }

    public bool Craft(PlayerController player)
    {
        return true;
    }

    private int GetPlayerSkills(PlayerController playerController)
    {
        var resx = playerController.Stats;
        return StationType == CraftingStationType.Cooking ? resx.Cooking.Level : resx.Crafting.Level;
    }
    private RavenNest.Models.Resources GetCraftingCost(PlayerController playerController)
    {
        var level = GetPlayerSkills(playerController);

        var fishWoodUse = Mathf.FloorToInt(level / 10f) + 1;
        var wheatOreUse = Mathf.FloorToInt(level / 10f) + 1;

        var a = fishWoodUse;
        var b = wheatOreUse;

        if (StationType == CraftingStationType.Cooking)
        {
            return new RavenNest.Models.Resources { Fish = a, Wheat = b };
        }

        return new RavenNest.Models.Resources { Wood = a, Ore = b };
    }
}

public enum CraftingStationType
{
    Crafting,
    Cooking
}
