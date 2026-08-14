# GameManager

**Location:** `Assets/Scripts/Game/Managers/GameManager.cs`

## Overview

`GameManager` is the central orchestrator of the entire game. It's a singleton that manages all subsystems, coordinates with external services, and processes game events.

## Key Responsibilities

1. **Session Management** - Handles game sessions with the RavenNest server
2. **Manager Coordination** - References and coordinates all other managers
3. **Event Processing** - Queues and dispatches game events
4. **State Persistence** - Saves/loads game state and player data
5. **Stream Integration** - Manages stream labels, overlays, and Twitch features

## Key Properties

### Managers
```csharp
public PlayerManager Players { get; }        // Player management
public RaidManager Raid { get; }             // Raid events
public DungeonManager Dungeons { get; }      // Dungeon instances
public ChunkManager Chunks { get; }          // Training areas
public IslandManager Islands { get; }        // World islands
public ItemManager Items { get; }            // Game items
public CraftingManager Crafting { get; }     // Crafting system
public ArenaController Arena { get; }        // PvP arena
public FerryController Ferry { get; }        // Ferry travel
public VillageManager Village { get; }       // Village/town management
public OnsenManager Onsen { get; }           // Rest area
```

### External Services
```csharp
public RavenNestClient RavenNest { get; }    // Server connection
public RavenBotConnection RavenBot { get; }  // Chat bot connection
public TwitchEventManager Twitch { get; }    // Twitch events (subs, bits)
```

### Game State
```csharp
public bool IsLoaded { get; }                // Game finished loading
public bool IsSaving { get; }                // Currently saving data
public SessionSettings SessionSettings { get; } // Session configuration
public bool PotatoMode { get; set; }         // Low graphics mode
```

## Event System

GameManager processes events through registered handlers:

```csharp
// Event registration (in Start())
RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
RegisterGameEventHandler<RaidJoinEventHandler>(GameEventType.PlayerJoinRaid);
RegisterGameEventHandler<ItemAddEventHandler>(GameEventType.ItemAdd);
// ... many more
```

### Event Types
- **Player Events**: Add, Remove, Task, Travel, Appearance, etc.
- **Combat Events**: JoinRaid, JoinDungeon, JoinArena
- **Item Events**: Add, Remove, Equip, Unequip, Buy, Sell
- **Session Events**: ServerMessage, VillageInfo, ExpMultiplier

## Singleton Access

```csharp
// Access from anywhere
GameManager.Instance.Players.GetPlayerCount();
GameManager.Instance.Raid.Join(player);
```

## Stream Labels

GameManager provides stream label functionality for OBS/streaming software:

```csharp
StreamLabels.RegisterText("uptime", () => Time.realtimeSinceStartup.ToString());
StreamLabels.RegisterText("online-player-count", () => playerManager.GetPlayerCount().ToString());
StreamLabels.Register("exp-multiplier", () => GetExpMultiplierStats());
```

## Important Methods

### Player Management
```csharp
// Add items to players from rewards
public PlayerItemDropText AddItems(EventItemReward[] rewards, int dungeonIndex = -1, int raidIndex = -1)
```

### Session Control
```csharp
// Session settings include:
// - ExpMultiplierLimit
// - AutoRestCost
// - AutoJoinDungeonCost
// - AutoJoinRaidCost
```

### Graphics/Performance
```csharp
public bool PotatoMode { get; set; }         // Enable low-quality mode
public bool AutoPotatoMode { get; set; }     // Auto-enable based on player count
```

## Dependencies

GameManager depends on (via serialized fields or runtime lookup):
- `GameSettings` - Configuration
- `GameCamera` - Camera control
- `RavenBot` - Chat bot
- `LoginHandler` - Authentication
- `DayNightCycle` - Time of day
- `Volume` - Post-processing effects
- Various UI components

## Lifecycle

1. **Awake()**: Initialize singleton, setup systems
2. **Start()**: Register event handlers, load settings, start game systems
3. **Update()**: Process event queue, update timers, save state periodically
4. **OnApplicationQuit()**: Save state, cleanup connections
