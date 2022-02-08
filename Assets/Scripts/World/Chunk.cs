using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Chunk : MonoBehaviour, IChunk
{
    [SerializeField] private GameObject spawnPoint;

    private RockController[] miningSpots;
    private FishingController[] fishingSpots;
    private EnemyController[] enemies;
    private TreeController[] trees;
    private CraftingStation[] craftingStations;
    private FarmController[] farmingPatches;

    private ChunkTask task;
    private ChunkTask secondaryTask;

    public TaskType Type;
    public TaskType SecondaryType = TaskType.None;

    public Vector3 CenterPoint;
    public bool IsStarterArea;

    public int RequiredCombatLevel = 1;
    public int RequiredSkilllevel = 1;
    public GameManager Game;

    public Vector3 CenterPointWorld => CenterPoint + transform.position;
    public TaskType ChunkType => Type;

    public IslandController Island { get; private set; }
    public int GetRequiredCombatLevel() => RequiredCombatLevel;
    public int GetRequiredSkillLevel() => RequiredSkilllevel;

    // Start is called before the first frame update
    void Start()
    {
        //var arena = GetComponentInChildren<ArenaController>();
        if (!Game) Game = FindObjectOfType<GameManager>();
        Island = GetComponentInParent<IslandController>();
        task = GetChunkTask(Type); // arena
        if (SecondaryType != TaskType.None)
        {
            secondaryTask = GetChunkTask(SecondaryType); // arena
        }

        var enemyContainer = gameObject.transform.Find("Enemies");
        if (enemyContainer)
        {
            enemies = enemyContainer.GetComponentsInChildren<EnemyController>();
        }

        var treeContainer = gameObject.transform.Find("Trees");
        if (treeContainer)
        {
            trees = treeContainer.GetComponentsInChildren<TreeController>();
        }

        var fishingContainer = gameObject.transform.Find("Fishingspots");
        if (fishingContainer)
        {
            fishingSpots = fishingContainer.GetComponentsInChildren<FishingController>();
        }

        var miningContainer = gameObject.transform.Find("Rocks");
        if (miningContainer)
        {
            miningSpots = miningContainer.GetComponentsInChildren<RockController>();
        }

        var craftingStationContainer = gameObject.transform.Find("CraftingStations");
        if (craftingStationContainer)
        {
            craftingStations = craftingStationContainer.GetComponentsInChildren<CraftingStation>();
        }

        var farmingPatchesContainer = gameObject.transform.Find("FarmingPatches");
        if (farmingPatchesContainer)
        {
            farmingPatches = farmingPatchesContainer.GetComponentsInChildren<FarmController>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPlayerSpawnPoint()
    {
        if (!IsStarterArea || !spawnPoint) return Vector3.zero;
        return spawnPoint.transform.position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object GetTaskTarget(PlayerController player)
    {
        return task?.GetTarget(player);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTaskCompleted(PlayerController player, object target)
    {
        return task == null || task.IsCompleted(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        return task != null && task.CanExecute(player, target, out reason);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TargetAcquired(PlayerController player, object target)
    {
        task.TargetAcquired(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExecuteTask(PlayerController player, object target)
    {
        return task != null && task.Execute(player, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CalculateExpFactor(PlayerController playerController)
    {
        return CalculateExpFactor(Type, playerController);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CalculateExpFactor(TaskType taskType, PlayerController playerController)
    {
        var maxFactor = 1d;
        var allChunks = Game.Chunks.GetChunksOfType(taskType);
        var chunkIndex = allChunks.IndexOf(this);// System.Array.IndexOf(, this);
        var isLastChunk = chunkIndex + 1 >= allChunks.Count;

        // last chunk must always be maxFactor, regardless of task.
        // since ther are no "better" places to train. This has to be it.
        if (isLastChunk) return maxFactor;
        var s = playerController.Stats;
        var a = RequiredCombatLevel;
        var b = RequiredSkilllevel;
        var requirement = a > b ? a : b;
        var nextChunk = allChunks[chunkIndex + 1];
        a = nextChunk.GetRequiredCombatLevel();
        b = nextChunk.GetRequiredSkillLevel();
        var nextRequirement = a > b ? a : b;
        var secondary = 0;
        if (a > 1) secondary = s.CombatLevel;
        switch (taskType)
        {
            case TaskType.Fighting:
                {
                    var skill = playerController.GetActiveSkillStat();
                    return CalculateExpFactor(System.Math.Max(skill.Level, secondary), requirement, nextRequirement);
                }
            case TaskType.Mining:
                return CalculateExpFactor(System.Math.Max(s.Mining.Level, secondary), requirement, nextRequirement);
            case TaskType.Fishing:
                return CalculateExpFactor(System.Math.Max(s.Fishing.Level, secondary), requirement, nextRequirement);
            case TaskType.Woodcutting:
                return CalculateExpFactor(System.Math.Max(s.Woodcutting.Level, secondary), requirement, nextRequirement);
            case TaskType.Crafting:
                return CalculateExpFactor(System.Math.Max(s.Crafting.Level, secondary), requirement, nextRequirement);
            case TaskType.Farming:
                return CalculateExpFactor(System.Math.Max(s.Farming.Level, secondary), requirement, nextRequirement);
            case TaskType.Cooking:
                return CalculateExpFactor(System.Math.Max(s.Cooking.Level, secondary), requirement, nextRequirement);
        }

        return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateExpFactor(double level, double requirement, double nextRequirement)
    {
        if (nextRequirement <= requirement) return 1d;
        
        // if the current level is more than 20% of the required next place. You don't receive any more exp.
        var upperBounds = nextRequirement * 1.2;
        if (level >= upperBounds) return 0;
        if (level <= 30) return 1d; // early levels are not going to change the factor.

        var reqRatio = requirement / nextRequirement;
        var midPoint = (nextRequirement - requirement) * reqRatio;
        if (level <= (requirement + midPoint))
            return 1d; // at the midPoint tip, we will return the max as well. 
                       // Example, if current level requirement is 100, next is 200.
                       // Then you will gain full exp all the way to level 150.

        // Imagine this:
        // Exp should not falter before player has reached %10 above the requirement for the next one.
        // example: Player is training at home at level 1, on first enemies. This one
        var delta = nextRequirement * .10;
        var value = System.Math.Min((requirement + delta) / (level - midPoint), 1d);

        if (level > nextRequirement)
        {
            var reduceFactor = 1d - (level / upperBounds);
            return GameMath.Lerp(value, 0, reduceFactor);
        }

        return value;
    }

    private ChunkTask GetChunkTask(TaskType type) // ArenaController arena,
    {
        switch (type)
        {
            case TaskType.Fighting:
                return new FightingTask(() => enemies);
            case TaskType.Mining:
                return new MiningTask(() => miningSpots);
            case TaskType.Fishing:
                return new FishingTask(() => fishingSpots);
            case TaskType.Woodcutting:
                return new WoodcuttingTask(() => trees);
            case TaskType.Crafting:
                return new CraftingTask(() =>
                    craftingStations.Where(x => x.StationType == CraftingStationType.Crafting).ToList());
            case TaskType.Farming:
                return new FarmingTask(() => farmingPatches);
            case TaskType.Cooking:
                return new CookingTask(() =>
                    craftingStations.Where(x => x.StationType == CraftingStationType.Cooking).ToList());
            default: return null;
        }
    }

    public IChunk CreateSecondary()
    {
        return new SecondaryChunk(this);
    }

    private class SecondaryChunk : IChunk
    {
        private readonly Chunk origin;
        private readonly ChunkTask task;

        public SecondaryChunk(Chunk origin)
        {
            this.origin = origin;
            task = this.origin.secondaryTask;
        }

        public object GetTaskTarget(PlayerController player) => task?.GetTarget(player);
        public bool IsTaskCompleted(PlayerController player, object target) => task == null || task.IsCompleted(player, target);
        public bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason)
        {
            reason = TaskExecutionStatus.NotReady;
            if (task == null) return false;

            if (!task.CanExecute(player, target, out reason))
            {
                if (target != null && reason == TaskExecutionStatus.OutOfRange)
                {
                    // Verify that target is part of chunk.                    
                    if (!task.TargetExists(target))
                    {
                        reason = TaskExecutionStatus.InvalidTarget;
                    }
                }
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TargetAcquired(PlayerController player, object target)
        {
            if (task != null)
            {
                task.TargetAcquired(player, target);
            }
        }

        public bool ExecuteTask(PlayerController player, object target) => task != null && task.Execute(player, target);

        public Vector3 CenterPointWorld => origin.CenterPointWorld;
        public TaskType ChunkType => origin.SecondaryType;

        public IslandController Island => origin.Island;

        public Vector3 GetPlayerSpawnPoint() => origin.GetPlayerSpawnPoint();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRequiredCombatLevel()
        {
            return origin.RequiredCombatLevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRequiredSkillLevel()
        {
            return origin.RequiredSkilllevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateExpFactor(PlayerController playerController)
        {
            return origin.CalculateExpFactor(origin.SecondaryType, playerController);
        }
    }
}

public interface IChunk
{
    bool IsTaskCompleted(PlayerController player, object target);
    bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason);
    void TargetAcquired(PlayerController player, object target);
    bool ExecuteTask(PlayerController player, object target);

    int GetRequiredCombatLevel();
    int GetRequiredSkillLevel();
    double CalculateExpFactor(PlayerController playerController);
    object GetTaskTarget(PlayerController player);
    Vector3 CenterPointWorld { get; }
    TaskType ChunkType { get; }
    Vector3 GetPlayerSpawnPoint();
    IslandController Island { get; }
}

public enum TaskExecutionStatus
{
    NotReady,
    OutOfRange,
    Ready,
    InsufficientResources,
    InvalidTarget,
}