# The Tower as worlds, and gates beside it (proposal, 2026-11-23)

A proposal for v0.9.8 and after, from a talk with the owner about how the tower should feel. When it was written
nothing of it was built; since 2026-11-24 its shape is (section 12: what v0.9.7 has of it, as built). It builds on docs/tower-design.md (the tower as it is) and changes how the climb is shaped, how dungeons and
raids fit in, and how the tower shows up in the main world.

Inspiration, in short:
- **Sword Art Online (Aincrad):** 100 floors, each floor a whole world with towns, fields, quests and a labyrinth
  that leads to the floor boss. The boss is beaten once, by a big raid; then the next floor opens for good and the
  floor below stays a place to live, train and shop. The quarter floors had much harder bosses, and the player who
  landed the last blow on a boss got a rare item.
- **Solo Leveling:** gates open at random around the world, ranked E to S, each a dungeon with a boss. A gate left
  too long breaks, and its monsters spill into the world. Rare red gates trap whoever goes in until the boss dies.
  It also has a tower (the Demon's Castle) with bosses at milestone floors: a big climb plus random gates.

The owner picked the mix of both: **the tower is the big shared story of a stream, done the SAO way; gates are the
random daily action, done the Solo Leveling way; and the two feed each other.**

## 1. Words: a band becomes a floor

What the tower calls things today, and what this proposal calls them:

| Today | Proposed | What it is |
|---|---|---|
| A band of ten floors (Overgrown Halls, Frozen Vault, Storm Spire) | **A floor** | A whole world with its own town, biome, monsters, element, pets and story |
| A floor (1 to 30) | **A part** of a floor | An area of that world: a courtyard, a forest path, a hall, the ramparts |
| The boss of floor 10, 20, 30 | **The floor boss** | The big fight that opens the next floor |
| Other floor bosses | **Mini bosses** on some parts | Not on every part any more |

So the tower today has **3 floors of 10 parts**, and band 4, the Dark Fortress, becomes **floor 4**. "Floor 21" in
the Legendary rules becomes "floor 3, part 1", and so on. Players never see two numbering schemes: the switch is
done once, everywhere (game, server, website, titles, chat, docs).

## 2. A floor is a world you explore

- **The town is always open** once the stream reaches the floor. It is the floor's home: NPCs, the vendor, resting,
  fishing and gathering, town quests for each player, the floor's pets to tame.
- **Parts open one by one** as the stream meets each part's **objective**. The parts of a floor are areas of one
  world, laid out as rooms and paths the group moves through (the multi-room layouts being built now), some
  indoors and some outdoors.
- **No automatic march:** clearing a part's fight does not throw everyone into the next one. The next part opens when
  its objective is done, for example:
  - beat the part's mini boss;
  - a town NPC's quest (the smith needs 500 Shadowoak logs to forge the key to the inner gate);
  - an item only a gate drops (see section 4), like a Sigil of the Hall;
  - explore: find three hidden things on the part (as the floor quests in the tower design already suggest);
  - defeat a number of the part's monsters while training there.

  Objectives must move forward while players idle, since Ravenfall is played idle on stream: counts, deliveries and
  gate drops all do; nothing needs a player to click at the right moment.
- **Mini bosses** guard some parts (every third or so), and the **floor boss** waits at the end of the last part.
  Fewer bosses than today, and each one a bigger moment (cutscenes, as the tower already has).
- **Milestone floors** (every fifth, then 25, 50...) have extra hard floor bosses, like SAO's quarter floors.
- **Last hit:** whoever lands the floor boss's last blow gets a small extra reward (a title or cosmetic, not power,
  so nobody fights over it).

## 3. A cleared part, or floor, is a place to train

Once a part is cleared, players can go there freely, like sailing to an island:
- **Training as on an island:** the part's monsters for combat, its resource spots for gathering, fishing spots;
  each floor has a level range like the islands' requirements, and the experience follows the island rules
  (no lost training time, see the memory on it).
- **Resting in the town** works as in the Onsen and the tower towns today.
- **Joining the push:** when the stream works on the current part, the players on cleared parts and in the town can
  join with `!tower`, as now.

This turns each floor into a new Ravenfall world with its own things to do, which is what the owner wants: "making
each band a new ravenfall world which they explore by clearing floor objectives".

## 4. Gates: dungeons reworked

Today's dungeons already work like gates: they pop up, people join, there is a boss. The proposal makes them part
of the tower's world:

- **A gate is a rift from a tower floor** the stream has reached. It opens on one of the islands as a visible portal,
  and its dungeon looks like that floor (Overgrown, Frozen, Storm, Dark Fortress...) with that floor's monsters.
- **Ranks E to S.** A gate's rank comes from how far the stream has climbed and how strong the joined players are.
  Higher ranks open as the stream climbs.
- **Variety:** built from the floor's own room kit (the multi-room system), so each gate is a different string of
  rooms, and gates have different goals:
  - kill the boss (the classic);
  - survive waves for a time;
  - a treasure vault (loot, few fights);
  - escort a lost NPC out;
  - a race against the clock.
- **Red gates** (rare): nobody leaves until the boss falls; harder, better loot.
- **Rewards connected to the climb:** gates drop the floor's materials, its pet eggs, its gear, and **key items the
  tower's parts ask for** (section 2). That is how gates feed the tower.
- **A gate left too long breaks** (see raids, below).

## 5. Breaks: raids reworked

Today a raid boss appears on an island and everyone joins. The proposal: a raid is a **gate break**.
- When a gate is left uncleared, or now and then by itself, a **break** happens: the gate's boss, or a horde of the
  floor's monsters, comes out onto the island. Players on that island are in it; others can sail or `!raid` over.
- The break's boss is the gate's floor and rank, so raids get the same variety as gates, with that floor's look and
  element.
- **Dropped from a break:** the floor's materials and, rarely, a key item, as gates do.
- Streamers who dislike surprises get a setting (as Ravebridge uprisings have): breaks off, gates just close.
- The existing raid code stays the engine: the change is where bosses come from, what they look like and what they
  drop.

## 6. The tower in the world

A **huge tower stands in the main world**, seen from every island: on its own islet in the middle of the sea, or
rising from the clouds behind the islands.
- **Its sections look like its floors**, stacked: overgrown stone at the base, frost above it, storm crowns, the dark
  fortress, then floors not reached yet hidden in cloud.
- **The stream's progress shows on it:** the floors the stream has cleared are lit; the current floor glows and
  pulses; a gate opening anywhere sends a thin beam toward the tower's floor it came from.
- **Going there:** the ferry or a boat can visit its foot (the tower's entrance plaza: the leaderboard, a statue for
  the stream's best climbers), and `!tower` goes in.
- Built in Blender from concepts first (memory: concept then Blender), one model with a section per floor so new
  floors are added on top. It follows the potato mode setting (a lower detail model, no glow animation).

## 7. Per stream progress, and a little shared

Progress stays **per stream**, as the owner concluded: each stream has its own climb, its own pace and its own story,
and a stream that starts later still gets to explore every floor itself. Shared progress would let the biggest
streams decide everything for everyone, and a new stream would never see a floor fall.

What all streams do share:
- **The tower leaderboard** (website and an in-game board at the tower's foot): the furthest floor and part per
  stream, and the time it took.
- **World firsts:** the first stream to beat each floor boss is named in chat on every stream, on the website, and
  carved into the tower's plaza for everyone. A shared race without shared progress.
- **Each player's own record:** the furthest part a character has fought in, on any stream (already kept as the
  "climber" record), used for titles and the Legendary rules.

## 8. What happens to today's content

- **The 30 existing floors become floors 1 to 3, with 10 parts each.** Their layouts become parts; their towns
  (Mossgate, Rimeholt, Galewharf) become the floors' towns; most of their bosses become mini bosses, the 10th,
  20th and 30th stay floor bosses. Stream progress maps straight across (a stream on floor 14 is on floor 2, part 4).
- **The current one-arena layouts** stay as parts. The multi-room layouts are added over time, so each floor gets
  some parts that move through rooms.
- **Floor objectives** reuse the stream quest system (as the tower design planned) and the town quests.
- **Raids and dungeons keep working** until their rework lands; then they become breaks and gates.

## 9. Suggested order

1. **v0.9.8, the worlds:** the floor and part words everywhere; part objectives (mini bosses, quests, deliveries,
   gate items later); free entry and training on cleared parts; floor 4, the Dark Fortress, built the new way
   (its town, the ghost quarter, and parts with rooms); the leaderboard and world firsts.
2. **Then gates:** dungeons become ranked gates in the look of a reached floor, with the goals above, and the key
   items for the parts.
3. **Then breaks:** raids become gate breaks.
4. **The tower in the world:** whenever it is ready (it only needs the progress data, which exists).

## 10. Questions to settle

- How many parts per floor for new floors: ten as today, or fewer, bigger parts (six to eight)?
- How hard should part objectives be to finish while idle: an evening per part, a few days per floor?
- Should cleared parts be free for any player, or only for players whose levels fit the floor (like island
  requirements)?
- Gate ranks: from the stream's climb only, or also from the joined players' levels?
- Where the tower stands in the world, and how big.
- What the last hit gives.

## 11. Review against the tower as built, and what to do in v0.9.7 (2026-11-24)

The owner (2026-11-24): if the tower ships one way in v0.9.7 and changes shape in v0.9.8, players have to unlearn it.
So check the plan hard, and if it holds, do the shape now even if v0.9.7 takes longer. This section is that check.

### What the tower already does (so it is not new work)

- Progress per stream, world firsts and a leaderboard (server `StreamTower`, `TowerClear`; website /tower).
- Quests before each boss (two kill quests per floor), the boss opened by the streamer or one voice in chat.
- A town per band with people, personal quest chains, a place to train every skill and a resting place.
- Training on floors already reached (`!tower floor 7`), but only for the whole group at once.
- Level sync: on a floor everyone fights at most at the floor's level, gear capped too.
- Room-to-room floors (the great hall prototype, 2026-11-23).

So the plan is mostly a new **shape and name** for what exists, plus three real additions (below).

### Weak points found, and the fix for each

1. **A stream of idle players must never get stuck on an objective.** Every objective counts by itself from normal
   play: kills by whoever trains on the part, materials counted from what the stream's players gather anyway (counted,
   not taken from bags), a guardian beaten. Nothing asks a player to click at a moment or hand something in. Personal
   town quests stay optional and pay personal rewards; they never block the stream.
2. **Small streams.** Fixed bosses tuned for five to eight players lock a two-player stream out. See "Scaling" below.
3. **"Each floor is one world scene" would mean rebuilding three bands.** Instead: a floor is a **town with areas
   around it**. The layouts that exist become the floor's areas (floor 1: the courtyard, the marsh, the crypt), each
   still its own scene loaded when used. A floor map (in the tower panel and on the website) shows the town in the
   middle, the areas and their parts, lit as they are cleared. It reads as one world without rebuilding anything.
4. **Free training on cleared parts for each player** needs more than one area loaded at once (today: one). Allow up
   to three areas beside the town: where the push is, and two more opened by whoever asks first; a player asking for a
   fourth is offered the open ones. Each loaded area costs about 20 MB and its monsters' updates; potato mode keeps it
   at one.
5. **Losing bosses nobody wants to lose.** The bunny family, the Quackers and the Moo-tants stay: every part keeps its
   guardian, now called a mini boss, with the short intro; the floor boss (today's floors 10, 20, 30) gets the full
   cutscene, the world first and the trophy. Fewer "big" bosses, no content lost.
6. **Numbers already in players' heads and in the data.** The server keeps its plain step number (1 to 30) inside;
   only what is shown changes ("Floor 2, part 4"). Titles, the Legendary rule ("floor 21" becomes "floor 3"), chat
   replies, about 65 game texts and the website are rewritten once. No migration.
7. **Gates and breaks are a rework of dungeons and raids, not of the tower.** They can come one version later without
   anyone unlearning the tower, as long as part objectives are built as a list of kinds, so "bring a Sigil from a
   gate" is one more kind later.

### Scaling (the owner's call, 2026-11-24)

"Scale the enemies dynamically a bit to make them more difficult if all players are high levels, but not overly much
on lower bands; first levels are okay to be easier, each gets more difficult."

Level sync already takes away levels and gear above a floor. What still flattens a floor is **how many** players
there are and what sync does not cap (skill trees, gems, enchantments). Proposal:

- **A pressure number** per fight: the players in it against the floor's suggested group (five), times how much
  stronger than a fresh level-synced player they are on average (trees, gems, enchantments; 1.0 to about 1.5).
- **Monsters in a room** come in numbers and health by pressure, so a room takes a group about as long whether five
  or thirty are in it.
- **Mini bosses and floor bosses** get health and helpers by pressure, their blows a little; never below the fixed
  floor numbers for a full group.
- **Small groups get some mercy, less with every floor:** under the suggested group the boss shrinks toward the
  group's size, down to 50% on floor 1, 65% on floor 2, 80% on floor 3, 90% from floor 4 on.
- **How hard the scaling bites grows with the floor:** floor 1 follows pressure at 40%, floor 2 at 60%, floor 3 at
  80%, floor 4 and on in full. So the first floor stays friendly for any stream, and the top is a real fight for
  everyone.
- World firsts stay fair: a clear is measured against the group that did it. The numbers come from the combat
  simulator before anything is played.

### The cut

**In v0.9.7 (the shape; so nothing has to be unlearned):**
1. Floors and parts everywhere: game, chat commands, server replies, titles, the Legendary rule, website, docs.
2. Mini bosses and floor bosses (intros, cutscenes, rewards and world firsts follow the floor boss).
3. Objectives as a list of kinds per part (kills, materials the stream gathers, find things, the guardian), all
   passive; the two kill quests per part become the simplest case.
4. Each player's free entry to cleared parts, up to three areas loaded.
5. The floor map in the tower panel and on the website.
6. Scaling by pressure, from the simulator.
7. Rooms: the crypt and the frost vault (already rooms without doors) get doors and the route, so floors 1 and 2 have
   parts the group moves through.
8. The tower standing in the main world, lit by the stream's progress (it is what makes the tower a place; it can
   slip to v0.9.8 if time runs out, nothing depends on it).

**In v0.9.8 (content on the same shape):** floor 4, the Dark Fortress, with the ghost quarter and the great hall;
dungeons as ranked gates in the look of a reached floor; raids as breaks; gate items as an objective kind.

Rough size of the v0.9.7 part: two to three weeks of building on top of what is there, then the playtest that
v0.9.7 needs anyway.

## 12. Built in v0.9.7: floors and parts, mini bosses, objectives, scaling (2026-11-24)

The first four pieces of the cut above are built (items 1, 2, 3 and 6): the words, mini bosses and floor bosses,
objectives as a list of kinds, and scaling by pressure. Not built yet: each player's own entry to cleared parts, the
floor map, doors and routes for the crypt and the frost vault, the tower in the main world. Nothing is deployed; it
runs on the local server.

### Words, and the numbers behind them

| Players see | In code and on the server | Example |
|---|---|---|
| **Floor** (a world with its town) | `TowerBand` | Floor 2: The Frozen Vault, town Rimeholt |
| **Part** of a floor | `TowerFloorDefinition`; its `Number` is the part's **step** | Floor 2, part 4 is step 14 |
| **Mini boss** | the guardian of every part but a floor's last | Nibbles the Vicious, floor 1 part 1 |
| **Floor boss** | the guardian of a floor's last part | Viridia (step 10), Glacius (20), Fulmina (30) |
| **Objectives** | `TowerObjective[]` on the part | Defeat, Gather, Find, then the guardian |

The server keeps what it kept: `StreamTower.Floor` and `TowerClear.Floor` are steps (a part's number counted through
every floor), so local data maps straight across and nothing was migrated. One mapping, in RavenNest.Combat `Tower.cs`,
turns steps into floors and parts and back: `FloorOf`, `PartOf`, `StepOf`, `PartsIn`, `FirstStep`, `LastStep`,
`IsFloorBoss`, `LatestStep`, `FloorsCleared`, `Label` ("Floor 2, part 4"), `Short` ("2-4") and `TryParsePlace` (what
chat types). A floor's parts come from the data (`TowerBand.Parts`); no caller assumes ten parts or a number of floors.
The game says a step with `TowerWords`, the website with `Tower.Label`.

Written as "Floor 2, part 4" in sentences and on the panel, "2-4" where it is typed.

### Chat commands

Unchanged: `!tower`, `!tower leave`, `!tower info`, `!tower boss`, `!tower climb`, `!tower town` (and a town's name),
`!tower rest`, `!tower talk`, `!tower close`, `!tower vote <any of them>`.

Going to a part already reached, to train there (the streamer or a moderator at once, anyone in the tower by vote):

| Typed | Goes to |
|---|---|
| `!tower part 7` | part 7 of the floor the group is on |
| `!tower floor 2` | floor 2's latest part: the stream's own part on its own floor, the last part of a cleared floor |
| `!tower floor 2 part 7`, `!tower 2-7`, `!tower 2 7` | that part |
| `!tower 7` | the same as `!tower part 7` |
| `!tower part`, `!tower floor` | back to the part the climb is on (or `!tower info` when it is there already) |
| `!tower train 7` | the old spelling, kept: `!tower part 7` |

`!tower floor 7` used to mean the seventh step. It now means floor 7, which is the only meaning that holds once there
are more floors; the tower was never released, so nobody has the old one in their fingers. A place not reached answers
"Floor 2, part 4 has not been reached yet. Your stream is on floor 1, part 6."; a place that does not exist on a floor
that does answers with the three ways to type one. A floor past the top is "not reached" too, so nothing says how many
floors are to come.

### Mini bosses and the floor boss

- **Every part keeps its guardian.** Parts 1 to n-1 of a floor: a mini boss. A 5 second intro (its name at a smaller
  size, with its line or "Mini boss of floor 1, part 3"), the fight, a 3.2 second fall with "Part 3 cleared" in silver,
  the usual chest, coins and class gear, then down to the floor's town.
- **The last part: the floor boss.** A 9 second intro ("Boss of floor 1, The Overgrown Halls" and its line), a 6.5
  second fall with "Floor 1 cleared" in gold, the **world first** announced for the first stream to beat it, two chest
  items, then on to the new floor's town with its arrival fly-over ("Floor 2: The Frozen Vault").
- **Firsts per part** are kept quietly: the first stream in the world to clear any part still gets double coins and the
  clear line says so; the website shows "First clear" on every part and "World first" on a floor boss's.
- **The last hit** on a floor boss gives a title and nothing else: the achievement *Last Blow*, "the Finisher" at one,
  "the Last Word" at three, "the Curtain Call" at ten. The game reports whose blows landed as the boss fell (the one that
  took its last health first, then any other within a tenth of a second); the server keeps only characters who were in
  the attempt and in its session, each once, and at most three share it (`Tower.LastHits`). A tie is shared rather
  than decided by which blow the game worked out first. Nobody valid, nobody gets it. Stored on the clear
  (`TowerClear.LastHitCharacterId`) and shown with the world first on the website.
- **Climber** keeps its tiers at the same steps (1, 5, 10, 15, 20, 25, 30), now worked out from the floors: the first
  part, each floor's middle part and each floor's boss. Titles earned locally mean what they meant.
- **Legendaries**: "a clear of floor 21 or higher" is "a clear on floor 3 or higher" (`Legendary.TowerFloor = 3`), the
  same parts as before.

### Objectives: a list of kinds, all passive

A part's objectives are `TowerObjective` entries (`Tower.cs`), each of a `TowerObjectiveKind`:

| Kind | Counts | From |
|---|---|---|
| `Kill` | one kind of the part's monsters, defeated by whoever trains there | the game's report, as before |
| `Gather` | gathering rounds of the named skills by the stream's players, wherever they train. Counted, never taken from a bag. Every monster defeated on the part counts one too, so a stream with nobody gathering is never stuck | the server's own `QuestTracker.Gathered` (one a round, about six a minute per gatherer), plus the game's kills |
| `Find` | hidden things on the part, one at a time. A second of searching counts in full while someone stands within 9 m of where it lies and a quarter while players are anywhere else on the part. 4 minutes of searching a thing on floor 1, 5 on floor 2, 6 on floor 3. Found: a word over the finder | the game's report; the server takes one a report and no two within 45 seconds |
| `Guardian` | the mini boss or floor boss; opens when the others are done | the clear |
| `GateItem` | kept for v0.9.8's gates; no part uses it | |

Nothing asks anyone to click, type or hand something in. The stream's progress text holds every counted objective
("defeat-hall-goblin:12;gather-iron:340;find-boots:1"). The tower panel shows a row per objective with its count, the
guardian as the last row, and "Objectives 1 / 3" on the bar; the website's /tower shows each part's objectives with who
asks for them and why, and the streamer's /town page shows the stream's own progress.

**The mix** (every part also has two kill objectives and its guardian):

| Floor | Gather | Find | Kills only |
|---|---|---|---|
| 1, Mossgate | 3 herbs for Bramble (Gathering, Farming); 5 timber for Warden Elm (Woodcutting); 7 iron ore for Warden Elm (Mining); 9 provisions for Old Mossbeard (Fishing, Farming) | 2 Stolen Boots (Mossbeard's guests'); 6 Lost Carrots (Pip left them for Nibbles); 8 Blight Samples for Bramble | 1, 4, 10 |
| 2, Rimeholt | 3 coal and ore for Hilda (Mining); 5 fish for Old Tove (Fishing); 7 firewood for Big Gunnhild (Woodcutting); 9 food for Tove's stew (Farming, Gathering) | 2 Duck Feathers for Ylva; 6 Mead Barrels for Gunnhild; 8 Miner's Lanterns for Hilda | 1, 4, 10 |
| 3, Galewharf | 3 planks for Captain Brisa (Woodcutting); 5 rations for Skipper Hob (Fishing, Farming); 7 copper ore for Tinker Volt (Mining); 9 fodder for Tessa (Farming, Gathering) | 2 Tufts of Wool for Tessa; 4 Lightning Bottles for Volt; 8 Oath Stones for Brisa | 1, 6, 10 |

**The pace.** Measured in the game: five players on floor 1, part 6 make 12 kills a minute. The amounts in `Tower.cs`
are set from the minutes of fighting a part should take five players:

| | Minutes for five players | Kills per kind (first, second objective) | Gather | Find |
|---|---|---|---|---|
| Floor 1 | 40, 60, 75, 80, 85, 90, 95, 100, 105, 110 (part 1 to 10) | 240 and 190 on part 1, up to 550 and 425 | 1,100 to 1,500 | 3 or 4 things, 12 to 64 minutes |
| Floor 2 | 130 to 175 | 775 and 625 up to 1,050 and 850 | 2,000 to 2,450 | 3 or 4 things, 15 to 80 minutes |
| Floor 3 | 190 to 235 | 1,150 and 900 up to 1,400 and 1,150 | 2,900 to 3,300 | 4 or 5 things, 24 to 120 minutes |

So a part is roughly an evening's stream on floor 1 (the first is a short one to start with), two evenings on floor 3,
and the three floors together are about 75 hours of a five player group fighting. A kill objective is its kind's share
of the kills in those minutes (a part's kinds stand on its spots in turn). On a gather part the kill objectives are
sized for 80% of the minutes and the gather amount for 120% of the kills in them: a stream of fighters alone takes a
fifth longer there, and two or three gatherers anywhere in the stream bring it under the usual time. Finds run beside
the rest and are never the last thing left.

### Scaling by pressure

One rule, `RavenNest.Combat.TowerPressure`, used by the game (which runs the fights), the server (for the record of a
clear) and the simulator.

- **Pressure** = heads / the suggested group x the group's strength. The suggested group is five for a part and its
  mini boss and **eight for a floor boss** (`Tower.FloorBossGroup`): the floor bosses' fixed numbers were set so that
  five lose and eight win, so eight or more meet at least those numbers and fewer than eight get the mercy below.
  A player counts as one head; one far below the part (low levels,
  poor gear) counts as that share of a head, a quarter at least, so bringing newcomers along does not make the fight
  bigger. Strength is how much more than a fresh level-synced player a character is: what it deals (70%) and what it
  takes (30%) against the part's standard monster, plus 4% for every active skill in its loadout; skill trees, gems and
  enchantments are what moves it. Each player's counts between 1.0 and 1.5 and the group's is the average.
- **Above a full group** health follows pressure by the floor's **bite**: x (1 + bite x (pressure - 1)), bite 40% on
  floor 1, 60% on floor 2, 80% on floor 3, all of it from floor 4. A guardian's blows grow by 5% per point of pressure
  times the bite, 25% at most. Never below the fixed numbers.
- **Under a full group** health shrinks toward the group's size, down to the floor's **mercy**: 50% on floor 1, 55% on
  floor 2, 60% on floor 3, 70% from floor 4. A guardian's blows shrink more than its health: they are set so that the
  fight is at most (1 + bite x (1 - pressure)) times as hard as a full group's (two of five on floor 1: 1.24 times, on
  floor 4: 1.6 times), where hard is the blows times the time the fight takes over the health pools there are.
- **Monsters on a part**: their health by the same rule, and under a crowd they come back sooner (20 seconds down to 8).
  They take the new numbers as they stand untouched or come back, so a group arriving or leaving changes the part
  within seconds. **Guardians**: health and blows as above, set when the fight begins from who is in it. **Helpers**:
  half of the scaling in how many are called, half in their health; for a small group they strike as softly as the
  guardian does.

Three things changed from the proposal's starting numbers, in the simulator and in play:
- **A floor boss is measured against eight, not five.** Against five, neither two nor five fresh players could ever
  win a floor boss in the simulator (it is made to want eight), so every small stream was locked out at the end of
  every floor, and in play nine mixed bots met 30% more health on Viridia and lost with a quarter of it left.
- **Mercy** was 50, 65, 80 and 90%. With those, two players needed twice a full group's time on floor 3 and ran into
  the bosses' enrage; 50, 55, 60 and 70% keep their fights at 1.25 to 1.75 times a full group's.
- **Blows for a small group.** With a boss's health and blows both halved, two players still lost every fight: the one
  holding it takes every swing, and there are two health pools to wear down where a full group has five. Hence the
  rule above. (Scaling blows also needed a real multiplier, `EnemyDefinition.DamageScale`: an enemy's power is a small
  part of its hit.)

**The simulator** (`dotnet run -c Release -- tower 200` in RavenNest's src/tools/RavenNest.CombatSimulator; `tower 200
fixed` is the old table). Players at each part's sync level in the best gear for it, no healers, no active skills, no
pets: the game is easier than this. Per cell: minutes to finish the part's kill objectives, then the guardian (attempts
won, minutes a win took).

Fresh synced players:

| Part | Guardian | 2 players | 5 players | 10 players | 30 players |
|---|---|---|---|---|---|
| 1-1 | Nibbles the Vicious | 71 min; 100%, 2.1 min | 32 min; 97%, 1.8 min | 18 min; 100%, 1.1 min | 8 min; 100%, 0.8 min |
| 1-3 | The Brood Mother | 118 min; 62%, 3.3 min | 58 min; 36%, 2.9 min | 33 min; 100%, 1.8 min | 17 min; 100%, 1.2 min |
| 1-6 | The Moo-tant | 185 min; 92%, 3.4 min | 92 min; 68%, 3.2 min | 53 min; 100%, 1.9 min | 27 min; 100%, 1.3 min |
| 1-9 | Old Rotbelly | 175 min; 100%, 3.3 min | 90 min; 99%, 3.0 min | 52 min; 100%, 1.9 min | 27 min; 100%, 1.3 min |
| 1-10 | Viridia, the Verdant Wyrm (floor boss) | 239 min; 100%, 5.1 min | 122 min; 100%, 2.7 min | 70 min; 100%, 2.3 min | 38 min; 100%, 1.4 min |
| 2-1 | Jarl Rimebeard | 256 min; 100%, 2.8 min | 119 min; 100%, 2.3 min | 72 min; 100%, 1.7 min | 40 min; 100%, 1.4 min |
| 2-5 | Emperor Quackers | 250 min; 100%, 4.1 min | 124 min; 100%, 3.3 min | 77 min; 100%, 2.5 min | 45 min; 100%, 2.0 min |
| 2-8 | The Thawed Ronin | 363 min; 85%, 4.9 min | 180 min; 80%, 3.9 min | 111 min; 100%, 2.5 min | 68 min; 100%, 2.0 min |
| 2-10 | Glacius, the Winter Wyrm (floor boss) | 362 min; 100%, 4.7 min | 173 min; 100%, 2.2 min | 107 min; 100%, 2.0 min | 63 min; 100%, 1.5 min |
| 3-1 | Captain Gale Blackwind | 400 min; 90%, 4.6 min | 189 min; 94%, 3.2 min | 122 min; 100%, 2.5 min | 80 min; 100%, 2.2 min |
| 3-5 | Captain Quackbeard | 366 min; 23%, 5.0 min | 174 min; 47%, 3.7 min | 117 min; 100%, 3.0 min | 77 min; 100%, 2.6 min |
| 3-8 | High Warden Aerin | 482 min; 95%, 4.6 min | 237 min; 95%, 3.4 min | 154 min; 100%, 2.7 min | 103 min; 100%, 2.4 min |
| 3-10 | Fulmina, the Thunder Wyrm (floor boss) | 522 min; 100%, 5.9 min | 246 min; 100%, 2.6 min | 170 min; 100%, 2.4 min | 112 min; 100%, 2.1 min |

Veterans (a damage tree's passives on top; strength 1.12 to 1.46):

| Part | 2 veterans | 5 veterans | 10 veterans | 30 veterans |
|---|---|---|---|---|
| 1-1 | 67 min; 100%, 1.4 min | 30 min; 100%, 1.3 min | 16 min; 100%, 0.8 min | 7 min; 100%, 0.6 min |
| 1-3 | 109 min; 85%, 2.3 min | 53 min; 99%, 2.1 min | 30 min; 100%, 1.3 min | 15 min; 100%, 1.0 min |
| 1-6 | 167 min; 98%, 2.4 min | 82 min; 100%, 2.2 min | 47 min; 100%, 1.5 min | 23 min; 100%, 1.1 min |
| 1-9 | 161 min; 100%, 2.3 min | 79 min; 100%, 2.0 min | 45 min; 100%, 1.4 min | 23 min; 100%, 1.1 min |
| 1-10 | 216 min; 100%, 3.6 min | 106 min; 100%, 2.1 min | 62 min; 100%, 1.7 min | 32 min; 100%, 1.1 min |
| 2-1 | 233 min; 99%, 2.2 min | 113 min; 100%, 1.9 min | 69 min; 100%, 1.5 min | 39 min; 100%, 1.3 min |
| 2-5 | 224 min; 100%, 2.8 min | 110 min; 100%, 2.4 min | 69 min; 100%, 1.9 min | 39 min; 100%, 1.7 min |
| 2-8 | 327 min; 91%, 3.3 min | 160 min; 100%, 2.6 min | 101 min; 100%, 2.0 min | 58 min; 100%, 1.7 min |
| 2-10 | 332 min; 100%, 3.2 min | 164 min; 100%, 2.1 min | 97 min; 100%, 1.7 min | 58 min; 100%, 1.3 min |
| 3-1 | 363 min; 64%, 3.1 min | 178 min; 100%, 2.5 min | 117 min; 100%, 2.1 min | 74 min; 100%, 1.9 min |
| 3-5 | 332 min; 17%, 3.3 min | 164 min; 97%, 3.1 min | 108 min; 100%, 2.6 min | 72 min; 100%, 2.4 min |
| 3-8 | 433 min; 94%, 3.1 min | 217 min; 100%, 2.6 min | 140 min; 100%, 2.2 min | 91 min; 100%, 2.1 min |
| 3-10 | 458 min; 100%, 4.0 min | 228 min; 100%, 2.4 min | 148 min; 100%, 2.0 min | 97 min; 100%, 1.8 min |

What it says: guardians take every group size about the same time (two players 1.2 to 1.5 times a full group's, thirty
about half to two thirds of it), two players win about as often as five on floors 1 and 2 and a little less on floor
3, and veterans are faster but not trivially (a third off the time). The floor bosses are sure wins in the simulator
for any group it measures them fairly for, as they were for eight before: their own numbers want a pass in the
playtest, with healers and real builds, which the simulator does not have.

**Parts are not the same pace for every size.** Half of a kill is walking to the next monster, which no health scaling
changes: a group of two needs about twice a full group's time for a part, ten about 0.6 of it, thirty a quarter (floor
1) to under a half (floor 3). See the open calls.

### On the server and the website

- `StreamTower.Progress` holds every counted objective; `TowerClear` gained `Strength` and `LastHitCharacterId` (added
  at startup like every column). `api/tower/{step}/cleared` takes a `TowerClearReport` (who took part, whose blows
  landed last, the group's strength) and answers with `FloorBoss`, `WorldFirst` (a floor boss's), `PartFirst`,
  `LastHitBy` and the `Pressure` it worked out.
- The server decides what it decided: the part, the progress (capped per report), whether the guardian is open, the
  clear and its rewards, the firsts, who gets the last hit title. The game reports and shows.
- Website: /tower by floor (its town, element, parts with objectives and who asks, the floor's world first and whose
  last blow it was), still hiding what no stream has reached, a floor's boss included until its last part is reached;
  the leaderboard by floor, then part; /town with the stream's own part and progress; the quest page's Climber as
  "Floor 2, part 4"; the Hall of Legends' rule as floor 3.
- Bot: `!tower`'s help text and usage (the bot passes the words on; the game reads them).

### Played (2026-11-24, editor, local server, dev character and bots, chat muted and the bot stopped)

- **The words**: the panel, every `!tower` reply and every announcement read through the real command handler with a
  kept (never sent) chat client: `!tower`, `info`, `help`, `boss` (locked, training, nobody inside, a vote), `part 3`,
  `part 4`, `1-5`, `2-4` (not reached), `floor 9` (not reached), `part 12` (no such place), `part` (back), `climb`,
  `close`. The dev stream's old progress (step 6, with its two kill counts) came up as floor 1, part 6.
- **Objectives of each kind** on floor 1: kills counting on parts 6, 7 and 10; a hidden thing found on part 6 after 16
  minutes with the group never near it (the next one lay 6 m from them); gathering on part 7 with the dev character
  mining on Ironhill and five bots fighting on the part: the count rose with the kills and by one about every ten
  seconds beside them.
- **A mini boss clear** (the Moo-tant, floor 1 part 6, 31 players): the short intro and fall, the clear saved with its
  strength (1.07), "cleared before any other stream" with double coins and no world first, everyone down to Mossgate,
  part 7 next.
- **A floor boss clear** (Viridia, floor 1 part 10): with 23 players 134,590 health (1.44 times its 93,750), five
  helpers a call where a group of eight gets four, 2.4 minutes (the last blow was a bot's, so nobody got the title); the long intro and fall, "WORLD FIRST", "Floor 2 is
  open: The Frozen Vault", everyone on to Rimeholt, its arrival played, floor 2 part 1 loaded with its numbers.
  Alone, the dev character (pressure 0.17 of eight) met her at half health (46,880) with blows at 8% and three
  helpers a call, and won in 6.7 minutes, a minute before her enrage: the last blow was its own, the server gave the
  Last Blow achievement and the game said so. (That line had its names the wrong way round; fixed.)
- **Scaling on the Moo-tant** (45,000 health for five fresh players): two players (pressure 0.51 with the dev
  character's strength of 1.48) 23,000 health and blows at 0.61, monsters at half health; eight (1.34) 51,200 and 1.01;
  thirty-one (5.5) 125,940 and 1.09, five helpers for three, monsters at 2.8 times and back sooner. A bot far below
  the part (level 18 on a level 60 part) counted as a quarter of a head.
- **Lost fights**: nine mixed bots without a healer lost to Viridia twice, at 122,120 health (measured against five,
  before the change to eight) with a quarter left, and at 86,320 (against eight) with a fifth left. Thirty-one bots
  killed the Moo-tant in under half a minute. The bosses' own numbers were not touched.
- **Going to a cleared part**: `!tower part 4` (the same layout, at once) and `!tower part 3` from the crypt (another
  layout, a climb). The second did not work before today: the climbers were sent back to the stream's part (the
  unload's "someone is waiting" reload took them); fixed.
- **Not played**: a floor boss's last hit by more than one player (a tie; the rule is unit tested), gather and find
  parts on floors 2 and 3, any fight above floor 2 part 1, the wording through the real bot in real chat, a second
  stream (a later clear without the first-clear line), the towns' people asking for the objectives (they do not talk
  about them yet: the story lines are on the website only), the website's /town section signed in (it needs the
  streamer's sign in; /tower was captured).

Captures in docs/mockups/tower: `worlds-panel-objectives.png` (three kinds of rows and the mini boss row),
`worlds-panel-training.png`, `worlds-panel-countdown.png`, `worlds-panel-floor-boss-ready.png`, `worlds-panel-town.png`,
`worlds-panel-floor2.png`, `worlds-mini-boss-intro.png`, `worlds-part-cleared.png`, `worlds-floor-boss-intro.png`,
`worlds-floor-cleared.png`, `worlds-website-tower.png`, `worlds-website-tower-end.png`.

### Open calls for the owner

1. **The pace of a part.** About an evening on floor 1 for five players, more than twice that on floor 3, 75 hours
   of fighting for the three floors. Too slow, too fast? It is the amounts in `Tower.cs`
   (scratchpad script: minutes per part x 12 kills a minute).
2. **Parts are faster for big streams and slower for small ones** (a quarter to half the time for thirty players, twice
   the time for two), because the kill counts are fixed and half of a kill is walking. Two ways to even it out, neither
   built: size a part's amounts to the stream when it reaches the part (from the players in its last clear: thirty
   would see "Defeat 1,750 Suspicious Cows", two "Defeat 280"), or let a part's monsters follow pressure in full on
   every floor (which only halves the gap).
3. **A floor boss is measured against eight players.** Fewer than eight meet a smaller boss, which is what lets a
   stream of two or five past the end of a floor. It also makes a floor boss gentler for five than it was (it was
   made so that five lose). Wanted that way, or should a floor boss stay as it is for everyone under eight?
4. **The mercy numbers** are 50, 55, 60 and 70% (the proposal said 50, 65, 80, 90), and a small group's boss strikes
   much softer than its health alone would say. Fine, or should small streams struggle more higher up?
5. **In play, bots flatten mini bosses** (thirty-one killed the Moo-tant's 126,000 health in under half a minute) and
   then lose to Viridia with nine, without healers. The scaling moves both the same way; the bosses' own numbers still
   want a pass in the playtest with real players.
6. **The last blow's titles**: "the Finisher", "the Last Word", "the Curtain Call" (1, 3 and 10 floor bosses). Other
   names welcome. The achievement has no picture yet (no image was generated).
7. **`!tower floor N` changed meaning** (floor N's latest part, not step N). Unreleased, so nobody loses a habit.
8. **Gather objectives count every kill on the part too**, so a stream of fighters is never stuck. Without that, a
   stream nobody gathers in would stop at part 3 of every floor. Say if gathering should be the only way.
9. **Hidden things** are found by the group being on the part (a quarter speed) or near the thing (full speed); in
   play a group of five never walked near one and each took 16 minutes. They could lie where the group fights instead.
