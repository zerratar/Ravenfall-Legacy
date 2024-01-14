using Shinobytes.Linq;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;

    private RockController[] miningSpots;
    private FishingController[] fishingSpots;
    private EnemyController[] enemies;
    private TreeController[] trees;
    private CraftingStation[] craftingStations;
    private FarmController[] farmingPatches;
    private GatherController[] gatheringSpots;
    private ChunkTask task;

    public TaskType Type;

    public Vector3 CenterPoint;
    public bool IsStarterArea;

    public int RequiredCombatLevel = 1;
    public int RequiredSkilllevel = 1;
    public GameManager Game;

    public virtual Vector3 CenterPointWorld => CenterPoint + transform.position;
    public virtual TaskType ChunkType => Type;

    public virtual IslandController Island { get; private set; }
    public virtual int GetRequiredCombatLevel() => RequiredCombatLevel;
    public virtual int GetRequiredSkillLevel() => RequiredSkilllevel;

    [System.NonSerialized] private bool started;

    // Start is called before the first frame update
    public void Start()
    {
        //var arena = GetComponentInChildren<ArenaController>();
        if (started) return;
        if (!Game) Game = FindAnyObjectByType<GameManager>();
        Island = GetComponentInParent<IslandController>();

        enemies = gameObject.GetComponentsInChildren<EnemyController>();
        trees = gameObject.GetComponentsInChildren<TreeController>();
        fishingSpots = gameObject.GetComponentsInChildren<FishingController>();
        miningSpots = gameObject.GetComponentsInChildren<RockController>();
        craftingStations = gameObject.GetComponentsInChildren<CraftingStation>();
        farmingPatches = gameObject.GetComponentsInChildren<FarmController>();
        gatheringSpots = gameObject.GetComponentsInChildren<GatherController>();

        task = GetChunkTask(Type);

        started = true;

        gameObject.name = $"{Type} Lv. " + Mathf.Max(RequiredSkilllevel, RequiredCombatLevel);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Vector3 GetPlayerSpawnPoint()
    {
        if (!IsStarterArea || !spawnPoint) return Vector3.zero;
        return spawnPoint.transform.position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual object GetTaskTarget(PlayerController player)
    {
        return task?.GetTarget(player);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool IsTaskCompleted(PlayerController player, object target)
    {
        return task == null || task.IsCompleted(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        return task != null && task.CanExecute(player, target, out reason);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void TargetAcquired(PlayerController player, object target)
    {
        task.TargetAcquired(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool ExecuteTask(PlayerController player, object target)
    {
        return task != null && task.Execute(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double CalculateExpFactor(PlayerController playerController)
    {
        return CalculateExpFactor(Type, playerController);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double CalculateExpFactor(TaskType taskType, PlayerController player)
    {
        try
        {

            // TODO: less dy´namic, more static, this is not reliable enough..

            if (player == null || !player) return 0;
            var maxFactor = GameMath.Exp.MaxExpFactorFromIsland;

            var s = player.Stats;

            var minLv = IslandManager.IslandLevelRangeMin[this.Island.Island];
            var maxLv = IslandManager.IslandLevelRangeMax[this.Island.Island];
            var efLv = IslandManager.IslandMaxEffect[this.Island.Island];

            if (RequiredCombatLevel > 1 && s.CombatLevel < minLv)
            // only break out if we are not high enough combat level, we need to check individual skill stat later instead so if we train a skill that is under the level of required combat level it should still gain exp.
            {
                player.IsGainingExp = false;
                return 0;
            }

            var skillLevel = player.GetSkill(taskType);
            if (skillLevel != null && RequiredSkilllevel > 1 && (skillLevel.Level < minLv || skillLevel.Level > maxLv))
            {
                player.IsGainingExp = false;
                return 0;
            }

            player.IsGainingExp = true;

            // TODO: rewrite to use minLv, maxLv and efLv instead.

            var allChunks = Game.Chunks.GetChunksOfType(taskType);
            var chunkIndex = allChunks.IndexOf(this);// System.Array.IndexOf(, this);
            var isLastChunk = chunkIndex + 1 >= allChunks.Count;

            //CalculateExpFactor

            // last chunk must always be maxFactor, regardless of task.
            // since ther are no "better" places to train. This has to be it.
            if (isLastChunk) return maxFactor;
            var a = RequiredCombatLevel;
            var b = RequiredSkilllevel;
            var requirement = a > b ? a : b;
            var nextChunk = allChunks[chunkIndex + 1];
            a = nextChunk.GetRequiredCombatLevel();
            b = nextChunk.GetRequiredSkillLevel();
            var nextRequirement = a > b ? a : b;
            var secondary = GameMath.MaxLevel;
            if (a > 1) secondary = Mathf.Min(secondary, s.CombatLevel);
            switch (taskType)
            {
                case TaskType.Fighting:
                    {
                        var skill = player.GetActiveSkillStat();

                        if (skill == player.Stats.Health)
                        {
                            // training all. We can't use combat level. Use average of atk,def,str?
                            var st = player.Stats;
                            var lv = (st.Strength.Level + st.Defense.Level + st.Attack.Level) / 3;
                            return CalculateExpFactor(player, Math.Min(lv, secondary), requirement, nextRequirement);
                        }

                        // if target skill is above max level, we aint gaining exp.
                        if (skill.Level > maxLv)
                        {
                            player.IsGainingExp = false;
                            return 0;
                        }

                        return CalculateExpFactor(player, Math.Min(skill.Level, secondary), requirement, nextRequirement);
                    }
                case TaskType.Mining:
                    return CalculateExpFactor(player, Math.Min(s.Mining.Level, secondary), requirement, nextRequirement);
                case TaskType.Fishing:
                    return CalculateExpFactor(player, Math.Min(s.Fishing.Level, secondary), requirement, nextRequirement);
                case TaskType.Woodcutting:
                    return CalculateExpFactor(player, Math.Min(s.Woodcutting.Level, secondary), requirement, nextRequirement);
                case TaskType.Crafting:
                    return CalculateExpFactor(player, Math.Min(s.Crafting.Level, secondary), requirement, nextRequirement);
                case TaskType.Farming:
                    return CalculateExpFactor(player, Math.Min(s.Farming.Level, secondary), requirement, nextRequirement);
                case TaskType.Cooking:
                    return CalculateExpFactor(player, Math.Min(s.Cooking.Level, secondary), requirement, nextRequirement);
                case TaskType.Gathering:
                    return CalculateExpFactor(player, Math.Min(s.Gathering.Level, secondary), requirement, nextRequirement);
                case TaskType.Alchemy:
                    return CalculateExpFactor(player, Math.Min(s.Alchemy.Level, secondary), requirement, nextRequirement);
            }

            return 1;
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Error Calculating Exp Factor: " + exc);
            player.IsGainingExp = false;
            return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateExpFactor(PlayerController player, double level, double requirement, double nextRequirement)
    {
        var result = 1d;
        try
        {
            if (nextRequirement <= requirement) return result;

            // if the current level is more than 2x of the required next place. You don't receive any more exp.
            //var upperBounds = nextRequirement * 2.0;
            //if (level >= upperBounds) return 0;

            var delta = nextRequirement - requirement;
            var midPoint = delta / 2f;
            if (level <= (nextRequirement + midPoint))
                return result;

            var upperBounds = requirement + (delta * 2);
            if (level >= upperBounds)
            {
                result = 0;
                return result;
            }

            if (level > nextRequirement)
            {
                var min = level - nextRequirement;
                var max = upperBounds - nextRequirement;
                return result = GameMath.Lerp(1, 0, min / max);
            }

            return result;
        }
        finally
        {
            player.IsGainingExp = result > 0;
        }
    }

    private ChunkTask GetChunkTask(TaskType type) // ArenaController arena,
    {
        switch (type)
        {
            case TaskType.Fighting: return new FightingTask(() => enemies);
            case TaskType.Mining: return new MiningTask(() => miningSpots);
            case TaskType.Fishing: return new FishingTask(() => fishingSpots);
            case TaskType.Woodcutting: return new WoodcuttingTask(() => trees);
            case TaskType.Farming: return new FarmingTask(() => farmingPatches);
            case TaskType.Crafting: return new CraftingTask(() => craftingStations.AsList(x => x.StationType == CraftingStationType.Crafting));
            case TaskType.Cooking: return new CookingTask(() => craftingStations.AsList(x => x.StationType == CraftingStationType.Cooking));
            case TaskType.Alchemy: return new AlchemyTask(() => craftingStations.AsList(x => x.StationType == CraftingStationType.Brewing));
            case TaskType.Gathering: return new GatheringTask(() => gatheringSpots);
            default: return null;
        }
    }

    internal void SetTargetInvalid(object taskTarget)
    {
        task.SetTargetInvalid(taskTarget);
    }
}

public enum TaskExecutionStatus
{
    NotReady,
    OutOfRange,
    Ready,
    //InsufficientResources,
    InvalidTarget,
}