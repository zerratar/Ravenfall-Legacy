# Player components: what should stay a MonoBehaviour

A player carries 26 components. Sessions regularly reach a thousand players, so every component is
multiplied by 1000: a Unity object to allocate, register, message and destroy, plus a slot in the
scene hierarchy. Anything that does not need to be a MonoBehaviour is paying that cost for nothing.

`DungeonHandler` has already been converted and is constructed in `PlayerController.Awake`, so the
pattern exists in the codebase. `PlayerProgression` was added as a plain class for the same reason.

## What actually requires MonoBehaviour

Only three things genuinely do:

1. **Inspector wiring that carries real data.** Not a `player` or `gameManager` reference that is
   resolved in `Start` anyway, but fields an artist or designer sets per prefab.
2. **Engine callbacks** - `Update`, `LateUpdate`, `OnTriggerEnter`, `OnDestroy` and friends.
   Note that a plain class can still be polled: `PlayerController.Update` already drives 13
   handlers through `Poll()`, which is cheaper than 13 engine callbacks anyway.
3. **Coroutines**, which need a MonoBehaviour host. A plain class can borrow the owning
   controller's host, so this is a weak requirement rather than a blocker.

## Audit

Measured from the source, not assumed. "Serialized" counts `[SerializeField]` plus public Unity
object fields.

| Component | Serialized | Engine callbacks | Coroutines | Verdict |
|---|---|---|---|---|
| `SyntyPlayerAppearance` | 50 | Awake, Update | yes | **Keep.** Heavily inspector-wired. |
| `PlayerEquipment` | 12 | Awake, Update | no | **Keep.** Real inspector data. |
| `EffectHandler` | 8 | Start, OnDestroy | no | **Keep.** Effect prefab references. |
| `ArenaHandler` | 3 | Start | no | Convertible |
| `RaidHandler` | 2 | Awake, Start | yes | Convertible, needs a coroutine host |
| `DuelHandler` | 2 | Awake, Start | yes | Convertible, needs a coroutine host |
| `StreamRaidHandler` | 2 | Start | no | Convertible |
| `TeleportHandler` | 2 | Start | no | Convertible |
| `PlayerMovementController` | 2 | Start, Update | no | **Keep.** NavMeshAgent coupling. |
| `ClanHandler` | 1 | Start | no | Convertible |
| `OnsenHandler` | 1 | none | no | **Best candidate.** No engine callbacks at all. |
| `PlayerAnimationController` | 0 | Start | no | Convertible |
| `ManualPlayerController` | 0 | Start | no | Convertible |
| `BotPlayerController` | 0 | Start | yes | Convertible, needs a coroutine host |
| `FerryHandler` | 0 | Awake, Start, LateUpdate | no | Convertible if `LateUpdate` is driven by the player |
| `DungeonHandler` | 0 | Start, Update | no | Already converted |

Roughly ten of sixteen are convertible. At a thousand players that is on the order of ten thousand
fewer Unity objects.

For most of them the serialized fields are only `player` and `gameManager`, which
`EnsureComponents` re-resolves at runtime regardless, so nothing real is lost by moving them to
constructor arguments.

## Why this is not a free win

`PlayerController.EnsureComponents` resolves these with `GetComponent<T>()` and has no
`AddComponent` fallback except for `PlayerMovementController`. **The components must already exist
on the player prefab.** Converting one therefore means editing the prefab to remove it, and a
component left behind whose script is no longer a MonoBehaviour shows up as a missing script with
orphaned serialized data.

So each conversion is: change the class, construct it in `Awake`, remove the component from the
prefab, and confirm nothing else in a scene or prefab referenced it directly. That is prefab
surgery, and prefab and scene edits are the least reversible kind of change in this project.

**Recommended order**, safest first, one per pass with a play test between:

1. `OnsenHandler` - no engine callbacks at all, one serialized field
2. `PlayerAnimationController`, `ManualPlayerController` - no serialized fields
3. `ClanHandler`, `TeleportHandler`, `StreamRaidHandler`
4. `FerryHandler` - only once its `LateUpdate` is driven from the player
5. `RaidHandler`, `DuelHandler`, `BotPlayerController` - last, they need a coroutine host

## Where a system would beat a component

Component-per-player is the wrong shape when the same work is done for every player every frame.
The engine-side cost is per object, and the data ends up scattered across a thousand separately
allocated objects, so each frame walks the heap in a random order and misses cache constantly.

Candidates, in rough order of expected benefit:

- **Health regeneration.** `UpdateHealthRegeneration` runs per player per frame and touches a
  handful of floats. A single system iterating packed arrays of `(currentHealth, maxHealth,
  regenTimer)` would do the same work in a fraction of the time, and is trivially parallelisable.
- **Status effect ticking.** Same shape: a small struct per active effect, and far fewer active
  effects than players.
- **Rested and idle timers.** Pure countdown arithmetic with no engine involvement.
- **Distance and range checks.** Currently recomputed per handler per player. Positions in one
  packed array would let this be done once per frame for everyone.

The natural target is Unity's Job System with Burst over `NativeArray`, but the value comes mostly
from the data layout, not from the jobs. Plain arrays of structs iterated in one loop would already
be a large improvement over a thousand objects each doing their own work.

This should not be attempted until the logic is separated. A system needs its data to be isolated
from the MonoBehaviour it currently lives in, which is exactly what the extraction passes are for.
