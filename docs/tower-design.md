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

## Open questions

- Is the tower always open while a stream is live, or does the streamer open it, as with Ravebridge?
- Who can start a boss attempt: only the streamer, or chat by vote as well?
- Should a stream's climb ever reset (seasons, with rewards for how far you got), or is progress forever?
- Can players who fall during a boss attempt be revived (a Restoration skill), or do they sit out the attempt?
- ~~Can players train on cleared floors?~~ Yes: `!tower floor N` (section 15) and the towns (section 14).
