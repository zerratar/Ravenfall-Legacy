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

**Each band is its own biome** (Zerratar 2026-11-07: like Sword Art Online's and Solo Leveling's floors, a new
floor can be a whole new place): a forest, a frozen vault, a forge, storm cliffs, a crypt. Building it: per band,
one biome kit (Synty packs, as the islands), three or four floor layouts reused with changes (paths, props,
weather, time of day) and a boss arena; each floor has its own enemies, boss and stats. A new band is a biome kit,
its layouts, a town (below) and ten bosses with their quests.

### Towns between the bands

**Every band opens with a town**: a calm floor with no fighting, the first one at the tower's foot, then one after
every tenth boss (so the town of floors 11 to 20 is reached by beating floor 10). In Sword Art Online every floor
had its town; one per band is a tenth of the work and still gives the climb its resting places.

- **NPCs** with dialogue (the quest system's NPC dialogue), in the band's style: a smith in the forge band's town,
  a frozen-bearded fisher in the ice band's.
- **Town quests for each player**, not only stream quests: errands, a hunt on the band's floors, a delivery, a
  small story per town. They unlock **special items** that exist nowhere else: the band's cosmetics, a recipe,
  a title, a decor piece for the village, and an **egg of a special pet** (one of the tiered model species, which
  evolve like every pet).
- **A vendor** with the band's goods (treats for taming the band's creatures, a few useful consumables).
- **Places to train**: a gathering spot or two that fit the biome (a frozen pond to fish, an ore seam in the
  forge), so a player can choose to stay in the town. Resting works as in the Onsen.
- **Tameable creatures** on the band's floors (see the battle pets design): every band brings new pets.

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

## 8. Where the tower lives: the main world never stops

The worry (Zerratar 2026-11-07): new places in their own Unity scenes could leave the players who stay behind not
loaded, not gaining experience, unable to sail, and coming back to the main world would mean a big load and a
fragile restore of everything. But cramming every new place into MainWorld makes it heavier with every update.

**The answer is to never unload the main world.** Tower floors are their own Unity scenes, **loaded additively**
next to MainWorld (far away in world space, out of sight of everything else), the way a dungeon works today,
except that a floor is only in memory while it is used.

- **Players who did not join notice nothing.** Their island, task, experience, sailing and commands keep working,
  because nothing was taken down. There is no "give experience over time" stand-in and no state to rebuild when
  the tower closes: the main world was running the whole time.
- **Joining is a choice** (`!tower`, during a countdown, like raids and dungeons; auto join can work the same way it
  does for them). Nobody who only wants to train is pulled in. The players who join go through what the dungeon
  already does (`DungeonHandler`): their task, island and position are stored, they are moved to the floor, and on
  leaving they get all three back. Commands that make no sense inside (sailing, the ferry) answer as they do in a
  dungeon.
- **Loading** happens during the join countdown (`SceneManager.LoadSceneAsync` with `LoadSceneMode.Additive`), so
  by the time anyone is moved the floor is there. A floor is one arena or one town, not a whole island, so it loads
  in a moment. If the camera is following a player who walks in early, only the camera shows a short "the tower
  opens" card; the game itself never waits. At most one fighting floor and one town are loaded at once, and a floor
  is unloaded when the last player leaves.
- **The look follows the camera.** Sky, fog, sun, ambient light (`SceneAmbient`), weather, music, and the world-wide
  systems (ocean, clouds, rain) switch to the floor's profile when the camera watches someone in the tower, and back
  when it watches someone in the world. The game has no baked lighting, so there are no lightmaps to merge.
- **Navigation:** each floor scene brings its own navmesh, added when it loads and removed when it unloads (Mangrove
  already has a navmesh area of its own).
- **A game restart during an attempt** puts the tower players back where they came from (the restore data keeps
  their previous island) instead of loading the floor again. The attempt is lost, never the progress: which floors
  a stream has cleared is the server's.

**The one risky code change, done first and on its own:** the game asks `dungeonHandler.InDungeon` in 61 places
across 37 files (sailing, travel, islands, auto join, the camera...). A tower handler would need every one of them.
So before the tower: one question for all of it, "is this player away from the world, and in what" (dungeon, raid,
tower, later others), with those 61 checks moved to it. Nothing changes for players; the tower then plugs into it.

### And the main world getting crowded

The same tool answers the second worry. Rendering is already culled (only what the camera sees is drawn), so a
bigger MainWorld costs mostly memory, load time, scripts and animations updating everywhere, shadow and culling
overhead, and a slower editor. Once the tower proves additive scenes work (loading, navmesh, the look following the
camera, restore), the places that are only used now and then can move out the same way:
- **Ravebridge** (the streamer opens it) and **the dungeons** (used only during a dungeon) are the first candidates:
  loaded when the event starts, unloaded after.
- Later, if the profiler says it is worth it: an island split into an always loaded part that the game needs to
  play (navmesh, colliders, spawn points, resource spots; small) and its looks (buildings, decor, foliage; big),
  with the looks loaded only near the camera. A large change; only after measuring what actually costs frames.

## 9. Proving it first

Before any floor is built, a test (a day or two) answers whether this works in Ravenfall:
1. A test floor scene (`Assets/Scenes/Tower/TowerTest.unity`): one small Synty biome, its navmesh, spawn points,
   a few enemies, a boss spot, its own light and fog profile.
2. In play mode with 100+ bots: load it additively during a countdown and measure the hitch and the memory; move
   20 bots in through a copy of the dungeon's handling; check they walk and fight there while the others keep
   training on the islands with their experience rate unchanged.
3. Switch the camera between a tower player and an island player: the look switches with it, the ocean and the
   clouds do not show in the tower.
4. Unload the floor: memory goes back down, the 20 bots are back on their islands doing what they did.
5. Restart the game in the middle: the tower players come back where they came from.

**Passes if** the hitch is short enough to hide in a countdown, nothing in the main world changes for the players
who stayed, and memory returns after unloading. **If not**, the fallback is the dungeon way: floors as disabled
areas inside MainWorld, which works today but grows the scene with every band.

### Result (2026-11-07): it passes

Done in play mode in the editor against the local server, with 110 bots (then 30 for a second run).

- **The away refactor first** (game 50f7057d): `player.Away` / `IsAway` and `IAwayHandler` replace the dungeon checks
  that meant "away from the islands"; what a player leaves behind and the way back moved from `DungeonHandler` into
  `WorldReturn`. Six bots went into a dungeon and came back with their tasks and islands.
- **Loading** a test floor (`Assets/Scenes/Tower/TowerTest.unity`, at (0, 0, -6000), its own navmesh, eight
  Metalons, Synty props): 0.25 to 0.32 s over two frames; the worst frame 188 ms (normal frames about 50 ms in the
  editor with 110 bots), so one stall of about 140 ms, easy to hide in a join countdown. Memory +6 to +48 MB (the
  larger figure on the first load of a session, which also brings the props' assets in).
- **20 of 20 bots joined** and fought the floor's enemies. **The 90 who stayed:** none away, none without their
  island, and their experience per minute with the tower running was 7,785 against 7,726 before it loaded. (One
  window read 1,612; it was taken while screenshots slowed the editor, and the clean repeat matched.)
- **The look:** with the camera on a tower player, the floor's light and fog, and the sea, clouds, cloud shadows and
  weather off; back on an island player, everything as before, the fog setting included. `GameManager` sets those
  systems from potato mode every frame, so the tower switches them off every frame while the camera is over the
  floor.
- **Unloading:** 0.54 to 0.57 s, worst frame 205 to 245 ms; every player back on the island they left, doing the
  same task; the floor's enemies gone from the game.
- **A restart:** the saved state of the players in the tower names the island and the spot they came from, so a
  restart puts them back there. Bots are never saved, so a real restart needs a signed-in player to check.

**Not built yet:** `!tower` and the server side (floors cleared per stream, the countdown, rewards), a real floor
(this one is a flat test plane), towns, floor bosses, dying and coming back on a floor, and the fog and light per
biome. Enemies on the floor came back after dying like island enemies do, which suits floors open for training.

## Open questions

- Is the tower always open while a stream is live, or does the streamer open it, as with Ravebridge?
- Who can start a boss attempt: only the streamer, or chat by vote as well?
- Should a stream's climb ever reset (seasons, with rewards for how far you got), or is progress forever?
- Can players who fall during a boss attempt be revived (a Restoration skill), or do they sit out the attempt?
- Can players train on cleared floors and in towns while no attempt is running? (This doc assumes yes, with at most
  one fighting floor and one town loaded at a time.)
