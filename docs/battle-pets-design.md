# Battle pets: a new skill for everyone, and a clan skill to go with it

Part of [combat-v2-design.md](combat-v2-design.md). A new **normal skill every player has from the start**
(name not decided, see below), for pets that fight beside you, level up, evolve into a new look, learn skills
and use them on their own. Some pets tank, some heal, some deal damage, and some help with gathering.
The clan gets a **complementary clan skill** that makes its members' pets grow better.

Decided with Zerratar (2026-11-06):
- The pet skill is a normal skill, there for everyone straight away, not a clan skill.
- The clan gets a separate skill that complements it.
- Evolving changes the pet to the next model in a tiered set (Zerratar has several tiered model sets, to be
  looked through) and raises its base stats.

---

## 1. The skill

**Name: Taming** (chosen 2026-11-07; the clan skill became *Rookery*). The candidates were
(Ravenfall's skills are one plain word each: Attack, Woodcutting, Sailing, Slayer,
Enchanting, so the name should be too):

| Name | For | Against |
|---|---|---|
| **Taming** (recommended) | Short, plain, reads like the other skills, easy in chat (`!taming`, "Taming 120"). Pairs well with the clan skill *Rookery* | Pets also come from eggs, which you hatch rather than tame (players will not mind) |
| *Beastmastery* | Evocative, says "pets that fight" | Long on name tags and in the stats list; strongly tied to another game's hunter class |
| *Kinship* | Warm, the bond with your pet is what grows | Not obvious what it does from the name alone |
| *Husbandry* | Accurate | Dull, and it reads as farming |

The library still says "the pet skill" (`RavenNest.Combat/BattlePets.cs`); it becomes Taming when the skill is added.

- Every character starts at level 1; it goes to 999 like every skill.
- **Experience:** while a battle pet is out, the pet skill gains a quarter of the experience of the skill you
  are training, the way Health gains a third of combat experience. So it grows with normal play, whatever
  you do, as long as your pet is with you.
- **What the level gives:**
  - **The pet's level cap:** a pet can never be higher level than your pet skill level.
  - **Stable size:** you can keep 1 pet at level 1, one more at 50, 150, 300 and 500 (5 in all). One is out at a time.
  - **Skill slots:** pets use 2 skills at level 1, 3 at pet skill level 100, 4 at 400 (and evolution adds one).
  - **Evolution:** each evolution stage needs a pet skill level as well as a pet level.

## 2. Pets

**Species** are data, one entry per kind of pet:

| Field | Example |
|---|---|
| Role | tank, healer, damage, support, gatherer |
| Element | fire, ice, lightning, poison or physical |
| Base stats per level | HP, power, accuracy, evasion, armour, resistances |
| Skills by level | level 1 Pounce, level 25 Howl, level 60 Fire Breath... |
| Evolution chain | three models from one tiered set, with the levels and materials for each step |
| Where it comes from | islands, bosses, quests, the tower |

The species list depends on the tiered models available; a first set of about 12 to 15 species (three to four
per role, a mix of elements) is plenty to launch with.

**Getting pets** (decided with Zerratar 2026-11-07: taming is real, so the skill's name means what it says):

1. **A starter pet from a short quest** when the skill arrives: pick a tank, a healer or a damage pet, so nobody
   starts without one and the quest teaches the idea.
2. **Taming creatures on the islands: the main way.** Started like training, `!tame metalon`: the character
   fights that kind of creature as usual, and when one is nearly beaten it has a chance to tame it instead of
   killing it. A failed try is a normal kill with its usual experience.
   - **One `!tame`, one pet** (2026-11-21): taming stops when a creature is tamed, and the next one takes another
     `!tame`. Unlike `!mine rune ore`, it does not keep going: pets are not meant to be farmed while away.
   - **The chance** comes from your Taming level against the creature's, and a **treat** raises it (cooked or
     farmed, so Cooking and Farming get a new use). Rare enough to matter, about like a Slayer drop.
   - **Only creatures:** beasts, insects, spirits and dragons. People (goblins, pirates, vikings, gladiators,
     elves) cannot be tamed.
   - **Taming experience comes from every try**, so the skill can be trained directly, not only through the
     quarter share from normal play. This answers the high level catch-up question below: a level 600 player
     levels Taming by taming low level creatures, which is real training rather than a free boost.
   - **The pet starts at level 1**; its kind sets its role, element and skills, and the creature's model is used,
     scaled down to pet size.
   - **A tame is a stream moment**: "Zerratar tamed a Metalon!", throttled so a busy stream does not flood chat.
3. **Eggs for the special pets** that cannot be found in the wild: raid and dungeon bosses, tower floors and towns,
   quests and events. These species use the tiered model sets and **evolve like every other pet**. Eggs are
   tradeable and hatch over time while you play; a hatched or tamed pet is bound to its owner.

**Tameable today** (from the enemy list; levels are the creatures' combat levels, the Taming level to try is a
first guess):

| Creature | Where | Level | Taming | Pet |
|---|---|---|---|---|
| Bunny | Home island | 3 | 1 | a tiny support pet; the first tame for most players |
| Cow | Home island, Ravebridge | 2 to 8 | 1 | a tank that moos at enemies (an easter egg that earns its place) |
| Tormented Soul | Ravebridge | 15 | 15 | a spirit healer or support, ice or poison |
| Metalon (red, purple, green) | Home island | 33 to 45 | 30 | one species: red, purple and green are its three evolution stages, so the models we already have make the chain |
| Dragons (red, blue, green, white, purple) | Ironhill | 100 | 100 | one species per colour with its element (red fire, blue and white ice, green poison, purple lightning) |
| Lord Quackers | Ironhill | 115 | 150 | very rare, one of a kind; a silly one |

Dragons have one model per colour and no tiers. Their stages can be the same model growing (hatchling, drake,
dragon) with the element's glow from the second stage (the same effects the elemental weapons may get; a
setting, off in potato mode). New islands and tower bands bring their own tameable creatures.

**Today's pets** (the cosmetic Pet items, the Monkey Pet, the Diamond Raven Pet and others) stay what they
are and keep their slot. A battle pet that is out takes the place of the cosmetic pet while it is out, so
there is one animal at your heels, not two. (See the open questions.)

## 3. Levels and evolution

- Pets gain experience while they are out, the same share as the pet skill, and level up to your pet skill
  level.
- **Evolution** at set levels (for example 30 and 100, plus a pet skill level and some materials): the pet
  changes to the next model in its set, its base stats go up (+15% at the first step, +30% at the second),
  it gets one more skill slot, and sometimes a skill only the evolved form learns.
- Evolving is a moment: a short glow and burst on the pet, and a chat line ("Zerratar's Pebble evolved into
  Boulderback!"). A good place for a silly easter egg or two.

## 4. In combat (and while gathering)

- The pet follows you as pets do today (the follow, catch up and idle behaviour of `PetController`), and joins
  your fights: it attacks your target with its element and uses its skills by their use rules.
- **Roles work like the players' roles:** a tank pet taunts and takes hits, a healer pet heals you and allies,
  a support pet buffs or weakens. Enemies can target pets (threat applies).
- **Knocked out, not dead:** a pet at 0 HP lies down and gets back up after 30 s, no penalty.
- **Gatherer pets** help while you gather instead: more speed, a chance of an extra resource, or a better chance
  at rare drops, depending on the species (a mole for mining, an otter for fishing, a beetle for farming...).
  They do not fight, which is the trade off.

## 5. Pet skills

Each species learns its skills at set levels and uses the ones in its slots automatically, on cooldowns, with
use rules like the players' skills (see [combat-skills-design.md](combat-skills-design.md)). The owner picks
which learned skills are in the slots.

Examples by role:
- **Tank:** *Guard* (taunts enemies attacking its owner), *Harden* (-40% damage taken for 5 s), *Bodyslam* (stun).
- **Healer:** *Lick Wounds* (heals the owner), *Soothing Aura* (heals everyone near over time), *Purify* (cleanses).
- **Damage:** *Pounce* (a big first hit), *Fire Breath* (fire, cone), *Frenzy* (attack speed for 6 s).
- **Support:** *Howl* (+attack speed for allies near), *Hex* (lowers an enemy's resistance to the pet's element).
- **Gatherer:** *Forage* (chance of an extra resource), *Keen Nose* (better rare drop chance), *Dig* (finds a bonus item now and then).

## 6. The clan skill that goes with it

**Name:** *Rookery* (2026-11-09; first *Menagerie*). Other candidates were *Kennels* and *Pet Lore*.

- Unlocks at a clan level (for example 20), the way clan skills already unlock (`Skill.RequiredClanLevel`).
- **Experience:** from members raising their pets, the way Enchanting fills from members enchanting.
- **Effects** (modest, so clanless players are not locked out of anything): more pet experience for members,
  cheaper evolution materials, faster egg hatching, and a small chance at a bonus skill when a pet learns one.
- On the website it sits beside Enchanting on the clan page, and the clan list can sort by it.

Before a second clan skill can exist, `ClanProcessor` has to stop assuming a clan's first clan skill is
Enchanting (it takes `clanSkills.FirstOrDefault()` today).

## 7. Commands and pages

- `!pet` shows your active pet (level, role, skills, HP); `!pet stable` lists your pets.
- `!pet use <name>` sends out another pet; `!pet rename <name>` names it.
- `!pet skills` lists learned skills and the slots; `!pet slot 2 howl` changes a slot.
- `!pet evolve` evolves when it can, and says what is missing when it cannot.
- **The website** gets a pets page on the character page: the stable, each pet's model, level, stats and
  skills, slots, and what the next evolution needs.

## 8. Technical notes

- A new skill means a new skill index and new columns in the character `Skills` table (the server adds missing
  columns at startup). The enchantment attributes `AIM`, `POWER` and `ARMOR` use attribute indexes 16 to 18
  today (16 is shared with ALCHEMY), so the new skill's attribute must not collide with them.
- The server already has an unused battle pet model (`RavenNest.DataModels/Pet.cs`, `Models/BattlePet.cs`
  with level, experience, attack, defense, strength, health, happiness and hunger). It can be replaced by a
  `BattlePet` table (owner, species, name, level, experience, evolution stage, learned skills, slots).
- Species definitions live on the server (so drops and eggs can use them) and are sent to the game with the
  rest of the static data.
- Pets fight with the same shared combat library as players (see the overview, section 7).

## Open questions

- Should the cosmetic pet stay visible next to a battle pet, or does the battle pet take its place while out (as above)?
- Can a battle pet wear the look of a cosmetic pet you own (a "skin"), so favourite pets can fight too?
- How many species at launch, once the tiered models are counted?
- **Pets of high level players start far behind** (taming experience from every try is the proposed answer, see
  section 2; the rest of this point is kept for the record). The pet's level is capped by the pet skill, which starts at 1
  for everyone, and a level 1 pet does nothing next to a level 600 owner. Options: the pet skill grows much faster
  while it is far below the owner's combat level (a catch-up factor, like rested experience), or the starter quest
  sets the pet skill to a share of the combat level. Needs a decision before launch; the simulator shows pets only
  at their owner's level.

---

## Prepared ahead of the models (2026-11-07)

Built so the models are the only thing missing; nothing in the game or on the server uses it yet.

- **`RavenNest.Combat/BattlePets.cs`** (RavenNest repo): `PetRole`, `PetStatProfile` (levels as shares of the pet's
  level, power, accuracy and armour per level standing in for gear, threat, attack interval), `PetSpecies` (three
  stage names, role, element, resistances, skills, origin, and `Sheet(level, stage)` that builds the same
  `CombatSheet` players and enemies fight with), `PetSkill` (learned at a pet level, an `ActiveSkill` like the
  characters' actives, or a gather bonus for gatherer pets) and the rules: experience share, 30 s knockout, stable
  levels, evolution levels and the pet skill level each needs, stage multipliers, skill slots, `CanEvolve` with
  what is missing for `!pet evolve`.
- **The 14 skills from section 5** with numbers. One **stand-in species per role** (`BattlePets.Archetypes`) until
  the real species are picked from the tiered models; a real species copies its role's numbers and changes them.
- **The simulator's `pets` scenario** (`dotnet run -c Release -- pets [runs] [armour share] [offence share]`): an
  owner in the standard melee kit with each stand-in at the owner's level (evolved at 30 and 100), one enemy of the
  same level, two minutes, the enemy choosing targets with the game's threat table. With the starting numbers:

  | Role | Result |
  |---|---|
  | Damage | +28 to 35% damage at every level |
  | Support | +17 to 23% damage (Howl, Hex and its own hits) |
  | Tank | takes 54 to 60% of the enemy's hits, the owner takes about 60% less; knocked out about once in two minutes (three times at level 10, the early tank is too soft) |
  | Healer | heals back 18% (level 10) to 53% (level 999) of the damage taken |

- **Clan skills** no longer assume Enchanting is the only one (`ClanProcessor`): every clan skill is sent to the
  game, a level up goes out at once (the old check compared a level with itself), and a clan gets a clan skill its
  level opens even when it already has another (it only checked clans with none). So *Rookery* can be added as a
  row in the `Skill` table with its `RequiredClanLevel`.

**Still waiting:** the species list (models), the `BattlePet` table and `!pet` commands, the pet skill's column and
attribute index (needs the name), the game's battle pet controller on top of `PetController`, the website's pets
page, and the catch-up question above.

---

## 9. Every pet fights: strike styles, companions and the first build (2026-11-08)

Zerratar (2026-11-08): start with the tameable creatures, do the evolved sets when their models arrive, and it would
be great if every pet could battle, even though not every model has animations that suit a melee attack.

**How a pet attacks is apart from its role.** Each species has a strike style that fits its model:

| Strike | Looks like | For |
|---|---|---|
| **Bite** | the model's own attack clip, up close | models with an attack clip: bat, ghost, pumpkin, spider, werewolf, yeti, metalons, dragons |
| **Pounce** | a leap done in code: crouch, jump in, squash on the hit, hop back | beasts without an attack clip: cats, foxes, wolves, bears, bunnies, cows, raccoons, the Halloween pets |
| **Bolt** | stays about 5 m back, a little hop and a glowing bolt of its element | small and magical pets: ravens, orbs, witches, the octopus, the Tormented Soul, Lord Quackers |

Heals and buffs show as a ring on the ground in green (or the element's colour), so no pet needs a cast animation either.
Of the ~50 pet prefabs only 6 have an attack clip; with these three styles all of them fight.

**Every pet item can fight (companions).** `!pets wake (pet item)` wakes a pet item the player has into a battle pet of
its companion species (one per kind of item; the item stays in the inventory, and the pet only comes out while the
player has it). A companion keeps its item's look and grows a little per stage (x1.12, x1.25) instead of changing
model. 31 companion species cover every pet prefab, with silly stage names (Turd Raven, Stinky Raven, Legendary
Stinker; Kitty, Housecat Supreme, Lord of the Sofa).

**Tamed creatures** use the model of the enemy they were tamed from, copied without its scripts and scaled to the
pet's height for its stage; the metalon's red, purple and green are its three stages. Species: bunny (Binky), cow
(Moo), Tormented Soul (healer, Wail), metalon (Venom Sting), the five dragons (one per element, Whelp, Drake, Dragon),
Lord Quackers (QUACK stuns everything near from sheer confusion, a tenth of the usual tame chance).

### What is built

- **Library** (RavenNest.Combat `PetSpeciesList.cs`, `BattlePets.cs`): 10 tameable and 31 companion species, the
  strike styles, models and heights per stage, 12 more pet skills, taming odds (2% at the species' level, +0.04% per
  Taming level above, 8% at most, a treat x1.5), tries per level for Taming experience. Tests.
- **Server**: Taming is skill 17 (no enchantment attribute: index 17 is POWER's); the unused `Pet` table carries the
  battle pets (species, stage, slots, source, the woken item); `BattlePetManager` with `pets.list/tame/wake/use/rename/
  slot/evolve/release/exp`; players carry their pets to the game.
- **Game** (`Assets/Scripts/Game/Pets`): `BattlePetController` (follows, fights on its owner's target, the three
  strikes, skills by their use rules, knocked out for 30 s), `PetClipPlayer` (clips straight from any model in a
  playable mixer, found by name; animation events off), `PetModels`, `PetFx` (bolt, ring, burst), `PlayerBattlePets`
  (the stable, taming rolls, the experience share, reports to the server every minute). `!tame` steers the player to the
  fighting area of their island with that creature and prefers it as a target. Taming in the details window.
- **Chat**: `!pets` (stable, use, away, rename, skills, slot, evolve, wake, release, about) and `!tame` (creature, stop)
  in both bots. `!pet` stays the cosmetic pet command.

Seen with bots in play mode (local pets, no server): the cow, raven, red drake, bunny, purple and green metalon, wolf,
fox, kitty, Wisp and duckling Quackers following and fighting, the Wisp's ice bolt, the drake's fire breath ring, the
cow guarding (taking half of its owner's blows), a knocked out cow lying down, the metalon evolving into its green look,
a bot taming a bunny (it went to the bunnies by itself) and Taming rising from the experience share.

### Choices made without Zerratar (easy to change)

- **Three stable places to begin with** (8 in all), so a tame and a woken pet item or two fit from the start.
- **Tank pets take half of the blows on their owner** while near, and all of them while Guard or Moo is up; other roles
  take none. Enemies do not target pets directly yet.
- **The quarter share of experience goes to Taming as well as the pet** while a pet is out. This is the catch-up for
  high level players: Taming climbs quickly towards their other levels. Since 2026-11-10 it slows once caught up: the
  full quarter while Taming is below the level of the skill being trained, a tenth at or above it
  (`BattlePets.TamingShare`), so Taming follows a player's levels instead of passing them. The pet keeps the quarter.
- The first pet tamed or woken goes straight out; a tame is announced at most every 8 s.
- `!pets release (name) confirm` lets a pet go for good.

### Round two (2026-11-08)

- **Taming is idle** (Zerratar asked): while a pet is out, the pet and Taming each get a quarter of all experience the
  owner earns, whatever they do; `!tame` tries are an extra way to train it. The chat texts say so.
- **A Battle pets setting** (Settings > Graphics): off, and in potato mode, pets still fight and guard unseen, their
  blows land at once and the cosmetic pet shows again.
- **Gatherer pets** help on the server, in every gathering skill's drops: Forage a chance of one more of a drop, Keen
  Nose better drop odds, Dig a find now and then when nothing dropped.
- **Treats**: each tameable creature loves some foods (bunny carrots or cabbage, cow wheat or corn, Tormented Soul apple
  pie or bread, metalon raw meat, dragons steak or raw meat, Lord Quackers bread, as one feeds ducks). Carried, a tame is
  half as likely again; one is eaten when it works. `!tame` and `!pets about` name them.
- **Starter pets**: `!pets starter fox` (damage), `bear` (tank) or `fawn` (healer) for an empty stable, on the HD animal
  models. This stands in for the starter quest.
- **Rookery** (first called Menagerie), the clan skill: opens at clan level 20 and grows while members have pets out
  (see "Rookery grows from time" below; it first took a tenth of the pets' experience, far too fast); members' pets
  earn 0.05% more a level (50% at 999) and their stables hold one more pet at 100, two at 300. The skill row is
  made at startup with a fixed id. Clan pages show it at once.
- **Taming's icon** in the details window: the icon pack's bear paw. No name tags over pets (they would crowd the
  player tags at overview zoom).

### Round three (2026-11-08)

- **/pets on the website**: how pets work and every kind there is, with pictures rendered from the game's own models
  (tools/website/pet-renders.cs in edit mode, then pet-renders.py crops them into the website's imgs/pets; the orbs
  have no mesh and no picture). The character's Pets tab shows them too.
- **The character details window** shows the pet that is out as the last chip of the loadout row: name and level over
  its health, red while knocked out, its kind and skills in the tooltip.
- A leaving player's unsent pet experience is sent at once; on a tower floor a pet fights at most at its owner's synced
  level. Guard and Moo say what they really do (the pet takes the blows).

### The signed-in path tested (2026-11-08)

Run on the local server with Zerratar's dev character through the real chat handlers (replies kept, never sent):
`!pets` (empty stable, info, stable, starter, rename, skills, slot, evolve, about, wake, release with and without
confirm, away, use), `!tame` (a creature, too high a level, a player, stop), the server's tame with its pause, the
experience share, the per-minute report, the report on leaving, and the stable and Taming coming back on join. An
independent review of the same path ran alongside. Fixed:

- **Pets were never saved.** `Pet.PlayTime` is a bigint column and the save sent TimeSpans as text, so every pet
  insert failed and pets lived only in the server's memory. TimeSpans are written as ticks now; the old server's
  unsaved changes were replayed on the next start. The first start with saved pets then failed to load: Happiness and
  Hunger are float (8 byte) columns read as 4 byte floats. Both mapped now.
- A pet at its Taming cap lost the experience over the cap. Taming is saved a little after the pet's report, so it was
  lost almost every report; it is kept now and spent once Taming is higher.
- The last experience report of a leaving player could reach the server after the remove and be refused.
- An experience answer replaced the whole pet, so a late one could bring a pet sent away back out; experience earned
  by one pet could be reported for the next one sent out.
- A woken pet whose pet item is gone (sold, gifted, banked) sleeps again on join and stops helping gather.
- A tame the game rolled could be refused because the server's Taming level was a level behind; it is tried once more.
- Smaller: a woken pet is named after its kind ("Disco Raven", not "Disco Raven Pet") so evolving renames it,
  `!pets use floof` finds Sir Floof, `!pets stable` asks the server for its places, "Taming 50" instead of "pet skill
  level 50", `!train taming` says how Taming trains instead of starting farming (two letters away), Discord's `!pets`
  offers starter.

Still unseen: gatherer bonuses in real drops, the Rookery growing on a clan of level 20, a real evolution (needs
pet level 30 and Taming 50), the website Pets tab signed in.

### Rookery grows from time (2026-11-08, retuned 2026-11-09)

Zerratar: the first test put the clan skill from 1 to 33 off one player's report; clans with hundreds of players
would max it in days. Enchanting, the other clan skill, only grows when members use its command on a cooldown (after
five years the best clan, 655 members, is 412; the average is 54). The first fix made it slower than that, which was
too slow: a really big clan should reach 999 in about a year and a half.

It grows from the **minutes members have a pet out**, not from what the pets earn (which grows with levels). Each
level takes 12 minutes times the level of one member's pet time (12 minutes for level 2, 20 hours for level 101), and
n members out at once count as 1 + log2(n) members: 2 count as 2, 20 as 5.3, 100 as 7.6, 300 as 9.2. Every pet report
(once a minute while a pet earns) adds the minutes since that member's last one, at most 1.5. Days to a level:

| Clan | 100 | 250 | 500 | 999 |
|---|---|---|---|---|
| 300 members out at once, all day and night | 4 | 28 | 113 | 450 |
| 100 out at once, all day and night | 5 | 34 | 136 | 543 |
| 20 out at once, 16 hours a day | 12 | 73 | 293 | 1171 |
| 3 out at once, 4 hours a day | 96 | 602 | 2413 | |

Members' pets earn 0.05% more a level, 50% at 999, so every level counts up to the top (it was 0.1% a level, capped
at 500, when 500 was out of reach). The numbers live in RavenNest.Combat `Rookery` (MinutesPerLevel, Pace,
ShareOfLevel, ExperienceBonus), with a test for the table's pace.

**Name:** first *Menagerie*; renamed *Rookery* (where rooks and ravens nest) on 2026-11-09, since Menagerie is hard
to say and spell. The skill row keeps its fixed id; a row made under the old name is renamed at startup.

**Changing clan:** a stable over its places keeps every pet. The extra places only gate adding: a player who filled
the places a big clan's Rookery gave and then leaves (or whose new clan's Rookery is lower) keeps all their pets and
can send any of them out, but cannot tame or wake another until they are under the new size (`!pets stable` shows
6/5). Nothing is ever taken away by a clan change.

### Eggs and evolution materials (2026-11-09)

Zerratar imported *Monsters Ultimate Pack 02 Cute Series*: 24 monsters that are eight chains of three stages, not sorted
into chains by folder. All 24 are used, each once, as eight species that hatch from eggs (RavenNest.Combat
`PetSpeciesList`, `Hatched`). The order of the three less obvious chains comes from the models' detail (triangles):

| Species | Stages | Role | Element | Egg | Taming to hatch | Minutes | Found in |
|---|---|---|---|---|---|---|---|
| wolf-pup | Wolf Pup, Wolf, Werewolf | damage, bites | none | Wolf Pup Egg | 1 | 60 | dungeons, raids |
| bud | Bud, Bloom, Blossom | healer, bolts | poison | Bud Seed | 1 | 60 | dungeons, raids |
| practice-dummy | Practice Dummy, Target Dummy, Training Dummy | tank, bites | none | Practice Dummy Egg | 1 | 45 | tower floors |
| sun-blossom | Sun Blossom, Sunflower Fairy, Sunflora Pixie | support, bolts | fire | Sun Blossom Seed | 10 | 90 | dungeons, tower |
| shell | Shell, Spike, Hermit King | tank, bites | ice | Shell Egg | 20 | 120 | uncommon and harder dungeons, raids |
| snakelet | Snakelet, Snake, Snake Naga | damage, bites | poison | Snakelet Egg | 40 | 150 | rare and harder dungeons, raids |
| bomb | Bomb, Snow Bomb, Poison Bomb | damage, bolts | fire | Bomb Egg | 60 | 180 | raids, higher tower floors |
| dragon-spark | Dragon Spark, Dragon Fire, Dragon Inferno | damage, bites | fire | Dragon Spark Egg | 100 | 240 | raids, epic and legendary dungeons (rare) |

- **Eggs** (`PetEggs`) are items with fixed ids (like the gems), made at server start (`GameData.EnsurePetEggs`),
  tradeable. Each character a dungeon, raid or tower chest rewards has a chance at one: dungeons 2% plus 0.5% a tier,
  raids 2%, tower chests 2.5% (lowered 2026-11-09 from 3% + 1%, 3% and 4%); then an egg from that source's table by weight (the dragon is about 2% of a raid's eggs).
- **`!pets hatch (egg)`** puts one in the stable: it takes a place, cannot go out, and warms while its owner plays.
  `!pets hatch` alone lists the eggs in the bag. In the Pet table an egg is a pet at level 0 whose `PlayTime` is its
  minutes so far. The game reports once a minute while a character has eggs (`pets.incubate`); every egg gets the
  minutes since the last report (at most 1.5) and hatches into a level 1 pet once it has its minutes. A hatched pet
  goes straight out when none is, and the hatch is a burst on the pet and a throttled chat line. `!pets release` on an
  egg gives it back to the bag, no confirm.
- **Evolving costs raw gems** of the pet's kind (`PetEvolution`): 10 for the first evolution, 30 for the second. Ruby
  for fire, Sapphire for ice, Topaz for lightning, Emerald for poison; a pet with no element takes its role's gem
  (Diamond for damage, Onyx for tanks and gatherers, Amethyst for healers and support). Raw gems are mined and traded,
  so nobody needs a clan for them. Every pet pays, tamed, woken or hatched; the answer says what is missing
  ("10 Amethyst (you have 3)").
- **The Rookery** now also makes evolutions cheaper (40% fewer gems at 999) and eggs hatch faster (a quarter at 999).
- **Models**: the 24 prefabs moved to `Assets/Resources/Pets/Cute/` (the model key is `pet:Pets/Cute/Wolf Pup`), their
  35 materials turned from Standard to URP Lit (hand painted, no shine: smoothness 0, no highlights) and the sparks to
  URP Particles Unlit. The pack and Resources are gitignored, so these changes live on the dev PC like the other paid
  assets; reimporting the pack would bring the prefabs back at their old place with Standard materials.
- **Clips**: the pet clip player now prefers "In Place" clips (the pack has "W Root" ones too), skips the moves between
  states ("Rest to Idle"), knows Fly Forward, Slither (and the pack's "Slither Foward") and Roll, and a bolt pet prefers
  a projectile or spell clip to a bite. Every one of the 24 gets an idle, a move, an attack, a hit and a knock out clip.
- **Pictures**: `tools/items/egg-renders.cs` (play mode) renders each species at each stage against black and white,
  framed on the posed model (flying idles lift Bud and the fairies out of their rest bounds, which
  `tools/website/pet-renders.cs` misses); `tools/items/egg-icons.py` makes the egg icons (the baby pet climbing out of a
  cracked egg, a striped husk for the two seeds, the resource icons' dark outline) and the website's pet pictures.
- **Website**: /pets has a "Hatched from eggs" section, the gems and the Rookery's new effects; the character's Pets
  tab shows eggs warming with their minutes left, and the gems an evolution takes.

Tested on the local server with the dev character: `!pets hatch` (the list, Taming too low for the dragon, a wolf
pup egg into the stable), the stable showing "warming, 60 min left", sending an egg out refused, the minute report,
releasing an egg back to the bag, `!pets about` for an egg species, evolving the Bunny into a Jackalope for 10 of its
12 Amethyst (the inventory feed showed 2 left), an egg saved and loaded again over a server restart, and the hatch on
join (the egg set to 59 minutes in the dev database). Not seen yet: an egg from a real dungeon, raid or tower reward,
a hatched pet fighting, the evolved models' look in a real fight, the website pages in a browser.

### Enemies and pets (2026-11-09)

Enemies go for a battle pet when it has more threat than its owner, as they would for any fighter:

- **Threat of its own.** A pet's blows are its own threat, times its species' threat multiplier (tanks 2.5, damage
  0.9, healers 0.8, support 1.0, gatherers 0.5); the kill, experience and everything else stay its owner's. Its heals
  add half the heal, shared over the enemies fighting the one healed, as a player healer's do.
- **Taunts.** Guard and Moo make the enemies on its owner attack the pet for their 5 s (`EnemyController.TauntByPet`);
  the old guard (every blow still aimed at the owner lands on the pet) stays for that time too.
- **Choosing.** The enemy's threat table holds players and pets together and picks the same way (10% more threat to
  switch, 30% for ranged and magic enemies). A knocked out pet drops out and the enemy turns to the next with the
  most threat, usually the owner, at once.
- **Only drawn pets.** Pets that are not drawn (the Battle pets setting off, potato mode, the owner on the ferry or in
  the onsen) are never attacked, so no enemy swings at nothing; a tank's share of its owner's blows stays.
  Dungeon bosses keep their own targeting.
- An enemy's blow on a pet is worked out from the two combat sheets (`CombatRules.PetBlow`): armour, evasion and crits
  as for anyone; Harden still takes its 40% off.

Seen in play mode with bots: a combat level 25 owner's level 60 Calf held every enemy; a combat level 368 owner
out-threatened its level 60 Wolf Pup; a combat level 521 owner kept the enemies except while its Practice Dummy's
Guard taunted. A pet whose bolts land before its owner reaches the enemy draws it first (a level 999 owner
then ends the fight in one blow). A pet knocked out under attack sent the enemy back to its owner within 0.25 s.

### Eggs from quests and events (2026-11-09)

On top of dungeons, raids and tower chests, eggs now come from quests and the island events. The chances and the
tables live in `RavenNest.Combat.PetEggs` (sources Quest, PirateRaid, Regatta, Ravebridge); the server rolls them
(`PetEggRewards`) and the inventory feed brings the egg.

| Where | Chance | Which eggs |
|---|---|---|
| A finished weekly quest | 15% | any egg, the dragon least often |
| A finished seasonal quest (Halloween, Christmas) | 6% | the same |
| Days played in a row: day 7, then every 14 days (21, 35, ...) | always | the same |
| A finished stream quest: the helper who added the most | 15% | the same |
| The pirates' raid held, each defender | 3% (9% for the one who held longest) | mostly the Bomb Egg (their powder kegs), Shell, Snakelet |
| The regatta, a real race: the winner / the rest of the podium | 10% / 4% | mostly the Shell Egg, Sun Blossom Seed |
| A Ravebridge story quest (paid once each) | 10% | mostly the Practice Dummy Egg (Lumbridge's training dummies), Wolf Pup |

The chances were lowered on 2026-11-09 (user: "a bit lower"), by about a third to a half from the first numbers
(weekly 25%, seasonal 10%, stream quest 25%, pirates 5%/15%, regatta 15%/6%, Ravebridge 15%); the streak egg is
unchanged.

**Days played in a row** (2026-11-09, user): the streak egg follows days played, not daily quests, so a viewer who
stays in a long stream for a week counts as one who leaves and joins every day. The server counts it for every
character in a running session on every tick (`QuestProcessor.PlayStreak`, stored as CharacterQuestStats.PlayStreak
and LastPlayDay, UTC days; the columns are added at startup): a new day adds one, a day without play starts again at
1. The first egg comes on day 7, then one every 14 days; the game says it in chat (event PlayStreakEgg, 59). Tested
with the dev character: a streak of 6 ending yesterday became 7 on joining and gave a Bud Seed.

- **The speckled egg**, a new chain (`!quest begin speckled`, the barkeep's): a dozen hen's eggs, a harvest, keeping
  the sniffers away, the chicken-or-egg riddle (every answer is right), a nest of wheat and a trip to Ironhill's
  dwarves, who say what it is. It ends with a soulbound Dragon Spark Egg (given even to someone who has one), Taming
  experience and the title "the Egg Sitter".
- Quest replies name the egg with the reward ("... and a Tome of Forgetting and a Wolf Pup Egg"); a stream quest's,
  the pirates' and the regatta's eggs and Ravebridge's get a chat line ("zerratar found a Shell Egg! !pets hatch puts it
  in the stable to warm."); on the banana island an egg counts as a rare find in the feed.
- Each species' "where to find" line (`!pets about`, the website) names the new places.

Tested: the chain played to the end on zerratar's dev character (12 Eggs, 50 harvested, 40 kills, any answer, 30
Wheat, Ironhill): it finished and paid the Dragon Spark Egg and the Taming experience. The egg merged into a tradeable
Dragon Spark Egg stack the character had, because the server's stacks do not look at soulbound (StackKey); a separate
fix for every soulbound reward. Not seen yet: a weekly, a streak,
the events or a stream quest actually giving one (chance-based, and their messages would have reached the live chat).

### The animals: 18 more egg pets (2026-11-21)

Zerratar imported polyperfect's *Low Poly Animated Animals* and wanted the pets widened with it. The animals are
realistic low poly rather than fantasy, but the style fits. Phase 1 (this section) is everything without the editor:
the species, their eggs, where the eggs are found, five new skills and the game's clip picking. Phase 2 makes the
prefabs, pictures and egg icons (the work list below). Until then an animal hatched on a test server shows a raven.

**All 18 hatch from eggs.** None of these animals is an enemy on the islands, and taming needs a creature to fight.
**Not used, because they are pets already:** bear (the starter and two pet items), wolf (the Wolf Pup line and a pet
item), fox and deer (starters and pet items), snake (the Snakelet line), rabbit and cow (the Bunny and the Cow are
this pack's rabbit and cow) and gorilla (the Monkey pet item is this pack's gorilla, small and brown, and ends as
"Silverback"). Cats and spiders are pet items too, but in another role and from other models, so they got an egg
line of their own here. Sea animals and the big savanna ones are left out.

| Species | Stages (model per stage) | Role | Element | Strike | Skills (pet level) | Egg, Taming, minutes | Found in |
|---|---|---|---|---|---|---|---|
| panda | Panda Cub, Panda, Bamboo Baron (Panda, growing) | tank | none | bites | Guard 1, Harden 20, **Bamboo Snack 25**, Bodyslam 60 | Panda Egg, 25, 120 | uncommon+ dungeons, tower floors 4+, quests |
| boar | Squeaker, Wild Boar, Tusk Lord (Boar, growing) | tank | none | bites | Guard 1, Harden 20, Bodyslam 60 | Boar Egg, 15, 90 | uncommon+ dungeons, raids, quests |
| horse | Pony, Warhorse, Nightmare (Horse_Brown, Horse_Grey, Horse_Black) | tank | fire | bites | Guard 1, Harden 20, Fire Breath 60 | Pony Egg, 60, 180 | raids, tower floors 7+, quests |
| dog | Pupper, Doggo, Woofer (Dog_Chihuahua, Dog_GoldenRetriever, Dog_GreatDane) | healer | none | pounces | Lick Wounds 1, Howl 1, Soothing Aura 25, Purify 60 | Pupper Egg, 1, 60 | dungeons, raids, quests, Ravebridge |
| capybara | Capybara Pup, Capybara, Onsen Master (Capybara, growing) | healer | ice | bolts | Lick Wounds 1, Soothing Aura 25, Purify 60 | Capybara Egg, 20, 120 | uncommon+ dungeons, quests, the regatta |
| sheep | Lamb, Woolly Sheep, Thunderfleece (Sheep, Sheep_Wool, Sheep_Wool) | healer | lightning | bolts | Lick Wounds 1, **Wool Coat 25**, Soothing Aura 25, Purify 60 | Lamb Egg, 10, 90 | uncommon+ dungeons, quests, Ravebridge |
| tiger | Tiger Cub, Tiger, Tyger Burning Bright (Tiger, growing) | damage | fire | bites | Pounce 1, Frenzy 25, Fire Breath 60 | Tiger Egg, 50, 180 | rare+ dungeons, raids, quests |
| eagle | Storm Eaglet, Storm Eagle, Thunderbird (Eagle, growing) | damage | lightning | bites | Pounce 1, Frenzy 25, Storm Breath 60 | Storm Eagle Egg, 40, 150 | raids, tower floors 13+, quests |
| goose | Gosling, Goose, Horrible Goose (Goose, growing) | damage | none | bites | **HONK 1**, Pounce 1, Frenzy 25 | Goose Egg, 5, 75 | dungeons, raids, quests |
| parrot | Parrot Chick, Parrot, First Mate (Parrot, growing) | damage | none | bolts | Barrage 1, Hex 25, Frenzy 25 | Parrot Egg, 30, 120 | the pirate raid, the regatta, quests |
| rat | Rat, Giant Rat, Rat King (Rat, growing) | damage | poison | bites | Pounce 1, Frenzy 25, Venom Breath 60 | Rat Egg, 1, 45 | dungeons, quests, Ravebridge |
| lion | Lion Cub, Lioness, Pride King (Lion_Female, Lion_Female, Lion_Male) | support | none | bites | Howl 1, Pounce 1, Hex 25 | Lion Egg, 75, 210 | raids, epic+ dungeons, quests, all rarely |
| spider | Spiderling, Weaver, Silk Matron (Spider_Green, Spider_Yellow, Spider_Red) | support | poison | bites | **Web 1**, Hex 25, Venom Breath 60 | Spider Egg, 35, 120 | rare+ dungeons, tower floors 4+, quests |
| penguin | Penguin Chick, Penguin, Emperor Penguin (Penguin, growing) | support | ice | bolts | **Snowball 1**, Hex 25, Frost Breath 60 | Penguin Egg, 20, 120 | tower floors 10+, the regatta, quests |
| reindeer | Reindeer Calf, Donner, Blitzen (Reindeer, growing) | support | lightning | bites | Spark 1, Howl 1, Storm Breath 60 | Reindeer Egg, 45, 150 | raids, tower floors 10+, quests |
| cat | Void Kitten, Orange Cat, Snow Cat (Cat_Black, Cat_Orange, Cat_White) | gatherer | none | bites | Forage 1, Keen Nose 25, Dig 60 | Kitten Egg, 1, 60 | dungeons, tower floors, quests |
| pig | Piglet, Truffle Pig, Truffle Baron (Pig, growing) | gatherer | none | bites | Forage 1, Keen Nose 25, Dig 60 | Piglet Egg, 10, 90 | dungeons, tower floors, quests |
| chicken | Chick, Hen, Rooster (Chick, Hen, Rooster) | gatherer | none | bites | Forage 1, Keen Nose 25, Dig 60 | Chick Egg, 1, 30 | dungeons, quests, Ravebridge |

Roles: 3 tanks, 3 healers, 5 damage, 4 support, 3 gatherers. Elements: fire 2, ice 2, lightning 3, poison 2, none 9
(ice and poison were well covered already; lightning was the rarest). A skill's level is the skill's own (Frenzy is
always 25), so a kit is which skills, not when. Egg names and first stage names start with a consonant, because chat
says "found a Panda Egg" and "hatched into a Panda Cub" (hence Storm Eaglet and Kitten Egg).

**The blurbs** (website and `!pets about`):

- panda: Eats, naps and takes hits, in that order. Pandas do not lay eggs. This one never got the memo.
- boar: Thick hide, thicker skull. Stands in the way on purpose.
- horse: Brown, then grey, then black as a bad dream. The Nightmare is a pun, and it is not sorry.
- dog: A pupper, then a doggo, then a woofer. Nobody can explain the breeds. Licks every wound better, asked or not.
- capybara: The chillest animal there is, which is why its element is ice. Everyone near it calms down and heals up.
  It has no idea.
- sheep: Count it when you cannot sleep. Do not pet it in dry weather: all that wool is full of static.
- tiger: Tyger, tyger, burning bright. The poem never says what it burns. It burns your enemies.
- eagle: One of the few pets here that is supposed to come out of an egg. Dives like lightning and, later, with it.
- goose: It is a lovely day in the village, and this is a horrible goose. Peace was never an option.
- parrot: Left the pirates for better crackers. Repeats everything you say, mostly at the enemy, mostly the rude parts.
- rat: Every adventurer's first kill, in some other game. This one has not forgotten, and it bites back.
- lion: Its roar makes everyone near hit faster. Everything the light touches is its litter box.
- spider: Green, then yellow, then red, like a traffic light that webs you. An enemy that cannot move is easy to hit.
- penguin: Throws snowballs and waddles away from the consequences. A penguin keeps its egg on its feet; you get to
  use a stable.
- reindeer: Grows up into Donner, then Blitzen: thunder, then lightning. Its nose glows a little in fog. It would
  rather not discuss it.
- cat: Leaves gifts at your feet while you gather. Some of them are even useful. The orange stage shares its one
  brain cell with every other orange cat.
- pig: Its nose finds what you miss. It eats half of what it finds, so your share is already after tax.
- chicken: Came first. Or the egg did. The old woman at the well still will not say. Pecks up what you drop while you
  gather. (The speckled egg chain's riddle.)

**Five new skills**, one each for something no pet skill did before; the numbers follow the pets' other skills and
stay under the players' own (a player's shield is 200% of a heal for 10 s, a freeze 2 s, a root 3 s, a self heal 25%
on 45 s):

| Skill | Role, level | Cooldown | Used | What it does |
|---|---|---|---|---|
| Bamboo Snack (panda) | tank, 25 | 40 s | below half health | heals itself 20% of its health |
| Wool Coat (sheep) | healer, 25 | 18 s | an ally below 60% | a shield of 150% of a heal for 8 s on the most hurt |
| Snowball (penguin) | support, 1 | 16 s | in a fight | 80% of a hit as ice, and the target is frozen for 1.5 s |
| Web (spider) | support, 1 | 18 s | in a fight | the target is rooted for 3 s and takes 10% more damage for 6 s |
| HONK (goose) | damage, 1 | 14 s | in a fight | 100% of a hit, and the target deals 15% less damage for 5 s |

The game already carried every one of these effects for pets (`BattlePetController.Apply`), and the website words them
from the numbers. No new gatherer skill: the server only knows Forage, Keen Nose and Dig (a speed bonus is in the
library's enum but nothing reads it).

**Where the eggs are found** (`PetEggs`, weights beside the cute series' 10/10/6/6/5/4/1). The chances per source
are unchanged, so every egg already there is a little rarer than before: a raid's eggs were 28% Wolf Pup and 2.8%
Dragon Spark, now 16% and 1.6%; an easy dungeon's were 38% Wolf Pup, now 19%. Raising the chances would keep the old
eggs as common as they were; that is the owner's call (they were lowered on purpose on 2026-11-09).

| Source | Animals (weight) |
|---|---|
| Dungeons, any | rat 6, chicken 5, dog 5, cat 4, goose 4, pig 4 |
| Dungeons, uncommon and harder | sheep 4, boar 4, capybara 3, panda 3 |
| Dungeons, rare and harder | spider 4, tiger 3 |
| Dungeons, epic and legendary | lion 2 |
| Raids | dog 5, goose 4, boar 4, eagle 3, reindeer 3, tiger 3, horse 3, lion 2 |
| Tower chests (a tier is three floors) | cat 5, pig 4; floors 4+: panda 4, spider 4; floors 7+: horse 3; floors 10+: penguin 5, reindeer 3; floors 13+: eagle 4 |
| Quests | all 18 (rat, chicken, dog, cat 6; goose, pig, sheep 5; boar, capybara, panda, penguin 4; spider, parrot, eagle, reindeer 3; tiger, horse, lion 2; the dragon stays the rarest at 1) |
| The pirate raid | parrot 4 (the Bomb Egg stays the usual find) |
| The regatta | capybara 3, penguin 3, parrot 2 (the Shell Egg stays the usual find) |
| Ravebridge's story quests | rat 6, chicken 5, dog 4, sheep 4 (Lumbridge's rats, chickens and sheep pen; the Practice Dummy Egg stays the usual find) |

**Built in phase 1**

- **Library** (RavenNest.Combat): the 18 species (`PetSpeciesList.Animal`, model keys `pet:Pets/Animals/<prefab>`,
  `BattlePets.Animals`), the five skills (`BattlePets`), the egg tables (`PetEggs`), tests (`Animals_widen_the_roster`).
- **Server**: nothing to write. `GameData.EnsurePetEggs` makes an egg item for every hatched species at startup, with
  the fixed id the library gives it (price 500 + 100 per Taming level), and every egg roll reads the library's tables.
- **Website**: /pets and the Pets tab read the species from the library. Only the five skills' icons were added
  (`PetLook`). A missing picture shows a paw until phase 2.
- **Game**: models load by the library's key, so nothing is mapped in the game. `PetClipPlayer` now finds the pack's
  clips: an idle named "..._Idle_Breathing" or "Dog-GreatDane_Idle" was thrown out for having "eat" in it, "_To_"
  clips are skipped like " to " ones, a model with no attack clip barks or shakes when it casts (the dogs, the
  penguin), and "fly" is a way to move for a model with no run or walk. `tools/items/egg-icons.py` knows the 18 eggs'
  shells and skips a species without renders.

**Phase 2, in the editor** (per species below)

1. **Prefabs** in `Assets/Resources/Pets/Animals/<name>.prefab`, named exactly as the model keys (the pack's own
   prefab names). Make each from the pack's prefab under `Assets/Low Poly Animated Animals/- Prefabs/Animals/` and
   keep only the model, its renderers and the Animator with its controller: take the pack's scripts (wander, sounds),
   AudioSources, the NavMeshAgent and colliders off, so the build does not pull the pack's sounds in through Resources
   (the game strips them at runtime anyway). Root scale 1; the game scales to the stage's height. Resources and the
   pack are gitignored, so the prefabs live on the dev PC like the cute series.
2. **Materials**: only the three dogs need a fix (their old materials point to the missing shader): give them the URP
   ones from `Assets/polyperfect/Low Poly Animated Animals/Materials/Animals/` (`Dog_Chihauhau`, `Dog_GoldenRetriever`,
   `Dog_GreatDane_Brown`). Everything else already uses URP Lit. `Cat_Orange` has no material set on its prefab (it
   takes the model's own): check that it is orange. Like the cute series, smoothness 0 and no highlights may suit the
   game's look better; judge in a close-up.
3. **Clips**: nothing to set up; the prefab's own controller holds them, and the clip player picks by name:

| Prefab (model key) | Material | Controller | Idle, move, attack, knocked out |
|---|---|---|---|
| Panda | Panda | CTL_Panda | Panda_Idle, Panda_Run, Panda_Attack, Panda_Death |
| Boar | Boar | CTL_Boar | Boar_Idle_Left_Right, Boar_Run, Boar_Attack, Boar_Death |
| Horse_Brown, Horse_Grey, Horse_Black | Horse_Brown, Horse_Grey, Horse_Black | CTL_Horse | Horse_Idle, Horse_Run, Horse_Attack, Horse_Death (not `Horse.prefab`: legacy, broken) |
| Dog_Chihuahua | **Dog_Chihauhau (swap)** | Chihuahua (old folder) | Dog_Chihuahua_Idle, _Run, _Bark as its cast; no death clip, it lies on its side |
| Dog_GoldenRetriever | **Dog_GoldenRetriever (swap)** | GoldenRetriever (old folder) | Dog-GoldenRetriever_Idle, _Run, _Bark; no death clip |
| Dog_GreatDane | **Dog_GreatDane_Brown (swap)** | Dog-GreatDane (old folder) | Dog-GreatDane_Idle, _Run, _Bark; no death clip |
| Capybara | Capybara | CTL_Capybara | Capybara_Idle, _Run, _Attack, _Death |
| Sheep | Sheep | CTL_Sheep | Sheep_Idle, _Run, _Attack, _Death |
| Sheep_Wool | Sheep Wool | CTL_Sheep (the plain sheep's clips: check they move the woolly rig) | Sheep_Idle, _Run, _Attack, _Death |
| Tiger | Tiger | CTL_Tiger | Tiger_Idle_Breathing, Tiger_Run, Tiger_Attack, Tiger_Death |
| Eagle | Eagle | CTL_Eagle | Eagle_Idle, Eagle_Run, Eagle_Flying_Attack, Eagle_Death (it runs on the ground and attacks with a flying clip: look at it) |
| Goose | Goose | CTL_Goose | Goose_Idle, _Run, _Attack, _Death |
| Parrot | Parrot | CTL_Parrot | Parrot_Idle, _Run, _Attack, _Death (it walks; Parrot_Fly is there if flying looks better) |
| Rat | Rat | CTL_Rat | Rat_Idle_2, Rat_Run, Rat_Attack, Rat_Death |
| Lion_Female | Lion_Female | CTL_Lion_Female | Lion_Female_Idle, _Run, _Attack, _Death |
| Lion_Male | Lion | CTL_Lion_Male | Lion_Male_Idle_Breathing, _Run, _Attack, _Death |
| Spider_Green, Spider_Yellow, Spider_Red | Spider_Green, Spider, Spider_Red (old folder, URP Lit) | Spider (old folder) | Spider_Idle, Spider_Walk, Spider_Attack, Spider_Death 3 |
| Penguin | Penguin (old folder, URP Lit) | Penguin (old folder) | Penguin_Idle, _Run, Penguin_Shake as its throw, _Death |
| Reindeer | Reindeer | CTL_Reindeer | Reindeer_Idle, _Run, _Attack, _Death |
| Cat_Black, Cat_Orange, Cat_White | Cat_Black, (the model's own), Cat_White (old folder, URP Lit) | Cat (old folder) | Cat_Idle, Cat_Run, Cat_Attack, Cat_Death |
| Pig | Pig | CTL_Pig | Pig_Idle_Breathing, Pig_Run, Pig_Attack, Pig_Death |
| Chick | Chick | CTL_Chick | Chick_Idle1 or 2, Chick_Run, Chick_Attack, Chick_Death |
| Hen | Hen | CTL_Hen (an override of CTL_Rooster) | Hen_Idle, Hen_Run, Hen_Attack, Hen_Death |
| Rooster | Roaster (the pack's spelling) | CTL_Rooster | Rooster_Idle_3, Rooster_Run, Rooster_Attack, Rooster_Death |

   30 prefabs in all. The clip names were read from the controllers and the models' import settings as text, and the
   picks worked out with the clip player's rules outside the editor; the Tiger's, Pig's, Rooster's and Goose's idle and
   walk states could not be named that way and are the likely ones.
4. **Pictures and egg icons**: `tools/items/egg-renders.cs` (play mode) renders every egg species at each stage, then
   `python tools/items/egg-icons.py <renders>` writes the egg icons to `Assets/Resources/Items` and the website's
   `imgs/items`, and the pets' pictures to the website's `imgs/pets`:
   - Egg icons (18, the same file in both places): `panda-egg.png`, `boar-egg.png`, `pony-egg.png`, `pupper-egg.png`,
     `capybara-egg.png`, `lamb-egg.png`, `tiger-egg.png`, `storm-eagle-egg.png`, `goose-egg.png`, `parrot-egg.png`,
     `rat-egg.png`, `lion-egg.png`, `spider-egg.png`, `penguin-egg.png`, `reindeer-egg.png`, `kitten-egg.png`,
     `piglet-egg.png`, `chick-egg.png`.
   - Pet pictures the website shows (32): one for a species whose model only grows (`panda.webp`, `boar.webp`,
     `capybara.webp`, `tiger.webp`, `eagle.webp`, `goose.webp`, `parrot.webp`, `rat.webp`, `penguin.webp`,
     `reindeer.webp`, `pig.webp`), three for one whose model changes (`horse`, `dog`, `sheep`, `lion`, `spider`,
     `cat`, `chicken`: `<id>.webp`, `<id>-1.webp`, `<id>-2.webp`). The script also writes `-1` and `-2` pictures for
     the others; the website does not use them.
5. **Look at each one in the game** (a critical close-up, a fight, a knockout): the heights per stage are first
   guesses (`StageHeights`, in metres), a wide model is sized by its length, and the spiders and the eagle are the
   ones most likely to need another number.
6. Rebuild `RavenNest.Combat` and copy it to `Assets/Libraries` again if any number changes.

### Not yet done

- Phase 2 of the animals (above): prefabs, the dogs' materials, pictures, egg icons, and a look at each in the game.

