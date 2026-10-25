# Quests

Status: design draft, 2026-10-25, for v0.9.6. Nothing is built yet. Decided with the user on 2026-10-25: personal
quests and stream quests both; the unused achievement tables may be reshaped for achievements on the same tracker; mostly passive objectives with some active steps; quests built into the game plus
quests streamers post themselves; rewards of every kind (coins, items, exp, quest-only rewards, loyalty points,
village rewards). Then: three active personal quests and three daily offers, a weekly quest as well, and kill
objectives in phase 1. Also on 2026-10-25 the user asked to give the unused places in the world a story through
quests (the Ironhill runestone, the "insufficient bananas" island, Ravebridge), islands that streamers unlock with
quests, NPCs the streamer talks to, and a hero mode where the streamer fights waves with their viewers. The open
questions at the end need answers before phase 1 starts.

## Why

Ravenfall has many things that want a reason to happen: decor that should come from a quest chain, boat pieces for
own boats (`docs/own-boats.md`), seasonal events with tokens, loyalty points with nothing much to spend them on, a
village treasury, a tavern with a quest giver who says "Quests are coming in a future update!" (`!quest`), and a
quest board standing by the tier 3 town hall. Quests tie these together: they give viewers something to aim for
beyond the next level, give streamers a way to run their own goals for their community, and give the village a
shared project.

## What exists to build on

- **`!quest`** answers "no quests yet", and in the tavern the Quest giver NPC (`TavernNpc.NpcRole.QuestGiver`) says it
  to the player too.
- **A quest board** prop with posters stands in front of the tier 3 town hall.
- **`ItemCategory.QuestItem`** exists for items that only quests give or take.
- **Seasonal windows** (Christmas, Halloween) and the seasonal tokens; decor already knows when an event runs.
- **Loyalty points** per viewer per streamer, and the **village treasury** (the village's coins, which the tavern
  shelves fill). Decor has a "quest" unlock source waiting.
- **What the server sees of a player** without anything new: experience and levels per skill (every save), every
  item that enters or leaves an inventory (the inventory feed, `GameData.InventoryItemChanged`), raid and dungeon
  rewards (it hands them out), which island a player is on (the player state saves), and the commands players run
  (craft, cook, vendor, gift, ...). That is enough to count almost any passive objective on the server, so quest
  progress cannot be faked by a modified game.

## Two kinds of quest

### Personal quests

A player's own. Each character has a quest log: up to three active quests at a time. Progress is the character's and
follows them from stream to stream; it only moves while they play in some stream.

- **Daily quests.** Every day each player is offered three, from a pool scaled to their levels (a level 20 miner is
  not asked for 500 adamantite). They take one or more with `!quest accept`. Unfinished dailies expire at the day's
  end. A small reward, and a daily streak that grows it.
- **Weekly quest.** One bigger quest a week per player (a raid streak, a crafting order, a long gathering goal), with
  a bigger reward, outside the three active slots.
- **Seasonal quests.** Offered only while an event runs (Christmas, Halloween, later others): themed objectives and
  the event's own rewards (tokens, seasonal cosmetics, the event's decor for the village).
- **Chain quests.** Short stories of three to eight steps, written into the game, each step unlocking the next. The
  first chains: "The shipwright" (gathering timber and iron for a boat, ending in the first own-boat pieces), "The
  village founder" (a statue for the village), and a silly one or two (the user loves easter eggs: the barkeep's lost
  cat, a fish that keeps escaping).

### Stream quests

The whole stream works on one goal together, shown to everyone. Anyone in the stream adds to it by playing.

- **Built-in stream quests.** The game offers the streamer a rotating set: "Gather 50,000 wood together in two
  hours", "Win three raids tonight", "Reach a combined 200 levels this stream". The streamer starts one from the
  quest window (the HUD's stream quest pill, or a click on any quest board) or with `!quest stream start <n>`.
  Not in build mode: starting a quest is something done while the stream runs, and a streamer who never builds
  would never find it there (decided 2026-10-25).
- **Streamer quests.** The streamer writes their own from the same objective types, with a target, a time limit and
  a reward they pay for: items from their stash, coins from the village treasury, loyalty points. The reward is set
  aside when the quest is posted, so it is always there to give.
- **Rewards** go to the village (treasury, resources, decor unlocks) and to everyone who helped, by how much they
  added (with a minimum share, so a small helper gets something).

## Objectives

Most objectives count what players already do while idling, so a quest never asks a viewer to sit and type. Some
steps, mostly in chains, ask for one command, which makes those moments feel like an event.

**Passive (counted on the server):**

| Objective | Counted from |
|---|---|
| Gather N of an item or resource (wood, an ore, a fish) | items entering the inventory from gathering (the inventory feed, by source) |
| Craft, cook or brew N (of an item, or of a kind) | the craft and produce commands |
| Gain N experience in a skill, or reach level L | the experience saves |
| Join, win or clear raids or dungeons (of a tier) | the server's raid and dungeon rewards |
| Defeat N enemies (on an island, or of a kind) | new in phase 1: the game reports kills per enemy kind with its saves |
| Sail to an island, or spend time on one | the player state saves (island) |
| Earn N coins, sell N items | the vendor and coin changes |

**Active (a command):**

- **Deliver** items to the quest giver: `!quest deliver`, which takes them from the inventory (quest items, or
  something the player gathered for it).
- **Visit**: be on an island or in the tavern and say `!quest`, for example to talk to an NPC who continues a chain.
- **Answer**: a riddle or a choice in a chain (`!quest answer 2`), for the silly ones and the stories.

## Places waiting for a story

The world has places that were built and never used. Quests give them their meaning:

- **The Ironhill runestone.** In the Ironhill cave a glowing runestone says "Power is emerging from this area... But
  nothing seem to be happening." A stream chain wakes it: the stream gathers and brings what it asks for over several
  steps, each step brightening it, until it opens. What it opens is the hook for the islands below (a portal, or the
  way to Ravebridge).
- **The "insufficient bananas" island.** North east of the main island, a mangrove island with stilt huts, docks, a
  tower and lobster pots, with "insufficient bananas" floating over it. The monkeys (or pirates) who live there want
  bananas: a stream quest gathers them (a new resource, perhaps only found during the quest), and when there are
  enough the island opens, with its own training spots and a reward for the village. The sign stays, of course,
  changing to "sufficient bananas" once paid.
- **Ravebridge.** A tribute to RuneScape Classic's Lumbridge, far to the west: the river, both bridges, the castle,
  the cow field, the crop field and a few houses are there; much of Lumbridge is not yet (the general store, the
  church, the kitchen, the goblins across the river, the swamp, the mill, chickens, more houses). The old joke is that
  a player who reaches level 999 in every skill is reborn in Lumbridge. Ravebridge becomes a place for an event and a
  chain: finished with its missing buildings and NPCs the streamer can talk to, opened by a chain (perhaps through
  the runestone), and the place where maxed characters are "reborn" (see the open questions for what that gives).

### Ravebridge as the streamer's own RuneScape (2026-10-25)

The user's direction for Ravebridge, beyond a quest place:

- **An event island.** Ravebridge opens as an event: the streamer starts it (or an event forces it), and viewers who
  want to help join the way they join a dungeon. Their characters are taken to Ravebridge and brought back when the
  event ends. On the island they do what they always do (cut trees, mine, fish, fight), plus a few chat commands made
  to help the streamer (heal them, fight beside them, bring them things).
- **The streamer plays, by hand.** The streamer walks a character around with WASD and the mouse, in the old
  RuneScape way: cutting trees, mining, fighting goblins, talking to NPCs, doing Ravebridge's own RuneScape-like
  quests. This is hero mode's other half: not only waves, an island to live on.
- **Which character.** If the streamer has one of their own characters in the stream, they choose (a character
  select): that character, whose skills, items and progress save to the server as usual, or their **Ravebridge
  character**: a character made for this island only, who levels up there and can never leave it. The Ravebridge
  character is not synced to the server; it is saved on disk, somewhere safe and easy to back up and move (so a
  reinstall or a new machine does not lose it), for example in the game's states folder with an export and import.
- **References.** RuneScape Classic's Lumbridge and its surroundings, the screenshot and the two maps in
  `docs/references/ravebridge/` (the castle with its round towers, the courtyard fountain and well, the general store,
  the church and graveyard, the river with its bridges, the cow and crop fields, the windmill to the north west, the
  swamp to the south, the goblins across the river, and roads toward Draynor, Varrock and Al Kharid).

### Endgame (to talk about later)

"Reborn in Ravebridge" began as a joke, but it points at a real gap: a character with every skill at 999 has nothing
fun left to do. Whether the answer is a rebirth, a prestige or something else is to be worked out together later.

## Islands unlocked by quests

Today's islands are the same for every stream (Home, Away, Ironhill, Kyo, Heim, Atria, Eldara, plus War Island for
raid wars). New islands become something a streamer earns: a stream quest chain opens an island for that stream for
good, and then players can sail there, train there and find what is only there. The first ones are the banana island
and Ravebridge; later ones can follow without changing the existing islands. On the server it is one more unlock per
streamer (like the decor unlocks); in the game a locked island is shown but cannot be sailed to, with what opens it.

## NPCs the streamer talks to

Chat commands work for everyone, but the streamer is at the keyboard and should get some of an RPG's feel too. NPCs
in the world (the quest giver, the runestone, the monkey chief, Ravebridge's people) can be clicked by the streamer:
a dialogue window opens with the NPC's lines and talk options. That is where the streamer starts stream quests,
continues chains (a choice in the story), hands in the stream's deliveries, and gets a few silly answers. Viewers see
the conversation on stream; chat can vote on a choice with `!quest vote 2` when the streamer asks.

## Hero mode

An event where the streamer takes control: their own character, moved with WASD and acting with the mouse (attack
where clicked, skills on 1 to 4, a dodge), not automated, fighting waves of enemies in an arena (War Island, or a
place of its own). Viewers join with chat and fight automatically as they do now, running at enemies and healing;
some commands let them help on purpose (a heal, a taunt, a cheer that buffs). Each wave is harder, ending with a
boss; how far the stream gets is its score. The idea is that the streamer goes "WTF! Let's go!" and the whole chat
pulls together.

It needs its own screen: an action RPG HUD for the streamer (health and mana orbs, the skill bar with cooldowns, the
wave counter, the boss bar, who in chat is helping most), the camera following the streamer's character, and the
normal HUD put away. It is a feature in its own right, with its own design doc before it is built, and it fits
quests well: a chain can end in a hero mode fight (the runestone's guardian, Ravebridge's goblin raid), and stream
quests can ask for a wave reached.

## Achievements

The server has `Achievement` and `CharacterAchievement` tables that were never used (decided 2026-10-25: reshape them
as needed). They become lifetime milestones, counted by the same tracker as quest objectives, so they cost little
once quests exist:

- **`Achievement`** turns into a definition like a quest's: key, name, description, objective type and target, tier
  (bronze, silver, gold for the same objective at growing targets), the reward (a title, a badge, sometimes a
  cosmetic), hidden or not (hidden ones for the silly easter eggs).
- **`CharacterAchievement`** keeps the character's progress as well as the day it was reached.
- Examples: cut 10,000 trees, win 100 raids, reach level 100 in every skill, finish every chain, give the village ten
  decor gifts, and a few hidden ones ("Fish up the same boot five times").
- They need no accepting: every character makes progress on every achievement all the time. The game announces one
  when it is reached (chat and a toast), and the player details window and the website show them.

## Where quests are given

- **The tavern's quest giver** offers personal quests (daily, seasonal, chains) and speaks the chain's story lines
  in a bubble, as the tavern NPCs do now. The tavern redesign (`tavern-and-quests-plan`) gives it its own corner.
- **The quest board** by the town hall shows the stream quest (goal, progress bar, time left, top helpers) and the
  streamer's posted quests. Every town hall tier gets one; tier 3 has it already.
- **Chat** for everything: `!quest` (what you have and what is offered), `!quest accept <n>`, `!quest log`,
  `!quest deliver`, `!quest abandon <n>`, `!quest stream` for everyone, and for the streamer and the moderators
  `!quest stream start <n>` and `!quest stream stop` (`!quest start` stays an alias of accept).
- **The game's HUD** shows a slim bar with the running stream quest, and the player details window gets a Quests tab.
- **The website** later: a player's quest log and finished chains, and posting streamer quests from the browser.

## Rewards

| Reward | Personal | Stream |
|---|---|---|
| Coins, items, experience | scaled to the quest and the player's levels | shared by contribution |
| Loyalty points with the streamer | yes, small amounts (they buy decor gifts and later redeemables) | yes |
| Quest-only rewards | titles, cosmetics, pets, boat pieces (chains) | a stream title or banner |
| Village rewards | a chain's statue or decor unlock | treasury coins, resources, decor unlocks |

Quest-only rewards are what make quests worth doing past the first week: things that cannot be bought. A finished
chain's title shows on the player's name tag; a village that finished a stream chain gets a unique piece of decor.

Loyalty points from built-in quests come from the game, not from the streamer, capped per day so they do not flood
the economy. Points in a streamer's quest come from the streamer's own budget (set aside when posted).

## The server

- **Quest definitions** are data: a catalog of quest templates on the server (key, kind, objective type and
  target, level range, rewards, chain steps, season), loaded from JSON so new quests and chains need no code. The
  game gets the parts it shows (names, texts) and has the localized strings.
- **`CharacterQuest`**: a character's quest (definition, step, progress, accepted, expires, finished).
- **`StreamQuest`**: a stream's quest (streamer, definition or the streamer's own objective, target, progress, start,
  end, the reward set aside), with **`StreamQuestContribution`** (character, amount).
- **`QuestRewardLog`**: what was given to whom, for support questions and to stop double rewards.
- **`Achievement`** and **`CharacterAchievement`**, reshaped as above.
- **Progress** is counted where the server already sees the activity (the table above), through one
  `QuestTracker` that every source reports to ("this character gained 120 wood", "this character won a raid"). It
  updates the character's and the stream's quests and tells the game when something moves, through the v2
  connection's events (a progress event per character, batched like the inventory feed).
- **Daily offers** are made per character at the first request of the day, from their levels, the same for the whole
  day (so `!quest` twice shows the same three).

## The game

- `!quest` and its sub commands, replies in chat and the quest giver's bubble.
- The quest window, opened from the HUD bar (or its pill when no quest runs) and by clicking any quest board in the
  world (each town hall tier's and the tavern's); never only in build mode.
- A Quests tab in the player details window.
- Quest givers for chains outside the tavern (a shipwright at the docks, later).

## Phases

1. **Personal daily quests.** Built 2026-10-25: `QuestManager` on the server (the catalog, `CharacterQuest`,
   `CharacterQuestStats`, `QuestTracker` fed by gathering rounds, experience saves, crafting and producing, raid and
   dungeon rewards and the game's new `EnemiesDefeated` count), `api/quests`, the QuestCompleted event (49), `!quest`
   (list, accept, abandon) on both bots and the game, the tavern quest giver's lines, a weekly quest. Tested against
   the local server: offers, taking, a kill quest finished through the saves, coins paid, the streak counted.
   First version:  Server catalog, `CharacterQuest`, the tracker for gather, craft, experience and raid or
   dungeon objectives, `!quest`, `!quest accept`, `!quest log`, `!quest abandon`, the tavern quest giver offering
   them, coin, item and experience rewards.
2. **Stream quests.** Built 2026-10-25: `StreamQuestManager` on the server (nine templates: wood, ore, fish and
   wheat rounds, kills, items made, levels gained, raids, dungeons; targets and rewards scaled to the players in the
   stream, at least 3; three offers a day per streamer; one at a time; ends when done, out of time or stopped),
   `StreamQuest` and `StreamQuestContribution`, `api/quests/stream` (state, start, stop), the StreamQuestEnded event
   (50). Done pays the village coins (and as much again of a gathered resource) and splits a coin pot among the
   helpers by share (at least 100 each) with 10 loyalty points each with the streamer. In the game: the HUD bar at
   the top center (a pill when none runs), the quest window (offers with Start, the running quest with top helpers
   and Stop), quest boards open it, `!quest stream`, chat announcements on start and end.
3. **Chains, seasons and achievements.** Built 2026-10-25, decided with the user: titles on a line of their own
   above the name tag, all four chains, Halloween quests at once. On the server: `QuestChains` (the chains as data:
   steps of the normal objectives plus deliver, visit and answer), `QuestManager` with seasonal dailies (one a day
   while Halloween or Christmas runs, outside the three slots, `!quest accept seasonal`) and chains (one under way at
   a time; `CharacterQuest` gained `step` and `target`; finished chains never expire), `AchievementManager` (18
   achievements in code, `CharacterAchievement` reshaped to key, progress and tier, three tiers each with a title,
   two hidden ones), the chosen title in `CharacterQuestStats.title`, endpoints for chains, deliver, answer,
   achievements, title and the session's titles, the AchievementReached event (51), and quest decor pieces
   (`VillageDecorPiece.Quest`) unlocked for the village where the chain is finished. The chains: the barkeep's lost
   cat (Sir Whiskers, an orange cat), the one that got away (a giant fish on a plinth), the village founder (the
   founder's statue), the shipwright (a boat in the making; the finished chain is the first boat piece for own
   boats) and the Pumpkin King (Halloween only, a jack-o'-lantern). In the game: `!quest chains`, `!quest begin`,
   `!quest deliver`, `!quest answer`, `!quest abandon chain`, `!title` and `!achievements` (both bots send them as
   `quest_command`), the story lines, endings and riddles in EN and SV (the Swedish riddles are other riddles with
   the same answer number), the quest giver telling the story in the tavern, and the title on the name tag.
   Quest items (items only quests give) and badges as pictures are left for later; deliveries ask for items
   gathering drops.
4. **Streamer quests.** Built 2026-10-25, decided with the user: both kinds, a bounty (each viewer works alone; the
   first Winners to reach the target each get the reward) and a stream goal (everyone together; it takes the stream
   quest's one place, coins and points are shared by share, the item goes to the top helpers); rewards from the
   village treasury, the streamer's character's coins and the streamer's items (stash or character), set aside
   through the tavern shelves' `TradeExecutor` escrow and returned when the quest ends; loyalty points from a weekly
   budget per streamer (2,500 a week, not carried over), because loyalty points from every stream are one pot and
   streamers may not print them freely; posted from the quest window's second tab with a name and a description of
   the streamer's own (a chat command and the website later). On the server: `StreamerQuestManager`,
   `StreamerQuest`, `StreamerQuestEntry`, `StreamerQuestBudget`, `api/quests/streamer` (list, a character's view,
   post, stop), events StreamerQuestClaimed (52) and StreamerQuestEnded (53). In the game: `StreamerQuestPanel`
   (the running quests and the form), the streamer's goal on the HUD bar, `!quest` shows the bounties with each
   viewer's own progress, `!quest stream` the goal, chat announcements on posting, claiming and ending.
   Later: per-streamer redeemables that only that streamer's loyalty points buy (the user's idea, 2026-10-25).
5. **Places and islands.** The runestone chain, the banana island chain and its island unlock, island unlocks per
   streamer, NPC dialogue for the streamer.
6. **Ravebridge.** Finishing the island (buildings, NPCs, monsters), its chain and event, rebirth.
7. **Hero mode**, with its own design doc first.

## Open questions

- **Endgame:** what a character with every skill at 999 does next (see above), to be talked through later.
- **Hero mode:** decided 2026-10-25 that it is event driven and can also be started by the streamer outside an event;
  some events force it; it uses Ravebridge (waves and the RuneScape-like island play above). Its own design doc
  comes first.
- **Stream quest timing:** a stream quest runs for a set time (one or two hours), until the stream ends, or until it
  is done?
- **Streamer quests:** decided 2026-10-25: loyalty points only from a weekly budget the streamer hands out.
- **Offline:** progress only while the character plays in a stream (proposed), never from outside the game.
- **Quest-only rewards:** which titles, cosmetics and pets first? The art for them (Synty packs have pets and
  cosmetics already).
- **Rerolls:** may a player swap a daily offer they do not like (once a day, or for coins)?
