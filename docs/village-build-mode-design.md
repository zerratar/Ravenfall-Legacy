# Village build mode

Status: phase 1 built, awaiting a play test. Mockup: `docs/mockups/town-resource-bar.webp`.

Phase 1 lives in `Assets/Scripts/Village/VillageBuildMode.cs` (state and server calls),
`Assets/Scripts/UI/Toolkit/BuildModeView.cs` with `BuildMode.uxml` and `ravenfall-build.uss`
(interface), `BuildingPreview.cs` (one-time card renders) and `HudPointer.cs` (keeps panel clicks
off the world). The BUILD MODE button is on the town bar.

Not in phase 1: removing a resident without assigning another, because the server has no call for
it yet. The old icon bar's "switch player" button was never implemented either.

The village today is a placeholder: a fixed set of plots in the scene, one house per plot, a type
per house, and a resident whose skill level sets an experience bonus for that type. Plots unlock
one per ten village levels (more with Patreon), up to 40. The streamer builds by clicking a plot
in free camera, which floats a row of icon buttons over it and opens a uGUI dialog.

The goal is a build mode that is fun to use and lets streamers make the village their own: a clear
way in and out, a proper building catalogue, free placement on a grid with rotation, roads and
decoration, costs, upgrades and unlocks. Without breaking any existing village.

## Ground rules

- **Existing villages keep working throughout.** No streamer has to rebuild or reassign anything.
  Every phase is additive on the server, and old game clients keep seeing what they see today.
- **The slot limit stays.** The number of buildings a village can have still comes from its level
  and Patreon tier, now and after costs arrive.
- **Experience is not affected by the global multiplier.** This is unchanged.
- **One step at a time.** Phase 1 changes only the interface. Phase 2 adds placement. Costs,
  upgrades and new building types come after, each as its own change.

## Phases

### Phase 1: build mode interface on today's village (no server changes)

Everything the old icon bar and dialogs did, in a real mode with UI Toolkit panels:

- **Enter and exit.** A BUILD MODE button on the town bar, and Esc or the same button to leave.
  Outside build mode, clicking the village does nothing, so a misclick while flying around can no
  longer start construction.
- **Plot markers.** In build mode every plot shows a marker over it: an empty plot shows a plus,
  a built one shows its building icon and the resident's name. Clicking a marker or the plot
  itself selects it.
- **Bottom toolbar.** Category tabs, as in the mockup. With today's buildings the categories are
  Combat (melee, ranged, magic, healing, slayer), Gathering (woodcutting, mining, fishing,
  farming, gathering) and Crafting (cooking, crafting, alchemy, sailing). Production, Decor and
  Paths appear when those exist.
- **Catalogue tray.** Cards for the buildings in the selected category: rendered preview, name,
  the skill it boosts, and the bonus its best available resident would give. Click a card with
  an empty plot selected to build there. Click a card with nothing selected to arm it, and every
  empty plot lights up as a target.
- **Plot panel.** For a selected building: type, resident with skill level and current bonus,
  and the actions Change building, Assign resident, Remove resident and Demolish. Assigning opens
  a ranked list of players, best bonus first.
- **Village summary.** The total bonus per skill, so the streamer can see what the village does.

The old `BuildSlotIconsManager` and dialogs stay in the scene and come back if the new view is
cleared, like every other migrated panel.

### Phase 2: placement (server stores positions, still no costs)

**Built, awaiting a play test.** Server: RavenNest `109d2b8a`, which needs
`sql/village-house-placement.sql` run first. What landed differs from the plan below in three ways:

- Positions come from a separate `api/village/placements` endpoint rather than being added to
  `VillageHouseInfo`, because that type is compiled into every client as a prebuilt library.
- Plots are still the unit. Any plot, built or empty, can be moved, and new plots still come from
  village level. Placing brand new buildings anywhere, with no plot first, needs the server to
  create houses on demand and belongs with phase 3.
- The client enforces the rules, and the server only rejects unusable numbers. The rules are: inside
  the village (no further out than the outermost original plot plus a margin), clear of the town
  hall and other plots, not on water, and not too steep.

Controls: select a plot, press Move, and the plot follows the mouse snapped to a 1 m grid. R turns
it 15 degrees (Shift+R back, Alt+R a quarter turn). Click places it when the outline is green, and
Esc or a right click cancels. Reset position puts a moved plot back where the scene had it.

Plots become positions rather than fixed scene objects.

- `VillageHouse` gains nullable `PositionX`, `PositionZ` and `Rotation`. Null means "the legacy
  plot position", so every existing house stays exactly where it is until the streamer moves it.
- A new endpoint moves or rotates a house; the server checks it is inside the village area and
  does not overlap another building.
- The village area is a grid of 1 m cells. Each building type has a footprint in cells. Placement
  snaps to the grid, and rotation snaps to 15 degree steps by default, or 90 with a modifier, so
  layouts stay tidy while still allowing angled streets.
- A buildable mask marks where building is allowed: not in water, on cliffs, or over the town hall,
  tavern and main paths.
- In build mode a building can be picked up, moved with a green or red ghost and rotated with R or
  the wheel, then dropped. Empty plots stop being fixed: building means placing a new building
  anywhere valid, up to the slot limit.
- Old clients keep receiving houses by slot index, and draw them at the legacy plot positions.

### Phase 3: production buildings, costs, upgrades and unlocks

#### Two kinds of building, one slot limit

Every building takes a plot, and plots are limited by village level. That makes each plot a real
choice, which is where the strategy comes from.

- **Skill buildings** (today's houses: Lumbermill, Magic House and so on) need a resident. They
  give that skill's experience bonus, from the resident's level, as now. New: a skill building for
  a gathering skill also raises the village's share of that resource from players gathering it.
  A Lumbermill with a good resident makes everyone's woodcutting feed the village more wood.
- **Production buildings** exist only for resources: a Lumber Yard makes wood, a Quarry ore, a
  Fishery fish, a Granary wheat and a Market coins, at a rate per hour that rises with each tier.
  Like skill buildings, they work only while a resident is assigned and that player is in the game.
  An empty or abandoned building produces nothing, so production follows the stream's activity
  rather than running unattended.

Players gathering still feed the village exactly as they do today. Production buildings add to
that rather than replacing it.

So the streamer chooses between experience now (skill buildings) and resources to grow the village
(production buildings). Resources pay for upgrades and new building types.

#### Rules

- **Plots.** Both kinds count toward the same limit, which still comes from village level and
  Patreon tier.
- **Tiers.** Every building has tiers, 1 to 5 to start with. Upgrading costs resources, and raises
  the bonus or the output. Tier pips show on the building panel, as in
  `docs/mockups/build-selected-building.webp`.
- **Unlocks.** Village level unlocks building types and higher tiers. Locked cards stay visible
  with the level that unlocks them (`docs/mockups/build-tray.webp`).
- **Costs.** Building, upgrading and demolishing follow the cost rules below.

#### Server

- **Catalogue.** It moves to the server, so rates and costs can be balanced without a game release.
  Each entry has a kind (skill or production), category, required village level, maximum tier,
  cost per tier, and its effect per tier (bonus percent, or resources per hour).
- **Production tick.** Production runs in `VillageProcessor`, which already ticks every 15 seconds,
  for each production building whose resident is in the session. Resources are added in proportion
  to elapsed time. Because it needs a player present, an offline village produces nothing and
  there is no backlog to cap.
- **Storage.** `VillageHouse` gains a tier. Production building types are numbered from 100, apart
  from `TownHouseSlotType`, so they never collide with a skill.
- **Old clients.** An older game client given a type it does not know shows the plot as empty and
  applies no bonus; `TownHouseSlot.UpdateExpBonus` already catches the failed lookup. That is
  acceptable, but a version check could hide production buildings from clients that cannot draw
  them.

#### Balancing to decide

- Output per tier for each production building, against what players gather per hour today.
- How big the gathering bonus from a skill building is, per tier.
- Upgrade costs per tier, and the village level for each unlock.

#### Costs, upgrades and unlocks

- **Catalogue on the server.** Building definitions move to the server, so balancing does not need
  a client release. Each definition has a category, footprint, required village level, maximum
  tier, cost per tier in wood, ore, fish, wheat and coins, and its effect per tier.
- **Costs.** Building and upgrading spend village resources. Demolishing refunds part of it.
  Migrated buildings are free: they were built under the old rules.
- **Upgrades.** Tiers raise a building's effect. For a skill house that could be a multiplier on
  the resident's bonus. The exact numbers need balancing against the current maximum bonus per
  slot.
- **Unlocks.** Village level unlocks building types and higher tiers. The build tray shows locked
  cards with the level that unlocks them, so there is always something to aim for.
- **Plan and commit.** Placements made in build mode can be staged, with the total cost shown and
  a single Build button that commits them all. That makes undo trivial and keeps a half finished
  layout from spending resources. It is the same pattern as the marketplace plan and commit
  design.

#### Town hall

The town hall stops upgrading itself as the village levels. Each new tier needs the village level
it requires and a resource cost, and the streamer chooses when to build it. That makes the town
hall the biggest thing to save resources for, and the clearest sign of progress.

### Phase 4: new building types, roads and decoration

- **Paths:** paint roads cell by cell.
- **Decor:** fences, lamps, trees, flower beds, statues. Cheap, no effect beyond looks.
- **Production buildings:** a lumber mill, quarry and so on that add to village resource income.
- **New art:** models per building and tier, made in Blender.

### The island for free placement

Today the town hall tiers are whole complexes: the hall plus castle walls, battlements, paths, trees
and a statue, about 700 renderers in tier 5. The village ground around them is uneven. Both were
made for fixed plots.

For free placement the town area should become open, mostly level ground the streamer fills
themselves:

- **Town hall tiers** keep the building and its immediate steps, and lose the walls, battlements
  and paths.
- **Terrain.** The town area is levelled into a few broad terraces. It is a Unity Terrain, so it
  can be reshaped from a script with a backup of the terrain data first. Cliffs, trees and rocks
  are moved or removed where they block building.
- **Plots.** Default positions are reset onto the new ground. Moved houses keep their saved
  positions.

### Walls

Walls become something the streamer builds, from the Decor or a Defence category: segments placed
like paths, with gates and towers. A possible use beyond looks: a raid boss that spawns on the
home island attacks the town if it is not killed in time, and stronger walls buy more time before
anything bad happens. Undecided; walls may stay decoration only.

### Later: clan construction

A player suggested clan buildings: members gather and deposit resources to build and upgrade
skill halls (Grand Arena, Archery Range, Arcane Spire and so on) that boost their skill for the
whole clan. That maps onto this design directly. The catalogue, placement, costs and tiers all
apply to a clan town as they do to a village, with clan resources in place of village resources.
It is worth building the server side generic enough for it: a "settlement" with an owner type of
user or clan.

## Open questions

- Should a streamer be able to build from the website as well? The town page already edits houses
  by slot, so phase 2 needs it to understand positions, or at least not undo them.
- Do residents stay one per building, or do larger houses hold more than one resident as they are
  upgraded?
- How should Patreon slot perks work once costs exist: more slots, cheaper building, or both?

## Mockup prompts

The existing mockups came from image generation and set the style. These prompts follow the same
art direction. Each describes one screen; generate at 16:9, 1920x1080 or larger.

Shared style, prepended to every prompt:

> Screenshot of a cozy low poly 3D fantasy village game with a polished game HUD. Stylized
> low poly art, soft lighting, warm colours, grassy island village with stone paths, timber framed
> houses with blue and red roofs, a river and rocky hills. The HUD is dark charcoal panels at 97%
> opacity with a thin gold top border, rounded corners, a rounded geometric bold sans serif font
> like FOT-NewRodin, gold (#E8A33D) accents, glacier blue (#5FA8C7) for live information, green
> for gains, crisp icons. Clean, readable, premium, like a modern cozy city builder. No text
> artifacts, no watermark.

1. **Build mode overview.** "The player has entered build mode. The camera looks down at the
   village at a steeper angle. A faint square grid overlays the ground. Every building plot has a
   small floating marker: empty plots show a gold plus in a circle, built houses show a round
   skill icon and a small name tag. The town resource bar runs along the top with village level,
   wood, ore, fish, wheat, coins and a highlighted gold BUILD MODE button reading EXIT. At the
   bottom centre a toolbar of category tabs: Combat, Gathering, Crafting, Production, Decor,
   Paths, with Gathering selected."

2. **Building catalogue tray.** "Build mode with the Gathering category open. Above the bottom
   toolbar a wide tray of five building cards slides up. Each card has a rendered 3D preview of
   the building on a soft vignette, the name (Lumber Camp, Mining Hut, Fishing Lodge, Farmhouse,
   Gatherer's Lodge), a small skill icon with '+12% Woodcutting XP', and a gold BUILD button. One
   card is locked and greyed with a padlock and 'Unlocks at village level 40'. A card shows the
   cost row with wood and ore icons."

3. **Placing a building.** "The player is placing a new Fishing Lodge near the river. A
   translucent green ghost of the building follows the cursor and snaps to the grid, with the
   grid cells under it highlighted green. A small floating hint reads 'R to rotate, Click to
   place, Esc to cancel'. Another ghost position nearby, over the water, is shown in red as
   invalid. A small panel at the bottom right shows 'Fishing Lodge, 1 of 3 plots free'."

4. **Selected building panel.** "Build mode with a house selected. The house has a gold outline
   and a soft glow on the ground under it. A panel on the right side shows: the building name
   'Arcane Tower', tier 2 of 5 with five pips, the resident's avatar and Twitch name with 'Magic
   level 511', a large '+18% Magic XP' bonus, and buttons: Upgrade (with cost of wood, ore and
   coins), Move, Rotate, Assign resident, Demolish in red."

5. **Assign resident list.** "Build mode, a modal list titled 'Choose a resident for the Arcane
   Tower'. Rows of players with their Twitch name in their colour, a small character portrait,
   their Magic level, and the bonus they would give, best first, the top row highlighted gold with
   '+20% Magic XP'. A search field at the top, and a 'Currently: zerratar' chip."

6. **Village summary.** "A compact panel titled 'Village bonuses' listing each skill with an icon,
   a small bar, and its total XP bonus from the village (for example Woodcutting +24%, Magic +18%,
   Fishing +12%), with the number of buildings contributing. Clean and readable, on the left side
   of the screen over the village."

7. **Paths and decor.** "Build mode with the Paths category active. The player is painting a
   cobblestone road: a highlighted strip of grid cells shows the road before it is placed, with a
   cost preview. A decor tray shows fences, lanterns, flower beds, benches and trees as small
   cards."
