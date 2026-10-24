# Village decor

Status: design, 2026-10-24, for v0.9.6. Nothing built yet. Decided: no limit on decor, only costs; removing one of the
island's own pieces gives back half its worth, like anything placed; flowers and grass under a road or building are
hidden, never in the way; viewers can give the village decor with their loyalty points.

Streamers decorate their village in build mode: trees, bushes, flowers, rocks, fences, lamps, benches, statues,
banners, wells, carts, gardens and seasonal pieces. The trees and flowers already on the island become decor too, so
they can be moved or removed like anything else. Decor unlocks as the town hall tiers up and the village levels up;
later, some decor can only be had in other ways: bought on the website, given by viewers with loyalty points, won
from quests, or found in seasonal events.

## What is on the island today

The village island has about 110 trees and bushes (`Town Landscape/Tree Belt`) and about 130 flower patches
(`Town Landscape/Flowers And Stones`). They are plain scenery: not woodcutting trees, not in any village's data, the
same in every stream. Buildings can be placed into them, and they cannot be cleared. The coast cliffs
(`Town Landscape/Coast Cliffs`) are part of the island's shape and stay scenery.

## How it plays

A new **Decor** tab in build mode, next to Roads. It opens a grid of decor by group: Nature, Fences and paths,
Lights, Furniture, Statues and monuments, Seasonal, Special. Each tile shows a preview, the cost, and for a locked
piece what unlocks it ("Town hall tier 3", "Village level 80", "Christmas event", "Quest: ...").

- **Place:** pick a piece; it follows the mouse with the same outline and footprint rules as buildings (the true look,
  problems shown by outline and message, never by tinting). R and the Rotate tile turn it, Alt places off the grid,
  as for roads. Decor may stand on slopes and next to roads; small pieces (flowers, grass, stones) may stand on roads.
- **Select:** click placed decor (or a tree on the island) to move, rotate or remove it. A small panel shows what it
  is and what removing it gives back.
- **In the way:** a house, the town hall or a road placed over decor is refused, naming the decor in the way, as for
  buildings today. Small pieces are the exception (decided 2026-10-24): a road or building simply hides flowers,
  grass and small stones under it, and they come back if it moves away. Streamers place those wherever they like.

## The island's own trees: default decor

Every tree, bush and flower patch on the island gets a stable key (an editor script stores it on each object once).
A village only keeps its **changes**: default pieces it moved or removed, and pieces it added. A village that never
touches decor looks exactly as it does now, and later changes to the island's defaults (a new flower bed, say)
reach every village without moving anything anyone placed.

## Costs and limits

- Placing costs village resources, like roads, by what the piece is made of: wood for trees, bushes and wooden
  pieces, wheat for flowers and gardens, ore for stone and metal, and coins on top for the finest (statues,
  fountains, monuments). Prices are shown before placing.
- So removing an island tree gives back wood, and a flower patch a little wheat: half of what that piece costs to
  place.
- Removing any piece gives back half its worth, the island's own trees and flowers included (decided 2026-10-24).
  A new streamer can clear some of the island for a start in resources; a big village will not notice. Removing and
  placing again always loses half, so it cannot be farmed, and each island piece can only be removed once.
- **No limit** on how much decor a village has (decided 2026-10-24): the cost is the only brake. Decor is drawn
  instanced (as the trees and gather spots are, with the BatchRendererGroup), so hundreds of pieces cost little; if
  a huge village ever costs frame rate, potato mode can thin the small pieces.

## Unlocks

| Source | Examples | When |
|---|---|---|
| Town hall tier | Tier 1: trees, bushes, flowers, wooden fences, crates. Tier 3: lamps, benches, a well. Tier 5: statues, fountains, banners | Phase 1 and 2 |
| Village level | Rarer trees at 50, a garden at 100, monuments at 200 | Phase 2 |
| Seasonal events | Christmas trees and lights, Halloween pumpkins and gravestones, spring blossoms | Phase 3; once unlocked, kept for good |
| Quests (v0.9.6) | A quest chain's reward, a unique statue | With quests |
| Village treasury | Special pieces bought with the treasury's coins, which the tavern shelves now fill | Phase 3 |
| Viewer loyalty | Viewers spend loyalty points with the streamer to give the village a piece (decided 2026-10-24) | Phase 3 |
| Website | Pieces for patrons, or bought on the site | Later |

Unlocks belong to the village. Tier and level unlocks are worked out from the village; the others are stored per
village when earned.

## The server

- `VillageDecor`: id, village, decor key (or the default piece's key), position, rotation, scale, removed (for a
  default piece taken away), paid resources (for the refund), created.
- `VillageDecorUnlock`: village, decor key, source (event, quest, treasury, loyalty, website), created. Only for
  unlocks that are not worked out from tier and level.
- The decor catalog (keys, groups, costs, unlock rules) on the server as well as in the game, so the server checks
  costs, limits and unlocks; the game has the prefabs and icons.
- Endpoints and socket commands like the road ones: list a village's decor, place, move, remove; events to the game
  when it changes.

## The game

- A decor catalog asset: key, name, group, prefab, icon, footprint.
- The Decor tab and the place, select and move tools in `VillageBuildMode`, sharing the building tools' footprint and
  outline code.
- A decor layer that draws the village's decor instanced, applying the village's changes over the defaults.
- An editor script that gives the island's trees, bushes and flowers their keys and catalog entries.

## Phases

1. The island's trees, bushes and flowers become movable and removable; the Decor tab with nature pieces unlocked
   by tier; the server tables and checks.
2. A wider catalog (Synty props: fences, lamps, benches, wells, carts, statues, banners, gardens), unlocks by tier and
   village level, limits.
3. The other unlock sources: seasonal events (starting with Christmas), the village treasury, viewer loyalty.
4. Quests and the website, when they exist.

## Open questions

- **Prices:** the price of each piece (and so what removing an island tree gives back).
- **Viewer gifts:** how viewers give decor with loyalty points: a chat command (`!decor give <piece>`) that unlocks the
  piece for the village (the streamer places it), or a gift that waits in build mode for the streamer to place. And
  what a piece costs in loyalty points.
- **Website:** paid decor (real money) or patron perks only?
