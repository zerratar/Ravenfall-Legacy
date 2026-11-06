# The tower: a 100 floor climb for the whole stream

Part of [combat-v2-design.md](combat-v2-design.md). A new game event, in the spirit of Sword Art Online's
floating castle: a tower of **100 floors** (for now) that a stream climbs together. Every floor has a few
**stream quests** and a **floor boss**. The boss can only be challenged once the stream has finished the
floor's quests; after that the stream can **retry the boss as often as it likes** until it wins. Floors come
out a few at a time: the first ones with the update that brings the new combat, more with every update after.

Decided with Zerratar (2026-11-06):
- Every floor: a couple of stream quests, then a floor boss.
- The boss room opens only after the floor's quests are done; then unlimited retries.
- Not all 100 floors at once; a few with the first update, more in every update.
- The tower uses the same new combat as everywhere else; it is not a separate combat mode.

---

## 1. Name

Candidates: *The Raven Spire* (recommended), *The Hundred*, *Skyreach*, *The Ascent*. A wink at the
inspiration somewhere inside (a floor name, a boss's line) fits Ravenfall's habit of small easter eggs.

## 2. How a floor works

Progress belongs to the **stream** (the village), like the stories: everyone playing in the stream climbs
together, and the floor a stream has reached stays reached.

A floor goes through four states:
1. **Locked:** the floor below is not cleared yet.
2. **Quests:** two or three stream quests are open (see below). The quest tracker shows them.
3. **Boss open:** the quests are done. The streamer, or chat by vote, starts a boss attempt.
4. **Cleared:** the boss is beaten. The next floor opens; this floor stays open for training and loot.

**Floor quests** reuse the stream quest system that exists today, with the floor's own content:
- defeat a number of the floor's monsters (players train on the floor as on an island);
- gather or deliver materials for something on the floor (a bridge, a door, a ward), like the village story;
- now and then a special one: survive a wave, find hidden things on the floor, protect a helper.

## 3. The boss fight

- **Starting:** the streamer starts an attempt (or chat votes for one). Players join with `!tower` during a
  short countdown, the way raids and dungeons gather people.
- **The fight:** the boss uses a full kit from the new combat: telegraphed attacks, area attacks, summons,
  phases and enrage (see the overview, section 6). Its element and weaknesses come from the floor's theme,
  so the right gems and skills matter. Roles matter here more than anywhere else: a tank to hold it,
  healers, damage.
- **Fixed difficulty:** unlike raids, which scale to whoever joins, a floor boss has fixed stats for its floor,
  so climbing is a real progression. Each floor shows a suggested group size and combat level.
- **Losing:** everyone who fell, or a time limit (for example 10 minutes), ends the attempt. The boss resets
  and can be tried again after a short pause (2 minutes, to regroup). No limit on attempts.
- **Winning:** the floor is cleared for the stream, and everyone who took part gets the floor's rewards.
- A tower attempt works like a dungeon for the players in it: they leave their task and come back after.

## 4. Floors and themes

Floors come in **bands of ten**, each with its own look, monsters and element, so each band asks for
different gems and skills. For example:

| Floors | Theme | Element | Weak to |
|---|---|---|---|
| 1-10 | Overgrown halls | poison and beasts | fire |
| 11-20 | Frozen vault | ice | fire, lightning |
| 21-30 | The forge | fire | ice |
| 31-40 | Storm galleries | lightning | poison |
| 41-50 | The crypt | undead (immune to poison) | fire, light |
| ... | ... | ... | ... |

Building it: one tower area that is **dressed per band** (Synty packs, as the islands), with each floor using
its band's layout, its own enemies, its own boss and its own stats. Ten layouts carry all 100 floors, and a
new band of ten is a new layout plus ten bosses and their quests.

## 5. Rewards

- **Every clear:** a chest per player who took part: coins, raw and enchanted gems, battle pet eggs of the
  band's species, tower gear (gear with sockets, made for the band's element).
- **First clear:** the first stream in the world to clear a floor is named in chat and on the website; floors
  10, 25, 50, 75 and 100 give titles (worn with `!title`).
- **The village:** a trophy decor piece per band cleared (the village decor system has room for it).
- **Floor quests** pay as stream quests do today, with the village's share.

## 6. The website

- A **tower page**: the floors that exist, each with its theme, boss and suggested group, the first stream to
  clear it and when.
- A **leaderboard** of the highest floor per stream.
- The stream's own climb on its town page.

## 7. Releases

- **First update:** floors 1 to 10 (one band), with the new combat.
- **Each update after:** the next band of ten, or more when ready.
- Floor difficulty is set with the combat simulator (see the overview, section 8), so each band fits the
  levels and gear players have by then.

## Open questions

- Is the tower always open while a stream is live, or does the streamer open it, as with Ravebridge?
- Who can start a boss attempt: only the streamer, or chat by vote as well?
- Should a stream's climb ever reset (seasons, with rewards for how far you got), or is progress forever?
- Can players who fall during a boss attempt be revived (a Restoration skill), or do they sit out the attempt?
