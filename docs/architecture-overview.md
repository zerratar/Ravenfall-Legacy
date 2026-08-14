# Architecture Overview

## High-Level Architecture

Ravenfall Legacy follows a **component-based architecture** typical of Unity games, with a central `GameManager` orchestrating various subsystems.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         GAME CLIENT                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌─────────────────────────────────────────────────────────────┐   │
│   │                      GameManager                             │   │
│   │  (Central orchestrator - manages all subsystems)             │   │
│   └─────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│   ┌──────────────────────────┼──────────────────────────────────┐   │
│   │                          │                                   │   │
│   ▼                          ▼                                   ▼   │
│ ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐│
│ │ Player   │  │  Raid    │  │ Dungeon  │  │  Island  │  │  Chunk   ││
│ │ Manager  │  │ Manager  │  │ Manager  │  │ Manager  │  │ Manager  ││
│ └────┬─────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘│
│      │                                                               │
│      ▼                                                               │
│ ┌──────────────────────────────────────────────────────────────┐    │
│ │                    PlayerController                           │    │
│ │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ │    │
│ │  │Movement │ │ Combat  │ │  Raid   │ │ Dungeon │ │  Ferry  │ │    │
│ │  │Handler  │ │ Handler │ │ Handler │ │ Handler │ │ Handler │ │    │
│ │  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘ │    │
│ └──────────────────────────────────────────────────────────────┘    │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                      EXTERNAL INTEGRATIONS                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │
│  │  RavenBot   │  │ RavenNest   │  │   Twitch    │                  │
│  │ (Chat Bot)  │  │  (Server)   │  │    API      │                  │
│  └─────────────┘  └─────────────┘  └─────────────┘                  │
└─────────────────────────────────────────────────────────────────────┘
```

## Core Components

### GameManager
The central hub that:
- Manages all other managers (PlayerManager, RaidManager, DungeonManager, etc.)
- Handles game events and state
- Coordinates with the RavenNest server
- Manages game sessions

### Player System
- **PlayerManager**: Creates, tracks, and removes players
- **PlayerController**: Main player component with all stats, inventory, and behavior
- **Handlers**: Specialized handlers for different activities (Raid, Dungeon, Ferry, etc.)

### World System
- **IslandManager**: Manages multiple islands in the game world
- **IslandController**: Individual island with spawn points, chunks, and docking areas
- **ChunkManager**: Manages training areas/zones
- **Chunk**: A training area with specific task type (Mining, Fishing, Fighting, etc.)

### Event System
- **RaidManager**: Coordinates raid boss events
- **DungeonManager**: Manages dungeon instances
- **TwitchEventManager**: Handles Twitch-specific events (subs, bits, etc.)

## Data Flow

### Player Join Flow
```
Twitch Chat → RavenBot → GameManager → PlayerManager → PlayerController
      ↓
RavenNest Server (authenticate & get player data)
      ↓
PlayerController initialized with stats, inventory, appearance
```

### Task Execution Flow
```
Player assigns task (e.g., "!train mining")
      ↓
ChunkManager finds appropriate Chunk for skill level
      ↓
ChunkTask.GetTarget() finds target (rock, tree, enemy)
      ↓
PlayerMovementController moves player to target
      ↓
ChunkTask.Execute() performs action
      ↓
Player gains experience → Stats updated → Synced to server
```

## Key Patterns

### IoC Container
Uses a simple IoC container (`IoCContainer`) for dependency resolution.

### Event Handlers
Game events are processed through registered handlers:
```csharp
RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
RegisterGameEventHandler<RaidJoinEventHandler>(GameEventType.PlayerJoinRaid);
```

### Handler Pattern
Each player has multiple handlers for different game systems:
- `raidHandler` - Raid participation
- `dungeonHandler` - Dungeon participation
- `ferryHandler` - Island travel
- `arenaHandler` - PvP arena
- `duelHandler` - 1v1 duels
- `teleportHandler` - Teleportation
- `effectHandler` - Visual effects

## Scene Structure

The main scene (`MainWorld`) is organized with root separators:
- `============ CAMERAS` - Camera rigs
- `============ LIGHTS` - Lighting setup
- `============ WORLD` - Islands, dungeons, environment
- `============ UI` - HUD and interface
- `============ GAME SETUP` - Core game objects (GameManager, etc.)
- `============ FERRY` - Ferry system
- `============ PLAYERS` - Active player instances
