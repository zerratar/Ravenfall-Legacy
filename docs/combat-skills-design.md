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

- **Skill trees,** one per combat skill. A tree holds **actives** (skills used in combat on a cooldown) and
  **passives** (always on: more armour, more crit, longer burns, cheaper threat...).
- **Skill points** come from combat levels and are spent in any tree. A node also needs a level in its
  tree's skill: "Whirlwind" needs Strength 120, so points alone do not open a tree you never trained.
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
- **A combat guide** at `/combat`: how a blow lands, a calculator (hit chance, max hit, armour), elements,
  resistances, every magical attribute with its cap, threat, crowd control and the trees. Its numbers come from
  RavenNest.Combat, so it follows balance changes.

Still open: the character page's tab has not been seen signed in; taunt and the healing skills want a real group of
players to be judged; Elements and Hunter look strong in the simulator.
