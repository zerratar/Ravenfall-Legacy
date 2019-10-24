using RavenNest.Models;
using Resources = RavenNest.Models.Resources;

public static class StatisticsResourcesExtensions
{

    public static bool SameAmount(this Statistics res, Statistics other)
    {
        if (other == null) return false;
        return FieldComparer.Same(res, other, nameof(Statistics.Id));
    }

    public static bool SameAmount(this Resources res, Resources other)
    {
        if (other == null) return false;
        return res.Wood == other.Wood && res.Ore == other.Ore && res.Fish == other.Fish &&
               res.Magic == other.Magic && res.Wheat == other.Wheat && res.Arrows == other.Arrows &&
               res.Coins == other.Coins;
    }

    public static decimal[] ToList(this Statistics s)
    {
        if (s == null) return new decimal[0];
        return new decimal[]
        {
            s.RaidsWon,
            s.RaidsLost,
            s.RaidsJoined,

            s.DuelsWon,
            s.DuelsLost,

            s.PlayersKilled,
            s.EnemiesKilled,

            s.ArenaFightsJoined,
            s.ArenaFightsWon,

            s.TotalDamageDone,
            s.TotalDamageTaken,
            s.DeathCount,

            s.TotalWoodCollected,
            s.TotalOreCollected,
            s.TotalFishCollected,
            s.TotalWheatCollected,

            s.CraftedWeapons,
            s.CraftedArmors,
            s.CraftedPotions,
            s.CraftedRings,
            s.CraftedAmulets,

            s.CookedFood,

            s.ConsumedPotions,
            s.ConsumedFood,

            s.TotalTreesCutDown
        };
    }

    public static decimal[] ToList(this Resources res)
    {
        return new[]
        {
            res.Wood,
            res.Fish,
            res.Ore,
            res.Wheat,
            res.Coins,
            res.Magic,
            res.Arrows
        };
    }
}
