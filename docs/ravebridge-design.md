# Ravebridge

Phase 6 of the quests (docs/quests-design.md): Ravebridge, the tribute to RuneScape Classic's Lumbridge far to the
west, becomes an event island where viewers come along and the streamer plays by hand. Decided with the user on
2026-10-25:

- **Opening:** a streamer story unlocks it for good, after which the streamer can start the event whenever they like.
  Some world events also pull a stream there whether it is unlocked or not.
- **Everything at once:** the island, the event, playing by hand, the Ravebridge character and the island quests (6a
  to 6e) are built together.
- **Quests:** a mix of RuneScape tributes and quests of Ravebridge's own.
- **The end:** the event runs until the streamer ends it. Viewers can leave earlier.

## What is there today

`WORLD/Islands/Ravebridge`, inactive, about 210 x 340 m around (-1620, 5, 1640), on terrain of its own:

- **West bank:** the castle, with walls, round towers, a courtyard with fountains, a well, cannons and banners.
- **The river,** with a stone bridge and a wooden one.
- **East bank:** the farmer's house, a crop field, a cow field, chickens, two small houses, the goblin camp, and the
  desert's edge (Al Kharid) beyond a fence.
- **The goblins** are Synty character rigs without enemy components. The cows are plain models.

## 6a. The island

- **Island 9.** `Island.Ravebridge = 9` on the server. The game calls it `(Island)9`, like Mangrove's 8. The island
  gets an IslandController (Identifier "Ravebridge") with a sphere around it. It has no dock, so it can never be
  sailed to; only the event takes players there. It is open to every level with the full island factor, as Mangrove
  is (`IslandController.OpenToAllLevels`).
- **Navmesh:** its own NavMeshSurface and navmesh area, as Mangrove's (`NavMeshArea`, `NavMeshHelper`).
- **Terrain:** the Ravenfall/Terrain material of the other islands, so it gets the new look.
- **Missing places, from Synty presets and props:**
  - the general store (a house, a shopkeeper and a stall);
  - the church and graveyard (preset church and gravestones);
  - the castle kitchen (a range and the cook);
  - the windmill north west (preset windmill);
  - the swamp to the south (swamp growth and darker ground) with its mine;
  - a few more houses;
  - a chicken coop on the farm.
- **Training spots:**
  - woodcutting: the trees round the castle;
  - mining: the swamp mine;
  - fishing: the river, by the swamp;
  - farming: the crop field;
  - gathering: the wheat field and the coop;
  - cooking: the castle range;
  - crafting and alchemy: in the general store's yard;
  - fighting: goblins across the river, cows in the cow field, and the ghost in the graveyard at night.
- **Enemies:** the goblin rigs get the pirates' enemy components (Humanoid Enemy). The cows are copies of the main
  island's cow enemies.

## 6b. The event

`RavebridgeManager`, an `IEvent` (one event at a time, like dungeons and raids).

- **Start:**
  - The streamer starts it from the Ravebridge bar or with `!ravebridge start` (broadcaster or moderator).
  - Only where the stream has the `island_ravebridge` unlock, unless it is forced.
- **Forced starts:**
  - a server game event (so a world event can pull every stream there);
  - an admin hotkey;
  - a rare random "goblin uprising" in any stream.

  A forced event ends after 30 minutes if the streamer never takes part. Every other event runs until the streamer
  ends it.
- **Joining:** `!ravebridge` joins and `!ravebridge leave` leaves. On joining, the player's state is saved (island,
  position, task, ferry), as the stream war's `PrevPlayerState` does. The player is taken to the island's spawn and
  keeps training their task there.
- **Leaving and ending:** players are brought back to where they were when they leave or when the event ends.
  Leaving the game counts as leaving.
- **On the island:**
  - Dying sends the player back to the island's spawn, not home.
  - Saves keep the island and position the player came from (`TcpApi.GetPosition`, `PlayerSaveSource`), so a crash
    brings them home.
- **Blocked while on Ravebridge,** behind the new flag `InRavebridge` (checked wherever `InWar` is): ferry, `!sail`,
  teleports, onsen, arena, duels, dungeons and raids.
- **Helper commands:**
  - `!ravebridge follow` fights beside the streamer's character, on its target.
  - `!ravebridge heal` heals it; healers only.
- **HUD:**
  - a Ravebridge bar (who is there, how long it has run, Play and End buttons);
  - a banner when the event starts;
  - the island observe camera stays on Ravebridge while it runs.

## 6c. Playing by hand

`ManualPlayerController`, the stub that is already there (`PlayerController.Controlled` skips all automatic play).

- **Moving:** WASD moves relative to the camera (`NavMeshAgent.Move`). Clicking the ground walks there.
- **Acting by clicking:**
  - a tree or gather spot (picked by screen distance, since they have no colliders);
  - a rock, fishing spot, farm patch or station;
  - an enemy (attack until it dies);
  - an NPC (talk).

  The click sets the target and the chunk's own task runs it: the same range checks, experience and items as
  automatic play. Nothing else picks a new target.
- **Camera:** a new `Controlled` camera follows the character. Right mouse turns it and scroll zooms. There is no
  auto-rotation, island tour or Tab switching.
- **Turning it off:** Esc leaves control.
- The same controller is what hero mode will build its skills and dodge on.

## 6d. The Ravebridge character

- **Character select:** when the streamer presses Play, they pick one of their own characters in the stream (saved
  to the server as usual) or their Ravebridge character.
- **The Ravebridge character:**
  - It is made for the island only, levels up there and can never leave.
  - It is a local player (`PlayerController.IsLocal`): never saved to the server, never listed in the session, and
    with no server item calls.
  - It is saved in `states/ravebridge/character.json`: name, look, skills, inventory, quest progress and where it
    stood.
- **Export and import:** export writes a copy next to the game, named with the date, and puts it on the clipboard.
  Import reads one back from the clipboard or from `states/ravebridge/import/`.
- **Making one:** a small creator with a name, gender, hair, head, facial hair and colours, using the bots'
  appearance ranges, with a live preview.
- **Items:** its items come from the island itself (a small local drop table per skill), using the server catalog's
  items (logs, ore, fish, eggs, milk, flour).

## 6e. NPCs and island quests

- **NPCs:** an `IslandNpc` component (a name and a dialogue) with a speech bubble. A character played by hand talks
  to one by clicking it within reach. The dialogue window is the story panel's look: the NPC's line and answer
  buttons.
- **The quest engine** runs in the game. The data is `StreamingAssets/Ravebridge/quests.json`, with English and
  Swedish text in it. Quest progress is saved per character: in the character file, or for the streamer's own
  characters in `states/ravebridge/quests-<characterId>.json`.
- **Quests:**
  - **The cook's big day** (tribute: Cook's Assistant). The Duke's cook needs an egg (from the coop), a bucket of
    milk (from a cow) and a pot of flour (wheat through the windmill) for the Duke's birthday cake.
  - **The restless ghost** (tribute). The graveyard ghost cannot rest. The priest gives an amulet so you can
    understand it, and its skull is in the swamp mine.
  - **Goblin diplomacy** (tribute). The goblins are about to go to war over the colour of their armour. Bring dyed
    armour until they agree, and make sure they never do.
  - **The windmill that won't** (Ravebridge's own). The miller's sails are stuck because the goblins stole the cogs.
    Until it turns there is no flour, so the cook waits on it.
  - **Mending the old bridge** (Ravebridge's own). The wooden bridge has rotted. Cut logs and mine nails, and the
    shortcut across the river opens again.
- **Rewards:** experience, items, and a "quest points" count. Finishing every quest gives the streamer the title
  "of Ravebridge" (for their own characters, through the server's title unlocks).

## The story that opens it

A new village story, "the road west". It can only begin once the runestone story is finished; that is a new `Requires`
field on stories. The runestone hums toward the west:

1. Talk to the runestone.
2. Find a torn map while gathering.
3. Clear dungeons.
4. Deliver planks and rope for the crossing.
5. Talk again.

It unlocks `island_ravebridge` and a decor piece: a signpost that reads "Ravebridge, a long way west".

## Later

Rebirth and the endgame for characters with every skill at 999, to work out together. Hero mode (phase 7) builds on
the playing by hand from 6c.
