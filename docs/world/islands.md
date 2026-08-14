# World: Islands & Chunks

**Location:** `Assets/Scripts/Game/Controllers/IslandController.cs`, `Assets/Scripts/World/`

## Overview

The Ravenfall world consists of multiple **Islands**, each containing **Chunks** (training areas). Players travel between islands using the **Ferry**.

---

## Islands

**Controller:** `IslandController`  
**Manager:** `IslandManager`

### IslandController

```csharp
public class IslandController : MonoBehaviour
{
    public string Identifier;              // Island name/ID
    public int LevelRequirement;           // Minimum level to access
    
    public Transform SpawnPositionTransform; // Player spawn point
    public Transform CameraPanTarget;      // Camera focus point
    public DockController DockingArea;     // Ferry dock
    
    public bool AllowRaidWar;              // Stream raid battles allowed
    public Transform RaiderSpawningPoint;
    public Transform StreamerSpawningPoint;
    
    public bool Sailable { get; }          // Has a dock
}
```

### Island Properties

```csharp
public Vector3 SpawnPosition { get; }           // Where players spawn
public IReadOnlyList<Chunk> TrainingAreas { get; } // Chunks on this island
public IReadOnlyList<PlayerController> GetPlayers(); // Players on island
public IslandStatistics Statistics { get; }     // Usage tracking
```

### Island Methods

```csharp
public bool InsideIsland(Vector3 position);     // Check if point is on island
public void AddPlayer(PlayerController player);
public void RemovePlayer(PlayerController player);
public int GetPlayerCount();
```

### Player-Island Relationship

```csharp
// Player tracks their current island
player.Island = islandController;

// Island tracks players
island.AddPlayer(player);
island.RemovePlayer(player);
```

---

## Chunks (Training Areas)

**Class:** `Chunk`  
**Manager:** `ChunkManager`

### What is a Chunk?

A Chunk is a designated training area for a specific skill. Each chunk contains the resources or enemies needed for that activity.

### Chunk Properties

```csharp
public class Chunk : MonoBehaviour
{
    public TaskType Type;                  // Mining, Fishing, Fighting, etc.
    public Vector3 CenterPoint;            // Center of the chunk
    public bool IsStarterArea;             // New player spawn point
    
    public int RequiredCombatLevel = 1;    // Minimum combat level
    public int RequiredSkilllevel = 1;     // Minimum skill level
    
    public IslandController Island { get; } // Parent island
}
```

### Task Types

```csharp
public enum TaskType
{
    None,
    Fighting,
    Woodcutting,
    Mining,
    Fishing,
    Farming,
    Cooking,
    Crafting,
    Gathering,
    Alchemy
    // Arena, Raid, Dungeon handled separately
}
```

### Chunk Resources

Each chunk contains relevant objects:

```csharp
private EnemyController[] enemies;         // Fighting chunks
private TreeController[] trees;            // Woodcutting chunks
private RockController[] miningSpots;      // Mining chunks
private FishingController[] fishingSpots;  // Fishing chunks
private FarmController[] farmingPatches;   // Farming chunks
private CraftingStation[] craftingStations; // Crafting chunks
private GatherController[] gatheringSpots; // Gathering chunks
```

### ChunkTask System

Each chunk uses a `ChunkTask` to manage its activity:

```csharp
public abstract class ChunkTask
{
    // Find the next target (tree, rock, enemy, etc.)
    public abstract object GetTarget(PlayerController player);
    
    // Check if current target is complete
    public abstract bool IsCompleted(PlayerController player, object target);
    
    // Check if player can perform action
    public abstract bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason);
    
    // Perform the action (chop, mine, attack)
    public abstract bool Execute(PlayerController player, object target);
}
```

### Task Implementations

| Task File | Description |
|-----------|-------------|
| `FightingTask.cs` | Combat with enemies |
| `WoodcuttingTask.cs` | Chopping trees |
| `MiningTask.cs` | Mining rocks |
| `FishingTask.cs` | Catching fish |
| `FarmingTask.cs` | Growing crops |
| `CraftingTask.cs` | Creating items |
| `CookingTask.cs` | Preparing food |
| `GatheringTask.cs` | Collecting herbs |
| `AlchemyTask.cs` | Brewing potions |

---

## ChunkManager

Coordinates all chunks in the game:

```csharp
public class ChunkManager : MonoBehaviour
{
    // Find chunks by type
    public IReadOnlyList<Chunk> GetChunksOfType(TaskType type);
    public IReadOnlyList<Chunk> GetChunksOfType(IslandController island, TaskType type);
    
    // Find best chunk for player
    public Chunk GetChunkOfType(PlayerController player, TaskType type);
    
    // Get starter area
    public Chunk GetStarterChunk();
}
```

### Level-Appropriate Training

```csharp
public Chunk GetChunkOfType(PlayerController player, TaskType type)
{
    // Filter chunks player can access
    var trainableChunks = chunks.Where(c =>
        c.Island == player.Island &&
        c.Type == type &&
        c.RequiredCombatLevel <= player.Stats.CombatLevel &&
        c.RequiredSkilllevel <= player.GetSkillLevel(type)
    );
    
    // Return highest level chunk they qualify for
    return trainableChunks.OrderByDescending(c => c.RequiredLevel).First();
}
```

---

## Experience Factors

Chunks calculate experience based on level appropriateness:

```csharp
public double CalculateExpFactor(TaskType taskType, PlayerController player, out ExpGainState state)
{
    // Full exp in appropriate areas
    // Reduced exp when overleveled
    // Zero exp when underleveled
}
```

---

## Scene Hierarchy

In the Unity scene, the world is organized:

```
============ WORLD
├── Islands
│   ├── Home Island
│   │   ├── Mining Chunk Lv. 1
│   │   ├── Fishing Chunk Lv. 1
│   │   ├── Fighting Chunk Lv. 1
│   │   └── ...
│   ├── Away Island
│   ├── Ironhill
│   ├── Kyo
│   └── ...
├── Dungeons
├── Terrains
├── Environment
└── Tavern
```

---

## Island Travel

See [Ferry System](./ferry.md) for inter-island travel.

### Quick Reference

```csharp
// Get player's current island
IslandController island = player.Island;

// Find island by position
IslandController island = islandManager.FindIsland(position);

// Check if player can travel to island
bool canTravel = player.Stats.CombatLevel >= island.LevelRequirement;
```

---

## Island Level Ranges

The `IslandManager` tracks level ranges for each island:

```csharp
public static Dictionary<Island, int> IslandLevelRangeMin;
public static Dictionary<Island, int> IslandLevelRangeMax;
public static Dictionary<Island, int> IslandMaxEffect;
```

These determine optimal training ranges and experience modifiers.
