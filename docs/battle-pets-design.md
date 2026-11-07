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

**Name candidates** (Ravenfall's skills are one plain word each: Attack, Woodcutting, Sailing, Slayer,
Enchanting, so the name should be too):

| Name | For | Against |
|---|---|---|
| **Taming** (recommended) | Short, plain, reads like the other skills, easy in chat (`!taming`, "Taming 120"). Pairs well with the clan skill *Menagerie* | Pets also come from eggs, which you hatch rather than tame (players will not mind) |
| *Beastmastery* | Evocative, says "pets that fight" | Long on name tags and in the stats list; strongly tied to another game's hunter class |
| *Kinship* | Warm, the bond with your pet is what grows | Not obvious what it does from the name alone |
| *Husbandry* | Accurate | Dull, and it reads as farming |

The code calls it "the pet skill" until the name is picked (`RavenNest.Combat/BattlePets.cs`).

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

**Getting pets:**
- **A starter pet for everyone** from a short quest when the skill arrives (pick one of three: a tank, a
  healer or a damage pet), so nobody starts without one.
- **Eggs** drop rarely on islands (each island its own species), from raid and dungeon bosses, from tower
  floors and quests. An egg hatches over time while you play.
- **Eggs are tradeable; a hatched pet is bound to its owner.** Buying an egg is buying a chance at a
  species, not buying a levelled pet.

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

**Name candidates:** *Menagerie* (recommended), *Kennels*, *Pet Lore*.

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

- The name: Taming (recommended), Beastmastery, Kinship or something else?
- Should the cosmetic pet stay visible next to a battle pet, or does the battle pet take its place while out (as above)?
- Can a battle pet wear the look of a cosmetic pet you own (a "skin"), so favourite pets can fight too?
- How many species at launch, once the tiered models are counted?
- **Pets of high level players start far behind.** The pet's level is capped by the pet skill, which starts at 1
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
  level opens even when it already has another (it only checked clans with none). So *Menagerie* can be added as a
  row in the `Skill` table with its `RequiredClanLevel`.

**Still waiting:** the species list (models), the `BattlePet` table and `!pet` commands, the pet skill's column and
attribute index (needs the name), the game's battle pet controller on top of `PetController`, the website's pets
page, and the catch-up question above.
