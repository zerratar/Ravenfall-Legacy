# Streamer shop (the tavern shelves)

Status: phase 1 (server) built and tested against the local dev server, 2026-10-24: RavenNest
`Game/StreamerShop` (manager, trade world, expiry watcher), `Controllers/ShopController.cs`, the
`shop.buy` socket command, and the game's `ShopApi` (`Shinobytes/RavenNestApi/Endpoints/ShopApi.cs`).
The tables are created at startup by `DatabaseSchema`. Phases 2 to 4 are not started.

The streamer puts items of their own on shelves in the tavern for a coin price. Viewers buy them with
the same `!redeem` they use for the seasonal redeemables. The coins do not go to the streamer: they
go into the village treasury, so they help the whole stream (town hall upgrades, roads and whatever
the village spends coins on next) and move coins around the economy instead of piling up with one
person. Buyers can earn loyalty points with the streamer for what they spend.

## How it plays

**The streamer** clicks an empty shelf in the tavern's redeemables room. A window opens:

1. Pick where the item comes from: the account stash, or one of their own characters that is in this
   stream. Search filters the grid of item icons as you type.
2. Pick an item. Items that cannot be sold are greyed out, with the reason on hover (soulbound, for
   example).
3. Set a price per item in coins, how many to put up (one, or more from the stack), how long the
   listing runs, and who may buy: everyone in the stream, members of the streamer's clan, or only
   chosen roles of that clan. Optionally a limit per viewer.
4. "Put on shelf". The item leaves the stash or character at once and sits on the shelf (held by the
   server) until it is sold, removed or runs out of time.

The window says plainly, next to the price and again on the confirm button, that **the coins go to
the village treasury, not to you**, and shows the treasury's current coins.

Clicking a filled shelf opens the same window on that listing: see what has sold and what the village
has earned from it, change the price, time or access, or take it down. Taking it down (or the time
running out) puts what is left back where it came from: the stash, or the character it came from
(the stash if that character no longer exists or cannot take it).

**A viewer** sees the item on the shelf with its name, price and how many are left, and buys one with
`!redeem <item name>` or `!redeem <shelf number>`. One per redeem, always. The reply says what they got,
what it cost, that the coins went to the village, and the loyalty points they earned.

## Rules

- **Sources.** The account stash, or a character of the streamer's that is in this game session
  (locked to it). A character playing in someone else's stream cannot be used: its inventory belongs
  to that session.
- **What can be sold.** The same rules as gifts and the marketplace: not soulbound (by item type or by
  instance), not transmogrified. Enchanted items are allowed: the listing keeps every instance field
  (enchantment, tag, name, flags), as marketplace listings do, so the buyer gets exactly that item.
  Streamer tokens and other currencies cannot be listed.
- **Price.** Whole coins per item, from 1 up to a cap (proposed 100,000,000), so a mistyped zero
  cannot empty a viewer's account.
- **Quantity.** 1 up to what the source holds. Each redeem buys one.
- **Duration.** From 1 hour up to 30 days (decided 2026-10-24); the window offers 1 hour, 6 hours,
  1 day, 3 days, 7 days, 14 days and 30 days.
- **Shelf spots.** Limited: proposed 3 shelves, plus 1 per town hall tier, up to 8. A reason to
  upgrade the town hall, and the shelves fill the room without crowding it.
- **Who may buy.** Per listing: everyone, the streamer's clan (the clan the streamer owns), or chosen
  roles of that clan (by role, so renaming a role keeps it working). An optional limit per viewer
  (proposed default: none) stops one viewer buying a whole stack.
- **The streamer cannot buy their own listings** (same account, any character).
- **Coins.** Coins are per account in RavenNest, so the price comes off the buyer's account coins and
  goes into the village's `Resources.Coins`. Nothing reaches the streamer's own coins.
- **Loyalty points.** The buyer earns points with this streamer: proposed 1 point per 1,000 coins
  spent, rounded down, at most 100 points per purchase. Numbers to be balanced.
- **The viewer must be playing in the stream** (a character in this session), like any redeem.

### `!redeem` with two kinds of redeemables

`!redeem` already buys the seasonal token redeemables. The shop must not take their names away:

- `!redeem 3` buys whatever is on shelf 3.
- `!redeem <name>` buys a shop item when the name matches a listing and is not a token redeemable.
- When a name matches both, the token redeemable keeps the name and the reply points to the shelf:
  "Rune 2h sword is also on shelf 3 for 25,000 coins: !redeem 3".
- When a name matches several listings (the same item at different prices), the reply lists the
  shelves.

## Server (RavenNest)

New tables (a SQL script in `sql/`, a `DbSet`, an `EntitySet` in `GameData`, as for the clan bank):

- `StreamerShopListing`: Id, StreamerUserId, Shelf (index), ItemId, Amount (left), PricePerItem
  (long), Name, Enchantment, Tag, TransmogrificationId, Flags, SourceKind (Stash or Character),
  SourceCharacterId, Access (Everyone, Clan, ClanRoles), AllowedClanRoleIds, MaxPerViewer, Sold,
  CoinsEarned, Created, Expires. The row is the escrow, as `MarketItem` is.
- `StreamerShopTransaction`: Id, ListingId, StreamerUserId, BuyerUserId, BuyerCharacterId, ItemId,
  Amount, Price, LoyaltyPoints, Created. The audit trail, and what the window shows as sales.

A `StreamerShopManager`, with every move done through the trade framework (`TradePlan` /
`TradeExecutor`, a new `TradeKind.StreamerShop` and a world like `ClanBankWorld` whose holders are the
source, the shop escrow, the buyer and the village), so a listing, a sale or a return happens
completely or not at all:

- **List:** checks the source and the rules, takes the items from the stash or character, writes the
  listing.
- **Edit:** price, time, access and per-viewer limit only. Changing the item means taking it down and
  listing again.
- **Take down:** returns what is left (character first when that was the source, else the stash),
  then removes the listing.
- **Buy:** checks access, stock, the per-viewer limit and the buyer's coins; gives the item, takes the
  coins, adds them to the village, adds the loyalty points, writes the transaction. Delivery first, as
  the marketplace does.
- **Expiry:** a hosted service like `MarketplaceExpiryWatcher` (or that one extended) returns expired
  listings every 15 minutes and survives single failures.

API, with the session token: list the shop (`GET api/shop`), the sellable items from the stash and
the streamer's characters in the session (`GET api/shop/sources`), list (`POST`), edit (`PUT
api/shop/{id}`), take down (`DELETE api/shop/{id}`). Buying goes through the socket like redeem (a new
`shop.buy` command next to `items.redeem`), by listing id. After any change the session gets a
`ShopUpdated` event so the shelves redraw, item add and remove events go to the characters involved,
and the village info push picks up the new coins.

## Game

- **The shelves.** A wall of shelves in the redeemables room, part of the shared layout so every theme
  has it: the inner face of the room's north wall, either side of the archway (room for 8). Each
  `ShopShelf` shows its item (the model from `GenericPrefab`, as `RedeemableDisplay` does, else the
  icon on a stand), a label with name, price and how many are left, and a small "clan only" mark when
  limited. Empty shelves show a faint "For sale: your item here" for the streamer.
- **The window.** UI Toolkit, in the style of the settings window: the source tabs (Stash, then each
  own character in the stream), a search field, a grid of icons with amounts, the listing panel
  (price with the vendor price as a hint, quantity, duration, who may buy, limit per viewer), the
  treasury line, and a "Your listings" list with sold counts, time left, coins earned for the village,
  edit and take down.
- **Icons.** The website's item images (`/imgs/items/<item-name>.png`, about 760 of them), downloaded
  once and cached on disk like clan logos, with a generic icon until they arrive.
- **Redeem.** `RedeemStreamerToken` tries the shop first by shelf number, then by name with the rule
  above, then falls back to the token redeemables as today. New messages: bought (with the village and
  loyalty lines), not enough coins, clan or role only, sold out, limit reached, own listing.
- **Settings.** None needed. The shelves only respond to the streamer's mouse in the game, so only the
  streamer can manage them.
- **Sales report.** A tab in the shop window, not in the village panel (decided 2026-10-24): every
  sale with buyer, item, price and loyalty points, and totals (`GET api/shop/report`).
- **Chat commands.** Allowed, but only for the streamer (the broadcaster), never moderators (decided
  2026-10-24): for example `!shop` to list the shelves and `!shop remove <shelf>`.

## Phases

1. Server: tables, manager on the trade framework, API, expiry, buy command. Tested against the local
   dev server.
2. Game: the shelves and the window (listing, editing, taking down), icons.
3. `!redeem` for shop items, the chat replies, loyalty points.
4. Polish: a `!shop` chat command listing the shelves (optional), a website page showing the stream's
   shop, sale notifications for the streamer.

## Decided (2026-10-24)

- The numbers as proposed above, with listings up to 30 days. Still open to balancing once players use
  it.
- Enchanted items are allowed, transmogrified ones are not.
- Shelf management in chat is for the streamer only.
- The sales report is a tab in the shop window; the village panel stays as it is.

## Risks

- **Item loss.** The listing row keeps every instance field and the trade framework rolls back on
  failure. The marketplace's item loss investigation (`docs/marketplace-item-loss-investigation.md`)
  lists the traps to avoid.
- **Abuse.** A streamer buying their own items through a second account only moves that account's
  coins into the village: no coins are created. The price cap stops accidental huge prices.
- **The old name rule.** Token redeemables keep their names, so `!redeem` behaves as today for
  everything that is not in the shop.
