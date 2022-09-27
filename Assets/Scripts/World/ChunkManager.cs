using System.Collections.Generic;
using Shinobytes.Linq;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static bool StrictLevelRequirements = true;

    // Start is called before the first frame update
    [SerializeField] private IslandManager islandManager;

    private Chunk[] chunks;

    private Dictionary<TaskType, List<Chunk>> chunksByType = new Dictionary<TaskType, List<Chunk>>();

    void Start()
    {
        Init();
    }

    public void Init()
    {
        if (!islandManager) islandManager = FindObjectOfType<IslandManager>();
        if (chunks == null || chunks.Length == 0)
        {
            chunks = GameObject.FindGameObjectsWithTag("WorldChunk")
                        .Select(x => x.GetComponent<Chunk>())
                        .ToArray();
        }

        foreach (var chunk in chunks)
        {
            chunk.Start();
        }
    }

    public Chunk GetChunkAt(int x, int y)
    {
        return null;
    }

    public Chunk GetStarterChunk()
    {
        return chunks?.FirstOrDefault(x => x.IsStarterArea);
    }

    public IReadOnlyList<Chunk> GetChunksOfType(PlayerController playerRef, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        return chunks
            .Where(x => (x.Island == playerRef.Island || x.Island == FindPlayerIsland(playerRef)) && (x.Type == type /*|| x.SecondaryType == type*/))
            .OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
            //.Select(x => x.SecondaryType == type ? x.CreateSecondary() : x)
            .ToList();
    }

    public IReadOnlyList<Chunk> GetChunksOfType(IslandController island, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        return chunks
            .Where(x => x.Island == island && (x.Type == type /*|| x.SecondaryType == type*/))
            .OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
            //.Select(x => x.SecondaryType == type ? x.CreateSecondary() : x)
            .ToList();
    }

    public Chunk GetChunkOfType(PlayerController playerRef, TaskType type)
    {
        if (chunks == null || chunks.Length == 0)
            return null;

        var strictCombatLevel = StrictLevelRequirements;
        var refIsland = playerRef.Island;
        var refCombatLevel = playerRef.Stats.CombatLevel;
        var chunk = chunks
            .Where(x =>
            {
                if (x.Island != refIsland && x.Island != FindPlayerIsland(playerRef))
                {
                    return false;
                }
                if (x.Type != type /*&& x.SecondaryType != type*/)
                {
                    return false;
                }

                var activeSkill = playerRef.ActiveSkill;
                var skillStat = activeSkill.IsCombatSkill() ? playerRef.Stats[activeSkill] : null;

                if (strictCombatLevel)
                {
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
                        if (skillStat != null)
                        {
                            return x.RequiredSkilllevel <= skillStat.Level;
                        }
                    }
                }
                else
                {

                    if (type == TaskType.Fighting)
                    {
                        var requirement = Mathf.Max(x.RequiredCombatLevel, x.RequiredSkilllevel);
                        if (skillStat != null)
                        {
                            var level = Mathf.Max(playerRef.Stats.CombatLevel, skillStat.Level);
                            return level >= requirement;
                        }
                    }

                }


                return x.RequiredSkilllevel <= GetSkillLevel(playerRef, type);
            })
            .Highest(x => x.RequiredCombatLevel + x.RequiredSkilllevel);
        //.OrderByDescending(x => x.RequiredCombatLevel + x.RequiredSkilllevel)
        //.ThenBy(x => Vector3.Distance(x.transform.position, playerRef.transform.position))
        //.FirstOrDefault();

        if (chunk == null)
        {
            return null;
        }

        return chunk;
        //return chunk.SecondaryType == type
        //    ? chunk.CreateSecondary()
        //    : chunk;
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

    public IReadOnlyList<Chunk> GetChunks()
    {
        return chunks;
    }

    public List<Chunk> GetChunksOfType(TaskType type)
    {
        if (chunksByType.TryGetValue(type, out var value))
            return value;

        // cache miss, slow.
        var c = new List<Chunk>();
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
