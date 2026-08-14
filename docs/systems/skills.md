# Skills & Progression System

**Location:** `Assets/Scripts/Game/Player/Skills.cs`, `Assets/Scripts/Game/Skills/`

## Overview

Ravenfall features 17 trainable skills divided into combat and non-combat categories. Players gain experience by performing tasks, and levels are calculated using a standard RPG experience curve.

## Skill List

### Combat Skills
| Skill | Description |
|-------|-------------|
| Attack | Melee accuracy |
| Defense | Damage reduction |
| Strength | Melee damage |
| Health | Hit points |
| Magic | Magic damage |
| Ranged | Ranged damage |
| Healing | Healing power |

### Gathering Skills
| Skill | Description |
|-------|-------------|
| Woodcutting | Chop trees |
| Mining | Mine rocks |
| Fishing | Catch fish |
| Farming | Grow crops |
| Gathering | Collect herbs/resources |

### Production Skills
| Skill | Description |
|-------|-------------|
| Crafting | Create items |
| Cooking | Prepare food |
| Alchemy | Brew potions |

### Misc Skills
| Skill | Description |
|-------|-------------|
| Slayer | Kill special enemies |
| Sailing | Travel by ferry |

## Skills Class

```csharp
public class Skills : IComparable
{
    // All 17 skills as SkillStat instances
    public SkillStat Attack;
    public SkillStat Defense;
    public SkillStat Strength;
    public SkillStat Health;
    public SkillStat Magic;
    public SkillStat Ranged;
    public SkillStat Healing;
    public SkillStat Farming;
    public SkillStat Cooking;
    public SkillStat Crafting;
    public SkillStat Mining;
    public SkillStat Fishing;
    public SkillStat Woodcutting;
    public SkillStat Slayer;
    public SkillStat Sailing;
    public SkillStat Gathering;
    public SkillStat Alchemy;
    
    // Computed properties
    public bool IsDead => Health.CurrentValue <= 0;
    public int CombatLevel { get; }          // Averaged combat skills
    public double TotalExperience { get; }   // Sum of all experience
    public float HealthPercent { get; }      // Current/Max health
}
```

### Combat Level Calculation

```csharp
public int CombatLevel => (int)(
    (Attack.Level + Defense.Level + Strength.Level + Health.Level) / 4f
    + (Ranged.Level + Magic.Level + Healing.Level) / 8f
);
```

## SkillStat Class

**Location:** `Assets/Scripts/Game/Skills/SkillStat.cs`

Each skill is represented by a `SkillStat`:

```csharp
public class SkillStat
{
    public string Name;                    // Skill name
    public Skill Type;                     // Enum value
    public int Level;                      // Current level (1-999)
    public int CurrentValue;               // Current HP (for Health)
    public int MaxLevel;                   // Level + bonuses
    public double Experience;              // Total experience
    public float Bonus;                    // Bonus levels from effects
    public int Index;                      // Array index for fast lookup
    public bool IsDirty;                   // Needs sync to server
}
```

### Experience Per Hour Tracking

```csharp
private List<ExpGain> expGains;           // Recent exp gains
private float windowDuration = 180;       // 3 minute window

public double GetExpPerHour();            // Calculate exp/hour rate
```

## Experience System

### Adding Experience
```csharp
public void AddExp(double experience)
{
    // Add to total
    Experience += experience;
    
    // Check for level up
    while (Experience >= GameMath.ExperienceForLevel(Level + 1))
    {
        Level++;
    }
    
    // Mark for server sync
    IsDirty = true;
    
    // Track for exp/hour calculation
    expGains.Add(new ExpGain(experience, Time.time));
}
```

### Level Requirements

The game uses the `GameMath` class for experience calculations:

```csharp
public static class GameMath
{
    public const int MaxLevel = 999;
    
    public static double ExperienceForLevel(int level);
    public static int ExperienceToLevel(double experience);
}
```

## Task System Integration

Skills are trained through the Chunk/Task system:

### TaskType to Skill Mapping
| TaskType | Primary Skill |
|----------|---------------|
| Fighting | Attack/Def/Str/Magic/Ranged/Healing |
| Woodcutting | Woodcutting |
| Mining | Mining |
| Fishing | Fishing |
| Farming | Farming |
| Crafting | Crafting |
| Cooking | Cooking |
| Gathering | Gathering |
| Alchemy | Alchemy |

### Experience Factors

Experience gained is modified by:

1. **Chunk Level** - Training in appropriate areas
2. **Rested State** - Bonus from resting at Onsen
3. **Village Bonuses** - Town building bonuses
4. **Event Multipliers** - Subscription/bit events
5. **Scrolls** - Consumable exp boost items

```csharp
public double CalculateExpFactor(TaskType taskType, PlayerController player, out ExpGainState state)
{
    // Returns 0.0 to MaxExpFactorFromIsland based on:
    // - Player level vs area level
    // - Whether this is the best training spot
    // - Equipment and bonuses
}
```

### ExpGainState

```csharp
public enum ExpGainState
{
    FullGain,           // Full experience
    ReducedGain,        // Partial experience (overleveled)
    LevelTooLow,        // Cannot train here yet
    LevelTooHigh,       // Area too low level
    TargetLevelReached, // Auto-train target hit
    PlayerRemoved,      // Player left game
    NotAValidSkill      // Invalid skill for task
}
```

## Dirty Mask System

For efficient server sync, skills use a dirty mask:

```csharp
public uint GetDirtyMask()
{
    uint mask = 0;
    for (int i = 0; i < SkillList.Length; i++)
    {
        if (SkillList[i].IsDirty)
            mask |= (uint)(1 << i);
    }
    return mask;
}

public void ClearDirtyMask()
{
    foreach (var skill in SkillList)
        skill.IsDirty = false;
}
```

## Combat Skill Selection

For fighting tasks, players can choose which combat style to train:

```csharp
// Training modes
player.TrainingMelee   // Attack, Defense, Strength, Health
player.TrainingRanged  // Ranged, Health
player.TrainingMagic   // Magic, Health
player.TrainingHealing // Healing, Health
player.TrainingAll     // All combat skills
```

## Skill Lookup

```csharp
// By name
Skills.TryGetSkill("mining", out SkillStat skill);

// By enum
SkillStat mining = player.Stats[Skill.Mining];

// Get all as array
SkillStat[] all = player.Stats.SkillList;
```
