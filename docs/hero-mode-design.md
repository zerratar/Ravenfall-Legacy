# Hero mode

Phase 7 of the quests (docs/quests-design.md): the streamer takes their character into battle by hand and fights
waves of enemies on Ravebridge, while chat fights beside them, heals them, cheers them on and drags them back to
their feet. The aim is the moment the streamer goes "WTF! Let's go!" and the whole chat pulls together.

## Decided

Earlier (2026-10-25, quests-design.md):

- Hero mode is an event. The streamer can also start it outside one, and some events force it.
- The streamer plays by hand (WASD, the mouse, skills on 1 to 4, a dodge), with an action RPG HUD.
- Viewers fight automatically, as they do now, and help with commands.

With the user on 2026-10-25, for this design:

- **Camera:** both views. A top-down action RPG camera by default, and over the shoulder a key press away.
- **Arena:** Ravebridge. The waves storm the town, and the island grows with hero mode: its layout will be tuned
  once the cameras and waves exist.
- **Skills:** from the combat style. Melee, ranged, magic and healing each have a kit of four skills, unlocked and
  strengthened by that style's level, so the character's existing skills matter.
- **Defeat:** chat can revive. A fallen hero is downed, not dead, and chat has a few seconds to bring them back.
  Otherwise the run ends and the wave reached is the score.

## What it builds on

- **6c, playing by hand:** `ManualPlayerController` (WASD relative to the camera, clicking things),
  `GameCamera.FollowControlled`, and `PlayerController.Controlled`, which skips automatic play.
- **The Ravebridge event:** viewers join with `!ravebridge`, come back afterwards, and their saves keep where they came
  from. The hero is the streamer's own character or their Ravebridge character (6d).
- **Combat:**
  - damage (`GameMath.Calculate{Melee,Ranged,Magic}Damage`) and attack speed from the animation;
  - healing (`PlayerCombat.Heal`) and status effects (`StatusEffect`, the speed and attack speed multipliers);
  - enemy aggro and health bars, and damage numbers (DamageNumbersPro).
- **Art already in the project:**
  - the RPG Character Animation Pack (926 clips: attacks, specials, rolls, dodges, blocks, bow aiming);
  - ErbGameArt's AAA Projectiles (97 projectile and impact prefabs) and glowing orbs;
  - Ravebridge's goblin rigs (goblins, a shaman, a war chief, skeletons, ghosts, a rock golem, knights).

## A run

1. **Starting.** While Ravebridge is open, the streamer presses Hero mode on the Ravebridge bar (or types
   `!hero start`) and picks the hero, using 6c's character choice. A world event can force it: "the goblins storm
   Ravebridge" opens Ravebridge and starts hero mode at once.
2. **Gathering.** A 30 second countdown. Chat joins with `!ravebridge` (or `!hero`, which joins as well) and
   gathers at the castle courtyard.
3. **Waves.** Enemies come from Ravebridge's gates: the stone bridge, the wooden bridge, the swamp and the desert
   fence. They go for the hero first and anyone close second. A wave ends when its last enemy falls, then comes a
   short breather (15 s), where the hero's health and mana refill a little. Every fifth wave is a boss wave.
4. **Getting harder.** Each wave brings more enemies, tougher ones and new kinds. Enemy numbers and health also
   scale with how many viewers fight, so a big chat is a blessing and a challenge.
5. **The end.** The hero is downed and nobody revives them in time, or the streamer leaves. The run's score is the
   wave reached; kills and time break ties. Everyone gets their rewards, and Ravebridge stays open afterwards.

## The hero

- **The character.** The hero keeps the character's own levels and gear. On top, a hero aura scales their health,
  damage and armour to the waves. A level 30 streamer and a level 900 streamer both get a fair fight; the levels
  decide which skills are unlocked and how strong they are.
- **Health and mana.** Health as always, and mana for skills. Mana refills slowly by itself and faster between
  waves. Chat's cheers top up both.
- **Potions.** A healing flask with charges, refilled each boss wave.

### Controls

| Input | Top-down | Over the shoulder |
|---|---|---|
| Move | WASD, relative to the camera | WASD, relative to where the hero faces |
| Aim | The mouse on the ground | The centre of the screen |
| Basic attack | Left mouse, toward the cursor (hold to keep attacking) | Left mouse |
| Skills | 1 to 4 | 1 to 4 |
| Dodge | Space: a roll toward the movement, a moment of invulnerability, 3 s cooldown | Space |
| Potion | Q | Q |
| Switch style | Tab (a short cooldown) | Tab |
| Switch camera | V | V |
| Turn the camera | Right mouse (and scroll to zoom) | The mouse while right mouse is held |
| Leave | Esc (asks first) | Esc |

### The cameras

- **Top-down (the default):** a fixed high angle (about 55 degrees) following the hero, with scroll to zoom. The
  view can be turned with the right mouse, but it never turns by itself. With the cursor free it suits a streamer,
  who also has to read chat and click the HUD.
- **Over the shoulder:** behind and above the hero. Holding the right mouse turns hero and camera together, so the
  cursor stays free otherwise. It is more cinematic, for the boss.
- Both keep the hero clear of walls and roofs (castle walls and houses fade out when they hide the hero).

### Skill kits

Four skills per style. They unlock at the style's level 1, 30, 100 and 250, and grow stronger with the level. The
basic attack is the style's own. Each skill costs mana and has a cooldown. The skill's look uses the animation and
effect packs above.

| Style | 1 | 2 | 3 | 4 |
|---|---|---|---|---|
| Melee (attack, strength, defense) | **Cleave**: a wide arc in front | **Leap slam**: jump to the cursor, damage and stun on landing | **Whirlwind**: spin while moving, hitting all around | **War cry**: enemies around turn on the hero, the hero takes less damage, viewers nearby hit harder |
| Ranged | **Multishot**: a cone of arrows | **Arrow rain**: an area at the cursor, hit over a few seconds | **Snare trap**: thrown at the cursor, holds enemies in place | **Rapid fire**: attack speed doubled for a few seconds |
| Magic | **Fireball**: a projectile that bursts | **Frost nova**: freezes everything around the hero | **Chain lightning**: jumps between enemies | **Meteor**: a short delay, then a huge blast at the cursor |
| Healing | **Mend**: heals the hero and viewers in front | **Smite**: holy damage, double against the undead | **Sanctuary**: a circle at the cursor that heals allies and hurts enemies inside | **Guardian angel**: revives all downed viewers nearby, and the next time the hero falls they stand back up |

The elements (fire, frost, lightning, holy) fit the v2 idea of elements, resistances and weaknesses
(v2-overhaul-equipment-stats); hero mode can start with plain damage and gain the elements with v2.

## Chat

Viewers on Ravebridge fight the waves automatically: the nearest enemy near the courtyard, preferring the one
attacking the hero. Their characters can fall too. They stand up again at the castle after a short wait, and keep
their place in the run.

| Command | What it does |
|---|---|
| `!hero` | Joins (like `!ravebridge`) and fights the waves |
| `!hero heal` | Heals the hero (healers; others heal the hero a little with a bandage, on a cooldown) |
| `!hero taunt` | Pulls enemies off the hero onto oneself, with a defense bonus for a few seconds |
| `!hero cheer` | A small boost for the hero (damage, attack speed, a bit of mana) that stacks with other viewers' cheers; one per viewer every 30 s |
| `!hero revive` | While the hero is downed: counts toward bringing them back |

`!ravebridge follow` and `heal` from 6b work in hero mode as they do on Ravebridge.

### Reviving the hero

- **Downed:** at zero health the hero is downed, not dead. A big countdown appears on screen: 20 seconds.
- **Reviving:** enough viewers type `!hero revive` (a share of those fighting, at least 3), or a healing viewer
  next to the hero channels for 3 seconds.
- **Back up:** the hero stands up with half health.
- **Each revive is harder:** the next one needs more viewers and the countdown is shorter, so the drama grows over
  the run.
- **Not revived in time:** the run ends.

## Enemies and waves

- **The kinds:**
  - goblins (warriors, archers that keep their distance, shamans that heal and shield goblins);
  - skeletons and ghosts from the graveyard;
  - rock golems (slow, hard, a charge);
  - knights later.

  Each kind is an enemy rig from the island with its own small behaviour.
- **Enemy AI:** today enemies only fight back when hit. Hero mode enemies seek the hero (and anyone close) on their
  own, surround them, and some have a skill: a goblin archer's volley, a golem's charge, a shaman's shield.
- **Bosses, every fifth wave:** the goblin war chief (calls more goblins, a ground slam), the tormented soul
  (teleports, fears), the swamp horror (a new rig: poison pools)... Each boss has a big health bar and two or three
  phases.
- **Waves are data** (StreamingAssets/HeroMode/waves.json): which kinds, how many, from which gates, and how it
  scales with the wave number and the number of fighters. That makes them easy to balance and to grow.

## The HUD

The usual HUD steps aside: the town bar, the stream quest bar and the island tour. The Ravebridge bar shrinks to a
line. The hero mode HUD is UI Toolkit, in the game's style:

- **Bottom left:** a health orb. **Bottom right:** a mana orb.
- **Bottom centre:** the skill bar. Four skill slots with keys, cooldown sweeps and mana costs, the dodge and potion
  slots, and the style (Tab to switch).
- **Top centre:** the wave counter and timer, and the boss bar when a boss is out.
- **Right:** the top helpers. Damage, healing, revives and cheers by viewer, so chat sees itself.
- **On screen:** damage numbers (DamageNumbersPro), the revive countdown in the middle when downed, and a
  "WAVE 10" banner between waves.
- **Settings and potato mode:** screen shake and effect intensity are settings, and potato mode turns them down
  (settings-follow-potato-mode).

## Score and rewards

- **The run's score:** the wave reached (kills and time break ties). The streamer's best run is kept on the server,
  and a stream leaderboard follows.
- **Viewers:** experience for each wave they fought in, in their combat style (like raids).
- **The hero:**
  - a reward chest at the end that grows with the wave (items, coins);
  - the streamer's titles at wave 10, 25 and 50 (the server's title unlocks).
- **Quests:**
  - stream quests can ask for a wave reached;
  - story chains can end in a hero mode fight (the runestone's guardian, Ravebridge's goblin raid).

## How it is put together

New pieces, next to the Ravebridge ones:

- **`HeroMode`:** the run. An `IEvent` inside the Ravebridge event, with its states: gathering, a wave, a
  breather, downed, ended.
- **`HeroController`:** the hero's input on top of ManualPlayerController. Aim, basic attack, skills, dodge, potion,
  switching style.
- **`HeroSkill` data:** cost, cooldown, cast time, animation, effect prefab, and how it lands (projectile, area,
  self, dash). Effects hit enemies through physics queries on the enemy layer. Damage goes through `GameMath`, with
  the skill's multiplier and the hero aura.
- **`HeroCamera`:** the two views, the switch between them, and fading what hides the hero.
- **`WaveDirector`:** reads the waves, spawns enemies at the gates (reusing pooled enemies) and scales them.
- **`HeroEnemyAI`:** seeking, surrounding and the kinds' skills. It hooks into EnemyController.
- **`HeroHud`:** the HUD above, and `HeroRevive`.
- **The animator:** an override layer for the hero's actions (skills, rolls, dodges) from the animation pack.
- **The server:**
  - the streamer's best run, and a leaderboard endpoint;
  - rewards (wave experience, the chest) through the server, as raids and dungeons do;
  - titles as streamer unlocks.

## Phases

1. **7a, the hero:**
   - the HeroController;
   - both cameras;
   - the basic attack and the dodge;
   - the melee kit;
   - the health and mana orbs and the skill bar, against Ravebridge's goblins.
2. **7b, waves:**
   - the WaveDirector and waves data;
   - enemies seeking the hero;
   - a boss;
   - downed and revive;
   - the score.
3. **7c, everyone's kit:**
   - the ranged, magic and healing kits;
   - switching style;
   - chat's commands and the top helpers.
4. **7d, the event:**
   - starting from the bar and `!hero start`, and forced starts;
   - rewards and the reward chest;
   - titles;
   - the best run and the leaderboard on the server;
   - quest hooks.
5. **7e, the island for hero mode:** Ravebridge reshaped with the cameras and waves in hand. Gates, the courtyard
   as the stand, roads with the road tool, sight lines.

## Open questions

- **Mana, or cooldowns only?** Proposed: both. Mana gives the orbs meaning and lets chat's cheers matter.
- **How long a run goes:** endless waves (the score is how far), or a set number (say 30) with a final boss?
  Proposed: endless, with the boss waves as milestones.
- **The hero aura's numbers:** how strong the hero is against the waves, and how that scales with viewers.
- **How big the rewards are**, so hero mode is worth it without beating raids and dungeons for experience.
- **The Ravebridge character:** can it be the hero (proposed yes), and does it get its own rewards (its own items
  and experience, never the server's)?
- **Later:** hero mode on other islands, or co-op (a second hero, for a guest streamer).
