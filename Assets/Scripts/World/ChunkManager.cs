using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] private IslandManager islandManager;

    private Chunk[] chunks;

    private Dictionary<TaskType, List<IChunk>> chunksByType = new Dictionary<TaskType, List<IChunk>>();

    void Start()
    {
        if (!islandManager) islandManager = FindObjectOfType<IslandManager>();

        chunks = GameObject.FindGameObjectsWithTag("WorldChunk")
                    .Select(x => x.GetComponent<Chunk>())
                    .ToArray();
    }

    public IChunk GetChunkAt(int x, int y)
    {
        return null;
    }

    public IChunk GetStarterChunk()
    {
        return chunks?.SingleOrDefault(x => x.IsStarterArea);
    }

    public IReadOnlyList<IChunk> GetChunksOfType(PlayerController playerRef, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        return chunks
            .Where(x => (x.Island == playerRef.Island || x.Island == FindPlayerIsland(playerRef)) && (x.Type == type || x.SecondaryType == type))
            .OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
            .Select(x => x.SecondaryType == type ? x.CreateSecondary() : x).ToList();
    }

    public IReadOnlyList<IChunk> GetChunksOfType(IslandController island, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        return chunks
            .Where(x => x.Island == island && (x.Type == type || x.SecondaryType == type))
            .OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
            .Select(x => x.SecondaryType == type ? x.CreateSecondary() : x).ToList();
    }

    public IChunk GetChunkOfType(PlayerController playerRef, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        var refIsland = playerRef.Island;
        var refCombatLevel = playerRef.Stats.CombatLevel;
        var chunk = chunks
            .Where(x =>
            {
                if (x.Island != refIsland && x.Island != FindPlayerIsland(playerRef))
                {
                    return false;
                }
                if (x.Type != type && x.SecondaryType != type)
                {
                    return false;
                }

                if (x.RequiredCombatLevel > refCombatLevel)
                {
                    return false;
                }

                if (type == TaskType.Fighting && x.RequiredSkilllevel > 1)
                {
                    if (playerRef.TrainingAll)
                    {
                        var attack = playerRef.Stats.GetCombatSkill(CombatSkill.Attack);
                        var defense = playerRef.Stats.GetCombatSkill(CombatSkill.Defense);
                        var strength = playerRef.Stats.GetCombatSkill(CombatSkill.Strength);
                        var req = x.RequiredSkilllevel;
                        return attack.Level >= req && defense.Level >= req && strength.Level >= req;
                    }

                    var skill = playerRef.GetActiveCombatSkillStat();
                    if (skill != null)
                    {
                        return x.RequiredSkilllevel <= skill.Level;
                    }
                }

                return x.RequiredSkilllevel <= GetSkillLevel(playerRef, type);
            })
            .OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
            .ThenBy(x => Vector3.Distance(x.transform.position, playerRef.transform.position))
            .FirstOrDefault();

        if (chunk == null)
        {
            return null;
        }

        return chunk.SecondaryType == type
            ? chunk.CreateSecondary()
            : chunk;
    }

    private int GetSkillLevel(PlayerController playerRef, TaskType type)
    {
        switch (type)
        {
            case TaskType.Cooking: return playerRef.Stats.Cooking.Level;
            case TaskType.Fishing: return playerRef.Stats.Fishing.Level;
            case TaskType.Woodcutting: return playerRef.Stats.Woodcutting.Level;
            case TaskType.Mining: return playerRef.Stats.Mining.Level;
            case TaskType.Crafting: return playerRef.Stats.Crafting.Level;
            case TaskType.Farming: return playerRef.Stats.Farming.Level;
        }
        return 1;
    }

    public IReadOnlyList<IChunk> GetChunks()
    {
        return chunks;
    }



    public List<IChunk> GetChunksOfType(TaskType type)
    {
        if (chunksByType.TryGetValue(type, out var value))
            return value;

        // cache miss, slow.
        var c = new List<IChunk>();
        var isCookingOrCrafting = type == TaskType.Cooking || type == TaskType.Crafting;
        var cl = this.chunks.OrderBy(x => x.RequiredCombatLevel + x.RequiredSkilllevel).ToArray();
        for (var i = 0; i < this.chunks.Length; i++)
        {
            var chunk = cl[i];
            if (chunk.ChunkType == type || (isCookingOrCrafting && (chunk.ChunkType == TaskType.Cooking || chunk.ChunkType == TaskType.Crafting)))
            {
                c.Add(chunk);
            }
        }
        
        return chunksByType[type] = c;
    }

    private IslandController FindPlayerIsland(PlayerController player)
    {
        return islandManager.FindPlayerIsland(player);
    }
}
