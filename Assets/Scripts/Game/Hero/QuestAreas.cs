using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Where the hero's island quests can be done, for the maps (HeroBigMap, HeroMinimap): every collect step under way
/// that is not yet complete, as areas on the island. A kill step's are where its enemies are (goblins, cows), a gather
/// step's where that skill's rocks, trees, fish or patches are. The maps draw them as soft red circles, as online
/// games mark a quest's area (the user, 2026-11-04). Worked out every few seconds: quests, enemies and resources
/// hardly move between.
/// </summary>
public static class QuestAreas
{
    public readonly struct Area
    {
        public readonly Vector3 Centre;
        public readonly float Radius;
        public Area(Vector3 centre, float radius) { Centre = centre; Radius = radius; }
    }

    private const float RefreshSeconds = 3f;
    /// <summary>Points closer than this to an area's middle belong to it.</summary>
    private const float Join = 28f;
    private const float Margin = 6f;
    private const float Smallest = 12f;
    private const int MostAreas = 12;

    private static readonly List<Area> areas = new List<Area>();
    private static readonly List<Vector3> points = new List<Vector3>();
    private static PlayerController lastHero;
    private static float nextRefresh;

    /// <summary>The areas of the hero's collect steps under way; empty off Ravebridge or with none.</summary>
    public static IReadOnlyList<Area> For(PlayerController hero)
    {
        if (hero != lastHero || Time.unscaledTime >= nextRefresh)
        {
            lastHero = hero;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            try
            {
                Refresh(hero);
            }
            catch (Exception exc)
            {
                areas.Clear();
                Shinobytes.Debug.LogError("Quest areas failed: " + exc.Message);
            }
        }
        return areas;
    }

    private static void Refresh(PlayerController hero)
    {
        areas.Clear();
        if (!hero || !hero.InRavebridge || !hero.Island || RavebridgeQuests.Data?.quests == null) return;
        var state = RavebridgeQuests.StateOf(hero);
        if (state == null) return;

        foreach (var quest in RavebridgeQuests.Data.quests)
        {
            var at = RavebridgeQuests.StepOf(state, quest.key);
            if (at <= 0 || quest.steps == null || at >= quest.steps.Count) continue;
            var step = quest.steps[at];
            points.Clear();
            if (step.type == "loot" && step.loot != null)
            {
                // a loot step's items: where the enemies are whose loot table drops them (bones: the swamp's skeletons)
                foreach (var wanted in step.loot)
                {
                    if (state.Loot.TryGetValue(wanted.Key, out var got) && got >= wanted.Value) continue;
                    foreach (var match in DroppersOf(wanted.Key)) EnemiesOf(hero.Island, match);
                }
            }
            else if (step.type == "collect" && !string.IsNullOrEmpty(step.item))
            {
                if (state.Items.TryGetValue(step.item, out var have) && have >= step.amount) continue;
                if (!string.IsNullOrEmpty(step.kill)) EnemiesOf(hero.Island, step.kill);
                else if (!string.IsNullOrEmpty(step.skill) && Enum.TryParse<TaskType>(step.skill, out var task)) ResourcesOf(hero.Island, task);
            }
            Cluster();
            if (areas.Count >= MostAreas) break;
        }
    }

    /// <summary>The enemies a kill step counts (their names hold its key, as RavebridgeQuests.Collect counts them).</summary>
    private static void EnemiesOf(IslandController island, string kill)
    {
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>())
        {
            if (enemy && enemy.Island == island && enemy.name.ToLowerInvariant().Contains(kill)) points.Add(enemy.transform.position);
        }
    }

    /// <summary>The loot tables' matches (enemy names) that drop an item; not the bosses' (wave bosses only) or the
    /// catch-all table.</summary>
    private static IEnumerable<string> DroppersOf(string item)
    {
        var tables = RavebridgeLoot.Data?.tables;
        if (tables == null) yield break;
        foreach (var t in tables)
        {
            if (t?.drops == null || string.IsNullOrEmpty(t.match) || t.match.StartsWith("boss ")) continue;
            foreach (var d in t.drops)
            {
                if (d.item != item) continue;
                yield return t.match;
                break;
            }
        }
    }

    /// <summary>A skill's spots on the island: rocks, fishing spots and patches, and the trees and herbs of its fields.</summary>
    private static void ResourcesOf(IslandController island, TaskType task)
    {
        foreach (var chunk in island.GetComponentsInChildren<Chunk>())
        {
            if (chunk.Type != task) continue;
            foreach (var rock in chunk.GetComponentsInChildren<RockController>()) points.Add(rock.transform.position);
            foreach (var fishing in chunk.GetComponentsInChildren<FishingController>()) points.Add(fishing.transform.position);
            foreach (var farm in chunk.GetComponentsInChildren<FarmController>()) points.Add(farm.transform.position);
            foreach (var field in chunk.GetComponentsInChildren<ResourceField>())
            {
                foreach (var instance in field.Instances) points.Add(instance.position);
            }
        }
    }

    /// <summary>The points as areas: each joins the first area whose middle is near, or starts one; then each area's
    /// circle reaches its farthest point.</summary>
    private static void Cluster()
    {
        if (points.Count == 0) return;
        var groups = new List<(Vector3 Sum, int Count, List<Vector3> Members)>();
        foreach (var p in points)
        {
            var joined = false;
            for (var i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var middle = g.Sum / g.Count;
                if (Flat(p - middle).sqrMagnitude > Join * Join) continue;
                g.Members.Add(p);
                groups[i] = (g.Sum + p, g.Count + 1, g.Members);
                joined = true;
                break;
            }
            if (!joined) groups.Add((p, 1, new List<Vector3> { p }));
        }

        foreach (var g in groups)
        {
            var middle = g.Sum / g.Count;
            var reach = 0f;
            foreach (var m in g.Members) reach = Mathf.Max(reach, Flat(m - middle).magnitude);
            areas.Add(new Area(middle, Mathf.Max(Smallest, reach + Margin)));
            if (areas.Count >= MostAreas) return;
        }
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
