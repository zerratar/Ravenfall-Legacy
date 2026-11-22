# Combat skills: skill trees, points and the active loadout

Part of [combat-v2-design.md](combat-v2-design.md). Players get combat skills that they use **automatically**,
on cooldowns, everywhere they fight: islands, raids, dungeons, the arena, Ravebridge and the tower.
Ravenfall is played from chat, so nothing here needs a player to press a button in a fight; they choose
*what* their character can do, and the character does it.

Decided with Zerratar (2026-11-06):
- Roles come from where the points go, not from a class picked up front. Tank, healer, damage, or any mix.
- Points are limited, so nobody has every skill and upgrade at once; changing your mind costs a respec.

---

## 1. The shape: trees, points, a loadout

- **Skill trees,** one per combat skill (and Beastmaster for Taming, see the end of this document). A tree holds **actives** (skills used in combat on a cooldown) and
  **passives** (always on: more armour, more crit, longer burns, cheaper threat...).
- **Skill points** come from combat levels (and, since 2026-11-21 and 2026-11-22, from Taming's levels and the nine
  trade skills', each counted apart at its own rate: `trade-skills-design.md`) and are spent in any tree. A node also
  needs a level in its tree's skill: "Whirlwind" needs Strength 120, so points alone do not open a tree you never
  trained.
- **The loadout:** up to **4 active skills** in combat, picked from the actives you have unlocked, plus your
  basic attack. Passives from all trees always apply.
- **Respec** gives all points back for coins (section 5).

### Why one pool of points, and not a kind of point per skill

A kind of point per skill (Defense levels give Defense points, and so on) sounds natural, but a player with
everything high would get every point of every kind and could fill every tree, which is exactly what we want
to avoid. One shared pool, with each node gated by its skill's level, keeps the choice: you earn enough to go
deep in one or two trees, or wide across several, never everything.

**How many:** about one point per 10 total combat levels early on, slower later (one per 40 after the first
1,000 levels). A character with every combat skill at 999 has about **280 points**; all trees together cost
about **750**, so even the strongest player fills well under half. A new player gets their first points
within the first hours.

(As built on 2026-11-22: the eight combat skills at 999 earn 274, Taming 34 more, the nine trade skills 97 more, 405
in all, against eleven trees that cost 957. Still one pool: the trade trees are paid from it too.)

## 2. The trees

Eight trees, each built the same way: about 20 nodes in four tiers, unlocked at that skill's level
1, 100, 300 and 600. Each tree has 4 or 5 actives and multi-rank passives. The lists below are the
starting set; numbers come from the simulator.

**Defense: Bulwark (the tank)**
- Actives: *Taunt* (forces enemies near you to attack you for 4 s), *Shield Wall* (-40% damage taken for 6 s),
  *Shield Bash* (stun 1.5 s, needs a shield), *Rally* (nearby allies take -15% damage for 8 s).
- Passives: more Armor, more threat, block chance with a shield, less damage while below half health,
  "Last Stand" (once per fight, survive a killing blow with 1 HP).

**Healing: Restoration (the healer)**
- Actives: *Mend* (big single heal), *Renew* (heal over time on up to 3 allies), *Barrier* (a shield that
  absorbs damage), *Cleanse* (removes poison, burn and slows), *Sanctuary* (heals everyone near over 6 s).
- Passives: more healing power, heals also give a small shield, cheaper threat from healing, faster casts,
  "Smite" basic attacks deal extra light damage to undead.

**Strength: Might (big melee hits)**
- Actives: *Cleave* (hits up to 3 enemies in front), *Crushing Blow* (a slow, huge hit), *Whirlwind*
  (hits everyone around you), *Leap* (jumps to a far enemy and stuns briefly).
- Passives: more melee power, bleed on crits, more damage against enemies below 30% health.

**Attack: Precision (accuracy and crits)**
- Actives: *Expose Weakness* (the target takes +15% damage from everyone for 6 s), *Flurry* (three quick hits),
  *Disarm* (the target hits 30% softer for 5 s).
- Passives: more accuracy, crit chance, crit damage, attack speed.

**Ranged: Marksman**
- Actives: *Multishot* (3 targets), *Arrow Rain* (area over 4 s), *Snare* (roots enemies in an area),
  *Piercing Shot* (ignores 50% of armour), *Rapid Fire* (double attack speed for 5 s).
- Passives: more range power, crit chance, element arrows (a share of ranged damage becomes the gem's element).

**Magic: Elements (the caster)**
- Actives: *Fireball* (fire, area), *Frost Nova* (ice, freezes enemies near you), *Chain Lightning*
  (lightning, jumps 4 times), *Toxic Cloud* (poison, area over time), *Meteor* (big fire area, long cooldown).
- Passives: per element (longer burns, more chill stacks, more jumps, more poison stacks), cast speed,
  lower resistances on enemies you hit.

**Health: Vitality (staying alive, for everyone)**
- Actives: *Second Wind* (heal yourself 25%), *Iron Skin* (immune to crowd control for 4 s).
- Passives: more max HP, regeneration in combat, life steal on hits, more healing received.

**Slayer: Hunter (bosses and big enemies)**
- Actives: *Mark Prey* (+20% damage to the marked enemy for the group), *Execute* (huge damage to an
  enemy below 20%).
- Passives: more damage to bosses and elite enemies, more loot luck in raids and dungeons, longer boss
  stun windows.

Utility nodes outside combat (for example the chance to keep a gem when removing it, see the gems
document) sit in a small **Artisan** tree, gated by Crafting. It uses the same point pool, so a player who
spends points there has fewer for combat.

(As built: until 2026-11-22 there was no Artisan tree and its one node, Gem Salvager, sat in Hunter. Since then
Artisan is one of the two **trade trees** (Gathering and Artisan, `trade-skills-design.md`), shown on their own
Trade tab, and Gem Salvager is there, opened by Crafting 300. They use the same point pool, as this paragraph
said, and the nine trade skills earn into it; the combat level does not count them.)

## 3. Using skills automatically

Each active has a **cooldown** (4 to 60 s) and a **use rule**. Players do not press anything; the
character checks its loadout every time it is free to act:
- There is a short shared pause between skills (1.5 s), so a character does not fire everything in one
  second.
- **Use rules** come with sensible defaults per skill: heals when an ally is below 60%, area attacks when 3 or
  more enemies are near, taunt when an enemy is attacking someone else, burst skills on bosses and elite
  enemies, cleanse when an ally has a harmful effect.
- **Priority:** the order of the loadout is the priority, top first. Players reorder it (section 6).
- **No mana.** Cooldowns are the only limit, which keeps it readable on stream. (Hero mode keeps its own
  mana for the streamer's hands-on play.)
- Skills from other styles work if you have what they need: a heal needs nothing, *Shield Bash* needs a
  shield, *Multishot* needs a bow equipped. This is what makes a healer-tank or a melee fighter with a
  volley possible.
- Experience is unchanged: the skill you train gains experience as today. Using skills does not give extra
  experience, so there is nothing to farm by spamming them.

Hero mode's code supplies most of this: the skill shape (unlock, cooldown, a run function), the cooldown
gating, area, arc and nearest targeting, stuns and hit feel.

## 4. Roles in groups

With threat (see the overview, section 4), a group plays better with a tank holding the boss, healers
keeping people up and damage on the boss. Nothing forces a group to have them; raids and dungeons scale
as today, and harder content (tower floor bosses, later raid tiers) is where roles start to matter.

## 5. Respec

- Gives every point back. Allowed anywhere except during a raid, dungeon, tower boss or duel.
- Costs coins: 1,000 x points spent, so a small change early is cheap and a full rebuild late is a decision.
- **Free once after each balance update** that changes the trees, so nobody is stuck with a build we broke.
- A first free respec for everyone when skill trees arrive.

## 6. Setting it up: chat and the website

Chat is fine for the loadout, not for a tree of 160 nodes:
- `!skills` shows your loadout, cooldowns and unspent points; `!skills <name>` explains one.
- `!loadout 1 cleave` puts Cleave in slot 1; `!loadout clear 3` empties a slot.
- `!respec` asks for confirmation and the price.
- **The website** gets a skill tree page on the character page: the trees side by side, points to spend,
  the loadout with drag to reorder, and the use rule per skill. A link from `!skills` opens it.
- The game shows the loadout on the player details window, with cooldowns.

## 7. On stream

- Skills show their effect (hero mode's bursts, small and quick) and the skill's name floats up briefly
  above the character when used, at most one at a time per character.
- Damage numbers keep their current look; elements colour them (fire orange, ice blue, lightning yellow,
  poison green) and immune hits show "Immune".
- All of it follows the potato mode and settings toggles, like other effects.

## Decided (2026-11-07)

- **The fifth loadout slot** is a node: *Battle Rhythm* in the Vitality (Health) tree, tier 3 (Health 300), 5 points.
  Health is the one combat skill everyone trains (it gains a third of all combat experience), so any build can
  reach it.
- **Health and Slayer keep their own trees** (Vitality and Hunter, as above).
- **Respec costs coins** (1,000 per point spent), and a **Tome of Forgetting** from quests gives one free respec.
  The free respecs after balance updates and when the trees arrive stay.

## Progress (phase 2)

Built 2026-11-07, on master and the dev database only (nothing deployed until v0.9.7):
- **Library** (RavenNest.Combat `SkillTrees.cs`): the eight trees, 69 nodes (actives with cooldown, targeting, use
  rule, gear and effects; passives as gear stats per rank), tiers at the skill's level 1, 100, 300 and 600, costs that
  grow with the tier (all trees about 580 points; every combat skill at 999 earns 274), Battle Rhythm (fifth slot),
  Last Stand, `SkillBuild` (ranks and loadout as text, the checks for learning and slotting). 21 passive stats joined
  the gear stats (power, accuracy, armour and health bonuses, block, cast speed, healing received, boss, execute and
  undead damage, low health reduction, bleed on crit, heal shield, combat regen, burn bonus, resistance shred, loot luck,
  crowd control duration).
- **Server**: `CharacterSkillBuild` (a table made at startup), `SkillTreeManager` (learn, loadout, respec: free, a Tome
  of Forgetting, or 1,000 coins a point), `api/skills`, the build sent with each character (`Player.SkillBuild`), the
  Tome of Forgetting item (quests still need to give it).
- **Game**: passives count like gear stats; `CombatSkillRunner` uses the loadout by itself when a swing or heal would
  land; `CombatEffects` carries the skills' buffs, debuffs, damage and healing over time, shields, taunts, stuns,
  freezes, roots and cleanses; the skill's name floats up in gold above the name tag. Checked in play mode with bots.
- **Chat**: `!tree` (the bot's `!skills` already means `!stats`), `!tree Might`, `!tree cleave`, `!learn`, `!loadout`,
  `!respec` / `!respec confirm`, in the hosted bot and the local one (RavenBot bd82d26).
- **Website**: a Skill trees tab on the character page (points, respec, loadout slots, the trees with Learn buttons).
  Not seen in a browser yet: the local site's certificate does not cover localhost for the built-in browser.
- **Simulator** `builds`: a full single-tree build deals 1.35 to 1.5 times the damage of no build against one enemy;
  Hunter on bosses 1.75 to 1.9; Elements up to 2.0 at level 999 (Meteor and cast speed), the first to tune down if
  playtests agree.

Done since (2026-11-07):
- **Tome of Forgetting** from every weekly quest and every finished chain (the quest text names it).
- **Free respec after a balance update**: `SkillTrees.Version`; a build from before the current version gets a
  free respec the next time it is looked at (`CharacterSkillBuild.TreeVersion`).
- **When each skill is used** is the player's choice on the website (a picker per loadout slot, stored with the
  build; the default clears it); the game follows it.
- **Changes reach the game at once**: any change (website or chat) sends `SkillBuildChanged` to the game the
  character plays in.
- **The loadout in the player details window**: a chip per active with its cooldown draining and a gold edge when
  ready, the skill's text in the tooltip.
- **Area skills with a group**: three Metalons moved next to a level 47 fighter; Whirlwind fired once three were
  near, Cleave hit several, and the group turned on the fighter.

- **The skill tree map** (`wwwroot/js/rf-skill-tree.js`): all eight trees on one zoomable map, WebGL (three.js:
  glowing nodes, rank arcs, drifting motes) with a 2D canvas fallback for browsers with WebGL off; drag, pinch, wheel
  and double tap zoom; tap a node to see it and learn it. On the character page's Skill trees tab (Map and List
  views) and on the public **planner** at `/skill-trees`: set any levels, build, take ranks back, slot actives, see
  what the build gives, and share it as a link (the address carries the build and the levels). Checked in a browser
  in WebGL and 2D, desktop and phone sized. Local testing: `RAVENNEST_HTTP=1` with the Development environment
  keeps the site on plain http, for browsers that refuse the development certificate.
- **Everything on the map** (2026-11-08, Zerratar: the Learn button as a tooltip by the selection, the points and the
  respec inside the canvas, the planner as a mode instead of a page, the confusing loadout dropdowns gone, painted
  icons): `SkillTreeStage.razor` puts the points left and the respec (its price on the button, a confirm on press) in
  the map's corner; a tapped star gets its panel beside it (Learn, Take back in the planner, loadout slot buttons and
  when it is used), kept beside it as the map moves, closed by a press anywhere else or Escape; the loadout is a bar
  along the bottom with the actives' icons (a slot opens a list of the learned actives and its "used when"). On the
  character page "My build | Planner" switches to the planner on a copy of the build with the character's levels:
  levels, start over, copy link, and saved plans (a dropdown, kept in the browser). The `/skill-trees` page is the same
  planner for anyone. The map fills the window's height and keeps itself fitted while resized until it is moved;
  on a phone the planner's tools fold behind Tools and the panel sits above the loadout. Icons: 32 actives and specials,
  the eight trees' emblems and the skill point from three Image Generator sheets (`tools/website/skill-icon-sheets.py`,
  sources in `tools/website/skill-icon-sheets`), and a painted raven medallion for the centre. Not yet seen signed in.
- **Real paths and the numbers** (2026-11-08, Zerratar: Iron Skin could be learned without the Battle Rhythm it hangs
  from; the descriptions gave no figures): a node now needs one of the nodes linked to it in the tier below
  (`SkillNode.Requires`, the paths in `SkillTrees`; the first tier is open), on top of its tree's level. Ranks learned
  before the paths stay (Zerratar's choice, it is only the dev server so far). The planner cannot take back a node
  that holds up another's path. The map draws exactly these links (walked bright, open half, closed faint) and places
  each node under the ones its path comes from. Every node says its numbers (`SkillNode.EffectText`: "1% less damage
  taken a rank", "80% of a hit (up to 3 enemies in front)", and for ranked passives "2% now, 5% at 5/5"), on the
  map's panel, in the list and in `!tree <skill>`, which also says what to learn first.
- **A combat guide** at `/combat`: how a blow lands, a calculator (hit chance, max hit, armour), elements,
  resistances, every magical attribute with its cap, threat, crowd control and the trees. Its numbers come from
  RavenNest.Combat, so it follows balance changes.

Still open: the character page's tab has not been seen signed in; taunt and the healing skills want a real group of
players to be judged.

**Elements and Hunter toned down (2026-11-10):** Fireball 1.2 to 1.0, Chain Lightning 1.0 to 0.8, Meteor 4.0 to 2.8,
Big Game 4% to 3% a rank, Apex Predator 10% to 8%, Execute 4.0 to 3.0. Simulator `builds` (damage against one enemy of
the player's level, as a multiple of no build, levels 100/300/600/999): Might 1.40-1.55x, Precision 1.37-1.46x,
Marksman 1.32-1.49x, Elements 1.44-1.83x (was 1.53-2.00x), Hunter against bosses 1.68-1.76x (was 1.77-1.86x, for 43
points but bosses only). Elements still grows with level because element damage takes only half of armour's share and
armour grows with level; the simulator's enemies resist nothing, while many real ones resist an element.

---

## Beastmaster: Taming's tree (2026-11-21)

Zerratar asked whether the skill trees should grow with Taming ("it does somewhat count as a support combat skill,
its like summoner in a way") and decided: **"yes start with beastmaster and make it earn points to use"**.

- **A ninth tree, Beastmaster**, gated by **Taming** the way every tree is gated by its own skill (tiers at Taming 1,
  100, 300, 600 and 999), paid from the **same pool** as the others: points in your pet instead of in yourself is the
  summoner's trade.
- **Taming's levels earn points** at the rate of one combat skill: one per 10 levels for the first 125 (an eighth of
  the first 1,000 combat levels), one per 40 after (`SkillTrees.TamingPoints`). Taming 76 gives 7, 300 gives 16, 999
  gives 34, on top of the combat levels' points (274 with all eight at 999, 308 with Taming). It is counted apart, so
  nobody's points from combat change, and it is worked out from the level each time: a character that already had
  Taming levels has its points the next time anything looks, with no migration. Taming stays out of the combat level.
- **The whole tree costs 118 points** (Bulwark 111, Restoration 101): twelve nodes, three a tier, and the capstone.
  Three actives for the loadout. Every role of pet has something: a tank's guard, a healer's and a damage pet's
  power, a support pet through its owner's buffs, a gatherer's skills.

| Tier (Taming) | Node | Kind, cost | What it does | For |
|---|---|---|---|---|
| 1 | **Fang and Claw** | passive, 5 ranks x 1 | the pet's blows and heals 4% stronger a rank (20%) | damage, healer, support |
| 1 | **Thick Hide** | passive, 5 ranks x 1 | 5% more health for the pet a rank (25%) | tank, every pet |
| 1 | **Sic 'Em** | active, 3 | every 12 s in any fight: the pet strikes your target for 250% of its hit. Needs a battle pet out | damage, any pet |
| 100 | **Back on Its Feet** | passive, 5 ranks x 2 | a knocked out pet gets up 10% sooner a rank (30 s to 15 s) | tank, every pet |
| 100 | **Kinship** | passive, 5 ranks x 2 | you: 2% more health a rank (10%) while a pet is out and on its feet | the owner |
| 100 | **Mend Pet** | active, 5 | every 30 s when the pet is below half health or knocked out: 30% of its health back, and back on its feet | tank, every pet |
| 300 | **Trick Training** | special, 8 | one more skill slot for the pet | support, healer, gatherer |
| 300 | **Shared Mending** | special, 8 | healing you receive heals the pet by the same share of its health; what makes you attack faster or take less damage does the same for it | every pet in a group |
| 300 | **Keen Senses** | passive, 3 ranks x 3 | a gatherer pet's skills 10% stronger a rank (Forage 5% to 6.5%, Keen Nose 10% to 13%, Dig 1% to 1.3%) | gatherer |
| 600 | **Elemental Bond** | passive, 5 ranks x 4 | with the pet out, 3% of each of your hits more a rank (15%) as the pet's element; a pet without an element gives half as much crit chance (7.5%) | the owner |
| 600 | **Guardian's Oath** | passive, 10 | a tank pet takes 70% of the blows meant for you instead of half | tank |
| 600 | **Call of the Wild** | active, 10 | every 40 s on bosses: the pet attacks 50% faster and deals 30% more for 8 s. Needs a battle pet out | damage, any pet |
| 999 | **Pack Leader** | capstone, 15 | a packmate of the pet's own kind fights beside it at half strength | every pet |

Paths: Back on Its Feet from Thick Hide; Kinship from Thick Hide or Fang and Claw; Mend Pet from Sic 'Em or Fang and
Claw; Trick Training from Mend Pet or Kinship; Shared Mending from Mend Pet or Back on Its Feet; Keen Senses from
Kinship or Back on Its Feet; Elemental Bond from Trick Training or Keen Senses; Guardian's Oath from Shared Mending
or Keen Senses; Call of the Wild from Trick Training or Shared Mending; Pack Leader from any tier 600 node.

**In line with the other trees:** first-tier passives are 4% and 5% a rank (Brute Force 4%, Thick Skin 4%, Hardy 3%);
Sic 'Em is 250% of the pet's hit every 12 s, about 0.6 of its owner's hit (Cleave is 0.8 on three enemies every 8 s);
Mend Pet's 30% is Second Wind's quarter for the pet; Kinship's 10% health needs a pet out where Hardy's 15% does not;
Elemental Bond is 15% for 20 points at tier 600 and meets resistances; Guardian's Oath is one rank for 10 like Titan
and Apex Predator; the capstone costs 15 like every capstone.

### The capstone, and the second pet from the stable

The sketch's capstone was "a second pet out at once, at reduced strength". Looked at honestly, a second pet **from the
stable** is not something to bolt on:

- **The server knows one pet out per character** in six places: `GameData.GetActivePet` (the gatherer bonus), `pets.use`
  (it clears every other pet's flag), the hatch and the tame that go "straight out when none is", the woken pet whose
  item is gone, the experience report, and `Stable`'s order. A second needs a field of its own on the `Pet` row and a
  rule for each of those.
- **The game holds one controller per player**, read by 51 places: the enemies' threat and taunts, the tank's share
  of blows, the details window's chip, the cosmetic pet that hides, `!pets` and its answers, the tower's level sync,
  the experience share.
- **Both bots' `!pets` grammar** has no word for "the other one": `!pets use`, `away`, `rename`, `skills`, `slot`
  and `evolve` all mean the pet that is out. The website's stable shows one pet out, the details window one chip.
- **Questions that are the owner's:** does the second gather (two Forages)? Two tanks, one share of the blows or two?
  Which one does `!pets away` send home? Half the experience each, or the second none?

So the capstone is built as **Pack Leader: a packmate of the pet's own kind** (the same pet a second time, a little
smaller, at half strength), which keeps the fantasy (two animals at your heels and in the fight) and the weight (a
pet and a half), and touches none of the above: the stable still has one pet out, the packmate is the game's alone.
It fights the owner's target and uses the pet's skills at half strength (no taunts), follows on the owner's other
side, takes a command with the pet (Sic 'Em, Mend Pet, Call of the Wild), is nobody's target, takes no blows for
its owner, earns no experience and gathers nothing.

**Decided against, 2026-11-22** ("lets not have a second pet out, game expects 1"; Pack Leader stays: "thats just a
clone of whatever you already got equipped"). What was proposed, kept for the record: a second pet from the stable
as a later step on top of Pack Leader. `!pets second (name)`
and `!pets second off`; a `Second` flag on the `Pet` row beside `Active`; the second at half strength with its own
skills and its own knocked-out state; the experience share split in two rather than doubled; no gathering and no
guard from the second (so two gatherers or two tanks do not double); both in the details window, in `!pets` and on
the website's stable. It replaces the packmate when one is chosen and falls back to it when none is.

### Built (2026-11-21)

- **Library** (RavenNest.Combat): `SkillTree.Beastmaster`, `CombatSkill.Taming`, the thirteen nodes and their paths
  (`SkillTrees`), `TamingPoints` and `EarnedPoints(combat, taming)`. Three small additions for a pet's actives: the
  targeting `Pet` (the effects are the pet's: damage is its own blow on the target), the use rule `PetHurt` and the
  need `Pet` ("a battle pet out"). Five stats the pet reads from its owner's build (`PetPower`, `PetHealth`,
  `PetRecovery`, `PetGathering`, `PetGuard`). `SkillBuild` says what a build does for the pet (`Passives(petOut)`,
  `PetElementBonus`, `ExtraPetSkillSlots`, `HealsReachPet`, `HasPetTwin`). Tests.
- **Server**: Taming opens the tree and earns the points (`SkillTreeManager`); a pet's slots count Trick Training
  (`BattlePetManager`: the slot command, filling, and `pets.list` fills a new slot or empties one that a respec took
  back); Keen Senses makes the gatherer bonus stronger. A respec and a Tome of Forgetting cover the tree like any other.
- **Game**: the pet reads its owner's build whenever the pet or the build changes (`BattlePetController.Refresh`):
  power, health, knockout time, the tank's share, the slots it may use, Shared Mending. The owner's own stats follow
  the pet (`PlayerController.UpdateEquipmentEffect`: Kinship and Elemental Bond count with a pet out and on its
  feet, and are worked out again when it comes, goes, falls or gets up). The actives run through the loadout like
  any other (`CombatSkillRunner`: the pet does what the skill says, `BattlePetController.Command`). The packmate is
  a second controller for the same pet (`PlayerBattlePets.Twin`).
- **Chat**: `!tree beastmaster` (or `!tree taming`), `!tree sic 'em`, `!learn`, `!loadout` and `!respec` work as
  for every tree; the texts say "needs a battle pet out" instead of "equipped", and `!tree` says Taming earns points.
- **Website**: the map has nine wedges (the script takes any number; on a phone the names by the top met, so the one
  nearest the pole steps outwards), the planner has a Taming level and counts its points, old shared links with
  eight levels still open, the stable tab counts Trick Training's slot, /combat and /pets explain the tree. The
  emblem and five icons are stand-ins made from the game's own pet pictures (`tools/website/beastmaster-icons.py`);
  **wanting drawn icons later:** the tree's emblem, Sic 'Em, Mend Pet, Call of the Wild, Trick Training, Shared
  Mending.
- **Older games**: a 0.9.6 game has no skill trees at all (it reads no build and has no handler for the build
  event), so Beastmaster nodes do nothing there, like every node. A 0.9.7 test build from before the tree drops the
  node ids it does not know when it reads a build and keeps the rest (a test). The game never writes a build back;
  the server is the one that saves.

### Tested

- Library: 133 tests pass (five new: the points, the tree's shape and gate, the texts, what a build carries, unknown
  nodes).
- **The test character** (zerratar: Taming 76, a Disco Raven support pet out) through the real chat handlers with
  the game cut off from the bot: 252 points (245 from combat levels, 7 from Taming, no migration); `!tree beastmaster`
  and `!tree taming` list the tree; two ranks each of Fang and Claw and Thick Hide took the pet from 870 to 957 health
  and its power to x1.08; `!learn mend pet` was refused ("needs Taming 100"); Sic 'Em went into slot 1 and was used
  three times in 45 s against Ironhill's dragons; `!respec confirm` gave all 7 points back (it took one of the
  character's Tomes of Forgetting, which the server then gave back; task and pet as before).
- **With bots and local pets** (a build put on a bot directly: the game follows a build whatever the levels, the
  server is what checks them), for the nodes from Taming 100 up: a Tiger went from 620 to 775 health and x1.20;
  knocked out, it was up after 15.1 s; its owner's health went from 4,360 to 4,796 with it standing and back while it
  lay (Kinship), and the owner's hits carried 15% fire (Elemental Bond; 7.5% crit with a Panda); a heal of a fifth
  on the owner healed the pet a fifth, and the owner attacking twice as fast halved the pet's 1.8 s (Shared Mending);
  Call of the Wild took it to x1.56 and 1.2 s; Mend Pet had it up 1.8 s after a knockout with 233 of 775; a first-stage
  Tiger had three slots instead of two (Trick Training); a Panda's share went from 0.5 to 0.7 and a blow of 1,000 on
  its owner left 300 (Guardian's Oath); the packmate was there at half health and x0.60, fought, could not be
  targeted, guarded nobody, and was gone with the build.
- **Not exercised in play:** Keen Senses and the server's side of Trick Training (the slot command and `pets.list`
  filling the slot) need Taming 300 on a real character; they compile and read right, no more. The website's
  character page signed in. Call of the Wild's "on bosses" rule against a real boss (it was put on "in any fight").
