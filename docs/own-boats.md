# Own boats: a player's own ship

Status: decided 2026-10-26, for v0.9.6, built in phases (see "Build phases" at the end). Proposed 2026-10-23.

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
| Fishing boat | Synty fishing boat | Sailing 50, Fishing 30 | hull, mast, sail, rigging | 50,000 | faster; room for a net later |
| Sloop | Synty small ship | Sailing 100, Crafting 50 | large hull, two masts, sails, rigging | 250,000 | fastest; the base for bigger boats and skills aboard |

The parts each need a Crafting level (about the boat's Sailing level) and materials players already gather: logs
and planks for hulls, masts and oars, cloth for sails, rope for rigging. The exact items and amounts are worked out
against the current item list when this is built; the fees are first guesses to tune against coin income.

## The boat as an item

A boat is an inventory item: a new item type `Boat` (category `Vehicle`), one item per boat model. That way every
existing path works for it without new code: loot, rewards, the token shop, saving, the inventory view, and later
the transmog and enchantment fields for customization.

- **Per character**, like the Sailing and Fishing skills it goes with.
- **Soulbound from the start** (decided 2026-10-26) for the quest's Rowboat. Crafted boats (phase 4) may be
  tradeable; that is decided when the shipwright is built.
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

### Guests aboard (discussed and built 2026-10-28)

The owner can take guests who tag along, up to the boat's seats. Seats belong to the boat type, so nothing is built
around two: the rowboat has 1 (the owner), the fishing boat 2 (the owner and one guest), and bigger player-owned
boats later (the sloop, or larger) can hold more. Each guest seat has its place on the boat and its own pair of oars
or a spot at the rail.

**Both must agree**, as with duels (`DuelHandler`: one request at a time, 30 s to answer, then it times out). A
player who just wants to travel never ends up in someone's boat by accident. It works both ways:

| Command | Who | What |
|---|---|---|
| `!boat invite <player>` | the owner | asks a player aboard |
| `!boat join <player>` | anyone | asks to come aboard that player's boat |
| `!boat accept` | whoever was asked | says yes to the request waiting for them, invite or join alike |
| `!boat decline` | whoever was asked | says no |
| `!boat leave` | the guest | gets off at the nearest dock (as does `!disembark`); ashore, no longer comes along |

- Asking needs both players on the same island and the owner ashore; a boat already at sea does not turn back. A
  full boat answers that there is no seat left.
- **No level needed** to come aboard (decided 2026-10-28), so new players can ride along with veterans.
- **The owner steers.** Only the owner's `!sail`, `!sail <island>`, `!sail stop` and `!disembark` move or land the
  boat. The guest's own sailing commands answer that they are a passenger (and say how to leave).
- **Fishing together.** When the owner anchors and fishes, guests can `!fish` too: a line over the side each.
- **Exp.** Everyone aboard earns Sailing exp while the boat moves.
- **Guests row**, and each one rowing makes the boat a little faster (decided 2026-10-28: a small boost, say +10%
  for the first guest and less for each one after, still within the boat's speed cap). On the fishing boat the
  guest sits at the front rowlocks and the front pair of oars comes on (`SM_Prop_Oar_01`, hidden for one aboard in
  `tools/sailing/make-boat-prefabs.cs`; the rowlock rings were kept for this).
- **It ends** when the boat docks for good: the owner lands, leaves the game, or goes to a raid or dungeon, and the
  boat docks at the nearest dock with everyone stepping ashore. A guest pulled away (raid, dungeon, leaving) simply
  drops off and frees the seat; nobody is left in the sea.
- **The owner's reward** for carrying guests (decided 2026-10-28; the ferry captain only makes the ferry faster, so
  this is new):
  - **Sailing exp**: about +10% per guest aboard on the owner's own Sailing exp, only while the boat moves, as all
    Sailing exp. It mirrors the guests' speed boost and grows with bigger boats.
  - **Titles** for guest trips (one trip = a guest aboard from setting out to stepping ashore): "the Ferryman" at 50,
    "Sea Captain" at 500, through the achievements on the quest tracker; titles already show on name tags.
  - **The captain's cut**: each fish a guest lands gives the owner a small chance of a bonus fish of their own,
    taken from nobody. Decided by the server, beside the guest's catch (the fishing task processor at sea).
  - **No fare.** Guests never pay coins; a ride stays a favour, not a trade.

**Built (2026-10-28).**

- Game: `BoatCrews` (requests, crews, seats per boat: rowboat 1, fishing boat 2, sloop 4), `OwnBoat` (guest seats,
  the fishing boat's front oars on while a guest sits under them, `SailingSpeeds.Boat` with the rowing boost and
  `OwnerExpFactor`), `FerryHandler` (the crew walks to the dock with the owner and boards as the boat comes
  alongside; a guest rides, earns Sailing exp, fishes over the port side at anchor and gets off; crews end when the
  boat is put away), `!boat invite/join/accept/decline/leave` in `BoatCommand`. Seats are `GuestSeat1`, `2`, `3` in
  the prefabs (`tools/sailing/make-boat-prefabs.cs`): the fishing boat's guest sits on the bench under the front
  oars, at its front edge clear of the mast; the sloop's stand on its deck. A guest trip counts after 150 m.
- Server: `BoatManager` keeps who is aboard whose boat (`api/boats/{owner}/guest-aboard/{guest}` and
  `guest-ashore/{guest}/{trip|none}`), the captain's cut (`GuestCaught`, from `FishingTaskProcessor` at sea: 20%
  before the usual one-in-two drop roll, about one fish in ten) and the `ferryman` achievement (50 and 500 trips:
  "the Ferryman", "the Sea Captain").
- Bots: only the `!boat` help text; both bots pass the arguments on and the game answers.
- Found while testing: at a high Sailing level an own boat circled its first waypoint for good (a turning circle
  wider than the distance to it); it now slows until the circle fits. Roaming (`!sail` with no island) goes at 45%
  of the boat's speed, a relaxed pace; a course to an island goes at full speed (decided 2026-10-28). An owner who
  weighed anchor kept the fishing pose while sailing on; back at the helm now.
- Not kept over a restart: crews live in the game only, so a guest comes back by their own boat or the ferry.

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

Answered 2026-10-26:

1. **Binding**: soulbound from the start, at least the boat the quest gives. Later boats that are crafted need not
   be soulbound.
2. **Fishing at sea**: both. The fish from land can be caught at sea too, and some fish only at sea.
3. **Speed**: by the player's Sailing level and the boat's tier, with a cap. The ferry is capped as well, since today
   it scales far too fast. See "Speeds" below.
4. **Riding along** and **storms damaging boats**: liked, not in the first version (notes below).

Notes from 2026-10-23:

- **Riding along on someone's boat**: the user likes it ("lots of fun") but not for the first version. It probably
  needs something more basic first, such as viewers forming parties (who sails with whom, who decides where to go,
  what happens when the owner leaves the stream). To be looked into separately.
- **Storms damaging or sinking boats**: liked, but not for the first version. The catch: weather is per stream and
  a streamer can turn changing weather off (and potato mode holds it fair), so damage tied to the weather that
  viewers see would hit players on some streams and never on others. If it comes, it should come from a server
  side sea state that is the same for everyone (the stream shows it if its weather is on), or be something a
  player opts into, like sailing into a storm for better fish.

## Speeds (decided 2026-10-26)

Today the ferry's speed is 20 plus the captain's Sailing level, with no cap. A 500 m leg takes 50 s with a level 0
captain, 8 s at level 100, 3 s at level 300 and 1 s at 999. On a high level stream the ferry barely moves before it
docks again, and its passengers barely get Sailing exp.

From now on, levels fill a curve towards a cap instead of adding speed without end:

    reach(level) = 1 - e^(-level / 150)      (0 at level 0, 0.49 at 100, 0.86 at 300, 1 at 999)
    speed = slowest + (fastest - slowest) x reach(Sailing level)

| | Slowest (level 0) | Level 100 | Level 300 | Fastest | Notes |
|---|---|---|---|---|---|
| Ferry | 20 | 49 | 72 | 80 | the captain's level; boost scrolls multiply on top, as today |
| Rowboat (quest boat, soulbound) | 12 | 18 | 22 | 24 | own boats hold a steady speed; the ferry's figures are its peak, it eases in and out and averages half |
| Fishing boat | 16 | 26 | 33 | 36 | crafted, phase 4 |
| Sloop | 20 | 35 | 46 | 50 | crafted, phase 4 |

Why these numbers: an own boat goes straight to the island and never waits at a dock, which is what owning one is
for. From Home to Heim the ferry sails round four legs (about 1900 m) and stops three times; the rowboat sails about
900 m. At level 100 the ferry takes about 80 s plus the stops, the rowboat about 50 s, the fishing boat 35 s and the
sloop 26 s. The ferry stays worth taking for its captain bonus and its boost scrolls, which are for the ferry only.
The constants live in one place in the game (`SailingSpeeds`) so they can be tuned.

## Build phases

1. **The boats and the ferry's cap.** Server: item type `Boat`, a `Boat` equipment slot, the three boat items, and
   the shipwright chain's end gives the Rowboat, soulbound (once per character). Game: the ferry's speed curve, the
   boat slot and the boat item (shown in the inventory, never drawn on the character).
2. **Sailing your own boat.** The sea graph, `!sail <island>`, wandering, `!disembark`, boats in the world, saving and
   restoring a player at sea.
3. **Fishing at sea**, and the sea-only fish (items, drops, cooking).
4. **The shipwright**: crafting the parts, `!boat build` for the fishing boat and the sloop, `!boat` on both bots.

## Built

**Phase 1 (2026-10-26).** RavenNest 62a84b65 and 4fe80baf, game c8193b2. Tested on the dev server: finishing the
shipwright chain gives the Rowboat, soulbound and unequipped; it reaches the game through the inventory feed.

**Phase 2 (2026-10-26).** Sailing your own boat, all in the game; the server only sees a player at sea as one on no
island, as on the ferry.

- `tools/sailing/bake-sea-chart.cs` bakes `StreamingAssets/Sailing/sea-chart.json`: a 10 m water grid over the
  sailable islands (deep enough, nothing standing in it, the sea's "Do Not Walk" box ignored) and each island's dock
  point, the end of the ferry's spline into it. `SeaChart` plans routes over it (A*, kept off the coasts, then pulled
  straight over open water): Home to Heim is 703 m, against the ferry's 1900 m loop. Rerun the tool when islands,
  docks or the ferry's splines move.
- `tools/sailing/make-boat-prefabs.cs` makes `Assets/World/Sailing/Boats` (Rowboat, FishingBoat, Sloop) from the
  PolygonPirates boats; `tools/sailing/add-boat-prefabs.cs` hangs them on the scene's GameState (`OwnBoatPrefabs`),
  since Resources is not versioned. The hulls ride high enough that the sea never shows inside them.
- `OwnBoat` sails a route at its tier's speed, turning like a boat, slowing through turns and coming alongside, and
  rides the waves near the camera. The rowboat's owner sits on the bench; the bigger boats are steered standing.
- `FerryHandler` takes the own boat whenever one is equipped: `!sail <island>` walks to the dock, the boat comes
  alongside and sails straight there; `!sail` wanders; `!sail <island>` at sea changes course; `!disembark` lands at
  the nearest dock. Sailing exp every 2.5 s while the boat moves. Raids, dungeons and Ravebridge take the player off
  and put them back aboard where they were.
- `OwnBoatLog` keeps each boat's place and course in `states/own-boats.json` (every 5 s, on landing, on quit), so a
  restart puts the player back aboard; older than a day, or unknown, they come back by the ferry.
- Chat: "You push off in your Rowboat, bound for Heim." and the ferry's arrival line, both in the Disembark group.

Tested in play mode: Kyo to Heim and landing, wandering, a restart at sea (back aboard, still wandering), and
`!disembark` to the nearest dock. Not yet: many players at sea at once, and the chat commands through a real bot.

**Phase 3 (2026-10-26).** RavenNest 8a23105e and the game commit after it.

- `!fish` on an own boat drops anchor where it is (no longer bound anywhere); setting fishing at anchor does not send
  the player ashore, as other tasks at sea do. A cast every 3 s over the side: the animation, the splash and Fishing
  exp at the full rate (no island chunk to scale it). `!sail` weighs anchor and wanders on; `!sail <island>` and
  `!disembark` work as at sea.
- While at anchor and fishing, the save says island 10 (`Island.Sea` on the server, `SeaFishing.Island` in the game,
  whose RavenNest.Models is older). The server's fishing then needs no island or level gate, and its catch includes
  the sea's own fish; island fishing spots filter them out (`FishingTaskProcessor.SeaOnly`).
- The sea's own fish: Raw Mackerel (15), Raw Tuna (60), Raw Marlin (150), Raw Anglerfish (260), Raw Giant Squid (450)
  and Raw Sea Serpent (800), each with a cooked and a burned dish, a recipe and effects a little above the land fish
  near its level; and a rare Message in a Bottle (from zerratar, who has more bottles). `!fish tuna` on land says
  it only bites out at sea. Icons made from the land fish's (hue shifted and turned round), for the game and the site.

Tested on the dev server: sailed out from Heim, `!fish raw mackerel` anchored the boat and a Raw Mackerel came in;
the save carried island 10 and Fishing.

**Phase 4 (2026-10-26).** The shipwright on Away, and `!boat` on both bots.

- Parts, crafted with `!craft` (Crafting recipes on the server): Rope (Crafting 20: 3 String), Sailcloth (30: 4 String,
  6 Hemp), Boat Hull (30: 20 Mystwood Logs, 6 Resin, 2 Iron Bars), Mast (35: 8 Pineheart Logs, 1 Rope, 1 Iron Bar),
  Large Hull (60: 25 Ironbark Logs, 12 Resin, 4 Steel Bars). String comes from Hemp by Alchemy. Wood Plank is
  obsolete, so the hulls use logs.
- `!boat build fishing boat` (Sailing 50, Fishing 30; a Boat Hull, a Mast, a Sailcloth, 4 Rope; 50,000 coins) and
  `!boat build sloop` (Sailing 100, Crafting 50; a Large Hull, 2 Masts, 3 Sailcloth, 8 Rope; 250,000 coins), only on
  Away where the shipwright works. The boat is given before the parts and coins are taken. Crafted boats are not
  soulbound (decided 2026-10-26), so they can be traded and sold on the marketplace.
- `!boat` says which boat is equipped and what it is doing (at the dock with its speed, sailing, roaming, at anchor),
  or how to get one; `!boat list` says what each boat needs. Server: `BoatManager`, `api/boats/blueprints` and
  `api/boats/{characterId}/build`; game: `BoatsApi`, `BoatCommand` (`boat_command`); bots: `!boat` in ROBot (`Boat`)
  and RavenBot (`BoatCommandProcessor`).
- Icons for the parts and repainted boat icons came from the Image Generator (Flare for new icons, Sunburst for edits).

## Customizing boats (decided 2026-11-01)

Synty's Pirate Realm (imported 2026-11-01, Assets/Synty/PolygonPirateRealm) has modular ships in three sizes: hulls,
fronts, middles and backs in two to four variants each, masts, rigging, bowsprits, rudders, seven figureheads and four
rams per size, plus fourteen sail materials, seventeen flags and five plank woods. The user decided:

- **Both ways to change a boat:** chat commands first, a boat designer on the website later.
- **Every look is earned through play;** nothing is bought.
- **Pirate Realm's ships become new, bigger boats above the Sloop.**

### What a look is

A boat's look is a short recipe kept on the boat's own inventory stack on the server (its Tag, which every stack
already has and boats, never stacked, can use): `look:1;hull=2;front=1;middle=3;back=2;figurehead=5;ram=0;sails=7;
flag=3;wood=2` (0 = none, where a part can be left off). It goes with the boat everywhere: every stream shows it the
same, and it stays with a crafted boat that is traded or sold. The game assembles the boat from Pirate Realm's parts
when it is launched (a BoatAssembler: the size's preset as the skeleton, each part swapped for its chosen variant at the
same place, sails, flags and planks by material).

- **Which boats:** the Pirate Realm boats are fully customizable: the Sloop moves to Pirate Realm's small ship, and the
  two new boats use the medium and large ships. The Rowboat and the Fishing Boat keep their models and take the flag
  (a pennant on a pole at the stern) and the sails (the fishing boat's) only.
- **Defaults:** every boat comes with variant 1 of everything, sails 1 and no flag.

### Earning looks

Unlocks are per player (all their characters), kept by the server (a BoatLookUnlock row per user and key, such as
`sails_7`, `flag_3`, `figurehead_5`). Proposed sources, to tune:

- **The regatta:** sails. A first place gives a sail not yet owned; a set number of races gives the regatta sails.
- **Treasure** (dug up, the pirates' chest): flags and figureheads, rarely.
- **Holding the pirates' raid:** the pirates' own black sails and flag.
- **Quests and achievements:** planks (wood), rams and hull variants (the shipwright's chain, the Ferryman and Sea
  Captain titles, Sailing levels 100, 200, 300).
- **Seasonal events:** special flags (a pumpkin flag at Halloween, a snowflake at Christmas).

### Commands

`!boat look` says what the boat wears, `!boat looks` what the player has earned, and `!boat sails 7`, `!boat flag 3`,
`!boat figurehead 5`, `!boat hull 2`, `!boat front 1`, `!boat middle 3`, `!boat back 2`, `!boat ram 1`,
`!boat wood 2` change one part (refused when not earned or not on this boat type). The change shows the next time the
boat is launched, or at once when it is out.

### The new boats

| Boat | Model | Sailing | Seats | Speed (m/s) |
|---|---|---|---|---|
| Brigantine | Pirate Realm medium ship | 100 | 6 | 24 to 60 |
| Galleon | Pirate Realm large ship | 200 | 10 | 28 to 70 |

Built at the shipwright on Away from crafted parts and a fee, like the Fishing Boat and the Sloop (the ferry's cap of
80 m/s stays the fastest thing at sea). New parts, crafted: Keel (Crafting 120), Canvas (Crafting 140, from Sailcloth),
Iron Fittings (Crafting 160, from bars). Seats follow BoatCrews.Seats.

### Phases

1. **Looks:** the BoatAssembler, the Sloop on Pirate Realm's small ship, looks on the boat's stack, unlocks on the
   server, the `!boat` look commands, and a starter set so they can be tried.
2. **Earning:** the unlock sources above.
3. **The new boats:** Brigantine and Galleon (items, parts, blueprints, speeds, seats, prefabs, icons).
4. **The website's boat designer:** a 3D preview, every earned look to pick from.

### Built: phase 1 (2026-11-01)

- Server: BoatLookUnlock (per user, the table is made at startup), BoatLooks (the parts per boat, what every boat
  comes with, the recipe), BoatManager.GetLook / SetLook / GrantLook, api/boats/{characterId}/look and
  api/boats/{characterId}/look/{slot}/{variant}.
- Game: BoatLook (the recipe), BoatParts (Assets/World/Sailing/BoatParts.asset), BoatAssembler (the Sloop built from
  Pirate Realm's small ship under its Model; sails and planks by material; the flag at the masthead, on the small
  boats on a pole at the stern), the Sloop on Pirate Realm's small ship (tools/sailing/make-pirate-realm-boats.cs, its
  Sloop Rig taken from the kit's preset), the looks applied at launch and at once while out, and `!boat look`,
  `!boat looks`, `!boat <part> <n|none>` (sail and planks also work).
- Free for every boat until phase 2 adds earning: variant 1 of every part (back 3, as the kit's preset), no
  figurehead or ram, sails 1 and 2, flags none and 1 to 3, woods 1 and 2.
- Tested with the dev server: !boat look, looks, a locked flag, a part a rowboat cannot change, a number out of
  range, a flag set and flown on a launched rowboat. The Sloop's parts were checked in renders; a real Sloop was not
  launched (no test character has one).

### Built: phase 2 (2026-11-01)

What earns which look (server: BoatLooks.ByProgress and the lists beside it, BoatManager.OwnedKeys and GrantNew):

- **The regatta:** the winner gets a sail not yet owned from sails 3, 4, 5 and 7 to 14; any other finisher one in ten.
- **Treasure dug up:** each digger has a 12% chance of a flag (8, 9, 11, 12, 14 to 17) and 4% of a figurehead (1, 3, 4,
  5, 7) not yet owned.
- **Holding the pirates' raid:** every defender gets the pirates' colours, the black sails (6) and the skull and
  bones (flag 7).
- **Progress at sea** (owned while it holds, counted over all of a player's characters, no row): Sailing 50 wood 3; 100
  ram 1 and back 2; 150 front 2; 200 wood 4, ram 2 and the second hull; 250 middle 2 and front 3; 300 wood 5 and ram 3;
  400 middle 3, front 4 and back 4; 500 ram 4. The shipwright's chain: flag 13. The Ferryman: figurehead 2; the Sea
  Captain: figurehead 6 and flag 10.
- No seasonal flags: none of the kit's seventeen is seasonal.

The chests and the regatta say which looks they gave (MangroveReward.Looks); the game puts a line in the banana
island's feed. Tested with the dev server: a regatta win gave sails 11, the pirates' chest sails 6 and flag 7,
`!boat looks` listed flag 13 from the finished shipwright's chain, and flag 7 could be set. The treasure's chance was
not rolled in a test.

### Built: phase 3 (2026-11-01)

- Server: the Brigantine and the Galleon (items, ItemType.Boat), their parts Keel (Crafting 120: 40 ebonshade logs,
  16 resin, 8 steel bars), Canvas (Crafting 140: 3 sailcloth, 2 rope) and Iron Fittings (Crafting 160: 6 iron, 4
  steel, 2 mithril bars); the shipwright's blueprints (Brigantine: Sailing 300, Crafting 120, 1,000,000 coins, a keel,
  2 masts, 4 canvas, 2 iron fittings, 12 rope; Galleon: Sailing 500, Crafting 200, 5,000,000 coins, 2 keels, a large
  hull, 3 masts, 8 canvas, 6 iron fittings, 24 rope); seats 6 and 10; their look slots (one hull each, as their
  rigging is made for it). Painted icons for the five, and a new one for the Sloop (it is a ship now).
- Game: BoatItems.Tier.Brigantine and Galleon, speeds 24 to 60 and 28 to 70, seats, sizes and turn rates; BoatParts
  per ship size (Small, Medium, Large); tools/sailing/make-pirate-realm-boats.cs builds all three rigs (everything on
  the preset's hull but the look's parts, the preset unpacked completely so the masts on the hull's locators come
  along) and boats (the Seat at the helm's own height, the guests on the main deck found by rays far above the world).
- Tested: a Galleon equipped in the game (not built on the server) launched with its look, black sails, skull flag,
  the owner at the helm; both ships checked in renders. Not yet tested: !boat build brigantine and galleon (no test
  character has the parts), guests aboard the big ships.

### Built: phase 4 (2026-11-01)

- The website's boat designer at /boats (Boats under "You" in the dashboard menu): every boat on all of the player's
  characters, the equipped one first, shown in 3D as the game builds it, and every part of its look as buttons
  (pictures for the sails, flags and planks). An owned part is saved at once; a locked one is only tried on in the
  view, with where it comes from ("Win the regatta at the banana island", "Reach Sailing level 200"). Drag to turn,
  scroll to zoom.
- The 3D view is three.js, the same local copy the clan cape editor uses (RavenNest wwwroot/js/three.module.min.js):
  wwwroot/js/rf-boat-designer.js builds a boat by the BoatAssembler's rules (the ship's parts, the rig, sails and
  planks by material name, the flag at the tallest mast; the small boats' flag on a pole at the stern). Everything is
  kept in Unity's space under a root mirrored in z, so the part matrices and the assembler's numbers are used as they
  are.
- The models: tools/sailing/export-boats-web.cs writes RavenNest wwwroot/boats/ from BoatParts and the small boats'
  prefabs: one file per mesh (positions in int16 over the mesh's bounds, normals in int8, uvs in uint16, indices),
  boats.json with the parts and materials, and the textures baked with their tints (512 px). About 11.5 MB in all,
  fetched only for the boat shown. Run it again whenever the boat parts change. The ships' set sails are skinned
  meshes (no bones; one blend shape, "Sails_Raised", at 0 so the sails are set); the export takes those too, or the
  ships showed only their jibs on the website (the game always had the full sails).
- Server: BoatManager.GetBoats(userId) and SetLook(userId, characterId, stackId, slot, variant) (any of the player's
  boats, equipped or not, the same ownership rules as `!boat`); BoatLookResult now says whose boat it is, and each
  slot where every variant comes from (BoatLooks.HowToEarn). BoatService in the website.
- Tested: all five boats rendered in the browser from the exported files (looks, sails, planks, figureheads, rams,
  masthead and stern flags), the swatches, and the website builds. Not tested: the page itself signed in (the local
  site's certificate is for ravenfall.stream), and whether a boat changed on the website while its owner is in a
  stream shows the new look before the game next loads the inventory.

### Sails in port (built 2026-11-01)

Pirate Realm's masts carry each sail twice: set ("..._Sail_Down_01", a skinned mesh whose one blend shape,
"Sails_Raised", hauls it up to its yard) and furled ("..._Sail_Tied_01"); the jibs and the spanker come as whole
pieces with a furled one beside them ("..._Tied_01"). BoatSails (on every ship, found by BoatAssembler) takes them in
over 2.5 s: the set sails rise on their blend shape, and three quarters of the way the furled ones (and the furled jibs
and spanker) take their place; setting them runs the other way. The furled sails wear the look's sailcloth with the
kit's wind turned off (BoatSails.Still), as the kit's own static sails do: they are lashed to their yards.

- A ship comes alongside furled and sets its sails as it pulls away from the dock.
- Under way (to an island, or roaming) its sails are set; in the last stretch before a dock (30 m, or one and a half
  ship lengths) it takes them in.
- At anchor (fishing at sea) they are furled.
- Landed, the ship lies at the dock with its sails furled for 20 s (OwnBoat.Moor), then goes; its owner sailing
  again takes it away at once, and so does leaving the game.

Tested in the editor: both ships' sails found (the Galleon 7, the Sloop 3), set, raising, furled, the furled ones in
the look's colour. Not yet tested in a live game: a ship sailing from dock to dock, fishing at anchor, the moored
ship, and the regatta's finish (every racer's ship lies at the Mangrove dock for 20 s, overlapping).

### The small boats' sail and flags (2026-11-02)

From the user's look at the website's designer: the fishing boat's sail could not be changed, the rowboat's flag pole
looked like a branch, and every flag hung centred on its pole or mast instead of beside it.
- The fishing boat's sail is now Pirate Realm's small ship's mast with only its fore-and-aft (gaff) sail, scaled to
  0.38 (tools/sailing/make-boat-prefabs.cs, `Only = "FishingBoat"` rebuilds it alone): its sails wear the look's
  sails (BoatAssembler.Paint) and furl in port (BoatSails), and the server's BoatLooks gives the Fishing Boat the
  ships' 14 sails besides its flag. The square sail on the yard was left off, far too wide for the boat.
- Flags fly beside what holds them, leading edge at the mast or pole, streaming aft: the ships' at the tallest
  masthead, the fishing boat's at its masthead too, over the top of its sail as on the Sloop (the user's pick; the
  gaff's peak was tried and looked detached), the rowboat's from Pirate Realm's straight banded post
  (SM_Prop_Flag_Post_04, standing upright) instead of the crooked one lying on its side. The website's
  rf-boat-designer.js does the same.

### The clan flag (2026-11-02)

The user's idea: a member of a clan can fly the clan's logo. The flag value 99 (BoatLooks.ClanFlag on the server,
BoatAssembler.ClanFlag here) is any member's while they are in a clan; `!boat flag clan` picks it, and the website's
Flag tab has a "Clan" tile. The game shows the plain dark flag (Flag_06) at once, then lays the logo (the one the capes
use: the owner's upload or their Twitch picture, versioned by its image id) as the flag's symbol: Synty's fabric shader
has a symbol overlay (the skull flags' emblems), switched on for the copy (`_USE_SYMBOL_OVERLAY`, the skull flags'
variant, so builds have it), with a clear symbol texture holding the logo where the flag cloth's shape sits on the
sheet (u 0.40 to 0.73, v 0.71 to 0.97). Out of a clan, or with the logos hidden, it stays the plain dark flag. Old
clients show no flag for it.
- Clan sails too (the user's next idea): sails 99 is the plain cream sails (ShipSails_01) with the logo laid as their
  symbol in the four places Pirate Realm's emblem sails put theirs (Sail_Symbols textures: 604,68-1432,960;
  660,1156-1376,1920; 1020,2516-2060,3636; 3288,3184-3904,3848 of 4096) and in the middle of the gaff sails' strip
  (u 0.81 to 0.99, v 0.6 to 0.8; drawn at 0.6 of its height across, the strip being stretched across the sail),
  which the emblem sails leave plain. `!boat sails clan`; the designer's Sails tab has a Clan tile. The value is
  BoatLooks.ClanLogo / BoatAssembler.ClanLogo, 99 for both parts.
- Clan colours (the user's idea): the clan owner picks the clan flag's and clan sails' cloth colour in the clan
  page's Look tab (any owner, not only patrons; a palette, the plain colours, or any colour), saved on the clan
  (FlagColor, SailColor) and sent with its ClanLook. The game tints the copy's `_Color_Tint` (and `_SSSTint`) at once,
  before the logo arrives; the website tints an uncoloured cloth the export writes for it (boats.json `clanCloth`, with
  the plain colours as `flagTint` / `sailsTint`). A boat already out takes new colours the next time it is launched.

### Sailing levels raised (2026-11-02)

The user: Sailing levels up by itself on a long `!sail`, and with the global exp multipliers players would reach the
old 30, 60, 100 and 200 too soon, so the Galleon should feel like a grind. The shipwright now asks Sailing 50 for the
Fishing Boat, 100 for the Sloop, 300 for the Brigantine and 500 for the Galleon (BoatManager's blueprints; the
Crafting levels, parts and fees are as they were). The level is asked when building; a built boat is soulbound, as the
rowboat is, so it can go to the player's other characters (the user: give it to your new character) but not be traded,
gifted or sold to anyone else.
