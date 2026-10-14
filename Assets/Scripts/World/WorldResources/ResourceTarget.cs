using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One tree or gather spot while the game runs: what players target, chop and gather. Made once per instance
/// when its <see cref="ResourceField"/> first hands out its targets, and kept for the field's life, so tasks can
/// hold on to it and compare it by reference. No GameObject; the field draws it standing or used up.
/// </summary>
/// <remarks>
/// Everyone who has targeted it gets the experience when it falls or is gathered, hit or not: on a crowded
/// server most players would otherwise never gain anything. That is deliberate.
/// </remarks>
public sealed class ResourceTarget
{
    private readonly List<PlayerController> targeters = new List<PlayerController>(4);
    private int health;
    private float respawnSeconds;
    private bool respawnLocked;
    private string name;

    public ResourceTarget(ResourceField field, int index, ResourceType type, Vector3 position, IslandController island, int level)
    {
        Field = field;
        Index = index;
        Type = type;
        Position = position;
        Island = island;
        Level = level;
        health = MaxHealth;
        respawnSeconds = BaseRespawnSeconds;
    }

    public ResourceField Field { get; }
    public int Index { get; }
    public ResourceType Type { get; }
    public Vector3 Position { get; }
    public IslandController Island { get; }

    /// <summary>The level the damage roll is against: the level its chunk requires.</summary>
    public int Level { get; }

    public float ActionDistance => Type ? Type.actionDistance : 3f;
    public bool KneelToGather => !Type || Type.kneelToGather;
    public bool IsAvailable => !Field.IsDepleted(Index);

    /// <summary>When it comes back, once used up.</summary>
    public float RespawnAt { get; private set; }

    /// <summary>The players who have targeted it since it last came back.</summary>
    public IReadOnlyList<PlayerController> Targeters => targeters;

    /// <summary>For logs: the type, the field's place in the scene, and the index.</summary>
    public string Name => name ??= (Type ? Type.name : "Resource") + " #" + Index + " in " + Field.gameObject.GetHierarchyPath();

    private int MaxHealth => Field.TreeHealth > 0 ? Field.TreeHealth : Type ? Mathf.Max(1, Type.health) : 1;
    private float BaseRespawnSeconds => Field.RespawnSeconds > 0f ? Mathf.Max(1f, Field.RespawnSeconds) : Type ? Mathf.Max(1f, Type.respawnSeconds) : 20f;

    /// <summary>
    /// The target for a player in a chunk: the nearest one standing, give or take a random 0 to 15 m so players
    /// spread out. Also speeds up the chunk's respawns when a quarter or less stand, and slows them back down
    /// when half or more do. Null when none stand.
    /// </summary>
    public static ResourceTarget PickFor(PlayerController player, IReadOnlyList<ResourceTarget> targets)
    {
        if (targets == null || targets.Count == 0) return null;

        ResourceTarget best = null;
        var bestScore = float.MaxValue;
        var standing = 0;
        for (var i = 0; i < targets.Count; ++i)
        {
            var target = targets[i];
            if (!target.IsAvailable) continue;
            standing++;
            var score = Vector3.Distance(player.Position, target.Position) + Random.value * 15f;
            if (score < bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        var left = (float)standing / targets.Count;
        if (left <= 0.25f)
        {
            for (var i = 0; i < targets.Count; ++i) targets[i].DecreaseRespawnTime();
        }
        else if (left >= 0.5f)
        {
            for (var i = 0; i < targets.Count; ++i) targets[i].IncreaseRespawnTime();
        }

        return best;
    }

    public void AddTargeter(PlayerController player)
    {
        if (!player) return;
        for (var i = 0; i < targeters.Count; ++i)
        {
            var other = targeters[i];
            if (ReferenceEquals(other, player)) return;
            // the same player back after a rejoin takes the old one's place
            if (other && other.Id == player.Id)
            {
                targeters[i] = player;
                return;
            }
        }
        targeters.Add(player);
    }

    /// <summary>A hit on a tree; true when it falls.</summary>
    public bool DoDamage(PlayerController player, int amount)
    {
        if (!IsAvailable) return false;
        AddTargeter(player);
        health -= amount;
        if (health > 0) return false;
        Deplete();
        return true;
    }

    /// <summary>Gathers the spot; false when it is already gone.</summary>
    public bool Gather(PlayerController player)
    {
        if (!IsAvailable) return false;
        AddTargeter(player);
        Deplete();
        return true;
    }

    /// <summary>Few left standing in the chunk: come back a second sooner, down to one, once per respawn.</summary>
    public void DecreaseRespawnTime()
    {
        if (respawnSeconds <= 1f || respawnLocked) return;
        respawnSeconds = Mathf.Clamp(respawnSeconds - 1f, 1f, BaseRespawnSeconds);
        respawnLocked = true;
    }

    /// <summary>Plenty standing: back towards the type's own respawn time, once per respawn.</summary>
    public void IncreaseRespawnTime()
    {
        if (respawnSeconds >= BaseRespawnSeconds || respawnLocked) return;
        respawnSeconds = Mathf.Clamp(respawnSeconds + 1f, 1f, BaseRespawnSeconds);
        respawnLocked = true;
    }

    private void Deplete()
    {
        Field.SetDepleted(Index, true);
        RespawnAt = Time.time + respawnSeconds;
        Field.QueueRespawn(this);
    }

    internal void Respawn()
    {
        targeters.Clear();
        health = MaxHealth;
        respawnLocked = false;
        Field.SetDepleted(Index, false);
    }
}
