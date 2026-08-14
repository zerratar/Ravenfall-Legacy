# Twitch Integration

**Location:** `Assets/Scripts/Game/RavenBot.cs`, `Assets/Scripts/Game/Managers/TwitchEventManager.cs`

## Overview

Ravenfall is deeply integrated with Twitch. Players join and control their characters through chat commands processed by the RavenBot.

---

## RavenBot

**Location:** `Assets/Scripts/Game/RavenBot.cs`

The RavenBot bridges Twitch chat and the game:

```csharp
public class RavenBot : IDisposable
{
    public bool UseRemoteBot { get; }
    public RavenBotConnection Connection { get; }
    public BotState State { get; set; }
    public bool HasJoinedChannel { get; set; }
}
```

### Command Registration

All commands are registered in the constructor:

```csharp
Connection.Register<PlayerJoin>("join");
Connection.Register<PlayerTask>("task");
Connection.Register<PlayerStats>("player_stats");
Connection.Register<RaidJoin>("raid_join");
Connection.Register<DungeonJoin>("dungeon_join");
// ... 100+ commands
```

---

## Command Categories

### Player Management
| Command | Handler | Description |
|---------|---------|-------------|
| `join` | `PlayerJoin` | Join the game |
| `leave` | `PlayerLeave` | Leave the game |
| `kick` | `KickPlayer` | Remove player (mod) |
| `unstuck` | `PlayerUnstuck` | Reset position |

### Training
| Command | Handler | Description |
|---------|---------|-------------|
| `task` | `PlayerTask` | Set training task |
| `train_info` | `TrainingInfo` | Get training status |
| `mine` | `Mine` | Start mining |
| `fish` | `Fish` | Start fishing |
| `chop` | `Chop` | Start woodcutting |
| `farm` | `Farm` | Start farming |
| `gather` | `Gather` | Start gathering |

### Combat
| Command | Handler | Description |
|---------|---------|-------------|
| `raid_join` | `RaidJoin` | Join raid |
| `raid_force` | `RaidForce` | Force start raid (mod) |
| `dungeon_join` | `DungeonJoin` | Join dungeon |
| `arena_join` | `ArenaJoin` | Join PvP arena |
| `duel` | `DuelPlayer` | Challenge to duel |

### Items & Equipment
| Command | Handler | Description |
|---------|---------|-------------|
| `equip` | `EquipItem` | Equip item |
| `unequip` | `UnequipItem` | Unequip item |
| `enchant` | `EnchantItem` | Enchant item |
| `craft` | `Craft` | Craft item |
| `cook` | `Cook` | Cook food |
| `brew` | `Brew` | Brew potion |

### Trading
| Command | Handler | Description |
|---------|---------|-------------|
| `buy_item` | `BuyItemFromMarket` | Buy from market |
| `sell_item` | `PutItemOnMarket` | Sell on market |
| `vendor_item` | `SellItemToVendor` | Sell to NPC |
| `gift_item` | `GiftItem` | Gift to player |

### Information
| Command | Handler | Description |
|---------|---------|-------------|
| `player_stats` | `PlayerStats` | View stats |
| `player_eq` | `PlayerEq` | View equipment |
| `player_resources` | `PlayerResources` | View resources |
| `highscore` | `Highscore` | View rankings |

### Clan
| Command | Handler | Description |
|---------|---------|-------------|
| `clan_info` | `ClanInfoHandler` | Clan information |
| `clan_join` | `ClanJoin` | Join clan |
| `clan_leave` | `ClanLeave` | Leave clan |
| `clan_invite` | `ClanInvite` | Invite player |

### Travel
| Command | Handler | Description |
|---------|---------|-------------|
| `ferry_info` | `FerryInfo` | Ferry status |
| `island_info` | `IslandInfo` | Island info |

---

## TwitchEventManager

**Location:** `Assets/Scripts/Game/Managers/TwitchEventManager.cs`

Handles Twitch-specific events:

```csharp
public class TwitchEventManager : MonoBehaviour
{
    public ExpBoostEvent CurrentBoost { get; }
    
    // Subscription events
    public void OnSubscription(TwitchSubscriber sub);
    
    // Bit/Cheer events  
    public void OnCheer(TwitchCheerer cheer);
}
```

### Experience Boost Events

Subscriptions and bits trigger experience multipliers:

```csharp
public class ExpBoostEvent
{
    public bool Active { get; }
    public float Multiplier { get; }
    public float TimeRemaining { get; }
    public string TriggerUser { get; }
}
```

---

## Player Identity

Players are identified by their Twitch credentials:

```csharp
public class User
{
    public Guid Id { get; }                // RavenNest user ID
    public string Username { get; }        // Twitch display name
    public string Platform { get; }        // "twitch"
    public string PlatformId { get; }      // Twitch user ID
    
    public bool IsModerator { get; }
    public bool IsBroadcaster { get; }
    public bool IsSubscriber { get; }
    public bool IsVip { get; }
}
```

### Player Lookup

```csharp
// Find player by Twitch username
PlayerController player = playerManager.GetPlayerByName("username");

// Find by platform ID
PlayerController player = playerManager.GetPlayerByPlatformId(twitchId, "twitch");
```

---

## Broadcaster Features

The streamer (broadcaster) has special abilities:

```csharp
player.IsBroadcaster = true;

// Broadcaster-only commands
Connection.Register<ReloadGame>("reload");
Connection.Register<RestartGame>("restart");
Connection.Register<UpdateGame>("update");
Connection.Register<SetExpMultiplier>("exp_multiplier");
Connection.Register<SetExpMultiplierLimit>("exp_multiplier_limit");
```

---

## Moderator Features

Channel moderators can help manage the game:

```csharp
player.IsModerator = true;

// Mod commands
Connection.Register<KickPlayer>("kick");
Connection.Register<RaidForce>("raid_force");
Connection.Register<DungeonForce>("dungeon_force");
Connection.Register<ArenaKick>("arena_kick");
```

---

## Chat Command Flow

```
Twitch Chat Message
        ↓
    RavenBot (external process)
        ↓
    RavenBotConnection (in-game)
        ↓
    Command Handler (e.g., PlayerJoin)
        ↓
    Game Action
        ↓
    Response → RavenBot → Twitch Chat
```

---

## Session Settings

Broadcasters can configure session behavior:

```csharp
public class SessionSettings
{
    public int ExpMultiplierLimit = 100;   // Max exp multiplier
    public int AutoRestCost = 500;         // Auto-rest coin cost
    public int AutoJoinDungeonCost = 5000; // Auto dungeon cost
    public int AutoJoinRaidCost = 3000;    // Auto raid cost
    public bool IsAdministrator { get; }   // RavenNest admin
}
```

---

## Stream Labels

For OBS/streaming software integration:

```csharp
StreamLabels.RegisterText("online-player-count", () => 
    playerManager.GetPlayerCount().ToString());

StreamLabels.RegisterText("uptime", () => 
    Time.realtimeSinceStartup.ToString());

StreamLabels.Register("exp-multiplier", () => 
    GetExpMultiplierStats());
```

---

## Message Filtering

Control which bot messages appear:

```csharp
public void SetMessageFilter(string toggle, bool enabled);

// Filter types
// - "PlayerWelcome" - Join messages
// - "LevelUp" - Level up announcements
// - "ItemDrop" - Item drop notifications
```

---

## Related Projects

- **RavenBot**: https://github.com/zerratar/RavenBot - Twitch chat bot
- **Ravenfall-Overlay**: https://github.com/zerratar/Ravenfall-Overlay - Twitch extension
