# Admin tools, enchanting, and streamer features

Three areas raised after the first feature survey, plus two corrections to it.

## Corrections to feature-opportunities.md

**The economy aggregates are already surfaced.** I wrote that
`DailyAggregatedEconomyReport` and `DailyAggregatedMarketplaceData` had nothing reading them. There
is a whole `Pages/Admin/Economy/` folder: `EconomyOverview`, `Marketplace`, `MarketplaceAvgPrice`,
`MarketplaceItemAmountsSellers`. I surveyed the domain models and the dashboard and never opened
the admin pages, so I reported "unused" when I meant "not used by anything I looked at".

The opportunity is still real but it is a different one: **the economy data is admin only.** A
public, read only version is what a trading community actually wants.

**The NPC tables are empty**, so the bestiary stays blocked. Recorded in the other document.

Both mistakes have the same root: I treated absence of evidence in the places I searched as
evidence of absence. Worth stating twice because it is the failure mode of this kind of survey.

## 1. Admin pages are unconverted

Every page under `Pages/Admin/` still has its own `.razor.css` and none use the design system. That
is now the largest block of unconverted UI in the project, and the components it needs all exist:
`rf-table`, `rf-item`, `rf-toolbar`, `rf-modal`, `rf-facts`.

**The admin panel is now fully converted.** There are no `.razor.css` files left anywhere under
`Pages/Admin/`. The order below is kept as a record of what was done and what each page turned up.

The last four:

- **`ItemDropManagement`.** The skill index went from the row into `SkillNames` with no bounds
  check, so a drop pointing at a skill index that no longer exists took the page down. Drop chance
  was printed as the stored fraction, `0.0125` rather than `1.25%`, which is not what anyone
  setting one is thinking in. The delete confirm named nothing, in a list where every row is the
  same shape.
- **`SessionManagement`.** `<input type="checkbox" value="@activeSessionsOnly">` again, the fourth
  copy: the filter checkbox never showed whether the filter was on. The player count was a `<td>`
  with a click handler, which no keyboard reaches and nothing announces. The player list was a bare
  div with no backdrop. And the two test buttons that make somebody else's live stream raid yours
  fired on the first click, labelled only by a title attribute.
- **`RavenbotLogDisplay`** and **`RavenbotLogDisplayLine`.** Severity was carried by row colour
  alone, so a warning and an error were two shades of brown; each row now has the level written in
  a badge, per rule 4. The line component read its severity in `OnInitialized`, so a row reused for
  a different entry as the list grew kept the first entry's styling. The load buttons lived in a
  `colspan` row inside the table body. The header now counts the warnings and errors in what has
  been loaded, which is what anyone opening a log is looking for.

Do them in this order, by how often they are used:

1. ~~`UserAndCharacterManagement` / `UserSearch` / `PlayerManagement` - the daily tools.~~ Done,
   along with `AdminUserView`. Both admin layouts now carry `.rf-dash`, so the sidebar, top bar and
   heading rules are right on every admin page even before the page itself is converted.
2. ~~`ItemManagement`~~ done, see below. `ItemDropManagement` **still to do** (387 lines, and the
   same shape, so `.rf-form` should carry most of it).
3. ~~`Economy/*`~~ Done. Three pages rather than four, see below.
4. `SessionManagement` **still to do**; ~~`ServerMangement`~~ (sic), ~~`RavenbotLogs`~~,
   ~~`AdminTools`~~ done. `RavenbotLogDisplay` and `RavenbotLogDisplayLine`, the two components the
   log viewer is built from, are still unconverted.
5. ~~`UsersManagement`~~ Done.
6. ~~`CodeOfConduct`~~ Done.

### What the last four turned up

The pattern held on every one of them.

**`UsersManagement`.** The "no highscore" column was `<input type="checkbox" value="@...">`, which
does not set a checkbox: fifty rows of boxes that all rendered unchecked whatever the stored value
was, on the one column whose entire job is to show that value. Same bug as `AdminUserView` had.
Suspending an account happened on the first click of a button sitting in a row of five others.
`LoadIndicator` inside the `tbody` again, `Next` paging past the end again, `pageCount` off by one
again, and `user.Characters.Count` unguarded.

**`CodeOfConduct`.** Status was one string called `updateError`, rendered in the error colour
whether it said "Message cannot be null" or "Last modified ..., revision 4", so a save that worked
looked like a save that failed. Clearing the message and saving **deletes the code of conduct**,
which was true before and was documented only in the text of the error you got once it had already
happened. It asks now.

**`ServerMangement`.** The server announcement goes to every player currently in the game and
cannot be recalled, and it fired on one click with nothing asked. The experience multiplier form
returned silently on every failure path, so pressing Send with an unparseable multiplier did
nothing at all with no way to tell why; the times were free text boxes parsed with
`DateTime.TryParse`. Both forms now report what happened, the times are `datetime-local`, and an
event that ends before it starts is refused. `Sentence()` indexed the first character of a key
without checking there was one. The bot stat fallback was eleven hand written label and value divs,
identical apart from two strings each.

**`RavenbotLogs`.** Delete was an anchor with a click handler, so it could not be reached from a
keyboard, and it deleted on the first click. Files over a gigabyte reported their size as the
string "very big".

**`ItemManagement`.** The dangerous one, and worth correcting an assumption I had written down
before reading it: both destructive actions **were** already confirmed, through the browser's own
`confirm()` box. What was wrong was what they said. Deleting asked "Are you sure you want to delete
this item?" without naming the item or mentioning that players are holding it; it now names the
item and says how many exist across every inventory, stash, vendor and listing, because deleting
something nobody holds and deleting something four hundred players are carrying are not the same
decision. Wiping possessions returned silently when the count was zero, so pressing it on an unused
item looked exactly like the button being broken.

A recipe ingredient was resolved with `availableItems.First(...)`, which throws rather than
returning null when a recipe names an item that has since been deleted, taking the whole item list
down. The category rail was the **fourth** hand written copy of the same nineteen buttons and is
now `ItemFilterOptions` like the other three. There was no search on a list of every item in the
game.

The editor modal was its own implementation: a background div, a close button made from a div, and
a tab strip made from more divs, none of it reachable from a keyboard. The shell is now the shared
modal and tabs. The form inside it is three hundred lines of label and input rows, and rather than
rewrite each row as an `rf-field`, there is a new `.rf-form` block in the components file that
styles the shape the markup already has. `ItemDropManagement` is the same shape and should be able
to use it as is.

### The economy pages

Converted, and they shared a `<ul>` of links to each other that no two of them agreed on. That is
now `EconomyNav`, one tab strip, which also marks the page you are on.

The arithmetic on the overview was wrong, not just plain. **Coins are stored per account and the
page was walking every character**, adding that character's account balance to the total, so an
account with five characters had its coins counted five times. "Total coins in the game" was
inflated by however many characters people happen to have, and "average per player" divided that
inflated total by the character count. Neither number was wrong by a constant factor, so they could
not even be compared with themselves over time. It walks accounts now, which is both correct and
cheaper. A median sits next to the average, because a handful of accounts hold most of the coins in
any game economy and an average alone says nothing about a normal player.

Four crashes fixed on the way, all of the same kind, all hidden by an empty `catch` that let the
page render as though the data were simply missing:

- `GetResources(character).Coins` with no null check, which is the exception that was reported.
- `GameData.GetItem(id).Name` on the marketplace detail page and again on the average price page,
  so one item deleted since it last sold took the page down.
- `GetCharacter(id).Name` for both buyer and seller in the transaction log, which is exactly where
  deleted characters turn up.

Those `catch (Exception exc) { }` blocks are why none of this was visible. They are now a flag the
page reads, and the page says the figures are incomplete rather than showing an economy with no
coins in it.

Two unbounded renders fixed: thirty days of every completed sale went into the DOM in one go, and
the average price page drew **one chart per item with no limit**, which for a busy marketplace is
hundreds of charts. Both paged, and the price charts are ordered by how much was actually traded so
the items that matter are on the first page.

`MarketplaceItemAmountsSellers` is deleted. Its entire file was commented out and it was written
against `ChartJs.Blazor`, which no project references, so it could not have compiled had anyone
uncommented it. What it would have shown, amount and seller count per item, is in the Marketplace
tab's daily summary already.

`AdminCharactersView` (374 lines, 306 lines of stylesheet) and `AdminPlayerInventory` are the two
components those pages still hang off and are the biggest remaining pieces. `AdminPlayerInventory`
is the same item table problem `/stash` and `PlayerInventory` already solved.

### Missing admin features

**Add item with an amount.** Done. `PlayerManager.AddItem` always took `int amount = 1`;
`PlayerService.AddItem` dropped it and always passed one, so granting a hundred of something meant
a hundred clicks. The service now passes it through and the dialog has an amount field with 1 / 10
/ 100 / 1000 shortcuts.

Worth doing next, in the same spirit of "the manager can already do it, the screen cannot":

- **Bulk grant to many characters at once**, for event prizes and compensation after an incident.
- **Remove or reduce an item**, not just add. Correcting a mistake currently needs a database edit.
- **Set resources** (coins, wood, ore) directly. Same reason.
- **An audit trail on admin actions.** Who granted what, to whom, when. The same argument as the
  clan bank log: without it, a dispute has no answer. This is the one I would push hardest for.
- **Search characters by item held**, which is how you investigate duplication.

## 2. Enchanting is missing from the dashboard

A fair hit: I read `EnchantmentManager` while working out clan skills and did not notice that
enchanting has no presence on the site at all, despite the clan tab now showing the Enchanting
level that gates it.

What exists:

| Piece | State |
| --- | --- |
| `PlayerManager.EnchantItemInstance(SessionToken, characterId, inventoryItemId)` | Real |
| `PlayerManager.GetEnchantmentCooldown(SessionToken, characterId)` | Real |
| `PlayerManager.ClearEnchantmentCooldown(SessionToken, characterId)` | Real |
| `GameData.GetEnchantmentCooldown(characterId)` | Real, and takes **only a character id** |
| `ItemService.GetItemEnchantments(InventoryItem)` | Real, already used to render enchantments |
| `PlayersController.EnchantItem` | Real, but authenticated by game **session token** |

### The split that matters

**Showing the cooldown is easy, and is now done.** `GameData.GetEnchantmentCooldown(characterId)`
needs nothing but a character id. `PlayerService.GetEnchantmentCooldownEnd` wraps it and the
character page shows the remaining time and the clock time it frees up, in the same facts list as
the auto train target.

**Fixed at the source.** `GameData.GetEnchantmentCooldown` now null checks the Enchanting skill
definition and the clan's row for it, and the try/catch in `PlayerService.GetEnchantmentCooldownEnd`
is gone, since it would only hide a real fault now.

Two more callers had the same crash and are fixed with it. `PlayerManager.GetEnchantmentCooldown`
and `PlayerManager.ClearEnchantmentCooldown` both went straight to `cd.CooldownEnd` on a return
value that has always been null for a character with no clan, so both threw for any clanless
character. They are reached from the game client through `PlayersController`, not from the website,
so this was a live server path rather than a page level bug.

One thing left alone deliberately: `GetClanSkillCooldown` creates and stores a cooldown row when
none exists, so reading a cooldown writes to the database. `EnchantmentManager` relies on that when
it sets one, so changing it wants its own change with a look at the write path.

**Performing an enchant from the site is not, yet.** Every write path takes a `SessionToken`, which
is the *game* session, not the website session. Enchanting from the dashboard therefore needs
either an overload that authorises from a website session, or a service wrapper that resolves the
character's active session. That is a real piece of work and it should be designed rather than
bolted on, because enchanting consumes a cooldown and can fail, and the failure modes are exactly
where item loss bugs come from.

### Proposed UI

- **On the item dialog**: an Enchant action when the item is enchantable, showing the clan
  Enchanting level, the success chance, and what the cooldown will be.
- **On the character page**: a cooldown readout next to the training block, since it is the same
  kind of "when can I next do a thing" answer the next level block already gives.
- **On the clan tab**: the clan Enchanting level already shows; link it to "what this lets you
  enchant".

## 3. Streamers

The site treats a streamer as a player who happens to have a Loyalty tab. They are the people who
bring every other user in, and almost nothing here is built for them.

### Things the data already supports

- **A public stream page.** `ravenfall.stream/@yourname`: who is playing right now, tonight's
  levels from `CharacterSkillRecord`, the top players on this stream by loyalty. A page a streamer
  can put in their panels, that updates itself, and that is an advert for the game.
- **"What happened tonight".** `CharacterSessionActivity` plus the dated level records gives an end
  of stream summary: who joined, who levelled, biggest gain, longest session. Streamers post that
  kind of thing; if the site generates it, the site gets linked.
- **Viewer milestones as alerts.** A level 99 by a viewer is a moment on stream. The site knows
  when it happened, to the timestamp.
- **Loyalty leaderboard as an overlay.** The loyalty data is already per streamer. A browser source
  URL with the top ten is a small piece of work and it goes on screen every stream.
- **A returning viewer flag.** Loyalty already knows who has played before. "Welcome back, first
  time in three months" is a good moment and it is a date comparison.

### Things worth building for growth

- **An invite or referral link** that credits the streamer when a new player joins on their stream.
  Loyalty already models the relationship; this makes it visible and rewardable.
- **Clan and stream integration.** A streamer's clan is their community. The clan pages should know
  which clan belongs to which stream.
- **Season or event leaderboards** scoped to one stream, resettable, so a small stream competes
  with itself rather than with the global highscore it will never top.
- **Exp multiplier events per stream**, if the model allows it, as a thing a streamer can trigger
  for a subathon.

### The one I would build first

**The public stream page.** It uses only data that exists, it needs no game client change, it is
the thing a streamer can link, and every visit to it is a person looking at Ravenfall who was not
looking at it before. Everything else on this list is better once that page exists to hang it off.
