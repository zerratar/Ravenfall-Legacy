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
- **Built 2026-11-24:** docs/world-tower.md (the tower on its islet, the light by progress, the plaza; visiting it and the gates' beams are not done).

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
objectives as a list of kinds, and scaling by pressure. Not built in this step: each player's own entry to cleared
parts, the floor map, doors and routes for the crypt and the frost vault (all three: step 2, section 13, the same
day), the tower in the main world (built since: docs/world-tower.md). Nothing is deployed; it runs on the local
server.

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
| `Find` | hidden things on the part, one at a time. A second of searching counts in full while someone stands within 9 m of where it lies and a quarter while players are anywhere else on the part. 4 minutes of searching a thing on floor 1, 5 on floor 2, 6 on floor 3 (since 2026-11-27: 2.75, 3 and 3.25, section 15). Found: a word over the finder | the game's report; the server takes one a report and no two within 45 seconds |
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

**The pace.** (Replaced on 2026-11-27: parts of about 25 minutes for five players, with packs on every spot; see
section 15. What follows is the first pace, kept for the record.) Measured in the game: five players on floor 1, part 6
make 12 kills a minute. The amounts in `Tower.cs` were set from the minutes of fighting a part should take five players:

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
   *Done in step 2 (section 13): a thing is searched in full wherever the group fights among the part's monsters.*

## 13. Built in v0.9.7, step 2: free entry to cleared parts, the floor map, rooms, and what a kill is worth (2026-11-24)

Cut items 4, 5 and 7 are built, the three things from step 1's play test are fixed, and one piece was added on the
owner's word the same day: kills count for more in a small stream and for less in a big one. Still not built: the
tower in the main world is its own work (docs/world-tower.md). Nothing is deployed; it runs on the local server.

### Each player's own way to a cleared part

A part the stream has cleared is a place any player can go to and train on, like sailing to an island, while the climb
goes on elsewhere.

| Typed | Who | What happens |
|---|---|---|
| `!tower` | anyone | to the part the climb is on (from the world: to the town when everyone waits there between parts; from the town or from a cleared part: straight to the climb) |
| `!tower part 3`, `!tower 3`, `!tower 1-3`, `!tower floor 2 part 7`, `!tower floor 2`, `!tower train 3` | anyone, the streamer too | **only that player** goes to that cleared part to train. A part alone is one of the floor the player is on (the part or the town they are in), else of the stream's floor |
| `!tower part`, `!tower floor` alone | anyone | the same as `!tower`: back to the climb |
| `!tower all part 3`, `!tower all 1-3`, `!tower all floor 2` (also `everyone`, `group`) | the streamer or a moderator at once, anyone of the group by vote | **the climbing group** goes to that part, as `!tower part 3` did in step 1. Players in the town stay there, and so do players who went to a cleared part by themselves (since the follow-ups below; before, they went along) |
| `!tower all` alone | the same | the climbing group goes back to the climb's part |
| `!tower boss now` (also `start`, `go`), or `!tower boss` again during the countdown | the streamer or a moderator | the countdown is cut short and the fight begins (the follow-ups below) |
| `!tower map` | anyone | the floor map on screen for 20 seconds (once a minute at most) |

**The choice for the streamer's own command:** it does what a viewer's does (the streamer's character goes alone).
Moving everyone is the explicit word `all`. Every command of step 1 still answers; two changed what they do:
`!tower part 3` and its spellings take one player and no longer everyone, and `!tower part` alone takes one player back
to the climb instead of everyone. The tower was never released, so nobody has the old ones in their fingers.

**Up to three parts are open at once** beside the town (`TowerManager.MaxAreas`): the climb's part always has its
place, and two more are opened by whoever asks first. A fourth is refused and the player stays where they are:
"3 parts of the tower can be open at once, and they are: floor 2, part 4 (the climb, !tower), floor 2, part 3 (!tower
2-3), floor 2, part 1 (!tower 2-1). Go to one of those, or try again when one has emptied." A part nobody has been on
for 90 seconds closes, and one that stands empty gives its place at once to a part someone asks for (the same layout
is filled again where it stands, with nothing to load). The climb's part is never closed during a boss attempt or
while it is wrapped up after a clear.

**Potato mode keeps one part open.** A player's own trip is then only possible to the part that is open (or, with
nothing open, it opens that part): "In potato mode one part of the tower is open at a time, and it is floor 1, part 6
(the climb, !tower). The streamer or a vote can take everyone to another: !tower all 1-3." When the one open part is
a cleared one, `!tower` takes a player there to the others (the reply says where the climb waits and that `!tower all`
brings everyone back): the step 1 behaviour, kept for potato mode. Switching potato mode on while more parts are open
closes none that has players; they go as they empty.

**Where the parts stand, and two parts of one layout.** Every layout scene is built on the same spot ((0, 0, -6000)).
A loaded area is a scene moved to a place of its own: the first stays there, the others stand 1.5 km apart to the
west (`SlotOffsets`), past the camera's reach, so no part is ever seen from another. The scene's root is switched off
for the move and on again, so what registered itself by its place (the navmesh, the grass, roads, light shafts, the
frozen pond) registers where it stands. Many parts share a layout (the courtyard is parts 1, 2, 5 and 6): **a second
part of the same layout is the scene loaded a second time** and moved; meshes, textures and materials are shared, the
second copy cost 13 to 20 MB and 0.25 to 0.6 s in the editor. There is a fourth place so that a part can open beside
three full ones (when everyone is moved) before the ones left empty have gone: nobody steps out of the tower while a
part loads, they stay where they are until it is ready.
One thing only the editor does: a second surface with the same baked navmesh drops it on load (the editor's guard
against copied surfaces), so the second copy had no navmesh and nobody on it could walk. `TowerManager.AddNavMesh`
adds it for a copy whose surface has none. A build has no such guard; **two parts of one layout in a build is not
tested** (on the checklist).

**What follows the player to a part.**
- Level sync is the part's (it was already by the floor a player stands on), and each part's monsters follow the
  pressure of the players on that part alone.
- **Objectives count only on the part the climb is on**, as step 1 had it: kills on a cleared part give experience,
  count for the player's own quests (the towns' chains) and for nothing of the stream's.
- A knocked out player gets back up on the part they are on. `!tower leave` takes them out from anywhere.
- **A boss attempt** is announced to everyone, and the panel of a player who trains elsewhere says "Boss attempt on
  floor 1, part 7" with its clock and `!tower`: that takes them to the climb and into the attempt. Leaving for the
  town or for another part during the countdown takes a player out of the attempt (step 1 kept them in it, and
  rewarded them). No attempts on cleared parts.
- After a clear the players on the cleared part go down to the town; those training on other parts stay where they
  are.
- **The tower panel shows the part the watched player is on.** On a cleared part: its name, how many train there,
  "Training here - the climb is on floor 1, part 6" and `!tower` (or the room they are in while they fight through a
  part of rooms). Watching anyone else, the panel shows the climb as before. One panel, never two.

In code: `TowerManager.Area` (a loaded part: its step, its place, its scene, its floor, who waits for it),
`Join` (the climb), `Visit` (one player's trip), `RequestMove` (everyone), `Room` (may a part open, and which empty one
gives way). `TowerFloor.Loaded` is gone: `TowerFloor.All`, and `TowerManager.PushArea` for the climb's.

### The floor map

Opened from the tower panel's **Map** button (the one thing on the panel that takes a click) or with `!tower map`; a
section of the panel, so it stacks with nothing, and it closes during a boss fight. The floor's town in the middle,
its areas to both sides (in turn left and right, in the order the climb reaches them), each area's parts as numbered
tiles: green once cleared, gold where the climb is, a question mark where the stream has not been, a crown on the
floor boss's part once it is reached, and a blue badge with how many players are on a part (or in the town) right
now. An area the stream has not reached is "Not reached yet" with a dashed path. Arrows page through the floors
reached; the map opens on the floor of the watched player. Under it: what the tiles mean, and how to go to a lit part.
UI Toolkit, built in code (`TowerMapView`), bold Rodin, nothing under 20 px, a solid background, no pictures made for
it (icons from the game's icon font).

The areas come from one list both sides use, `RavenNest.Combat.Tower.AreasOf(floor)`: a layout and the parts fought
there, named `AreaName` ("The Courtyard", "The Drowned Marsh", "The Crypt"; "The Glacier", "The Ice Vault"; "The Sky
Docks", "The Spire's Crown"). Any number of floors, areas and parts works: areas stack in two columns, tiles wrap.

**On the website** /tower has the same map at the top of every floor some stream has reached (`TowerFloorMap.razor`):
lit as far as any stream has climbed, the furthest part marked, everything beyond a question mark (unreached areas
unnamed, as the rest of the page keeps them a mystery). The streamer's /town page shows the map of the stream's own
floor with its own progress. On a phone the town goes on top and the areas under it.

### Rooms: the crypt and the frost vault

The two layouts that were rooms without doors are floors of rooms now (section 22 of the tower design): parts 4, 7, 9
and 10 of floor 1 and parts 3, 4, 7, 8 and 10 of floor 2. Both are the same five rooms:

1. **The Entry** (the arrival, 3 monsters), a gate to
2. **the Hall** (8), from which a gate opens to
3. **the West Crypt** (5; the West Vault in the frost vault), then a gate from the Hall to
4. **the East Crypt** (8; the hold: it refills for training and the objectives), and when an attempt is called the
   gate from the Hall to
5. **the Throne Hall**, the guardian's room.

24 monster spots as before. The hold has eight so that a group has about as much to fight as on a one-room part (the
spots of a kind come back two or three at a time: about six of one kind a minute for a small group, ten for a crowd).

A gate is a stone frame with two leaves (the dungeon pack's large door, scaled to 4.3 by 4 m), the wall closed to both
sides of it with low pieces like the rooms' own, and a seal that suits the place: **in the crypt thorns grown across
the door with a pale grave light in them, in the frost vault a wall of blue ice crystals** that glow from within. The
seal shrinks away as the leaves swing into the next room. The walls of both layouts are low and the gates stand no
higher than a tall wall piece, so nothing needed cutting away for the camera (no `TowerCutaway` here; the great hall
keeps it).

`tools/tower/tower-rooms-route.cs` adds the gates, the rooms and the route to the saved scenes (run after either
layout's builder; it can be run again). It does not touch the baked navmesh: the new wall pieces carve it at run
time. The audit now walks every room's monster spots too: 30 checks, no problems, in both scenes. A player's own trip
to a cleared part of rooms starts at its first room behind shut gates and is fought through like the climb's; the
panel says "Room 2 of 4" until the hold is reached.

### What a kill is worth (the owner, 2026-11-24)

(Since 2026-11-27 a kill is mostly fighting, with packs on every spot: `TowerPace.FightShare` is 0.85, so the weights
below changed; the aim did not. See section 15.)

Step 1 measured that a part's fixed amounts take two players twice the time of five, and thirty a quarter to a half.
Now each counted kill, and each counted gathering round, is worth more when few produce it and less when many do. The
amounts shown stay as they are ("Defeat 300 Moss Goblins"); the server keeps the part of a kill that is not a whole
one yet with the stream's progress and shows whole numbers.

One rule, `RavenNest.Combat.TowerPace`, next to `TowerPressure`; the server applies it, the simulator shows what it
gives.

- **The aim** (`TimeShare`): a part's objectives take (heads / 5) to the power of -0.367 of five players' time under
  five heads and to the power of -0.26 over five: two players 1.4 times, ten 0.84, thirty 0.63.
- **What a kill is worth** (`KillWeight`) is that aim against how fast a group really kills (`KillRate`): its heads
  over what a kill takes them, where the walk to the next monster stays (56% of a kill for five players) and the
  fight follows the monsters' health, which follows the group by the floor's bite and mercy. So the weight knows the
  floor: a kill by two players counts 1.39 on floor 1 and 1.47 on floor 3, by ten 0.70 and 0.81, by thirty 0.50 and
  0.73 (a crowd's monsters are already tougher higher up). Five heads are 1.00 everywhere, one player alone 2.2.
- **Heads** are counted as for pressure: a player is one, a player far below the part is a share of one (a quarter
  at least), a lone player at least one. What a group has beyond fresh players (trees, gems) does not count:
  veterans are faster, and may be.
- **Where the heads come from.** The game reports its kills every 8 seconds, and with them the heads that were on
  the climb's part as those kills fell ("defeat-hall-cow:3;kills:5;heads:447"). The heads of a kill are the most
  there were on the part in the 20 seconds before it. The server works out the weight (a report without heads, from
  an older game, counts as before).
- **Gathering** (`GatherWeight`): a round counts 1 with two of the stream's players gathering for the part's
  objective, 1.55 for a lone gatherer, 0.60 with four, 0.27 with twelve; "gathering" is having made a round for it in
  the last two minutes, counted by the server as each round is made. The kills that count for a gather objective are
  weighted like every kill.
- **Hidden things** are time, not kills: unchanged (but see the fix below).

**Nothing to gain by leaving or joining.** What a part gets a minute is the kills a group makes times what each is
worth, which is 1 / `TimeShare`: it only grows with the heads, on every floor (a test walks it from 1 to 200 heads on
floors 1 to 8). So sending players off the part makes the objectives slower, never faster, and a player joining
never slows them. Stepping off for the moment a monster falls does nothing either: the heads are the most of the last
20 seconds, so whoever leaves counts until they have been gone that long, and whoever comes counts at once. A weak
character counts as the share of a head it is, as for pressure: it adds little and costs little. Players on cleared
parts are not on the climb's part, so they are not heads there and their kills count for nothing of the stream's.

**The simulator** (`dotnet run -c Release -- tower 200`): minutes of fighting for a part's kill objectives, and in
brackets against five players' minutes. Fresh synced players.

| Part | Guardian | 2 players | 5 players | 10 players | 30 players |
|---|---|---|---|---|---|
| 1-1 | Nibbles the Vicious | 50 min (1.56) | 32 min (1.00) | 25 min (0.78) | 16 min (0.51) |
| 1-3 | The Brood Mother | 84 min (1.46) | 58 min (1.00) | 47 min (0.82) | 34 min (0.59) |
| 1-6 | The Moo-tant | 130 min (1.44) | 90 min (1.00) | 74 min (0.82) | 54 min (0.60) |
| 1-9 | Old Rotbelly | 126 min (1.44) | 88 min (1.00) | 73 min (0.83) | 54 min (0.62) |
| 1-10 | Viridia, the Verdant Wyrm | 170 min (1.41) | 121 min (1.00) | 100 min (0.83) | 75 min (0.62) |
| 2-1 | Jarl Rimebeard | 173 min (1.45) | 119 min (1.00) | 94 min (0.79) | 64 min (0.54) |
| 2-5 | Emperor Quackers | 178 min (1.43) | 125 min (1.00) | 101 min (0.81) | 73 min (0.59) |
| 2-8 | The Thawed Ronin | 251 min (1.39) | 181 min (1.00) | 152 min (0.84) | 111 min (0.61) |
| 2-10 | Glacius, the Winter Wyrm | 248 min (1.42) | 174 min (1.00) | 142 min (0.82) | 102 min (0.59) |
| 3-1 | Captain Gale Blackwind | 276 min (1.44) | 191 min (1.00) | 155 min (0.81) | 109 min (0.57) |
| 3-5 | Captain Quackbeard | 246 min (1.42) | 173 min (1.00) | 146 min (0.84) | 107 min (0.62) |
| 3-8 | High Warden Aerin | 336 min (1.45) | 231 min (1.00) | 191 min (0.82) | 141 min (0.61) |
| 3-10 | Fulmina, the Thunder Wyrm | 349 min (1.42) | 246 min (1.00) | 202 min (0.82) | 149 min (0.61) |

| A floor, all its parts | 2 players | 5 players | 10 players | 30 players |
|---|---|---|---|---|
| Floor 1 | 18.4 h, 1.45 (it was 2.02) | 12.7 h | 10.4 h, 0.82 (0.58) | 7.6 h, 0.60 (0.30) |
| Floor 2 | 33.6 h, 1.43 (2.05) | 23.4 h | 19.1 h, 0.81 (0.62) | 13.8 h, 0.59 (0.36) |
| Floor 3 | 48.1 h, 1.42 (2.09) | 33.8 h | 27.8 h, 0.82 (0.67) | 20.5 h, 0.61 (0.44) |

Five players are exactly as before (about 70 hours for the three floors in the simulator; the amounts were not
touched). Part 1-1 is the odd one: its monsters fall in a blow or two, so nearly all of a kill is the walk there and
the rule's share of fighting is too high for it (two players 1.56, thirty 0.51).

### Fixed from step 1's play test

- **Hidden things.** A thing lay at one monster spot, and only standing within 9 m of that spot counted in full. A
  group roams all over a part: in play five players were 30 to 60 m from the spot at any look, and moving the thing
  to where they fought every 20 seconds still gave only 0.36 of the full rate (11 minutes for a thing meant to take
  4). Now the things lie among the part's monsters: a second counts in full while someone stands within 9 m of **any
  of the part's monster spots** (on a part of rooms: of the rooms the group has reached), and a quarter while the
  players on the part are away from all of them (at the arrival, waiting for a boss, getting back up). Played: five
  players on floor 1, part 6 found the first Lost Carrot **4 minutes 18 seconds** after arriving (240 seconds are
  meant; the search ran at 0.93 to 0.97 of the full rate). Floors 2 and 3 use the same rule with 5 and 6 minutes;
  their finds were not played.
- **The tower panel under the player details window.** The top column is centred on the screen, and the panel grows
  with its text. `TopColumnFit` now stands the column between the windows that reach into its height (the player
  list, the player details, wherever they were dragged) and tells the panel how wide it may be; the floor map's
  columns give way when there is little room. The panel's layout scales with the screen (its reference is 1920 by
  1080), so 720p and 1440p lay out the same; captured at 1080p with the player list and the player details open.
- **The boss's body in its fall.** Watched in real fights (a script looked at the body ten times during each fall):
  Big Gorp (a mini boss), Viridia (the floor boss) and the Frozen Warden were on screen, drawn, in the middle of the
  picture for the whole fall, and gone 0.8 s after the cutscene, as meant. Step 1's two captures showed an empty room
  because they were not taken in a real fight. Both retaken from real clears.

### Played (2026-11-24, editor, local server, dev character and bots, chat muted and the bot stopped)

- **Trips and areas** (the dev stream on floor 1, part 6): three bots to the climb, the dev character to part 3 (the
  marsh, a second place), a bot to part 2 (the courtyard a second time, a third place), a fourth part refused with
  the names of the open ones; `!tower` back to the climb from a cleared part; `!tower info` naming the open parts;
  a vote and the streamer's `!tower all 1-3`, `!tower all` back; a cleared part emptied and replaced by another;
  `!tower leave`, `!tower town`, `!tower part` alone, `!tower climb`.
- **Potato mode**: a third part refused while two were open, both closed as they emptied; one part open, `!tower`
  joining it with the potato reply, `!tower boss` from there ("the climb is on floor 1, part 6, and nobody is there
  now"), `!tower all` back to the climb, `!tower all 1-5` filling the same layout again where it stood.
- **The crypt** (floor 1, part 7, the dev character and five bots): Entry, Hall, West Crypt and East Crypt in about
  two minutes, each gate opening as its room was cleared, the hold refilling; the objectives finished there; the
  boss called, the way to the throne hall opening, everyone gathering; Big Gorp beaten in 146 s, the clear saved,
  everyone down to Mossgate, the crypt unloaded (the next part is the marsh). Meanwhile one bot alone on part 4 (the
  crypt a second time) fought through its own rooms, saw the boss attempt on its panel, joined it with `!tower` and
  left it again for part 4.
- **The floor boss** (floor 1, part 10, the crypt, 23 players): rooms, the boss called with one bot training on part
  7, Viridia beaten in 118 s, "Floor 1 cleared", everyone on to Rimeholt, the bot on part 7 left where it was. The
  map then showed floor 1 all green with the crown and floor 2 with its first part.
- **The frost vault** (floor 2, part 3, the dev character and four bots): the four rooms in about a minute, the ice
  seals going one by one; the Frozen Warden beaten in 206 s; the same scene filled again for part 4 where it stood,
  gates shut, and `!tower climb` took everyone into it. A bot alone on part 3 (a second copy) and two on part 1 (the
  glacier) beside it: three parts of floor 2 open, a fourth refused.
- **What a kill is worth**, on the local server through the game's own reports: ten kills with two heads counted 14,
  with five 10, with thirty 6, without heads 10; the same ten for a gather objective. In play five players (4.47
  heads, one bot far below the part) made about 12 counted kills a minute on floor 1, part 6.
- **The Map button** with pointer events (the panel picks the button; a press and release opens and closes the map),
  `!tower map`'s reply and its minute of rest, the debug menu's "Tower: Great Hall" and back (by its call).
- **Not played**: a build (two copies of one layout scene, the scene list); the finds of floors 2 and 3; a gather
  objective with real gatherers (the server's count of who gathers); floor 3's parts; the crypt's parts 4 and 9 and
  the vault's 7, 8 and 10 as the climb's part (they are the same rooms); a mini boss on a part of rooms with a crowd
  of thirty; votes with several real viewers; the wording through the real bot in real chat; the website's /town
  map (it needs the streamer's sign in; /tower was captured); the map's arrows with a mouse; the map as `!tower map`
  shows it for 20 seconds (it is the button's map); UI scales other than the one in use.

Captures in docs/mockups/tower: `worlds2-trip-panel.png`, `worlds2-trip-countdown.png` (a boss attempt seen from a
cleared part), `worlds2-three-areas.png` (with the reply to a fourth), `worlds2-floor-map-game.png`,
`worlds2-floor-map-website.png`, `worlds2-crypt-door-sealed.png`, `worlds2-crypt-door-open.png`,
`worlds2-frostvault-door-sealed.png`, `worlds2-frostvault-door-open.png`, `worlds2-panel-beside-details.png`,
`worlds2-map-beside-details.png`, `worlds2-part-cleared.png`, `worlds2-floor-cleared.png`.

### Open calls for the owner

1. **The streamer's `!tower part 3` takes only the streamer**, and `!tower all part 3` everyone. If streamers will
   expect the short one to move the stream, the two can swap for them.
2. **`!tower all` takes everyone on any part**, also players who went to a cleared part by themselves (not those in
   the town). It could leave them alone and move only the climb's group.
3. **Anyone may go to any cleared part**, whatever their level (level sync only brings levels down; a reply warns a
   character far below a part). Section 10 asks whether a floor should want levels, like an island.
4. **The hold of the crypt and the frost vault is a side room** (the East Crypt), and the way to the guardian goes
   back through the Hall. It keeps the rooms as they were built; a route that ends in front of the throne hall would
   need the rooms laid out anew.
5. **Three parts at once** is a constant (`MaxAreas`); a big stream may want more, a weak PC fewer than three without
   going all the way to potato mode. A setting is easy to add.
6. **The pace curve** (2 players 1.4 times five's time, 10 players 0.82, 30 players 0.6) is two exponents in
   `TowerPace`. A lone player's kill counts 2.2, which makes a part about 1.8 times five players' time for them.
7. **Gathering's measure**: two gatherers count as they are. A stream with nobody gathering loses nothing (kills count
   for gather objectives), one with many gatherers gets less per round.
8. **A frame of the wrong light** when the camera cuts from an island to a tower place (the look follows the camera a
   frame late). It was there before this step; it showed in 3 of about 110 snapshots.
9. **The seals**: thorns and grave light in the crypt, glowing ice in the vault. The crypt's is green again, like the
   great hall's ghostfire, by its light; another color is one line in the route script.

### Follow-ups the same day (2026-11-24): the owner's answers and what he saw in the test fights

The owner answered the open calls above ("go with all your recommendations") and watched the boss fights being
tested; this is what changed after it. Captures: docs/mockups/tower/worlds2-*.png.

**Decided.** (1) The streamer's `!tower part 3` takes only the streamer; `all` moves the group. (2) `!tower all` moves
**the climbing group only**, see below. (3) Cleared parts are open to any level, with the warning in the reply for a
character far below the part: decided, nothing to build. (4) to (7) and (9) stay as built. (8) is fixed, see below.

**`!tower all` moves the climbing group.** Players who went to a cleared part by themselves (`!tower part 3`) stay
where they are when the group moves, as the players in the town always did. `TowerManager.OnOwnWay` marks them from
their own trip until they type `!tower` (back to the climb) or leave; `GroupPlayers` is everyone else on the tower's
parts. They still see a boss countdown on the panel and join it with `!tower`. Only the group votes for where the
group goes (a majority of its members who are here; someone on a part of their own is told why their vote does not
count), the streamer or a moderator moves it at once, and one of them who is on a part of their own takes the group
along and goes with it. When no place is free for the part the group goes to (two parts are open for players on their
own), it opens as a fourth for the moment the group needs to walk over and the part the group left closes at once; a
loaded part is only turned into another where it stands when nobody but the group is on it. `!tower` from outside
while no place is free for the climb's part goes to where the group is. In potato mode the one open part is
everyone's, so there `all` moves everyone as before. Played: two players on parts of their own while the streamer
moved a group of three (to a second copy of the crypt, on the fourth place), a newcomer joining the group there, a
vote of the group (4 members: "2 more votes", refused for both players on their own way), the group back to the
climb, a player back from their own way and moved with the group, the streamer taking the group along from a part of
their own. Not played: potato mode after the change (its rule is the old one, by one line), real chat.

**The seals of the crypt and the frost vault were redone.** The door captures of the first round did not show a seal
that says "shut" from the game's camera: thin thorns and pea-sized wisps in the crypt, and in the vault a few ice
crystals among a room full of the same ice. Now every sealed door has a **curtain of light** across the whole
doorway, from the floor to the top of the leaves, with a strip of the same light on the floor before it
(`TowerWard`: a mesh, a generated texture and an additive material made in code, rising slowly; it stands still in
potato mode and stays on, because it tells what the game does). The place's own barrier stands in the light: a hedge
of thorns as high as a player in the crypt (dark against the grave green), a wall of pale ice crystals in the vault
(a cold white-blue, lighter than the vault's own ice). The seal's light stands further before the door, so it no
longer shines through into the next room. Judged from the pictures: at the normal camera (25 m, 45 degrees) a sealed
door is the brightest thing in its room and has the shape of the doorway, an open one is a dark gap; from straight
above the strip on the floor still marks it. What is left: from the next room a thin line of the light shows through
the crack between a leaf and the frame.
Two faults found on the way and fixed: **the vault's doors never shut.** The pack's stone door is a pair (leaf 01
hinged at its right edge, leaf 02 at its left); the vault had leaf 02 on both hinges placed as if it were leaf 01,
so both leaves stood over the walls beside the opening, behind the ice, and the doorway was only closed by the seal
and the navmesh. Both places now use the pair as it is made, and the leaves swing into the next room.
`tools/tower/tower-audit.cs` checks every leaf (shut: in the doorway; open: in the next room): 38 checks, 0
problems, both scenes.

**The frame of the wrong light at a camera cut is fixed.** The look of a tower place (light, fog, haze, no sea or
sky clouds) was set in `TowerManager.LateUpdate` from where the camera stood, and the game's camera moves in its own
LateUpdate, later in the frame: on the frame of a cut the place was drawn in the island's light, and the island in
the place's on the cut back. Measured before: 20 of 20 cuts wrong on their first frame, both ways. The look is now
set by `TowerLook`, a late LateUpdate on the tower manager's object, after every camera has moved: 20 cuts, 500
frames, 0 wrong.

**The meters window in the tower** (the dungeon's, `DungeonMetersWindow`; one window, one run at a time). A boss
fight (mini boss or floor boss) opens it when the fight begins, after the intro; it ranks damage done, healing done
and damage taken, is titled with the fight ("Floor 1, part 10 - Viridia, the Verdant Wyrm"), says "MINI BOSS" or
"FLOOR BOSS" while it runs and "VICTORY" or "DEFEAT" after, lingers the dungeon's 60 seconds and is hidden by its
close button until the next fight. On a floor boss the result line names the last blow ("VICTORY  Last blow:
Bot12876", from the game at once, then as the server gave the title) and the name stands in gold in its row
(worlds2-meters-floorboss.png, from a real fight). **Which run shows: the one the camera is watching.** With the
camera at a place of the tower it is the tower's (a fight while the camera is on its part; its result wherever in
the tower the camera went, the town after a win); with the camera anywhere else it is the dungeon's as before, and
it comes back when the camera returns within the linger. It is never over a tower cutscene and never over the
tower panel's floor map. The tower panel has a **Meters** button for the part being watched (its damage since its
players arrived, or since the window's "count from now" button). The tower's damage is counted where a monster
loses health (`EnemyController.TakeDamage`), so skills, effects and pets count and the damage on a guardian adds up
to its health exactly: 98,710 of 98,710 (mini boss), 74,970 of 74,970 (floor boss, 9 players), 150,960 of 150,960
(30 players). `!dps` adds the fight ("Floor 1, part 10 - Viridia, the Verdant Wyrm: 8.8K DMG, 0 healed, rank 2 of
9") during it and for five minutes after. The window keeps the place and size the dungeon meters have (remembered
per machine). Its type went up to the stream sizes (names 20 px), with the name column given most of the width.
Not done: a setting of its own, and healing by skills is counted as the dungeon counts it (the heal action only).

**Boss fights, from what the owner saw** (floor 1, part 10, nine players):

- *Damage numbers add up into one over a guardian*, as over a dungeon or raid boss (`EnemyController.MergeDamage`,
  set by `TowerFloor.SpawnBoss`). A part's own monsters do the same once five players are on one
  (`MergeWhenCrowded`); the world's monsters are as they were. Seen with 9 and with 30 attackers: one large number
  over the guardian, the helpers' and the players' own numbers beside it.
- *Knocked out players lay down and slid away.* A body was still given orders: on a part of rooms the walk to its
  place in the guardian's room, elsewhere the way to the next monster. `TowerHandler.Update` now leaves a knocked
  out player alone. Measured on 20 knockouts (several at the same blow, 5 and 30 players): 0.00 m moved while lying;
  up by the arrival about 6 seconds later.
- *Recovering, and back into the fight* (the owner: "so they can keep helping"). A knocked out player is no longer
  out for the attempt: up by the arrival they recover for **40 seconds** (`RavenNest.Combat.TowerRecovery.Seconds`,
  one fixed time on every floor and for every knockout) and then walk back into the fight at **half their health**
  (`ReturnHealth`). While they recover the guardian does not come after them and its slams do not touch them. The
  attempt is lost when nobody stands at the same moment, or when the time runs out. So that nobody wonders why they
  do not help: "Knocked out" over them as they fall, "Recovering 23s" now and then while they wait, "Back in the
  fight" when they go; their health bar fills to the half as the time passes (over their head and in the players
  list); each goes to a place of their own beside the arrival (2.5 to 7.5 m to one side of the way in, so the
  doorway and the walk to the guardian stay clear, never within the slam's reach plus 5 m) and waits there their
  own way for the whole attempt: half of them sit, a fifth sit cross-legged, a fifth stand, one in ten cheers the
  others on. The falls reported for the hidden achievement and the Last Blow rule are unchanged. Played: recovered
  players back in the fight; a second knockout of the same player; everyone down at once with others seconds from
  getting up (lost); the fight won with 12 of 30 recovering (the clear names all 30); `!tower leave` and a trip to
  another part while sitting (up, not sitting, not marked); the camera on a recovering player; after a loss
  everyone free and whole. Not done: weapons are not put away while sitting (the game has no cheap way), a healer
  does not shorten the wait (the heal picks its target by health in another place), no effect on the name tag.
- *Challenge and Start now.* The tower panel has a filled **Challenge** button when the guardian is open (the
  streamer's `!tower boss`) and **Start now** during the countdown. In chat `!tower boss now` (also `start`, `go`)
  from the streamer or a moderator, and their `!tower boss` typed again during the countdown, begin the fight at
  once with everyone who has joined; the first-time intro still plays and a `!tower` during it counts. Chat is told
  in one line ("The countdown is cut short: the fight with Viridia, the Verdant Wyrm starts now, with the 5 who
  joined."). A viewer's `!tower boss now` is refused.
- *The players list follows the camera into the tower.* While the camera is on a part of the tower the list is that
  part's players ("TOWER 9"), a boss fight's first (standing, then recovering), with "Knocked out" and "Recovering"
  by the name and the row dimmed; a switch in the header ("All" / "Tower") shows everyone and back, and outside the
  tower it is everyone as always. One pass over the players per refresh, rows rebuilt only when who is listed
  changes; the window's place and size are not touched. Found on the way: the list's health bars measured health
  against the health skill's level (a tenth of the real maximum), so they stood full until a player was nearly
  down; they now use the real maximum.
- *A lost attempt says so on screen.* "Defeated" in the red of the boss names, the reason under it ("Everyone was
  knocked out" or "Time ran out") and what the guardian had left ("Viridia had 44% left"), for seven seconds over
  the game as it goes on, never over a cutscene. While the group regroups the panel's bar is the guardian's health
  left, in red, with "Everyone was knocked out - Viridia had 44% left", and its line reads "Viridia still stands -
  next attempt in 1:45"; then the Challenge button is back. With the objectives done the bar reads "Objectives
  done", not "Objectives 2 / 2", and during a fight the panel counts "7 standing, 2 recovering".
- *The panel squeezed between two windows* (the players list and the player details): its Map and Meters buttons
  are their icons, the chat hint gives way to the Challenge button, and "TOWER" gives its room to the clock.

**The simulator with recovery** (`tower 200` against `tower 200 norecover`; fresh players at each part's level in
the best gear for it; won of 200 fights; only the guardians where anything is under 100):

*Fresh players (before -> with recovery):*

| Part | Guardian | 2 players | 5 players | 10 players | 30 players |
|---|---|---|---|---|---|
| 1-1 | Nibbles the Vicious | 100% | 99% -> 100% | 100% | 100% |
| 1-2 | Warchief Grubnose | 97% -> 100% | 74% -> 100% | 100% | 100% |
| 1-3 | The Brood Mother | 61% -> 100% | 41% -> 100% | 100% | 100% |
| 1-4 | Mortimer Bonewhistle | 66% -> 95% | 49% -> 100% | 100% | 100% |
| 1-5 | Old Thornback | 93% -> 100% | 64% -> 100% | 100% | 100% |
| 1-6 | The Moo-tant | 92% -> 100% | 64% -> 100% | 100% | 100% |
| 1-7 | Big Gorp, the Keep Colossus | 98% -> 100% | 94% -> 100% | 100% | 100% |
| 1-8 | Lord Pestilus | 89% -> 98% | 49% -> 100% | 100% | 100% |
| 1-9 | Old Rotbelly | 100% | 99% -> 100% | 100% | 100% |
| 2-6 | Sigrid the Unthawed | 100% | 99% -> 100% | 100% | 100% |
| 2-8 | The Thawed Ronin | 85% -> 100% | 80% -> 100% | 100% | 100% |
| 2-9 | The Moo-tant, Refrozen | 100% | 99% -> 100% | 100% | 100% |
| 3-1 | Captain Gale Blackwind | 82% -> 100% | 96% -> 100% | 100% | 100% |
| 3-2 | Zap, Nibbles' Shocking Cousin | 51% -> 100% | 66% -> 100% | 100% | 100% |
| 3-3 | Master Kaze | 4% -> 88% | 4% -> 100% | 100% | 100% |
| 3-4 | Mother Squall | 77% -> 100% | 90% -> 100% | 100% | 100% |
| 3-5 | Captain Quackbeard | 24% -> 98% | 47% -> 100% | 100% | 100% |
| 3-6 | Champion Voltus | 19% -> 99% | 37% -> 100% | 100% | 100% |
| 3-7 | Skyfang | 81% -> 99% | 90% -> 100% | 100% | 100% |
| 3-8 | High Warden Aerin | 95% -> 100% | 96% -> 100% | 100% | 100% |
| 3-9 | The Moo-tant, Supercharged | 93% -> 100% | 95% -> 100% | 100% | 100% |

*Veterans:*

| Part | Guardian | 2 players | 5 players | 10 players | 30 players |
|---|---|---|---|---|---|
| 1-3 | The Brood Mother | 92% -> 97% | 100% | 100% | 100% |
| 1-4 | Mortimer Bonewhistle | 81% -> 94% | 99% -> 100% | 100% | 100% |
| 1-5 | Old Thornback | 98% -> 100% | 100% | 100% | 100% |
| 1-6 | The Moo-tant | 96% -> 100% | 100% | 100% | 100% |
| 1-7 | Big Gorp, the Keep Colossus | 97% -> 100% | 100% | 100% | 100% |
| 1-8 | Lord Pestilus | 94% -> 98% | 100% | 100% | 100% |
| 2-1 | Jarl Rimebeard | 99% -> 100% | 100% | 100% | 100% |
| 2-2 | Snowball, Nibbles' Big Sister | 96% -> 100% | 100% | 100% | 100% |
| 2-3 | The Frozen Warden | 99% -> 100% | 100% | 100% | 100% |
| 2-6 | Sigrid the Unthawed | 99% -> 100% | 100% | 100% | 100% |
| 2-8 | The Thawed Ronin | 89% -> 100% | 100% | 100% | 100% |
| 2-9 | The Moo-tant, Refrozen | 97% -> 99% | 100% | 100% | 100% |
| 3-1 | Captain Gale Blackwind | 69% -> 100% | 100% | 100% | 100% |
| 3-2 | Zap, Nibbles' Shocking Cousin | 39% -> 98% | 100% | 100% | 100% |
| 3-3 | Master Kaze | 2% -> 47% | 65% -> 100% | 100% | 100% |
| 3-4 | Mother Squall | 61% -> 91% | 100% | 100% | 100% |
| 3-5 | Captain Quackbeard | 17% -> 80% | 99% -> 100% | 100% | 100% |
| 3-6 | Champion Voltus | 31% -> 99% | 97% -> 100% | 100% | 100% |
| 3-7 | Skyfang | 81% -> 97% | 100% | 100% | 100% |
| 3-8 | High Warden Aerin | 93% -> 100% | 100% | 100% | 100% |
| 3-9 | The Moo-tant, Supercharged | 90% -> 99% | 100% | 100% | 100% |

Ten and thirty players won every fight before and after. With 60 seconds in place of 40 a group of five wins 99 to
100 of 100, two players 71 to 100; with 90 seconds five lose the harder mini bosses about one time in ten and two
players as often as with no recovery. **The guardians' own numbers were not changed** in that round: with recovery a
fresh group of five in the best gear did not lose a mini boss in the simulator any more (it lost the harder ones one
time in two to four before). The tuning round below put the risk back into the guardians, floor by floor.

**Also played in this round.** The floor map's arrows with pointer events (floor 2 to 1 and back, each greyed at
its end). A gather objective fed by real gathering: the dev character mining alone, +53 in 336 seconds, which is the
lone gatherer's 1.55 a round at a round every 10 seconds (two gatherers could not be played: the bots are not
characters on the server). The hidden things of a floor 2 part (2-8, five players): the first after 5 minutes 12
seconds (designed 5).

**Open calls for the owner (this round).**

1. **Recovery makes the guardians easy for a group of five** in the simulator (table above). Raising the guardians'
   numbers per floor is the lever; it was not touched.
2. **The recovery time is 40 seconds on every floor** (`TowerRecovery.Seconds`), half health on return.
3. **Which meters show**: the run the camera is watching (tower in the tower, dungeon elsewhere).
4. **Damage numbers of a part's monsters** add up from five attackers on; the world's monsters do not.
5. **The seal is a curtain of light at every sealed door**, green in the crypt and white-blue in the vault.
6. **The players list's health bars** now show real health everywhere (they stood full before).
7. **The meters window's type is larger**, in dungeons too.

### The guardians rebalanced for recovery (2026-11-24, the tuning round)

With recovery (40 seconds, half health; unchanged, one fixed number) a fresh group of five won every guardian, so
the fights had no risk left. A fight is now only lost when nobody stands at the same moment, and a guardian's health
does not make that happen: what does is how hard it strikes, because its slam is a share of a blow and reaches
everyone standing near it. So the risk was put back into the blows, and only a floor boss got more health, to make
its fight clearly longer than a mini boss's.

**What changed, all in `RavenNest.Combat` (`TowerPressure`, applied in `Tower.Boss(floor, pressure)`):**

- `Fury(floor, floorBoss)`: what a guardian's blows (and with them its slam and spit) are multiplied by against a
  full group. Mini bosses: **1.45 on floor 1, 2.2 on floor 2, 1.8 on floor 3**; floor bosses: **1.5, 1.65, 1.45**.
  Floor 2's number is the biggest because its guardians were the softest for their floor (with 1.3 a group of five
  still won all of them); the aim is the risk, which grows with every floor (below). From floor 4 on there are no
  guardians to measure: the rule gives floor 3's number and a tenth more with every floor (mini bosses up to 2.6,
  floor bosses up to 2.2), to be measured when a floor is made.
- `FloorBossPool` = 1.8: a floor boss's health against a full group, so its fight takes five players about 4.5 to 5
  minutes where a mini boss takes 3 to 4.
- `FuryShare`: a group smaller than the part's suggested five meets that share of both (two players: 40% of the extra
  blows and of the extra health). The small-group mercy and the scaling by pressure work as before, under it.
- Nothing else: the slam's share, reach and rhythm, the helpers, the enrage and the time limit are as they were.
  Tried and dropped: a bigger slam alone (with twice its share, 1.2 blows, five still won 99 to 100 of 100 on floors 1
  and 2), and the same blows for every group (two players fell to 34 of 100 on floor 3).

**The simulator, before and now** (`tower 200`; both with recovery; won of 100, the mini bosses of a floor as lowest
/ mean / highest, and the minutes a won fight took):

*Fresh synced players:*

| Floor | Players | Mini bosses before | Mini bosses now | Floor boss before | Floor boss now |
|---|---|---|---|---|---|
| 1 | 2 | 95 / 99 / 100, 3.2 min | 81 / 94 / 100, 3.3 min | 100, 5.1 min | 100, 6.6 min |
| 1 | 5 | 100 / 100 / 100, 2.7 min | 72 / 91 / 100, 3.1 min | 100, 2.6 min | 91, 5.1 min |
| 1 | 10 | 100 / 100 / 100, 1.7 min | 100 / 100 / 100, 1.8 min | 100, 2.3 min | 100, 4.2 min |
| 1 | 30 | 100 / 100 / 100, 1.2 min | 100 / 100 / 100, 1.2 min | 100, 1.4 min | 100, 2.4 min |
| 2 | 2 | 100 / 100 / 100, 3.6 min | 84 / 97 / 100, 3.9 min | 100, 4.7 min | 100, 6.1 min |
| 2 | 5 | 100 / 100 / 100, 2.8 min | 49 / 82 / 100, 3.5 min | 100, 2.2 min | 86, 4.5 min |
| 2 | 10 | 100 / 100 / 100, 2.2 min | 100 / 100 / 100, 2.3 min | 100, 2.0 min | 100, 3.8 min |
| 2 | 30 | 100 / 100 / 100, 1.7 min | 100 / 100 / 100, 1.8 min | 100, 1.5 min | 100, 2.6 min |
| 3 | 2 | 88 / 98 / 100, 4.6 min | 14 / 73 / 97, 4.8 min | 100, 5.9 min | 100, 7.8 min |
| 3 | 5 | 100 / 100 / 100, 3.2 min | 22 / 70 / 97, 4.0 min | 100, 2.6 min | 90, 5.1 min |
| 3 | 10 | 100 / 100 / 100, 2.7 min | 100 / 100 / 100, 2.9 min | 100, 2.4 min | 100, 4.4 min |
| 3 | 30 | 100 / 100 / 100, 2.4 min | 100 / 100 / 100, 2.5 min | 100, 2.1 min | 100, 3.6 min |

*Veterans (a tree's passives on the synced kit):*

| Floor | Players | Mini bosses before | Mini bosses now | Floor boss before | Floor boss now |
|---|---|---|---|---|---|
| 1 | 2 | 94 / 99 / 100, 2.3 min | 78 / 92 / 100, 2.3 min | 100, 3.5 min | 100, 4.8 min |
| 1 | 5 | 100 / 100 / 100, 2.0 min | 97 / 100 / 100, 2.1 min | 100, 2.1 min | 92, 4.1 min |
| 1 | 10 | 100 / 100 / 100, 1.3 min | 100 / 100 / 100, 1.4 min | 100, 1.7 min | 100, 3.0 min |
| 1 | 30 | 100 / 100 / 100, 1.0 min | 100 / 100 / 100, 1.0 min | 100, 1.1 min | 100, 1.9 min |
| 2 | 2 | 99 / 100 / 100, 2.6 min | 23 / 67 / 100, 2.8 min | 100, 3.3 min | 94, 4.7 min |
| 2 | 5 | 100 / 100 / 100, 2.2 min | 81 / 97 / 100, 2.6 min | 100, 2.1 min | 64, 4.5 min |
| 2 | 10 | 100 / 100 / 100, 1.8 min | 100 / 100 / 100, 1.9 min | 100, 1.7 min | 100, 3.0 min |
| 2 | 30 | 100 / 100 / 100, 1.5 min | 100 / 100 / 100, 1.6 min | 100, 1.3 min | 100, 2.2 min |
| 3 | 2 | 47 / 90 / 100, 3.2 min | 1 / 32 / 64, 3.3 min | 100, 4.0 min | 97, 5.7 min |
| 3 | 5 | 100 / 100 / 100, 2.6 min | 57 / 91 / 100, 3.1 min | 100, 2.3 min | 76, 4.6 min |
| 3 | 10 | 100 / 100 / 100, 2.3 min | 100 / 100 / 100, 2.5 min | 100, 2.0 min | 100, 3.6 min |
| 3 | 30 | 100 / 100 / 100, 2.1 min | 100 / 100 / 100, 2.2 min | 100, 1.8 min | 100, 3.1 min |

Read against the aims (fresh players, no healers or skills): five win a mini boss 91, 82 and 70 times in 100 on
floors 1, 2 and 3, and the floor bosses 91, 86 and 90, in about 5 minutes against 3 to 4. Two players win 94, 97 and
73 of 100 mini bosses and every floor boss, in 6 to 8 minutes (the limit is 10). Ten and thirty still win
everything; their fights take 1.2 to 2.9 minutes for a mini boss and 2.4 to 4.4 for a floor boss.

**What was not right yet** (both fixed in the second tuning round, below).

- *The spread within a floor is wide.* The hardest mini bosses for five are The Brood Mother (72), The Moo-tant,
  Refrozen (49), The Thawed Ronin (58), Captain Quackbeard (48), Mother Squall (62) and Master Kaze (22; he was
  the hardest before recovery too, 4 of 100). One number per floor cannot even them out: that takes each
  guardian's own numbers.
- *Veterans do worse than fresh players in two places.* A group's strength counts as pressure, so a guardian is
  bigger for veterans, and the fury multiplies that: two veterans win 67 of 100 mini bosses on floor 2 and 32 on
  floor 3 (fresh: 97 and 73), and five veterans win the floor bosses of floors 2 and 3 only 64 and 76 times (fresh:
  86 and 90). Five veterans against mini bosses do better than fresh players (100, 97, 91). This needs a look at
  how strength is counted against a guardian; it was not changed here.

**Played after it** (editor, local server, bots; the bots are above the parts' levels, so they are level-synced
players without skills or healers, as the simulator's):

| Fight | Players | Guardian's health | Outcome | Knockouts | Most down at once | The simulator says |
|---|---|---|---|---|---|---|
| Floor 1 boss, Viridia (1-10) | 5 | 98,140 | won after 5.5 min | 9 | 3 (2 left standing) | 91 of 100, 5.1 min |
| Floor 2 mini boss, Jarl Rimebeard (2-1) | 5 | 75,670 | won after 3.2 min | 4 | 3 (2 left standing) | floor 2: 82 of 100, 3.5 min |
| Floor 1 boss, Viridia (1-10) | 9 | 169,690 | won after 5.6 min | 18 | 4 (5 left standing) | ten: 100 of 100, 4.2 min |
| Floor 2 mini boss, Jarl Rimebeard (2-1) | 9 | 111,130 | won after 3.0 min | 6 | 4 (5 left standing) | ten: 100 of 100, 2.3 min |

One fight each, so this says the kind, not the odds. The game agrees with the simulator in kind: the fights last what
it says for five (a little longer for nine than its ten), a floor boss is clearly the longer fight, players fall all
through the fight and come back, and with five both fights came down to two players standing at their worst moment:
one more knockout at the wrong time and they were lost. Nine never came near it. In every fight the meter's damage
on the guardian was its health to the point. The mini boss played is floor 2's first (the floor 1 boss's win leads
there); no floor 1 mini boss was played in this round.

**The leftovers of the last round.**

- *The meters window keeps clear.* It never lies over another HUD window (the player details, the players list, any
  other), the tower panel or the pills in the bottom left corner: where its remembered place would, it stands at the
  nearest free place (a search on a 24 px grid, only when something is in the way), goes back when its own place is
  free again, and a drag by hand makes the new place its own. It is moved through `DraggableWindow.Nudge`, which
  saves nothing (written straight into the style, the window's own keeper put it back on the next layout). Seen:
  from its remembered place under the "Bot offline" pill it stood 82 px higher, clear of the pill, the player
  details and the players list; with the players list in its way, beside it. The window's panel measures the
  screen in pixels, so this was seen at the editor's 2560 by 1440 only; 1920 by 1080 and 1280 by 720 were not
  captured.
- *The last blow has a line of its own* under the outcome in the result ("VICTORY  Closes in 52s" and under it "Last
  blow: Bot53442, Bot36390, Bot28142"), so nothing runs into the timer; seen with three names from a real fight.
- *Captures retaken:* worlds2-boss-lost.png (the title under the tower panel, clear of the name tags of nine players
  lying under it) and worlds2-meters-floorboss.png (a real fight of five and its real result).
- *The line of light at a sealed door's jamb, seen from the next room, is still there.* The curtain is now narrower
  than the two leaves, which did not remove it: it is the lit side of the doorway seen through the gap the pack's
  leaf leaves at the frame. Not quick; left.
- *Not done:* a loss by time, and the first-time intro after Start now.

### The second tuning round: veterans, and each guardian's own numbers (2026-11-24)

Two weak points of the round above were faults, and are fixed in `RavenNest.Combat`; recovery (40 seconds, half
health), the floors' fury, the floor bosses' health and the unmeasured rule for floors beyond 3 are as they were.

**Veterans never do worse than fresh players.** A group's strength counted as heads: two veterans were "2.8 fresh
players" to the rule, so the small-group mercy (softer blows the fewer heads there are) was taken away again, and
with the harder blows they lost fights two fresh players won. Five veterans met a harder floor boss than five fresh
players for the same reason (a floor boss is measured against eight). Now `TowerPressure.Of` counts strength by
the group's heads (`StrengthShare`): **nothing under 0.6 of the group a fight is made for, all of it from a full
group on**, where it mostly buys the guardian's health (its blows hardly follow the pressure). So below a full group
a player's trees, gems and enchantments are all their own, and from a full group on a strong group gets the longer
fight. In the simulator a veteran group now wins at least as often as a fresh one against every guardian, with 2, 5,
10 and 30 players (the smallest gap is 0). What this does not do: under a full group strength buys no longer fight
either (two veterans meet the fight two fresh players meet, and win it faster). For strength to buy only health
there, the game has to hand the group's strength to the rule beside its heads; the game was not touched in this
round.

**The guardians that stood out, and why** (`tower why 3,18,19,23,24,25 300`: what five fresh players win with one
thing taken away at a time). The slam is what loses a fight for five against every guardian (without it they win
86 to 99 of 100 everywhere); what set these six apart was something on top of it:

| Guardian | Five won | What made it hard | Changed |
|---|---|---|---|
| The Brood Mother (1-3) | 68 | four helpers at once, the most on its floor (91 without them), and a spit on top (86 without) | three helpers, the spit 1.0 of a blow |
| The Thawed Ronin (2-8) | 60 | the hardest blows of its floor, power 1.25 (87 with a plain power) | power 1.1 |
| The Moo-tant, Refrozen (2-9) | 44 | 60% of every blow is ice and its spit is 1.2 blows (72 without either) | 40% ice, the spit 1.0 |
| Master Kaze (3-3) | 23 | a blow every 2 seconds at power 1.15: the slam comes every 10 seconds instead of 12.5 (83 striking every 2.5 s), and for two players his three helpers (23 to 69 without) | still every 2 seconds, at power 0.95, the slam every sixth swing (12 s), two helpers |
| Mother Squall (3-4) | 68 | her spit (88 without) on top of two calls for helpers and lightning | the spit 1.0 |
| Captain Quackbeard (3-5) | 58 | his spit (79 without; for two players 41 to 64) and a tenth more health than the others | the spit 0.9, a plain pool |

**Each guardian's own edge** (`TowerBoss.Edge`, beside its other numbers in `Tower.cs`): what its floor's fury is
multiplied by for it, found with `tower tune 300` (by halving, to the part's aim: the floor's 90, 80 or 70, five more
on a floor's first part and five fewer on its ninth). Floor 1: Nibbles 1.05, Grubnose 1.05, The Brood Mother 0.95,
Mortimer 1.05, Old Thornback 1.2, The Moo-tant 0.95, Big Gorp 1.25, Lord Pestilus 0.95, Old Rotbelly 1.2. Floor 2:
Jarl Rimebeard 1.2, Snowball 1.2, The Frozen Warden 1.05, The Wailing Matron 1.4, Emperor Quackers 0.95, Sigrid 1.05,
Hoarfang 1.1, The Thawed Ronin 1, The Moo-tant Refrozen 0.95. Floor 3: Gale Blackwind 1.2, Zap 1.05, Master Kaze
1.05, Mother Squall 1, Quackbeard 1, Voltus 1.05, Skyfang 1.1, Aerin 1.1, The Moo-tant Supercharged 1.1. The floor
bosses keep 1.

**Every guardian, before and now** (`tower 300`; won of 100; before is the round above):

| Part | Guardian | Two fresh | Five fresh | Two veterans | Five veterans |
|---|---|---|---|---|---|
| 1-1 | Nibbles the Vicious | 100 | 98 -> 96 | 100 | 100 |
| 1-2 | Warchief Grubnose | 99 | 96 -> 94 | 100 | 100 |
| 1-3 | The Brood Mother | 90 -> 99 | 72 -> 92 | 82 -> 100 | 99 -> 100 |
| 1-4 | Mortimer Bonewhistle | 81 | 94 -> 90 | 78 -> 100 | 100 |
| 1-5 | Old Thornback | 100 -> 99 | 99 -> 90 | 99 -> 100 | 100 |
| 1-6 | The Moo-tant | 92 -> 97 | 81 -> 90 | 88 -> 100 | 100 |
| 1-7 | Big Gorp, the Keep Colossus | 100 | 100 -> 85 | 100 | 100 |
| 1-8 | Lord Pestilus | 84 | 81 -> 86 | 80 -> 100 | 97 -> 100 |
| 1-9 | Old Rotbelly | 100 | 100 -> 94 | 100 | 100 |
| 1-10 | Viridia, the Verdant Wyrm (floor boss) | 100 | 91 -> 90 | 100 | 92 -> 100 |
| 2-1 | Jarl Rimebeard | 100 | 100 -> 89 | 83 -> 100 | 100 -> 99 |
| 2-2 | Snowball, Nibbles' Big Sister | 100 | 98 -> 79 | 57 -> 100 | 99 -> 96 |
| 2-3 | The Frozen Warden | 100 | 88 -> 83 | 33 -> 100 | 99 |
| 2-4 | The Wailing Matron | 100 | 100 -> 74 | 100 | 100 -> 99 |
| 2-5 | Emperor Quackers | 100 -> 99 | 83 -> 85 | 79 -> 100 | 97 -> 99 |
| 2-6 | Sigrid the Unthawed | 99 -> 95 | 82 -> 75 | 90 -> 100 | 100 -> 98 |
| 2-7 | Hoarfang | 98 -> 91 | 83 -> 76 | 86 -> 100 | 99 -> 94 |
| 2-8 | The Thawed Ronin | 84 -> 94 | 58 -> 73 | 53 -> 100 | 94 -> 96 |
| 2-9 | The Moo-tant, Refrozen | 89 -> 100 | 49 -> 84 | 23 -> 100 | 81 -> 98 |
| 2-10 | Glacius, the Winter Wyrm (floor boss) | 100 | 86 -> 85 | 94 -> 100 | 64 -> 100 |
| 3-1 | Captain Gale Blackwind | 97 -> 82 | 97 -> 68 | 55 -> 100 | 100 -> 92 |
| 3-2 | Zap, Nibbles' Shocking Cousin | 77 -> 75 | 80 -> 75 | 21 -> 100 | 97 -> 96 |
| 3-3 | Master Kaze | 14 -> 37 | 22 -> 66 | 1 -> 100 | 57 -> 92 |
| 3-4 | Mother Squall | 81 -> 84 | 62 -> 72 | 24 -> 100 | 98 |
| 3-5 | Captain Quackbeard | 52 -> 79 | 48 -> 65 | 5 -> 100 | 79 -> 94 |
| 3-6 | Champion Voltus | 69 | 76 -> 73 | 23 -> 100 | 99 -> 93 |
| 3-7 | Skyfang | 78 -> 64 | 84 -> 71 | 35 -> 100 | 98 -> 89 |
| 3-8 | High Warden Aerin | 96 -> 86 | 84 -> 69 | 64 -> 100 | 100 -> 98 |
| 3-9 | The Moo-tant, Supercharged | 91 -> 78 | 77 -> 61 | 56 -> 100 | 95 -> 89 |
| 3-10 | Fulmina, the Thunder Wyrm (floor boss) | 100 -> 98 | 90 -> 88 | 97 -> 100 | 76 -> 100 |

*Veterans by floor, before and now (mini bosses lowest / mean / highest and minutes, floor boss and minutes):*

| Floor | Players | Mini bosses before | Mini bosses now | Floor boss before | Floor boss now |
|---|---|---|---|---|---|
| 1 | 2 | 78 / 92 / 100, 2.3 min | 100 / 100 / 100, 2.2 min | 100, 4.8 min | 100, 4.5 min |
| 1 | 5 | 97 / 100 / 100, 2.1 min | 100 / 100 / 100, 2.1 min | 92, 4.1 min | 100, 3.2 min |
| 1 | 10 | 100 / 100 / 100, 1.4 min | 100 / 100 / 100, 1.4 min | 100, 3.0 min | 100, 3.0 min |
| 1 | 30 | 100 / 100 / 100, 1.0 min | 100 / 100 / 100, 1.0 min | 100, 1.9 min | 100, 1.9 min |
| 2 | 2 | 23 / 67 / 100, 2.8 min | 100 / 100 / 100, 2.5 min | 94, 4.7 min | 100, 4.1 min |
| 2 | 5 | 81 / 97 / 100, 2.6 min | 94 / 98 / 99, 2.6 min | 64, 4.5 min | 100, 2.7 min |
| 2 | 10 | 100 / 100 / 100, 1.9 min | 100 / 100 / 100, 1.9 min | 100, 3.0 min | 100, 3.0 min |
| 2 | 30 | 100 / 100 / 100, 1.6 min | 100 / 100 / 100, 1.6 min | 100, 2.2 min | 100, 2.3 min |
| 3 | 2 | 1 / 32 / 64, 3.3 min | 100 / 100 / 100, 3.1 min | 97, 5.7 min | 100, 5.1 min |
| 3 | 5 | 57 / 91 / 100, 3.1 min | 89 / 93 / 98, 3.1 min | 76, 4.6 min | 100, 3.1 min |
| 3 | 10 | 100 / 100 / 100, 2.5 min | 100 / 100 / 100, 2.4 min | 100, 3.6 min | 100, 3.6 min |
| 3 | 30 | 100 / 100 / 100, 2.2 min | 100 / 100 / 100, 2.2 min | 100, 3.1 min | 100, 3.1 min |

Read against the aims: five fresh players win the mini bosses of floor 1 85 to 96 times in 100, of floor 2 73 to
89, of floor 3 61 to 75, every one within ten of its floor's 90, 80 and 70; the floor bosses 90, 85 and 88. Two
fresh players win at least 81 on floors 1 and 2 and at least 64 on floor 3 but for Master Kaze (37, who was 14).
Ten and thirty win everything as before. A run of 300 fights moves a guardian by three to five wins from one run to
the next, so the order inside a floor (later parts a little harder) holds on the whole and not for every
neighbour: the edges are aimed at it, the table's single numbers wobble around it.

**Played after it** (editor, local server): Master Kaze's part (3-3, The Wind Bridges) once, with five bots above
the part's level: won after 3.9 minutes (the simulator: 66 wins of 100, about 4 minutes), 8 knockouts, three down at
once at the worst moment with two left standing, the meter's damage on him his health to the point (164,090). A sane
fight that could have been lost; it is the first fight played on floor 3. One fight, so it says the kind and not the
odds. Not played: veterans (the bots have no trees or gems), two players, the other five changed guardians.

## 14. Gates and breaks: the owner's decisions (2026-11-26)

Decided by the owner. Planned for v0.9.8 on my recommendation (the owner was fine with waiting; reasons below):

- **Gates are joined from the world.** Nobody has to be in the tower to join a gate or a break; a gate opens on an
  island like a dungeon does today. Dungeon scrolls still open one.
- **Climbers are never auto joined** into a raid, dungeon, gate or break. They choose by hand: `!raid join` takes a
  climber out of the tower at once, `!dungeon join` when the dungeon starts (built in v0.9.7, game 9fc4135f). So some
  players climb while others fetch the tower's key items from gates.
- **Breaks: all of section 5 and the havoc ideas are approved.** A gate left too long breaks; by default an uncleared
  break grows stronger, spreads to the next island, its monsters go for players gathering or training there, and the
  village loses its bonus while it is open. As a streamer opt-in, a break also damages village buildings (their bonus
  is gone until repaired with resources). Never lost exp, items or training time.
- **A setting turns breaks off** (gates then just close), as Ravebridge uprisings have.

Why v0.9.8: about the size of the tower's steps 1 and 2 together (floor-themed gates from the room kits, ranks, five
goal kinds, red gates, rewards and key items on the server, the GateItem objective in tower parts, portals on the
islands, breaks with spreading and village damage, settings and texts). v0.9.7 reaches only three floors, so gates
would have three looks; floor 4 (Dark Fortress) comes with v0.9.8. And key items change the tower's pace, which the
v0.9.7 playtest has not measured yet.

## 15. Pace, monsters, levels and experience: the owner's decisions from the bot playtest (2026-11-26, built 2026-11-27)

The owner watched the bot playtest of 2026-11-26 (docs/releases/v0.9.7.0a-playtest.md) and decided, for v0.9.7:

- **A part takes about 20 to 30 minutes for five players**, 25 on average. Six bots needed about 90 minutes for
  Hare Hollow ("too long for sure"): viewers have to stay interested, and the clears are the moments.
- **Tower monsters must not feel dead.** They stood still until hit. They wander as on the islands, and notice
  players near them.
- **A level curve**: floor 1's parts at levels 1 to 100, floor 2's at 100 to 250, floor 3's at 250 to 400, flattening
  toward 999 around floor 10, so a stream has to level up before the higher floors. Level sync stays as it is. Going
  past 999 is a later design (a few floors per update after v0.9.7).
- **Climbing is never worse than the island**, plus 5% more experience a floor with no cap ("they wont reach +50%
  until floor 10"), on the climb's part and cleared parts, for combat skills and Healing; the towns stay at the
  island's rate. Shown on the panel and the floor map.

### What was built (game and RavenNest commits)

| | Game | RavenNest |
|---|---|---|
| Monsters wander | ef020861 | |
| Monsters notice players | 84a2f919 | |
| Packs, respawn by kill rate | 3e2c8a66 | |
| Parts of about 25 minutes, players go for what the part needs | 442f281a | fd89a809 |
| Level curve, the best-gear table, guardians' edges | 48708b00 | 8af8bad0 |
| Experience: never less than the island, +5% a floor | 4fae433a | ed915a3c |

**Wandering.** A tower part has no island, so its monsters counted as dungeon enemies and never wandered.
`EnemyController.TowerMonster` (set by `TowerFloor` on a part's monsters, never on a guardian or its helpers) lets them
take the islands' wander: 1.5 to 4 m about their own spot every 4 to 11 seconds, stopping when a player picks them;
none more than 150 m from the camera, with graphics off or in potato mode (docs: enemy wandering, 2026-10-28).

**Noticing.** Every half second a part looks at its idle monsters: one that sees a player within reach walks over
and attacks, as dungeon monsters do (`TowerFloor.Notice`, `EnemyController.Notice`). Floor 1: 5.5 m, and only while
the player has no monster on them (relaxed). Floor 2 up: 9 m, and up to three monsters on one player; the ones a
player fights count. Not through a wall or a shut gate (a navmesh ray), not from a room not reached, never at players
in a boss attempt, recovering from one or gathering for it (a monster on a player who goes into one of those walks
back to its spot). A noticed player gets a little threat, so a tank's blows pull the monster as usual; a monster keeps
at its player while it can reach them and gives up after 5 seconds out of reach.

**Packs.** Every spot holds a pack of two or three of its kind by turns (`TowerFloor.PackSize`), the others spread
2 to 3 m round the spot on the navmesh, never through a wall or a gate, made in code at spawn so the hand-edited
scenes keep their spots. A part of 24 spots has 60 monsters. Floors of rooms get packs in every room; rooms before the
hold stay cleared, the hold refills. **Respawn by kill rate:** a part keeps no more than a quarter of its monsters down
(`TowerFloor.DownShare`): from the kills it sees a second (smoothed over 45 s) a monster comes back in a quarter of the
part's monsters over that rate, at least 5 s and never slower than the pressure's own time (20 s, down to 8 under a
crowd).

**Players go for what the part needs.** With thirty bots on Hare Hollow the crowd felled the quick kind four times as
fast as the tough one (under its pressure a Frost Raider has 3.8 times its health): 28 Snow Hares a minute, 8 Frost
Raiders, so the raiders alone held the part up longer than five players took. A player looking for the next monster
now counts one of a kind the part does not need now as three times as far plus 10 m (`TowerFloor.FollowNeeds`): the
kill objective furthest behind (within a fiftieth) is wanted, by the stream's progress on the climb and by what fell
there since it opened on a cleared part; a kind with no objective (an elite on the spots) or one already done is not
wanted. So a part's kill objectives fill together.

**The amounts** (`Tower.cs`, RavenNest fd89a809): five players need about 20 minutes for a floor's first part and 30
for its last (20 + 10 x (part - 1) / 9), at the kills a minute five bots made with packs: about 21 on floors 1 and 2
and 23.5 on floor 3 (it was 12 with one standing monster a spot). A part's kill objectives together take those
minutes (a twentieth more, and a seventh more with an elite kind on the spots); on a gather part they take 80% and the
gather amount is 120% of the kills made meanwhile, as before. Hare Hollow asks for 225 Snow Hares and 185 Frost Raiders
(was 800 and 650), the Pasture for 285 cows and 220 bunnies (550 and 425), the Eye of the Storm for 370 drakes and 305
gladiators (1,400 and 1,150). **Hidden things** take 2.75 minutes of searching on floor 1, 3 on floor 2 and 3.25 on
floor 3 (were 4, 5 and 6), at most 4.

**What a kill is worth by the group** (`TowerPace`): the share of a kill that is fighting is 0.85 (was 0.44): with
packs the walk is a small part of a kill. Bots on Hare Hollow made 13.6, 19.6, 26.5 and 35.8 kills a minute with 2, 4,
9.4 and 26.9 heads, which 0.85 fits within 4% (0.8 and 0.9 within 5%, 0.6 within 15%). So a kill by two players counts
about 1.1 (was 1.4): two players kill faster a head than five. A crowd meets monsters with more health the higher the
floor, so its kills come slower: thirty players' kills count 0.72 on floor 1, 0.94 on floor 2, 1.17 on floor 3. The
aim is unchanged: two players 1.4 times five's time, ten about 0.8, thirty about 0.6.

**The level curve** (`Tower.Level`, RavenNest 8af8bad0): floor 1's parts at levels 10 to 100 (ten a part, as
before), floor 2's at 115 to 250 and floor 3's at 265 to 400 (fifteen a part), then 999 - 599 x ((100 - step) / 70)
to the power 1.75, which rises fifteen a step where it starts and flattens to 999 at step 100: 415 at step 31, 542 at
40, 678 at 51, 784 at 61, 871 at 71, 938 at 81, 982 at 91. 999 from step 100 on. Level sync keeps its margin (a tenth
above the part's level): 11 to 110 on floor 1, 127 to 275 on floor 2, 292 to 440 on floor 3.

What reads the level, checked:
- **Sync and the best-gear table.** The table of the best gear wearable at a level had rows at 200, 300 and 500 only,
  so a player synced to 340 to 499 counted as wearing level 280 gear though the Ancient (340) and Atlarus (400) sets
  exist, and floor 3 at its new levels lay in that gap: in the simulator five fresh players won High Warden Aerin 2
  times in 100 and Fulmina 8. The table now has every level where the best gear changes (the simulator's new
  `gearcaps all`). Not in it yet: the Dreadforged weapons (450), first worn on floor 4; the simulator's item list is
  from 2026-11-07.
- **Monsters and guardians** take their levels from the part's: floor 2's are 5 to 25% higher, floor 3's 26 to 33%. Floor 2's
  later parts sit between the level 200 and 280 gear, which no item fills, so five fresh players won Hoarfang 44
  times in 100 where they won 83. Each guardian of floors 2 and 3 keeps its floor's aim with a new edge from the
  simulator's `tower tune 200` (Snowball 1.1, the Frozen Warden 1.2, the Wailing Matron 1.2, Emperor Quackers 0.9,
  Sigrid 1.0, Hoarfang 0.9, the Thawed Ronin 0.9, the Moo-tant Refrozen 0.8; Captain Gale Blackwind 1.25, Zap 1.15,
  Mother Squall 1.15, Captain Quackbeard 1.1, Champion Voltus 1.0, Skyfang 0.95, High Warden Aerin 1.05, the Moo-tant
  Supercharged 0.9), and Glacius and Fulmina have an edge of 0.8. Simulator, five fresh players, wins of 100 before
  and now: floor 2 73 to 90 and 72 to 85, floor 3 56 to 92 and 64 to 90, floor 1 unchanged.
- **Chests**: the class gear tier of a clear follows the part's level: tier 5 from floor 2, part 7 (level 205; was
  from floor 2's floor boss) and tier 6 from floor 3's floor boss (level 400; was tier 5). Gem tiers go by the part,
  not the level: unchanged.
- **The far-below warning** on the way in (under 60% of the part's level): now under 78 for Hare Hollow (was 72), 159
  for floor 3's first part (was 126), 240 for its last (was 180).
- **Legendary.TowerFloor** is a floor (3), not a level: unchanged. **The website's tower page** shows each part's
  level and sync level from the curve, and now each floor's levels and its experience bonus as badges.

**Experience** (game 4fae433a, RavenNest ed915a3c). On a part every skill gets the island's top-up now
(`TowerHandler.TrainsAway`, `PlayerProgression.TrainAway`): combat skills and Healing get what they earn by fighting
and healing, and the island's rate (learned per player and skill on their island, a typical rate until 90 seconds are
known) when that is less, so a boss fight, its countdown, recovering, being knocked out and the walk between monsters
pay at least the island. On top, a part pays combat skills and Healing `Tower.ExpBonus`: 5% a floor with no cap, on
everything earned there (kills, heals, the top-up), the climb's part and cleared parts alike; the towns pay the
island's rate. The tower panel shows "+10% exp" in gold beside the players count, the floor map "+10% experience" by
the floor's name, the website's tower page a badge per floor and a line under the rules. The player details window
shows the time to the next level on a part (it said "Not able to train here" for combat skills there).

**Slayer from tower bosses** (the owner, 2026-11-27; `TowerManager.PaySlayer`). None was paid. A won attempt pays
everyone who fought in it, knocked out ones included (not those who sat it out, nothing for a lost attempt), once the
server has the clear: a mini boss the dungeon boss's reward (`DungeonController.AddExperienceReward`: Slayer factor
max(level / 200, 70) x 0.125, and max(5, half that) in the trained skill), a floor boss 2.5 times both, the floor's
`Tower.ExpBonus` on both (a combat skill or Healing gets it from `GetExperience` on the part, not twice). Paid as a
reward that the island's top-up does not count against (`PlayerProgression.AddReward`). Measured with bots on floor 2
(+10%), Slayer against what a dungeon boss pays the same character: 1.100 for the mini bosses (six, twenty and five
bots), 2.750 for Glacius; the trained skill's bonus 1.100 and 2.750 (1.32 and 2.97 for those still targeting the boss,
who also get its kill's share); 0 for a bot in the town and for everyone after a lost attempt. The server keeps no
experience for a clear, so it has nothing to match.

### Measured (2026-11-27, editor, local server against the dev database, bots, chat muted and the bot stopped)

Minutes for a part's kill objectives, from the kills a minute bots made over 150 seconds on each part (the part's kill
amounts over the kills a minute times what a kill is worth for the heads on it; players go for what the part needs,
so the objectives fill together). Bots at or above the part's sync level; heads below the bots' number are bots
whose gear makes them a share of a head. In brackets: against five heads' time, the aim in square brackets.

| Part (aim for five) | 2 bots | 5 bots | 10 bots | 30 bots |
|---|---|---|---|---|
| Floor 1, part 6, The Pasture (25.6 min) | 2 heads: 30.7 min (1.31) [1.4] | 4.1 heads: 25.3 min, about 23.5 for five | | 25.4 heads: 19.2 min (0.82) [0.66] |
| Floor 2, part 2, Hare Hollow (21.1 min) | 2 heads: 30.0 min (1.41) [1.4] | 4.4 heads: 23.4 min, about 21.3 for five | 7.9 heads: 18.3 min (0.86) [0.88] | 23.7 heads: 13.9 min (0.65) [0.67] |
| Floor 3, part 1, The Cloud Docks (20 min) | 1.75 heads: 28.9 min (1.40) [1.47] | 3.75 heads: 22.9 min, about 20.6 for five | 7.8 heads: 16.3 min (0.79) [0.89] | |

Before (2026-11-26): six bots needed about 90 minutes for Hare Hollow.

| On the part, every run | Before | Now |
|---|---|---|
| Monsters standing | 2.3 on average (a room of the frost vault) | 53 to 56 of 60 (at least 49) |
| Ambling at a time | none | 12 to 16 (none on a part the camera is not near) |
| Monsters on a player | only when hit | 1.2 (floor 1, 2 bots) to 12 (30 bots) |
| Players fighting / walking / knocked out | 57% / 43% / none below 86% health | 60 to 85% / 11 to 40% / 1 to 15% of the time |

Knocked out: 9 to 15% of the time with 2 to 5 bots on floor 2 (three monsters on one player there), 2 to 6% on floor
1 (one at a time), 4% with thirty. The crypt (floor 1, part 4) with five bots: 60 monsters in packs over its five rooms
(8, 20, 12 and 20, the guardian's empty), the group fought through the first three, which stayed cleared, and the
hold refilled (14 of 20 standing, respawn 12 s).

Experience an hour on floor 2, part 2 (+10%), every combat skill summed with Health, three bots training Attack,
against the same bots on an island that suits them (a 23 minute sample there; the samples in the tower are 1 to 8
minutes):

| | Attack 172 (Ironhill) | Attack 405 (Heim) | Attack 999 (Eldara) |
|---|---|---|---|
| Island | 3,386 | 28,723 | 226,653 |
| The part's monsters | 10,699 | 70,795 | 502,603 |
| Boss countdown | 6,877 | 78,359 | 604,725 |
| Boss fight (Snowball, won with the three) | 4,232 | 47,957 | 383,585 |
| Recovering | | 50,831 | |
| Knocked out | 5,226 | 48,388 | 421,878 |

Every situation pays at least the island's rate and the bonus (the low bot's boss fight 1.25 times its island); a
part's monsters pay two to three times the island, as before the change (the bot pass measured 114,000 against
59,000).

### Untested, and calls for the owner

- **The pace on floor 1 with a crowd**: thirty bots needed 0.82 of five's time on the Pasture (aim 0.66). Floor 1's
  monsters fall in a blow or two and notice players only at 5.5 m one at a time, so a crowd walks 40% of the time.
  Floors 2 and 3 are on the aim. A closer reach on floor 1 for crowds, or more monsters for them, would fix it; the
  owner's call (floor 1 was to stay relaxed).
- **Knocked out more often**: up to three monsters on one player on floor 2 knock out 9 to 15% of a small group's
  time (nobody went below 86% health before). Fine for the drama, or a cap of two?
- **Guardians' edges on floors 2 and 3** changed with the level curve to keep each floor's odds (the list above).
  The alternative is items between levels 200 and 280 and in the Dreadforged range, so sync has real gear there.
- **The tower pays far more than an island** on a part's monsters (two to three times), before the 5% a floor; the
  rule "never less than the island" changes the boss fight, countdown and recovery, not that.
- **Packs in potato mode** stay (the pace counts on them): a part has 60 monsters where it had 24; one part is open
  in potato mode.
- **Untested**: real players with a real chat; a Healing player's top-up and bonus (the rule is the same code path);
  floor 3's guardians at the new levels in the game (the simulator only); the website's badges at full width (the
  page was read as text); 30 players on floor 3; a part of the frost vault with packs (the crypt was played).
