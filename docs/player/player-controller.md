# PlayerController

**Location:** `Assets/Scripts/Game/Player/PlayerController.cs`

## Overview

`PlayerController` is the main component attached to each player in the game. It manages all aspects of a player's state, including stats, inventory, combat, and participation in game events.

## Class Definition

```csharp
public class PlayerController : MonoBehaviour, IAttackable, IPollable
```

Implements:
- `IAttackable` - Can be attacked and take damage
- `IPollable` - Can be polled for updates (optimization pattern)

## Key Components

### Identity
```csharp
public Guid Id { get; }                    // Character ID
public Guid UserId { get; }                // User account ID
public string PlayerName { get; }          // Display name
public string Platform { get; }            // "twitch", "youtube", etc.
public string PlatformId { get; }          // Platform-specific ID
public User User { get; }                  // User account data
```

### Stats & Resources
```csharp
public Skills Stats { get; }               // All skill stats
public Resources Resources { get; }        // Coins, wood, ore, fish, wheat
public EquipmentStats EquipmentStats { get; } // Bonuses from gear
```

### Equipment & Inventory
```csharp
public PlayerEquipment Equipment { get; }  // Equipped items
public Inventory Inventory { get; }        // Backpack items
```

### Appearance
```csharp
public SyntyPlayerAppearance Appearance { get; } // Character customization
```

## Handler Components

Each player has specialized handlers for different game systems:

```csharp
public RaidHandler raidHandler;            // Raid participation
public StreamRaidHandler streamRaidHandler; // Stream raid events
public DungeonHandler dungeonHandler;      // Dungeon participation
public ArenaHandler arenaHandler;          // PvP arena
public DuelHandler duelHandler;            // 1v1 duels
public FerryHandler ferryHandler;          // Island travel
public TeleportHandler teleportHandler;    // Teleportation
public EffectHandler effectHandler;        // Visual effects
public ClanHandler clanHandler;            // Clan membership
public OnsenHandler onsenHandler;          // Rest/recovery
public PlayerAnimationController playerAnimations; // Animation control
public PlayerMovementController Movement;  // Navigation
```

## State Tracking

### Combat State
```csharp
public bool InCombat { get; set; }
public bool TrainingMelee { get; }
public bool TrainingRanged { get; }
public bool TrainingMagic { get; }
public bool TrainingHealing { get; }
public bool TrainingAll { get; }           // Train all combat skills
public Transform Target { get; }           // Current target
```

### Activity State
```csharp
public TaskType CurrentTask { get; }       // Current activity
public string taskArgument { get; }        // Task parameters
public Skill ActiveSkill { get; }          // Currently training skill
public bool IsReadyForAction { get; }      // Action cooldown finished
public bool IsAfk { get; }                 // Player idle too long
```

### Event Participation
```csharp
public bool InRaid => raidHandler.InRaid;
public bool InDungeon => dungeonHandler.InDungeon;
public bool InArena => arenaHandler.InArena;
public bool OnFerry => ferryHandler.OnFerry;
```

### Rested State
```csharp
public CharacterRestedState Rested { get; } // Rest bonuses
// - ExpBoost: Experience multiplier
// - CombatStatsBoost: Combat stat bonus
// - RestedPercent: Rest percentage remaining
```

## Attack Ranges

```csharp
public float AttackRange = 1.8f;           // Melee range
public float RangedAttackRange = 15f;      // Bow/crossbow range
public float MagicAttackRange = 15f;       // Magic spell range
public float HealingRange = 15f;           // Healing range
```

## Key Methods

### Equipment
```csharp
public void EquipBestItems();              // Auto-equip best gear
public void UnequipAllArmor();             // Remove all armor
public void UnequipAllItems();             // Remove everything
```

### Combat
```csharp
public float GetAttackRange();             // Get range based on combat style
public float GetHitRange();                // Get hit detection radius
```

### Appearance
```csharp
public void UpdateCharacterAppearance();   // Refresh visual appearance
internal bool TurnIntoMonster(float time); // Halloween/special event
internal bool ApplyPlayerFullBodySkin(GameObject meshSkinObject);
```

### Actions
```csharp
internal void InterruptAction();           // Cancel current action
internal void BeginInterruptableAction<TState>(...); // Start timed action
internal void ClearTarget();               // Clear attack target
```

### State Management
```csharp
internal void SetScale(float scale);       // Resize player (fun command)
internal void SetRestedState(PlayerRestedUpdate data); // Update rest bonus
public void OnRemoved();                   // Cleanup when leaving game
```

## Animation Times

```csharp
private float attackAnimationTime = 1.5f;
private float rangeAnimationTime = 1.5f;
private float healingAnimationTime = 3f;
private float magicAnimationTime = 1.5f;
private float chompTreeAnimationTime = 2f;  // Woodcutting
private float rakeAnimationTime = 3f;       // Farming
private float fishingAnimationTime = 3f;
private float craftingAnimationTime = 3f;
private float cookingAnimationTime = 3f;
private float mineRockAnimationTime = 2f;
private float respawnTime = 4f;
```

## Lifecycle

### Awake()
- Cache transform reference
- Initialize DungeonHandler

### Start()
- Get SyntyPlayerAppearance component
- Get hit range collider
- Ensure all component references
- Add health bar

### Update()
- Process scheduled actions
- Handle regeneration
- Update status effects
- Process item queue
- Handle monster transformation timer

### LateUpdate()
- Correct rotation (ensure upright)
- Check for stuck states

## Twitch Integration Properties

```csharp
public bool IsModerator { get; }           // Twitch mod status
public bool IsBroadcaster { get; }         // Is the streamer
public bool IsSubscriber { get; }          // Twitch subscriber
public bool IsVip { get; }                 // Twitch VIP
public int PatreonTier { get; }            // Patreon support tier
public DateTime LastChatCommandUtc { get; } // Last command time
```

## Special Features

### Diaper Mode
```csharp
public bool IsDiaperModeEnabled { get; }   // No armor equipped (joke mode)
internal void ToggleDiaperMode();
```

### Bot Players
```csharp
public bool IsBot { get; }                 // AI-controlled player
public bool IsNPC { get; }                 // Bot or "Player X" name
public BotPlayerController Bot { get; }    // Bot controller reference
```

### Auto-Training
```csharp
public int AutoTrainTargetLevel { get; set; } // Stop training at level
public Skill? RaidSkill { get; set; }      // Preferred raid combat style
public Skill? DungeonSkill { get; set; }   // Preferred dungeon combat style
```
