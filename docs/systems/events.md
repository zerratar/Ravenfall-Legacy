# Events: Raids & Dungeons

**Location:** `Assets/Scripts/Game/Managers/RaidManager.cs`, `Assets/Scripts/Game/Controllers/DungeonController.cs`

## Overview

Raids and Dungeons are special group events where players collaborate to defeat bosses for rewards.

---

## Raids

**Manager:** `RaidManager`  
**Controller:** `RaidBossController`

### Raid Lifecycle

1. **Spawn Timer** - Random time between raids (10-50 minutes)
2. **Boss Spawns** - Raid boss appears in the world
3. **Join Phase** - Players can join via `!raid join`
4. **Combat Phase** - Players fight the boss
5. **Completion** - Boss dies or timeout
6. **Rewards** - Items distributed to participants

### RaidManager Properties

```csharp
public bool Started { get; }               // Raid in progress
public float SecondsLeft { get; }          // Time until timeout
public float SecondsUntilNextRaid { get; } // Time until next spawn
public int Counter { get; }                // Raid index (for tracking)
public RaidBossController Boss { get; }    // Current boss
public IReadOnlyList<PlayerController> Raiders { get; } // Participants
public PlayerController Initiator { get; } // Who started the raid
public string RequiredCode { get; }        // Optional join code
```

### Joining a Raid

```csharp
public enum RaidJoinResult
{
    CanJoin,
    NoActiveRaid,
    AlreadyJoined,
    MinHealthReached,  // Boss HP too low
    WrongCode          // Incorrect join code
}

public RaidJoinResult CanJoin(PlayerController player);
public void Join(PlayerController player);
public void Leave(PlayerController player, bool reward = false, bool timeout = false);
```

### Raid Difficulty System

**Location:** `Assets/Scripts/Game/Managers/RaidDifficultySystem.cs`

Tracks player combat power to scale future raids:

```csharp
public void Track(int raidIndex, PlayerController player)
{
    // Record combat level and equipment stats
    // Used to scale next raid boss
}
```

### RaidHandler (Per-Player)

Each player has a `RaidHandler` component:

```csharp
public bool InRaid { get; }
public IslandController PreviousIsland { get; }  // Return location
public Vector3 PreviousPosition { get; }

public void OnEnter();   // Called when joining raid
public void OnLeave(bool reward, bool timeout);
```

---

## Dungeons

**Manager:** `DungeonManager`  
**Controller:** `DungeonController`

### Dungeon Structure

Dungeons consist of multiple rooms generated procedurally:

```csharp
public enum DungeonRoomType
{
    Start,      // Entrance room
    Combat,     // Enemies to clear
    Boss,       // Final boss room
    Loot,       // Treasure room
    Corridor    // Connecting passage
}
```

### DungeonController

```csharp
public class DungeonController : MonoBehaviour
{
    public Transform StartingPoint { get; }     // Player spawn
    public Transform BossSpawnPoint { get; }    // Boss location
    public DungeonRoomController Room { get; }  // Current room
    public DungeonRoomController[] Rooms { get; } // All rooms
    public DungeonRoomController BossRoom { get; }
}
```

### Dungeon Types

```csharp
public class DungeonType
{
    public string Name;                    // Display name
    public int Level;                      // Minimum level
    public DungeonTier Tier;               // Rarity tier
    public DungeonDifficulity Difficulity; // Scaling mode
    public float SpawnRate;                // Enemy density
    public float MobsDifficultyScale;      // Enemy strength
    public float BossCombatScale;          // Boss damage
    public float BossHealthScale;          // Boss HP
}
```

### Dungeon Tiers

```csharp
public enum DungeonTier
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
```

### Dungeon Difficulty

```csharp
public enum DungeonDifficulity
{
    Easy,
    Normal,
    Hard,
    Dynamic  // Scales with player levels
}
```

### DungeonHandler (Per-Player)

```csharp
public class DungeonHandler
{
    public bool InDungeon { get; }
    public bool Joined { get; }                 // Joined current dungeon
    public int AutoJoinCounter { get; }         // Auto-join uses
    public long AutoJoinCount { get; }
    
    public IslandController PreviousIsland { get; }
    public Vector3 PreviousPosition { get; }
    
    public void OnEnter();  // Teleport to dungeon
    public void Update();   // Combat logic in dungeon
}
```

### Dungeon Combat Flow

```csharp
public void Update()
{
    if (InDungeon && dungeon.Started)
    {
        if (player.TrainingHealing)
        {
            // Find lowest HP ally
            healTarget = FindLowestHealthAlly();
            HealTarget();
        }
        else
        {
            // Find and attack enemies
            enemyTarget = dungeon.GetNextEnemyTarget(player);
            AttackTarget();
        }
    }
}
```

---

## Event Management

### IEvent Interface

Both raids and dungeons implement `IEvent`:

```csharp
public interface IEvent
{
    string EventName { get; }
    bool IsEventActive { get; }
}
```

### GameEventManager

Prevents overlapping events:

```csharp
public bool TryStart(IEvent evt, bool hasInitiator);
public void End(IEvent evt);
```

Only one major event can run at a time (Raid OR Dungeon).

---

## Player Handlers Summary

| Handler | Purpose |
|---------|---------|
| `RaidHandler` | Raid participation state |
| `DungeonHandler` | Dungeon participation and combat |
| `ArenaHandler` | PvP arena battles |
| `DuelHandler` | 1v1 player duels |

---

## Rewards

### Raid Rewards
- Distributed based on participation
- Higher damage = better chance
- Special items possible

### Dungeon Rewards
- Room-by-room loot
- Boss drops guaranteed items
- Scales with dungeon tier

---

## Auto-Join Feature

Players can configure auto-join:

```csharp
// Session settings
public int AutoJoinDungeonCost { get; } = 5000;  // Coin cost
public int AutoJoinRaidCost { get; } = 3000;

// Per-player tracking
player.dungeonHandler.AutoJoinCounter;
player.dungeonHandler.AutoJoinCount;
```

---

## Commands

| Command | Description |
|---------|-------------|
| `!raid` | View raid status |
| `!raid join` | Join active raid |
| `!dungeon` | View dungeon status |
| `!dungeon join` | Join active dungeon |
| `!raid auto` | Toggle auto-join |
| `!dungeon auto` | Toggle auto-join |
