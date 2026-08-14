# Chunks & Tasks System

**Location:** `Assets/Scripts/World/Chunk.cs`, `Assets/Scripts/Tasks/`

## Overview

Chunks are designated training areas in the world. Each chunk has a specific task type (Mining, Fishing, Fighting, etc.) and contains the resources or enemies needed for that activity.

---

## Chunk Class

```csharp
public class Chunk : MonoBehaviour
{
    public TaskType Type;                  // Activity type
    public Vector3 CenterPoint;            // Center position
    public bool IsStarterArea;             // New player spawn
    
    public int RequiredCombatLevel = 1;    // Minimum combat level
    public int RequiredSkilllevel = 1;     // Minimum skill level
    
    public IslandController Island { get; } // Parent island
    public GameManager Game { get; }
}
```

### Task Types

```csharp
public enum TaskType
{
    None,
    Fighting,      // Combat with enemies
    Woodcutting,   // Chopping trees
    Mining,        // Mining rocks
    Fishing,       // Catching fish
    Farming,       // Growing crops
    Cooking,       // Preparing food
    Crafting,      // Creating items
    Gathering,     // Collecting herbs
    Alchemy        // Brewing potions
}
```

---

## Chunk Resources

Each chunk initializes its relevant objects on Start:

```csharp
void Start()
{
    enemies = GetComponentsInChildren<EnemyController>();
    trees = GetComponentsInChildren<TreeController>();
    fishingSpots = GetComponentsInChildren<FishingController>();
    miningSpots = GetComponentsInChildren<RockController>();
    craftingStations = GetComponentsInChildren<CraftingStation>();
    farmingPatches = GetComponentsInChildren<FarmController>();
    gatheringSpots = GetComponentsInChildren<GatherController>();
    
    // Create task handler for this chunk type
    task = GetChunkTask(Type);
    
    // Rename for clarity in hierarchy
    gameObject.name = $"{Type} Lv. {RequiredSkilllevel}";
}
```

---

## ChunkTask System

**Location:** `Assets/Scripts/Tasks/ChunkTask.cs`

Abstract base class for all task implementations:

```csharp
public abstract class ChunkTask
{
    // Find target for player (tree, rock, enemy, etc.)
    public abstract object GetTarget(PlayerController player);
    
    // Check if target action is complete
    public abstract bool IsCompleted(PlayerController player, object target);
    
    // Check if player can perform action
    public abstract bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason);
    
    // Perform the action
    public abstract bool Execute(PlayerController player, object target);
    
    // Called when target is selected
    public virtual void TargetAcquired(PlayerController player, object target) { }
}
```

### Task Execution Status

```csharp
public enum TaskExecutionStatus
{
    NotReady,          // Cooldown active
    Ready,             // Can execute
    OutOfRange,        // Too far from target
    InvalidTarget,     // Target doesn't exist
    InventoryFull,     // No space
    LevelTooLow        // Requirements not met
}
```

---

## Task Implementations

### FightingTask

```csharp
public class FightingTask : ChunkTask
{
    public override object GetTarget(PlayerController player)
    {
        // Find nearest alive enemy
        return enemies.Where(e => !e.Stats.IsDead)
                      .OrderBy(e => Distance(player, e))
                      .FirstOrDefault();
    }
    
    public override bool Execute(PlayerController player, object target)
    {
        var enemy = target as EnemyController;
        player.Attack(enemy);
        return true;
    }
}
```

### WoodcuttingTask

```csharp
public class WoodcuttingTask : ChunkTask
{
    public override object GetTarget(PlayerController player)
    {
        // Find tree that's not depleted
        return trees.Where(t => !t.IsDepleted)
                    .OrderBy(t => Distance(player, t))
                    .FirstOrDefault();
    }
    
    public override bool Execute(PlayerController player, object target)
    {
        var tree = target as TreeController;
        tree.Chop(player);
        player.AddExp(Skill.Woodcutting);
        return true;
    }
}
```

### MiningTask

```csharp
public class MiningTask : ChunkTask
{
    public override object GetTarget(PlayerController player)
    {
        return miningSpots.Where(r => !r.IsDepleted)
                          .OrderBy(r => Distance(player, r))
                          .FirstOrDefault();
    }
    
    public override bool Execute(PlayerController player, object target)
    {
        var rock = target as RockController;
        rock.Mine(player);
        player.AddExp(Skill.Mining);
        return true;
    }
}
```

### FishingTask

```csharp
public class FishingTask : ChunkTask
{
    public override object GetTarget(PlayerController player)
    {
        return fishingSpots.OrderBy(f => Distance(player, f))
                           .FirstOrDefault();
    }
    
    public override bool Execute(PlayerController player, object target)
    {
        var spot = target as FishingController;
        spot.Fish(player);
        player.AddExp(Skill.Fishing);
        return true;
    }
}
```

### CraftingTask / CookingTask / AlchemyTask

These use **StationTask** base class:

```csharp
public abstract class StationTask : ChunkTask
{
    public override object GetTarget(PlayerController player)
    {
        // Find available crafting station
        return stations.OrderBy(s => Distance(player, s))
                       .FirstOrDefault();
    }
}
```

---

## Task Execution Flow

```
1. Player assigned task (via !train or auto)
        ↓
2. ChunkManager.GetChunkOfType(player, taskType)
        ↓
3. Chunk.GetTaskTarget(player)
        ↓
4. Player moves to target
        ↓
5. Chunk.CanExecuteTask(player, target)
        ↓
6. Chunk.ExecuteTask(player, target)
        ↓
7. Player gains experience
        ↓
8. Chunk.IsTaskCompleted(player, target)
        ↓
9. If complete, get new target (back to step 3)
```

---

## Chunk Methods

```csharp
// Get next target for player
public virtual object GetTaskTarget(PlayerController player)
{
    return task?.GetTarget(player);
}

// Check if current action is done
public virtual bool IsTaskCompleted(PlayerController player, object target)
{
    return task == null || task.IsCompleted(player, target);
}

// Check if player can perform action
public virtual bool CanExecuteTask(PlayerController player, object target, out TaskExecutionStatus reason)
{
    reason = TaskExecutionStatus.NotReady;
    return task != null && task.CanExecute(player, target, out reason);
}

// Perform the action
public virtual bool ExecuteTask(PlayerController player, object target)
{
    return task != null && task.Execute(player, target);
}
```

---

## Experience Calculation

Chunks calculate experience factors based on level appropriateness:

```csharp
public virtual double CalculateExpFactor(TaskType taskType, PlayerController player, out ExpGainState state)
{
    var skill = player.GetSkill(taskType);
    var minLv = IslandManager.IslandLevelRangeMin[this.Island.Island];
    var maxLv = IslandManager.IslandLevelRangeMax[this.Island.Island];
    
    // Too low level
    if (skill.Level < minLv)
    {
        state = ExpGainState.LevelTooLow;
        return 0;
    }
    
    // Too high level (overleveled)
    if (skill.Level > maxLv)
    {
        state = ExpGainState.LevelTooHigh;
        return 0; // or reduced
    }
    
    state = ExpGainState.FullGain;
    return GameMath.Exp.MaxExpFactorFromIsland;
}
```

### Experience States

```csharp
public enum ExpGainState
{
    FullGain,           // Full experience
    ReducedGain,        // Partial (overleveled)
    LevelTooLow,        // Can't train here yet
    LevelTooHigh,       // Area too easy
    TargetLevelReached, // Auto-train target hit
    PlayerRemoved,      // Player left game
    NotAValidSkill      // Invalid skill
}
```

---

## ChunkManager

Coordinates all chunks:

```csharp
public class ChunkManager : MonoBehaviour
{
    public static bool StrictLevelRequirements = true;
    
    private Chunk[] chunks;
    
    // Get chunks by type
    public IReadOnlyList<Chunk> GetChunksOfType(TaskType type);
    public IReadOnlyList<Chunk> GetChunksOfType(IslandController island, TaskType type);
    
    // Get best chunk for player's level
    public Chunk GetChunkOfType(PlayerController player, TaskType type);
    
    // Get new player spawn
    public Chunk GetStarterChunk();
}
```

### Smart Chunk Selection

```csharp
public Chunk GetChunkOfType(PlayerController player, TaskType type)
{
    var trainableChunks = chunks.Where(c =>
        c.Island == player.Island &&
        c.Type == type &&
        c.RequiredCombatLevel <= player.Stats.CombatLevel &&
        c.RequiredSkilllevel <= player.GetSkillLevel(type)
    );
    
    // For combat, consider individual skill levels
    if (type == TaskType.Fighting)
    {
        return FindBestCombatChunk(player, trainableChunks);
    }
    
    // For skilling, use highest qualified chunk
    return trainableChunks.OrderByDescending(c => c.RequiredSkilllevel).First();
}
```
