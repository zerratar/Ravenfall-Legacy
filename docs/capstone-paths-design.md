# Capstone paths: three endings for each tree, pick one

**Status: built 2026-11-22, on master and the local server only (nothing deployed).** The owner approved the draft
on 2026-11-22 ("build the three capstone paths too"). The idea is the owner's: "each tree could have multiple ending
skills at 999 but only one can be picked per skill tree, like lets say attack skill could offer 3 different ones but
you can only pick one of them". The trees themselves are in `combat-skills-design.md`. What follows is the design as
built: the numbers are the simulator's, not the draft's, and "Built", "Tested" and "Calls made" at the end say what
exists, what was played and what the owner may want to change.

## The rule

- Each of the nine combat-side trees ends in **three capstones** at level 999 in place of one. **Only one of a
  tree's three can be learned.** The one each tree had stays as the first of its three, unchanged (same id, stats,
  price and path), so a build made before this keeps working.
- A capstone costs 15 points, as before. To change path, respec (the usual rules and price) and learn another. In
  the planner, Take back on the chosen one frees the choice.
- **Each new capstone asks for all the nodes of its own theme** (the old ones ask for one of their tree's level 600
  nodes and keep doing so), so a path is something you build toward and not a pick on the last day. The reply to
  `!learn` names what is still missing.
- A second capstone of the same tree is refused with the one chosen by name: "You chose First Blood as your
  Precision ending, and a tree has only one. A respec gives the choice back."
- A build text or a shared planner link with two capstones of one tree keeps the first and drops the rest; nothing
  crashes.
- The trees' total cost counts one capstone a tree (957 points, as before the paths), since nobody can buy three.
- Gathering and Artisan keep one capstone each.
- The paths line up with the class gear lines on purpose (rogue, barbarian, paladin, dark knight, witch hunter,
  monk, samurai), so a path has an outfit and `!equip rogue` has a build to go with.

In chat: `!learn assassin` or `!learn first blood` (the path's name or the capstone's), `!tree precision` lists the
three with the one chosen, `!tree assassin` explains one. On the website a wedge ends in three crowns side by side;
the chosen one is lit, the other two are dim with "You chose (name)" in their panel.

## The 27 endings

The first of each three is the old capstone. "Asks for": the old ones need one of the nodes named, the new ones all
of them.

### Precision (Attack)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Duelist | **Perfect Form** (unchanged) | +4% crit chance, +20% crit damage | Disarm or Precision Strikes |
| Assassin, the rogue | **First Blood** | Your first hit on a new target is a critical hit that ignores 25% of its armor, once every 12 s. Your critical hits poison: 4% of the hit a second for 4 s (it does not stack) | Sharp Edge and Precision Strikes |
| Blade Dancer, the samurai | **Flowing Steel** | +5% attack speed. Every hit in a row on the same target adds 1% more, up to 5%; gone when you switch target or rest 6 s | Swift and Flurry |

### Might (Strength)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Colossus | **Colossus** (unchanged) | +12% melee power, +10% damage to enemies below 30% | Leap or Titan |
| Berserker, the barbarian | **Blood Rage** | Up to +25% melee damage as your health falls (all of it at 30% health and below, half of it at 65%). 3% of your damage heals you | Brute Force and Rend |
| Earthshaker | **Aftershock** | Every fourth melee hit strikes every other enemy within 4 m of you for 70% of its damage. Your stuns, freezes and roots last 20% longer | Cleave and Whirlwind |

### Bulwark (Defense)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Unbreakable | **Unbreakable** (unchanged) | 6% less damage from everything, +25% threat | Rally or Last Stand |
| Guardian | **Shield of the Many** | You take 15% of the damage enemies deal to allies within 10 m in their place, after your own armor, while you are above 30% health | Rally and Thick Skin |
| Avenger, the dark knight | **Retribution** | +10% thorns. After you block, your next hit deals 60% more | Spiked Armor and Shield Training |

### Restoration (Healing)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Miracle Worker | **Miracle Worker** (unchanged) | +15% healing power, heals give a 10% shield | Sanctuary or Smite |
| Crusader, the paladin | **Holy Fire** | Every heal you cast also strikes an enemy near the one healed for 20% of the amount healed (armor takes half its usual cut). +20% damage to the undead | Smite and Devotion |
| Lifebinder, the monk | **Guardian Angel** | Once a minute, an ally within 10 m who would die to an enemy is left at 1 health and healed for 40% of theirs. +10% cast speed | Barrier and Quick Hands |

### Marksman (Ranged)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Deadeye | **Deadeye** (unchanged) | +12% ranged power, 15% armor penetration | Snare or Rapid Fire |
| Sniper | **Clean Kill** | +50% damage to enemies above 80% health, +30% crit damage | Eagle Eye and Piercing Shot |
| Stormbow, the witch hunter | **Volley** | Every third shot also hits a second enemy within 8 m of the target for the shot's full damage. +10% attack speed | Multishot and Hunter's Pace |

### Elements (Magic)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Archmage | **Archmage** (unchanged) | +12% magic power, +8% cast speed | Meteor |
| Pyromancer | **Wildfire** | Your burns are 150% stronger, and when a burning enemy dies its burn jumps to an enemy within 6 m, at full strength for another 4 s | Searing and Fireball |
| Stormcaller | **Eye of the Storm** | Lightning that jumps hits 30% harder on every jump (Chain Lightning's jumps too), and your cooldowns are 10% shorter | Chain Lightning and Unravel |

### Vitality (Health)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Undying | **Undying** (unchanged) | +10% health, 0.6% of it back a second in fights | Iron Skin |
| Blood Drinker | **Crimson Pact** | +5% life steal, and life steal beyond full health becomes a shield of up to 15% of your health (it lasts 10 s after the last drop) | Bloodthirst and Hardy |
| Ironblood | **Ironblood** | Enemies' chance of a critical hit on you is 10% lower, and you receive 35% more healing | Resilience and Iron Skin |

### Hunter (Slayer)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Legend of the Hunt | **Legend of the Hunt** (unchanged) | +10% damage to bosses, +5% loot luck | Apex Predator |
| Culler | **Thin the Herd** | +8% damage to everything that is not a boss, and an enemy you helped kill gives 10% more damage for 6 s | Execute and Pressure Points |
| Fortune's Favorite | **Fortune's Favorite** | +12% loot luck in raids and dungeons, and nothing else | Treasure Hunter |

### Beastmaster (Taming)

| Path | Capstone | What it does | Asks for |
|---|---|---|---|
| Pack Leader | **Pack Leader** (unchanged) | A packmate of your pet's kind fights beside it at half strength | Elemental Bond, Guardian's Oath or Call of the Wild |
| Alpha | **Alpha** | No packmate: your one pet is a quarter larger, 40% stronger and has 40% more health | Fang and Claw and Guardian's Oath |
| Trailblazer | **Trailblazer** | Your gatherer pet's skills are 50% stronger (added to Keen Senses), and your pet is never knocked out while you gather | Keen Senses and Back on Its Feet |

## What the simulator says

`RavenNest.CombatSimulator paths [runs] [tree]` (RavenNest, `src/tools/RavenNest.CombatSimulator/PathScenarios.cs`).
A level 999 player in the standard kit for its style has every node of the tree below the capstones (so every
ending's "asks for" is met) and its actives in the loadout; the same fight runs with no ending and with each of the
three, and an ending's worth is what it adds. 300 s a fight, 1,000 runs. `paths 200 might aftershock=AFTERSHOCK:80%`
tries other numbers without a rebuild.

The fights: **one**, enemies of the player's level one after another (island enemies: three quarters of the kit's
armor; the next arrives 2 s after a kill); **pack**, four at once; **boss**, one boss whose pool lasts about 90 s.
In the damage tables the player's health is held at 90% (hurt: 50%): a fighter alone only loses health until it
falls, which says nothing about how hurt players are in a real fight.

Damage dealt as a multiple of the same build with no ending; in brackets how far behind the tree's best:

| Tree | Ending | one | pack | boss |
|---|---|---|---|---|
| Precision | Perfect Form | 1.037 (-9%) | 1.040 (-9%) | 1.051 (-3%) |
| | First Blood | **1.142** | **1.148** | 1.062 (-3%) |
| | Flowing Steel | 1.035 (-9%) | 1.052 (-8%) | **1.089** |
| Might | Colossus | **1.060** | 1.064 (-10%) | **1.102** |
| | Blood Rage, at 90% health | 1.024 (-3%) | 1.024 (-13%) | 1.034 (-6%) |
| | Blood Rage, at 50% health | | 1.132 | **1.179** |
| | Aftershock | 1.001 (-6%) | **1.177** | 0.999 (-9%) |
| Marksman | Deadeye | 1.152 (-2%) | 1.132 (-7%) | **1.236** |
| | Clean Kill | **1.177** | 1.134 (-7%) | 1.117 (-10%) |
| | Volley | 1.057 (-10%) | **1.213** | 1.069 (-14%) |
| Elements | Archmage | **1.099** | 1.100 (-5%) | **1.154** |
| | Wildfire | 0.999 (-9%) | 1.137 (-2%) | 1.060 (-8%) |
| | Eye of the Storm | 1.021 (-7%) | **1.162** | 1.037 (-10%) |
| Hunter | Legend of the Hunt | 1.000 | 1.000 | **1.085** |
| | Thin the Herd | **1.113** | **1.125** | 1.001 |
| | Fortune's Favorite | 1.000 | 1.000 | 1.000 |

(At 50% health Colossus is 1.067 in the pack and 1.106 on the boss, Aftershock 1.180 and 1.002.)

**Vitality** (staying alive; a melee fighter with all of Might, 600 s; how long a life lasts, as a multiple of no
ending): alone against one enemy at a time hitting at full power, Undying 1.93, Crimson Pact 1.91, Ironblood 1.77;
against three at once with a healer who has two others to look after (a heal every 4.5 s), Undying 1.89, Ironblood
1.65, Crimson Pact 1.37.

**Bulwark** (the tank beside one ally, a pack hitting at full power, the tank held at 70% health; damage taken by
both, as a multiple of no ending): Unbreakable 0.955, Shield of the Many 0.939, Retribution 0.992 with 1.17 times the
tank's own damage.

**Restoration** (a healer behind a tank that is always hurt; output a minute): Miracle Worker 1.197 times the healing
and shields; Guardian Angel 1.068 times, and one save a minute worth 40% of the saved player's health; Holy Fire 1.000
times, and damage worth 44% of a melee fighter's with all of Might.

**Beastmaster** is not simulated (the `pets` command fights one pet). A pet's blows and heals scale with its power:
Pack Leader is 1.5 times the pet's damage and healing with the pet's own health unchanged; Alpha is 1.4 times both
its damage and its health; Trailblazer is nothing in a fight.

### What was tuned, and why

- **First Blood** was the draft's big outlier: an enemy of a fighter's own level falls in four or five blows, so
  "the first hit on every new target crits through half its armor" made every fourth blow that hit, 1.77 times the
  damage on enemies one after another and 2.08 times in a pack where the tree's other endings give 1.04. Now the
  first hit waits 12 s, ignores a quarter of the armor, and the poison is 4% a second: 1.14.
- **Flowing Steel** 8% and 2% a hit to 5% and 1%: it was 1.16 on a boss beside Perfect Form's 1.05.
- **Blood Rage** 30% to 25%: at half health it was 1.20 on a boss beside Colossus's 1.11.
- **Aftershock** 50% to 70%: it gives nothing on one enemy, so in a pack it has to lead clearly (1.12 before).
- **Clean Kill** 25% and 20% crit damage to 50% and 30%; **Volley** 60% and 6% speed to the whole shot and 10%:
  Deadeye was best at everything (1.15, 1.12, 1.23 against 1.10, 1.07, 1.07 and 1.04, 1.13, 1.03).
- **Wildfire** 40% to 150% stronger burns: at 40% it was 1.02 on one enemy, 1.06 in a pack, 1.04 on a boss.
- **Crimson Pact** 4% and a 10% shield to 5% and 15%; **Ironblood** 15% to 35% more healing received: alone they
  were 1.61 and 1.27 beside Undying's 1.84.
- **Thin the Herd** 12% to 8%: it was 1.15 on everything but bosses beside Legend of the Hunt's 1.07 on bosses.
- **Holy Fire** 30% to 20%, and armor takes half its usual cut of it (as of an element): at 30% past all armor it
  was more damage than a fighter deals, from a healer still healing at full strength.
- **Guardian Angel** 30% and 8% cast speed to 40% and 10%: its healing was 1.04 times beside Miracle Worker's 1.20.
- **Alpha** 30% to 40%: beside Pack Leader's 1.5 times the pet's damage.

### Where an ending is behind, and what the simulator cannot say

The aim was "no tree has one plainly best ending for everything, within about ten percent on the main job". Where it
is not met, or cannot be judged:

- Precision: Perfect Form is never the best (3% to 9% behind); it is the one that grows with crit gear and gems.
- Might: Aftershock is 9% behind on a boss and worth nothing on a single target; Blood Rage is behind while
  healthy and ahead while hurt, so its worth depends on how hurt players really are, which the simulator does not
  know (the tables name the health they are for).
- Marksman: Volley is 14% behind on a boss, Clean Kill 10%.
- Vitality: Crimson Pact is 28% behind Undying in the healed pack fight (life steal scales with damage dealt, not
  with damage taken).
- Hunter, Restoration and Bulwark do different jobs by design (bosses, trash or loot; healing, a save or damage;
  less damage for the group, a share of it, or damage back), so their endings never stand in one column.
- Not modelled: positions (everything is in reach, so Aftershock's 4 m and Volley's 8 m always hold), Taunt, more
  than one ally (Shield of the Many grows with the group, Unbreakable does not), the undead (Holy Fire's bonus),
  resistances, pets, loot (Fortune's Favorite), gathering (Trailblazer), and the price of a fall that Guardian
  Angel prevents.

## More loot and more finds

- **Loot luck was a dead stat.** Treasure Hunter and Legend of the Hunt showed "more loot in raids and dungeons"
  and nothing rolled it. It is rolled now (RavenNest `LootLuck`, in the raid and dungeon rewards): the chance of the
  item times 1 + luck, and where the item is already sure (a dungeon's often is), a chance of a second one. So 20%
  luck is 20% more items on average. The most that counts is 50%. Treasure Hunter's five ranks, Legend of the Hunt
  and Fortune's Favorite are 10%, 5% and 12%: 22% at most for one character. Seasonal token rolls are not touched.
- Trailblazer adds 50% to a gatherer pet's skills (Forage 5% becomes 7.5% without Keen Senses, 9% with its three
  ranks). Both belong on the same list as the supply numbers in `trade-skills-design.md`.

## Built (2026-11-22)

**Library** (RavenNest `src/RavenNest.Combat`):
- `SkillTrees.cs`: `SkillNode.Path` and `NeedsAll`, the 18 endings (`Ending(...)`), `SkillTrees.Capstones(tree)`,
  `SkillBuild.CapstoneOf(tree)`, the refusal in `CanRaise`, `CanLower` holding every node an ending asks for,
  `Clean` keeping the first of two capstones, `TotalCost` counting one a tree, `Matches` finding a capstone by its
  path, words for every new stat.
- `GearStats.cs`, `CombatSheet.cs`: 17 stats for the rules (FIRST_STRIKE, POISON_ON_CRIT, COMBO_SPEED, RAGE_POWER,
  AFTERSHOCK, ALLY_GUARD, BLOCK_RETORT, HEAL_STRIKE, GUARDIAN_ANGEL, HEALTHY_DAMAGE, VOLLEY, BURN_SPREAD,
  LIFE_STEAL_SHIELD, NON_BOSS_DAMAGE, KILL_RUSH, PET_ALPHA, PET_SAFE_GATHERING).
- `CombatMath.Attack`: what one blow settles (Clean Kill, Blood Rage, Thin the Herd's damage, poison on a crit, a
  forced crit). `PathRules.cs`: what remembers something between blows (`PathState`: the target, hits in a row, the
  count to the next Aftershock or Volley, a block not yet answered, the first hit's and the angel's wait) and the
  sizes of the rest (a guardian's share, Holy Fire's strike, the overflow shield, the burn's jump).
  `StatusEffects.cs`: poison that does not stack, taking a burn off the dead. `LootLuck.cs`.
- Tests: `CapstonePathsTests.cs` (27 tests: the rule, old builds, links with two capstones, names and paths, each
  ending's math, loot luck); 170 tests pass.
- Simulator: `PathScenarios.cs`, the `paths` command.

**Server**: the rule needs nothing new (it learns through `SkillBuild.Raise`). Loot luck in
`GameManager.GetRaidRewardsAsync` and `GetDungeonRewardsAsync`. Trailblazer's stronger gathering is counted where
Keen Senses already was (`BattlePetManager.GatherBonusOf` reads the build's PET_GATHERING). The server computes no
fights.

**Game**:
- `Core/CombatRules.cs`: every blow goes through `Resolve`, which runs the path rules before and after it;
  `AttackSpeed` adds Flowing Steel's hits in a row.
- `Game/Combat/CombatPaths.cs` (new): the path memory a player, and what the rules set off: Aftershock's burst and
  ring, Volley's second arrow, Shield of the Many's share, Holy Fire's strike, Guardian Angel's save, Wildfire's
  jump, Crimson Pact's shield, Thin the Herd's rush. `CombatPaths.Counts` counts each for tests.
- `CombatEffects.cs` (the crit's poison, life steal through CombatPaths, `TakeBurn`, `DealAs`),
  `CombatSkillRunner.cs` (Chain Lightning's jumps use the jump bonus; skill heals strike with Holy Fire),
  `PlayerCombat.cs` (heals strike; a kill tells CombatPaths), `PlayerController.TakeDamage` (a guardian's share, an
  angel's save), `BattlePetController.cs` (Alpha's size, Trailblazer's pet on its feet while its owner gathers).
- Cues, all from effects the game had: a ground ring for Aftershock and for an angel's save, a small bolt for
  Volley's second arrow, Holy Fire's strike and Wildfire's jump, the words "First Blood", "Retribution", "Guardian
  Angel" and "Burning" over the fighter, and the larger pet. Rings and bolts are off with Weapon effects off, with
  graphics off and in potato mode; the words follow the Effect words setting.
- Chat (`SkillsCommand.cs`, `Localization.cs`): `!tree <tree>` lists the three endings after the skills with the
  one chosen, `!tree <capstone or path>` says whose ending it is and which one this character chose, `!learn` takes
  either name. The longest reply is 445 characters. The language template is re-exported.

**Bot**: one sentence each in the help for `!tree` and `!learn`.

**Website**: the star map shows a wedge's three crowns (the 18 new ones with their own pictures, the old ones with
their tree's emblem), a closed ending dim with a lock, the panel with whose ending it is and "You chose (name)". The
map draws an ending's links only from the highest node it asks for (the panel names them all). On a phone's overview
a side tree's name lies inside its own wedge so no crown is covered; a long crown name is set on two lines; a crown's
name may lie beside it. The list view, the planner's "Endings chosen" and the combat guide's "The trees' endings"
section list them.

**Pictures**: two sheets of nine sigils (`tools/website/skill-icon-sheets/skill-sheet-f.webp` and `-g.webp`), cut by
`tools/website/skill-icon-sheets.py` into the website's `imgs/skills`. Two pictures were generated. The game shows
no skill pictures, so it needed none.

## Tested (2026-11-22)

All on the local server and the development character (Attack, Defense, Strength, Vitality, Healing and Slayer 999,
Ranged 797, Magic 35, Taming 76), with the game cut off from the bot so nothing reached real chat.

**Through the real chat handlers and the server:**
- `!tree precision`, `bulwark`, `vitality`, `beastmaster` list three endings (and `gathering` one capstone);
  `!tree assassin`, `first blood`, `duelist`, `blade dancer`, `shield of the many`, `fortune's favorite`,
  `trailblazer`, `colossus`, `berserker` explain them; `!tree guardian` says it could mean three things.
- `!learn assassin` before the path: "First Blood needs Sharp Edge and Precision Strikes first." After them: "You
  learned First Blood, your Precision ending." Then `!learn perfect form`, `duelist` and `blade dancer` are refused
  with "You chose First Blood...". A respec, then Flowing Steel is learned by `!learn blade dancer` and First Blood
  is refused.
- Three builds were learned this way: First Blood, Shield of the Many and Holy Fire; then Flowing Steel,
  Retribution, Guardian Angel, Aftershock, Crimson Pact and Thin the Herd (Colossus and Fortune's Favorite refused
  beside them).

**In real fights on Ironhill** (its dragons have 920 health and fall to one blow):
- First Blood: 6 first hits in 80 s, each a critical hit that poisoned, 12 to 16 s apart; none of the other 18 hits
  was a crit.
- Holy Fire: heals on bots struck dragons (105 strikes over the session).
- Thin the Herd: a rush after every kill (damage up in 176 of 180 samples). Crimson Pact: the shield filled to its
  cap of 1,555 (15% of 10,369). Aftershock: every fourth melee hit burst (no second dragon within 4 m).
- Flowing Steel: attack speed 1.07 throughout (Swift's 2% and the 5%); every dragon fell to the first hit, so no
  hits in a row.
- Shield of the Many: one real blow from a dragon taunted onto a bot beside the character was shared.

**With blows dealt or rolled by the test** (the game's own `TakeDamage`, `CombatRules.Melee` and `Ranged`; real
sheets and rules, but not a fight that happened by itself):
- Shield of the Many: a blow of 1,000 on a bot 1.5 m away cost the character 33 (its share of 150 after armor); no
  share with the character at 25% health, none with the bot 18 m away.
- Guardian Angel: a bot at 300 health took a blow of 5,000 and stood at 1,611 of 4,025; the same two seconds later
  killed it (the angel waits a minute).
- Retribution: of 240 enemy blows rolled on the character 1 hit and was blocked, and the next hit answered it.
- Flowing Steel: eight blows in a row gave speeds 1.08 to 1.12 and stayed there, another enemy 1.08, six seconds of
  rest 1.07.
- Aftershock with a dragon moved 3 m from the character: it lost 8,025 health to the burst. The ring was captured
  in a screenshot.

**With builds put on the character locally** (the server would refuse them or they were only wanted for a moment;
the server's build was put back after):
- Blood Rage: over 3,000 blows each, 1.13 times at 65% health and 1.25 times at 25%.
- Ironblood: a heal of 1,000 gave 1,390 (with Resilience's 4%).
- Clean Kill: shots at a healthy enemy 1.56 times those at one at half health (300 each).
- Volley: 9 shots that hit, 3 volleys, the second enemy struck.
- Wildfire: a burning dragon fell, the burn (500 a second) was on the dragon 2.4 m away for 4 s.
- Alpha: the Disco Raven's health 870 to 1,262, its model a quarter larger, no packmate; with Pack Leader the
  packmate came back; a build text with both kept the first.
- Trailblazer: knocked out while its owner trained Attack, on its feet 1.5 s after the switch to Woodcutting, and
  the same blow left it standing at 1 health.

**The website** was looked at in screenshots at 1600, 1100, 820 and 390 wide, zoomed out and in, with an ending
chosen and without, the panel of a closed ending, the trade view at 1600 and 390 (unchanged), and the combat guide's
new section.

**The character was left as found**: an empty build, two Tomes of Forgetting, Elder Atlarus, its Phantom Ring, the
Disco Raven out, the Crafting task. The tests used two Tomes and 87,000 coins in respecs; the Tomes were given back
through the game's own item call and the coins in the development database.

**Not verified:**
- Eye of the Storm's harder jumps in play (Chain Lightning needs a staff and Magic 300); only its two stats on the
  sheet were seen. Chain Lightning with a Topaz's jump bonus likewise.
- Fortune's Favorite and loot luck in a real raid or dungeon reward: unit tests only.
- Trailblazer's stronger gathering in a real gathering round: it rides on the code path Keen Senses uses.
- Ironblood's lower crit chance in a fight; Holy Fire against the undead; Retribution against real blows (Ironhill's
  dragons almost never hit this character).
- Shield of the Many and Guardian Angel in a crowd, a raid or a dungeon; any ending in a raid, a dungeon or the
  tower.
- The bolts and the words in a screenshot (only Aftershock's ring was captured); the cues with potato mode on.
- The signed-in character page (its map and its list view): the planner page shows the same map, the list view
  only compiles.
- At 820 wide the Beastmaster tree's name lies under the points panel in the map's corner when the map is zoomed
  out. The map's size and the names' places were not changed for the paths, so this was so before; it is not fixed.
- Real Twitch chat through the bot, and a 0.9.6 game (it has no trees, so nothing should change for it).

## Calls made that the owner may want to change

1. **A new ending asks for all the nodes named**, not one of them (the draft said "asks for the nodes of its
   theme").
2. **First Blood waits 12 s** between first hits and ignores 25% of the armor (the draft: every new target, 50%).
   Without the wait no number brought it near the tree's other endings.
3. **A target is "new" again after 6 s without a blow struck**, and when it falls.
4. **Guardian Angel is once a minute for each healer**, not "once per fight" (a healer's fight has no clear end),
   and only against enemies' blows.
5. **Shield of the Many only while the guardian is above 30% health**, from one guardian a blow, never more than
   half a blow. In a crowd a guardian without a floor would fall over and over.
6. **Holy Fire** strikes for direct heals only (not heals over time), cannot crit, and armor takes half its cut.
7. **Loot luck is rolled now**, which also turns on Treasure Hunter and Legend of the Hunt as their texts always
   said: up to 22% more raid and dungeon items for a character that buys all three.
8. **Chain Lightning's jumps use the lightning jump bonus** (30% harder each jump with Eye of the Storm). A Topaz
   in a ring or amulet carries the same stat, so it now makes Chain Lightning stronger too.
9. **Thin the Herd's rush goes to everyone who fought the enemy**, as its experience does.
10. **Wildfire's burn jumps at full strength for a fresh 4 s**, so in a crowd that keeps dying it keeps going.
11. **Trailblazer adds its 50% to Keen Senses** (1.8 times with both, not 1.95). "Never knocked out while you
    gather" is mostly for looks: the server never stopped a knocked out pet from gathering.
12. **The old capstones keep their tree's emblem; only the 18 new ones have pictures.**
13. **Path names**: Perfect Form's path is the Duelist; where the draft had no separate name the path is the
    capstone's own (Colossus, Unbreakable, Ironblood, Alpha...). `!learn guardian` is refused as unclear
    (Guardian's Oath, Guardian Angel, the Guardian's Shield of the Many).
14. **The trees' version is not raised**, so nobody gets a free respec for this: no build that existed changes.
15. **Rings and bolts follow the Weapon effects setting** (and potato mode), not a setting of their own.
16. The numbers in "What was tuned", all of them.

## Not built

- Three endings for Gathering and Artisan (not drafted: those trees are a day old).
- A window in the game for the trees: there is none, so the game shows the endings only in chat and by their cues.
