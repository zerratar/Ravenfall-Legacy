# The tower: a 100 floor climb for the whole stream

Part of [combat-v2-design.md](combat-v2-design.md). A new game event, in the spirit of Sword Art Online's
floating castle: a tower of **100 floors** (for now) that a stream climbs together. Every floor has a few
**stream quests** and a **floor boss**. The boss can only be challenged once the stream has finished the
floor's quests; after that the stream can **retry the boss as often as it likes** until it wins. Floors come
out a few at a time: the first ones with the update that brings the new combat, more with every update after.

> **The words changed on 2026-11-24 (v0.9.7, before the tower's first release).** Players now see **floors**, each a
> world with its own town, and the **parts** of a floor. What this document calls a *band* is a floor (the Overgrown
> Halls are floor 1, the Frozen Vault floor 2, the Storm Spire floor 3), what it calls a *floor* is a part ("floor 14"
> is floor 2, part 4), a floor's quests are a part's *objectives*, and its boss is a *mini boss*, or the *floor boss* on
> a floor's last part (the old floors 10, 20 and 30). The server and the code still count a part by its old floor
> number, now called its *step*. The sections below keep the words they were written with, as the record of how the
> tower was built; where a command, a number or a text changed, a line marked *Since 2026-11-24* says so. The shape
> as built (words, commands, mini bosses and floor bosses, the kinds of objectives, scaling by pressure) is
> [tower-worlds-proposal.md](tower-worlds-proposal.md), section 12.

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
  *Since 2026-11-24:* the fixed numbers are what the suggested group of five meets. A bigger or stronger group meets
  more (health, helpers, a little harder blows), a smaller one less: scaling by pressure, tower-worlds-proposal.md
  section 12.
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

*Since 2026-11-24:* every part's clear pays the chest, coins and gear as before (two chest items from a floor boss and
from every fifth part). The world first that is announced is the floor boss's; the first clear of any other part is
kept for the website and doubles the coins quietly. The last blow on a floor boss gives a title ("the Finisher").

## 6. The website

- A **tower page**: the floors that exist, each with its theme, boss and suggested group, the first stream to
  clear it and when.
- A **leaderboard** of the highest floor per stream.
- The stream's own climb on its town page.

*Since 2026-11-24:* /tower shows floors with their towns and a card per part with its objectives; the leaderboard
ranks by floor, then part; the streamer's /town page shows the stream's part and its objectives' progress.

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

## 10. The first playable version (2026-11-07)

Built on the plausibility test, against the local server only (nothing deployed):

- **Floors 1 to 10** (the Overgrown Halls band: poison, weak to fire) in RavenNest.Combat (`Tower.cs`), shared by the
  game, the server and the simulator: each floor's name, a line of text, its level (10 per floor), two quests
  (defeat a number of two of its monsters), its monsters (looks from today's enemies: bunnies, goblins, Metalons,
  skeletons, cows) and its boss. Bosses: Nibbles the Vicious (a giant bunny), Warchief Grubnose, the Brood Mother,
  the Rootbound King, Old Thornback, the Moo-tant, King Grubnose the Second, Redshell the Blighted, the Gardener and
  Viridia, the Verdant Wyrm on floor 10.
- **Boss numbers** from the simulator's `tower` scenario (fallen players stay down, 10 minutes, a slam every few
  swings): five players at the floor's level win in 2 to 4 minutes, easily on floors 1 to 4 and narrowly or not at
  all on floors 8 and 10 (which want more players or higher levels); players at 1.5 times the level win in 1 to 2
  minutes. Healers, skill trees and gems are not in the simulator, so the game is easier than these numbers.
- **The server** (`TowerManager`, `api/tower`): one `StreamTower` row per streamer (its floor, the quest progress,
  attempts) and a `TowerClear` row per cleared floor (when, how many players, after how many attempts; the earliest
  is the world's first). A clear pays every player who took part 250 coins per floor number (twice that for a world
  first) and a chest from the dungeon table (tier by floor, two items on every fifth floor), and the village 1,000
  coins per floor number. Both tables are made at startup.
- **The game:** `!tower` enters the stream's floor (the first player's `!tower` loads it), `!tower leave`,
  `!tower info`, `!tower boss`. Kills on the floor count for its quests (reported every 8 seconds); when they are
  done chat is told the boss is open. The streamer or a moderator starts an attempt at once, or three players in the
  tower vote for one (everyone in it, when fewer are in). A 60 second countdown lets others join with `!tower`; the
  boss then comes out on the plaza and everyone in the attempt fights it from their own place around it. Every few
  swings it slams: a red ring on the ground, then everyone inside is hit. Won: the server records the clear, chat is
  told (and a world first), the floor fills itself again as the next floor. Lost (everyone fell, or 10 minutes): 2
  minutes to regroup. A floor nobody has been in for 90 seconds unloads.
- **The first layout** (`Assets/Scenes/Tower/Tower_Overgrown.unity`, built by a script): a sunken courtyard of an
  old hall gone wild, a terrain in a bowl of wooded hills (so it never ends at an edge), a colonnade of broken
  arches, tall pillars around a cobbled plaza where the boss waits, 24 monster spots and its own navmesh. Uses the
  local Polygon Nature Biomes and Dark Fortress packs (untracked, like the other Synty packs). All ten floors share it
  for now; the camera follows players from above, so the clearing has no trees.
- **Choices made for the open questions below, easy to change:** the tower is open whenever the stream is live (no
  streamer opening); the streamer and moderators start attempts, chat can vote; progress is forever (no seasons
  yet); a fallen player is out of the attempt (and back on their island, as in a dungeon), also when training.

Checked in play mode with 30 to 40 bots: the floor loads in 0.2 to 0.4 s (one stall of 0.2 to 0.4 s in the editor,
+20 MB, or +290 MB the first time a session loads the packs), quests count and open the boss, an attempt starts,
the boss falls, the server records the clear and the next floor fills; with a toughened boss the slam ring shows and
the players stand around it; knocking everyone out ends the attempt, and they are back on their islands with their
tasks. Progress was kept between play sessions (the server's).

Not checked or not built: the chest and coins for a signed-in player (bots get none), chat lines in a real chat,
towns, a second layout, bosses with more than the slam, the website's tower page and leaderboard, and training on
floors already cleared (the tower always goes to the stream's current floor).

## 11. Mossgate, three looks, boss moves and no spoilers (2026-11-08)

Asked by Zerratar on 2026-11-07: the town with NPCs and quests next; the website must not reveal floors no stream has
reached; floors should vary their look, and some should be real dungeons (not like the dungeon events); and are the
floors challenging enough?

- **No spoilers on the website.** /tower shows a floor in full only once some stream has reached it (the floor after
  the highest cleared anywhere, `Tower.Revealed`); the floors above are one dashed card ("Floors 5 to 10 are still
  hidden") and bands above are "unseen". The leaderboard and first clears stay.
- **Three looks in the first band** (`TowerFloorDefinition.Layout`): the overgrown courtyard (floors 1, 2, 5, 6), the
  drowned marsh (3, 8: mud, black pools with steep banks, reeds, dead trees, mist, fireflies and frogs) and the crypt
  under the halls (4, 7, 9, 10: a real dungeon of rooms, low broken walls and no ceiling for the camera, a pillared hall,
  two side crypts with coffins, a throne hall for the boss, braziers with warm light against a cold green dark, roots of
  grass through the floor, giant mushrooms, a dragon's skull as a hint of floor 10). Built by scripts like the first
  (PolygonDungeon, Dungeon Realms, Nature Biomes, Dark Fortress, Fantasy Village; all local packs). When the next floor
  is another layout, the players climb: 12 seconds after the clear the old scene goes and the new one loads with them.
- **Roads with the village road tool** (Zerratar: "those roads look the best"): `SceneRoads` lays roads into a scene
  with `RoadMesh` and the road shader's materials, and a new type, mossy flagstones (Resources/Roads/Road_4, made from
  the forest's footpath and moss). The courtyard and the marsh have a flagstone road from the arrival to the plaza;
  Mossgate has cobbles round its square, flagstones to the gate and dirt lanes. Road 4 is not offered in the village
  yet (it needs a server road type and a price).
- **Mossgate** (`Tower_Mossgate.unity`, at (600, 0, -6000), loaded beside the floor): a mossy hamlet with a cobbled
  square and well, the Sleepy Stump inn, houses, Bramble's herb hut and garden, and the tower's walled gate at the north
  end with the tower rising behind. `!tower town` goes there (from the world or from a floor, keeping the way back),
  `!tower` goes up to the floor, `!tower talk elm` talks. Players stroll between spots. It unloads after 90 empty seconds.
- **Its people** (`TowerTownDefinition.People`): Warden Elm at the gate, Bramble the herbalist, Pip (who lost his bunny)
  and Old Mossbeard the innkeeper (lines and a rumour about the stream's current floor, never past it). The first three
  each give a personal quest chain (the server's QuestChains, `Town = "mossgate"`, begun only in the town):
  - *The warden's watch*: monsters on any floor, Moss Goblins, iron ore for the gate, take part in a floor clear, report
    back; the Wardens Thornband (a ring) and the title "the Gatewarden".
  - *Bramble's remedies*: mushrooms, Vine Metalons, which mushroom is safe, brew potions, come back; the Bramble Charm (an
    amulet) and "the Herbwise".
  - *Where is Nibbles?*: Feral Bunnies, an old man's question about swallows, cabbage, be there when a boss falls, tell
    Pip; the Holy Hand Grenade (a keepsake: count to three) and "the Bunny Finder". Nibbles is the floor 1 boss.
  New objectives: `tower_kill` (of one kind when the step names it; the game reports kills per character to
  api/tower/kills), `tower_clear` (counted by the server when a clear is saved) and `talk` (!tower talk, in the town).
- **Boss moves** (`TowerBoss`): help at shares of its health (the floor's first monster, 3 by default; the Gardener at
  two thirds and one third, Viridia at three quarters, half and a quarter), enrage after 6 minutes (swings at 0.6 of the
  time, 1.5 times the power; Viridia after 7), and a spit at the player farthest from it (not the one holding it): a
  green ring under them, then a hit for everyone inside (the Brood Mother, the Moo-tant, Redshell, the Gardener,
  Viridia). Players turn on the helpers first. In the simulator (no healers, skills or gems), five players at a floor's
  level now win floors 1 and 2 almost always, floors 3 to 9 a quarter to half of the time, and floor 10 not at all (it
  wants eight); eight players win every floor in about 2 minutes.

Checked in play mode with 30 bots on the local server: Mossgate loads and its people stand with their marks, bots
stroll; the marsh loads for floor 3 (0.26 s); bots go from the town up to the floor; a boss attempt calls its helpers
below half, enrages (2.5 s to 1.5 s, power 21 to 32), the clear is saved and the players climb into the crypt for floor
4 (0.24 s). Not checked: `!tower talk` and the chains with a real signed-in character (bots are not session
characters), the spit landing, the new items' icons (none yet), Mossgate's gate braziers' fire in play.

### Is it challenging enough?

For a group at the floor's level, yes, and floors 8 and 10 are hard. For a real stream, no: twenty players far above
the floor's level flatten any fixed boss (in the test, bots of combat level 100 to 1,200 killed a level 30 boss's
helpers within a second). Two ways to keep it a climb, for Zerratar to choose:
- **Level sync**: in the tower a player fights at most at the floor's level (plus a margin); gear, skills and gems still
  count. Fixed bosses then stay fixed and mean the same for every stream.
- **Group size**: a boss attempt takes at most a set number of players (for example 10), the rest train on the floor.

## 12. Level sync (decided 2026-11-08)

Zerratar chose level sync over a group cap. On a floor (not in a town), every player fights as if at most at the floor's
sync level, a tenth above the floor's level (`Tower.SyncLevel`: 11 on floor 1, 44 on floor 4, 110 on floor 10):
- **Skill levels** (attack, strength, defence, ranged, magic, healing) and the combat level count only up to it.
- **The health pool** shrinks to the sync level's, keeping what gear adds. The real health bar stays as it is: blows and
  heals taken on a floor are multiplied by how much bigger it is (`TowerHandler.SyncHealthScale`), so it loses the same
  share as the synced pool would.
- **Gear** counts up to the best gear wearable at the sync level (`Tower.CapGear`, a table from the item list):
  power, accuracy and armour above it give nothing more on the floor; worse gear counts as it is. Skill trees, gems,
  crits, elements, attack speed and enchantments count in full.
Levels alone were not enough: in the simulator, players at ten times a floor's level with levels synced but their own
gear still won every floor with three players. With gear capped too, they do about as well as players at the floor's
level (five players win most middle floors; floor 10 wants eight).

In play mode on floor 4: a combat level 1,171 bot fights at level 44 (health 8,720 down to 440, armour 2,149 to 179,
blows on it times 19.8), and the bots kept fighting the floor without falling. `!tower` and `!tower info` say the sync
level; the website explains it and shows it on every floor.

## 13. Titles, icons and pictures (2026-11-08)

- **Climber** (an achievement): the highest floor whose boss fell with the character there; the titles "the Climber"
  (floor 1), "the Spire Walker" (5) and "the Bane of Viridia" (10). More tiers come with the next bands (25, 50, 75, 100).
  *Since 2026-11-24:* the same steps in the new words: the first part, the middle part of each floor and each floor's
  boss (floor 1 part 1, 1-5, floor 1 cleared, 2-5, floor 2 cleared, 3-5, floor 3 cleared), worked out from the floors
  that exist; the website shows the climber's progress as "Floor 2, part 4". New: **Last Blow**, for last blows on
  floor bosses: "the Finisher" (1), "the Last Word" (3), "the Curtain Call" (10).
- **Nibbled** (hidden): knocked out during a boss attempt on floor 1, by a bunny; the title "the Bunny Snack". The game
  sends knock-outs during an attempt with the kills report (`TowerKillReport.Falls`); the server counts only the
  stream's own floor.
- Icons for the Wardens Thornband, the Bramble Charm and the Holy Hand Grenade (Image Generator), pictures for the two
  achievements and the three Mossgate chains with their banners (tools/quests/quest-ui-pictures.cs).
- The website's quest page says where a town chain is begun ("while in Mossgate") and shows a chain's item reward, and
  the Mossgate chains' words were added to its texts (tools/website/export-quest-texts.py).

## 14. Training and resting in the towns (2026-11-08)

Zerratar: in the calm floor villages players should keep training every skill and gain exp as if they were on their own
island, so waiting for the next floor costs nothing (the same complaint came from Ravebridge); and add a resting place.
- **Training away**: each player's real training rate is learned while they train on an island (the sum of the exp
  factors per second, per skill, halving every ten minutes so it follows their current spot; `PlayerProgression`). In a
  tower town, and on Ravebridge, every five seconds they get what the island would have given, less what they earned in
  that skill there anyway (Ravebridge's fights), never less than nothing. Level, boosts, rested and the village's bonus
  apply as usual. Until 90 seconds on an island are known, a typical island rate per skill is used. The server keeps
  giving resources as before (it sees the island they came from).
- **Resting place**: Mossgate has a campfire beside the inn with six cushions (meditating) and five rugs (sleeping).
  `!rest` or `!onsen` in the tower (from a floor: down to the town first), or `!tower rest` from anywhere, takes a
  free spot; resting there is the Onsen's rest (the server counts it by the same flag, so no training meanwhile). A new
  task, `!onsen leave` or `!tower` gets them up; leaving ends it.
- **Ravebridge uprisings** can be turned off (Settings > Game > World): Ravebridge then only opens when the streamer
  starts it, for streams left running (AFK streamers found the random uprisings disruptive for their viewers).

## 15. The second band, the Frozen Vault (2026-11-10)

Zerratar (2026-11-10): the tower does not stop at ten floors for v0.9.7; keep adding floors while there is time.

- **Floors 11 to 20** (`Tower.cs`, band 2 "The Frozen Vault", biome ice): monsters hit with ice (30% of each blow),
  resist ice by half and are a quarter weak to fire and a tenth to lightning, so the band asks for fire gems and ice
  resistance. Monsters from today's looks: Frost Raiders (vikings), Snow Hares, Frozen Skeletons, Frost Wraiths (souls,
  magic), Quacklings (Lord Quackers at 0.4 scale: `TowerEnemy.Scale` is new), Raider Thanes (viking chiefs), Frost
  Drakes (white dragons), Frozen Ronin (samurai) and Frostbitten Cows. Bosses: Jarl Rimebeard, Snowball (Nibbles' big
  sister: "You defeated my little brother. Prepare to be nibbled."), the Frozen Warden, the Wailing Matron, Emperor
  Quackers ("QUACK."), Sigrid the Unthawed, Hoarfang, the Thawed Ronin, the Moo-tant, Refrozen ("Mooooo. Brrrr.") and
  Glacius, the Winter Wyrm on floor 20 (adds at three quarters, half and a quarter, enrage after 7 minutes). Simulator
  `tower`: the same shape as band 1, five synced players win the middle floors 5 to 89% of the time, eight always;
  floor 20, like floor 10, wants eight.
- **Two layouts**: the glacier (`Tower_Glacier`, floors 11, 12, 15, 16, 19): a snowed-in valley in a bowl of snow pines
  with mountains behind, a frozen pond with a wrecked boat (the ducks of floor 15 stand on it), a ring of rune stones
  with burning braziers round the boss's frozen flagstones, a gravel path, soft snowfall. The ice vault
  (`Tower_FrostVault`, 13, 14, 17, 18, 20): the crypt's rooms frozen over (made from its builder by
  tools/tower/make-vault.py), cold blue light against warm braziers, glowing floor runes, ice crystals, icicles off the
  walls and pillars, snow drifted in the corners, the dragon skull kept as the hint of floor 20.
- **Rimeholt** (`Tower_Rimeholt`, at (1200, 0, -6000)), the band's town: a snowed-in hamlet of snow-roofed log houses
  round a cobbled square with a statue, Hilda's glowing forge, the Thawing Mead hall, watchtowers by the gate up to the
  vault, Old Tove's frozen fishing pond, and a campfire with pelts and logs for `!tower rest`. Its people and chains
  (the server's QuestChains, `Town = "rimeholt"`):
  - *Hilda's forge* (Hilda Embersmith): monsters on any floor, Frost Raiders, 50 coal, take part in a clear, report
    back; the Embersmith Band (a ring, a little fire damage and ice resistance) and "the Emberkeeper".
  - *The old man and the ice* (Old Tove): catch fish, a riddle (what gets wetter the more it dries?), Snow Hares, raw
    salmon; the Frostbeard Amulet (ice resistance) and "the Ice Fisher".
  - *Where is Sir Quacksalot?* (Ylva): Quacklings, why the duck crossed the road, bread, be there when a boss falls;
    the duck was in her hat all along; the Rubber Duck (a keepsake) and "the Duck Whisperer".
  - Big Gunnhild keeps the mead hall and the gossip.
  Icons for the three items (Image Generator). Climber gets two more titles: "the Frostwalker" (floor 15) and "the Bane
  of Glacius" (20).
- **Towns by name**: `!tower town` goes to the town that is open (players go where the others are), else the stream's
  band's; `!tower town mossgate`, `!tower mossgate` or `!tower rimeholt` goes to that one if the stream has reached it,
  so Mossgate's chains can still be finished. One town is loaded at a time: another opens once the open one is empty.
- *Since 2026-11-24:* the command is `!tower part 7` (a part of the floor the group is on), `!tower floor 2` (that
  floor's latest part), `!tower floor 2 part 7` or short `!tower 2-7`; `!tower part` or `!tower floor` alone goes back
  to the climb. `!tower train 7` still works as `!tower part 7`.
- **Training on cleared floors** (the open question below, answered): `!tower floor 7` (the streamer and moderators)
  takes everyone in the tower down to a floor the stream has reached; its monsters give experience and count for the
  town chains, but the climb's quests and boss stay on the stream's floor. The same layout refills at once, another is
  a climb as after a clear. `!tower floor` alone brings everyone back up. The top floor already worked this way once
  everything is cleared.

Checked in play mode with bots on the local server (the dev stream set to floor 11 for it): floor 11 loads the glacier
with Frost Raiders and Snow Hares (level 110, ice); Rimeholt loads with its four people and six rest spots; asking for
Mossgate while others are in Rimeholt is refused, and once they left Rimeholt went and Mossgate came; floor 13 loads
the vault with Frozen Skeletons and Frost Wraiths; `!tower floor` went 11 to 5 (a climb), 5 to 2 (refilled), 2 back to 11.
Not checked: the band 2 boss fights in the game, Rimeholt's chains with a signed-in character, the snowfall and fire
effects' cost in a full stream.

## 16. Training in town, the way between floors and chat in charge (2026-11-17)

Zerratar (2026-11-17): check the floors' roads and that everything can be reached; players must train everything in the
tower towns, automatically doing what they train outside, while they wait for the streamer (or chat, by vote) to go on or
call it quits; viewers must be able to run the tower when the streamer is away, so it never gets stuck; viewers can leave;
every level should look good.

- **Work places in the towns.** Mossgate and Rimeholt have a place for every skill (`TowerTown.Work`: a child per place,
  named by its kind, with "Stand" children where a player stands, turned toward the work). A player in town walks to the
  place of the skill they train and does it there with the island's animation, tool and sound: woodcutters at big trees,
  miners at rocks with ore, fishers at the pond (Mossgate's new mill pond, holes in Rimeholt's ice), farmers in a fenced
  vegetable patch, cooks at a spit, crafters at anvils and a workbench, alchemists at cauldrons, gatherers at mushrooms or
  berry bushes, attack, strength, defense and health at straw dummies, ranged and magic at targets, healers beside the
  dummies, sailors on a bench by a boat. Taming has a pen (Pip's bunny hutch, Rimeholt's frostbitten cows and a snow hare)
  for when it becomes a trainable task. The experience is still the island's (section 14); the work place is what it looks
  like. A crowd shares the stands, a little to one side of each other. Strolling stays for players with nothing to train.
- **No lost training on the floors either.** On a floor, a player training a skill the floor's fights do not train (a
  woodcutter fighting along) gets the island's experience for it as in town (`TowerHandler.TrainsAway`); combat skills and
  Healing earn by fighting and healing as before.
- **After a clear, down to the town.** 12 seconds after a boss falls, everyone on the floor goes down to the town of the
  next floor's band (the new band's town after a tenth floor), keeps training there, and the floor gets ready (the same
  layout refills for the next floor, another unloads and loads on the way up). `!tower climb` takes everyone in the town up
  to the next floor. `!tower` from the world goes to wherever the group is (the town between floors); `!tower` again from
  the town goes up alone.
- **Chat can make every choice.** The streamer or a moderator decides at once; anyone in the tower can vote instead.
  **One voice is enough** (the user, 2026-11-21; before, a quarter of everyone inside had to vote within two minutes,
  which a big stream of idle players might never reach): the first voice opens the vote for a minute
  (`TowerManager.VoteSeconds`), everyone is told, and when the minute is up it passes. It passes at once when a
  majority of the players who are here have voted: players who typed no command for 15 minutes are away and do not
  count (`ActivePlayers`, `Majority`), so one player among idlers decides at once. When two votes are open, the one
  with fewer voices lapses. `!tower close` alone only passes by majority, never on time (my choice: one voice must not
  send everyone out; `!tower leave` takes one player out). The panel shows an open vote and its seconds. The choices: `!tower boss` (an attempt),
  `!tower climb` (on up from the town), `!tower floor 7` (train on a floor already reached; was the streamer's only),
  `!tower close` (everyone back to what they were doing, not during an attempt for a vote; the climb stays where it is).
  `!tower vote climb` works too. `!tower leave` still takes only the one player out, any time.
- **Roads and reach.** An audit (`tools/tower/tower-audit.cs`) walks the navmesh from the arrival to every monster spot, the
  boss, the people, rest spots, strolls, work stands and road points, and says where a road ends far from any other.
  It found Rimeholt's mead hall standing across the road to the gate (the road stopped at its door and went on behind it),
  four marsh monsters beyond a pool's banks, and Mossgate's inn covering the smithy's door (the village preset buildings'
  pivots are off to one side). Rimeholt was relaid: the hall moved beside the gate road, the watchtowers out, eight lanes
  from the square that branch to every work place, the forge, the pond, the resting place and the hall. Mossgate's
  buildings are now placed by the middle of what they look like, the smithy moved clear of the inn, lanes reach the inn's
  door, the smithy, the yard, the range, the pond, the rocks, the trees, the patch and the hutch, and its resting place is
  built with the town (moss-rest.cs is gone). The builders log any prop standing on a lane. The marsh moves a spot nobody
  can reach to the nearest place they can. Every scene passes the audit with no problems.
- **Looks.** Fence pieces now run along their length (they stood crosswise, as loose posts, at Rimeholt's gate), resting
  on Rimeholt's log benches is sitting on them (it was meditating inside them) and sleeping is on a flat pelt (some pelts
  were hanging ones, floating), the glacier's empty middle got two frozen raiders' camps and ice chunks.

Checked in play mode with 14 bots on the local server: every skill went to its place and worked there in both towns; four
votes of fourteen took them from Mossgate up to floor 4, four more down to floor 2; the streamer's choice back to 4; the way
after a clear took them down to Mossgate and unloaded the crypt; a newcomer's `!tower` went to the town; a close vote sent
everyone back to their islands (one player voting twice counts once); a sailor sat on the bench by Rimeholt's frozen boat.
Not checked: the after-clear walk with a real boss kill (tested without the server call), the vote replies in a real chat.

## 17. Cutscenes (2026-11-17)

Zerratar (2026-11-17): players were pleasantly surprised by Ravebridge rising from the sea, so more cutscenes would be nice:
the first time the tower opens, the floor bosses, the new towns. Decided: the rare moments get a real cutscene, the frequent
ones stay short so the climb is not interrupted.

- **The tower opens (built).** The first time a stream sends someone into the tower, the camera comes in high over the
  trees with the tower standing in the haze, glides over Mossgate's square to the gate and ends low beside the gatehouse,
  looking up the tower, with "The Tower, opens its gate". 20 seconds. Joins wait for it, as for Ravebridge, and go through
  after it (the chat reply is the usual "the tower opens, you will be taken to floor N in a moment"). Mossgate loads for it
  when it is not open; nobody walks in, and it goes again when nobody comes.
- **A new band's town (built).** The first time a stream reaches a band's town (after the band's last boss, floors 10, 20,
  and on), a fly-over of the town while the players walk in, with its name and "Floors 11 to 20: The Frozen Vault". 15
  seconds, and at the end the camera goes to someone in the town. Mossgate has none of its own: the opening is its arrival.
- **The boss intro (built 2026-11-18).** The first time a stream fights a boss, when the countdown ends: 7 seconds, the
  camera in from the players' side, high and wide, to low under the boss's face while it taunts, with its name in red and
  its line (or "Guardian of floor N"). Still the countdown: a !tower meanwhile joins, and the fight starts after it. A
  close in the middle stops it, no fight follows, and it plays again next time.
- **The taunts.** Most enemy controllers have no taunt, so a clip plays over the controller for the moment
  (`TowerBossTaunts`, the asset Resources/Tower/Boss Taunts made by `tools/tower/tower-boss-taunts.cs`; Resources is not in
  git): the humanoids cheer (goblins, skeletons, vikings, the ronin, the wailing soul), Nibbles and Snowball stand up and
  stare, the metalons rear up and cast, the dragons roar fire, Emperor Quackers flaps his wings, and the Moo-tant is not
  impressed and goes on eating. Each is timed to end as the camera settles and the name shows. A model with no taunt winds
  up its attack, slowed.
- *Since 2026-11-24:* a **mini boss** has the short versions (a 5 second intro with "Mini boss of floor 1, part 3" or
  its line, a 3.2 second fall with "Part 3 cleared" in silver); the **floor boss** has the full ones (9 seconds with
  "Boss of floor 1, The Overgrown Halls" and its line, a 6.5 second fall with "Floor 1 cleared" in gold). A town's
  arrival says "Floor 2: The Frozen Vault".
- **The boss falls (built 2026-11-18).** Every clear: 4.5 seconds round the boss as its death plays in slow motion (from
  the start, not after its swing; a dragon's long death at its own pace so it is down before the end), "Floor 7 cleared"
  in gold, "The Rootbound King falls". The body stays down until it goes, and the players cheer. Without the cutscene
  (off, or the camera busy) the death still shows before the body goes.
- **Framing.** Both shots are worked out round the boss, sized to it (room for a taunt that stands it up, never right in
  its face), from its front unless a wall, a pillar or a ruin is in the way: then turned round it to the clearest side.
  The names show at the top, over the sky, not over the boss. The debug menu has "Boss Intro" and "Boss Falls" for the
  loaded floor.
- **How they are made.** Each town has a `Cutscene` child with an "Arrival" row of camera poses (and "Opening" at the
  tower's foot); `TowerCutscene` flies a smooth curve through them at an even pace, easing in and out, and the name shows over
  the last four seconds with a deep boom. The builders place the poses; `tools/tower/tower-cutscene-preview.cs` renders
  frames along a path in edit mode to tune them. A new town only needs its row of poses and a title colour.
- **What they share with Ravebridge's.** `CutsceneScreen`: the screen steps aside, a Skip button sits in the corner (Esc and
  Space too), the name has a soft dark band behind it so it reads over snow or a bright sky. One **Cutscenes** setting
  (camera settings) turns them all off, Ravebridge's rising included; the settings' "Introductions" plays them again. Each
  plays once per streamer on this PC, and only when the camera is free (not in a raid, a dungeon, a war or a character played
  by hand); a skipped chance is tried again next session. The debug menu has "Tower Opens" and "Town Arrival".
- **The horizon in wide shots.** From high up, a town's ground ends a hundred metres out and the sky's dark lower half
  showed past it like a blue sea round a floating plateau. While a place's look is on, a wide sheet in its fog colour lies
  under it and a low ring of the same colour stands round the camera up to the horizon, so the haze reaches the horizon.
- **Rimeholt's snow** was a checkerboard: the glacier's builder made its terrain layers anew on every run (new ids), and
  Rimeholt uses them too. The builder now updates them in place, and Rimeholt was rebuilt.

## 18. The third band, the Storm Spire (2026-11-19)

Zerratar (2026-11-19) picked the sky over fire: "the lighting, sky one. sounds interesting". Floors 21 to 30, lightning,
weak to poison (`Resistance { Lightning 0.5, Poison -0.25, Ice -0.1 }`), the town Galewharf.

- **The monsters** (existing looks, new names): Sky Pirates, Static Hares, Storm Ninjas, Storm Wisps, Squall Ducklings,
  Storm Corsairs, Thunder Gladiators, Gale Sentinels, Storm Drakes and Charged Cows.
- **The bosses:** Captain Gale Blackwind (21), Zap, Nibbles' Shocking Cousin (22), Master Kaze (23), Mother Squall (24),
  Captain Quackbeard (25), Champion Voltus (26), Skyfang (27), High Warden Aerin (28), The Moo-tant, Supercharged (29) and
  Fulmina, the Thunder Wyrm (30). The family traditions hold: a bunny relative, a Quackers and a Moo-tant in every band.
  Their taunts: the captain, the ninja, the champion and the warden cheer, Zap stands up and stares, Skyfang beats his
  wings, Fulmina roars, the cow keeps eating.
- **Two layouts.** `Tower_SkyDocks` (21, 22, 23, 25, 27, 29): a grassy rock above a sea of clouds, its cliffs dropping
  into the haze, a pier out over the void with a sky pirates' ship moored at it and more ships drifting, floating islets
  and ruined bridges ending in mid-air, a stone ring with four lightning rods, a pirates' camp. `Tower_StormSpire` (24,
  26, 28, 30): the spire's paved top, broken colonnades and ruined towers, six rods round a raised dais, chunks of the
  spire drifting off into the clouds, a darker sky and more lightning.
- **The storm.** `TowerLightning` strikes a rod or the far clouds now and then (a jagged bolt, a flash over the place,
  thunder late and soft for far strikes); `TowerSky` hides the world's sunny sky behind a dome from dark slate overhead
  to the fog colour at the horizon while the camera is at the place; `TowerPlace.HazeDepth` puts the haze floor (the
  cloud sea) 48 m under the rim. Lightning is off in potato mode.
- **Galewharf** (1800, 0, -6000): a harbour on a rock above the clouds under the spire's wall. A cobbled square round a
  giant anchor, the Crow's Nest inn, timber houses and sailors' shacks, a windmill, a lighthouse, a long pier with
  Captain Brisa's Gullwing moored at it, Tinker Volt's shed with a lightning rod the storm likes, Tessa's pen, a campfire
  to rest at, and the gate with stairs up into a notch in the spire. A work place for every skill (67 stands, all
  reachable): the fishers drop their lines off the pier into the clouds, the sailors mend rope by the Gullwing.
- **Its people and chains:** Captain Brisa Windlass ("Brisa's sails": the pirates cut the Gullwing's sails; Sailing,
  the Windlass Band), Tinker Volt ("Lightning in a bottle": copper, a riddle, the wisps, bronze bars; Crafting, Bottled
  Lightning), Tessa ("Where is Woolly?": her sheep floated off on a cloud, and everyone can hear him baa; Farming,
  Woolly's Bell, a joke item like the Rubber Duck), and Skipper Hob of the Crow's Nest. Titles: the Sky Sailor, the
  Lightning Catcher, the Cloud Shepherd, and Climber at 25 (the Stormrider) and 30 (the Bane of Fulmina).
- **Builders:** `tools/tower/tower-skydocks-build.cs`, `tower-stormspire-build.cs`, `tower-galewharf-build.cs` (made from
  Rimeholt's, its helpers kept).
- **Not tested yet:** the chains with a signed-in character (the server's side), the real climb from floor 20 into the
  band (the town arrival plays, tested with the debug replay), boss fights balanced on band 3's floors.

## 19. The Dark Fortress dead and the Behemoth (2026-11-23)

Synty's POLYGON Dark Fortress characters became tower monsters (docs/dark-fortress-design.md): templates in the crypt
and marsh layouts (prefabs from `tools/dark-fortress/dark-fortress-enemies.cs`, put in by `dark-fortress-templates.cs`
and by the crypt and marsh builders), on the humanoid enemy animations.

- Floor 4: Risen Dead and Risen Soldiers beside the skeletons. Floor 7: Grave Knights (the pack's Knight, floor + 3)
  with the goblins and skeletons. Floor 8: Plague Walkers with the red and purple Metalons. Floor 9: Shambling Corpses
  and Crypt Wraiths with the red Metalons. Quests on 4, 8 and 9 name the new monsters.
- **Four band 1 bosses are Dark Fortress characters** (round 2, same day), each in a slot that had a reused look:
  floor 4 **Mortimer Bonewhistle** (the Necromancer, raises Risen Dead three times; was the Rootbound King), floor 7
  **Big Gorp, the Keep Colossus** (a ground slam every third swing, goblins at half health; was King Grubnose the
  Second), floor 8 **Lord Pestilus** (the Plague Lord, poison clouds every third swing, Plague Walkers; was Redshell
  the Blighted), floor 9 **Old Rotbelly** (the Behemoth, the Gardener's fight and numbers). Numbers set with the
  simulator so the floors stay about as hard; the table is in docs/dark-fortress-design.md.
- Elites: Bog Apothecary (floor 3), Antlered Reaver (floor 5, the overgrown layout has its template), Fallen
  Inquisitor (floor 10).
- **Trophies**: a boss can name items of its own (`TowerBoss.Trophies`); on the stream's first clear each fighter has
  `TowerLoot.TrophyChance` (1 in 4) of one. Floor 4: Bonewhistle Staff, Grinning Skull; 7: Colossus Cleaver, Keepward
  Shield; 8: Plague Mitre, Blightbone Staff; 9: Rotbelly Bonesaw, Rotreaper.

Played with the dev character and four bots: floor 9 (round 1) and full fights against the Necromancer, the
Colossus and the Plague Lord (their help, slam and spit), and the new monsters on floors 3, 4, 5, 7, 8 and 10. A real
clear (the server's trophy roll) was not played: the dev stream is on floor 6, so these were training attempts.

## 20. Legendaries from the top band (2026-11-23)

*Since 2026-11-24:* "floor 21 or higher" reads "floor 3 or higher": any part of floor 3, the Storm Spire, and every
guardian on it (mini bosses and the floor boss) is the source. `Legendary.TowerFloor` is 3 now, the floor's number;
`Legendary.TowerStep` is its first part's step, 21.

The Legendary weapons (docs/legendary-design.md) ask for a clear of floor 21 or higher fought in (the climber
achievement's highest floor, per character), and the boss of floor 21 and up is their best source: 4% a qualifying
fighter on the stream's clear, against 0.25% for a heroic dungeon and 0.1% for a raid. Every tower clear also counts a
boss kill toward awakening a worn Legendary. The line is a constant (21), not "the top band", so band 4 does not lock
anyone out when it comes. **When band 4 ships, raise it** (`Legendary.TowerFloor`; on the release checklist).

## 21. Band 4 concepts, the Dark Fortress (2026-11-23)

Planned for v0.9.8, not v0.9.7 (the owner agreed: finish and playtest v0.9.7 first). Floors 31 to 40, a dark
citadel band built from the Synty Dark Fortress pack (plus Dungeon Realms and Horror Mansion where they fit). The
owner asked for a fresh look, not a match with bands 1 to 3, and a town with a fresh layout too, so the climb does
not repeat itself. Concepts (Image Generator, from scratch, no earlier concept as reference) in
`docs/mockups/tower/band4/`:

- `floor-a-gatehouse.jpg`: the outer bailey, portcullis, green ghostfire, chains, wrecked siege engines, eclipse.
- `floor-b-great-hall.jpg`: a roofless great hall, pillars, chandeliers, a violet rune circle, feast tables.
- `floor-c-ramparts.jpg`: wall walks and bridges between towers over a misty chasm, hanging cages, a red moon.
- `boss-throne.jpg`: the open throne room at the top, ectoplasm channels, skull carvings, a vortex and eclipse.
- `town-a-lanternrest.jpg`: a warm camp of adventurers on a ledge in the fortress's shadow.
- `town-b-ghost-quarter.jpg`: a friendly ghost quarter with ghost shopkeepers inside the walls.

When it is built: raise the Legendary tower requirement (section 20) to the new top band, and model the custom
shapes in Blender from the picked concepts (memory: concept then Blender).

## 22. Floors of rooms (2026-11-23)

Zerratar (2026-11-23): "can we get some tower floors that are like not just a circled plateau but actual rooms and stuff?
so it can look like its inside a castle/dungeon/etc? not all of them need to look like it [...] but they actually have to
move about as they progress?" Every floor so far is one arena: the monsters stand round it and the group fights wherever
the nearest one is. A floor can now be a **route of rooms** instead, and the group fights its way through it.

**How it plays**
- A layout with a route (`TowerRoute` on the floor's root) has rooms (`TowerZone`) in order: an arrival, the rooms and
  corridors between, and the boss's room last. Each room has its own arrival point, its own monster spots and the gate
  (`TowerGate`) to the next room.
- All the rooms are filled when the floor is (the kinds dealt out over every spot in route order, as on a one-room
  floor), so the monsters ahead stand waiting behind their doors. The group only fights the rooms it has reached
  (`TowerFloor.NearestEnemy` skips the rest), so nobody runs at a shut door.
- When a room's monsters have all fallen, its gate opens (the ghostfire seal goes out, the leaves swing into the next
  room, a stone grind) and the players simply walk on to the next room's nearest monster: the walk is the navmesh, no
  teleport. A room that has had nobody kill anything for 90 seconds while players are on the floor opens anyway, so a
  monster that cannot be reached never shuts a floor.
- The rooms before the **hold** (the last room with monsters) stay cleared once cleared. The hold refills like a
  one-room floor (`Tower.EnemyRespawnSeconds`), so a floor of rooms stays open for training and its quests as long as
  the stream likes. Floor quests count every kill in every room.
- **The boss.** When an attempt is called, every gate up to the boss's room opens and everyone walks there during the
  60 second countdown and waits on an arc in front of the room's arrival (`TowerRoute.RallyPoint`; newcomers arrive
  there). The boss comes out at the floor's boss point in that room, its intro and fall work as before. After the
  attempt the players go back to the hold's monsters; the gates stay open.
- Arriving (`!tower`), getting back up after a knock out and `!tower floor N` all use the room the group is in
  (`TowerPlace.Spawn` is virtual; `TowerFloor` answers `TowerRoute.Arrival`). When the floor is filled again (a clear,
  `!tower floor N`) the route starts again at the first room, gates shut, and anyone on it goes back to its arrival.
- **Progress on screen:** the tower panel's bottom line shows "Room 2 of 4" (the boss's room not counted) while the
  floor's quests are open and nothing else needs that line (a vote, the boss ready, the countdown and the fight keep
  it). No new panel, no chat line per room (rooms clear in seconds with a big group; chat would drown).
- **What does not change:** the server decides the floor, its quests, the boss and the clear exactly as before; the
  rooms are the game's presentation of the same fight. Level sync, training away, votes, cutscenes, boss help, the
  boss's slam and spit, the after-clear walk to town: all as on a one-room floor. **A floor without a route works as
  before**: `Route` is null on floors 1 to 30, every new code path checks it.

**Seeing into rooms from the game's camera**
- The game's camera orbits the player it follows from 45 degrees up, slowly turning, so any wall can come between it
  and the fight. `TowerCutaway` (on the floor's root) cuts every wall piece that stands between the camera and what it
  follows down to its stub, and stands it up again once the camera has turned past it: the far walls always stand with
  their windows, banners and moonbeams, the near ones show where the room ends. The rule (`TowerCutaway.Between`): the
  wall's plane parts the camera from the followed player, and the line of sight crosses it within the wall's half
  length plus 7 m. A cut wall keeps its shadow (so the room's light does not change as the camera turns); what hangs on
  it hides with it (torch flames too). A few dot products per wall a frame; no setting needed.
- Each wall piece (`TowerWallPiece`) has its full wall and a stub a metre high. The stubs are the Dark Fortress pack's
  own 5 m wall cut in Blender with a broken top (`tools/tower/blender/tower_great_hall.py`), so the bricks match; a
  doorway's stub is the same with the arch's opening cut out. Pillars at the joints are pieces of their own (their
  stub is their foot).
- No ceilings or roofs: the halls are roofless ruins (the concept's broken rafters were left out; anything overhead
  would hide the fight from above).
- Gates stay standing when their wall is cut: a shut door with its green seal is what tells viewers the way is closed.

**The prototype: the band 4 great hall** (`Assets/Scenes/Tower/Tower_GreatHall.unity`, built by
`tools/tower/tower-greathall-build.cs`, concept `docs/mockups/tower/band4/floor-b-great-hall.jpg`). Five rooms of 6 m
Dark Fortress walls on dark stone tiles, about 45 by 110 m, at (0, 0, -6000) like every floor:
1. **The Gatehall** (arrival, 5 monsters): the skull gate the players came in by behind them, ghostfire braziers, rubble,
   the fallen.
2. **The Hall of Arms** (4): a gallery of knights in armor on plinths down both sides, a crimson runner.
3. **The Chapel** (5): pews either side of an aisle, an altar with candles and two knights, a small font of soul water.
4. **The Great Hall** (the hold, 10): the violet rune circle (a Blender mesh: rings, a seven pointed star, runes and
   cracks, glowing), feast tables with food, goblets and candles down both sides, knights between the windows,
   ghostfire at the corners.
5. **The Throne Room** (the boss's): a crimson runner to the throne between two great knights, the biggest fires.
The route zigzags: north, east into the chapel, north into the hall, north into the throne room. Night: cold moonlight
from high up with moonbeams through the windows on the moon's side (`TowerLightShaft`), a violet-blue dark, crimson
banners, green ghostfire. The ghostfire flames, candle lights and fill lights sit under a `TowerFx` group that goes
off in potato mode; the moonbeams already do; the braziers' and the seal's lights stay (the rooms would go black).
Monster templates: band 1's looks and the Dark Fortress dead, so any floor of 1 to 10 can be tried in it. The audit
(`tower-audit.cs`, now also checking every room's arrival) passes: 30 checks, no problems.

**Live and dormant.** The system code ships with v0.9.7 but nothing uses it: no floor in RavenNest.Combat names the
great hall, `TowerFloor.Route` is null in every v0.9.7 layout, and the server is unchanged. The great hall is reachable
only in the editor, from the admin debug menu's "Tower: Great Hall" (every floor then loads it, the loaded floor climbs
into it; "Tower: Own Layouts" goes back; `TowerManager.LayoutOverride`), and the scene is not in the build's scene list.
Band 4 (floors 31 to 40, v0.9.8) wires it in with `LayoutOf` and a scene list entry.

**Played** in the editor against the local server with the dev character and 3 to 7 bots, on floors below the dev
stream's (so nothing reached the server's climb): each room fought and cleared, the gates opening and the walk on
through every door, the hold refilling, a boss called (everyone walked to the throne room during the countdown), the
fight in the throne room with the slam ring, the boss falling, `!tower floor N` putting everyone back at the first room.
The panel's "Room 3 of 4" was checked by refreshing it in play with the local copy of the stream's floor set to the
loaded one (a training floor shows its training line there instead).
Captures from the game's camera: `docs/mockups/tower/band4/great-hall-*.png`.

**Weak spots, for later**
- A strong group clears a room in a few seconds; with 8 high level bots the whole route took about a minute and the boss
  of floor 5 fell in about 10 seconds (level sync's numbers, not the rooms'). Band 4's rooms may want more monsters or
  elites per room once its numbers are set with the simulator.
- Everyone piles onto the nearest monster, as on every floor: the group reads as one blob from far away.
- A cut wall pops rather than fades; the camera turns slowly, so it is rare, but it shows.
- The throne room is plain next to its concept (docs/mockups/tower/band4/boss-throne.jpg): it is the great hall's
  boss room for now, not the band's top.
- Outside the rooms is a flat dark void; from a low angle it reads as nothing rather than more fortress.

**Adding rooms to other floors**
- *A band 1 to 3 layout:* give its builder rooms in order with their spots and an arrival each (TowerZone), a TowerGate
  in each doorway (leaves, seal, a carving NavMeshObstacle saved disabled), a TowerRoute on the root and `floor.Route`
  set. Open layouts (the courtyard, the glacier) need only gates or barriers between areas (a fallen tree, an ice wall,
  a ward), no cutaway. Walled ones (the crypt, the frost vault) get `TowerCutaway` and pieces with stubs: the crypt's
  low walls could be TowerWallPieces at full height again. Nothing on the server changes; a floor can switch layouts
  in `LayoutOf` at any update.
- *The other band 4 floors:* the gatehouse (outdoors) is a route of open areas: the outer bailey, the portcullis as the
  gate (a TowerGate whose leaf rises instead of swinging: one more mode), the inner yard, the keep's door. The ramparts
  are rooms along a wall walk: towers as rooms (with cutaway), bridges and wall walks as corridors between them, the
  chasm below as the void. Both reuse this builder's helpers (wall pieces, stubs, gates, ghostfire) and the same
  stubs; each wants its own Blender pieces (bridge undersides, the chasm's cliffs, the portcullis).

## Open questions

- ~~Is the tower always open while a stream is live, or does the streamer open it?~~ Open on every stream: any viewer can
  `!tower` in, and chat climbs by vote when the streamer is away. A streamer who does not want it turns off **Tower open to
  chat** (settings, World): then `!tower` answers "The tower is closed on this stream", and players inside can still leave
  (2026-11-19).
- ~~Who can start a boss attempt: only the streamer, or chat by vote as well?~~ Both, and every other choice too (section 16).
- Should a stream's climb ever reset (seasons, with rewards for how far you got), or is progress forever?
- ~~Can players who fall during a boss attempt be revived, or do they sit out the attempt?~~ They sit it out (2026-11-19):
  anyone knocked out in the tower gets back up at the floor's or town's arrival and stays in the tower (before, they were
  sent back to the world and quietly dropped out of the climb). In a boss attempt they are out of it for good: the boss
  forgets them, its slams and spits pass them by, and the attempt is lost when every participant has fallen. They still
  share the clear's rewards.
- ~~Can players train on cleared floors?~~ Yes: `!tower floor N` (section 15; `!tower part N` since 2026-11-24) and the
  towns (section 14). Each player going to a cleared part alone, with several parts loaded, is the next step
  (tower-worlds-proposal.md, section 11).
