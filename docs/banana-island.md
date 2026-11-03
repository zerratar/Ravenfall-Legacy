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

**Status: done (playtested 2026-11-03).** The user's call: it is for a laugh, not a AAA racer, and good enough to ship;
revisit once real streamers have played it. Possible later: racer commands (tack, line choice), seasons, per-tier
records, rivalries, an own-key wheel grip on boats added later (ReadWheel handles them already).

- A boat race round the island for own boats. `!regatta` joins (an own boat equipped), the broadcaster starts it
  with `!regatta start` (without their own character joining; with no sign-up open it opens one as `!regatta`
  does); it also starts by itself when enough boats have joined.
- A race takes at least two boats, also when the broadcaster starts it (the user, 2026-11-03): `!regatta start`
  with fewer leaves the sign-up open, and a sign-up that ends with fewer is called off. If racers drop out before
  the gun and one is left, it is a time trial: the best time goes on the board but not a win, and the server pays
  the lone finisher the back of a race's prize, with no sail (a race takes two finishers).
- Nobody has to sail to the island to race: the boat comes for them wherever they are, and a few seconds after the
  race (when the race camera lets go) they go back (the user, 2026-11-03): to the spot on the island they were on,
  or, if they were out sailing their own boat, aboard it where it was, sailing on. Not those who dropped out, who
  were on the island or sailing to it anyway, or who went elsewhere since. The public ferry's passengers cannot race.
- Gags, only to look at (the user, 2026-11-03): a racer goes overboard and swims ahead towing the boat on a rope, or
  becomes the propeller at the stern; while a strong tailwind blows they hang on to the stern rail by their hands, the
  body flapping out behind like a flag. A bottle gives the first two now and then (12%); viewers send them free with
  !regatta swim / propeller [name], once in 30 s each, a racer once in 20 s. The race itself does not change.
- The race camera swings up to look down on a shot when a boat's sails would fill the middle of it (one passing the
  lens, or the camera close behind another), and back down once the view is clear.
- Racers signed up or still racing are left out of raids' and dungeons' auto-join, which would put the boat away and
  knock them out.
- The racers' boats line up off the island, sail round buoys and finish at the jetty. Speed is the boat's own
  (Sailing level and boat tier, capped), with wind gusts per leg so a lower boat can still win.
- Places pay coins and Sailing exp (the server: api/game/regatta-reward). Best times and wins on a board at the
  jetty (kept by the game, like the tavern games' boards).

#### Pickups, hazards and meddling (built 2026-11-01)

Asked for 2026-11-01: make the race exciting to watch, with its own camera, random obstacles, Mario Kart style
pickups, and viewers in chat interfering.

- **The start:** the boats lie at the line with their sails furled for 5 s, the bar counts 3, 2, 1, the war horn
  sounds and every boat sets sail at once (`OwnBoat.Held`). The boats sit abreast with 4 m between the hulls, spaced
  by each boat's beam.
- **Lanes:** each racer sails up to 15 m to one side of the course (`OwnBoat.Lane`). The boat steers its course as
  before and sits that far to the side of it, its bow turned a little into a lane change.
  - Every few seconds a racer moves to a new lane, often towards a bottle ahead.
  - A racer alongside or overlapping another moves over, so the boats sail beside each other, not through each other.
  - On the last leg the lanes close up towards the dock point.
- **The order:** a buoy counts as rounded within 35 m of it. The order (the bar, the camera, the cannon's target) is
  buoys rounded, then the distance to the next one.
- **Dropping out:** a racer whose boat is sent anywhere else (`!sail`, `!disembark`, `!fish`; `OwnBoat.CourseVersion`)
  is out of the race, so nobody sails straight to the jetty past the buoys. Finishers lie at the dock for 3 s, not the
  usual 20, as every racer finishes at the one dock point.
- **Mystery bottles:** a row of four across the middle of every leg but the last, back 8 s after one is taken. A
  racer carries one item at a time and uses it after a moment. What is in a bottle depends on the racer's place:
  - the front third: barnacle shield 40%, banana peel 45%, tailwind 15%;
  - the middle: tailwind 30%, cannonball 30%, peel 20%, shield 20%;
  - the back third: tailwind 45%, cannonball 40%, shield 15%.

  A tailwind is 1.5x speed for 4 s. The cannonball goes at the boat ahead (the leader fires back at the second). The
  shield takes the next hit within 15 s. The peel is dropped behind the boat; the next boat over it spins round.
- **Hazards:** 2 to 4 reefs (60%) or patches of floating wreckage on the legs round the island, away from the bottle
  rows. A racer that sees one in its lane steers round it at a chance of 20% at Sailing level 1, rising with the level as boat speed does
  (SailingSpeeds.Reach) to 80%: about 50% at level 100, 70% at 300.
- **Hits:**
  - cannonball: 45% speed for 2 s;
  - powder keg: spins round, then 35% speed for 2.6 s;
  - peel: spins round, then 50% speed for 1.4 s;
  - reef: 40% speed for 1.6 s;
  - wreckage: 60% speed for 1.2 s;
  - the Kraken: 5% speed for 3.2 s, shields or not.

  A hit boat cannot be hit again for 4 s. It sails through a keg or a peel meanwhile, which stays for the next boat.
  A hit slows the boat but does not blow out a tailwind it has (a viewer may have paid for it).
- **Viewers meddle from chat** (anyone playing who is not racing):
  - `!regatta keg [name]` lobs a powder keg in front of a racer. A keg is live once it lands, and is half as easy to
    steer round as a reef.
  - `!regatta wind [name]` gives a racer a tailwind.
  - Without a name it picks a random racer, and is free once every 60 s per viewer. Before the free one is back it
    costs 500 coins; aimed at a name it always costs 2000. A paid one first replies with the price, and the viewer
    sends the same command again within 30 s to pay.
  - The coins come off the game's balance at once, so a second command cannot spend them again. Then it happens, and
    then the server takes them (api/mangrove/{characterId}/spend/{coins}, at most 10000). A race that ends meanwhile
    never charges for nothing, and coins the server did not take are given back.
  - A keg cannot be aimed at a racer hit in the last 4 s, or aimed at by a keg in the last 8 s; a random keg picks
    only racers free of both, and gives its racer the same 8 s rest.
  - Viewers throw at most 2 + the number of racers kegs a race.
  - Racers, also those who dropped out or finished, cannot meddle until the race is over.
  - A meddle whose coins the server could not take says it was free, not "paid".
  - `!regatta kraken` (the broadcaster or a moderator, once a race): the Kraken wakes under the leader. Its three biggest
    arms burst up in front of the bow and smaller ones on both sides of the boat; it roars from below (the picture
    shakes), curls them over the boat and holds it about 3 s while they wave, and they sink back. Its arms are Pirate
    Realm's tentacle (KrakenTentacle waves the joints); the roar is Assets/Sounds/Kraken Roar.mp3, a bear and a tiger
    roar pitched down and layered with ffmpeg. Woken just before the race ends, it still lets go and sinks.
- **The race camera** (RaceCamera; the setting Game > Observer > Regatta camera, on by default): a director that
  cuts between shots.
  - The start line from ahead and to one side, every bow in the picture, while the boats wait.
  - Then mostly the leader from behind, close battles from abeam, a wide orbit over the leading pack, and now and
    then another racer.
  - A fixed camera at each buoy as the leader rounds it, and a close look at a boat that was just hit.
  - Once the leader is on the last leg, a camera out on the water beside the way in to the dock, which stays until 6 s
    after the race.

  It only takes over when nothing bigger has the camera (a raid, a dungeon, the arena, a war, the free camera, a
  character played by hand). Players joining or cheering do not take it away; any camera key does. Afterwards the
  camera goes back to the islands or the players.
- **Props:** tools/mangrove/regatta-props.cs makes Resources/Mangrove/Regatta Props (RegattaProps) and the things in
  Resources/Mangrove/Regatta from Pirate Realm's bottle, barrels, crate, banana, cannonball, spiky rocks and effects,
  the tentacle, the war horn and the Kraken's roar.
- Splashes, blasts and auras are left out in potato mode.
- First guesses to tune: the prices, the free minute, the item odds and strengths, and the dodge chances.

#### Balance from the design review (2026-11-02)

A reviewer found boat speed decided every race (a galleon about five times a rowboat, races of 17 to 30 s, a pickup
worth 1 to 2 s), so the biggest boat always won and the pickups only shuffled the places behind it. The user asked for
the changes before playing it.

- **Race speed:** in the race a boat goes at `30 + (its own speed - 12) * 0.25` (`Regatta.RaceSpeed`, set as the boat's
  pace): 30 m/s for a new rowboat, 44.5 for a master's galleon. The order still follows boat and level, by half again at
  most instead of almost six times. Out of the race boats go at their own speed.
- **Two laps** (`Regatta.Laps`) round the same buoys: about 60 to 90 s. Buoys, bottle rows and hazards are placed once
  and sailed through on both laps.
- **The end:** once the winner is in, the rest have 25 s ("The winner is in! The rest have 25 seconds to finish."); then
  those still sailing are placed as they lie, without a time. They are paid and counted as races on the board.
- **Bottles:** each racer takes each bottle once a lap, a taken bottle shows again after 1.5 s (was 8), and the pickup
  reach is 4 m for every hull (a galleon's beam used to catch twice as much). The first boats through a row no longer
  take every bottle in it.
- **Catch-up:** the back third's tailwind is 1.7x for 5 s (others 1.5x for 4 s), and 15% of its cannonballs are chain
  shots at the leader instead of the boat ahead.
- **The Kraken** holds its boat at 15% speed (was 5%), so it costs the leader its lead but not the race.
- **Viewers' wind:** a racer gets it at most every 8 s and twice a race. Aiming costs 2000, doubling with each aimed one
  a viewer sends in the race (4000, then 8000).
- **The pot:** half of every paid meddle goes in the session's pot on the server, shared by the next race's podium
  50/30/20; chat hears "Chat's meddling put N coins in the prize pot". Free meddles get no chat reply (the feed shows
  them), to spare the bot's message budget.
- **Prizes:** `(500 + Sailing level * 5)` times the place's factor (was `300 + level * 10`: the high levels already win
  more often), plus the pot share. Second and third get a new sail at 30% (others 10%, the winner always).
- **Standings on screen:** every racer in order on the right (MangroveView, `Regatta.Standings`), with icons for the item
  it carries, a raised shield or a tailwind, and its lap or finish time. The bar keeps the time, the leader's lap and how
  to meddle.

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
