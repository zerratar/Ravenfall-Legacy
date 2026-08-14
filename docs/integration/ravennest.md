# RavenNest API Integration

**Location:** `Assets/Scripts/` (via RavenNest.SDK namespace)

## Overview

RavenNest is the backend server that handles player accounts, data persistence, and game sessions. The game client communicates with RavenNest for authentication, player data, and state synchronization.

---

## RavenNestClient

The main client for server communication:

```csharp
public class RavenNestClient
{
    public bool Authenticated { get; }     // Successfully logged in
    public bool SessionStarted { get; }    // Game session active
    
    // Player operations
    public Task<PlayerInfo> PlayerJoinAsync(PlayerJoinData data);
    public Task PlayerLeaveAsync(PlayerController player);
    
    // Data sync
    public Task SavePlayerStateAsync(PlayerController player);
    public Task SaveExperienceAsync(PlayerController player);
}
```

---

## Authentication Flow

```
1. Game starts
        ↓
2. LoginHandler shows login UI
        ↓
3. User enters credentials or uses saved token
        ↓
4. RavenNestClient.AuthenticateAsync()
        ↓
5. On success: Start game session
        ↓
6. Load items, session settings
        ↓
7. Accept player joins
```

---

## Player Join Process

```csharp
public async Task<PlayerController> JoinAsync(User user, bool isBot)
{
    // 1. Check if game is ready
    if (!Game.Items.Loaded) return null;
    
    // 2. Check if already in game
    if (Contains(user.PlatformId)) return null;
    
    // 3. Request join from server
    var playerInfo = await Game.RavenNest.PlayerJoinAsync(
        new PlayerJoinData
        {
            PlatformId = user.PlatformId,
            Platform = user.Platform,
            UserName = user.Username,
            UserId = user.Id,
            Moderator = user.IsModerator,
            Subscriber = user.IsSubscriber,
            Vip = user.IsVip
        });
    
    // 4. Create PlayerController with returned data
    return CreatePlayer(playerInfo);
}
```

---

## Data Models

### Player Definition

```csharp
public class RavenNest.Models.Player
{
    public Guid Id { get; }                // Character ID
    public Guid UserId { get; }            // Account ID
    public string Name { get; }
    public Skills Skills { get; }          // All skill data
    public Resources Resources { get; }    // Coins, materials
    public InventoryItem[] Inventory { get; }
    public Appearance Appearance { get; }
    public CharacterState State { get; }
}
```

### Skills Data

```csharp
public class RavenNest.Models.Skills
{
    public int AttackLevel { get; }
    public double Attack { get; }          // Experience
    public int DefenseLevel { get; }
    public double Defense { get; }
    // ... all 17 skills
}
```

### Resources

```csharp
public class RavenNest.Models.Resources
{
    public long Coins { get; }
    public long Wood { get; }
    public long Ore { get; }
    public long Fish { get; }
    public long Wheat { get; }
}
```

---

## State Synchronization

### Experience Updates

Experience is periodically saved to the server:

```csharp
// GameManager Update loop
experienceSaveTime -= Time.deltaTime;
if (experienceSaveTime <= 0)
{
    experienceSaveTime = experienceSaveInterval; // 2-3 seconds
    SavePlayerExperience();
}
```

### State Updates

Character state (position, task, etc.) is also synced:

```csharp
stateSaveTime -= Time.deltaTime;
if (stateSaveTime <= 0)
{
    stateSaveTime = stateSaveInterval;
    SavePlayerStates();
}
```

### Delta Client

**Location:** `Assets/Scripts/DeltaClientBehaviour.cs`

Uses a delta-sync approach for efficient updates:

```csharp
public class DeltaClientBehaviour : MonoBehaviour
{
    // Only sends changed data
    // Uses dirty flags for efficiency
}
```

---

## Session Settings

Retrieved from server at session start:

```csharp
public class SessionSettings
{
    public int ExpMultiplierLimit { get; }
    public int AutoRestCost { get; }
    public int AutoJoinDungeonCost { get; }
    public int AutoJoinRaidCost { get; }
    public bool IsAdministrator { get; }
}
```

---

## Game Events

The server can push events to the client:

```csharp
public enum GameEventType
{
    // Player events
    PlayerAdd,
    PlayerRemove,
    PlayerExpUpdate,
    PlayerTask,
    PlayerTravel,
    
    // Combat events
    PlayerJoinRaid,
    PlayerJoinDungeon,
    PlayerJoinArena,
    
    // Item events
    ItemAdd,
    ItemRemove,
    ItemEquip,
    ItemUnEquip,
    
    // Session events
    ServerMessage,
    VillageInfo,
    ExpMultiplier,
    SessionSettingsChanged
}
```

### Event Handlers

Registered in GameManager:

```csharp
RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
RegisterGameEventHandler<RaidJoinEventHandler>(GameEventType.PlayerJoinRaid);
RegisterGameEventHandler<ItemAddEventHandler>(GameEventType.ItemAdd);
```

---

## Item System

Items are loaded from the server:

```csharp
public class ItemManager : MonoBehaviour
{
    public bool Loaded { get; }
    
    public Item Get(Guid itemId);
    public Item GetByName(string name);
    public IEnumerable<Item> GetAll();
}
```

### Item Definition

```csharp
public class RavenNest.Models.Item
{
    public Guid Id { get; }
    public string Name { get; }
    public ItemCategory Category { get; }
    public ItemType Type { get; }
    
    // Stats
    public int WeaponPower { get; }
    public int ArmorPower { get; }
    // ... other stats
    
    // Requirements
    public int RequiredAttackLevel { get; }
    public int RequiredDefenseLevel { get; }
    // ... other requirements
    
    public bool Soulbound { get; }
}
```

---

## Error Handling

```csharp
try
{
    var result = await RavenNest.PlayerJoinAsync(data);
    if (!result.Success)
    {
        // Handle error message
        ShowError(result.ErrorMessage);
    }
}
catch (Exception ex)
{
    // Network error
    Debug.LogError($"Failed to join: {ex.Message}");
}
```

---

## Offline Mode

The game can cache state locally:

```csharp
public static class GameCache
{
    public static bool IsAwaitingGameRestore { get; set; }
    
    public static LoadStateResult LoadState();
    public static void SaveState();
}
```

This allows recovery if the connection is lost.

---

## Related Links

- **RavenNest Server**: https://github.com/zerratar/ravennest
- **API Documentation**: (internal)
