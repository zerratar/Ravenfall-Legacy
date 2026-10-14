# Migrating the UI to UI Toolkit

A long running plan, done one screen at a time, with a shippable game at every point. uGUI and UI
Toolkit coexist in the same project, so there is never a big bang cutover.

## Why, in order of how much it actually matters

**1. Consistency stops being a discipline problem.** The UI drifted across years of solo work
because nothing enforced a shared style. uGUI can be made consistent with prefab discipline, but
nothing makes you keep it. USS does: one stylesheet, one set of tokens, and consistency becomes the
default rather than something to remember. Three duplicate SDF assets of the same font sitting in
the project (`FOT-NewRodin Pro M SDF 1/2/3`) is that drift made visible.

**2. The UI leaves the scene file.** This one is easy to miss and worth a lot here. There are no UI
prefabs; the entire interface is built into the scene hierarchy, which is why `MainWorld.unity`
holds 971 RectTransforms and is 125 MB. Every UI tweak today means editing a 125 MB binary-ish YAML
file that cannot be reviewed, cannot be merged, and had to be moved to Git LFS to be pushable at
all. UI Toolkit layouts live in `.uxml` and `.uss` text files, outside the scene. Migrating a
screen physically removes it from `MainWorld.unity`, and the result diffs cleanly.

**3. Room to grow.** Pets, NPCs, quests and achievements all need UI. Adding those to the current
setup means more hand placed hierarchy in the same scene.

**4. Performance.** Real, but the smallest reason. Do not lead with it.

## What is actually in scope

Corrected from an earlier estimate of "143 files with a Canvas": almost all of those are
third party demo scenes, mostly SuperScrollView. The real UI is:

| Scene | Canvases | RectTransforms |
|---|---|---|
| `MainWorld.unity` | 2 | 971 |
| `Overlay.unity` | 1 | 35 |
| `CodeOfConduct.unity` | 1 | 18 |
| `Update.unity` | 1 | 12 |

Around 1,036 UI objects in four scenes, driven by 34 scripts. That is a real project, but it is
roughly ten screens, not 143.

### Screens

- Player details (the observed player panel, the most seen UI on stream)
- Player list and its rows
- Settings, with graphics, sounds and UI tabs
- Main menu
- Island details
- Player inventory
- Notifications: arena, raid, stream raid
- **Game restore overlay** - the full screen "Game is being restored / Downloading characters
  data" cover shown while `GameCache.IsAwaitingGameRestore` is set. It sits over everything, so it
  is both high visibility and a good early candidate: it has almost no interaction, only text.
- Stat and skill observers
- Code of Conduct screen (own scene)
- Update screen (own scene)
- Overlay scene UI

### Not migrating

World space UI stays exactly as it is. UI Toolkit's runtime is screen space; world space is not a
real answer there.

- `NameTag` and `NameTagManager`
- `HealthBar` and `HealthBarManager`
- `DamageCounter` and `DamageCounterManager`
- `MessageBubbleManager`
- DamageNumbersPro, an entire TMP based asset

## Notifications carry text baked into images, which blocks localization

The arena, raid, dungeon and duel notifications do not draw their headline text. They toggle
GameObjects holding pre-rendered images with the English wording painted in, and only the dynamic
parts (names, timers, levels) are real text.

| Image | Size |
|---|---|
| `CongratulationsForWinning.png` | 997x76 |
| `ArenaStart.png` | 866x99 |
| `ComeFight.png` | 653x59 |
| `ArenaIsAboutToStart.png` | 789x196 |
| `ArenaIsNowActive.png` | 684x196 |
| `ArenaDraw.png` | 841x314 |
| `RaidBoss.png` | 980x307 |

Those aspect ratios are the tell: they are lines of text, not artwork.

This matters beyond consistency. **Text inside a PNG cannot be translated.** A Spanish speaking
streamer currently gets English banners no matter what, because the words are pixels. The game
already has `Localization.cs` with 261 string constants for bot messages, so the infrastructure
exists; the UI is what was left behind.

Each image also had to be redrawn by hand whenever the style changed, which is a large part of why
these screens drifted apart from each other over the years.

Converting them to real text in UXML therefore does three things at once: it makes the
notifications consistent, it removes seven hand maintained assets, and it is the prerequisite for
the localization pass later. That is why notifications rank above Player Details despite being less
visible, even though Player Details is the more watched panel.

## Rule: never disable a Canvas GameObject, disable the Canvas component

The project's convention is to put UI scripts **on the Canvas object and on its children**, because
that makes them easy to find in the hierarchy. So switching off a Canvas GameObject to hide the old
UI also stops those scripts from running, and it fails completely silently: nothing executes, so
there is no error, just a screen that never populates.

This already bit both migrated screens. `GameUpdater` and `CodeOfConductController` are components
on the Canvas object itself, so disabling it meant `Awake` never ran and the new UI sat empty while
looking correctly styled.

**Hide old UI by disabling the Canvas component, and the GraphicRaycaster with it** so the hidden
hierarchy cannot swallow clicks. Leave the GameObject active.

Run `tools/canvas-script-preflight.py <scene>` before touching a screen. It reads the scene file
directly, needs no editor, and lists every one of our scripts on or under each Canvas. Current
state:

| Scene | Canvas | Our scripts |
|---|---|---|
| `Update` | Canvas | 1 on the canvas itself (`GameUpdater`), 1 on a child |
| `CodeOfConduct` | Canvas | 1 on the canvas itself (`CodeOfConductController`) |
| `MainWorld` | Canvas2D | 52 on children |
| `MainWorld` | Canvas3D | 25 on children, mostly `LookAt`, this is the world space canvas |

MainWorld carries 77 of our scripts under canvases. That is the real reason its screens have to be
migrated one panel at a time rather than by switching the canvas off.

## A second silent failure worth knowing about

Unity runs **every `Awake` before any `OnEnable`**, and `UIDocument` builds its visual tree in
`OnEnable`. A controller that writes text from `Awake` is therefore writing into a tree that does
not exist yet. Nothing throws; the UXML placeholder text simply stays on screen.

Both views handle this by holding values until the tree exists and flushing them once it does. Any
new screen view should do the same rather than assuming the tree is ready.

## The design constraint that should drive every decision

**This UI is watched through a video encoder by an audience, not just operated by a player.**

Twitch at 1080p is around 6000 kbps, and a large share of viewers watch at 720p or lower, often on
a phone, while also reading chat. H.264 at that bitrate destroys thin strokes, low contrast pairs,
small text, subtle gradients and fine outlines.

That has a direct consequence for the existing direction. The project carries 18 weights of
FOT-Rodin and FOT-NewRodin Pro, the NieR:Automata typeface family. NieR's look is thin type, low
contrast, beige on beige. It is beautiful at native resolution on a monitor and turns to mush
through a stream encoder.

The useful part: the bold weights of the same family are already in the project
(`FOT-NewRodin Pro B`, `DB`, `EB`, `UB`). Keeping the family and dropping the Light weights
preserves the identity that was wanted while surviving compression. That is continuity, not a
reset.

Rules of thumb that follow:

- High contrast, solid fills rather than gradients
- Heavier weights, larger minimum sizes than a desktop app would need
- Fewer things on screen at once; a viewer glances, they do not study
- Anything that must be readable on a phone at 720p gets tested at that size, not at 4K

## Order of work

**Step 0, before any migration: the design system.** Migrating an inconsistent UI to UI Toolkit
produces an inconsistent UI in USS. Define the tokens first: colour, type scale, spacing, panel
chrome, and the small set of shared controls. Everything after this hangs off that file.

Then, smallest and most isolated first, so the cost per screen is learned somewhere cheap:

1. **`Update.unity`** - 12 RectTransforms, its own scene, seen rarely. The ideal spike.
2. **`CodeOfConduct.unity`** - 18, also isolated.
3. **`Overlay.unity`** - 35, self contained, and it is what viewers see.
4. **Notifications** - small, self contained, high visibility on stream.
5. **Game restore overlay** - text only, no interaction, but covers the whole screen so it is worth
   getting right early.
6. **Player details** - the most seen panel, and the one that benefits most.
7. **Player list and rows** - depends on virtualised lists; UI Toolkit's `ListView` replaces
   SuperScrollView here.
8. **Settings and its tabs** - the largest script at 522 lines, but low traffic and low risk.
9. **Island details, inventory** - last.

Each screen: build it in `.uxml` plus shared `.uss`, port the driving script to query the visual
tree, delete the old hierarchy from the scene, play test, commit. One screen per commit so any of
it can be reverted alone.

## Notes on the work itself

- Do not invest further in optimising uGUI panels that are on this list. `PlayerDetails` was
  already given a refresh throttle, which was worth it because the migration is months out, but
  going deeper there would be wasted.
- Every screen removed shrinks `MainWorld.unity`. Track the size; it is a good progress metric and
  it directly reduces the LFS problem.
- `SuperScrollView` can eventually be dropped once lists move to `ListView`, which removes a large
  third party dependency.
- Keep a screenshot of each screen before migrating. Consistency is the goal, but changes should be
  deliberate rather than accidental.

---

## Window framework and player list

### DraggableWindow

`Assets/Scripts/UI/Toolkit/DraggableWindow.cs`. Attach it to any toolkit element with a drag
handle and an id; it persists the position and keeps the window on screen.

It replaces `Dragscript`, which had three faults that together explain the windows people kept
losing:

- **It clamped only while dragging.** A saved position was applied on load with no clamping, so a
  window near the right edge of a 2560 wide screen was gone after relaunching at 1280, with no way
  back short of clearing preferences.
- **Nothing reacted to the game window resizing.** Same outcome without needing a restart. The new
  one re-clamps on every `GeometryChangedEvent` of the container, which is the event the old system
  had no equivalent of.
- **Reset mixed coordinate spaces.** It restored `transform.position` (world) but saved through keys
  read back as `localPosition`, so resetting could move a window somewhere new on the next launch
  rather than back to its default.

Positions are stored only as the top left corner in panel pixels. A window is allowed to hang off
an edge but never by more than `MinVisible` (64px), and never above the top, so the title bar you
grab it by is always reachable. That is the invariant: whatever the preferences file says, the
window cannot be unreachable.

`GameMenuHandler.ResetUIPositions` calls both the legacy `Dragscript.ResetPosition` and
`DraggableWindow.ResetAllPositions`, so the existing settings menu button covers both systems
during the migration.

### Player list

`Assets/Scripts/UI/Toolkit/PlayerListWindow.cs`, `Assets/UI/Screens/PlayerListWindow.uxml`,
`Assets/UI/Styles/ravenfall-hud.uss`.

Why the old one made streams stutter, measured from the code rather than guessed:

- Rows were pooled to the visible count, then **every pooled row ran a full refresh every frame**.
- Its `SetText` guard compared the finished string, so it avoided the text mesh rebuild but had
  already paid for the concatenation and a `Trim` allocation to find out.
- The tracked collection used `Contains` on add, `FirstOrDefault` on remove, and `RemoveAt(0)` on
  every scroll step. All linear, so a thousand player stream paid a thousand element move per row of
  scrolling, on top of the per frame row work.

Three changes address that. Rows exist only while on screen, because `ListView` is virtualised.
Rows refresh on an interval rather than per frame, and only the rows that exist. And each row caches
the last value it displayed, so a string is built only when the number behind it actually changed.

The auto scrolling ticker is kept, because it is how every viewer eventually sees their own row, but
it now advances the scroll offset instead of rotating the data. That removes the `RemoveAt(0)`
entirely. It also pauses while the pointer is over the list, which the old one could not do.

`PlayerList` keeps its public surface and forwards to the toolkit window when the
`uiToolkitWindow` field is assigned, falling back to the legacy rows when it is not. Note that
`Hide()` no longer deactivates the GameObject in toolkit mode: this component is the entry point the
rest of the game calls, so deactivating it would stop the forwarding along with the view.

### Still to do

- **Scene wiring.** A GameObject with a `UIDocument` pointing at `PlayerListWindow.uxml` and
  `RavenfallPanelSettings`, with `PlayerListWindow` on it, then assign it to `PlayerList.uiToolkitWindow`.
  Nothing is wired yet, so the legacy list is still what runs.
- **Nothing has been compiled or play tested.** The editor held the assembly lock during this work.
- ~~The experience progress calculation needs checking.~~ **Resolved, and it was already correct.**
  `SkillStat.Experience` counts within the current level and resets on level up with overflow rolled
  forward. `ExperienceForLevel(n)` returns `ExperienceArray[n - 2]`, the amount needed to go from
  `n-1` to `n` rather than a running total, so for a level L skill `ExperienceForLevel(L + 1)` is the
  size of the level currently being worked through and the division is a true 0..1 ratio. The
  cumulative form is the obsolete `OldExperienceForLevel`; totalling across levels overflowed and
  capped max level, which is why it resets per level now. `PlayerDetails` can use the same
  calculation with confidence.
- `PlayerDetails` is next, and should reuse `DraggableWindow` and the same row and bar classes.

### Dragscript repaired rather than left to rot

`Dragscript` still drives three windows: the PlayerDetails prefab
(`Assets/Prefabs/ObservedPlayerDetails.prefab`), `ObservedIslandDetails` and `ObservedEnemyDetails`.
Those keep using it until each is ported, so it was repaired in place rather than left broken behind
the new system. Four faults, which together explain "draggable on some but lots of bugs":

1. **Clamped only while dragging.** A saved position was applied on load untouched, so a window near
   the right edge of a wide screen was off screen after relaunching on a narrow one. Now clamped on
   load too.
2. **No reaction to the game window resizing.** Same outcome without a restart. The canvas rect is
   now polled and the position re-clamped when it changes.
3. **The clamp read `sizeDelta` from the canvas.** For a stretched canvas that is `(0,0)`, so the
   bounds collapsed; with min above max `Mathf.Clamp` returns the max, meaning the window snapped to
   a fixed wrong spot instead of staying where it was dropped. Now reads `rect.size`.
4. **Drag was not scale aware.** The raw screen pixel delta was applied to a local position, so on
   any resolution where the CanvasScaler factor was not exactly 1 the window drifted away from the
   cursor, worse the further you dragged. The original carried unused `scaleX`/`scaleY` fields, so
   this was known about and never wired up. Now divided by `canvas.scaleFactor`.

`ResetPosition` also deletes its saved keys rather than overwriting them with the current default, so
a later change to the scene's layout is not permanently overridden by a stale saved value. It used to
restore a world position through keys read back as local, which could move a window somewhere new
rather than back.

### Verification so far

Driven through the Unity CLI against the live editor:

- Scripts compile: `recompile_status` reports `completed, failed:false, errors:[]`.
- Scene wiring is saved and resolves: the `UI Toolkit Player List` object exists in MainWorld with
  its UIDocument, PanelSettings and UXML assigned at sorting order 10, and
  `PlayerList.uiToolkitWindow` points at it as a prefab override with a real object reference.
- The visual tree resolves structurally: `playerlist-window`, `playerlist-header`, `playerlist-view`
  and `playerlist-count` are all found, all three stylesheets attach, and `fixed-item-height` came
  through from the UXML. Those are the failures that happen silently, so they are the ones worth
  checking this way.
- No USS or UXML import errors. `text-shadow`, `translate`, `transition`, `cursor: pan`,
  `text-overflow` and the `.unity-scroller` overrides all parse on 6000.7.0a4.

Not yet verified, and it needs Play mode: geometry, dragging, position persistence, and whether rows
actually render. MonoBehaviour callbacks do not run in edit mode, so the view never binds there; a
probe confirmed `itemsSource` is null and resolved sizes are NaN outside Play mode. Entering Play
mode starts a server session, so that is a decision for the person at the keyboard.

### Player list, second pass (after the first play test)

Feedback from the first play test: hover looked bad, no health bar or experience percentage, and
the window could be dragged but not resized.

- **Hover.** The grey wash was Unity's default ListView hover style, never overridden. The
  override selectors are now more specific than the theme's own (`.rf-playerlist
  .rf-player-row.unity-collection-view__item:hover`), so they win whichever stylesheet the panel
  resolves first. The default "List is empty" label is hidden too, since it would show on stream.
- **Row.** Name in the player's own Twitch colour, lifted toward white when its relative luminance
  is under 0.45 so dark blues and purples stay readable after stream compression. Rested time as a
  pill. Full skill name with level, experience percentage, and xp/h aligned right. A health bar
  (brightens below a third) above the experience bar. Combat level in its own captioned column. The
  streamer's own row gets a brass edge and a warm wash.
- **Percent text uses the invariant culture.** The game runs on Swedish and other locales that
  would print `42,1%` next to English text.
- **Resize.** `DraggableWindow.EnableResize` takes a grip element and a minimum size. Size is saved
  per window id with the position, and clamped so the window cannot grow past the container edges.
- **Collapse.** A chevron button in the header folds the window to its header, remembered per
  window. Rows are not refreshed or auto scrolled while collapsed.
- **Reset** now clears saved size and collapsed state as well as position.

Two bugs in the first version, found while doing this:

- The resize re-clamp listened for geometry changes on the window. A window with a fixed pixel
  size and position does not change geometry when the game window resizes, only its container
  does, so the clamp could never fire. The container is now watched as well.
- A button inside a drag handle would never receive its click: the handle captured the pointer on
  the press as it bubbled up. Presses on buttons, toggles and text fields inside the handle are now
  ignored by the drag.

### Player list, third pass (after the second play test)

- **Drag and resize did nothing until the menu had been opened.** The old list's ScrollRect
  `Playerlist/Viewport` is an Image at alpha 0 with raycasting on, roughly 560 by 1070 down the left
  of the screen. Invisible, but uGUI still lets it catch pointer events, and it sits exactly where
  the toolkit window opens. `PlayerList.Awake` now puts a CanvasGroup with `blocksRaycasts = false`
  and alpha 0 on the old list root when the toolkit window is assigned. Nothing in the scene
  changes. Worth remembering for every later migration: an old uGUI panel left in place can block
  input to its UI Toolkit replacement even when it draws nothing.
- **Windows could be dragged off the left, right and bottom edges.** That was deliberate in the first
  version, which kept only a 64px strip on screen. It read as a bug, so windows now stay fully on
  screen, pinning to the top left only when larger than the screen. The size used for clamping is
  now the laid out size rather than the content rect, which excluded the border.
- **Spacing, and the missing experience bar.** Unity's default theme gives every Label padding and
  margin. In a dense row that opened a large gap between the name and stats line, floated the level
  caption above its number, and pushed the row past its fixed height, where the ListView item clips,
  hiding the experience bar. Labels inside `.rf-window` are now zeroed.
- **Legibility.** The background now reads as nearly solid. 0.9 alpha measured correctly but looked
  far more see-through, because UI Toolkit blends in linear colour space. Names and levels use
  `-unity-text-outline` rather than only a soft shadow. An outline keeps a hard glyph edge against
  any terrain and survives stream compression better than a blur. Font sizes are up a step and rows
  are 80px.

### Dev tools toggle

`GameManager.isDebugMenuVisible` was a plain field: forgotten every launch, and ignored completely
in DEBUG builds, so the settings toggle did nothing in the editor. It is now a property persisted in
PlayerPrefs, honoured in every build, defaulting to shown in the editor and hidden in builds (what
each did before). OnGUI draws a small "Hide dev tools" / "Show dev tools" button above the stack for
administrators.

## Raid strip

`Assets/Scripts/UI/Toolkit/RaidHudView.cs`, raid section of `ravenfall-hud.uss`. The strip first
lived in its own `RaidHud.uxml`; it is now part of `TopStatus.uxml` (see "Mockup pass" below), and
the layout described here was superseded by that pass.

The old raid put three things on screen: a "A boss appeared" card about 1150 by 230 across the top,
a "897 seconds left" timer floating in the middle of the fight, and a health bar at the bottom. On a
play test it covered too much of the screen, overlapped the experience multiplier banner, and put
text right over the fight viewers came to watch.

All of it is now one 600px strip at the top centre, below the multiplier banner:

- Boss level, raider count, and time left as `m:ss` rather than a three digit second count. The
  timer turns red under a minute.
- A health bar with the numbers on it and a trailing "ghost" fill. The ghost follows the real fill
  with a slower, delayed transition, so a heavy hit leaves a lighter band that drains a moment later.
- "Type !raid to join" while joining is open, hidden once the boss drops below the join threshold.
- The announcement moment is kept but brief: the strip opens with a large "A boss appeared!!" line,
  which folds away after four seconds.
- Nothing in it takes clicks.

Every value is cached, and labels are only rewritten when the displayed text changes. RaidManager
pushes health, level and time every frame, so that matters.

`RaidNotifications.raidHud` forwards everything when assigned and falls back to the old objects when
cleared. RaidManager now also passes the raider count.

Wiring: a `UI Toolkit Raid HUD` object in MainWorld with its UIDocument at sorting order 15, above
the player list (10) so a window dragged to the top cannot cover the raid, and below the
announcement documents (100).

The shared announcement card used by the arena was also the source of the oversized look:
`min-width: 60%` made it 1150px wide at 1080p. It is now sized to its text, capped at 720px, sits
120px down to clear the multiplier banner, and uses smaller type.

Dungeons still use the old boss health bar; they are next for the same treatment.

## Mockup pass

Reference images in `docs/mockups/`: `player-list`, `raid-panel`, `character-details`,
`dungeon-meters` and `town-resource-bar`. They were generated as a direction, not a spec. Where the
game has information a mockup leaves out, the game wins.

### Top status column

`Assets/UI/Screens/TopStatus.uxml`, `RaidHudView.cs`, `XpBoostView.cs`.

The XP multiplier banner and the raid panel were separate pieces of UI that each assumed they owned
the top of the screen, so they overlapped. They are now one flex column at the top centre: the XP
boost pill first, then whichever event is running underneath it. Whatever combination is active,
they stack.

- **XP boost pill.** Multiplier, time left as "8h 41m", a thin drain bar, and "Thanks to name"
  underneath when a viewer started it. `BATimer.toolkitView` forwards to `XpBoostView`; while it is
  assigned, the uGUI banner stays hidden. Clearing it brings the banner back. GameManager now also
  passes the multiplier as a number through `BATimer.SetMultiplier`.
- **Raid panel.** 780px wide, matching the mockup. The top line has the boss title, "LV" with a
  gold level, a live dot and an `m:ss` clock. Below that is a 34px health bar with
  "7,560,000 / 12,000,000 HP" on it and the trailing ghost fill. The bottom line has the raider
  count, a large centred percentage, and a gold bordered "Type !raid to join" pill. The pill is
  hidden by visibility rather than display, so the percentage stays centred after joining closes.

Scene: the `UI Toolkit Raid HUD` object is renamed `UI Toolkit Top Status`, its UIDocument points
at `TopStatus.uxml`, and it carries both `RaidHudView` and `XpBoostView`. Sorting order is still 15.

### Player list

- Rows are 96px. The name is 22px in the viewer's Twitch colour, lifted to a readable luminance.
  The level is 32px extra bold.
- The skill level is bold through rich text. The experience rate is compact: "1.76M xp/h" rather
  than a seven digit number. Rested reads "Rested 1h" or "Rested 25m".
- A footer shows "8 of 11 players" when the list scrolls, and "11 players" when it all fits. Only
  fully visible rows count.

### Fonts

The HUD now uses the game's own font, FOT-NewRodin Pro, referenced straight from the `.otf` files:
M for body text, B for numbers in running text, and EB for the large numbers people glance at.
Icons come from Font Awesome 5 Pro Solid, through the `rf-icon` class.

### Formatting

`HudFormat` is shared by every window and always uses invariant culture, so Swedish and other
locales do not print decimal commas in English text. It provides:

- `Compact`: three significant figures with a suffix, such as 842K or 1.76M.
- `Grouped`: comma separated thousands.
- `Clock`: `m:ss`.
- `Duration`: the two largest units, such as 8h 41m.

`Utility.FormatValue` rounds to one decimal and inserts a leading space for digit counts that are
multiples of three, so the HUD no longer uses it.

### Character details, combat stats

The mockup only shows Armor and Power. The game has an aim and a power value for each of melee,
ranged and magic. The plan for the port is a row of four tiles: Armor, Melee, Ranged and Magic.
Each style's tile shows power as the large number with aim small beneath it. The tile for the
player's current combat style gets the gold border, and the other two are dimmed. This replaces the
Armor/Power row, takes the same height, and answers "how strong is this character at what they are
doing right now" at a glance, without hiding the other styles.

## Player details

`Assets/UI/Screens/PlayerDetailsWindow.uxml`, `Assets/Scripts/UI/Toolkit/PlayerDetailsWindow.cs`,
`Assets/Scripts/UI/Toolkit/HudTooltip.cs`, `Assets/UI/Styles/ravenfall-details.uss`.

Laid out after `docs/mockups/character-details.webp`, from the top in the order a viewer asks:

- **Header.** Name in the viewer's Twitch colour, lifted to a readable luminance; clan; coins;
  combat level. Badges (Streamer, Moderator, Subscriber, VIP, Developer) and a rested chip, which
  turns gold while resting in the onsen.
- **Observe timer.** A thin line under the header that drains until the camera moves on, in place
  of the "(20s)" label.
- **Training card.** Skill icon, "TRAINING · MAGIC 511", percent, progress bar, time to the next
  level, xp/h, and the recommended island when the player is not on it. It covers the same cases as
  the old panel, in the same order: target level reached, max level, level too high or too low for
  the island (with the `!sail` hint), not able to train here. The card turns red or green for those.
  Training all now reports whichever of attack, defence and strength levels first; the old panel
  compared defence twice and never looked at attack.
- **Skills.** 18 tiles in four columns: combat, then support, then gathering and crafting. Each tile
  has an icon, the current value (green when boosted, red when drained, gold at max level) and an
  experience bar. The skill being trained gets the gold frame. Enchanting is the clan skill and
  shows locked when the clan has not unlocked it. Hovering a tile shows level, progress,
  experience, rate and time to the next level.
- **Equipment.** Folds away and remembers whether it was open, like the old inventory toggle. It
  holds:
  - The live face camera, with buttons that turn it around the character.
  - A 3 by 4 slot grid: gold frames for equipped items and purple for enchanted ones.
  - Exp, raid and dungeon scroll counts.
  - The combat stat tiles: Armor, Melee, Ranged and Magic. Each shows power as the large number
    with aim beneath. The style in use gets the gold frame and the others are dimmed. Hovering
    splits base and enchantment bonus.
- **Item tooltips.** Item stats, enchantments in purple and transmog status. They are built as
  label and value rows, since the old version padded with spaces inside a monospace tag.

`PlayerDetails` remains the controller: who is observed, the timeout, camera switching, F2 and the
settings toggle. With `uiToolkitWindow` assigned it forwards to the window, switches off the uGUI
panel's children so their per frame observers stop running, and skips the old observers in
`ForceUpdate`. Clearing the field brings the old panel back.

Refreshing:

- Header, training card and skill tiles refresh every 0.25s.
- Gear stats, scroll counts and the clan skill refresh every second.
- Slots refresh when equipment changes.
- Every element keeps the value it last showed.

`PlayerObserveCamera.Rotate` adds a yaw offset that eases into place and resets when a different
player is observed. `EquipmentSlot.GetItemSprite` is now shared, so both panels use one item icon
lookup and cache.

Scene: a `UI Toolkit Player Details` object with its UIDocument at sorting order 12, above the
player list (10) and below the top status column (15). The icon sprites were copied from the old
panel's own images, so both use the same art.

The combat stat tiles are kept as one small, data driven piece of the window. Equipment stats may
move away from aim and power to an elemental system later, and that should only need to replace
this part.

### Player details, first play test fixes

- The scroll counts sat in dark boxes overlapping the header line. `.rf-scroll` was already the
  core sheet's ScrollView frame, so the chips inherited its background and border. They are now
  `rf-scroll-chip`.
- Skill names were cut to five letters ("Atta...", "Fishi..."), because name and level shared one
  line. The name now has its own line, with the bar and level beneath it.
- The character in the preview was small and floating mid frame. The camera image now extends
  past the frame's bottom edge, which scales the character up and lowers them so the feet sit just
  above the turn buttons.

## Dungeon

`DungeonHudView.cs` (in `TopStatus.uxml`), `DungeonMetersWindow.cs` with `DungeonMeters.uxml` and
`ravenfall-meters.uss`, and `Game/CombatMeter.cs`.

### Panel

The dungeon gets the raid panel's layout in the top status column, with a purple edge and dot so
viewers can tell the two apart.

- **Joining:** boss level, a countdown to the start, how many have joined, and a "Type !dungeon to
  join" pill. It opens with "A dungeon has appeared!", which folds away after four seconds.
- **Running:** the room as "ROOM 3 / 5" or "BOSS ROOM", the run clock, players alive out of
  joined, and enemies left.
- **Boss:** the health bar and percentage appear in the boss room, or as soon as the boss is hurt.

The view reads `DungeonManager` on a 0.1s interval rather than being pushed values. The old
notifications only ever received part of the state, and polling keeps it all in step.
`DungeonNotifications.hudView` forwards the announcement, since the join code is only known then,
and hides the old objects. The horn, stream labels and dungeon stats work either way. Clearing the
field brings the old UI back.

### Meters

Damage done, healing done and damage taken per player, in the style of MMO combat meters. Tabs
switch the metric, and the choice is remembered. Each row shows:

- Rank and a combat style icon.
- The name in the viewer's colour, with a bar relative to the leader in the style's colour: red
  melee, green ranged, purple magic, teal healing.
- Total, per second over the run, and share of the group.

The leader's row is highlighted. The footer shows the group total and group rate.

The window opens on its own when a run starts. It stays up for a minute after the run ends, with
"RESULT" in place of the room, so the outcome can be read. Closing it hides it until the next run.
It is draggable, resizable and collapsible like the other windows, and virtualised, so a hundred
player run costs the same to draw as a six player one.

### Recording

`CombatMeter.Dungeon` records three numbers per player per run: damage, healing and damage taken.
Each hit is a dictionary lookup on the player id and three additions; sorting is left to the
window, on its interval. The hooks are in `PlayerCombat.DamageEnemy`, the heal path, and
`PlayerController.TakeDamage`. Each only records while that player is in the dungeon.
`DungeonManager` begins the meter when the run starts and ends it in `ResetDungeon`. Ending
freezes the numbers rather than clearing them, so the result can be shown afterwards.

The meter is not tied to dungeons, so raids could get the same window by recording into a second
instance.

Scene: `DungeonHudView` sits on `UI Toolkit Top Status`. `UI Toolkit Dungeon Meters` is at sorting
order 11, between the player list (10) and player details (12).

### Second play test fixes

- **Player list highlight** now marks the observed player, not the broadcaster. Recycled rows
  kept the old highlight class, because the cache reset to "off" while the class stayed on the
  element. The cache now starts unknown, so the first refresh always writes it.
- **Clicking a row** observes that player.
- **Level overlap:** the level column sizes to its number instead of a fixed 82px, so a four digit
  level no longer spills over the rate and bars.
- **Scroll wheel:** `DraggableWindow.IsPointerOverWindow` tracks hover on every HUD window, and the
  orbit, island and mouse orbit cameras ignore the wheel while it is true. Scrolling a list no
  longer zooms the world.
- **Boost reason:** on a dark chip in the bold weight. The outline on thin text read as hollow
  letters.
- **Dungeon boss bar:** the maximum is `Health.Level`. Boss health is scaled by writing Level and
  CurrentValue directly, which leaves MaxLevel unscaled, so the bar read "79,510 / 751".
- **Meters:**
  - Anchored by top rather than bottom. With both set, collapsing to an auto height stretched the
    window between the edges instead of folding it.
  - Number columns share extra width when resized, and the percent column fits "100.0%".
  - After a run a line drains under the header, with "closes in 45s" next to RESULT.
- **Training card:**
  - Idle shows no bar, just a prompt to type `!train [skill]`.
  - Resting in the onsen shows the rested pool filling toward its two hour cap, "1h 14m of 2h 00m",
    and how long until it is full. Rested time builds at twice real time.
- **Player details** opens at 64px from the top, below the settings cog.

Open: the meters header only drags from a thin strip at its top. The HUD panel's sort order (0)
is below the legacy Canvas2D (2), so any active uGUI raycast target over a window wins the click.
The culprit needs a live raycast in Play mode to name.

## Town resource bar

`TownBarView.cs`, in `TopStatus.uxml`. Laid out after `docs/mockups/town-resource-bar.webp`.

Shows village level with its experience bar, then wood, ore, fish, wheat and coins with hourly
rates, then houses built out of slots. It is first in the top status column, so the boost pill
and any event stack beneath it. It shows in free camera mode, except in the tavern, which uses
the free camera but has its own interface.

The server only sends totals, so the rates are measured locally. The totals are sampled every 10
seconds, and a rate is the change across the last 10 minutes. A rate only shows once a minute of
history exists; earlier, a single server update would make it swing wildly. Falling totals show
in red. `TownHallManager` now keeps the slot counts it used to hand only to the world space info
panel. The resource icons are the town hall info panel's own sprites.

The mockup's build mode button and building toolbar wait for build mode itself: free placement,
limited by resources, with building types unlocked by village level.

## UI scale

`UiScale.cs`. One scale for all 2D interface, saved as `uiScale` in the game settings, applied on
load. Ctrl+= and Ctrl+- change it in 5% steps between 60% and 160%. The keypad keys are left
alone because they already set the free camera speed, and Ctrl+0 would also jump to stored camera
position 0.

- UI Toolkit panels: `PanelSettings.scale`.
- Screen space canvases that scale with screen size: a smaller reference resolution.
- Constant pixel size canvases: a multiplied scale factor.
- World space canvases, such as name tags, are not touched.

Original values are remembered the first time each is seen, so changes never compound. The panel
settings are an asset, so their original scale is restored when the application quits, which in
the editor is when Play mode ends.

The settings menu needs a slider for it in the menu pass. The old player list scale and size
sliders act on the hidden uGUI list and do nothing now; that is part of the same pass.

## Combat numbers

`FloatingTextView.cs`, `FloatingText.uxml`, `ravenfall-floating.uss`.

These replace a world space canvas per number, each with a comic "pow" burst behind it. Every
number is now a pooled label on one screen space layer, projected from the world each frame, below
every window in sorting order.

- **Colour says what happened:**
  - White for damage dealt, gold for a standout hit.
  - Red for damage a player takes. These sink rather than rise, so they never read as a hit the
    player landed.
  - Green with a plus for healing.
  - A smaller grey "Miss" in place of a 0.
- **Size says how much:** relative to the biggest recent hit on the same target, with that peak
  decaying over time. The big hits stand out at any scale, from a goblin to a raid boss. Distance
  from the camera shrinks numbers further away.
- **Motion:** each number pops in large and slightly tilted, settles upright with a little
  overshoot, rises on a sideways arc and fades over its last 40%.
- **Merged numbers:** bosses merge hits into one climbing total, which pops again on every hit.

`DamageCounterManager.toolkitView` forwards every call; clearing it brings the old counters back.

## Island details and enemy target frame

`IslandDetailsWindow.cs`, `EnemyDetailsWindow.cs`, their UXML, and `ravenfall-observe.uss`.

**Island details** shares the player details corner, since the camera observes one or the other.
- The header has the island's name, a level requirement chip, an onsen chip when there is a resting
  area, and the player count. The observe timer line drains under it.
- What people are training there is a ranked bar chart rather than fourteen fixed counters, most of
  them zero.
- The island's statistics follow in two columns.
- It refreshes twice a second; the old panel rebuilt everything every frame.
- The old panel showed the Health counter in the Healing row, so healers never appeared. Healing is
  counted as itself now.

**Enemy target frame** opens for an enemy clicked in free camera mode. It shows:
- Name and combat level.
- A health bar, measured against `Health.Level` for the same reason as the dungeon boss bar.
- Attack, defense and strength.
- Armor and weapon power and aim.
- Players defeated and deaths.

Clicking an enemy no longer goes through a HUD window to the world beneath it.

`IslandDetails.uiToolkitWindow` and `EnemyObserveDialog.uiToolkitWindow` forward when assigned.
The window's close button is hooked on the first show rather than in `Awake`, because with the
window in use the old dialog's object is never activated.

## Settings and the game menu

`SettingsWindow.cs`, `GameMenuWindow.cs`, `SettingsApplier.cs`, their UXML, `ravenfall-settings.uss`
and `ravenfall-gamemenu.uss`. Scene objects are added by `tools/ui/settings_window_setup.cs` and
`tools/ui/game_menu_setup.cs`.

**The game menu** (Esc or the cog) is a centred panel over a dimmed screen:
- The logo, with a random tip under it each time it opens. The tips are easter eggs; keep adding them.
- Continue, Settings, Log out and Exit game. Log out and exit ask for a second click within three
  seconds and turn red while they wait.
- Version, graphics API, memory, system and uptime in small print, and the log folder link.
- Clicking the backdrop closes it, like Continue. The old Players and Raid and exit buttons were
  already switched off and are not carried over.

`GameMenuHandler` stays the entry point. With `toolkitMenu` assigned it opens and closes the toolkit
view and leaves the uGUI panel switched off; the buttons call back into it, so the login screen,
log out and exit behave as before.

**Settings** is a window that moves by its header and resizes from the corner, both remembered.
There is no backdrop, so graphics changes can be watched as they are made. It first opens at 66% by
80% of the screen. Tabs: Game, Graphics, Sound, Interface, Bot messages, and Admin for
administrators.
- The rows are built in code from one set of controls: an on/off switch where the whole row is the
  click target, a slider with its value beside it, a row of buttons for up to four choices, and
  arrows either side of the value for longer lists. None of Unity's Toggle or DropdownField is
  used, so nothing depends on the runtime dropdown popup.
- The item drop setting shows an example made by the real `PlayerItemDropText`, so it always
  matches what chat gets.
- The Interface tab has the UI scale slider this page asked for, and the dead player list size and
  scale sliders are gone.
- Changes apply at once and the file is written when the window closes.

`SettingsApplier` holds what each setting does, for the window and for `GameManager` on load. Bugs
fixed on the way, all from the old menu:
- The camera rotation speed was saved with both signs in turn, so it flipped direction every time the
  menu closed.
- Bot message filters were stored inverted and never written to the file, so they were lost on
  restart. A message the game was never told about counts as filtered, so every one is set on load.
- The quality, player state expiry and navigation dropdowns all called the boost requirement
  handler, so quality only applied on close and navigation quality never did. Navigation also used
  the minimum table for its maximum.
- The DungeonJoin and RaidJoin bot filters are not checked anywhere and are left out.

**Resets.** Every setting knows its default (`SettingsApplier.Defaults`; the two volumes are read
from the scene's audio sources before the saved settings apply). A reset button shows beside any
setting that has been changed, and the footer's Reset page puts the whole page back. On the
Interface page it also resets window positions. The interface size slider applies when it is let
go: the window scales with it, so applying while dragging moved the slider under the pointer.

**Settings added** that the game already read but no menu showed: how long the observer camera
stays on everyone, subscribers, VIPs and moderators, and after a sub or a cheer; time per island
and whether empty islands get a turn; kicking players who are away after a number of hours; the
no exp warning and how soon it comes; whether `!loot` says where items came from; and the stream
label text and json files separately. Item drop messages moved to the Bot messages page.

**Reset window positions did nothing.** `DraggableWindow.ResetAllPositions` cleared each window's
inline position and then clamped to its resolved position straight away, which still held the old
one until the next layout. It now clamps on the next geometry change. A scheduled callback did not
work either: the scheduler runs before layout.

**Shortcut menu.** The bar of shortcuts is now a corner micro menu, bottom right, in the manner of
an MMO: square icon buttons in groups (camera, players, interface, admin, menu) with each key as a
badge on its button. Runtime UI Toolkit shows no tooltips, so a caption above the cluster names the
hovered button. It dims while the mouse is elsewhere. The raid and dungeon buttons only show for
administrators, whose keys are the only ones that work.

**Second round of settings.**
- Graphics: window mode and resolution (applied by `SettingsApplier.ApplyScreen`, which does nothing
  until one is chosen, and nothing in the editor), vertical sync and a frame rate limit. Every
  quality level turns vertical sync on by itself, so GameManager calls `ApplyFrameRate` after each
  quality change, or potato mode would quietly undo the player's choice.
- Sound: a master volume through the audio listener. There are no interface sounds and no ambient
  sounds to give their own sliders. `MusicManager` stopped bringing back the track it had faded
  out: the old track kept looping at zero and the volume setter turned both up.
- Camera: free camera speed, mouse look, scroll zoom and inverting either, in `CameraSettings`.
  Scroll zoom no longer scales with frame time, which made each wheel tick zoom less the higher the
  frame rate, and keypad minus can no longer fly the free camera backwards.
- Keys: the Keys page rebinds every shortcut, through `Keybinds`, which every hotkey check now goes
  through. Modifiers match exactly (Shift no longer counts as Ctrl), and nothing fires while a text
  field has the keyboard or while a key is being bound. Esc stays Esc.
- Bot: the warning times for the exp multiplier, and the RavenBot connection and query engine,
  which apply when the game reloads. A boost that arrived with under a minute left used to get
  three warnings at once; now passed warning times share one message.

The admins' OnGUI "Show dev tools" button is gone; the Admin page's Developer tools switch does
the same.

Views made in this pass rebind when a live reload replaces their visual tree, so markup and styles
can be tuned in Play mode without the window going dead.

Esc closes the settings window and brings the menu back. Both windows report the frame they closed
in (`SettingsWindow.IsOpen`, `GameMenuHandler.ClosedThisFrame`), so `GameManager` does not take the
same key press as a reason to open the menu again.

The uGUI menu and settings are gone: the hierarchy under `Game Menu` (Panel, Main, Players, Settings,
RaidAndExit; 438 objects) was deleted with `tools/ui/remove_old_menu.cs`, and `SettingsMenuView`,
`MenuView` and the empty `SoundsSettings`, `UISettings` and `GraphicsSettings` went with it. The
`Game Menu` object stays, switched off, as the home of `GameMenuHandler`; since it never wakes, the
handler finds its references on first use. Before deleting, the scene file was scanned offline for
anything outside the hierarchy pointing into it. Do not run that scan inside the editor: walking every
serialized property of every component in MainWorld crashed Unity.
