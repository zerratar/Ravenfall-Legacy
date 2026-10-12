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

### Phase 3: costs, upgrades and unlocks

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

### Phase 4: new building types, roads and decoration

- **Paths:** paint roads cell by cell.
- **Decor:** fences, lamps, trees, flower beds, statues. Cheap, no effect beyond looks.
- **Production buildings:** a lumber mill, quarry and so on that add to village resource income.
- **New art:** models per building and tier, made in Blender.

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
