# Trade skills: skill trees for gathering and making

**Status: decided by Zerratar on 2026-11-22 and built the same day** (master and the development database only,
nothing deployed until v0.9.7). It began as a proposal with numbers for "2 skill tree tabs, combat related and
another for the rest" (2026-11-21); the decisions are in section 2, what was built in section 8, what was played in
section 9, and the calls made while building, which are the owner's to change, in section 10. The combat trees and
Beastmaster are in `combat-skills-design.md`.

## 1. Why

- **Nine skills earned nothing in the trees.** Skill points came from the eight combat skills and Taming.
  Woodcutting, Mining, Fishing, Farming, Gathering, Crafting, Cooking, Alchemy and Sailing gave none, and there
  was nothing to spend them on.
- **A lot of players live there.** Of 3,151 characters used on production from 22 to 24 October 2026 (the three
  days before the development copy was taken, the only clean sample at hand), **717 (23%) have more levels in
  those nine skills than in all combat skills together.**
- The combat design promised a small "Artisan tree" for things like keeping a gem. It was never built as a tree:
  its one node, Gem Salvager, sat in Hunter (Slayer).

| Of the 3,151 characters | level 100+ | 300+ | 600+ | 999 |
|---|---|---|---|---|
| best gathering skill | 464 | 193 | 81 | 27 |
| best of Crafting, Cooking, Alchemy | 425 | 157 | 54 | 14 |
| Sailing | 151 | 46 | 27 | 11 |

## 2. What was decided (2026-11-22)

1. **One pool of points, not two.** Zerratar: "i think it can contribute to combat skill points, but it doesnt have
   to raise the combat level". The nine trade skills earn into the same pool as the combat skills and Taming, and
   the trade trees' nodes are paid from it. The proposal's pool of its own is gone. The combat level is untouched.
2. **The "Trade" tab stays** as the way to show the two new trees apart from the nine combat-side ones, on the
   website's planner and character page, with the one shared points counter.
3. **Rate, node numbers and costs as proposed** (Bountiful 10%, Quick Return 10%, no exemption for finds with a
   long wait). No experience nodes. Seafaring is not built; Sailing levels earn points.
4. **Nest Finder: yes, "if its a suitable egg".** The eggs are those of the three hatched kinds whose role is
   gathering (section 4).
5. **Gem Salvager moves from Hunter to Artisan** and needs Crafting 300 in place of Slayer 300.
6. Everything else by the proposal's recommendations.

### One pool, and what it changes

The proposal argued for a pool of its own: one pool lets a fighter with 300 points drop 45 into gathering without
feeling it. That is now allowed on purpose. What still holds a tree back is its **level gate**: a trade node needs
a trade skill at its tier's level, so points from fighting open nothing in a tree whose skills were never trained,
and a gatherer's levels alone pay for a gatherer's nodes (44 points from one skill at 999, see below).

The rule is in one small place in the code, `RavenNest.Combat.SkillPoints` (which pool pays for a tree, what the
levels earn into a pool, what a build spent from it), so a split into two pools later is a change there and in
what shows the points, not in every caller.

## 3. Points

**One point per 10 trade levels (all nine added up) up to 400, then one per 150.**

`TradePoints(total) = floor(min(total, 400) / 10 + max(0, total - 400) / 150)`

Fast at the start so a new player has something to pick within the first hour, slow afterwards so that being
high in everything does not buy every node. Worked out from the levels each time, like the combat points and
Taming's: nothing to migrate, and nobody's points from fighting change because the three are counted apart and
added (`SkillTrees.EarnedPoints`).

| Who | Trade levels | Points from them |
|---|---|---|
| New character (nine skills at 1) | 9 | 0 (the first at one level gained) |
| Median of the sample | 97 | 9 |
| Upper quarter | 130 | 13 |
| One skill at about 390, or nine at 45 | 400 | 40 |
| Top tenth | 783 | 42 |
| One skill at 999, the rest untouched | 1,007 | 44 |
| Top twentieth | 1,302 | 46 |
| Top hundredth | 3,007 | 57 |
| The highest in the sample | 6,760 | 82 |
| All nine at 999 | 8,991 | 97 |

**The whole pool:** 274 from the eight combat skills at 999, 34 from Taming, 97 from the trade skills: **405 points
at the very most**, against trees that cost **957** (the nine combat-side trees 803, Gathering 70, Artisan 84). So a
character with everything at 999 fills 42% of everything; before the trade trees it was 308 of 812 (38%).

Against the two trade trees alone: they cost 154 together. One skill at 999 with nothing else (44 points) pays for
the five Gathering nodes below level 600 less one rank. A fighter who also gathers pays for them out of the same
pool and has that much less for combat.

## 4. The two trees

How gathering works, because the numbers lean on it (server, `ResourceTaskProcessor` and `SimpleDropHandler`):
every 10 seconds of gathering is a **round**. Each round the village gets one of its resource. One round in two
also tries for a **find**: the item aimed at first (`!mine rune ore`), with a chance of its base (mostly 20%) plus
0.5% per level above what it asks for, then the others. Each find then has a **wait** before the same character
can find it again: 10 seconds for Copper Ore, six hours for Atlarus Ore.

So common things are held back by rounds, rare things by their wait. That difference decides what a node does to
the economy (section 6).

**The gate:** a node can be learned once **one** of its tree's skills is at its tier's level (the combat trees'
tiers: 1, 100, 300, 600, 999), and it then **works in each of the tree's skills that is at that level**, counted by
the skill's own level, gear aside. Bountiful (level 100) works while mining with Mining 100 and not while fishing
with Fishing 40. The capstones' "in a skill at 999" follows from the same rule. Costs are the combat trees' own: 1
to 5 points a rank by tier, 15 for a capstone.

### Gathering (Woodcutting, Mining, Fishing, Farming, Gathering): 7 nodes, 70 points

| Level | Node | Ranks x cost | What it does | Comes after |
|---|---|---|---|---|
| 1 | Sure Hands | 5 x 1 | +2% chance on every find per rank (10%: what 20 levels give). The points are added before a gatherer pet's Keen Nose multiplies. Helps most with things at your own level; does nothing once the chance is 100% | - |
| 1 | Village's Share | 5 x 1 | 6% chance per rank that a round gives the village one more of its resource (30%). Nothing in the Gathering skill itself, which gives the village no resource | - |
| 100 | Bountiful | 5 x 2 | The "double gather": 2% chance per rank that a find is two (10%), on top of a gatherer pet's Forage (5%, one roll for both) | Sure Hands |
| 100 | Tireless | 5 x 2 | The safe kind of gathering speed: 2% chance per rank that a round counts twice (10%). One more for the village and one more try at a find; waits still count | Village's Share or Sure Hands |
| 300 | Quick Return | 5 x 3 | The other kind of speed: every find's wait is 2% shorter per rank (10%) | Bountiful or Tireless |
| 600 | Nest Finder | 1 x 10 | A pet egg while gathering, about one in 20 hours (1 in 7,200 rounds, rolled once a round whatever Tireless did). The egg is a gatherer's: Chick Egg (weight 10), Kitten Egg (8) or Piglet Egg (6), the three hatched kinds whose role is gathering, from a table of their own (`EggSource.Gathering`) | Quick Return |
| 999 | Master of the Trade | 1 x 15 | In a gathering skill at 999: every round counts twice for the village, and Tireless's chance is doubled (20%) | Nest Finder or Tireless |

Tireless is the proposal's "Second Wind". It could not keep the name: Vitality has an active called Second Wind,
and `!learn second wind` must mean one skill.

### Artisan (Crafting, Cooking, Alchemy): 7 nodes, 84 points

Making is instant (one command makes any amount), so there is no speed to sell here. What there is: 34 of the 49
cooking recipes can fail (20 to 50% success at the recipe's own level, 100% about 185 levels above it), crafting
and alchemy never fail, gems have a keep chance and combining fees, enchanting has a wait.

| Level | Node | Ranks x cost | What it does | Comes after |
|---|---|---|---|---|
| 1 | Steady Hands | 5 x 1 | +3% success per rank where a recipe can fail (15%, never past certain). A recipe that starts at 20% starts at 35% and is safe 115 levels above it in place of 185. Cooking only, as things are | - |
| 100 | Thrifty | 5 x 2 | 2% chance per rank that making something uses up no ingredients (10%). The ingredients must be owned; they are only left where they are, so nothing is made from nothing | Steady Hands |
| 100 | Second Helping | 5 x 2 | Cooking and Alchemy: 2% chance per rank of one more meal or potion (10%), of a make that worked. Not for crafted gear | Steady Hands |
| 300 | Gem Salvager | 3 x 3 | From Hunter, unchanged: +5% chance per rank to keep a gem taken out of its socket (55% at most with the clan's part). **Crafting 300** | Thrifty or Second Helping |
| 300 | Gem Cutter | 5 x 3 | Combining gems costs 6% less per rank (30%): 1,000 / 5,000 / 25,000 / 125,000 coins become 700 / 3,500 / 17,500 / 87,500. **Crafting 300** | Thrifty or Second Helping |
| 600 | Enchanter's Patience | 5 x 4 | The wait between two enchants is 3% shorter per rank (15%); the shortest wait (30 seconds) stays. **Crafting 600** (Enchanting itself is the clan's skill) | Gem Salvager or Gem Cutter |
| 999 | Master Artisan | 1 x 15 | In a skill at 999: Thrifty and Second Helping are each worth 5% more there (15%), where they are learned | Enchanter's Patience, Thrifty or Second Helping |

The three gem and enchanting nodes are opened by Crafting alone (`SkillNode.Skill`); the others by any of the
three skills. A capstone's path normally comes from the tier below. Here it also comes from the nodes it makes
better: a cook at 999 with no Crafting cannot learn the Crafting nodes between, and a gatherer should not have to
buy eggs to master the trade.

Hunter lost Gem Salvager and costs 63 in place of 72. The node kept its id, ranks and cost, so a build that learned
it in Hunter keeps the ranks and the points spent on them, and they still count (the server reads the rank, not
the level). Nobody has to be paid back: the trees are not released, and `SkillTrees.Version` was not raised.

## 5. Seafaring (Sailing): a sketch, not built

What Sailing has that a node could turn: the captain's cut (a guest's fish at sea gives the boat's owner a 20%
chance of one too), the sea's own fish, the regatta, the ferry, boat building. Candidates:

- **Captain's Cut:** +2% per rank on that chance (20% to 30%).
- **Deep Water:** Bountiful's chance is worth half as much again at sea.
- **Good Host:** guests aboard your boat get a share more experience while they fish there.
- **Shipwright's Friend:** boat blueprints and looks cost less.

Four nodes are not a tree and two of them need the boat numbers looked at first. Sailing's levels earn points from
the first day; Seafaring can be added when it has seven nodes worth choosing between. The map draws any number of
trees (three would be three fans).

## 6. What it does to supply and prices

More per swing is more ore on the market. These are the proposal's numbers; the decision was to keep them.

**One player with every Gathering node, three cases:**

| Case | Before | With the tree | Change | Which nodes |
|---|---|---|---|---|
| Common: Copper Ore at Mining 200+ (wait 10 s, chance 100%) | 180 an hour | 208 an hour | +16% | Tireless, Bountiful |
| At your level: Rune Ore at Mining 110 (chance 20%, wait 250 s) | 10.3 an hour | 13.9 an hour | +35% | all four |
| The rarest: Atlarus Ore at Mining 999 (wait 6 h) | 3.98 a day | 4.87 a day | +22% | Quick Return, Bountiful |

(Copper's wait is as long as a round, so half of Tireless's second tries land inside it and bring another ore
in its place: all finds together are up 21%.)

With a gatherer pet out (Forage: 5% for one more, 6.5% with Keen Senses) the rarest goes to 5.16 a day, +30% on
3.98 without a pet.

Taming gear (docs/taming-gear-design.md, 2026-11-22) adds its PET_GATHERING to Keen Senses: up to 10% more of the
pet's gather skills at tier 8, so Forage at 5% x (1 + 0.3 + 0.1) = 7% with both. A small step on the line above.

**The dial for the rarest things** is Bountiful's and Quick Return's last rank, and nothing else touches them:

| Bountiful / Quick Return at full rank | The rarest finds, per player with both |
|---|---|
| 5% / 5% | +10.5% |
| **10% / 10% (decided)** | **+22%** |
| 15% / 15% | +35% |

**How many players that is.** Of the 3,151 characters, 21 can mine Atlarus Ore (Mining 999) and 34 Ancient Ore
(850). The top of the other skills is nearly empty: 3 characters can cut Voidheart Logs (950), 1 can catch a
Leviathan (900), 4 can farm Truffles (800). So the market this moves is mostly ore. Both nodes at full rank cost 26
points with Sure Hands' one rank on the way. **With one pool every one of those miners can pay that**, out of
mining's own 44 points or out of fighting's, so expect about a fifth more top ore from the players who mine it,
once they have spent the points. (With a pool of its own the answer was the same for a 999 miner; one pool only
adds the miners below 999 who fight a lot.)

**What is kept out of the rarest things on purpose:** Tireless (more rounds) and Sure Hands (more chance) do
almost nothing to a find with a six hour wait, and the capstone is village resources and Tireless only. An earlier
draft had a "next find ignores its wait" burst: for Atlarus Ore that is twelve times the supply, so it is not here.

**Artisan:** Thrifty takes up to 10% (15% at 999) out of what making things uses up, Second Helping adds as much
food and potions. Gem Cutter takes up to 30% out of one coin sink. None of them makes gear from nothing.

**Not in the trees: experience.** No "+5% Mining experience" node. Experience already has several multipliers
(village huts, rested, the stream's own), and a node everyone must take is not a choice.

## 7. What the quests and the stream's story count

The trees add resources, not progress in quests: the counters count what a character did by hand.

- **A round** (`QuestTracker.Gathered`): one a round of ten seconds, whatever Tireless or the capstone made of it.
  "Gather for 100 rounds" stays a measure of time.
- **A find the stream's story wants delivered** (`QuestTracker.ItemFound`): the story is asked about the find, once.
  When it takes the find, nothing more comes of it. Bountiful's (and a pet's Forage's) second one goes to the bag
  without being offered. Tireless's second try at a find is a find of its own, and is offered.
- **Things made** (`QuestTracker.Made`): one for each make that worked. Second Helping's extra meal is not counted;
  a make that Thrifty paid for is.
- The captain's cut (a guest's catch at sea may bring the boat's owner one too) is asked once a round, whatever the
  guest's tree made of the round.

## 8. Built (2026-11-22)

- **Library** (RavenNest.Combat): `SkillTree.Gathering` and `SkillTree.Artisan`; the nine trade skills in
  `CombatSkill` (the enum keeps its name; a planner's list of levels is in its order, new ones at the end);
  `SkillTrees.TradePoints`, `EarnedPoints(levelOf)` and the pool rule in `SkillPoints`; the gate
  (`SkillsOf`, `GateLevel`, `GateName`, `GateText`, `SkillNode.Skill`, `SkillBuild.RankIn`); the nodes with what
  they turn (`TradeEffect`, `SkillNode.TradeAmount` and `TradeText`) and their paths; `TradeSkills` (new file) with
  what a build is worth in a skill at a level (`Gathering`, `Making`, `CombineFee`, `EnchantWaitFactor`, the find's
  chance and wait); `EggSource.Gathering` and its table. Tests for the points table, the pool, the gate, each
  effect's numbers, the eggs, Gem Salvager's move, old shared links.
- **Server**: `SkillTreeManager` counts the trade levels and keeps each character's parsed build until its text
  changes (`BuildOf`: the rounds ask every ten seconds). A gathering round goes through
  `ResourceTaskProcessor.GatherRound` for all five skills (Village's Share, Tireless, the capstone, Nest Finder);
  the find takes Sure Hands, Bountiful and Quick Return in `SimpleDropHandler`; making takes Steady Hands, Thrifty
  and Second Helping in `PlayerManager.ProduceItem`; the combine fee takes Gem Cutter (`GemManager`), the enchant
  wait Enchanter's Patience (`EnchantmentManager`). Nest Finder's egg comes with the play streak's event with no
  days, so no new packet.
- **Game**: `!tree gathering` and `!tree artisan` (or any of their skills: `!tree mining`, `!tree cooking`),
  `!tree bountiful`, `!learn`, `!respec` through the same commands; `!tree` says where the points come from
  (combat levels, Taming, trade skills); the details window's stats view has a Skill trees group (points to spend,
  the three sources, the trade skills learned); "came across a Chick Egg while gathering" in chat. No combat code
  changed. The texts are in `Localization.cs` and the language template.
- **Website**: Combat | Trade on the map (the planner at `/skill-trees` and the character page's Skill trees tab).
  Combat is the wheel of nine as it was. Trade shows the two trees as two fans from the same middle, side by side
  on a wide screen and one above the other on a phone, with the nine trade levels and their points where the
  loadout is. Levels for all eighteen skills in the planner, a shared link carries eighteen levels and the tab
  (`tab=trade`); a link with eight or nine levels still opens, its missing trade levels at 1. The combat guide's
  text about points and trees. Every trade node has a drawn icon (one sheet of sixteen sigils, `skill-sheet-e`).
- **Older games**: every effect is the server's, so a 0.9.6 game gets them too once nodes are learned on the
  website. Such a game keeps its own count of ingredients after a make, which is off by what Thrifty kept until it
  next reads the inventory; a 0.9.7 game shows the server's numbers.

## 9. Tested (2026-11-22)

**Library:** 143 tests pass, ten of them new: the points table of section 3, the one pool (405 at the most, the
combat and Taming numbers unchanged), a planner's eighteen levels and links with eight or nine, the two trees'
shape and prices, the gate (which skill opens a node and where it counts), each Gathering and Artisan number, the
egg table, Gem Salvager's move with an old build, and the texts.

**Played** on the local server with the development character (zerratar: Woodcutting 999, Mining 847, Fishing 711,
Farming 700, Gathering 737, Crafting 773, Cooking 704, Alchemy 726, Sailing 727, Taming 76), through the real chat
handlers with the game cut off from the bot:

- **Points:** 252 before (245 from combat levels, 7 from Taming), **335 now** (83 more from 6,924 trade levels), with
  no migration. `!tree` says so: "335 of 335 skill points to spend (245 from combat levels, 7 from Taming, 83 from
  trade skills like Mining and Cooking...)".
- **Chat:** `!tree gathering`, `!tree artisan`, `!tree mining` and `!tree cooking` (the tree of that skill), `!tree
  bountiful` and six other nodes, `!tree second wind` (still Vitality's active), `!tree master` ("could mean Master
  of the Trade or Master Artisan"), an unknown name (the answer lists the two trade trees). `!learn` for 42 ranks in
  eleven nodes, and the refusals: "Master Artisan needs Crafting, Cooking or Alchemy 999.", "Enchanter's Patience
  needs Gem Salvager or Gem Cutter first.", "Quick Return needs Bountiful or Tireless first.". `!respec` named the
  price (a Tome of Forgetting) and `!respec confirm` gave all 119 points back.
- **Gathering, counted.** For the test the server's round was one second long (a temporary hook, not committed), so
  minutes were hundreds of rounds at the real odds. The village's count is read between two of its pushes to the game.

| Phase | Village, a second | Finds | Two at once | Seconds between two finds of the item aimed at |
|---|---|---|---|---|
| Farming 700, no nodes (215 s) | 0.927 wheat | 63 | 0 | Coriander (wait 90 s): 91.0 |
| + Village's Share 5/5 (160 s) | 1.172 (x1.26; 1.30 expected) | 44 | 0 | 91.7 |
| + Sure Hands, Bountiful, Tireless, Quick Return 5/5 (300 s) | 1.283 (x1.38; 1.43 expected) | 95 | 10 (10.5%) | 83.6, 86.4, 82.0 (wait 81 s) |
| Woodcutting 999, + Nest Finder and Master of the Trade (170 s) | 2.598 wood (2.80 a round; 2.76 expected) | 43 | 6 | Logs (wait 10 s, 9 with Quick Return): from 9.3 |
| Farming 700 again, the same build (100 s) | 1.350 (the capstone does not count at 700) | 35 | 4 | - |

  Sure Hands could not show on this character: at Farming 700 every find it can make is already certain.
- **Nest Finder:** with its chance forced for three and a half seconds the character got four eggs, three Kitten Eggs
  and a Piglet Egg, all from the gathering table. The chat line reads "zerratar came across a Chick Egg while
  gathering (Nest Finder)! !pets hatch puts it in the stable to warm.".
- **Cooking Kraken** (Cooking 700, 20% at its own level; 23.7% expected at 704), 300 at a time through the server's
  own call:

| Batch of 300 | Makes that worked | Extra meals | Makes that used no fish |
|---|---|---|---|
| no nodes | 69 (23.0%) | 0 | 0 |
| Steady Hands 5/5 | 105 (35.0%; 38.7% expected) | 0 | 0 |
| + Thrifty and Second Helping 5/5 | 119 (39.7%) | 9 | 28 |
| the same again | 114 (38.0%) | 10 | 29 |

  `!cook 3 kraken` went through chat. With no Raw Kraken left and Thrifty at 5/5, 200 asked made nothing, and
  `!cook kraken` answered "You do not have enough resources for creating Kraken. You need Raw Kraken 0/1".
- **Gem Cutter:** three Chipped Rubies into a Flawed Ruby for 1,000 coins without it, 880 at 2/5, 700 at 5/5; the
  coins fell by exactly that.
- **Gem Salvager after the move:** learned in Artisan with Crafting 773 (after Thrifty); `!unsocket footpad mask`
  showed a 4% chance to keep the gem before, 19% at 3/3, and 4% again after the respec. No gem was taken out.
- **Website** (screenshots at desktop, tablet and phone widths, signed out): the planner on both tabs, a link with
  eighteen levels and a build, a link from before the trade trees (nine levels: 281 points, "Trade 0"), the
  character's page with the build above learned (the lit Gathering fan, 265 of 335 points left), its list view, a
  skill's panel in the trade view, the levels panel with its two groups, and the locked fifth loadout slot (hover,
  click, tap).

**Put back afterwards:** the points by the respec; the Tome of Forgetting it used, the test's fish, meals, rubies,
eggs and gathered goods, and the 2,580 coins the combining cost, in the development database with the server
stopped. The character wears its Elder Atlarus set as before. Not put back: a few hundred wheat and wood in the
streamer's village, and the minutes of Farming, Woodcutting and Cooking experience.

**Not verified:**
- Enchanter's Patience in play (the factor is tested; no enchant was made).
- Second Helping with Alchemy, Thrifty with Crafting, Steady Hands on any recipe but Kraken.
- Sure Hands in play (needs a character near a find's own level); Mining, Fishing and Gathering rounds (they run
  through the same `GatherRound` as the two that were played); the captain's cut with a doubled round.
- Nest Finder at its real odds, and its line from real chat (the bot was cut off, so the game's announcement of the
  server's event went nowhere; the line above was put through the reply formatting by hand).
- The quests' and the story's counters (section 7): by reading the code, not by finishing a quest.
- A build that learned Gem Salvager in Hunter: by test in the library; there was none in the development database
  to load (the test character's build was empty).
- The website signed in (Learn, respec and the planner's Save on the character page); an older game.

## 10. Calls made while building, for the owner to change

- **"Second Wind" is called Tireless**, because Vitality's active has the name.
- **Paths:** the proposal named none. They are in the tables of section 4. The capstones also hang from the nodes
  they make better (Tireless; Thrifty and Second Helping), so Master Artisan can be had by a cook with no Crafting.
- **Gem Cutter needs Crafting 300** like Gem Salvager (the proposal said so only for Gem Salvager and Enchanter's
  Patience). The other Artisan nodes open with any of the three skills.
- **A node counts by the skill's own level**, without the levels gear adds, the same level that lets it be learned.
- **The egg table:** Chick 10, Kitten 8, Piglet 6. Nest Finder rolls once a round, also when Tireless doubles it.
- **Counters** (section 7): a doubled round is one round for quests, an extra find or meal is not counted.
- **The egg's chat line** uses the play streak's event with no days, to save a packet. A 0.9.7 test build from
  before today would say "has played 0 days in a row".
- **No `!points` command and no per-tab respec.** `!tree` shows the points, as before; one pool means one respec
  gives everything back. The bot's help texts for `!tree` and `!learn` still name only the combat trees.
- **The game has no tree window**, so the Trade tab is the website's. In the game: chat, and a Skill trees group in
  the details window's stats (points left, the three sources, and the trade skills that count in what the character
  trains at that moment).
- **On a phone the trade view shows the trade levels as one line** ("Trade skills: 6,924 levels earn 83 points"), to
  leave the map its height; the nine levels are in the planner's Levels.
- **Colors:** Gathering leaf green, Artisan forge amber. One picture was generated (a sheet of sixteen sigils).
- **`SkillTrees.Version` stays 1:** no free respec for the move of Gem Salvager, the trees are unreleased.
- **Steady Hands also counts for recipes with a fixed chance** that can fail (there are none today that are not
  certain).
