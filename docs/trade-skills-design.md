# Trade skills: a second tab of skill trees for gathering and making

**Status: proposal, not approved, nothing built** (written 2026-11-22). The combat trees and Beastmaster are in
`combat-skills-design.md`; this is the other tab the owner asked about on 2026-11-21 ("2 skill tree tabs, combat
related and another for the rest"), with numbers. Every number here is a starting point and every "decide" at the
end is the owner's.

## 1. Why

- **Nine skills earn nothing in the trees today.** Skill points come from the eight combat skills and Taming.
  Woodcutting, Mining, Fishing, Farming, Gathering, Crafting, Cooking, Alchemy and Sailing give none, and there
  is nothing to spend them on.
- **A lot of players live there.** Of 3,151 characters used on production from 22 to 24 October 2026 (the three
  days before the development copy was taken, the only clean sample at hand), **717 (23%) have more levels in
  those nine skills than in all combat skills together.**
- The design document already promised a small "Artisan tree" for things like keeping a gem. It was never built
  as a tree: its one node, Gem Salvager, sits in Hunter (Slayer) and costs combat points.

| Of the 3,151 characters | level 100+ | 300+ | 600+ | 999 |
|---|---|---|---|---|
| best gathering skill | 464 | 193 | 81 | 27 |
| best of Crafting, Cooking, Alchemy | 425 | 157 | 54 | 14 |
| Sailing | 151 | 46 | 27 | 11 |

## 2. The shape

- **A second tab, "Trade", beside "Combat"** in the game's tree window, on the website's planner and on the
  character page. Same look, same commands (`!tree gathering`, `!learn bountiful`, `!points`).
- **Its own pool of points,** earned from the nine trade skills and spent only in the trade trees. Combat points
  stay as they are: nobody's build changes, and fighting never costs gathering or the other way round.
- **No actives and no loadout.** Every node is a passive, rolled by the server where the drop, the craft or the
  gem is already rolled. So the game needs the tab and the texts, and little else (section 7).
- **Two trees to start, a third later:** Gathering (five skills) and Artisan (three). Seafaring (Sailing) is
  sketched in section 5 and is too thin for a tree yet; Sailing levels earn trade points from the first day.
- **A node works in each of its tree's skills that is at the node's level.** Bountiful (level 100) can be
  learned once one gathering skill is 100, and then works while mining if Mining is 100, while fishing if
  Fishing is 100, and so on. Tiers are the combat trees' own: 1, 100, 300, 600, 999. Costs too: 1 to 5 points a
  rank by tier, 15 for a capstone.
- **Beastmaster stays on the combat tab.** Keen Senses (a gatherer pet's skills) stays there too: it is a point
  in the pet.

### Why its own pool and not the combat points

One pool sounds simpler, but it sets the two against each other. A fighter with 300 points would drop 45 into
gathering without feeling it, and a gatherer with no combat levels would still have nothing. With a pool of its
own the gatherer's levels are what pay, which was the reason to do this.

## 3. Points

**One point per 10 trade levels (all nine added up) up to 400, then one per 150.**

`TradePoints(total) = floor(min(total, 400) / 10 + max(0, total - 400) / 150)`

Fast at the start so a new player has something to pick within the first hour, slow afterwards so that being
high in everything does not buy every node. Worked out from levels, like the combat points: nothing to migrate.

| Who | Trade levels | Trade points |
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

Against what there is to buy: Gathering costs 70 and Artisan 84, **154 together**, about 205 with a Seafaring
tree. A character with everything at 999 has 97, so 63% of two trees and under half of three. One skill at 999
with nothing else (44 points) pays for the five Gathering nodes below level 600 less one rank; the level 600 node
and the capstone want more levels elsewhere. For comparison the combat side gives 308 points against 812.

## 4. The two trees

How gathering works today, because the numbers lean on it (server, `ResourceTaskProcessor` and
`SimpleDropHandler`): every 10 seconds of gathering is a **round**. Each round the village gets one of its
resource. One round in two also tries for a **find**: the item aimed at first (`!mine rune ore`), with a chance of
its base (mostly 20%) plus 0.5% per level above what it asks for, then the others. Each find then has a **wait**
before the same character can find it again: 10 seconds for Copper Ore, six hours for Atlarus Ore.

So common things are held back by rounds, rare things by their wait. That difference decides what a node does to
the economy (section 6).

### Gathering (Woodcutting, Mining, Fishing, Farming, Gathering): 7 nodes, 70 points

| Level | Node | Ranks x cost | What it does |
|---|---|---|---|
| 1 | Sure Hands | 5 x 1 | +2% chance on every find per rank (10%: what 20 levels give). Helps most with things at your own level; does nothing once the chance is 100% |
| 1 | Village's Share | 5 x 1 | 6% chance per rank that a round gives the village one more of its resource (30%) |
| 100 | Bountiful | 5 x 2 | The "double gather": 2% chance per rank that a find is two (10%) |
| 100 | Second Wind | 5 x 2 | The safe kind of gathering speed: 2% chance per rank that a round counts twice (10%). One more for the village and one more try at a find; waits still count |
| 300 | Quick Return | 5 x 3 | The other kind of speed: every find's wait is 2% shorter per rank (10%) |
| 600 | Nest Finder | 1 x 10 | A small chance of a pet egg while gathering, about one in 20 hours (1 in 7,200 rounds), from the quests' egg table. Today eggs only come from fighting and quests, so a gatherer has no road to a gatherer pet but taming |
| 999 | Master of the Trade | 1 x 15 | In a gathering skill at 999: every round counts twice for the village, and Second Wind's chance is doubled (20%) |

### Artisan (Crafting, Cooking, Alchemy): 7 nodes, 84 points

Making is instant (one command makes any amount), so there is no speed to sell here. What there is: 34 of the 49
cooking recipes can fail (20 to 50% success at the recipe's own level, 100% about 185 levels above it), crafting
and alchemy never fail, gems have a keep chance and combining fees, enchanting has a wait.

| Level | Node | Ranks x cost | What it does |
|---|---|---|---|
| 1 | Steady Hands | 5 x 1 | +3% success per rank where a recipe can fail (15%). A recipe that starts at 20% starts at 35% and is safe 115 levels above it in place of 185. Cooking only, as things are |
| 100 | Thrifty | 5 x 2 | 2% chance per rank that making something uses up no ingredients (10%) |
| 100 | Second Helping | 5 x 2 | Cooking and Alchemy: 2% chance per rank of one more meal or potion (10%). Not for crafted gear |
| 300 | Gem Salvager | 3 x 3 | Moves here from Hunter, unchanged: +5% chance per rank to keep a gem taken out of its socket (55% at most with the clan's part). Needs Crafting 300 in place of Slayer 300 |
| 300 | Gem Cutter | 5 x 3 | Combining gems costs 6% less per rank (30%): 1,000 / 5,000 / 25,000 / 125,000 coins become 700 / 3,500 / 17,500 / 87,500 |
| 600 | Enchanter's Patience | 5 x 4 | The wait between two enchants is 3% shorter per rank (15%). Needs Crafting 600 (Enchanting itself is the clan's skill) |
| 999 | Master Artisan | 1 x 15 | In a skill at 999: Thrifty and Second Helping are worth 5% more there (15%) |

Hunter loses Gem Salvager and costs 63 in place of 72. Nobody has to be paid back: the trees are not released.

## 5. Seafaring (Sailing): a sketch, not a tree yet

What Sailing has that a node could turn: the captain's cut (a guest's fish at sea gives the boat's owner a 20%
chance of one too), the sea's own fish, the regatta, the ferry, boat building. Candidates:

- **Captain's Cut:** +2% per rank on that chance (20% to 30%).
- **Deep Water:** Bountiful's chance is worth half as much again at sea.
- **Good Host:** guests aboard your boat get a share more experience while they fish there.
- **Shipwright's Friend:** boat blueprints and looks cost less.

Four nodes are not a tree and two of them need the boat numbers looked at first. Recommendation: build Gathering
and Artisan, let Sailing earn points from the start, and add Seafaring when it has seven nodes worth choosing
between.

## 6. What it does to supply and prices

This is the part to decide with open eyes: more per swing is more ore on the market.

**One player with every Gathering node, three cases:**

| Case | Today | With the tree | Change | Which nodes |
|---|---|---|---|---|
| Common: Copper Ore at Mining 200+ (wait 10 s, chance 100%) | 180 an hour | 208 an hour | +16% | Second Wind, Bountiful |
| At your level: Rune Ore at Mining 110 (chance 20%, wait 250 s) | 10.3 an hour | 13.9 an hour | +35% | all four |
| The rarest: Atlarus Ore at Mining 999 (wait 6 h) | 3.98 a day | 4.87 a day | +22% | Quick Return, Bountiful |

(Copper's wait is as long as a round, so half of Second Wind's second tries land inside it and bring another ore
in its place: all finds together are up 21%.)

With a gatherer pet out (Forage: 5% for one more today, 6.5% with Keen Senses) the rarest goes to 5.16 a day,
+30% on today's 3.98 without a pet.

**The dial for the rarest things** is Bountiful's and Quick Return's last rank, and nothing else touches them:

| Bountiful / Quick Return at full rank | The rarest finds, per player with both |
|---|---|
| 5% / 5% | +10.5% |
| **10% / 10% (proposed)** | **+22%** |
| 15% / 15% | +35% |

**How many players that is.** Of the 3,151 characters, 21 can mine Atlarus Ore (Mining 999) and 34 Ancient Ore
(850). The top of the other skills is nearly empty: 3 characters can cut Voidheart Logs (950), 1 can catch a
Leviathan (900), 4 can farm Truffles (800). So the market this moves is mostly ore, and a 999 miner reaches both
nodes with the points one maxed skill gives (25 of 44). Expect about a fifth more top ore from the players who
mine it, once they have spent the points.

**What I kept out of the rarest things on purpose:** Second Wind (more rounds) and Sure Hands (more chance) do
almost nothing to a find with a six hour wait, and the capstone is village resources and Second Wind only. An
earlier draft had a "next find ignores its wait" burst: for Atlarus Ore that is twelve times the supply, so it is
not here.

**Artisan:** Thrifty takes up to 10% (15% at 999) out of what making things uses up, Second Helping adds as much
food and potions. Gem Cutter takes up to 30% out of one coin sink. None of them makes gear from nothing.

**Not in the trees: experience.** No "+5% Mining experience" node. Experience already has several multipliers
(village huts, rested, the stream's own), and a node everyone must take is not a choice. If you want one anyway it
fits at level 1 of each tree, and I would keep it to 1% a rank.

## 7. What building it touches

- **Combat library** (`SkillTrees.cs`): two trees, a pool on each tree, `TradePoints`, the "works in each skill at
  its level" rule, Gem Salvager's move. Tests beside the existing ones.
- **Server:** the saved build keeps one list of nodes; spending checks each pool. Effects in the places that
  already roll: `SimpleDropHandler` (Sure Hands, Bountiful, Quick Return, where the pet's Forage and Keen Nose
  already sit), `ResourceTaskProcessor` (Village's Share, Second Wind, Nest Finder), `PlayerManager.ProduceItem`
  (Steady Hands, Thrifty, Second Helping), `GemManager` (Gem Cutter), `EnchantmentManager` (the wait).
- **Game:** the tab in the tree window, `!tree` and `!points` answers, the texts and the language template. No
  combat code, no new packets beyond what the trees send today.
- **Website:** the planner's tab and a longer level list in shared links (old links leave the new skills out),
  the character page, the guide.
- **Respec:** as today, 1,000 coins a point, per tab (`!respec trade`); a Tome of Forgetting resets one tab.
- **Pictures:** two tree emblems and about fourteen node icons, one or two drawn sheets cut by the icon script.
- **Older games:** every effect is the server's, so a 0.9.6 game gets them too; only the tab is missing there.

Because nothing here runs in a fight, it is a good deal smaller than Beastmaster was.

## 8. To decide

1. **Own pool** (proposed) or the combat points.
2. **The rate:** one per 10 to 400, then one per 150 (97 at most), or more generous.
3. **The supply dial** (section 6): 10% / 10%, or 5% / 5% to be careful, and whether finds with a wait of an hour
   or more should be left out of Bountiful and Quick Return altogether.
4. **Nest Finder:** eggs from gathering, yes or no, and how often.
5. **No experience nodes** (proposed), or small ones.
6. **Seafaring later** (proposed) or a thin tree now.
7. **Gem Salvager moves** to Artisan (proposed) or stays in Hunter.
