using UnityEngine;

/// <summary>
/// Animals placed as scenery (the Low Poly Animated Animals pack's wandering cows on Ravebridge's Lumbridge, from the
/// OSRS layout) look just like the island's enemies of their kind, so in hero mode some cows took hits and others did
/// nothing (playtest 2026-11-02, 16). When the game loads, each one with an enemy of its kind on the same island becomes
/// a copy of that enemy where it stood: in the enemy's fighting area when it is close to it (that area's characters
/// fight it too), else on its own where hero mode still finds it. An animal with no enemy of its kind stays scenery.
/// </summary>
public static class SceneryAnimals
{
    /// <summary>This close to an enemy of its kind, the animal joins that enemy's fighting area.</summary>
    private const float FieldReach = 40f;

    public static int MakeFightable(IslandController island)
    {
        if (!island) return 0;
        var enemies = island.GetComponentsInChildren<EnemyController>(true);
        var made = 0;
        foreach (var animal in island.GetComponentsInChildren<LowPolyAnimalPack.WanderScript>(true))
        {
            var kind = Kind(animal.name);
            EnemyController template = null;
            var best = float.MaxValue;
            foreach (var e in enemies)
            {
                if (!e || Kind(e.name) != kind) continue;
                var distance = (e.transform.position - animal.transform.position).sqrMagnitude;
                if (distance < best) { best = distance; template = e; }
            }
            if (!template) continue;

            var at = animal.transform;
            var parent = best < FieldReach * FieldReach ? template.transform.parent : at.parent;
            var enemy = Object.Instantiate(template, at.position, at.rotation, parent);
            enemy.name = template.name;
            enemy.Stats = template.Stats * 1f;
            enemy.EquipmentStats = template.EquipmentStats;
            Object.Destroy(animal.gameObject);
            made++;
        }
        return made;
    }

    /// <summary>"Cow (2)" and "Cow" are both a cow; "RavenMMO the Cow" is someone in particular.</summary>
    private static string Kind(string name)
    {
        var kind = name.ToLowerInvariant().Trim();
        var bracket = kind.IndexOf(" (");
        return bracket > 0 ? kind.Substring(0, bracket) : kind;
    }
}
