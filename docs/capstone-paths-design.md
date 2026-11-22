# Capstone paths: three endings for each tree, pick one

**Status: draft for the owner to react to, nothing built** (2026-11-22). The idea is the owner's: "each tree could
have multiple ending skills at 999 but only one can be picked per skill tree, like lets say attack skill could
offer 3 different ones but you can only pick one of them". The trees themselves are in `combat-skills-design.md`.

## The rule

- Each of the nine combat-side trees ends in **three capstones** at level 999 in place of one. **Only one of a
  tree's three can be learned.** The one each tree has today stays as the first of its three, unchanged, so a
  build made before this keeps working.
- A capstone costs 15 points, as today. To change path, take the capstone back (the usual respec rules and
  price) and learn another.
- **Each new capstone asks for the nodes of its own theme** (the trees already support "requires"; today's
  capstones ask for their tree's level 600 nodes and keep doing so), so a path is something you build toward and
  not a pick on the last day: the assassin's ending wants the crit and armor piercing nodes, the berserker's the
  power and bleed nodes, and so on.
- On the map a wedge ends in three crowns side by side; the chosen one is lit, the other two dim with "you chose
  (name)". In chat: `!learn assassin`, and `!tree precision` lists the three.
- The paths line up with the class gear lines on purpose (rogue, barbarian, paladin, dark knight, witch hunter,
  monk, samurai), so a path has an outfit and `!equip rogue` has a build to go with.

In code this is one more field on a capstone ("group") and one check when learning. The work is the 18 new
capstones: about a third are numbers the game already has, the rest need a small new rule each, marked below.

## The paths

Numbers are first guesses in the scale of today's capstones, for the combat simulator to tune. "New" means a rule
the game does not have yet.

### Precision (Attack)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Duelist | **Perfect Form** (today's) | +4% crit chance, +20% crit damage | Disarm, Precision Strikes (as today) | no |
| Assassin, the rogue | **First Blood** | Your first hit on every new target is a critical hit that ignores half its armor. Your critical hits poison (6% of the hit a second for 4 s) | Sharp Edge, Precision Strikes | first hit rule; poison on crit (bleed on crit exists) |
| Blade Dancer, the samurai | **Flowing Steel** | +8% attack speed. Every hit in a row on the same target adds 2% more, up to 10%; it is gone when you switch target | Swift, Flurry | the hits in a row rule |

### Might (Strength)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Colossus | **Colossus** (today's) | +12% melee power, +10% damage to enemies below 30% | Leap, Titan (as today) | no |
| Berserker, the barbarian | **Blood Rage** | The less health you have, the harder you hit: up to +30% melee power at 30% health and below. 3% of your melee damage heals you | Brute Force, Rend | power by missing health |
| Earthshaker | **Aftershock** | Every fourth melee hit strikes every enemy around you for half its damage. Your stuns last 20% longer | Cleave, Whirlwind | the every fourth hit rule |

### Bulwark (Defense)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Unbreakable | **Unbreakable** (today's) | 6% less damage from everything, +25% threat | Rally, Last Stand (as today) | no |
| Guardian | **Shield of the Many** | You take 15% of the damage dealt to allies near you in their place, after your own armor | Rally, Thick Skin | the share (the tank pet's guard does this for one owner already) |
| Avenger, the dark knight | **Retribution** | +10% thorns. After you block, your next hit deals 60% more | Spiked Armor, Shield Training | the after a block rule |

### Restoration (Healing)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Miracle Worker | **Miracle Worker** (today's) | +15% healing power, heals give a 10% shield | Sanctuary, Smite (as today) | no |
| Crusader, the paladin | **Holy Fire** | Every heal you cast also strikes your target for 30% of the amount healed. +20% damage to the undead | Smite, Devotion | heals that strike |
| Lifebinder, the monk | **Guardian Angel** | Once per fight, an ally near you who would die is left at 1 health and healed for 30% (Last Stand, for someone else). +8% cast speed | Barrier, Quick Hands | Last Stand for an ally |

### Marksman (Ranged)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Deadeye | **Deadeye** (today's) | +12% ranged power, 15% armor penetration | Snare, Rapid Fire (as today) | no |
| Sniper | **Clean Kill** | +25% damage to enemies above 80% health, +20% crit damage | Eagle Eye, Piercing Shot | damage to the healthy (the mirror of Executioner) |
| Stormbow, the witch hunter | **Volley** | Every third shot also hits a second enemy for 60%. +6% attack speed | Multishot, Hunter's Pace | the every third shot rule (shared with Aftershock) |

### Elements (Magic)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Archmage | **Archmage** (today's) | +12% magic power, +8% cast speed | Meteor (as today) | no |
| Pyromancer | **Wildfire** | Your burns hurt 40% more, and when a burning enemy dies its burn jumps to an enemy near it | Searing, Fireball | the jump on death |
| Stormcaller | **Eye of the Storm** | Lightning that jumps hits 30% harder on every jump, and all your cooldowns are 10% shorter | Chain Lightning, Unravel | no (both are stats the game has) |

### Vitality (Health)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Undying | **Undying** (today's) | +10% health, much more regeneration in combat | Iron Skin (as today) | no |
| Blood Drinker | **Crimson Pact** | +4% life steal, and life steal beyond full health becomes a shield of up to 10% of your health | Bloodthirst, Hardy | the overflow shield |
| Ironblood | **Ironblood** | Enemies' chance of a critical hit on you is 10% lower, and you receive 15% more healing | Resilience, Iron Skin | no |

### Hunter (Slayer)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Legend of the Hunt | **Legend of the Hunt** (today's) | +10% damage to bosses, +5% loot luck | Apex Predator (as today) | no |
| Culler | **Thin the Herd** | +12% damage to everything that is not a boss, and killing an enemy gives 10% more damage for 6 s | Execute, Pressure Points | damage to non-bosses; the after a kill rule |
| Fortune's Favorite | **Fortune's Favorite** | +12% loot luck in raids and dungeons, and nothing else | Treasure Hunter | no (but it is more loot: see below) |

### Beastmaster (Taming)

| Path | Capstone | What it does | Asks for | New |
|---|---|---|---|---|
| Pack Leader | **Pack Leader** (today's) | A packmate of your pet's kind fights beside it at half strength | Elemental Bond, Guardian's Oath, Call of the Wild (as today) | no |
| Alpha | **Alpha** | No packmate: your one pet is larger, 30% stronger and has 30% more health | Fang and Claw, Guardian's Oath | the larger pet (a scale) |
| Trailblazer | **Trailblazer** | Your gatherer pet's skills are half as strong again (Forage 5% to 7.5%, before Keen Senses), and a pet is never knocked out while you gather | Keen Senses, Back on Its Feet | no |

## What to look at before saying yes

- **Three real choices, or one right answer?** A path is only a choice if none of a tree's three is plainly the
  best. The simulator has to show the three within a few percent of each other for their own job (single target,
  groups of enemies, staying alive), and that will move these numbers.
- **More loot and more finds:** Fortune's Favorite (+12% loot luck in place of +5%) and Trailblazer (more from a
  gatherer pet, on top of the Gathering tree's Bountiful) put more items into the world. Both are small and both
  cost the player a combat ending, which is the point of them, but they belong on the same list as the supply
  numbers in `trade-skills-design.md`.
- **Gathering and Artisan** could get three endings each too (for example the village, the rare finds, the eggs).
  Not drafted: those two trees are a day old.
- **Pictures:** 18 new capstone icons, two drawn sheets.
- **Old games (0.9.6)** have no trees, so nothing changes for them.

## To decide

1. Whether to build it at all, and whether for v0.9.7 or after it.
2. The paths and names above: which to keep, change or drop.
3. Whether a capstone may ask for nodes of its theme (proposed), or only for the level.
