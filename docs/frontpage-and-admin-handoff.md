# Handoff: front page and admin panel

The dashboard is done. Every page under `Pages/Dashboard/` is on the design system with no per page
stylesheets left. The two remaining bodies of UI are the front page and the admin panel, and this
is the brief for both.

Written at the end of a session rather than started badly at the end of one.

## The constraint that shapes the front page work

**Keep the hero panels and the large images.** They are the only place the site looks like a game
before you log in, and they are doing a job the design system cannot do with tokens: they show the
world. The job is not to replace them.

The job is everything around them: the type, the spacing, the buttons, the cards, the tables, the
colour drift, and the parts that are visibly older than the rest.

## Why the front page is now cheap

`ravenfall-components.css` is deliberately **not** scoped under `.rf-dash`. That split was made when
`PlayerView` turned out to render on `/inspect`, which is a front page. Every `rf-` component
therefore already works on the front page today, with no new plumbing:

`rf-panel`, `rf-btn`, `rf-stat`, `rf-table`, `rf-item`, `rf-tile`, `rf-chip`, `rf-badge`,
`rf-modal`, `rf-facts`, `rf-toolbar`, `rf-filters`, `rf-tabs`, `rf-charpick`, the rarity bands.

`ravenfall-dashboard.css` is the part that must stay behind `.rf-dash`, because it overrides bare
elements and `site.css` class names. **Do not put front page rules in it.**

### The one thing to watch

`site.css` styles bare headings globally:

- `h1::after` injects a decorative `LineBreakWhite.png`, 28px tall with 30px of margin.
- `h1`, `h2`, `h3` are `#ff7f00`, a legacy orange not in the palette.

On the dashboard both are switched off inside `.rf-dash`. On the front page they are **load bearing**
until each page is converted, so switch them off per component or per page, not globally, exactly
as `.rf-player`, `.rf-skills` and `.rf-clan` already do.

## Suggested order for the front page

1. ~~**The shared front layout**: header, nav, footer.~~ Done, see below.
2. ~~**`/marketplace`**.~~ Done, see below.
3. **`/highscore`**. A table, and `rf-table` exists. Consider adding clan and total level columns
   while there.
4. **`/inspect`**. Already half converted, since `PlayerView` renders on it. Only the page chrome
   around it is old.
5. **The landing page itself**, keeping the hero panels. Mostly type, spacing and buttons.
6. **`/login`, `/towns`, `/calc`, the static pages.** Note that `/about`, `/how-to-play`, `/team`
   and `/credits` have no hero section, and the header is positioned over the hero, so their one
   line of content currently renders underneath the header. They are stubs, so nobody has noticed,
   but converting them means giving them either a hero or a top spacer.

## Admin panel

Order and missing features are in
[admin-and-streamer-features.md](admin-and-streamer-features.md). Short version:

1. `UserAndCharacterManagement`, `UserSearch`, `PlayerManagement` - the daily tools.
2. `ItemManagement`, `ItemDropManagement`.
3. `Economy/*` - four pages, same table shape as the stash.
4. `SessionManagement`, `ServerMangement`, `RavenbotLogs`, `AdminTools`.

The admin pages are dark and table heavy, which is exactly what the component set was built for, so
these should go faster than the dashboard did.

## After the conversion: start deleting

Deleting has started, out of order, because reported layout faults forced it: the mobile hero
margins and the dead header rules are gone, and `h1::after` plus the `#ff7f00` headings are
overridden for the whole front page rather than page by page. `site.css` is 3423 lines.

The rest, in this order, checking each against what still uses it:

1. The item table rules (`.items-list`, `.item-row`, `.item`).
2. The stats rows (`.stats-row`, `.stats-label`, `.stats-progress`).
3. `button.tab-link`, `.btn-new`, `.btn-action`.
4. The `h1::after` line break image and the `#ff7f00` heading colour, which is the point at which
   the `.rf-dash` neutralising rules in `ravenfall-dashboard.css` can also go.

## Method that has been working

Worth repeating because it is most of why this went well:

1. **Before styling a page, ask what the server already knows that the page does not show.** Almost
   every good thing this week came from that question, not from the CSS.
2. **A model being referenced does not mean the feature exists. Check what writes rows.** This
   caught `CharacterAchievement`, and missing it is how I got the bestiary wrong.
3. **Check the admin pages before declaring anything unused.** That is how I got the economy
   aggregates wrong.
4. **Verify in a browser against a static harness** that loads the real stylesheets, at a desktop
   width and at 375px. It caught three real layout faults in one page.
5. **Every value from a token, every space off the 4px scale, colour never the only carrier of
   meaning.**

## Done: the shared front layout

`ravenfall-front.css` is new, and is the front page counterpart of `ravenfall-dashboard.css`:
everything in it is scoped under `.rf-front`, which `MainLayout` now puts on its root element. The
design system is four files rather than three. It is loaded from `_Host.cshtml` after the other
three.

Nothing in it switches off `h1::after` or the `#ff7f00` headings for `.rf-front` as a whole. That
trap is still live and the file says so at the top. Each converted page turns them off for itself.

What changed in the header beyond colour:

- **Two rows became one.** SUPPORT, COMMUNITY and the Discord social icon were three links to the
  same Discord invite, and FANDOM had a row of its own for one link. The wiki moved into the Game
  menu, and Highscore, Marketplace and Streams came out of it, since those are what a visitor
  comes for and all three were a hover deep.
- **The Game menu is a `<details>` element.** It was a hover only div with a hardcoded 150px width
  and 334px height, so it could not be opened from a phone or a keyboard, and a ninth entry would
  have overflowed it.
- **There is a phone layout.** There was none. The logo alone is 150px and the links were a flex
  row that never wrapped, so at 375px the header ran off the side and took the document with it.

  The breakpoint was first set at 68rem, which is where the row measured out at full size: 1101px
  with the admin link, 1035 without. That was reported back as a hamburger appearing far too early,
  and it was right. A hamburger at 1090px leaves the bar looking empty on a normal laptop. The row
  now gets **smaller before it gets replaced**: between 56rem and 75rem the links tighten and the
  four social icons drop out, since they are decoration next to the navigation and are duplicated
  verbatim in the footer. That is about 190px, so the full nav now survives down to 900px rather
  than 1090px. Measured at 900px with every link an administrator sees: single row, 20px to spare,
  no overflow.
- **A scrim behind the header.** The nav text sat directly on the hero photograph, so legibility
  depended on which image happened to be there.
- **`width: 100vw` on the content and the hero is now `100%`.** 100vw counts the vertical
  scrollbar, so every front page was a few pixels wider than its own viewport and scrolled
  sideways. That is rule 5, broken globally, and it had been there for years.

The footer was a logo, five icons and a copyright line marked up as an `h3` so it would pick up a
separator image. It now carries four columns of navigation, which is what someone who reaches the
bottom of a page is looking for. `footer:before` painted `FooterSplit.png`, a grunge fade designed
for a white page, 400px above the footer and overlapping whatever was there; against the near black
sections it has been sitting on it did nothing at all. Switched off in scope, replaced by the same
gold hairline the page headers use.

`MainLayout.razor.css` was deleted rather than rewritten, which is the target state.

## Done: `/marketplace`

Converted, per page stylesheet deleted. It reuses `rf-table`, `rf-item`, `rf-toolbar`,
`rf-filters`, `rf-modal` and the rarity bands with no new components except `rf-price`.

What it gained, all of it from data the page already had loaded:

- **Whether the price is any good.** Every row now says what the asking price is as a multiple of
  what a vendor pays for the item, and marks the cheapest listing of each item with a count of how
  many there are. The vendor sort already existed in the code behind; the column had been commented
  out of the markup. The cheapest comparison keys on the item **and** its enchantment, because an
  enchanted sword is a different product and is priced like one.
- **What the whole lot costs.** Amount times price per item was never shown, so working out what a
  listing came to meant multiplying two columns by hand.
- **Search by name.** Nineteen category buttons and no name search, on the page whose entire
  purpose is finding one particular thing.
- **Enchanted items say so.** The field was on every row and rendered on none of them.
- **Summary stats**: listings, distinct items, coins asked.
- **Expiry in words**, and `entry.Expires.Value` is no longer dereferenced without a null check.
- **Cancelling asks first**, and the admin "cancel all expired" button only appears when something
  has actually expired, with the count in the label.
- Filtering is client side now. The page already fetched the whole board on load, category clicks
  were re-fetching it filtered, and search plus category together is not something the server side
  filter can express.

## Done: the admin panel, partly

**`AdminLayout` and `DarkAdminDashboardLayout` now carry `.rf-dash`.** The admin sidebar is
literally the same `NavMenu` component the dashboard uses, so one class gives all eighteen admin
pages the iron sidebar, the brass active marker, the top bar and the neutralised heading rules at
once. `DarkAdminDashboardLayout` also had an inline `<style>` block setting a blue logout link and
a green notification count, which is two more of the 253 one off colours; both are tokens now.

Three pages converted, the daily tools:

- **`UserAndCharacterManagement`** (`/admin/user/{id}`). Its floating control block was rendered
  outside the null check above it, so any id that did not resolve threw a
  `NullReferenceException`; the page now says there is no such user. The block was also a fixed
  panel held at 40% opacity until hovered, and that is where Ban lived: a destructive account level
  action that only appeared when the pointer happened to pass over it, with no confirmation. It is
  now a normal panel with a confirm dialog. The character view tabs became a real `rf-tabs` strip
  with `role="tablist"`.
- **`AdminUserView`**. Gained the account's **connections** (it showed only the obsolete `UserId`
  property, so an account connected through anything but Twitch looked like it had no identity, and
  this is the first thing you want when a username change is being investigated), the **created
  date and account age**, a **stash count**, and the **actual account status**, of which there are
  three and the view could say two. The "hidden in highscore" control was
  `<input type="checkbox" value="@...">`, which does not set a checkbox: it rendered unchecked
  whatever the stored value was, so the one thing it existed to show was the one thing it could not.
- **`UserSearch`**. The result count said "@totalCount users" where totalCount was the length of
  the page just fetched, so it read "25 users" for any search matching more than 25. Rows now carry
  each character's combat level and which stream it is locked to, both already on the wire, and the
  name is the link rather than the guid.
- **`PlayerManagement`**. `LoadIndicator` was inside the `tbody`, which browsers hoist out of the
  table. Next paged past the end with no upper bound. `pageCount` was `floor(total / size) + 1`,
  which claims one page too many whenever it divides evenly. Two calls to `Equals` on a name that
  can be null. Four icon only buttons, two of them the same red for "kick from the session" and
  "delete the character for ever"; they are words now and both of those confirm. Rows gained combat
  level and the character's **alias**, which is what a viewer types after `!join` and is what
  support questions are usually about.

Two more since, both small and both the first thing an administrator sees:

- **`/admin`**, the panel's own front door. It drew two charts and stated no numbers, so answering
  "how many signed up this month" meant reading a line off an axis. It now leads with the total for
  the selected period, the best day in it and the busiest hour of day, all three summed from the
  series the chart was already plotting. Its chart line is brass rather than the default teal.
  Fixed while there: `GetChartData` dereferenced `record.Key` on the line before its own null
  check, so any hour of the day with no signups threw instead of plotting a zero.
- **`/admin/others`**. Two tools in a bulleted list. The Generate button only appeared once a user
  had been picked, which reads as a missing feature rather than as an answer; it is always there
  now and says what it is waiting for.

Since then: **`Economy/*`** (three pages plus a new shared `EconomyNav`, with four crashes and a
counting bug fixed), **`RavenbotLogs`**, **`UsersManagement`**, **`CodeOfConduct`** and
**`ServerMangement`**. What each turned up is in the admin document.

And **`ItemManagement`**, **`ItemDropManagement`**, **`SessionManagement`**, and the two
**`RavenbotLogDisplay`** components.

**The admin panel is done.** There is no `.razor.css` file left anywhere under `Pages/Admin/`, and
none under `Shared/` except the two loading indicators. What each page turned up is recorded in the
admin document; the short version is that every single one contained at least one unguarded
dereference, and several contained the same four defects as each other. They have
the right chrome from `.rf-dash` on the layout, so they are dark and consistent at the edges, but
the content of each is still the old page.

Worth knowing before starting one: every admin page converted so far has contained at least one
unguarded dereference of something that can legitimately be null, and usually an empty `catch`
hiding it. Read for those first; they are more valuable than the CSS.

## The front page baseline, and a reversal

Reported from use: too much white space between the hero and the content on many pages, and
heading text that looked like a dark colour on a dark background.

Both were real, and fixing them meant reversing the caution recorded further up this document. The
advice was to leave `h1::after` and the `#ff7f00` headings alone until each page converted, on the
grounds that unconverted pages were drawn against them. That was wrong. They are not load bearing,
they are the fault, and waiting for each page's own conversion left the whole site broken in the
meantime. `ravenfall-front.css` now carries a baseline for every front page, converted or not:

- **Headings are `--rf-ink`** rather than the legacy orange, and `h1::after` is the same gold
  hairline the converted page headers use rather than a 28px image with 30px of margin. That is
  about 50 vertical pixels given back under every heading on the site.
- **The dark on dark text is fixed**: `.skill-selector` on the highscore and towns pages,
  `.usage-example` and the command aliases on the command list, the note on the password page, and
  the paragraph in the Twitch panel on the landing page. All are `#333`, `#666` or `#303030` in
  their own stylesheets, left over from when the front page had a white background.

### The white space had three separate causes

1. **The hero was a fixed height set in eight places.** `site.css` says 535px for the landing page
   and seven subpages override it to 335px in their own stylesheets. The four that never got the
   override, `/login`, `/calc`, `/cookies` and `/password`, carried a 535px photograph over a
   single line of text. `/marketplace` joined them when its stylesheet was deleted during its
   conversion, which was a regression this fixes. It is now one rule, the height comes from the
   content, and no page needs an override.
2. **The top padding was 150px**, tuned for the old two row header. The header is 62px now, so a
   third of that padding was empty by definition.
3. **Four mobile rules that cancelled each other out.** `.top-section` had `margin-top: -140px` at
   641px and `-180px !important` at 450px, each paired with a `+140px` or `+180px` on
   `.front-page .content` to put it back, plus `.hero-text { margin-top: 50px !important }` on top.
   The net effect of all five was to shove the hero text 50px down into the torn edge graphic. All
   of them are deleted from `site.css`, along with the `.top-row.corner`, `.top-social` and
   `.main-nav` rules that belonged to the header that no longer exists.

The landing page hero is now the only one with a fixed larger height, and it says so in its markup
(`top-section--tall`) rather than through a stylesheet nobody can find. Measured at 1280px and
375px: the hero is 320px for a subpage and 512px for the landing page, the hero text clears the
166px torn edge graphic by 16 to 24 pixels, and there is no gap at all between hero and content.

## One fault the browser harness caught

Worth recording because it would have shipped and because it is invisible in source.

`site.css` sets `.content form { display: flex; flex-flow: column }`. Any `rf-search` inside an
`EditForm` is therefore a flex item of a **column**, and `flex: 1 1 16rem` becomes its **height**:
measured at 375px the admin search box rendered 256 pixels tall. `.rf-toolbar > form` is now
`display: contents`, which drops the form's own box so the input is a flex item of the toolbar row
again, and the form still submits.

Also fixed while there: `a.rf-chip` was picking up the dashboard's gold anchor rule, so every
character chip in a search result came out gold. `.rf-chip` joins the exclusion list.

## Still unverified

`/clan`, `/loyalty`, `/clan-invites`, `/notifications`, `/patreon` are still verified by build and
by the component styles being proven elsewhere, and still have not had a browser pass of their own.
They were not done this session either; the conversion work was judged the better use of the time
and the harness runs done here covered the components those pages share. The `display: contents`
fault above is a reason to keep doing them: none of these pages puts a search box in an `EditForm`,
so that particular one does not reach them, but it is the kind of thing only a browser finds.

The header, the footer, `/marketplace` and the three admin pages were each checked in a static
harness that loads the real stylesheets, at desktop width and at 375px, plus 1000px and 1100px for
the header breakpoint. Note that in this environment the browser pane returned geometry and
computed styles but could not produce a screenshot, so these were verified by measurement rather
than by eye. Worth a look with human eyes on the header in particular, since it is the most visible
change.
