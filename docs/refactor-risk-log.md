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

## Not a regression, but must not be forgotten

- **Odin Inspector is broken on Unity 6.7.** `GUITimeHelper.Init` and `SdfIcons.FixBug` both throw
  on every domain reload. Editor only, no runtime effect, but it needs an updated build from
  Sirenix. See [unity-6.7-local-fixes.md](./unity-6.7-local-fixes.md).
- **Third party asset patches live outside version control.** A fresh clone will not compile until
  they are reapplied by hand.
- **No chat rate limiting exists anywhere.** `RavenBot.Announce` sends directly, so any policy
  built on top of it is currently spending an assumed budget rather than a real one.

---

## Before the update ships

1. Resolve the drop announcement volume entry above.
2. Play test a session with a raid, a dungeon and several hundred players, watching status effects,
   healer targeting and player orientation.
3. Compare a profiler capture against the previous release rather than trusting reasoning alone.
   None of the performance work here has been measured on a live session yet.
