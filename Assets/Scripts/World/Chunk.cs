using System.Linq;
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

    public Vector3 CenterPointWorld => CenterPoint + transform.position;
    public TaskType ChunkType => Type;

    public IslandController Island { get; private set; }
    public int GetRequiredCombatLevel() => RequiredCombatLevel;
    public int GetRequiredSkillLevel() => RequiredSkilllevel;

    // Start is called before the first frame update
    void Start()
    {
        var arena = GetComponentInChildren<ArenaController>();

        Island = GetComponentInParent<IslandController>();

        task = GetChunkTask(arena, Type);
        if (SecondaryType != TaskType.None)
        {
            secondaryTask = GetChunkTask(arena, SecondaryType);
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
    public Vector3 GetPlayerSpawnPoint()
    {
        if (!IsStarterArea || !spawnPoint) return Vector3.zero;
        return spawnPoint.transform.position;
    }

    public object GetTaskTarget(PlayerController player)
    {
        return task?.GetTarget(player);
    }

    public bool IsTaskCompleted(PlayerController player, object target)
    {
        return task == null || task.IsCompleted(player, target);
    }

    public bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason)
    {
        reason = TaskExecutionStatus.NotReady;
        return task != null && task.CanExecute(player, target, out reason);
    }

    public void TargetAcquired(PlayerController player, object target)
    {
        task.TargetAcquired(player, target);
    }

    public bool ExecuteTask(PlayerController player, object target)
    {
        return task != null && task.Execute(player, target);
    }

    private ChunkTask GetChunkTask(ArenaController arena, TaskType type)
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

        public int GetRequiredCombatLevel()
        {
            return origin.RequiredCombatLevel;
        }

        public int GetRequiredSkillLevel()
        {
            return origin.RequiredSkilllevel;
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