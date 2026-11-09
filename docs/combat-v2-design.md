# Combat v2: the overview and the foundation

The next big step for Ravenfall's combat, planned with Zerratar on 2026-11-06. Combat gets elements,
resistances and weaknesses, a real health pool, skills that players, pets and enemies use on their own,
gems in sockets, and a 100 floor tower to test it all against. **The new combat is for everywhere:**
islands, raids, dungeons, the arena, duels, Ravebridge and the tower all use the same rules.

This document is the overview and the foundation everything else stands on. The other parts:

| Document | What it covers |
|---|---|
| [combat-skills-design.md](combat-skills-design.md) | Player combat skills: skill trees, points, the active loadout, respec, roles |
| [battle-pets-design.md](battle-pets-design.md) | The new pet skill everyone gets, battle pets that level, evolve and use skills, and the clan skill that goes with it |
| [gems-design.md](gems-design.md) | Sockets, enchanted gems, combining, removing (and when it does not destroy the gem) |
| [tower-design.md](tower-design.md) | The 100 floor tower event: floor quests, floor bosses, retries, a few floors per update |

Decided with Zerratar (2026-11-06):
- **Roles come from the skill tree,** not from a class you pick. A player can be a healer and a tank at once, a bit of everything, or all in on one thing.
- **Not everything at once:** skill points are limited, so no player has every skill and upgrade. Changing means a respec.
- **The pet skill is a normal skill everyone has from the start.** The clan gets a separate skill that complements it.
- **Removing a gem destroys it,** unless the clan's Enchanting level or the player's own skill tree gives a chance to keep it.
- **Health becomes a pool.** The Health skill no longer *is* your health; it raises it (for example 10 HP per level).
- **Aim and power on gear are replaced** by a stat model that can carry elements, resistances and more.
- **Enemies, and bosses above all, use skills too.**

---

## What combat is today (the baseline)

From a read of the code on 2026-11-06 (game `Assets/Scripts`, server `RavenNest/src`). The full notes
are in `docs/systems/combat.md`; this is what matters for the redesign.

- **Combat runs in the game client.** `Core/GameMath.cs` decides hits and damage; the server stores items,
  enchantments, status effects and experience, and only sanity checks levels.
- **Damage:** one function for all styles. Melee uses Attack for accuracy and Strength for damage; Ranged
  and Magic use their own level for both; Healing uses the Healing level with the *magic* gear stats.
  `MaxHit = (int)(((level + 3) * (power * 0.00175 + 0.1) + 1.05) * 0.95)`.
- **Hit chance:** an attack rating (aim skill, gear aim, a fifth of the damage skill) against a defence
  rating (Defense level, a share of ArmorPower, a quarter of the defender's damage skill), plus a dice roll.
  Defense only changes hit chance; armour changes hit chance and narrows the damage spread. **There is no
  damage reduction.**
- **Gear stats:** `WeaponAim/Power`, `MagicAim/Power`, `RangedAim/Power` and `ArmorPower` on the item (no
  item mixes melee with magic or ranged; 24 armour pieces also carry an offence stat). The highest values
  on the dev copy: weapon power 854, armour 517, item level 1016. All skills go to level 999.
- **Health is the Health skill level.** Health gains a third of all combat experience and is never trained
  on its own. Out of combat it regenerates 10% of max per second after 10 seconds.
- **Attack speed:** players swing every 1.5 s (healing 2 s), enemies mostly every 0.75 s, 1 s or 1.5 s.
- **Enemies are melee only,** with stats serialized on each enemy in the scenes. They already keep a
  threat table (damage times 2.5 with a one handed sword) and switch to the highest. They have no abilities,
  except hero mode's wave enemies (bolt, lunge and slam with telegraphs).
- **Enchantments:** `NAME:value;NAME:value` on the inventory item. 20 attributes: `AIM`, `POWER`, `ARMOR`
  (1-20%) and one per skill (1-25%). 20,992 enchanted stacks on the dev copy.
- **Status effects:** food, potions and scrolls give timed effects. Poison, Bleeding, Burning and the
  attack attribute effects (HealthSteal and others) are stored but never read.
- **Hero mode** already has what automatic skills need: a skill shape (unlock level, cooldown, a run
  function), cooldown gating, area and arc targeting, stuns, telegraphed attacks and hit feel.
- **There is no combat simulator.** `GameDataSimulation` covers mining drops and levelling only.

Existing bugs to fix as part of the foundation, since they skew today's numbers:
- POWER, AIM and ARMOR enchantments count twice (`PlayerController.UpdateEquipmentEffect`, L3009 and L3019).
- Heals pass crit chance into the dodge slot: heals never crit and "miss" as often as they should crit (`GameMath.cs:68`).
- Hit chance modifiers are backwards: raising hit chance does nothing, lowering it makes hitting easier.
- `StyleBonus` is always 0; the dungeon boss's halved defence never applies; the raid boss's history leaves out weapon stats.
- `IncreasedCriticalHitDamage` effects vanish on arrival; poison, bleed and burn effects do nothing.

---

## 1. The character sheet

Every fighter (player, pet, enemy) has the same derived sheet, built from skills, gear, gems,
enchantments, skill tree passives and status effects:

| Stat | From | What it does |
|---|---|---|
| **Max HP** | 8 per Vitality level (the skill stored as Health), 0.8 per point of armour on armour pieces, plus gear, gems, passives | The health pool. Vitality 999 is 7,992; a full Elder Atlarus set with a shield adds 2,075 (10,067). Enemies and pets keep 10 per Health level. Changed 2026-11-09, see Progress. |
| **Power** (per style) | Strength for melee, Ranged, Magic; weapon power | The size of hits |
| **Accuracy** (per style) | Attack for melee, Ranged, Magic; weapon accuracy | Chance to hit |
| **Evasion** | Defense level, some gear | Chance to be missed |
| **Armor** | Armour pieces, Defense passives | Takes a share off physical damage |
| **Element damage** | Weapon element, gems, skills | Extra damage of fire, ice, lightning or poison |
| **Resistances** | Gear, gems, passives | Take a share off each element's damage; can be negative (a weakness) |
| **Critical chance and damage** | Gear, gems, passives | 5% and 150% to start |
| **Attack speed / cast speed** | Weapon type, passives, effects | Seconds between basic attacks |
| **Healing power** | Healing level, healing weapons, passives | The size of heals and shields |
| **Threat** | Role skills, Defense passives | How much attention enemies pay to you |

Health regeneration stays as it is out of combat. In combat only skills, food and potions heal.

## 2. Elements

Five damage types to begin with, each with its own effect, so picking one is a real choice:

| Element | Effect on hit (chance grows with element damage) | Typical enemies |
|---|---|---|
| **Physical** | none; reduced by Armor | everything |
| **Fire** | **Burn:** damage over 4 s, refreshes | weak: undead, plants, ice creatures; immune: fire elementals, dragons resist |
| **Ice** (water and frost) | **Chill:** -20% attack speed for 4 s; 5 stacks freeze for 1.5 s | weak: fire creatures; resist: sea creatures |
| **Lightning** | **Shock:** 30% of the hit jumps to one more enemy nearby; small stun chance | weak: sea creatures, golems; resist: elementals |
| **Poison** | **Poison:** stacks up to 5, damage over 6 s, ignores Armor | weak: beasts; **immune: undead, golems, elementals** |

Light and dark (holy and shadow) can follow later; undead would be weak to light. Elements also flavour
the islands and tower floors (see the tower document).

**Resistances** are a percentage per element, capped at 75% for players. Enemies can go below zero
(a weakness takes extra damage) or to 100% (immune; the hit shows "Immune"). Element damage on gear and
gems is shown on the character page and in item tooltips.

## 3. The damage formula

Kept recognisable, so today's progress still means the same thing, then extended.

**Step 1: the base hit.** Today's max hit, times ten (health is ten times larger too):
`MaxHit = 10 * ((level + 3) * (power * 0.00175 + 0.1) + 1.05) * 0.95`, where *level* is the style's damage
skill and *power* the style's power. A hit rolls between 50% and 100% of it, leaning high (today's bands,
without the armour narrowing).

**Step 2: hit or miss.** `HitChance = clamp(5%, 95%, 50% + (Accuracy - Evasion) / (2 * max(Accuracy, Evasion, 1)))`,
where Accuracy = 2 x aim skill + gear accuracy and Evasion = 2 x Defense + gear evasion. Even fights hit half
the time, a fighter twice as accurate hits 75%, and nothing is ever certain. (The constants are starting
points for the simulator, see section 8.)

**Step 3: split by element.** A hit is physical, plus element damage from the weapon, gems and the skill used.
For example a fire staff with a ruby: 60% fire and 40% physical.

**Step 4: reduce.** Physical: `x 640 / (640 + Armor)`. Elements: `x (1 - resistance)`.

Why 640 (found with the simulator on 2026-11-07, see section 8): the max hit multiplies by `1 + power / 57.1`,
and gear carries about 2.8 armour per point of power, so armour in this form answers power: in matching gear a
blow stays about four times an ungeared one at every level, and fights last about as long at level 50 as at
999, while better gear still wins. The first idea, armour measured against the attacker's level, let a level
999 fight end in two swings.

**Step 5: crit, then effects.** A crit multiplies the hit; then each element rolls its effect (burn, chill,
shock, poison).

Healing uses the same shape: base heal from the Healing level and healing power, crits, no miss and no
reduction.

## 4. Threat (aggro)

Roles only mean something if enemies go for the right person. Enemies already keep a threat table, so it
is extended rather than replaced:
- Damage dealt adds that much threat. Healing adds half of the amount healed, split over the enemies
  fighting the one healed.
- Tank skills multiply threat (for example Defense's "Taunt" forces the target for 4 s and adds a burst
  of threat).
- An enemy switches when someone has 10% more threat than its current target (melee) or 30% more (at
  range), so targets do not flicker.
- Bosses can break the rules on purpose (a skill that hits whoever is furthest away, for example).

## 5. Status effects, buffs and debuffs

One system for all of them, used by skills, elements, food, potions and enemy attacks:
- An effect has a type, a value, a duration, stacks, and a source.
- Damage over time (burn, poison, bleed) ticks once a second.
- Crowd control (stun, freeze, slow, root) has diminishing returns on bosses (half duration, then immune
  for 6 s), as hero mode already does.
- The effects that are stored but never read today (Poison, Bleeding, Burning, HealthSteal and the
  attack attributes) finally do something, through this system.

## 6. Enemies and bosses

**Definitions instead of numbers in the scenes.** Each kind of enemy gets a definition: level and stats,
attack style (melee, ranged, magic), element of its attacks, resistances and weaknesses, the skills it
uses, and loot. An editor script makes the first definitions from the stats serialized in the scenes today,
so nothing gets weaker or stronger by accident.

**Skills.** Regular enemies get at most one or two simple skills (a spider's poison bite, a goblin
shaman's heal, a skeleton archer's volley). **Bosses get a kit:** telegraphed attacks taken from hero
mode's bolt, lunge and slam, plus area attacks, summons, enrage and phases at health thresholds. Raid and
dungeon bosses use them in their normal fights, not only in hero mode.

**Ranged and magic enemies** stop at range and use their element, instead of every enemy walking up to melee.

## 7. Where it runs, and old clients

Combat keeps running in the game client, where it runs today. The server keeps storing items, gems,
enchantments, skill trees, pets and experience, and checks what the game reports.

- **One shared combat library.** The formulas (sections 1 to 5) move into a small library both the game and
  the simulator use (the way `RavenNest.Models.dll` is shared today), so the simulator and the game can
  never disagree.
- **Old games keep old combat.** A 0.9.6 game ignores what it does not know. The server keeps the old item
  columns filled for one version, so older games still show and use the old stats, while new games read
  the new ones. Items, gems and trees are stored the same for both.

## 8. Balance: a combat simulator before anything ships

Today's balance was never measured. Before the new combat goes out:
- A command line simulator runs thousands of fights with the shared library: a player of a given level,
  gear and build against each island's enemies, each raid and dungeon boss scaling, and each tower floor.
- It reports time to kill, damage taken, deaths and healing needed, per level band.
- Targets to tune against: an island enemy at your level dies in 4 to 8 swings; you can fight for minutes
  without dying at the right island; a raid boss takes 3 to 6 minutes for a normal group; a tower floor boss
  needs the right roles and elements.
- Characterization tests pin today's formulas (times ten) first, so we know what changed and by how much.

**Built 2026-11-07:** `RavenNest.Combat` (the shared library, 40 tests) and `src/tools/RavenNest.CombatSimulator`
in the RavenNest repo. `parity` compares today's melee with the new rules for a fighter against an equal one in
the best gear for its level (from the public item list). First results, swings to win:

| Level | 10 | 50 | 100 | 200 | 400 | 500 | 700 | 999 |
|---|---|---|---|---|---|---|---|---|
| Today | 3.9 | 5.0 | 5.0 | 8.6 | 4.2 | 8.0 | 11.7 | 13.9 |
| Today's hit rate | 100% | 100% | 96% | 45% | 65% | 29% | 18% | 12% |
| New | 4.1 | 7.0 | 8.6 | 9.3 | 7.8 | 7.3 | 7.4 | 7.2 |
| New hit rate | 93% | 79% | 72% | 68% | 65% | 66% | 65% | 64% |

Today's high level fights only last because armour makes nearly every blow miss; the new rules hit about two
thirds of the time at every level and keep fights a steady length.

## 9. Moving existing data over

- **Items:** a one time mapping on the server. `WeaponAim/Power`, `MagicAim/Power`, `RangedAim/Power`
  become Accuracy and Power on that weapon's style; `ArmorPower` becomes Armor. The 24 armour pieces with an
  offence stat keep it as a small Power or Accuracy bonus. Item stats move into a flexible list (stat, value)
  so new stats need no new columns.
- **Enchantments:** `AIM` becomes `ACCURACY`, `POWER` and `ARMOR` stay; the double counting bug is fixed at
  the same time, so enchanted gear will show *lower* numbers than today. The release notes have to say why.
  Enchantments keep base stats and skill bonuses; elements and resistances come from gems (see the gems
  document), so the two do not overlap.
- **Health and healing items:** HP becomes ten times larger, so food and potion heal amounts become ten
  times larger as well. Health experience stays as it is (a third of combat experience).
- **Enemies:** definitions are generated from today's scene stats (section 6).
- **Characters:** nothing to migrate; skill trees start empty with all points to spend.

## 10. Phases

A proposal, to be decided per release:

1. **Foundation:** the character sheet, health pool, elements, the new formula, threat, the status effect
   system, enemy definitions, the bug fixes, the data migration, the shared library and the simulator.
2. **Player combat skills:** skill trees, points, the loadout, respec, automatic use; enemy and boss skills.
3. **Battle pets:** the new skill for everyone, pets that level, evolve and use skills; the clan skill.
4. **Gems and sockets:** enchanted gems, combining, removal; clan Enchanting tied in.
5. **The tower:** floors 1 to 10, then more with every update.

Phases 1 to 3 share the same "skills on a cooldown, used automatically" machinery, so they could be one
update (the headline being battle pets and the new combat), with gems and the first tower floors in the
next.

## Progress

Phase 1 (foundation), started 2026-11-07:
- [x] The shared library `RavenNest.Combat` (RavenNest repo): sheet, elements, hit chance, max hit, armour,
  resistances, crits, element effects, status effects, threat. 40 tests.
- [x] The simulator `src/tools/RavenNest.CombatSimulator` with `parity`; armour tuned to 640 from it.
- [x] The game uses the library (`Assets/Scripts/Core/CombatRules.cs`, switch `CombatRules.Enabled`) and Health
  is a pool of ten per level everywhere (game 8531a41). Play mode checked with 30 bots.
- [x] Enemy definitions (attack style, element, resistances, skills): `EnemyDefinition` in the library, one
  `EnemyDefinitionAsset` per kind in `Assets/World/Enemies` (28: 26 island kinds, the dungeon and raid bosses as
  scaled), every MainWorld enemy points at its kind and takes its levels from it on Awake. Made by
  `tools/combat/make-enemy-definitions.py` and `apply-enemy-definitions.cs`; exported to the simulator with
  Ravenfall > Combat > Export Enemy Definitions To Simulator. Resistances are flavour for now (nothing deals element
  damage yet) and every kind still attacks in melee with physical damage.
  **Found on the way:** the Unity 6.7 upgrade (c016ead, 2026-08-14) made SkillStat's levels `ObscuredIntSplit`, and Unity
  dropped the old numbers from the scene: 136 of the island enemies (Home's bunnies, cows and Metalons, the away
  island's pirates, Ironhill, Kyojin, Iceheim, Atria, Eldara) have been combat level 3 with 10 HP in every release
  since, v0.9.5.0 to v0.9.6.1 included. Their levels come back from MainWorld at f944e82 (2025-05-09), matched by
  file ID. Two strays were evened out with their group (one bunny with Defense 3 instead of 4, one Red Metalon with
  Health 35 instead of 20).
- [x] Elements and resistances on gear (2026-11-07). `GearStats` in the library: one list of named stats in the
  enchantments' text form (`MELEE_POWER:120;MELEE_ACCURACY:95;FIRE_DAMAGE:30%;ICE_RESISTANCE:10%`): power and accuracy
  per style, healing power, armour, evasion, max health, crit chance and damage, element damage and resistances for the
  four elements. Shares are written as percentages. Stats a reader does not know are kept, not dropped.
  The aim/power migration (section 9) is `GearStats.FromLegacy`: WeaponAim becomes MeleeAccuracy, ArmorPower Armor.
  On the server, `Item.ExtraStats` (a new column, made at startup) holds what the classic columns cannot; the columns
  stay the record for power, accuracy and armour while 0.9.6 games read them. `RavenNest.Models.Item.Stats` sends the
  full list (columns plus extras), so a new game reads one list and an old one ignores it. The website's item tooltips
  show the new stats; the admin item editor has a "Combat v2 stats" field and the list shows them as a chip.
  In the game, `Item.GetGearStats()` (Stats, or the columns from older servers) feeds `EquipmentStats.Extras` (all worn
  gear but weapons) and `WeaponExtras` per style: a player wears a sword, a bow and a staff at once, and each weapon's
  element damage converts the hits of its own style only; element damage on other gear adds to every hit. Max health
  from gear adds to the Health pool (`SkillStat.GearHealth`). Item tooltips and `!stats` list the new stats.
  **Themed gear** (server `GameData.GearStats.cs`, given at startup where an item has no stats yet, so admin edits
  are kept; 134 items on the dev copy): each of four sets has one element, its weapons turn 25% of their hits into it
  (Elder versions 35%) and each armour piece resists it 5% (Elder 8%, a full set with shield 30% or 48%): Dragon fire,
  Abraxas poison, Phantom ice, Ether lightning. Jewellery: Ruby and Emerald resist fire and poison, Dragon and
  Phantom rings and amulets add a little of their element and resist it, Ring Of The Celestial resists lightning,
  Windcallers Amulet adds lightning and evasion, Owls Eye Ring crit chance, Archmages Pendant and Warriors Might Ring
  crit damage, Knights Emblem health. The silly weapons get theirs: Bacon 2H Sword fire, Turd 2H Sword 50% poison,
  Ultimate Tuna Slapper ice, the Pickle Ricks poison, Ghrims Stick lightning, the Bananarang crit chance.
  **Simulator `elements`** (a player at each area's requirement with a weapon a quarter physical, fire, ice, lightning
  or poison): against today's enemies (no armour) an element changes little except where they resist or are weak
  (skeletons: poison 14.4 swings against 11.0 physical, fire 10.4). Against enemies in armour like the player's, any
  element they do not resist ends fights a third sooner from level 200 up (gladiator 7.1 swings physical, 4.8 with a
  quarter fire), because element damage skips armour entirely. **To decide before enemy gear tuning:** let armour take
  part of element damage too, give enemies a base resistance that grows with level, or keep weapon shares smaller.
- [x] Threat and status effects in the game's fights (2026-11-07, with `CombatRules.Enabled`):
  - `CombatEffects` (Game/Combat) keeps the library's `StatusEffectSet` per fighter. Every blow of the new rules
    reports its element procs (`CombatRules.BlowLanded`): fire burns, poison stacks, ice chills and five chills
    freeze, lightning jumps 30% of the hit to the nearest enemy within 6 m and sometimes stuns. Damage over time
    ticks once a second, reduced by the target's resistance (bleed by armour), credited to whoever caused it, and a
    kill by it counts as theirs. Chilled fighters attack slower (players and enemies), frozen and stunned ones not at
    all; bosses take shorter crowd control and then ignore it for 6 s. Checked in play mode: an all-fire sword's
    burn took an Iceheim viking from 2,250 to 1,707 health with no further hits; three chills put a samurai's attack
    speed at 0.4.
  - Enemies keep the library's `ThreatTable`: damage adds threat (a one handed sword still 2.5 times, the tank's
    weapon), healing someone they fight adds half the heal shared over them, and they switch target only to someone
    with 10% more threat (30% for ranged and magic enemies). The old table switched whenever anyone caught up.
  - Not yet: effect icons or "Burn" numbers on stream (open question), enemies dealing elements themselves (their
    definitions are all physical), taunts (they come with the skill trees).
- [x] The baseline bugs (2026-11-07):
  - POWER, AIM and ARMOR enchantments counted twice (`UpdateEquipmentEffect` added the item's number with the bonus,
    then the bonus again). Fixed: a Lionsbane Katana (173 power) with POWER:20% wears 207, not 241. Enchanted gear
    shows lower numbers than in 0.9.6; the release notes must say why (section 9).
  - `IncreasedCriticalHitDamage` effects were dropped on arrival (no case for them); they now add to the crit
    multiplier, and the new rules add effects to the 150% base instead of replacing it.
  - The dungeon boss's halved Defense never applied (combat reads MaxLevel, which kept the full level). Fixed.
  - The raid boss's history counted past raids' players but not their weapons, watering its weapon power and aim
    down. Fixed, so raid bosses hit harder and more often than in 0.9.6.
  - Heals putting crit chance in the dodge slot, backwards hit chance effects and `StyleBonus` live in the old
    `GameMath` formulas, which the new rules replace (`CombatRules`): heals can crit, hit chance effects raise
    accuracy, and there is no style triangle. They stay as they were for `CombatRules.Enabled` off.
- [~] Simulator scenarios: `enemies` (a player at each fighting area's requirement, every combat skill at that level,
  best melee gear, against its enemies) is in; raids and dungeons are next. First results (swings to win, new rules):
  Home 2 to 6, Ravebridge 8 to 16, the away island's pirates 5 to 7, Ironhill 6 to 9, then Kyojin 4, Iceheim 3 to 5,
  Atria 3 and Eldara 2. From Kyojin up the enemies sit below their area's requirement and have no gear, so a player at
  the requirement barely gets touched; the next tuning step gives enemies power, accuracy and armour like the best
  gear at their level (the parity table says that lands at 4 to 9 swings). Mangrove's "Lv. 1" group holds level 15
  pirates and a level 34 captain that a new character cannot beat (today's rules or new).
  **`bosses`** (2026-11-07): groups of 5 to 50 players of one level in the best melee gear for it, against the boss the
  game makes for them (raid: the players' average levels, Health times 100, their average gear, a swing every 3 s;
  dungeon: their levels times 0.75 times the dungeon's scale, Defense halved, Health times 100 times players/10 times
  the scale times 1.15, a third of their gear, a swing every 2 s). Minutes to win, new rules:

  | | 5 players | 10 | 25 | 50 |
  |---|---|---|---|---|
  | Raid boss | 2.8 to 4.7 | 1.3 to 2.3 | 0.5 to 0.9 | 0.3 to 0.5 |
  | Dungeon, Normal | 0.6 to 1.9 | 0.3 to 0.9 | 0.3 to 0.9 | 0.3 to 0.9 |
  | Dungeon, Heroic | 8.6 to 17.7 (60 to 90 falls) | 3.8 to 8.0 (about 30 to 40 falls) | 3.5 to 7.6 | 3.4 to 7.5 |

  The raid boss's health ignores how many fight it, so a big stream ends it in half a minute (the target is 3 to 6
  minutes for a normal group); Normal dungeon bosses fall in under a minute and barely touch anyone; Heroic ones
  (2.5 times stronger) take a few minutes but knock a ten player group down 30 to 40 times. **To tune next:** raid boss
  health by group size, Normal dungeon bosses stronger, Heroic ones hitting less hard.
- [x] Data migration check (2026-11-07): food and potion heals and heal over time are shares of the maximum health
  (`Stats.Health.MaxValue * amount`), so they grow with the ten times larger pool by themselves; nothing to migrate.
  The enchantment rename (AIM to ACCURACY) waits for the server's item stat switch, when 0.9.6 games stop reading
  the old columns.
- [x] Armour against elements (2026-11-07): armour takes half its usual share off element damage
  (`CombatMath.ElementArmorShare`). Elements skipping armour made any unresisted element end armoured fights a third
  sooner; at half, an unresisted quarter-element weapon ends them 5 to 18% sooner (gladiator 4.9 swings to 4.3) and a
  resisted one takes longer, so the element is a choice, not a must.
- [x] Magical attributes (2026-11-07), not bound to the old formulas: six new gear stats in `GearStats`, each capped
  for players: **thorns** (a share of melee blows taken hurts the attacker, at most 50%), **threat** (more threat
  from damage and healing, the tank's stat), **damage reduction** (off all damage, at most 50%), **life steal** (a
  share of damage dealt heals, at most 25%), **attack speed** (shorter swings, at most twice as fast) and **armour
  penetration** (ignore part of the target's armour, at most 75%), on top of crit chance and damage, evasion, health,
  element damage and resistances. Enchanting can roll eight of them as attributes, named like the stats: crit chance
  1-5%, crit damage 2-25%, attack speed 1-8%, armour penetration 2-15% and life steal 1-5% on weapons, rings and
  amulets (attack speed on gloves too); thorns 2-15% and damage reduction 1-6% on armour and shields; threat 5-30% on
  shields, helmets and chests (server `GameData.MagicalAttributes.cs`). Element damage and resistances stay with gems
  (phase 4). Checked in play mode: 20% thorns gave back 18 of a samurai's 90 damage, life steal healed, attack speed
  1.1, threat 1.3. More to add the same way (one stat, one cap, one place in the blow): block chance, crit
  resistance, healing received, damage against a family (Dragonslayer), movement speed.
- [x] Accuracy instead of aim (2026-11-07): the game's tooltips, `!eq`, `!stats` and the enchantment label, and the
  website's tooltips, item chips and admin editor say Melee/Ranged/Magic Accuracy and Power, and armour "takes a share
  off physical damage (and half that off elements)". The stored names (item columns, the AIM enchantment) stay while
  0.9.6 games read them.
- [x] Enemy gear (2026-11-07): each island kind wears 75% of the armour and 25% of the power and accuracy of the
  standard melee kit (the metal sets, no joke weapons) at its own combat level (simulator `enemygear 500 0.75 0.25`,
  applied with `tools/combat/apply-enemy-gear.cs`). A player at the enemy's level in the standard kit for it wins in 5
  to 9 swings with about half their health left and falls in at most 5% of fights; gladiators 6.6 swings (63% left),
  elven soldiers 5.9 (70%). **Areas whose requirement is below their enemies** (simulator, a player at the
  requirement): Ravebridge's skeletons and souls (level 15 and 26 at "Lv. 10/15"), Home's purple and green Metalons
  (57 and 71 at "Lv. 40"), Ironhill's dragons (120 to 135 at "Lv. 100") and Mangrove's "Lv. 1" pirates (15 and 34):
  a player at the requirement loses most fights there. **Fixed (2026-11-07)** by lowering those enemies
  (`tools/combat/fit-enemy-levels.cs`), not raising the areas: Home's Lv. 40 area holds all three Metalons (raising it
  would lock out the red one and leave nothing between 20 and 71), Ironhill at 120 would leave a gap after Small
  Island's 50, and every island from Kyojin up keeps its enemies at or below the requirement. Now: Ravebridge cows 2,
  goblins 5, skeletons 10, the soul 15; Mangrove pirates 3, the captain 6; purple Metalons 40, green 45; Ironhill's
  dragons 100, Lord Quackers 115. A player at the requirement loses at most 10% of fights against the regular enemies;
  the tougher ones are meant to be harder (green Metalon 24%, the captain 18%, Lord Quackers 45%). Ravebridge takes any
  character, and maxed ones beat these whatever their level.
- [x] Raid and dungeon bosses (2026-11-07, with the new combat; simulator `bosses tuned`): the raid boss's health pool
  grows with the raiders expected (the past raids' average, or the players in the game): 2.5 times at ten, at least
  half that, its level and rewards unchanged, so a raid takes 3.3 to 5.9 minutes at any group size. Dungeon bosses: a
  type's combat scale moves toward 1 (Heroic's 2.5 becomes about 1.4) and the pool grows three times on Normal and
  twice on Heroic: ten players beat Normal in 0.9 to 2.8 minutes (a couple of falls) and Heroic in 2.5 to 7.2 (5 to 14
  falls).

- [x] Gems and sockets (phase 4, 2026-11-08): see [gems-design.md](gems-design.md), section 7.
- [x] Vitality and armour health (2026-11-09, user): the Health skill is shown as **Vitality** everywhere players
  look (game, chat, website); stored names, the database, the API and leaderboard addresses keep Health, and
  `!train health` still works next to `!train vitality`. A player's pool is 8 a Vitality level
  (`CombatMath.PlayerHealthPerLevel`) plus 0.8 health per point of base armour on armour pieces (helmet to shoulders,
  `CombatMath.HealthPerArmor`; enchantments to armour do not count), flat health on gear and "more health" shares of
  the whole pool. Combat level adds nothing (the user dropped that: health would grow too big). Worked out in the game
  from what is worn rather than stored on items, so an edited item never saves it twice. Simulator `enemies`: the
  same fight lengths, players a few percent sturdier at low and mid levels (Lord Quackers knocks out 12% instead of
  19%). Synced tower floors cap armour health with the armour (`Tower.SyncGearHealth`). The details window has a
  health bar of its own, the Vitality tile shows the level, the armour tile says how much health its armour gives,
  and an Equipment / Stats switch lists every stat from the fight's own combat sheet (mockups
  docs/mockups/details-vitality-*.webp). Website: the combat guide has a Health section and calculator.

## For later

- **Weapon effects you can see:** weapons with an element could carry a visual effect (embers on a fire sword, frost
  on an ice staff, sparks on lightning, dripping venom), not on every item but where it fits: the themed sets'
  higher tiers, the silly weapons (a sizzling Bacon 2H Sword), and later gems socketed into weapons. Like the other
  look features, it needs a settings toggle and turns off in potato mode.

## Open questions

- Should Slayer and Health have their own skill trees (boss hunting; vitality and lifesteal), or only Attack, Strength, Defense, Ranged, Magic and Healing?
- The fifth loadout slot: at a combat level, a tree node, or a later update?
- Light and dark elements now, or later with the tower floors that need them?
- How visible should numbers be on stream (damage numbers, "Immune", effect icons) without cluttering the view?
