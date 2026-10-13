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
- **The slot limit stays for now.** The number of buildings a village can have still comes from its
  level and Patreon tier, through the costs phase too. Whether it ever goes is decided later, once
  costs are balanced and players are used to the new system.
- **Experience is not affected by the global multiplier.** This is unchanged.
- **One step at a time.** Phase 1 changes only the interface. Phase 2 adds placement. Costs,
  upgrades and new building types come after, each as its own change. Players get time to settle
  into each step before the next one. Changing the whole system at once could scare people and leave
  them confused, especially before the numbers have been balance tested.

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

### Next: a movable town hall, then town hall upgrades (plan, 2026-10-12)

Two changes, shipped one after the other. The first only moves things, the second adds the first cost in
the game. Neither touches the slot limit. Nothing here is built yet.

#### Step A: the streamer can move and turn the town hall (no costs)

**Built 2026-10-12, tested 2026-10-13 against a local RavenNest** (Ravenfall > Use Local RavenNest Server):
moving the hall, reloading after a restart and resetting all round-trip. The test found that every move was
refused (the request type carried int tier fields the client sends as null); fixed in RavenNest `9bcd5c1d`.
Server: RavenNest `b1e1e439`. The columns are added at startup by `DatabaseSchema`; the SQL files are optional. In build mode, click the town hall to select it; its panel has Move, Rotate and Reset
position. Houses now also keep off the whole town hall area (about 49 x 45 m round the hall), not just the
building, so an upgrade never lands on one. The same change fixed the mouse point while moving: it was cut
against a plane at the manager's height, eight metres under the village, so buildings followed a spot away
from the cursor.

Today all five tiers stand on one plaza at world (-23.6, 8, -109.7), and the client picks the tier from
the village level (`TownHallManager.GetTownHallTier`: under 10, 30, 80 and 150, then tier 5).

Server (RavenNest), following the house placement pattern from `109d2b8a`:

- `Village` gains nullable `TownHallX`, `TownHallZ` and `TownHallRotation` (`double?`). Null means the
  plaza, so every existing village stays as it is. The columns come from a new `sql/village-town-hall.sql`,
  safe to run twice, run before deploy.
- New endpoints on `VillageController`, authenticated with the session token like the placement ones:
  `GET api/village/townhall` (position and, from step B, the tier), `POST api/village/townhall/place`
  (X, Z, rotation) and `GET api/village/townhall/reset-placement`.
- The server only rejects unusable numbers (NaN, infinity, beyond 10000), as it does for houses.
- The answer is a new type. `VillageInfo` stays as it is, because it is compiled into every client.

Client:

- The five tier roots move and turn together, so an upgrade later appears where the old hall stood. The
  height comes from the ground under the new position.
- In build mode a click on the hall selects it, with a small hall panel: Move, Rotate and Reset position.
  It uses the same controls as moving a plot (grid snap, R to turn, click to place, Esc to cancel).
- Validation, client side like the plots:
  - Inside the village.
  - Clear of every plot.
  - Not on water, not too steep.
  - The footprint checked is the largest tier's, shown as a faint outline, so the hall always has
    room to grow and an upgrade never lands on a house.
- Fixes that come with it:
  - The village area is measured from the plaza, not from the active hall. Otherwise moving the hall
    would move the whole buildable area.
  - The mouse point uses a ground raycast instead of the manager's height.
  - The resource piles refresh when a tier is switched on.
- The halls are not part of the baked navmesh, so moving one leaves no hole and needs no rebake.
  Players already walk through halls today; a NavMeshObstacle could stop that later.
- Old clients ignore the new endpoint and draw the hall on the plaza.

#### Step B: town hall upgrades bought with resources and a village level

**Built 2026-10-12, tested 2026-10-13 against a local RavenNest:** a level 135 village kept tier 3 when
the tier was first stored; the castle was refused for want of level 150 with nothing spent; with the stored
tier set back to 2, buying tier 3 through the Upgrade button's path spent exactly 20,000 wood, 30,000 ore,
10,000 fish and 10,000 wheat, and the tier held after a reload. Server: RavenNest `6b9e4da6`. The town hall panel in build mode shows the next
tier, what it needs (met in green, missing in red) and an Upgrade button. First draft numbers, in
`TownHallTiers` on the server, to be tuned from play rather than from existing stockpiles, which have
grown for years with nothing to spend them on:

| Tier | Hall | Level | Wood | Ore | Fish | Wheat |
| --- | --- | --- | --- | --- | --- | --- |
| 2 | Town hall | 10 | 2,000 | 1,000 | - | - |
| 3 | Guild hall | 30 | 8,000 | 6,000 | 3,000 | 3,000 |
| 4 | Keep | 80 | 20,000 | 30,000 | 10,000 | 10,000 |
| 5 | Castle | 150 | 60,000 | 90,000 | 30,000 | 30,000 |

A gathering player adds one resource to the village every 10 seconds (360 an hour), so a small stream
with about three players on a resource earns roughly a thousand of it an hour.

When an upgrade can be bought, a small green arrow shows on the town bar's BUILD button.

- **Stored tier.** `Village` gains a nullable `TownHallTier` (`int?`), returned by
  `GET api/village/townhall`.
  - Null means "follow the village level", which is today's behaviour.
  - When the feature goes live, a village keeps the tier its level gives it today. The first read stores
    that tier, so nobody ever loses a tier.
  - From then on the tier only rises by buying it.
- **Requirements.** Each tier above the stored one needs a village level and a resource cost.
  - Today's thresholds (10, 30, 80, 150) are the natural level gates.
  - The costs are open. They come from the user before any balancing.
  - The table lives on the server, so it can be tuned without a game release.
- **Buying.** `POST api/village/townhall/upgrade`:
  - The server checks the level and the resources.
  - It subtracts the cost from the village's `Resources` (the same check-then-subtract as player
    crafting costs), raises the tier, and returns the new state.
  - The client then calls `TownHallManager.SetTier`.
- **Client.**
  - When the server sends a stored tier, the three `SetTierByLevel` callers stop switching the tier
    on level-up. The town bar shows "upgrade available" when the level is reached instead.
  - The hall panel shows the current tier, a preview of the next one, each requirement as have and
    need, and an Upgrade button that stays disabled until everything is met.
  - Old clients keep deriving the tier from the level, which can show a tier the village has not
    bought. That is acceptable during the transition, and a version check could close it later.
- **Village coins never increase today.** Nothing on the server adds them. A coin cost would block
  every upgrade, so step B either leaves coins out of the costs or gives the village a coin income
  first.

#### Decided (2026-10-12)

- Placement checks the largest tier's footprint, so the hall always has room to grow.
- Moving the hall is free, like moving plots.
- At launch every village keeps the tier its level gives it today; only higher tiers are bought.
- Upgrade costs leave coins out (wood, ore, fish and wheat only) until the village has a coin income.
- Village coin income, first edition: defeating a raid or a dungeon pays the village coins, on top of
  what the players earn. No taxes or cuts from player gains. Players should never feel they lose
  something by helping, so the village is rewarded alongside them, not at their cost. Built as a first
  draft in RavenNest (`VillageCoinRewards`): a raid pays 500 plus 50 per player, a dungeon 800 plus 60 per
  player, times 1.0 (common) to 3.0 (legendary); players count up to 50. Paid from the existing raid and
  dungeon reward calls. Once it has run for a while, coins can join the upgrade costs.


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

- **Roads:** drawn by the streamer as editable splines. See "Roads" below.
- **Decor:** fences, lamps, trees, flower beds, statues. Cheap, no effect beyond looks.
- **Production buildings:** a lumber mill, quarry and so on that add to village resource income.
- **New art:** models per building and tier, made in Blender.

#### Roads

Requirements set by the user on 2026-10-12. This is not designed in detail yet; it belongs in phase 4,
after costs exist.

- **Drawn, not painted.** The streamer draws a road by placing points, the way city builders such as
  SimCity and Cities: Skylines do. The road is a runtime spline through those points, and the game builds
  its mesh. A grid snap (the same 1 m grid as placement) and an angle snap keep layouts tidy, and a modifier
  key gives free curves.
- **Editable in game.** A road can be selected at any time to drag, insert or delete points, change its
  type, or remove it. Picking must work even where a plot or a new building now overlaps the road: roads
  get their own pick layer in build mode, and their point handles draw on top of everything.
- **Town hall upgrades.** A new tier has a bigger footprint, and new plots appear as the village levels.
  Roads that now cross the town hall or a plot are flagged, for example drawn red with a warning, and stay
  editable. They are never deleted or rerouted automatically.
- **Road types and costs.** Each type has a width, a look and a cost per metre in village resources: a dirt
  road is free or cheap, cobbles and paving cost ore and more. Removing a road refunds part of its cost.
  Better types unlock with town hall tiers. The numbers come from the user, like the other phase 3 costs.
- **Mesh.** The spline is sampled into a strip of the type's width that follows the terrain, with UVs
  running along its length. Junctions start simple (overlapping ends sit flush), with proper intersection
  meshes later.
- **Server (RavenNest).** A village stores its roads separately from its houses, so old clients ignore
  them: an id, a type and a list of (x, z) points per road. A new endpoint saves and loads them, and the
  server checks the cost and the unlocks. Roads do not use building slots.
- **Possible gameplay later.** A navmesh area cost could make villagers and players prefer roads, and a
  road could give a small walking speed boost.

Decided: freeform splines with an optional grid snap, so both styles work. Open: whether one road can
have more than one width.

### The island for free placement

Done in October 2026, following `docs/mockups/island-overview.webp` and
`docs/mockups/townhall-tiers.webp`. The scripts that did it are in `tools/village-rebuild/`, with
notes in its README.

- **Terrain.** The old terraces, walls, stairs, paths and inner cliffs are gone. The town is one
  plateau at about 8 m, with a gentle slope down to the north beach and the bridge, and cliffs on
  the other sides. The cliff faces are dark stone, mossy rocks line the rim and a belt of trees runs
  round the edge.
- **Town hall tiers.** All five stand on one plaza in the middle of the island, facing south:
  1. Longhouse.
  2. Town hall with a bell tower.
  3. Guild hall on a terrace.
  4. Keep with onion-dome towers.
  5. Grand castle.

  The keeps have no roof, so you look down into a great hall. Each tier is self-contained under
  its `TownHall_Tier_n` root, so moving the town hall later only needs a stored position.
- **Easter eggs.** Kept:
  - The screaming lady, in the great hall from tier 3 up.
  - The QR tapestry.
  - The jester and the guards.
  - The quest board.

  New on tier 5:
  - Sir Honksworth the Crowned, a goose modelled for the fountain.
  - Sir Clucksworth, the chicken who held the fountain before him, dethroned and sulking at its foot with a
    paper bag on his head.
  - A raven in a party hat on a plinth.
  - A knight statue wearing a traffic cone.
- **Plots.** All plots were put back on the new ground. Three that sat inside the tier 5 footprint
  moved outward. Moved houses keep their saved positions.

### Status (2026-10-12)

- Phase 1 (interface) and phase 2 (placement, plus "place anywhere") are done.
  - Server side: RavenNest commit 109d2b8a.
  - `sql/village-house-placement.sql` must be run before deploying.
  - Play-test fixes from the last round still need a re-test.
- Island and town hall rebuild: done, as above.
- Every building type has its own house, modelled in Blender (`tools/village-rebuild/blender/house_*.py`).
  The building definitions point at the new prefabs in `Assets/Village/Prefabs`; the old prefabs are kept.
- The dark chest and red standard seen by the west plots in offline renders were ghosts: the GPU Resident
  Drawer drew objects removed in the same editor frame. A clean render shows nothing there.
- Next steps:
  - Phase 3: production buildings, costs, tiers and unlocks. Needs balancing numbers.
  - A movable town hall, then town hall upgrades bought with resources plus a level requirement.
    Planned in "Next: a movable town hall, then town hall upgrades" above, with open questions.
  - Phase 4: paths, decor and walls.
  - More custom Blender models. The first set is in: a clock tower for tier 3, round towers for tiers 4 and
    5, the tier 5 frontispiece and the fountain goose (`tools/village-rebuild/blender`).

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
