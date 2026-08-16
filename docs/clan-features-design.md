# Clan features: bank, permissions, skills

Written after converting the clan pages to the design system. The conversion surfaced what the
server actually stores about clans, and the gap between that and what a clan wants is larger than
the styling gap was.

This is a design, not a change. Everything here touches the database, the API and in most cases
the game client, so it wants its own branch and its own testing rather than being folded into a
front end pass.

## What exists today

| Thing | State |
| --- | --- |
| `Clan` | Real. Owner, level, experience, name, logo, skills. |
| `ClanRole` | `clanId`, `cape`, `level`, `name`. |
| `ClanRolePermissions` | **Real, and complete.** Declared in `Clan.cs`, not `ClanRole.cs`. Thirteen typed permissions, backfilled on startup, enforced by the chat commands. The website ignored it until now. |
| `CharacterClanMembership` | Real. Ties a character to a clan and a role. |
| `CharacterClanInvite` | Real. Drives `/clan-invites`. |
| `ClanSkill` | Real. `clanId`, `skillId`, `level`, `experience`. Only Enchanting writes to it. |
| `ClanBankItem` | **A stub. Declared, referenced by nothing.** |

Two findings worth stating plainly. One is the sort of thing that looks implemented until you go
looking; the other is the opposite, and caught me out.

### `ClanBankItem` cannot work as written

`RavenNest.DataModels/ClanBankItem.cs` is a byte for byte copy of `UserBankItem`, down to the
first field:

```csharp
[PersistentData] private Guid userId;   // in a clan bank
```

It was copied and the owning column was never changed, so the row cannot say which clan holds the
item. Nothing references the type anywhere in the solution, so nothing has ever caught it. Any
work on a clan bank starts by fixing this, and it is a schema change rather than a rename because
the meaning of the column changes.

**Fixed.** The field is `clanId` now. It cost nothing, because there was nothing to migrate: see
the next section for why.

### There is no automatic schema reconciliation

Worth stating plainly, because it changes what "a schema change" means in this project and the
answer was not obvious from the code.

Searched across the whole solution for `EnsureCreated`, `Migrate()`, `CREATE TABLE`, `ALTER TABLE`,
`sys.columns` and `INFORMATION_SCHEMA`. **There are no hits.** There is no `Migrations` folder and
no DDL script. `RavenfallDbContext` calls `UseSqlServer` and nothing else; `OnModelCreating` only
configures conversions and value generation for types whose tables already exist.

So EF Core here assumes the database already matches the model, and nothing checks that it does:

- adding a `[PersistentData]` field, or renaming one, changes the SQL that EF emits and **nothing
  updates the database**. The mismatch surfaces at runtime as a `SqlException` on the first query
  that touches the column, not at startup and not at build;
- adding a new entity needs the table created by hand **and** a `DbSet` added to the context;
- the `*_error_query.sql` files in the repository root are not migrations. They are `INSERT`
  batches `GameData` dumps when a save fails, so the rows can be replayed by hand.

`ClanBankItem` has no `DbSet` and no table, which is why renaming its field was free. It also means
the clan bank needs, in this order: the table created by hand, a `DbSet<ClanBankItem>` in the
context, and only then any code that reads or writes it.

### Correction: ranks are not decorative

An earlier draft of this document said clan ranks had no permissions. **That was wrong**, and the
mistake is worth recording because it is easy to repeat: `ClanRole.cs` holds only `clanId`,
`cape`, `level` and `name`, so the permissions look absent. They are not in that file.
`ClanRolePermissions` is declared in **`Clan.cs`**, and the whole system already exists:

- `ClanRolePermissions` (`clanRoleId`, `permissions`) stores a string of `0` and `1` characters,
  one per permission, positional.
- `ClanRolePermissionsBuilder.Parse` and `.Generate` convert it to and from
  `TypedClanRolePermissions`, which has thirteen typed flags.
- `ClanManager.EnsureClanRolePermissions()` runs at construction and backfills a default row for
  every role in every clan, so no clan is ever without them.
- `GetOwnerPermissions()` returns all ones, so the founder can never be locked out.
- `ClanManager` already enforces them: `AddClanRole`, `RemoveClanRole`, `RenameClanName`,
  invites and role assignment all check first.

The thirteen: `CanRenameClan`, `CanAddClanRole`, `CanRemoveClanRole`, `CanRenameClanRole`,
`CanAssignAllRoles`, `CanAssignRoles`, `CanKickMembers`, `CanKickAllMembers`, `CanMakePublic`,
`CanCreateInvite`, `CanDeleteInvite`, `CanUseClanSkills`, `CanSeeClanDetails`.

So the design problem was never "build a permission system". It was that **the website ignored the
one that existed**, checking `clan.OwnerUserId == session.UserId` everywhere instead, and that
there was nowhere to see or change any of it.

### Authorisation holes found while wiring it up

Three of the four member mutation methods on `ClanService` had **no permission check at all**.
`RemoveInvite`, `UpdateMemberRoleAsync` and `InvitePlayer` checked only that you were signed in,
so any authenticated user who knew a clan id could invite characters to it, cancel its invites, or
restructure its ranks. All three now check.

`RemoveMember` had the opposite fault: it required `character.UserId == user.Id`, so it only ever
removed **your own** characters. A clan owner could not remove anybody from the website. Worse, it
returned null on refusal, which the roster assigned to its member list and rendered as a permanent
loading spinner. Leaving is still always allowed, since that is your own character; removing anyone
else needs the permission, and refusals now return the roster unchanged.

### What was done

- `ClanService` gained `GetRolePermissions`, `GetMyPermissions` and `UpdateRolePermissions`.
- `/clan` now derives what you can do from your rank rather than from founding the clan.
  Renaming, managing members and editing ranks are three separate checks now, not one.
- The **Ranks tab is an editor**: each rank shows what it can do, and anyone whose rank holds
  `CanRenameClanRole` can change it, grouped as Members / Ranks / The clan.
- Editing works on a copy, so cancelling cancels.
- Every member action is gated on its own permission rather than one combined flag, in the UI and
  in the service. The plain kick and assign permissions only reach ranks below your own, and the
  rank dropdown only offers ranks below your own, so an Officer cannot promote themselves.
- `ClanRolePermissionsBuilder.Parse` is bounds checked, so appending a fourteenth permission no
  longer throws on every existing row.

### What a threshold model would have cost

The earlier draft proposed permissions as "minimum rank level", one row per permission per clan.
It is a nice model, and adopting it now would mean migrating a working system and rewriting the
chat command checks for no player facing gain. **Extend the string instead.**

### Note for whoever adds bank permissions

`ClanRolePermissionsBuilder.Parse` indexes thirteen fixed positions:

```csharp
values.CanRenameClan = Bool(permissions[index++]);   // ... thirteen times
```

Every stored row is therefore exactly thirteen characters. **Appending a fourteenth permission for
the bank will throw `IndexOutOfRangeException` on every existing row.** Make `Parse` tolerant of
short strings first, defaulting missing positions to false:

```csharp
private static bool Bool(string s, int i) => i < s.Length && s[i] == '1';
```

That is a five line change and it must land before any new permission is added, otherwise the fix
arrives after the outage rather than before it.

## 2. Clan bank

The precedent is already in the codebase and working: the **stash** is `UserBankItem` plus
`PlayerService.SendToCharacter`. A clan bank is the same machine with a different owner column and
a permission check. That is the argument for doing it: it is not new machinery.

### Schema

```csharp
public partial class ClanBankItem : Entity<ClanBankItem>
{
    [PersistentData] private Guid clanId;              // was userId, the bug above
    [PersistentData] private Guid itemId;
    [PersistentData] private long amount;
    [PersistentData] private string name;
    [PersistentData] private string enchantment;
    [PersistentData] private string tag;
    [PersistentData] private bool soulbound;
    [PersistentData] private Guid? transmogrificationId;
    [PersistentData] private int flags;
}
```

Plus the part that is not optional:

```csharp
public partial class ClanBankLog : Entity<ClanBankLog>
{
    [PersistentData] private Guid clanId;
    [PersistentData] private Guid characterId;
    [PersistentData] private Guid itemId;
    [PersistentData] private long amount;      // negative for a withdrawal
    [PersistentData] private DateTime time;
}
```

**A shared bank without a log is a grief vector, not a feature.** The first time someone empties
it, the clan needs to know who, and without a log the honest answer is that nobody can tell. Every
game that has shipped a guild bank has learned this; there is no reason to learn it again.

### Withdrawal limits

A permission alone is too blunt: "Officers can withdraw" means one bad Officer can take
everything. Add a per rank daily allowance, stored the same way as permissions:

```csharp
[PersistentData] private Guid clanId;
[PersistentData] private int roleLevel;
[PersistentData] private int itemsPerDay;   // -1 for unlimited
```

The log makes this cheap to enforce: sum today's negative amounts for that character.

### Soulbound items

Soulbound items must not be depositable. The item view already surfaces soulbound, and the stash
already refuses to move some things. Whatever rule the stash applies, the bank applies the same
one, and the deposit UI greys them out with the reason rather than failing on submit.

### Item movement must not lose items

There is an existing investigation in the repo,
[marketplace-item-loss-investigation.md](marketplace-item-loss-investigation.md), into items
disappearing during transfer. **A clan bank must be designed against its findings rather than
written first and reconciled later.** Anything the bank does to move an item should go through the
same commit path being designed in
[trade-plan-commit-design.md](trade-plan-commit-design.md), so there is one place where item
movement is made atomic instead of three.

That is the real sequencing constraint on this feature, and it is a good reason to do the plan and
commit work first.

### UI

Almost free, because the item work already landed:

- The bank list is `rf-table` plus `rf-item`, exactly as `/stash` renders.
- Deposit reuses the transfer dialog pattern from the stash, with the character picker replaced by
  a quantity and a soulbound warning.
- Withdraw shows the remaining daily allowance next to the button, so the limit is visible before
  it is hit rather than as an error afterwards.
- A **Log** tab: who took what, when. Plain table, newest first, filterable by member.

### Game client

`!clan deposit <item> [amount]` and `!clan withdraw <item> [amount]`, mirroring the existing stash
commands. This is the part that needs the client, so it can ship after the website version; the
website alone is already useful.

---

## 3. Clan skills

Clan skills already work and, until this week, were rendered nowhere. They are now on both the
clan tab and `/clan`. What is missing is that there is only really one of them.

`EnchantmentManager` is the only writer, so Enchanting is the only skill that ever moves. The
model is general: `ClanSkill` has a `skillId`, so more skills need no schema change, only
something that grants experience.

Candidates that fit what the game already tracks, in rough order of how cheap they are:

- **Enchanting.** Exists. Gates enchantment count and success rate.
- **Banking.** Raises the clan bank's item cap. A natural sink for a clan that is hoarding, and it
  gives the bank a progression rather than being a static box.
- **Loyalty.** Raises the clan's share of loyalty points earned by members on other streams. Ties
  two features together that currently do not know about each other.
- **Expedition.** Raises drop rates for members playing on the same stream at the same time, which
  rewards the thing clans are actually for.

Each one wants a source of experience and a curve. Do not add a skill without a reason for its
number to move; a clan skill stuck at level 1 forever is worse than not having it.

---

## 4. Other things worth building

Ordered by value against effort, from what the data already supports.

**Clan activity feed.** The bank log generalises: joins, leaves, promotions, level ups, big
enchants. Most of these already happen somewhere in the managers; they just are not recorded. One
table, and the clan page stops being a static roster.

**Who is playing right now, everywhere.** Already built for the clan pages this week. The same
join belongs on the overview: "3 of your clan are playing" is a reason to open the game.

**Clan level rewards.** Clan level exists and does nothing visible. Even a cosmetic ladder, a cape
tier per five levels, would make the experience bar mean something. Ask what clan level should
unlock before adding more ways to earn it.

**Rank capes.** `ClanRole.Cape` is stored and never shown on the site. If the client renders it,
the site should at least name it.

**Clan highscore.** The highscore page ranks characters. Ranking clans by total level, by clan
level, or by members playing this week is the same query with a group by, and it gives clans a
reason to recruit.

**Leaving a clan you do not own.** Currently only reachable from a character's clan tab. It should
be on `/clan` too, and it should be the one thing a member can always do.

---

## Suggested order

1. ~~**Rank permissions.**~~ Done. The system already existed; the website now uses it and there
   is an editor for it.
2. ~~**Make `Parse` tolerant of short permission strings.**~~ Done.
3. **Plan and commit item movement**, per the existing design doc. The bank depends on it.
   *Half done.* `TradePlan`, `ItemMove`, `CoinMove` and `TradeExecutor` exist in
   `RavenNest.BusinessLogic/Game/Trading`, with validate-then-commit and exact rollback. Proved
   against a fake world: a two seller purchase rolls fully back from each of its eight possible
   failure points, and quantities are accumulated across the plan so two moves that are only
   impossible together are refused before anything is written.

   **What is left is the risky half:** nothing live uses it yet. `SellItemToVendor` still has
   three implementations that disagree about which safety checks apply, and `BuyItem` still
   interleaves selection, affordability and mutation. Moving those onto the executor is a change
   to live economy code and wants its own review rather than being folded into the bank.
4. **Clan bank**, including the log and the per rank daily limit from day one.
   Schema is written and ready to run: `sql/clan-bank.sql` in the RavenNest repo creates
   `ClanBankItem`, `ClanBankLog` and `ClanBankWithdrawalLimit`. It has to be run against the live
   database *before* the DbSets are added, because GameData loads every set eagerly at boot and a
   DbSet without a table takes the server down rather than degrading.
5. **Clan activity feed**, which is the bank log generalised.
6. **More clan skills**, once there is something for them to gate.
