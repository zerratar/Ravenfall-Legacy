# Quests

Status: design draft, 2026-10-25, for v0.9.6. Nothing is built yet. Decided with the user on 2026-10-25: personal
quests and stream quests both; the unused achievement tables may be reshaped for achievements on the same tracker; mostly passive objectives with some active steps; quests built into the game plus
quests streamers post themselves; rewards of every kind (coins, items, exp, quest-only rewards, loyalty points,
village rewards). The open questions at the end need answers before phase 1 starts.

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
  quest board (build mode or the tavern) or with `!quest start`.
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
| Defeat N enemies (on an island, or of a kind) | to be added: the game reports kills in its saves |
| Sail to an island, or spend time on one | the player state saves (island) |
| Earn N coins, sell N items | the vendor and coin changes |

**Active (a command):**

- **Deliver** items to the quest giver: `!quest deliver`, which takes them from the inventory (quest items, or
  something the player gathered for it).
- **Visit**: be on an island or in the tavern and say `!quest`, for example to talk to an NPC who continues a chain.
- **Answer**: a riddle or a choice in a chain (`!quest answer 2`), for the silly ones and the stories.

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
  `!quest deliver`, `!quest abandon <n>`, and for the streamer `!quest start <n>` and `!quest stop`.
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
- The quest board in the world (a window when clicked in build mode or the tavern) and the HUD bar.
- A Quests tab in the player details window.
- Quest givers for chains outside the tavern (a shipwright at the docks, later).

## Phases

1. **Personal daily quests.** Server catalog, `CharacterQuest`, the tracker for gather, craft, experience and raid or
   dungeon objectives, `!quest`, `!quest accept`, `!quest log`, `!quest abandon`, the tavern quest giver offering
   them, coin, item and experience rewards.
2. **Stream quests.** Built-in stream quests, `StreamQuest` and contributions, the quest board window and the HUD
   bar, village rewards (treasury, resources), `!quest start` for the streamer.
3. **Chains, seasons and achievements.** The step system, deliver, visit and answer steps, quest items, seasonal
   quests, the first chains with quest-only rewards (titles, decor unlocks, the shipwright chain toward own boats),
   and achievements on the same tracker, with titles and badges.
4. **Streamer quests.** Posting a quest with its reward set aside, from the game and later the website; loyalty point
   rewards from the streamer's budget.

## Open questions

- **How many and how often:** three active personal quests and three daily offers, or other numbers? A weekly quest
  as well?
- **Stream quest timing:** a stream quest runs for a set time (one or two hours), until the stream ends, or until it
  is done?
- **Streamer quests:** may they reward loyalty points freely (the streamer's own currency) or only within a cap?
- **Enemies:** kill objectives need the game to report kills; worth adding in phase 1, or later?
- **Offline:** progress only while the character plays in a stream (proposed), never from outside the game.
- **Quest-only rewards:** which titles, cosmetics and pets first? The art for them (Synty packs have pets and
  cosmetics already).
- **Rerolls:** may a player swap a daily offer they do not like (once a day, or for coins)?
