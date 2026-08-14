# Refactor risk log

Running record of anything changed during the refactor that could behave differently in a live
session, plus anything deliberately left half done.

**Nothing here may ship unresolved.** Players getting a version that is worse than the one it
replaced is worse than shipping nothing. Every entry is either verified, or has an owner and a
plan, before an update goes out.

Status values: `verified` (proven unchanged), `needs play test` (reasoning is sound but not
observed live), `open` (known difference, not yet resolved).

---

## Behaviour changes

### Drop announcements are now split into several messages
**Commit:** `16e677f` **Status:** `open`

A single item dropped to many players used to produce one message over the 475 character limit,
which chat rejected outright, so nobody was told. It is now split into as many messages as needed.

Strictly better on its own, but it converts one rejected message into several accepted ones, and
Twitch also rate limits. With ~300 auto joined players the extra volume could push other messages
out. Length is fixed; volume is not.

**Resolution:** implement the activity based recipient selection in
[twitch-message-budget.md](./twitch-message-budget.md) before release, so announcements name
recently active players and stay within a real send budget. **This is the one entry most likely to
be noticed by players if left as is.**

### Raid heal target and stream raid target selection rewritten
**Commit:** `356fa50` **Status:** `verified`

`Where + OrderByDescending + FirstOrDefault` and `OrderBy + ThenBy + ThenBy + FirstOrDefault` were
replaced with single passes. LINQ's `OrderBy` is a stable sort, so `FirstOrDefault` returns the
first element among equals; strict comparisons preserve that. `SelectionPatternTests` checks this
over 60,000 randomised trials with deliberately small key ranges so ties are common, and confirms
ordering by squared distance matches ordering by distance.

Verified by test rather than by play, but the property tested is exactly the one that could change
which player is healed or attacked.

### Player pitch correction is now conditional
**Commit:** `356fa50` **Status:** `needs play test`

`LateUpdate` used to assign `transform.rotation` every frame for every player. It now only writes
when the pitch is non-zero. Equivalent unless something depends on the write itself rather than the
resulting value, for example a physics or animation system observing the transform as dirty.

**Resolution:** watch for players visibly tilting on slopes, ragdoll recovery, or after knockback.

### Status effect iteration uses a cached buffer
**Commit:** `356fa50` **Status:** `needs play test`

`UpdateActiveEffects` iterated `statusEffects.Values.ToList()`, allocating twice per affected
player per frame. It now iterates a buffer rebuilt only when an effect is added or removed.

Order is preserved and `RemoveEffect` does not touch the buffer, so removal during iteration is
still safe. The one difference: an effect added by another effect's tick becomes visible on the
next frame rather than the same one. No current effect does this.

**Resolution:** confirm buffs and debuffs still expire correctly, especially chained effects.

---

## Structural changes verified as behaviour preserving

- `fc688e7` **`IAttackable.IsEnemy`** replacing `is EnemyController`. `IAttackable` has exactly two
  implementors and `EnemyController` has no subclasses, so the two agree for every possible value.
- `2a4e000` **`Ravenfall.Core` extraction.** File moves only. `.meta` files moved with their `.cs`
  so GUIDs are unchanged and scene references still resolve. `Skills.GetCombatSkill` widened to
  public, which is additive.
- `8d9399d` **`PlayerProgression` extraction.** Logic moved with `PlayerController` keeping one
  line delegates, so no call site changed.
- `0dea5a5` **PipeSQL assembly.** Dependencies inverted, no logic changed.
- `424c494` **`TcpApi_Old.cs` deleted.** 852 lines, zero of them live code.

---

## Editor iteration speed

Entering and exiting play mode takes 30 to 60 seconds, and the editor is unresponsive during
"executing OnBeforeAssemblyReload callbacks". Not a regression from this work, but it costs more
time than anything else being optimised here.

**Disabling domain reload is the usual fix and is NOT safe in this project.**
`ProjectSettings/EditorSettings.asset` has `m_EnterPlayModeOptionsEnabled: 1` with
`m_EnterPlayModeOptions: 0`, so the feature is on but nothing is actually disabled and a full
domain and scene reload still happens on every play.

Turning on `DisableDomainReload` would require static state to survive being carried between play
sessions, and there are **111 mutable static fields** in game code. The dangerous ones are in
`GameCache`: `IsAwaitingGameRestore`, `stateCache` and `playerCache`. Stale restore state between
sessions would produce confusing bugs that look like gameplay problems.

An earlier note in this session claimed the codebase had zero mutable static state. That was
wrong, the check that produced it silently matched nothing. The real figure is 111.

Doing it properly means resetting those statics from
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, which is real
work but would pay for itself quickly at 30 to 60 seconds per iteration.

**Unproven hypothesis worth testing first:** the project's own code registers exactly one
`[InitializeOnLoad]` (`HierarchySeparator`), so the minute spent in assembly reload callbacks is
coming from packages or plugins rather than game code. Odin is the prime suspect: it is already
known to be incompatible with Unity 6.7, throws from two `InitializeOnLoad` handlers on every
domain reload, and does heavy assembly scanning at editor time. This is a hypothesis, not a
measurement. The editor log has no domain reload profiling enabled, so it could not be confirmed
from here. Testing it means temporarily moving `Assets/Plugins/Sirenix` aside and timing a reload.

## Not a regression, but must not be forgotten

- **Odin Inspector is broken on Unity 6.7.** `GUITimeHelper.Init` and `SdfIcons.FixBug` both throw
  on every domain reload. Editor only, no runtime effect, but it needs an updated build from
  Sirenix. See [unity-6.7-local-fixes.md](./unity-6.7-local-fixes.md).
- **Third party asset patches live outside version control.** A fresh clone will not compile until
  they are reapplied by hand.
- **No chat rate limiting exists anywhere.** `RavenBot.Announce` sends directly, so any policy
  built on top of it is currently spending an assumed budget rather than a real one.

---

## Live play test, first session after the refactor

Tested in the editor with a real Twitch chat connection: `!join` worked, the character joined and
began training immediately, `!leave` worked, `!join` again worked, and after restarting the game the
character was restored and auto joined correctly. No behavioural regression observed.

Two things noted that are not yet explained:

- Several seconds between typing `!join` and the character appearing, on an otherwise empty server.
  Not measured against a pre refactor baseline, so it is not known whether this is new.
- Profiler with one player: `PlayerDetails.Update` was the largest per frame allocation at 2.7 KB,
  since fixed in `283ffdd`. `PlayerController` showed **0 B** allocated, which is the
  `UpdateActiveEffects` fix confirmed under real measurement rather than by reasoning.

No CPU comparison against the previous release exists yet, because the game had not been run for
long enough before the changes to capture a baseline.

## Before the update ships

1. Resolve the drop announcement volume entry above.
2. Play test a session with a raid, a dungeon and several hundred players, watching status effects,
   healer targeting and player orientation.
3. Compare a profiler capture against the previous release rather than trusting reasoning alone.
   None of the performance work here has been measured on a live session yet.
