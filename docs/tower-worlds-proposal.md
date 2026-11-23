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
