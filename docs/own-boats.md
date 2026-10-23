# Own boats: a player's own ship

Status: proposal, 2026-10-23. For the version after 0.9.5; nothing is built. The open questions at the end need
answers before work starts.

## The idea

A player can own a boat. Every time they sail, they take their own boat straight to the island they asked for,
instead of waiting for the ferry's fixed loop. They can also just sail, wandering the sea until they name an island
or go ashore at the nearest one. From their boat they can fish: what they catch depends on what they fish for and
their Fishing level, not on where they are. While the boat lies still and they fish, they gain no Sailing exp.

Later: boats that look the way their owner wants, and bigger boats on which more skills can be trained (cooking,
alchemy and so on).

## What exists today

A survey of the game, the server and the bots (2026-10-23) found what this builds on and what it does not have:

- **The ferry** runs a fixed loop of splines through the islands (`PathSelector`, `FerryController`), docks at
  each, and waits for players walking to the dock. The fastest passenger is the captain; their Sailing level and a
  ferry boost scroll speed it up. A leg takes about `2 x length / (9 + captain's Sailing level)` seconds.
- **On board**, a player is parented to a seat, their movement is locked, and every 2.5 s they gain Sailing exp
  (`FerryHandler`, client side; the server stores what it is sent). Their island is empty while at sea, which is how
  the server knows they are sailing (`SailingTaskProcessor`, which drops sailor hats).
- **Islands** have a dock (`DockController.DockPosition`), an identifier (Home, Away, Ironhill, Kyo, Heim, Atria,
  Eldara) and `IslandManager.FindClosestIsland`.
- **Fishing** needs a fishing spot on the player's island (client) and a real island with its level gate (server).
  The catch itself is not tied to the spot: the server picks from one table per skill (`ResourceItemDrop`, by the
  fish asked for or weighted by level). So fishing anywhere mostly needs the island checks to allow "at sea".
- **Items**: no boat, deed or vehicle item exists. `ItemType` and `ItemCategory` are enums that can grow, items
  can be soulbound (no gifting or marketplace), and `Flags`, `Tag`, `Enchantment` and `TransmogrificationId` exist
  on every stack.
- **Crafting** has recipes with ingredients and level requirements (`ItemRecipe`, `PlayerManager.ProduceItems`).
- **No NPC sells anything for coins** in the game; the vendor only buys, and its website buy only resells what
  players sold. Coins have no real sink (see [vendor-buy-and-command-config.md](vendor-buy-and-command-config.md)).
- **Handing out items** already works through dungeon and raid loot, the token shop (redeemables), loyalty and
  Patreon rewards, drop parties and direct grants.
- **Rendering**: the ferry and the moored boats ride the waves with `Ocean.Float` (`Boats.cs`). The instanced
  renderers (trees, grass) only handle things that stand still. The project has Synty rowboats, fishing boats,
  small and medium ships and a raft.
- **Commands**: `!sail` (no argument: ride the ferry, `stop`: get off, an island: travel), `!disembark`, `!ferry`,
  `!fish`, `!train sail` (RavenBot), routed to the game's `ferry_*` and `fish` packets, and to v2 commands
  (`CommandNames`, `CommandDispatcher`) on the server.

## Goals of the first version

- Get a boat (build it, or receive it).
- `!sail <island>` takes your own boat straight there.
- `!sail` on your own boat wanders until you name an island or go ashore.
- `!disembark` (or `!sail stop`) lands at the nearest island's dock.
- `!fish` at sea: the boat anchors and you fish; Fishing exp while anchored, Sailing exp only while moving.
- Saved and restored: a game restart puts you back on your boat, where you were, going where you were going.
- Works with hundreds of players at sea without hurting the frame rate.

Not in the first version: customization, bigger boats, other skills aboard, passengers on someone else's boat,
combat at sea.

## Playing it

"With a boat" means a boat equipped (see below); a player who wants the ferry unequips it.

| Command | With a boat | Without a boat |
|---|---|---|
| `!sail <island>` | your boat goes straight there | the ferry, as today |
| `!sail` | your boat wanders the sea | ride the ferry's loop, as today |
| `!sail stop`, `!disembark` | land at the nearest dock | off at the next stop, as today |
| `!fish [fish]` at sea | anchor and fish | (not at sea) |
| `!sail` again while fishing | weigh anchor, carry on | - |
| `!boat` | your boat: name, type, where it is | how to get one |
| `!boat build` | - | at the shipwright: build one from the parts (below) |

A boat sets out from the dock of the island you are on: you walk to the dock as you would for the ferry, the boat
appears beside it, you board, it sails. Arriving, it docks, you step ashore, and it is put away (a boat only exists
in the world while someone is on it).

States: ashore, boarding, sailing to an island, wandering, anchored, landing.

## Getting a boat

**Recommended: crafted parts plus a shipwright.** It makes a boat a real goal, gives crafters and gatherers a reason
to work, and the shipwright's fee is the first proper coin sink in the game.

1. Craft the parts with existing recipes (`ItemRecipe`): a hull (planks, Crafting), a mast (logs, Woodcutting and
   Crafting), sails (cloth, Gathering and Crafting) and rigging (rope). The parts are ordinary items: they can be
   traded and sold, so players without the skills can buy them on the marketplace.
2. Take them to the shipwright at a harbour (`!boat build`, on the island with the dock): the parts, a coin fee and a
   Sailing level requirement turn into a boat.
3. Events, the token shop, loyalty and Patreon rewards can hand out a boat directly, as they hand out items today.

**A first suggestion, to be revised.** The shipwright works at the Home island's harbour, where everyone starts.
Three boats, each a step up:

| Boat | Model | Needs | Parts | Fee | What it gives |
|---|---|---|---|---|---|
| Rowboat | Synty rowboat | Sailing 10 | small hull, oars | 10,000 | own trips, fishing at sea |
| Fishing boat | Synty fishing boat | Sailing 30, Fishing 30 | hull, mast, sail, rigging | 50,000 | faster; room for a net later |
| Sloop | Synty small ship | Sailing 60, Crafting 50 | large hull, two masts, sails, rigging | 250,000 | fastest; the base for bigger boats and skills aboard |

The parts each need a Crafting level (about the boat's Sailing level) and materials players already gather: logs
and planks for hulls, masts and oars, cloth for sails, rope for rigging. The exact items and amounts are worked out
against the current item list when this is built; the fees are first guesses to tune against coin income.

## The boat as an item

A boat is an inventory item: a new item type `Boat` (category `Vehicle`), one item per boat model. That way every
existing path works for it without new code: loot, rewards, the token shop, saving, the inventory view, and later
the transmog and enchantment fields for customization.

- **Per character**, like the Sailing and Fishing skills it goes with.
- **Tradeable until first launched**, then soulbound (the stack's `Soulbound`, set by the server on first use):
  boats from rewards or the marketplace can change hands, a boat in use cannot.
- **Equipped in its own boat slot** (decided 2026-10-23): the equipped boat is the one used, `!equip` and
  `!unequip` work as for gear, and a player who wants the ferry (for its captain bonus or a boost) unequips it. The
  slot changes no stats and nothing on the character; the equipment code needs to know that.

## Sailing your own boat

**Where it goes.** The ferry's splines are one fixed loop and cannot take a boat straight anywhere. Own boats use a
small graph of sea points instead: one off each dock, and enough points around the islands that a straight line
between neighbours never crosses land. A route from dock to dock is the shortest path through that graph (a few
dozen points; worked out once per trip). Points are placed by hand in the scene, with an editor check that every
link is over water. Wandering picks random neighbours, avoiding the last few points.

**How fast.** As the ferry: a base speed plus the Sailing level, so a high level is faster. Because the route is
direct and there is no waiting at every dock, an own boat is quicker than the ferry, which is the point of owning
one. The ferry keeps a reason to be taken: its captain bonus for others and its boost scrolls (see the open
questions).

**On board.** As on the ferry: the player sits at a point on the boat, movement is locked, the boat rides the waves
with `Ocean.Float`. Sailing exp every 2.5 s while the boat moves; none while anchored or docked.

## Fishing at sea

- `!fish` while on your boat at sea anchors it and starts fishing over the side, every 3 s as on land.
- **Server**: the fishing task processor accepts a character at sea on an anchored boat. The catch comes from the
  same per-skill drop table as today (`SimpleDropHandler`: the fish asked for, or one picked by level), with only
  the player's Fishing level as the gate, since there is no island requirement out at sea.
- **Client**: the exp factor normally comes from the island chunk's level window; at sea it comes from the player's
  level against the fish's required level. `PlayerController.Fish` reads the island's statistics and must not
  assume an island.
- Later, if wanted: fish only found at sea (deep sea fish), a small bonus for fishing far out.

## Saving and restoring

New character state, saved with the rest:

- `BoatMode` (none, sailing, wandering, anchored), `BoatDestination` (an island or none), and the boat's position.
- The game reads it in `PlayerSaveSource.ReadState`, which both the v2 saves and the old TCP path use. v2: a new
  `PlayerStateFields` bit and `PlayerStateSave` key; the old delta socket gets the same field; the server maps them
  in `SetCharacterState`. The columns are added by `DatabaseSchema` at startup.
- Restoring after a restart puts the player back on their boat at the saved position, still going where they were
  going. (Today's ferry restore loses the destination; it should keep it as well, see below.)

## Showing hundreds of boats

The first version uses ordinary objects, one per player at sea, from three or four Synty boats (rowboat, fishing
boat, small ship), riding the waves with `Ocean.Float` as `Boats.cs` already does. To keep it cheap:

- boats far from the camera stop riding the waves and drop to a low detail mesh;
- beyond a distance they are hidden, with a cap on how many are drawn (players on hidden boats still sail);
- potato mode: no wave riding, a lower cap.

If that is not enough at 1000 players, boats move to an instanced renderer like the trees, with transforms written
each frame, which the current instanced renderers do not support yet.

## Server, protocol and bots

- **Server**: `PlayerManager` methods to build a boat (check parts, fee and level, deliver the boat before taking
  coins and parts, as other purchases do), set the boat in use and soulbind it on first launch.
- **v2 commands**: `boats.build`, `boats.use` (in `CommandNames`, registered in `CommandDispatcher`, called from a
  client `BoatsApi`). Sailing and fishing go through the existing travel and fish commands.
- **Bots**: a ROBot `!boat` command and new packets (`boat_info`, `boat_build`, `boat_use`); `!sail`, `!disembark`
  and `!fish` stay as they are and the game decides whether the player's boat or the ferry is meant.
- **Older games**: a boat is an item of a type they do not know. Before boats can be built, check that 0.9.5 and
  older clients show or ignore an unknown item type without errors; if not, building boats waits until the server
  expects the boat version.

## Later

- **Customization**: hull colour, sails, a figurehead and a flag, stored on the boat's stack (transmog and flags
  fields), bought with coins or crafted. Shown to everyone on stream.
- **Bigger boats**: room for more aboard; train cooking (a galley), alchemy and crafting at sea; perhaps carry a
  few friends.
- **The sea**: events at sea, sea creatures, treasure fished up.

## Found while surveying (fix independent of boats)

- The server saved a sailing player's destination as the island they were on (fixed in RavenNest 9d7cd5fb).
- After a game restart a player on the ferry is put back on board with no destination, so they never get off.
- Players who sail on with no destination probably never get the Sailing hat drops (their task index is -1, so the
  server skips them). Inferred from the code, not tested.
- `!fish` with too low a level only warns and starts fishing anyway (`Fish.cs`, the check does not return).
- `FishingController.Level` is not used anywhere.

## Open questions

Answered 2026-10-23:

- **Boat or ferry**: the boat is equipped; unequip it to take the ferry.
- **The shipwright and its numbers**: the suggestion above, to be revised.

Still open:

1. Per character and tradeable until first launch, soulbound after: agreed?
2. **Riding along on someone's boat**: the user likes it ("lots of fun") but not for the first version. It probably
   needs something more basic first, such as viewers forming parties (who sails with whom, who decides where to go,
   what happens when the owner leaves the stream). To be looked into separately.
3. Fishing at sea: the same fish as on land, or some only at sea?
4. How much faster than the ferry should an own boat be?
5. **Storms damaging or sinking boats**: liked, but not for the first version. The catch: weather is per stream and
   a streamer can turn changing weather off (and potato mode holds it fair), so damage tied to the weather that
   viewers see would hit players on some streams and never on others. If it comes, it should come from a server
   side sea state that is the same for everyone (the stream shows it if its weather is on), or be something a
   player opts into, like sailing into a storm for better fish.
