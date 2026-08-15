# Dashboard design system

Foundation is in and one page is converted. The remaining pages are a page at a time job.

## What was actually wrong

Measured across our own stylesheets, excluding Bootstrap and third party libraries:

- **253 distinct hardcoded colour values**
- **0 CSS variables**
- Two body fonts competing, `Heebo` and `Segoe UI`
- Two FontAwesome versions in play, 5 Brands and 6 Sharp, plus Open Iconic

Worth correcting one assumption: light and dark themes are not fighting each other, because there
is no theme system to fight with. There is no `prefers-color-scheme` rule and no theme class
anywhere in the project. What looks like a theme conflict is 253 literal colours disagreeing,
some picked against a light background and some against a dark one, in files written years apart.

That is good news. There is no theming architecture to unpick, only values to centralise.

## The foundation

**`wwwroot/css/ravenfall-tokens.css`** holds every colour, size, space and radius as a custom
property. Values only, no selectors. Declaring a variable does nothing until something references
it, so loading this globally cannot affect the existing front and admin pages.

**`wwwroot/css/ravenfall-components.css`** holds the pieces a page is assembled from: panels,
buttons, stats, tabs, the player view. These are selected by `rf-` class names alone. No
unconverted page writes those names, so they need no ancestor scope to stay off it, and shared
components can therefore use them outside the dashboard.

**`wwwroot/css/ravenfall-dashboard.css`** holds the page chrome and the legacy cascade overrides,
every one scoped under `.rf-dash`, which `DashboardLayout` puts on its root element. These select
bare elements and class names that already exist in `site.css`, so the scope is what keeps them
from reaching the front pages. A page that has not been converted keeps working exactly as it does
now.

**`wwwroot/css/ravenfall-front.css`** arrived with the front page conversion and is the front page
counterpart of the dashboard file: front page chrome and legacy overrides, every one scoped under
`.rf-front`, which `MainLayout` puts on its root element. The system is four files, not three.

The one thing that file must never do is switch off the legacy heading rules for `.rf-front` as a
whole. On the dashboard that is safe because every dashboard page is converted. On the front page
most are not, and they are still drawn against those rules, so each converted page turns them off
for itself.

All four are loaded from `_Host.cshtml` after `site.css`.

The components/chrome split arrived with `PlayerView`, which renders on `/characters`, on the
front page `/inspect` and in the admin panel. A component in three layouts cannot sit behind a
`.rf-dash` ancestor, or it arrives unstyled in two of them.

## The look

Deliberate choices rather than defaults, since the brief was that it should not feel like Blazor:

- **Warm ink on cool stone.** Backgrounds are blue leaning rather than neutral grey, and text is
  parchment tinted rather than pure white. Pure white on grey reads as a spreadsheet; warm off
  white on blue stone reads as a game.
- **Gold is scarce.** `--rf-gold` is the same `#e8a33d` used to highlight chat commands in the in
  game announcements, so the site and the game agree on what important looks like. It is used for
  stat values, primary actions and accent rules, and nothing else.
- **Panels are lit, not outlined.** Every panel carries a one pixel top highlight
  (`--rf-bevel`) alongside its shadow. That highlight is most of the difference between a game
  panel and a div with a border.
- **One ornament.** `.rf-panel--accent` draws a gold corner bracket from a pseudo element. It
  gives the RPG feel with no imagery and no extra markup.
- **One font.** Character comes from colour, weight and letter spacing rather than a novelty
  typeface, which ages badly and reads poorly at small sizes.
- **Numbers are the gamified part.** `.rf-stat` is a big gold number over a small uppercase label.
  A player checking their dashboard wants numbers that feel like achievements rather than table
  cells.

## Rules

1. Never write a raw colour in a page or component stylesheet. Reference a token. If the token is
   missing, add it to the token file rather than inventing a local value.
2. Spacing comes off the 4px scale. A value not on the scale is a mistake, not a decision.
3. Every interactive state sets both background and text colour. A hover rule that changes only
   the background is exactly how the in game buttons ended up unreadable, and the same trap exists
   here.
4. Colour never carries meaning alone. Status banners say in words what their colour implies, so
   they still work in greyscale or for a colour blind reader.
5. Wide content gets its own `overflow-x` container. The page body must never scroll sideways.

## Converted

- `Pages/Dashboard/Bot.razor`. Its per page stylesheet was deleted rather than rewritten; the page
  now contains no styling of its own, which is the target state for every converted page.

## Remaining, roughly in order of value to players

The brief was that players use the site heavily for inventory, transfers, equipment and clans, and
that those have been neglected. That matches what the pages look like.

1. **Characters** and **Stash**. The most used pages, and the ones that most need the rarity
   tokens so items finally look the same here as they do in the game.
2. **Clan management**, called out as neglected.
3. **Marketplace**. Front page rather than dashboard, but it shares the item presentation problem,
   and it is where the plan and commit work will change behaviour anyway.
4. **Loyalty**, **Quests**, **Notifications**.
5. **Session**, which currently says the page is not done yet. Now that `BotService` exists it can
   probably be folded into the Bot page rather than rebuilt separately.

The rarity tokens are already defined and unused. Doing Characters and Stash next is what makes
them earn their place, and it is the change players will notice most.

## Not done

- Nothing was removed from `site.css`. It is 3436 lines and still styles the front pages, the
  admin panel and the shared layout. It should shrink as pages convert, but deleting from it
  before its dependents are converted would break pages that are currently fine.
- The duplicate icon fonts are untouched. Worth settling on one, but it is cosmetic churn rather
  than a structural problem, and it touches every page at once.
- The dashboard pages have not been checked against a running server yet. `PlayerView`,
  `PlayerSkills` and `PlayerClan` were checked in a browser against a static harness that loads
  the real stylesheets, at 1280px and at 375px, inside and outside `.rf-dash`. That caught three
  layout faults, all fixed: the admin edit button wrapping to a second grid row, the Health row's
  `842 / 1337` sizing its own column so the bars stopped lining up down the list, and the phone
  layout collapsing the skill name track to zero.

---

## Current state (supersedes the two sections above)

### Done

| Area | State |
| --- | --- |
| Tokens + component system | Built. `ravenfall-tokens.css`, `ravenfall-dashboard.css`, scoped under `.rf-dash`. |
| Sidebar and top bar | Restyled. Blue/purple gradient replaced with iron; brass marks the active page. |
| Nav structure | Regrouped: Overview, You, Together, Your stream, Account. |
| `/dashboard` Overview | Rebuilt from a one line stub into the front door. |
| `/bot` | New page. |
| `/characters` | Reworked; tab strip became character cards. |
| `/session` | Folded into `/bot`, kept as a redirect. |
| `/stash` | Converted. Table, search, summary, transfer dialog. Per page stylesheet deleted. |
| `/tv` | Removed. Dead project, cut. |
| `PlayerView` | Converted, along with `PlayerSkills` and `PlayerClan`. Three per component stylesheets deleted. |
| `PlayerInventory` | Converted. Equipment, item grid and all four dialogs. |
| `/clan` | Converted, with `ClanMemberList` and `ClanRoleList`. |
| `/loyalty` | Converted. |
| `/clan-invites` | Converted. Cards, empty state, decline confirms. |
| `/notifications` | Converted. Grouped by day, unread filter. |
| `/patreon` | Converted. Perks say what they do; unlink confirms. |

Every dashboard page is now on the design system. No dashboard page has a per page stylesheet
left; all eleven were deleted rather than rewritten.

Since then, on the front page and in the admin panel:

| Area | State |
| --- | --- |
| `MainLayout` header, nav, footer | Converted. `ravenfall-front.css` added, `MainLayout.razor.css` deleted. |
| `/marketplace` | Converted. Per page stylesheet deleted. |
| `AdminLayout`, `DarkAdminDashboardLayout` | Both now carry `.rf-dash`, so all eighteen admin pages have the dashboard chrome. |
| `/admin/user/{id}` and `AdminUserView` | Converted. Two stylesheets deleted. |
| `/admin/search` | Converted. |
| `/admin/players` | Converted. |

Details of what each gained are in
[frontpage-and-admin-handoff.md](frontpage-and-admin-handoff.md).

### The character tabs, split

Reported from use: the Skills tab had to be scrolled before you reached any skills. Everything
added to it - the four stat tiles, the "Right now" panel, the next level block, the in game
settings, the resources - came to about half a screen above the twenty rows the tab is named after.

Skills is now two tabs. **Overview** leads and is the default, and holds all of the above.
**Skills** holds the list, the sort control and the admin actions, and opens directly on the list.
Combat level and total level stay with the list as a single line of text rather than as stat tiles,
because the tiles cost about ninety pixels to say what twenty pixels says.

The lesson worth keeping: everything added to that tab was individually justified, and the tab
still ended up unusable for its own purpose. "Is this worth showing" is not the same question as
"does this belong on this screen", and only the second one has a budget.

`PlayerOverview.razor` is the new component. The matching of what the game says is being trained
against the rows the site renders moved to `RavenNest.Blazor.Services/TrainingSkills.cs`, because
both tabs need it and it is the piece most likely to change: the names the game sends ("atk",
"heal", "all") are not the names the site shows.

### Only coins are a live resource

Reported: wood, ore, fish, wheat, magic and arrows belong to an older version of the game and
nothing uses them now. Coins are the only resource still in play.

The overview was showing all seven in one grid, which said they were seven live resources. Coins
moved into the headline stats with the other numbers a player checks. The rest appear only when a
character actually still holds a balance, as a chip row under a heading that says the game does not
use them, so a stockpile is explained rather than silently dropped.

`rf-resources` and `rf-resource` are deleted from `ravenfall-components.css` rather than left
behind, since nothing writes those class names now.

Worth recording as a general point: this is the second thing on that tab that was shown because the
server knew it, without asking whether it was still **true**. "The data exists" is not the same as
"the data means what it used to". Anything derived from the older version of the game deserves that
question, and the answer is not in the database.

### The dashboard chrome, reworked

All reported from use.

- **The top bar is gone**, from all three layouts. It was 3.5rem on every page and held a logout
  link the sidebar already carries under Account, plus the notification bell. The bell floats at
  the top right, which is where it appeared to be anyway. Page titles start at the top of the page
  now instead of under an empty strip.
- **The sidebar stays put when the page scrolls.** Both scoped stylesheets had asked for
  `position: sticky; top: 0` and it had never worked: a flex item stretches to the height of its
  container by default, and a box that already spans the container has nowhere to stick to.
  `align-self: flex-start` is the fix.
- **The content has bottom padding.** There was none, so the last control on a page, the Leave clan
  button for one, ended flush with the bottom edge of the window.
- **The layout rules moved into `ravenfall-dashboard.css`.** `.page { flex-direction: row }` and
  the sidebar width and height were duplicated in `DashboardLayout.razor.css` and
  `AdminLayout.razor.css`, and **missing from `DarkAdminDashboardLayout.razor.css`**, which set
  `flex-direction: column` instead: that layout had been stacking its sidebar on top of the page
  rather than beside it. All three scoped stylesheets are deleted, along with the empty
  `NavMenu.razor.css`.

That last one is worth the note. Two copies of a rule and one silently different third is exactly
what per component stylesheets produce, and it is the argument for the design system in one
example.

### Forms, and two more things site.css was still doing

Reported as the code of conduct editor wasting its space, and the item editor modal being broken.
Both were `site.css`, and one of them was a rule I had left behind myself.

**`.content form { display: flex; flex-flow: column; align-items: center }`.** Written for the
login form on the front page, and it reaches every form in the dashboard and the admin panel. Two
different symptoms from one rule: a field set to `width: 100%` shrinks to its own content and sits
centred in the middle of a wide panel, which is what the code of conduct editor was doing; and a
form taller than its box has its children overflow **both** ends, because that is what centring
does when there is not enough room. It is reset inside `.rf-dash`, with the `rf-toolbar`
`display: contents` rule ordered after it so the search boxes keep working.

This is the third distinct fault caused by that one declaration. The first was the search box that
rendered 256px tall. It is worth going and deleting it from `site.css` once the login page is
converted.

**`form.item-editor-form { position: absolute; left: 25px; right: 25px; top: 50px; bottom: 70px }`.**
This is the one I left behind. It pinned the item editor's form inside the old bespoke modal box.
When that box was replaced with `rf-modal`, which is not a positioned ancestor, the form escaped to
the page and laid itself out 1390px wide across the top of everything. The sections scattered over
the screen were this single rule. Deleting a per page stylesheet is not enough on its own: the same
page's rules can also be sitting in `site.css`, and `.razor.css` files are the only ones the file
name warns you about.

Verified at 1440px and 900px: modal centred and sized to the viewport, three columns then two, every
section inside it, the modal scrolling internally rather than the page.

### Two grid and flex traps, both found the same way

Both were reported as "it looks broken", both turned out to be a sizing rule doing exactly what it
was told, and neither was visible without measuring in a browser.

**`rf-facts` collapsed its value column in a narrow panel.** It was
`minmax(9rem, 14rem) 1fr`. A track with a fixed maximum is grown with the free space *before* any
`fr` track gets a share, so in a container narrower than about 14rem the label column absorbed
everything: measured at 200px wide, the columns resolved to 200px and 0px. Both cells break words
anywhere, so the value then wrapped one character per line, which is why the equipped totals panel
showed `2,626` stacked down five rows. It is `fit-content(14rem) minmax(0, 1fr)` now, which sizes
to the content and does not go hunting for free space. Verified at 250, 400 and 900px.

The totals panel itself moved to a new **`rf-statlist`**, which is flex rather than grid: the label
takes what is left and wraps, the number is sized by its own content and never breaks. That is the
right shape for a label and a number and it cannot recur.

Worth noting for both of these: a media query cannot see a narrow *container* inside a wide
viewport, so the responsive rule at the bottom of `rf-facts` was never going to help.

### The sidebar shrinking, and the sideways scroll

Reported together, and they are one fault with two symptoms.

The sidebar was `width: 250px` on a flex item, and a flex item shrinks below its width by default,
so on any page with a wide table it was squeezed narrower to make room. It is `flex: 0 0 250px`
now.

The sideways scroll is the more interesting half. A flex item's `min-width` is `auto`, which means
it refuses to shrink below the intrinsic width of its contents. One wide table therefore pushed the
whole row wider than the window, and **every `rf-scroll-x` on the page was powerless against it**:
a scroller cannot be narrower than its parent, and the parent was refusing to be narrower than the
table inside it. `.rf-dash .main { min-width: 0 }` lets the column shrink again, at which point the
scrollers do the job they were always there to do.

Measured at 760px with a 736px table: before, the document was 1010px wide in a 760px window;
after, the document is 760px and the table scrolls inside its own 412px container.

Worth remembering, because rule 5 assumes it: `overflow-x: auto` only contains a wide child if
every flex ancestor between it and the viewport can actually shrink.

### Pagination

`rf-pager` is new. The loyalty page rendered every row it had: a streamer with a few years behind
them has a couple of thousand loyalty rows, and both tables on that page were unbounded. Both are
paged at 25 now, and the search resets to the first page rather than leaving you on page seven of
results that no longer exist.

The admin character list hand rolls the same three controls and should move onto `rf-pager` when it
is next touched.

### New components

- **`rf-price`**. A price cell that answers "is that a good price" rather than only stating a
  number: the value, then the comparison under it in small muted type. Built for the marketplace
  and deliberately not scoped to it, since the vendor page and the admin economy tables are the
  same shape.
- **`rf-adminuser__head`** and **`rf-adminuser__avatar`**. An avatar beside a facts list, which is
  the shape both the account panel and the clan panel want.
- **`a.rf-chip`** and **`a.rf-item__name`**. Chips and item names that are links. Neither is gold:
  on an admin table every row would be, and the accent stops meaning anything when it is
  everywhere. Underline on hover is the affordance instead, and both set colour and background
  together per rule 3.

### Three bugs in PlayerInventory

Found from a reported exception on `/inspect`, and all three are older than the conversion work.

- **`GetItemName(InventoryItem i)` read `itemDetailsDialogItem.ItemId`**, the item the details
  dialog happens to be showing, rather than `i.ItemId`, the one being asked about. Every caller
  outside that dialog - the equipment slot labels, the tile titles, the search filter - asks while
  no dialog is open, and `InventoryItem.Name` is only set for renamed items, so the null coalesce
  fell through to a null field for every ordinary item on the page. That is the reported
  `NullReferenceException`.
- **`session.UserId` was read without a null check.** This component renders on `/inspect`, which
  is a public page, so a signed out visitor opening any character's inventory threw.
- **`UpdateItemsToVendor` called `GetMyCoins()` unconditionally**, overwriting the figure loaded a
  moment earlier for the character being looked at. On `/inspect` the coin count shown against
  someone else's inventory was your own.

### Two rules learned the hard way

- **`--rf-ink-faint` is for labels, not for facts.** On `--rf-surface-1` it measures about 3.2:1.
  That is fine for a heading whose shape you already know and not fine for a number you are
  comparing, which is why `rf-price__meta` is `--rf-ink-muted` at about 6.6:1. Worth a pass over
  the other faint text at some point, but that is a system wide decision rather than a page one.
- **A form is a flex item; the input inside it is not.** `site.css` sets
  `.content form { display: flex; flex-flow: column }`, so an `rf-search` inside an `EditForm`
  becomes a flex item of a column and its `flex: 1 1 16rem` basis is applied to its **height**. At
  375px the admin search box rendered 256 pixels tall. `.rf-toolbar > form` is now
  `display: contents`.

### PlayerView

Shared by `/characters`, `/inspect` and the admin panel, so it is the component that forced the
components/chrome split described above. The legacy heading rules are switched off at the
component roots (`.rf-player`, `.rf-skills`, `.rf-clan`) rather than only inside `.rf-dash`,
because two of its three homes are outside the dashboard.

What changed beyond colour:

- The `!join` prose became command chips in the same gold the game uses to highlight a chat
  command, with the alias editor next to them rather than as a bare link in a sentence.
- The tab strip was filled black boxes that grew to fill the width, so a two tab character and a
  four tab character looked like different pages. It is now an underline strip with brass marking
  the selected tab, and it carries `role="tablist"` and `aria-selected`.
- Skill rows are a grid. The old progress bar was a fixed 100px box whose fill was set to
  `{percent}px`, so it only read as a percentage because the box happened to be 100px wide, and it
  would have emitted `43,2px` under a Swedish locale. The fill is now a real percentage formatted
  with invariant culture.
- The training row says the word "Training" as well as carrying the highlight, per rule 4.
- Idle bars are deliberately not gold. Twenty gold bars spends the accent on the rows nobody is
  looking at and stops the one that matters from standing out.
- The skill editor was a fixed white box with no backdrop, which on a dark page read as a
  rendering fault. It is now `.rf-modal` over a scrim.
- On a phone the row folds to two lines rather than dropping columns; at four columns the name
  track collapsed to nothing, and a skill list you cannot read the names in is not worth showing.

`PlayerInventory` (1084 lines) was left alone deliberately. It is the same item table problem as
`/stash`, and the two should be done together so they end up agreeing. `PlayerCustomization` is a
Unity WebGL canvas and has nothing to restyle.

### What the tabs gained

A reskin was not the brief. The rule applied was: if the server already knows something a player
would want, showing it is worth more than styling what was already there. Almost everything below
needed no new data, only joining up fields that were already on the wire.

**Time to level.** `RavenNest.BusinessLogic/Extended/SkillProgress.cs` is new and holds the maths:
experience owed to the next level, experience owed to an arbitrary target level, and time for a
given amount at a given rate. The experience curve was already in `GameMath` and the rate already
arrives with the character state as `ExpPerHour`; nothing had ever divided one by the other. Note
that `PlayerSkill.Experience` counts progress within the current level rather than a running
total, which is why crossing several levels adds the levels in between whole.

`TimeFor` returns null when there is no rate, which is every skill the character is not currently
training. The page says so in words rather than inventing a number.

**Skills tab.**

- Headline stats: combat level, **total level** (never shown before, and a number every RPG player
  compares), exp per hour and rested time.
- A "Right now" panel. The one line status sentence stays, and under it sits the next level block:
  which skill levels next, how much experience is left, and roughly how long that takes.
- When several skills train at once, which is what `!train all` does, the next level block names
  whichever of them is **closest**, not whichever comes first alphabetically. That is the question
  being asked.
- Facts that existed only in game and could not be checked anywhere else: the auto train target
  level with an ETA to reach it, remaining auto joins for raids and dungeons with the lifetime
  count, auto resting, and ferry captain. The auto join counter counts **down** and `int.MaxValue`
  means until turned off, confirmed against `RaidManager.cs` in the game client rather than
  guessed.
- Resources. Coins, wood, ore, fish, wheat, magic and arrows are tracked per character and were
  visible only in game.
- Each skill row now answers "how much is left" (`12.4K to 73`) instead of repeating the bar as a
  percentage. Exact figures are on the tooltip both the bar and the column carry.
- Sort control: in game order, highest level, or closest to levelling. Twenty skills in a fixed
  order is fine when you know what you are looking for and useless when the question is what you
  are about to level.

**Clan tab.** It was a logo, a name, a level and a Leave button.

- Clan level progress with experience to the next clan level. The clan's experience was on the
  wire and only the level was rendered.
- **Clan skills** now appear. They were not rendered anywhere on the website at all, so the only
  way to know the clan's enchanting level was to ask in game.
- Your standing: rank, rank level, join date and days in the clan.
- Member roster with combat levels and ranks, sorted by rank then strength, capped at twelve with
  a control to show the rest. Each entry links to that character's inspect page.
- Leaving now asks for confirmation. It used to happen on the first click, and clan membership is
  not something a player can restore themselves.
- The empty state explains what a clan is for and links to clans and to pending invites, rather
  than being an orange heading saying there is nothing here.

The roster surfaced a bug in the existing system: the dashboard's content anchor rule was reaching
inside character cards and painting every name gold, which is most of the accent budget spent on
the least important thing on the panel. `.rf-char` and `.rf-card` are now excluded from it, which
also fixes the overview page.

### Items: the stash and the inventory

Done together, because they were two implementations of the same screen. That is how they ended up
with different labels for the same category ("Armor" against "Armors"), a different alchemy icon,
and nineteen hand written filter blocks each. They now share three things:

- **`ItemFilterOptions`** - the category rail as one list. Adding a category is one line.
- **`ItemRarity`** - see below.
- **The `rf-item` cell and `rf-tile`** in `ravenfall-components.css`.

#### Rarity

Ravenfall has no rarity field. What it has is `ItemMaterial`, declared in ascending order of
quality, and the site already treated that order as the ranking: sorting a stash by material sorts
on the same index. `ItemRarity` bands that index into the six rarity tokens, which had been defined
since the design system was built with nothing referencing them.

Six bands rather than thirty materials, because the point of colour here is picking the good thing
out of two hundred rows at a glance. Thirty shades is a legend to memorise. Bronze and Iron are
common; the plain run tops out at Atlarus as legendary; every Elder material is mythic, which
matches where the index puts them.

Worth knowing: `ItemTooltip.razor.css` has an older per material colour set keyed on
`[data-tier='5']` and similar, but `GetItemTier` returns material *names*, so all but three of
those selectors have never matched anything. That is dead code, not a competing convention.

#### `/stash`

- Summary stats: items stored, distinct items and **total vendor value**, none of which existed.
- **Search by name.** A stash of a few hundred distinct items had no way to find one short of
  reading the whole table, which is the single most common reason to open the page.
- Rarity coloured names, and **enchanted and soulbound now show**. Both were on every row in the
  data and rendered on none of them, and both change what you can do with the item.
- The transfer column was one button per character, so five characters meant five buttons on every
  row, most of the table width spent on the same word. It is now one Send button opening a dialog.
- **The dialog says whether the character you are sending to can actually use the item.** That is
  a comparison of two numbers the page already held, and it is exactly where the decision is made.
  The magic requirement is satisfied by either magic or healing, matching the in game tooltip.
- Sortable headers are buttons rather than clickable `th`, so sorting is reachable from a keyboard,
  and they carry `aria-sort`.

#### `PlayerInventory`

- **Equipment slots show what is in them.** They were image squares with a slot label, so finding
  out what you had equipped meant clicking all fourteen.
- **Empty slots are named.** An empty slot is free stats you are not wearing, and nothing said
  which ones were empty without counting squares.
- The item grid carries rarity as a bar along the bottom edge of each tile. A full coloured border
  on two hundred squares reads as a grid of borders; one coloured edge reads as items. Tiles also
  have a title, so the grid is no longer anonymous.
- **Search by name**, kept separate from the bulk vendor actions on purpose: "Vendor all" promises
  the category, not whatever is typed in the search box.
- All four dialogs are `rf-modal` with a scrim and click outside to close. The vendor ones say in
  plain words that vendoring cannot be undone.
- **When an item cannot be equipped, it says why.** The Equip button simply was not rendered, which
  reads as a missing feature rather than an answer.

### Legacy cascade neutralised inside `.rf-dash`

`site.css` styles bare headings globally. Two rules were visibly wrong on the dashboard and are
switched off in scope rather than deleted, because the front and admin pages still depend on them:

- `h1:after` injects a decorative `LineBreakWhite.png` block, 28px tall with 30px margin below.
  That was the unexplained gap under page titles.
- `h1/h2/h3 { color: #ff7f00 }`, a legacy orange not in the palette, was tinting panel headings.

When the whole site adopts the system these should be deleted from `site.css`, not overridden.

### Clans and loyalty

#### Who is playing right now

The clan pages had no way of telling a member who plays daily from one who left a year ago, which
is most of what a clan actually wants to know. The answer was one join away: a character is locked
to the session it joined, so the set of live sessions plus each member's lock is enough.

`ClanService.GetMembers` now fills `PlayingOn` on every member, and it builds the live stream map
**once** rather than asking per member. That matters: `GameData.GetSessionByCharacterId` scans
every character in the game, which is fine for one lookup and not fine for two hundred.

It shows up in three places: the roster on a character's clan tab, the member list on `/clan`
(with a filter for it), and a count per rank on the Ranks tab.

#### `/clan`

- **Members who did not found their clan were shown the create a clan form.** `GetClan` only ever
  looked a clan up by its owner. `GetMyClan` falls back to the clan any of your characters belong
  to. This was a straight bug, not a styling issue.
- Clan level progress and **clan skills** now appear here too. The character's clan tab already
  had both, which made the management page the less informative of two views of the same clan.
- Member rows carry combat level, which was in the data and shown on neither clan screen.
- "Joined" was a raw timestamp. Nobody counts back from `2024-03-04 12:33:21`.
- Search, and sorting by playing, then rank, then strength.
- **Removing a member asks first.** It used to happen on the first click.
- The Patreon gate now says what you *can* do (join a clan, check your invites) rather than only
  what you cannot.

#### `/loyalty`

- **The streams you have played on were hidden behind a "click here to show" line.** That list is
  where every point on the page comes from. It is now a normal section.
- **A reward you cannot afford said nothing.** It was dimmed, with a button that silently returned
  when pressed. It now says how many more points you need.
- The redeem dialog shows the cost, what you have, and what is left afterwards, instead of a
  Redeem button that quietly refuses when the arithmetic does not work. Character choice is the
  card picker rather than a bare dropdown of names.
- Reward items carry their rarity colour.
- Both tables sort by points, and the streamer view has a search.
- `FormatTime` was rendering `2.34 hours`.

#### One system-wide fix

Every component in `ravenfall-components.css` sets a width and a padding together, which only
works under `box-sizing: border-box`. That was arriving from Bootstrap, loaded earlier in
`_Host.cshtml`, so the design system silently depended on a third party stylesheet: without it a
`.rf-input` at `width: 100%` overflows its field by 26px and `.rf-modal--wide` is 690px rather
than 640. The components now declare it themselves, scoped to `rf-` class names so it still cannot
reach an unconverted page.

### The standing brief

Restyling alone is not the job. For each page, ask what the server already knows that the page
does not show, and what a player currently has to leave the site or ask someone in chat to find
out. That is where the remaining value is; the tokens only make it look like it belongs.

Things noticed while doing PlayerView that are worth doing but were out of scope:

- **A rate history.** `ExpPerHour` is a snapshot. Storing it over time would turn "about 6 hours"
  into a real projection and let the site show a training graph.
- **Highscore rank per skill.** The highscore page has the data; a rank next to each skill row
  would make the list competitive rather than informational.
- **Item and equipment awareness in the next level block.** The item work has landed, so this is
  now buildable: the requirement comparison already exists in two places, and pointing it the other
  way round answers "what does this level unlock".
- **A "what can I equip" filter** on the stash and the inventory, using the same requirement
  comparison the transfer dialog does per character.
- **Vendor value per category** on the stash, matching what the inventory already shows.

### Next, in priority order

Existing pages before new ones.

1. **`/marketplace`** on the front page. It shares the item vocabulary that now exists, so it is
   much cheaper than it was, and it is where the plan and commit work changes behaviour anyway.
2. **`AdminPlayerInventory`** and `AdminCharactersView`, the last screens still on the `site.css`
   item styling.
3. **Start deleting from `site.css`.** Enough now converts that the item table, stats row and tab
   link rules are close to dead. Check each against the admin pages before removing it.

### New pages wanted, not started

- **Marketplace, as a dashboard page.** The nav entry was pulled rather than left pointing at the
  front page version, which threw you out of the dashboard. The front page nav still carries it.
- **Vendor page.** Buy from stock other players vendored. Depends on the vendor buy feature in
  [vendor-buy-and-command-config.md](vendor-buy-and-command-config.md), which depends on the plan
  and commit work in [trade-plan-commit-design.md](trade-plan-commit-design.md).
- **Map**, showing where each character is.
- **Bestiary.** Still blocked. `NPC`, `NPCItemDrop` and `NPCSpawn` exist as entity sets with the
  right shape, but **the tables are empty and nothing seeds them**, so the original reasoning
  holds: the game client is the only thing that knows its own enemy and drop tables. See
  [feature-opportunities.md](feature-opportunities.md).
- **Quests.** Route and nav slot deliberately kept. Design not settled; intended to be a major
  part of the next version.

### The town page

`/town` is new and is the first page here built from a design document rather than from an existing
screen. Details are in [town-page-design.md](town-page-design.md); three things generalise.

**A grid per row beats one grid for every row.** The bonus summary was a single grid with a column
per field, which is tidier to write and reflows into nonsense the moment a breakpoint changes the
column count: a bar told to span the full width cannot fit its own row, so it takes the next one
and drags every following cell out of position. `grid-template-areas` on a per row grid cannot do
that. This is the same lesson as `rf-facts` becoming `rf-statlist`, arrived at from the other
direction.

**Bars that are compared with each other need identical tracks.** Content sized side columns gave
seven bars seven different widths, between 727 and 836 pixels. Each fill was a correct percentage
of its own bar and the set was still unreadable, because the eye compares lengths. Both side tracks
are fixed now.

**`rf-badge--quiet`** is new. A plain badge is drawn in full ink, so on the plot grid the badge
saying "nothing built" was louder than the one saying "counting". Muted rather than faint, because
a state word is a fact and not a label.

The standing brief paid again, and mostly off maths rather than off fields: the town levelling rate
was one linear function away from a real time estimate, and it turns out **a town levels at the
same speed however many viewers are playing**, because the processor passes a fixed player count.
Neither the game nor the site had ever said so.

### TV removal

Page and nav entry deleted. The backend was left in place and is a separate decision:

- `src/RavenNest.BusinessLogic/Tv/` (manager, prompt builder, prompt generator, request model)
- `src/RavenNest.Models/Tv/Episode.cs`
- `src/RavenNest.Blazor/Controllers/TvController.cs`
- registrations in `Startup.cs`, plus references in `SessionManager.cs` and `JsonRepository.cs`

Removing those touches live server code, so it wants its own change with a build and a smoke test
rather than being folded into a styling pass.
