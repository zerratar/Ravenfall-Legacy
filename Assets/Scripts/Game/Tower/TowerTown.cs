using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     The root of a tower town scene (docs/tower-design.md, section 4): a calm place between the bands with no
///     fighting. Its people (<see cref="IslandNpc"/>s under it, keyed as in RavenNest.Combat.Tower's town) stand about,
///     say their lines, and give each player their own quest chain (!tower talk). Players who come here stroll between
///     the <see cref="Strolls"/> spots. Loaded next to MainWorld like a floor, and beside one.
/// </summary>
public sealed class TowerTown : TowerPlace
{
    /// <summary>The town that is loaded now, or null.</summary>
    public static TowerTown Loaded { get; private set; }

    /// <summary>The town's key in RavenNest.Combat.Tower.Towns ("mossgate").</summary>
    public string Key = "mossgate";

    /// <summary>Spots players stroll to and linger at: every child is one.</summary>
    public Transform Strolls;

    /// <summary>
    ///     The resting place's spots (!tower rest, as in the Onsen): every child is one, its name saying how the player
    ///     rests there ("Sit", "Meditate" or "Sleep" at the start), placed and turned as the player will be.
    /// </summary>
    public Transform RestSpots;

    private readonly Dictionary<Transform, PlayerController> restTaken = new Dictionary<Transform, PlayerController>();

    private readonly List<IslandNpc> people = new List<IslandNpc>();

    public TowerTownDefinition Town => Tower.Town(Key);

    public IReadOnlyList<IslandNpc> People => people;

    private void OnEnable()
    {
        Loaded = this;
        people.Clear();
        people.AddRange(GetComponentsInChildren<IslandNpc>(true));
        // their idle lines are the town's texts (UI_TOWN_ELM_1, ...); the ones who give a chain show a talk mark
        foreach (var person in people)
        {
            var key = person.Key;
            var lines = new List<string>();
            for (var i = 1; i <= 6; ++i)
            {
                var field = typeof(Localization).GetField(("UI_TOWN_" + key + "_" + i).ToUpperInvariant(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field?.GetValue(null) is string line) lines.Add(line);
            }
            person.Lines = () => lines;
            var definition = Town;
            var givesChain = false;
            if (definition != null) foreach (var n in definition.People) if (n.Key == key && !string.IsNullOrEmpty(n.Chain)) givesChain = true;
            person.MarkSource = () => givesChain ? IslandNpc.Mark.Available : IslandNpc.Mark.Talk;
        }
    }

    private void OnDisable()
    {
        if (Loaded == this) Loaded = null;
    }

    /// <summary>One of the town's people by key ("elm"), or null.</summary>
    public IslandNpc Person(string key)
    {
        foreach (var p in people) if (p && p.Key == key) return p;
        return null;
    }

    /// <summary>A free resting spot for the player (the nearest free one), or null when every spot is taken.</summary>
    public Transform TakeRestSpot(PlayerController player)
    {
        if (!RestSpots || !player) return null;
        Transform best = null;
        var bestDistance = float.MaxValue;
        foreach (Transform spot in RestSpots)
        {
            if (restTaken.TryGetValue(spot, out var who) && who && !who.isDestroyed && who.towerHandler != null && who.towerHandler.Resting && who != player) continue;
            var d = (spot.position - player.Position).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = spot; }
        }
        if (best) restTaken[best] = player;
        return best;
    }

    public void ReleaseRestSpot(Transform spot, PlayerController player)
    {
        if (spot && restTaken.TryGetValue(spot, out var who) && who == player) restTaken.Remove(spot);
    }

    /// <summary>A spot to stroll to, picked at random, near the town's spawn when it has none.</summary>
    public Vector3 RandomStroll()
    {
        if (Strolls && Strolls.childCount > 0) return Strolls.GetChild(Random.Range(0, Strolls.childCount)).position;
        var offset = Random.insideUnitCircle * 8f;
        return Spawn + new Vector3(offset.x, 0f, offset.y);
    }
}
