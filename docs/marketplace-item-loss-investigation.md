# Marketplace purchase where the coins go but the items do not

Status: investigation only, nothing changed. RavenNest side.

Report: a player bought around 3000 coal from the marketplace. The game showed the right
amount afterwards. After `!leave` and `!join` they had 1 coal and the coins were gone. Not
everyone is affected.

## What the symptom tells us

The client showed the correct amount because it applies the `ItemBuy` game event optimistically
to its own copy. `!leave` and `!join` re-read from the server. So the server side was already
wrong at the moment of purchase, and the client was showing its own optimistic number rather
than anything the server agreed with.

That narrows it: the coins were deducted and the item add did not survive.

## Ruled out

These were checked and are sound:

- `BuyItem` and `BuyMarketItem` deduct coins and add inventory using the same `buyAmount`.
  There is no path where the two numbers differ.
- `CreateInventoryItem` assigns `Amount = amount` with no clamping.
- `AddItem` merges into an existing stack with `stack.Amount += amount`.
- `EntitySet.Add` maintains the keyed lookup groups, so a newly created stack is visible to
  `GetAllPlayerItems` straight away.
- `OnEntityPropertyChanged` re-indexes the groups when a key property changes, and correctly
  skips entities already tracked as added or removed.
- `ValidateInventory` only builds a log message; it never mutates.
- `PlayerInventoryProvider` is a singleton with one `PlayerInventory` per character, so two
  instances cannot fight over the same character.
- Nothing adds or removes an `InventoryItem` in `GameData` outside `PlayerInventory`.

## Most likely cause: the add can fail silently

`InventoryItemCollection.Add`:

```csharp
public void Add(InventoryItem item)
{
    items.Add(item);                     // local list, always succeeds
    LastAddResult = gameData.Add(item);  // can fail, result is not looked at
}
```

`EntitySet.Add` has three failure returns:

```csharp
if (addedEntities.ContainsKey(key) || updatedEntities.ContainsKey(key))
    return AddEntityResult.AlreadyAdded;

if (removedEntities.ContainsKey(key))
{
    // so item was removed but added again.
    // could this be related to moving an item and trying to use the same ID?
    // this is appearant when the "item missing bug" occurs.
    return AddEntityResult.AlreadyRemoved;
}

if (entities.ContainsKey(key))
    return AddEntityResult.AlreadyExists;
```

On any of those the entity never enters `entities`, so it is never persisted and never
returned by a later read. But it is already in the local `items` list, so for the rest of that
session the server behaves as though the player has it.

`LastAddResult` is never acted on. Searching the whole solution, it is read in exactly one
place: a string inside `ValidateInventory`'s error message. Nothing checks it, nothing retries,
nothing tells the caller.

That produces precisely the reported symptom: coins gone, purchase reported as successful,
item present until the inventory is re-read, then absent.

The existing comment on `AlreadyRemoved` is worth noting: this is the branch already suspected
of causing the item missing bug.

## Contributing factor: the purchase is not atomic

`BuyMarketItem` performs its steps in this order:

1. remove or decrement the market listing
2. credit the seller's coins
3. debit the buyer's coins
4. `inventory.AddItem(...)`
5. write the `MarketItemTransaction` and both notifications
6. `return (int)buyAmount;`

Step 4 cannot report failure, and steps 1 to 3 are not rolled back if it does. The return value
is `buyAmount` regardless of what happened, so `BuyItem` reports `ItemTradeState.Success` with
the full amount, the `ItemBuy` event goes to the client with `Amount = buyAmount`, and the
transaction row records a purchase that did not fully happen.

So even once the underlying add problem is fixed, this path has no way to fail safely.

Worth noting: the marketplace add is one of the few inventory mutations with no validation.
`ValidateInventory` is called from six places in `PlayerInventory`, none of them the `AddItem`
overload the marketplace uses.

## How to confirm

The evidence should still exist for the affected player:

1. `MarketItemTransaction` rows for that character and the coal item will show the purchase and
   the amount the server believed it completed.
2. Compare against their current `InventoryItem` row for coal. A transaction for 3000 with an
   inventory row of 1 confirms the loss happened at the add rather than later.
3. Server logs around that time. If `ValidateInventory` ran for that character it prints
   `(Add: <result> Remove: <result>)` along with "Item count mismatch" or "Items with wrong
   amount". An `Add: AlreadyRemoved` or `AlreadyAdded` there confirms the mechanism directly.

If the logs show nothing, the add most likely returned `AlreadyRemoved`, which needs an id
collision with a stack removed earlier in the same save window. Reproducing that means buying
into a stack that was emptied and removed shortly before, in the same session, before a save
flushed `removedEntities`.

## Other defects noticed while reading

Not the cause of this report, but real:

- `GetUnequipped(Guid itemId)` finds a merge target by item id alone. The tag aware call is
  commented out next to it (`//Get(itemId, false, tag)`), yet when no stack is found the new
  item is still created with the tag. So tagged items, which is how streamer tokens work, merge
  into untagged stacks and lose the tag distinction.
- `BuyMarketItem` returns `int` while amounts are `long` everywhere else, and casts on the way
  out. Harmless at 3000, wrong above about 2.1 billion.

## Suggested direction, if this is confirmed

1. Make `AddItem` surface a failed add instead of dropping it, and make the marketplace abort
   and refund rather than reporting success.
2. Order `BuyMarketItem` so the inventory add happens first and the coins move only once it
   succeeded. That turns the worst case into a failed purchase rather than a paid one with no
   goods.
3. Log every non success `AddEntityResult` for inventory items with the character and item id.
   Silent failure is what made this expensive to find.
