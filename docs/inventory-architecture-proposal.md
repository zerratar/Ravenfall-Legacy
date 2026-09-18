# Inventory and trading: what to change server side

Design notes, nothing implemented. Companion to
[marketplace-item-loss-investigation.md](marketplace-item-loss-investigation.md).

## The actual problem

Three separate things combine into "coins gone, items missing", and only one of them is the bug
found in the marketplace investigation.

### 1. There is no unit of work

A single marketplace purchase writes to five entity sets: `MarketItem`, the buyer's `Resources`,
the seller's `Resources`, `InventoryItem`, and `MarketItemTransaction`.

`GameData.SaveChanges` iterates entity sets one at a time, opening a connection per batch:

```csharp
foreach (var entitySet in entitySets)
{
    var queue = BuildSaveQueue(entitySet);
    while (queue.TryPeek(out var saveData))
    {
        using (var con = db.GetConnection())
        {
            ...
            var result = command.ExecuteNonQuery();
```

There is no transaction spanning the sets. On a `SqlException` it creates a restore point for
that one set and carries on. So even with perfect in-memory logic, a database level failure can
persist "coins moved" without "item added". The in-memory model is atomic only because nothing
interrupts it, which is not a guarantee.

This is the thing worth fixing properly. The rest is comparatively easy.

### 2. Failure is unobservable

Covered in the investigation: `gameData.Add` can return `AlreadyAdded`, `AlreadyRemoved` or
`AlreadyExists`, `InventoryItemCollection` stores that in `LastAddResult`, and nothing ever
reads it except a log string. An operation that did not happen reports success.

### 3. Authority is split by accident rather than by rule

The offline tolerance is worth keeping. The problem is not that the client has authority over
some things, it is that the client sends **state** rather than **intent**. A state message says
"my inventory is X", which overwrites. An intent says "I picked up 3 coal", which merges. State
messages cannot be safely replayed, reordered, or arrive late. Intents can.

That single distinction is most of the difference between a system that reconciles cleanly
after the server was away and one that produces bad states.

## Recommendations, cheapest first

### A. Make failure impossible to ignore

Small, self contained, no design commitment. Worth doing regardless of everything below.

- Have the add path return a result rather than `List<InventoryItem>`, and have callers check
  it.
- Log every non success `AddEntityResult` for inventory items with character id and item id.
- In `BuyMarketItem`, move the inventory add ahead of the coin transfer. The worst case becomes
  a purchase that did not happen rather than one that was paid for and not delivered. That is
  the correct way round for a failure you cannot yet prevent.

### B. Collapse the overloads with one stack key

`PlayerInventory` is 1861 lines. The add/remove/stack surface alone is 34 methods:

| Method | Overloads |
| --- | --- |
| `CanBeStacked` | 12 |
| `GetUnequipped` | 6 |
| `RemoveItem` | 5 |
| `AddItem` | 4 |
| `AddItemStack` | 2 |
| `AddItemInstance` | 2 |
| `TryAddItem` | 2 |
| `TryRemoveItem` | 1 |

The cause is not carelessness. There are five different representations of an item stack:
`DataModels.InventoryItem`, `Models.InventoryItem`, `ReadOnlyInventoryItem`, `UserBankItem` and
`AddItemRequest`. Any operation comparing two of them needs a pairwise overload, so the count
grows with the square of the representations. Twelve `CanBeStacked` overloads is what that
looks like.

The fix is one value type that every representation projects to:

```csharp
public readonly struct StackKey : IEquatable<StackKey>
{
    public readonly Guid ItemId;
    public readonly string Tag;
    public readonly string Enchantment;
    public readonly Guid? TransmogrificationId;
    public readonly string Name;
    public readonly int? Flags;
}
```

Five projections replace twelve pairwise comparisons, and "can these stack" becomes
`a.Key == b.Key` plus a single `IsStackable(key)` predicate. It also fixes a live inconsistency:
`GetUnequipped(Guid itemId)` currently matches on item id alone while the created item carries a
tag, so tagged items merge into untagged stacks.

Do this before the marketplace work. Trying to make transactions correct on top of 34 entry
points means proving 34 things.

### C. Split the marketplace purchase into plan and commit

Buying across several sellers at different prices has to stay. It is also the reason the current
code is hard to reason about, because selection, affordability, mutation and reporting are
interleaved in one loop with a `todo(zerratar): Rewrite this!! This is horrible` on top.

Separate them:

```csharp
// Pure. No mutation, no data layer writes. Testable without a database.
PurchasePlan Plan(Guid itemId, long amount, double maxPricePerItem, long availableCoins);

// PurchasePlan { Allocation[] Allocations; long TotalAmount; double TotalCost; }
// Allocation  { Guid MarketItemId; long Amount; double PricePerItem; Guid SellerCharacterId; }

// Validates the plan still holds, then applies it as one unit.
PurchaseResult Commit(PurchasePlan plan, Character buyer);
```

What this buys:

- The allocation rule becomes a pure function, so the "cheapest first, average price under the
  cap" behaviour described in the existing comment can actually be implemented and tested.
- `Commit` has one place to validate and one place to fail. Either every allocation applies or
  none do.
- A listing that changed between plan and commit is detected by comparing the plan against
  current state, rather than by hoping nothing moved mid loop.
- Partial fills become explicit data rather than a side effect of loop exits.

### D. Make the transaction record the recovery mechanism

`MarketItemTransaction` already exists but is written last, as a receipt for something assumed
to have worked. Invert it:

1. Write the transaction rows first, marked pending.
2. Apply the effects.
3. Mark them applied.

A startup and periodic pass finds pending rows and either completes or reverses them. Given
there is no cross entity set database transaction, this is what actually makes a purchase
survive a crash or a partial save. It also gives real answers when a player reports a loss,
instead of inference from logs.

This is worth doing even if nothing else on this list gets done, because it converts silent
corruption into a detectable, repairable state.

### E. Move the client from state to intent

Not for now, but it is the direction that fixes the class of bug rather than instances.

- Client sends `{ intentId, characterId, "add", stackKey, amount }` rather than an inventory
  snapshot.
- Server applies by `intentId` and ignores repeats. Replay after downtime becomes safe, and a
  late message can no longer overwrite newer truth.
- Keep the client authoritative over simulation only: position, current task, animation, combat
  timing. Nothing that can create or destroy an item or a coin.

The offline tolerance survives this and gets better, because a queue of intents is exactly what
you want to flush when the server returns.

## Removing streamer tokens

Smaller than it looks. Twenty references across seven files, four of which are enum
declarations. Sixteen are in business logic:

- `MarketplaceManager` lines 144 and 276: the `itemTag` special case that scopes a listing to
  one streamer.
- `PlayerManager` lines 851, 863, 2076, 2108, 2154, 2188, 2244, 2282: mostly guards excluding
  tokens from vendoring and gifting.
- `PlayerInventory` lines 1377, 1396, 1625, 1649: `AddStreamerTokens`, `GetStreamerTokens` and
  two category exclusion lists.

One trap. `StreamerToken` sits at index 8 of `ItemCategory`:

```
Weapon, Armor, Ring, Amulet, Food, Potion, Pet, Resource, StreamerToken, Scroll, Skin, ...
```

Categories are stored as integers. Removing the member shifts `Scroll` from 9 to 8 and every
value after it, silently recategorising every scroll, skin, cosmetic, quest item and loot box in
the database. **Keep the enum member.** Rename it to something like `Retired_StreamerToken` if
you want it obviously dead, and delete only the behaviour around it.

Suggested order:

1. Stop new tokens being created: remove the `AddStreamerTokens` calls.
2. Remove the marketplace `itemTag` special case, which also deletes the one place where tags
   affect trading and makes B's stack key simpler.
3. Decide what happens to the ones players hold. Leaving them as inert inventory entries is
   fine and is the least risky option; vendoring them for a token amount of coins is friendlier
   if you want them gone from inventories entirely.
4. Drop the guards in `PlayerManager` and `PlayerInventory` once nothing depends on the
   category behaving specially.

## Suggested order overall

1. A, the failure reporting and the operation reorder in `BuyMarketItem`. Days, low risk, stops
   the bleeding.
2. Streamer token removal steps 1 and 2, because they shrink the surface B has to cover.
3. B, the stack key and the collapsed API.
4. C and D together, since plan/commit and the pending transaction record are the same change
   viewed from two sides.
5. E, only when there is appetite for touching the client protocol.

Nothing here needs to happen at once, and each step leaves the system better than it found it.
