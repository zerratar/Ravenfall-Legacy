# Banana island: what the Mangrove island offers

Status: decided 2026-11-01, for v0.9.6. The user asked for all of it, built in phases (see "Build phases").

## Why

The Mangrove island opens when the streamer finishes the "bananas" story ("insufficient bananas", the monkey chief,
150 raw sprat, two raids, naming the chief Bonzo, Sir Bananas or Kevin). Once open it was a normal training island:
Away's training spots, weaker pirates, +10% exp, open to every level. People who saw screenshots asked what it
offers. The island was built around monkeys and bananas, and neither existed in the game, only in the story text.

## What it offers now

### 1. Monkeys

- A troop of monkeys lives on the island: in the palms, on the boardwalks, round the stilt huts. They amble, sit,
  beat their chests and run.
- The monkey chief stands on the jetty, under the name the streamer gave him in the story. He is an island NPC with
  speech bubbles: he comments on the banana pile, the pirates and whoever is training nearby.
- **Monkey business:** now and then a monkey runs up to someone training on the island, snatches something
  ("raw shrimp", "a log", "their hat") and runs off. The player chases it for a few seconds and either gets it back or
  the monkey eats it. It is only a show: nothing leaves the inventory, the chase costs a few seconds of training.
  The line shows in an on-screen feed, never in chat (chat is scarce, docs/twitch-message-budget.md).
- Temporary model: the Low Poly Animated Animals gorilla, small and brown for the troop, big for the chief (the user
  is looking for a real monkey).

### 2. The banana pile and the sign

- The sign over the island no longer flips to "sufficient bananas" once and stops. It shows the monkeys' banana pile,
  which they slowly eat:
  - **Abundant bananas**: happy monkeys, never steal, +5% more exp on the island.
  - **Sufficient bananas**: as before.
  - **Barely sufficient bananas**: monkeys steal now and then.
  - **Insufficient bananas**: grumpy monkeys steal often (the island stays open; the story is done).
- The pile grows when players gather on the island (each gathering on the island adds to it) and when viewers feed
  real bananas with `!banana feed [amount]` (a Golden Banana fills it right up).
- The pile is kept per streamer by the game (it is the stream's own island), not on the server.

### 3. Banana items (server)

- **Banana**: gathered only on the Mangrove island (Gathering, level 1).
- **Banana Bread** (Cooking 60: banana, flour, sugar, eggs; can burn) and **Banana Split** (Cooking 320: banana,
  milk, chocolate, sugar), food with effects.
- **Golden Banana**: a treasure-only trophy; fed to the monkeys it fills the pile.
- **Bananarang**: a treasure-only bow, the banana is held like a bow. A joke, with real stats.
- **Monkey** pet: a treasure-only pet (temporary gorilla model).

### 4. Pirate treasure

- Mangrove pirates sometimes drop a torn treasure map (and a soggy one sometimes turns up fishing the lagoon). An X
  appears on a beach or in the shallows of the lagoon.
- The finder walks over and digs; anyone else on the island joins with `!dig`. After the dig a chest comes up and
  everyone who dug gets coins and a roll on the treasure table (the server: api/game/treasure-chest).
- One map at a time.

### 5. The regatta

- A boat race round the island for own boats. `!regatta` joins (an own boat equipped), the broadcaster starts it
  with `!regatta start`; it also starts by itself when enough boats have joined.
- The racers' boats line up off the island, sail round buoys and finish at the jetty. Speed is the boat's own
  (Sailing level and boat tier, capped), with wind gusts per leg so a lower boat can still win.
- Places pay coins and Sailing exp (the server: api/game/regatta-reward). Best times and wins on a board at the
  jetty (kept by the game, like the tavern games' boards).

### 6. The pirate raid

- When the banana pile is big, pirates come for it: a pirate ship anchors off the south beach and waves of pirates
  row ashore and go for the pile in the stilt village. The broadcaster can also call them with `!pirates`.
- Everyone training on the island fights them automatically. The streamer can take their own character by hand and
  defend the village in hero mode (the same hero controller as Ravebridge).
- Held: the pile is kept and the defenders share a chest. Lost: the pirates carry half the pile off.

## Build phases

1. Banana items, Mangrove-only bananas, recipes, treasure table, endpoints (RavenNest).
2. Monkeys, the chief, monkey business, the on-screen feed, the pile and the sign (game) and `!banana` (bots).
3. Pirate treasure and `!dig`.
4. The regatta and `!regatta`.
5. The pirate raid and hero mode on the island, `!pirates`.

## Built (2026-11-01)

All five phases in one go: RavenNest 1b98f933 (items, recipes, Mangrove-only bananas, MangroveManager and
api/mangrove), game 165f2f8 (everything in the game), RavenBot 769b83a (!dig, !banana, !bananas, !regatta, !pirates on
both bots).

- Scene and models: tools/mangrove/mangrove-monkeys.cs makes the monkey, the Monkey Pet (and its animator), the
  Bananarang, the treasure chest, the regatta buoy and the pirate ship, and puts the troop, the chief and the regatta
  board on the island. Prefabs and Resources are not in git (as everywhere in the project); rerun the tool to remake
  them.
- Tested in the editor with bots (the remote bot disconnected so nothing reached chat): monkeys amble and steal
  (and the chase), the feed, the sign's moods, a treasure found off a pirate and dug up, a regatta of three fishing
  boats (31.5 s, 32.0 s, 35.1 s, board updated, everyone landed on the island), the pirates' raid (landing, waves,
  defenders sent at them).
- Not yet tested: the server's rewards (the running dev server is older than 1b98f933), !banana feed, the chat
  commands through a real bot, hero mode defending by hand on the island, the Monkey Pet and the Bananarang in game.
- First guesses to tune: map chances (1/120 pirates, 1/900 fishing), the pile's numbers, pirate strength (0.55 of the
  defenders' average combat level, captain 0.9), the regatta's radius (150 m) and gusts (0.82 to 1.18).
