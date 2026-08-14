# Combat System

**Location:** `Assets/Scripts/Game/Combat/`

## Overview

The combat system handles player vs enemy encounters, damage calculation, aggro management, and combat animations.

## Core Interfaces

### IAttackable

Both players and enemies implement `IAttackable`:

```csharp
public interface IAttackable
{
    string Name { get; }
    bool InCombat { get; set; }
    bool GivesExperienceWhenKilled { get; }
    float HealthBarOffset { get; }
    Transform Transform { get; }
    
    // Combat methods
    void TakeDamage(IAttackable attacker, int damage);
    int CalculateDamage(IAttackable target);
}
```

## EnemyController

**Location:** `Assets/Scripts/Game/Combat/EnemyController.cs`

### Key Properties
```csharp
public Skills Stats;                       // Enemy stats
public EquipmentStats EquipmentStats;      // Equipment bonuses
public float AggroRange = 7.5f;            // Detection range
public float AttackRange = 2.88f;          // Melee range
public bool AutomaticRespawn = true;       // Respawn after death
public double ExpFactor = 1d;              // Experience multiplier
```

### State Tracking
```csharp
public bool InCombat { get; set; }
public bool IsRaidBoss { get; }
public bool IsDungeonBoss { get; }
public bool IsDungeonEnemy { get; }
public IslandController Island { get; }
public PlayerController TargetPlayer { get; }
```

### Aggro System
```csharp
internal readonly HashSet<string> AttackerNames;
internal readonly List<IAttackable> Attackers;
private readonly Dictionary<string, float> attackerAggro;

public IReadOnlyDictionary<string, float> Aggro { get; }
```

Enemies track aggro per attacker and target the highest threat.

### Combat Loop

```csharp
void Update()
{
    if (InCombat && TargetPlayer != null)
    {
        // Move towards target
        if (Vector3.Distance(position, target.position) > AttackRange)
        {
            movement.MoveTo(target.position);
        }
        else
        {
            // Attack when in range
            if (attackTimer <= 0)
            {
                Attack(TargetPlayer);
                attackTimer = attackInterval;
            }
        }
    }
}
```

### Spawn Points
```csharp
private Vector3 spawnPoint;
private Quaternion spawnRotation;
private float respawnTime = 7f;

// Reset to spawn after death
public void Respawn()
{
    transform.position = spawnPoint;
    transform.rotation = spawnRotation;
    Stats.Health.CurrentValue = Stats.Health.MaxLevel;
}
```

## Combat Skills

**Location:** `Assets/Scripts/Game/Combat/CombatSkill.cs`

```csharp
public enum CombatSkill
{
    Attack,
    Defense, 
    Strength,
    Health,
    Magic,
    Ranged,
    Healing
}
```

## Equipment Stats

**Location:** `Assets/Scripts/Game/Combat/EquipmentStats.cs`

```csharp
public class EquipmentStats
{
    public int Armor;                      // Damage reduction
    public int WeaponPower;                // Base damage
    public int WeaponAim;                  // Accuracy bonus
    public int MagicPower;                 // Magic damage
    public int MagicAim;                   // Magic accuracy
    public int RangedPower;                // Ranged damage
    public int RangedAim;                  // Ranged accuracy
    public int HealingPower;               // Healing bonus
}
```

## Attack Types

```csharp
public enum AttackType
{
    Melee,
    Ranged,
    Magic,
    Healing
}
```

## Combat Handlers

### FightingTask

**Location:** `Assets/Scripts/Tasks/FightingTask.cs`

Handles player combat in training chunks:

```csharp
public override object GetTarget(PlayerController player)
{
    // Find nearest enemy in chunk
    return FindNearestEnemy(player);
}

public override bool Execute(PlayerController player, object target)
{
    var enemy = target as EnemyController;
    
    // Check range
    if (InAttackRange(player, enemy))
    {
        // Perform attack
        player.Attack(enemy);
        return true;
    }
    
    return false;
}
```

### Combat Flow

1. **Target Acquisition**
   - ChunkTask finds nearest valid enemy
   - Player moves into attack range

2. **Attack Execution**
   - Check attack cooldown (`IsReadyForAction`)
   - Calculate damage based on stats + equipment
   - Apply damage to target
   - Play attack animation

3. **Experience Gain**
   - Gain exp based on damage dealt
   - Combat style determines which skill gains exp

4. **Death Handling**
   - Enemy dies → respawns after timer
   - Player dies → respawns at island spawn

## Damage Calculation

Damage is based on:
- **Attack vs Defense** for accuracy
- **Strength + WeaponPower** for melee damage
- **RangedLevel + RangedPower** for ranged
- **MagicLevel + MagicPower** for magic

```csharp
// Simplified damage formula
int baseDamage = (skill.Level + equipmentPower) / 10;
int roll = Random.Range(1, baseDamage + 1);
int finalDamage = Mathf.Max(1, roll - targetArmor);
```

## Status Effects

**Location:** `Assets/Scripts/Game/Player/StatusEffect.cs`

```csharp
public enum StatusEffectType
{
    Poison,
    Stun,
    Slow,
    Burn,
    Heal,
    Shield,
    // ... more
}

public class StatusEffect
{
    public StatusEffectType Type;
    public float Duration;
    public float Value;
    public float TickRate;
}
```

## Stats Modifiers

**Location:** `Assets/Scripts/Game/Player/StatsModifiers.cs`

Temporary stat modifications from buffs, equipment, etc.

```csharp
public class StatsModifiers
{
    // Attack bonus percentage
    // Defense bonus percentage
    // Damage multiplier
    // etc.
}
```

## Health Regeneration

Players regenerate health over time:

```csharp
public float RegenTime = 10f;              // Time between regen ticks
public float RegenRate = 0.1f;             // Percentage of max HP per tick
```

## Combat Animations

Controlled by `PlayerAnimationController`:

```csharp
public void PlayAttackAnimation();
public void PlayRangedAttackAnimation();
public void PlayMagicAnimation();
public void PlayHealingAnimation();
public void PlayDeathAnimation();
public void PlayTakeDamageAnimation();
```

## Special Enemies

### Raid Boss
- High health pool
- Scaled based on player count and levels
- Drops special rewards

### Dungeon Boss
- Final room of dungeon
- Dynamic scaling
- Multiple tiers of difficulty

### Dungeon Enemies
- Spawn in dungeon rooms
- Must be cleared to proceed
- Scale with dungeon level
