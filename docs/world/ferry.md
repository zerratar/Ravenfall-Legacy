# Ferry System

**Location:** `Assets/Scripts/FerryController.cs`, `Assets/Scripts/Game/Handlers/FerryHandler.cs`

## Overview

The Ferry is the primary method of travel between islands. Players board the ferry at docks and sail to their destination while gaining Sailing experience.

---

## FerryController

The ferry is a single shared vehicle:

```csharp
public class FerryController : MonoBehaviour
{
    public FerryState state;               // Current state
    public IslandController Island;        // Current/docked island
    
    public bool Docked { get; }            // Currently at dock
    public PlayerController Captain { get; } // Player steering
}
```

### Ferry States

```csharp
public enum FerryState
{
    Idle,
    Docked,
    Sailing,
    Arriving
}
```

---

## FerryHandler (Per-Player)

Each player has a `FerryHandler` to manage their ferry interaction:

```csharp
public class FerryHandler : MonoBehaviour
{
    public PlayerFerryState State;
    
    public bool OnFerry { get; }           // Currently on ferry
    public bool Active { get; }            // Ferry interaction active
    public bool Embarking { get; }         // Walking to ferry
    public bool Disembarking { get; }      // Walking off ferry
    public bool IsCaptain { get; }         // Steering the ferry
    
    public IslandController Destination { get; }
}
```

### Player Ferry States

```csharp
public enum PlayerFerryState
{
    None,
    Embarking,      // Walking to ferry
    Embarked,       // On the ferry
    Disembarking    // Leaving ferry
}
```

---

## Boarding the Ferry

### Embark Process

```csharp
public void Poll()
{
    if (Embarking && !OnFerry)
    {
        // Walk to dock
        if (!player.Island.DockingArea.OnDock(player))
        {
            player.SetDestination(player.Island.DockingArea.DockPosition);
        }
        else
        {
            // At dock, wait for ferry
            player.Movement.Lock();
            
            if (ferry.Docked && ferry.Island == player.Island)
            {
                AddPlayerToFerry();
            }
        }
    }
}
```

### Disembark Process

```csharp
if (Disembarking)
{
    if (ferry.Docked && destination == ferry.Island)
    {
        RemovePlayerFromFerry(destination);
    }
}
```

---

## Sailing Experience

While on the ferry, players gain Sailing experience:

```csharp
private float expTime = 2.5f;              // Exp tick rate
private float expTimer = 2.5f;

void Update()
{
    if (OnFerry)
    {
        expTimer -= GameTime.deltaTime;
        if (expTimer <= 0f)
        {
            expTimer = expTime;
            player.AddExp(Skill.Sailing);
        }
    }
}
```

---

## Captain System

One player can be the ferry captain:

```csharp
public bool IsCaptain => OnFerry && ferry.IsCaptainPosition(transform.parent);

// Captain gets special animations
player.Animations.SetCaptainState(IsCaptain);
```

The captain has higher sailing exp gain and controls the ferry destination.

---

## Dock Controller

**Location:** `Assets/Scripts/DockController.cs`

Each island has a dock:

```csharp
public class DockController : MonoBehaviour
{
    public Vector3 DockPosition { get; }   // Where players wait
    
    public bool OnDock(PlayerController player);  // Is player at dock
}
```

---

## Ferry Progress UI

**Location:** `Assets/Scripts/FerryProgress.cs`

Shows ferry travel progress to players.

---

## Usage Commands

| Command | Description |
|---------|-------------|
| `!sail [island]` | Travel to island |
| `!ferry` | Ferry status |
| `!disembark` | Leave ferry at current island |

---

## Integration with Events

Ferry state is saved when entering events:

```csharp
// DungeonHandler saves ferry context
public class FerryContext
{
    public bool OnFerry;
    public PlayerFerryState State;
    public bool HasDestination;
    public IslandController Destination;
}

// Restore after dungeon/raid
if (Ferry.OnFerry)
{
    player.ferryHandler.Restore(Ferry);
}
```
