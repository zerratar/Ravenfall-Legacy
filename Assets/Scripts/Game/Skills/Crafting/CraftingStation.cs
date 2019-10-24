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

    public RavenNest.Models.Resources GetCraftingCost(PlayerController playerController)
    {
        var (fishWood, wheatOre) = GetPlayerResources(playerController);
        var level = GetPlayerSkills(playerController);

        var fishWoodUse = Mathf.FloorToInt(level / 10f) + 1;
        var wheatOreUse = Mathf.FloorToInt(level / 10f) + 1;

        var a = fishWood >= fishWoodUse ? fishWoodUse : fishWood;
        var b = wheatOre >= wheatOreUse ? wheatOreUse : wheatOre;

        if (StationType == CraftingStationType.Cooking)
        {
            return new RavenNest.Models.Resources { Fish = a, Wheat = b };
        }

        return new RavenNest.Models.Resources { Wood = a, Ore = b };
    }

    private Tuple<decimal, decimal> GetPlayerResources(PlayerController playerController)
    {
        var resx = playerController.Resources;
        return StationType == CraftingStationType.Cooking
            ? new Tuple<decimal, decimal>(resx.Fish, resx.Wheat)
            : new Tuple<decimal, decimal>(resx.Wood, resx.Ore);
    }

    private int GetPlayerSkills(PlayerController playerController)
    {
        var resx = playerController.Stats;
        return StationType == CraftingStationType.Cooking ? resx.Cooking.Level : resx.Crafting.Level;
    }

    public bool Craft(PlayerController player)
    {
        //var proc = player.Skills.Crafting.CurrentValue / Level;
        //return proc * UnityEngine.Random.value >= 0.5f;
        return true;
    }
}

public enum CraftingStationType
{
    Crafting,
    Cooking
}
