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
  high level players: Taming climbs quickly towards their other levels. It may be too quick; watch it.
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
  tradeable. Each character a dungeon, raid or tower chest rewards has a chance at one: dungeons 3% plus 1% a tier,
  raids 3%, tower chests 4%; then an egg from that source's table by weight (the dragon is about 2% of a raid's eggs).
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

### Not yet done

- Eggs from quests and events.

