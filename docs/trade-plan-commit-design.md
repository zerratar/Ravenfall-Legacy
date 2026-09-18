# Marketplace and vendor: plan, validate, commit

Design notes, nothing implemented. Follows on from
[inventory-architecture-proposal.md](inventory-architecture-proposal.md).

## The finding that shapes everything else

Every trading path in the codebase makes the same mistake: an inventory mutation whose success
is never checked, followed unconditionally by the counter effect. Three instances, three
different symptoms, one cause.

**Listing on the market** (`MarketplaceManager.SellItem`):

```csharp
inventory.RemoveItem(itemToSell, amount);   // returns bool, ignored

var marketItem = new DataModels.MarketItem { Amount = amount, ... };
```

If the remove fails, the item stays in the inventory and also appears on the market.
**Duplication.**

**Selling to a vendor** (`PlayerManager.SellItemToVendor`, the session token overload):

```csharp
inventory.RemoveItem(itemToVendor, amount);   // returns bool, ignored
resources.Coins += itemToVendor.Item.ShopSellPrice * amount;
```

and a few lines later:

```csharp
inventory.RemoveStack(itemToVendor);          // returns bool, ignored
resources.Coins += totalPrice;
```

If the remove fails, the player keeps the item and gets paid. **Coin creation.**

**Buying on the market** (`MarketplaceManager.BuyMarketItem`): coins move, then
`inventory.AddItem(...)` runs and cannot report failure at all. **Item loss.**

`RemoveItem` genuinely can fail. It returns false and logs
`"Error removing item, item could not be found in backpack"` when the stack is not in the
inventory's list.

So duplication, coin creation and item loss are not three historical mysteries. They are one
missing `if`, in three places, with the sign flipped.

That is worth saying plainly because it changes what a fix has to achieve. The goal is not to
add validation to the marketplace. It is to make it structurally impossible to apply half of a
trade.

## The shape these operations share

Listing, buying, vendoring and gifting are all the same thing: **things move between holders,
and either all of it happens or none of it does.**

The differences that look like complexity are not:

- A vendor is just a counterparty that is not a character.
- Buying from several sellers at once is just more entries in the same list.
- Vendor prices varying with stock (`GameMath.CalculateVendorBuyPrice(item, inStock)`) is the
  same "varying price across a quantity" problem the marketplace has, in miniature.

Market listings already escrow correctly: `SellItem` takes the items out of the seller's
inventory and the `MarketItem` row holds them. So a purchase never needs to reach into a
seller's inventory, only into the listing. That is a good existing decision and it makes the
buy side simpler than it first appears.

## The proposal

Three steps, and the middle one is where correctness lives.

```csharp
// 1. Plan. Pure function. No mutation, no data layer writes, no session, no token.
//    Decides what should move. Testable without a database.
TradePlan PlanPurchase(Guid itemId, long amount, double maxPricePerItem, long availableCoins);

// 2. Validate. Re-checks the plan against current state. Returns why, not just whether.
ValidationResult Validate(TradePlan plan);

// 3. Commit. Applies everything or nothing.
TradeResult Commit(TradePlan plan);
```

with a deliberately small plan type:

```csharp
sealed class TradePlan
{
    public IReadOnlyList<ItemMove> Items;   // { From, To, StackKey, Amount }
    public IReadOnlyList<CoinMove> Coins;   // { From, To, Amount }
    public TradeKind Kind;                  // for the ledger record
}
```

`From` and `To` are character ids, with `Guid.Empty` meaning "outside the world": a vendor, a
market listing, a loot drop, an admin grant. That one convention is what lets vendor and
marketplace share an executor instead of each growing their own.

A marketplace purchase across three sellers becomes: three item moves from listings to the
buyer, three coin moves from the buyer to each seller. A vendor sale becomes one item move to
`Guid.Empty` and one coin move from `Guid.Empty`. Same executor, same guarantees.

### Why commit can actually be atomic without database transactions

This is the part worth being clear about, because it is where the current design gets stuck.

The authoritative state is the in-memory entity graph. The database is a batched projection of
it. So a trade does not need a distributed transaction. It needs two much simpler things:

1. **Validate everything before mutating anything.** Every move is checked against current
   state first: the stack exists, it holds enough, the buyer has the coins, nothing is locked.
   Only then does anything change. Most failures never become partial states because they are
   caught before the first write.

2. **Undo what you applied if a later step still fails.** Because every mutation is a field
   write on an object you already have a reference to, the inverse is trivial and exact. Apply
   moves in order, keep the list of what was applied, and on failure walk it backwards. No
   framework, maybe forty lines.

Step 1 makes step 2 rare. Step 2 makes step 1 not have to be perfect.

Crash safety is the remaining gap, and that is what the pending ledger record from the previous
document covers: write the `MarketItemTransaction` or `VendorTransaction` rows as pending
before applying, mark them applied afterwards, and reconcile the stragglers on startup. Nothing
else needs to know about crashes.

### What this collapses

- Three `SellItemToVendor` implementations become one. They currently disagree with each other
  in ways that are certainly not deliberate: only the session token overload calls
  `integrityChecker.VerifyPlayer`, only the other two check `inventory.IsLocked`, and only the
  other two check the result of the remove. Whichever entry point a player happens to hit
  decides which safety checks apply to them. In a single validate step, that question does not
  arise.
- `BuyItem`'s loop stops interleaving selection, affordability, mutation and reporting. The
  comment above it (`todo(zerratar): Rewrite this!! This is horrible`) describes an intended
  allocation rule, cheapest first and fall back to a higher price as long as the average stays
  under the cap, that is not currently implemented. As a pure function it is about fifteen lines
  and can be unit tested.
- Partial fills become data on the result rather than a consequence of where a loop broke.

## What I would not do

Worth stating, given how easy it is to turn this into a framework:

- No generic transaction engine, no command pattern, no event sourcing. Two move types and one
  executor.
- Do not try to make this cover equipment changes, enchanting or crafting. They are not trades
  and forcing them in is how the current `PlayerInventory` reached 34 add and remove methods.
- Do not introduce an interface until there is a second implementation. There will not be one.

## Suggested order

1. Add the missing result checks to the three sites above. That is a handful of lines and stops
   duplication, coin creation and item loss immediately, independent of any redesign. Do this
   first even if nothing else follows.
2. Build `StackKey` (previous document, section B). Plan and validate both need one honest
   answer to "are these the same stack".
3. Extract `PlanPurchase` as a pure function and put tests on it. No behaviour change yet;
   `BuyItem` calls it and then runs its existing loop over the result.
4. Add `Validate` and `Commit` with the applied list and reverse walk. Move `BuyItem` onto it.
5. Move the vendor paths onto the same executor, collapsing the three overloads.
6. Add the pending ledger record.

Steps 1 to 3 are individually shippable and each leaves the system better. Step 4 is the one
that needs care and a test pass, because that is where the guarantee actually lives.

## Open questions for you

- Is there a buy-from-vendor path? `VendorTransaction.TransactionType` and
  `CalculateVendorBuyPrice` suggest both directions exist, but I only traced selling to the
  vendor. If players can buy from the vendor with stock dependent pricing, that is the second
  caller for the allocation function and worth designing in now rather than later.
- Should a partially fillable purchase complete partially or fail whole? Current behaviour is
  partial, and the client is told the amount that succeeded. Keeping that is fine, but it should
  be a stated rule rather than an emergent one.
- `AcquiredUserLock` and `integrityChecker.VerifyPlayer` are applied inconsistently across
  these paths today. Validate is the natural home for both, but which operations genuinely need
  the integrity check is your call.
