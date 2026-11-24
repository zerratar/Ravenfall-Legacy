# The Tower as worlds, and gates beside it (proposal, 2026-11-23)

A proposal for v0.9.8 and after, from a talk with the owner about how the tower should feel. Nothing here is built
yet. It builds on docs/tower-design.md (the tower as it is) and changes how the climb is shaped, how dungeons and
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
