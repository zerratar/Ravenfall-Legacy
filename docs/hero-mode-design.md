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

Later with the user (2026-10-25, after trying 7a):

- **Hero mode is a way to play, not the waves.** The streamer plays Ravebridge as an action RPG hero with no waves
  running: walking around, fighting what is there, talking to the NPCs and doing their quests. The waves ("A run"
  below) are one quest among many on offer during hero mode, started like any other.
- **The screen of an action RPG:** C opens the character's details (stats, with the equipment part showing, which
  the panel's own button can still fold away); I opens an inventory window of its own, where a right click equips
  or swaps an item and a tooltip shows the item's details. The players list gives way to a list of who is on
  Ravebridge.
- **The orbs are 3D:** a shaded glass sphere of liquid that moves, not flat circles.

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

The wave quest ("Defend Ravebridge" or similar), one of the quests on offer in hero mode:

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
2. **7b, waves, as a quest:**
   - the WaveDirector and waves data, started as a quest from an NPC (or a forced event);
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

## Built

### 7a, the hero (2026-10-25)

- **Starting:** while playing by hand on Ravebridge, Hero mode on the Ravebridge bar makes the character the hero.
  Leave hero mode (or Esc) goes back to playing by hand.
- **`HeroController`:**
  - WASD relative to the camera; the left mouse attacks the enemy at the cursor, or the nearest one toward it;
  - 1 to 4 for the melee kit; Space dodges (5 m, invulnerable, 3 s cooldown); Q drinks a potion (35% of health,
    3 charges);
  - mana: 100, 4 a second.
- **The hero aura:** a fixed half of the damage taken for now; 7b sets it from the waves. The kills of skill hits
  count as any other (`PlayerCombat.Defeated`).
- **`HeroSkill`, the melee kit:**
  - Cleave (an arc, x1.6);
  - Leap slam (to the cursor, 9 m at most, x2 and a 1.5 s stun where the hero lands);
  - Whirlwind (3 s spin, x0.7 every 0.4 s, walking allowed);
  - War cry (enemies within 10 m turn on the hero, half damage for 6 s; the buff for viewers waits for 7c).

  They unlock at melee level 1, 30, 100 and 250 (the average of attack, strength and defense).
- **`HeroCamera`:** top-down (55 degrees, scroll to zoom 8 to 28 m) and over the shoulder, with V gliding between
  them and the right mouse turning either. The hero's own name tag and health bar hide while hero mode runs. Fading
  roofs and walls is still to come.
- **`HeroHudView`:**
  - the health and mana orbs;
  - the skill bar (icons, keys, cooldown sweeps, mana costs, unlock levels, dodge and potion), where a refused
    skill flashes red;
  - the town bar, the shortcuts and the stream quest bar step aside.
- **The animator:** `tools/hero/hero-animations.cs` adds the hero's moves to the player animator from the RPG
  Character Animation Pack: roll, cleave, leap and land, a looping spin and its end, war cry and drink.
- **Tested in play mode** with a level 999 bot:
  - the skills hit, kill and give experience;
  - the leap moves the hero and the dodge makes it untouchable;
  - the potion heals;
  - both cameras render.

  Real keyboard and mouse input, and the HUD's look, still need a run by hand.

### After 7a: fixes, and the rest of the kits (7c's first part, 2026-10-25)

After the user's first try ("not playable yet"):

- **Moving:**
  - WASD steers at once: a short path ahead plus the agent's velocity. Velocity alone did nothing (an agent with
    no path drops it), which was the "stuck" the user saw;
  - the left mouse walks to the ground under the cursor, and holding it keeps walking toward the cursor;
  - an enemy under the cursor is attacked;
  - a ring marks the place (`HeroMarker`, the Hero Marker shader): gold for walking, red under an enemy.
- **The weapon:** the style's own weapon shows (the woodcutting axe stayed in hand before).
- **The screen:**
  - the players list and the details window step aside, and come back after;
  - C opens the hero's details with the equipment part showing (its own button still folds it);
  - I opens the inventory window (`HeroInventoryWindow`, on the right, draggable): worn gear slot by slot, then the
    backpack as a grid of icons. A right click equips an item or takes a worn one off: through the server for the
    streamer's characters, on this computer for the Ravebridge character. Hovering shows the item's tooltip (shared
    with the details window), with what a too-high item needs. Esc closes it;
  - the party panel on the left lists who is on Ravebridge, with their health.
- **The orbs:** the Hero Orb shader draws a glass sphere into a render texture each frame (every third in potato
  mode). It has a waving surface that sloshes when the level moves, swirling clouds and rising bubbles, and a paler
  trail that shows what a hit took. The glass has a rim light, a highlight and a glint, all inside the gold rim.
- **The camera:** zoom the same way round as the other cameras, the right mouse tilts as well as turns, over the
  shoulder zooms (2.5 to 10 m), and no zooming while the mouse is over the HUD.
- **The Ravebridge bar:** a drawer at the top that slides up a few seconds after the mouse leaves it. A small tab
  stays at the top edge with who is there, the time and any quest notice, and it brings the drawer back.
- **Grass on Ravebridge:** its two terrains have grass fields with the main island's profile. `GrassRenderer` grows
  on the field whose terrain the camera is over; before, it only ever used the first.
- **Styles:** Tab switches (Shift+Tab goes back; 1 s between switches). Each style has its kit, weapon, task (so
  experience goes to that style) and its own cooldowns:
  - ranged: Multishot, Arrow rain, Snare trap, Rapid fire;
  - magic: Fireball, Frost nova, Chain lightning, Meteor;
  - healing: Mend, Smite (double on the undead, by name), Sanctuary, Guardian angel (heals everyone near in full,
    and the next fatal blow leaves the hero at half health).

  Healing has no attack of its own in the game, so the healer's basic attack is a bolt of holy light (10 m). Damage
  uses the style's formula (`GameMath`). The look comes from `HeroEffects` (AAA Projectiles, made by
  `tools/hero/hero-effects.cs`), and new animator moves: shoot, volley, cast, area cast, buff.
- **Tested in play mode** with the streamer's character:
  - steering 6 m/s, at once, stopping dead;
  - style switching, and the skills firing and killing;
  - the shader orbs, the drawer and its tab, the markers and the projectiles on screen.

  Real keyboard and mouse input still need a run by hand.

### 7b, waves as a quest (2026-10-25)

- **The quest:** Captain Hargrove stands by the castle's east gate (`tools/hero/hero-places.cs`, which also places
  the wave gates). His quest "Hold the line" asks for five waves, through a new `defend` step in quests.json and
  `RavebridgeQuests`. Talking to him at that step starts the run, and turns on hero mode first if needed. Falling
  short leaves the step open. Once the quest is done, he offers endless runs and says the character's best.
- **`WaveRun`:**
  - **Waves:** an 8 second count, then each wave comes in from the two gates nearest the hero (stone bridge, wooden
    bridge, desert gate, swamp, mill road). When every gate is over 70 m away, they come from a ring around the
    hero. A wave has 3 + 2 per wave enemies, plus some for the viewers there, with at most 12 alive at once. Between
    waves there is a 12 second breather, with a quarter of the health and 40 mana back. Every boss wave refills the
    potions.
  - **The enemies:** copies of Ravebridge's goblin rig (one rig, many variants), reused when they fall. Raiding
    goblins come from wave 1, goblin warriors from 3, skeletons from 6, swamp ghosts from 7, skeleton knights from
    11. They hunt the hero (`EnemyController.Hunting`: they never lose interest) and give experience as any enemy.
  - **Bosses:** every fifth wave, in turn: Warchief Grubnash, the Swamp Golem, the Tormented One, Sir Rattlebones.
    Each shows a big health bar.
- **Fair at any level:**
  - an enemy's health is a number of the hero's own average hits (`HeroController.ExpectedHit`): 2.5 for a goblin,
    30 to 50 for a boss, growing with the wave;
  - its blow is a share of the hero's health (`WaveRun.BlowFor`, through `HeroController.AdjustTaken`, now told who
    hit): about 1% for a goblin and 5 to 6% for a boss, growing with the wave, once a second at most per enemy.
- **Downed and revive:**
  - at zero health in a run the hero is downed, not dead: lying down, with no moving or skills;
  - enemies turn on viewers close by;
  - chat has 20 seconds: `!rb revive` from a share of the viewers on the island (30%, at least 1 and at most 5),
    or a healer standing within 12 m for 5 seconds. With nobody else on the island the run ends after 4 seconds;
  - back up with half health, untouchable for a moment;
  - the guardian angel still saves the hero once before that.
- **Viewers:** those training melee, ranged or magic on the island are sent at the nearest wave enemy. Healers heal
  as they do. `!rb follow` and `!rb heal` work as before.
- **The end:** reaching the goal, falling, or leaving hero mode or Ravebridge ends the run. The hero gets full
  health back. The score is the waves held. The best is kept per character in PlayerPrefs for now (the server's
  leaderboard is 7d). Chat hears the start, being downed, the revive and the result.
- **The HUD (`HeroWaveHud`):**
  - at the top: the wave (of the goal), what is left and fallen, the count to the next wave, the boss's bar;
  - while downed: "DOWN!", the count, and the votes;
  - after a run: the result for 10 seconds.
- **Tested in play mode** with the streamer's character and four bots:
  - the captain's quest and the start;
  - waves 1 to 5 with the goblins coming and hitting, the breathers, and the boss;
  - downed, then revived by two votes;
  - the goal reached, the quest done and the endless offer;
  - the result panel.

  The quest progress and the best were reset afterwards.

### 7c, chat's part (2026-10-25)

The ranged, magic and healing kits and switching style came with the fix round. 7c adds chat:

- **`!hero`** (both bots send it as `!quest hero`):
  - alone, it joins Ravebridge and fights beside the hero (follow, or heal for healers);
  - `heal` heals the hero. Healers (healing level 20, or training healing) heal properly every 10 s; anyone else
    throws a bandage (3% of the hero's health) every 30 s. The heal flies from the viewer when they are close;
  - `taunt` turns up to three enemies on the hero (within 15 m) onto the viewer for 6 seconds, from anywhere on
    Ravebridge. The viewer takes half the damage meanwhile. Once every 30 s;
  - `cheer` adds 5% to the hero's skill damage for 20 seconds, stacking up to ten, plus 5 mana. Once every 30 s
    per viewer. The normal attack is not boosted yet;
  - `revive` is the same as `!rb revive`.
- **Revives get harder:** each one makes the next need one more vote (never more than the viewers there) and
  gives 4 seconds less (20, 16, 12, at least 8). Chat's messages now say `!hero revive` and the real count.
- **`HeroHelpers`:** per viewer, the damage dealt on Ravebridge while hero mode runs, the healing the hero got
  (their attacks, heals and `!hero heal`), revives (the voters, or the healer who did it), cheers and taunts.
  Cleared when hero mode starts.
- **`HeroHelpersHud`:**
  - the top helpers on the right (five, best first);
  - a feed under the wave line of what chat just did, fading after 6 seconds;
  - the cheer bonus by the style.
- **Not built:** the "defense bonus" of a taunt is the half damage; viewers who fall stand up the usual way
  (their own respawn), not at the castle.
- **Tested in play mode** with four bots:
  - cheers stacking (+10%) and the cooldown;
  - a heal;
  - a taunt pulling 3 enemies 52 m to a bot;
  - the feed, the cheer bonus and the helpers panel.

  The commands' chat replies go through the bots and are untested live.

### 7d, starting, rewards and the leaderboard (2026-10-25)

- **Starting (`HeroStart`):**
  - the Hero mode button on the Ravebridge drawer, as before;
  - `!hero start` from the streamer (and `!hero end`): opens Ravebridge if it is closed and unlocked, brings the
    streamer's character, and makes it the hero once it is on the island;
  - forced: the RavebridgeForced event with `Storm` (server `api/admin/ravebridge-force?storm=true`) opens
    Ravebridge, locked or not, and goes straight into hero mode and an endless wave run. "The goblins storm
    Ravebridge!".
- **Viewers:** every wave held gives the viewers fighting on the island experience in their combat style (factor
  2 + half the wave), as a kill does.
- **The reward chest:** a run of five waves or more. The server's `api/game/{waves}/hero-chest` gives items from
  the dungeon drop table, its tier from the waves (5 common, 10 uncommon, 15 rare, 25 epic, 40 legendary). The hero
  gets one plus one per ten waves (five at most) and the top three helpers one each. It is not a dungeon, so there
  is no dungeon quest progress, and it can be opened once a minute per session. The Ravebridge character gets coins
  instead (100 a wave), as its items are its own.
- **The record (server `api/quests/hero/run/{waves}/{kills}/{seconds}`, `HeroModeManager`):**
  - each streamer's best, kept as the user property `hero_best`;
  - the titles at waves 10, 25 and 50 (streamer unlocks `title_hero_10/25/50`: "the Gatekeeper", "the Shield of
    Ravebridge", "the Unbreakable"), announced in chat;
  - the best's rank among every streamer's.
- **The leaderboard:** `api/quests/hero/leaderboard` gives every streamer's best (ten), cached in the game
  (`WaveRun.Board`) when hero mode starts and after a run. Captain Hargrove names the top three. The result panel
  shows the rank, new titles and the chest.
- **Quest hooks:** the waves held count as the stream quest objective `hero_wave` (`QuestTracker.HeroWaves`). A new
  stream quest "Hold 10 waves on Ravebridge in hero mode" is offered only to streamers who have Ravebridge
  unlocked. Story chains ending in a hero mode fight are still to write.
- **Tested in play mode:**
  - `!hero start` with Ravebridge locked says so;
  - the storm opened Ravebridge, brought the character, made it the hero and started a run;
  - five waves held: the server ranked it #1, the chest gave an item, and the leaderboard and result panel showed
    it.

  Not tried live: a title (wave 10), the stream quest, and the chat replies.

### 7e, Ravebridge for hero mode, and the orbs again (2026-10-25)

- **Sight lines (`HeroCamera`):** ten times a second, anything tall between the camera and the hero is drawn as its
  shadow only while it is in the way (a capsule of 2.5 m: whole roofs, wall walks and towers, not just one plank).
  People, enemies and the ground stay. It is a cut, not a fade: the buildings' shaders have no dithering.
- **The gates and the courtyard (`tools/hero/hero-places.cs`):**
  - each wave gate has goblin spike barricades on both sides of the path the waves walk (found from a navmesh path
    to the courtyard) and a brazier with fire on whichever side has ground;
  - the courtyard, the stand, has battle banners at the east gate, two braziers and the captain's arrow barrels and
    crates;
  - the fires are Synty's meadow campfire. PolygonDungeon's FX_Fire has no material and rendered magenta.
- **The waves (`WaveRun`):** the gates count up to 130 m away (the courtyard's nearest are 87 and 117 m). Enemies
  charge in at 6.5 m/s until 20 m from their target, then fight at their own pace. A purple puff marks where each
  one comes in. The HUD says where the next wave comes from ("From the stone bridge and the swamp").
- **Deaths (`EnemyMovementController`, every enemy on the Humanoid Enemy controller):** its `Die` is a trigger, and
  `SetBool` on it did nothing, so enemies just vanished. Now a trigger is fired as a trigger. The hide follows the
  death clip until it ends, not a fixed timer. Wave enemies refresh their renderer list after their rig variant
  changes, so the right meshes hide.
- **The orbs, after the user's references and `docs/Unity-Glass-Orb-Kit`:**
  - no frame, medallion or label box any more: the glass sphere, the number, and "HEALTH" / "MANA" under it;
  - `HeroOrb.shader` follows the kit. A view looking a little down at a unit sphere, with the ray's part under the
    liquid's surface integrated in 12 steps (emission and absorption, Beer-Lambert). The liquid scatters its colour,
    glows in its currents (the kit's flow noise at two scales) and core, and burns in the caustics low down. The kit's
    liquid body art adds depth, and a few motes drift up. The surface disk, its glowing band at the front glass, and
    the light spilling onto the glass above it are part of the liquid;
  - the glass is see-through: a faint tint, with the kit's glass art (weaker at its bright edge) and a thin rim added
    as light;
  - the blending is a UI Shader Graph (`Assets/UI/Shaders/HeroOrbLight.shadergraph`). It is URP's UI template with
    its demo tints white and the blending set to Premultiply (One, OneMinusSrcAlpha), on the orb element through
    `style.unityMaterial`. The orb's render texture holds premultiplied light and coverage, so its light adds over
    the world and the empty glass lets it through;
  - the render texture is half float and linear: 8-bit linear banded the dark tones.
- **Tested in play mode:**
  - a storm run from the courtyard (the waves came from the stone bridge and the swamp);
  - a goblin's death playing out before it hid;
  - the fires;
  - the sight line cutting a wall walk;
  - the orbs at several fills over the game.


### The fight's feel, after the user's second try (2026-10-25)

The user's list: Ravebridge took the streamer's character along unasked, a card's text ran out of the window, the
drawer covered the stream quest, clicks attacked too slowly, Space did nothing, enemies hurt while chasing, and the
camera hid stairs and bushes. Gatebound (the user's other game) was the model for the fight.

- **Ravebridge:** the debug start no longer joins the streamer's characters; "Who plays?" opens as Ravebridge
  opens. The top column (the village bar, the stream quest) steps down under the drawer (`rf-rb-open`,
  `rf-rb-open-full` on the documents). Card lines wrap.
- **The click attack (`HeroController.Attack.cs`):**
  - a swing starts the moment the button goes down, at the enemy under the cursor or, with Shift, while WASD
    moves the hero, or over the shoulder, toward the cursor even into empty air (aim assist turns it to an enemy
    within 40 degrees close by). An enemy out of reach is run to first;
  - holding the button keeps swinging; a click mid swing is buffered. Three swings make a combo per weapon kind
    (2H sword, 2H axe, spear, sword, axe: the RPG pack's attack clips), the third hitting 35% harder; it resets
    after 1.2 s;
  - the hero is rooted while swinging. The hit lands halfway, on up to five enemies in a 120 degree arc within
    the weapon's reach (spear 3.4 m, two-handed 2.9 m, else 2.4 m);
  - a hit stops the hero's and the struck enemies' animation for 0.05 to 0.12 s (not the game: the world is
    shared), shakes the camera a little and leaves a short white trail from the blade's tip (`HeroTrail.mat`);
  - bow, magic and healing shoot on the click instead: an arrow, a lightning bolt or holy light.
- **The roll:** Space pressed early in a swing waits until the swing may be cut (35% in) instead of being lost. A
  wall or the navmesh's edge shortens the roll; a roll with no room flashes its slot. Invulnerable for 0.5 s.
- **Enemies:** a blow lands only if the target is still within 1.25 times the reach (plus 0.6 m) when it comes
  down, and a hero-mode enemy stands still until its swing is over. Stepping or rolling away during the wind-up
  makes it miss. A wave enemy caught on a wall corner (the towers have a few) is moved 2 m along its path after
  2 s of getting nowhere.
- **Stances:** every hero move (swings, roll, casts) goes back to the held weapon's fighting idle, not the plain
  idle, which drew the weapon again after each swing. Healing uses the magic staff's stance and the Action
  parameter is held at 1, since the game's staff idles start their training cast loop on their own.
- **Sight lines (`HeroCamera`):** three thin rays to the hero's head, chest and feet. Only what crosses them, and
  only pieces tall enough to hide the hero, goes shadow-only, together with its small group (up to three parents
  under 16 m). Stairs and bushes beside the line stay.
- **Sounds (from Gatebound's packs, in `Assets/Sounds/Effects/<Kind>`, unversioned like the other sounds):**
  swing whooshes just before the hit, flesh slashes (a heavy one for a finisher or a kill), the bow, arrow hits,
  electric and fairytale casts for magic and healing, the roll's whoosh, potion and heals, goblin grunts, yelps
  and deaths, and the thud of a blow on the hero. The hero counts as the watched player, so they are always heard.
- **Tested in play mode:** "Who plays?" opening with the character left where it was; the top column at 108 px
  under the open drawer; the card wrapping; a three-swing combo and each style's stance and attack; a 40 s wave
  fight (no enemy moved while swinging, all the sounds played); rolling away from a wind-up 6 of 6 missed,
  standing 6 of 6 hit; the gateway arch hidden over the hero with the walls and statues beside it kept.
- **Not checked by hand:** the feel with a real mouse, the trail's look, and the hit stop at full frame rate.

### The user's third try (2026-10-26)

- **Ravebridge play:** a click shows the marker (the ground, an enemy, a tree, rock, fish, farm or NPC). The top
  column's room under the drawer is measured from the bar and the tab, so it clears a bar whose text takes two lines.
- **Hero clicks:**
  - enemies are picked on screen (the body nearest the cursor), not by colliders: an enemy's large trigger sphere
    caught clicks meant for another;
  - an enemy out of reach is run to a spot well inside the reach (a swing used to start at its edge and miss); the
    one swung at is hit wherever it stepped while still in reach, and the hero turns with it until the blow lands;
  - from above a clicked enemy is fought on until it falls (RuneScape); over the shoulder every swing is the
    player's. A walk, WASD or a click elsewhere stops it;
  - a click on a tree, rock, fish, farm patch or NPC uses the character's own way of doing it
    (`ManualPlayerController.Interact`); a swing, a roll, a skill or a walk ends it and brings the hero's stance back.
- **The bow:** Hero-Shoot is `2Hand-Bow-Attack4` now. The Aiming-Fire clip draws side on: the arrow's line (string
  hand to bow hand) was 85 to 89 degrees left of where the character faced. Attack4's is within 5 at the release.
- **Enemies on Ravebridge** amble within 4 m of their spawn now and then at 1.6 m/s; one the hero goes for holds
  still. Elsewhere they keep their places.
- **Animator fixes (tools/hero/hero-animations.cs):** putting the axe away went straight back to chopping whenever
  Action was 1, so the axe chopped and sheathed in a loop (hero mode holds Action at 1; likely the "two-hand axe
  stance with a staff or bow" seen before). The bow's idle and run had no way out of the fight, so a bow user could
  not go on to chop, mine or fish.
- **Controlled characters** are never "unstuck" to the island's spawn by the player validator.
- **Tested in play mode:** the drawer room open and up; 20 Ravebridge enemies ambling; a click 8 m from a goblin
  running in and the first swing landing; 25 s of auto attack on one goblin (about 45 hits, then nothing after it
  fell); the bow's arrow line; chopping a tree in melee and bow style and the stance coming back.
- **Not checked by hand:** the click marker in Ravebridge play and the screen picking with a real mouse.

### Loot, bound to Ravebridge (2026-10-26)

The user's call: loot kept on this computer for now (a server-side "Ravebridge only" flag can come later).

- **Data:** `StreamingAssets/Ravebridge/loot.json`, like the quests: items (name and description in each language, a
  Font Awesome 7 icon, a rarity from common to legendary, a kind, a worth in Ravebridge coins) and drop tables per
  enemy. A wave enemy drops by its kind's model (`WaveRun.LootKey`, "boss " in front for a boss; its name is in the
  streamer's language), the island's own enemies by their names. `any` adds to every table.
- **The bag** is part of the character's Ravebridge state (`RavebridgeQuests.State.Coins` and `.Loot`): in the
  Ravebridge character's file, or `states/ravebridge/quests-(character id).json` for the streamer's own characters.
  Never sent to the server, never in the character's inventory. Saved a few seconds after a change, when hero mode
  ends and when the game closes.
- **Drops (`LootDrop`):** a hero's kill (`HeroController.Strike`) tosses coins (a coin prop), items (a pouch) and
  potions (a bottle) out of the enemy onto the navmesh. They hover and turn; from uncommon up a beam in the rarity's
  colour rises from them, taller and wider for the rarer. Their names show over them (`LootLabels`). Walking over
  one picks it up; coins come to the hero from 3.5 m. A click on a drop walks to it. Unclaimed drops go after two
  minutes; past 80 on the ground the oldest go straight into the bag.
- **Feel:** coins clink when they land, pickups have their own sounds (coins, leather, a bottle), and a rare or
  better drop plays a loot jingle with a sparkle. "+12 coins" and item names rise from the hero in their colours.
- **Healing draughts** fill the hero's potions while there is room, the rest go into the bag; Q drinks from the bag
  once the hero's own are gone, and the Q slot shows them ("3/3 +2").
- **The inventory window (I)** shows the Ravebridge bag above the backpack: the coins, the loot framed in its
  rarity's colour with tooltips (rarity, amount, worth, "Only on Ravebridge"), and the island quests' items.
- **Tested in play mode:** goblins killed in hero mode dropping coins, ears and teeth; walking over every drop
  picking it up; the file written; draughts filling potions and Q drinking from the bag; wave goblins dropping by
  their model key; rare and legendary beams; the window.
- **Next, not built:** a Ravebridge merchant for the coins and materials, quests that ask for loot, and gear that
  works only in hero mode.

### The shopkeeper's cart (2026-10-26)

Ravebridge's shopkeeper ("Come back when the cart arrives") finally has his cart: talking to him offers "Show me your
wares", which opens his shop (`RavebridgeShop`, `RavebridgeShopWindow`).

- **Buy** with Ravebridge coins: healing draughts (25), a whetstone (15% more damage for 5 minutes, 60) and a hearty
  stew (15% less damage taken for 5 minutes, 60), which stack in time; hero mode upgrades that stay: a bigger potion
  belt (one more potion, three levels: 300, 750, 1500), light boots (half a second off the roll, 500, 1200) and a lucky
  charm (a tenth more coins from drops, 400, 1000). And a bucket and a cooking pot for 1 coin each.
- **Sell** the loot in the Ravebridge bag at its worth, one or all of a kind. Quest items are not for sale.
- Everything is in `StreamingAssets/Ravebridge/loot.json` ("shop"). Upgrade levels are kept with the bag
  (`RavebridgeQuests.State.Upgrades`); buffs last while the game runs. Buffs on now show over the skill bar.
- The window closes when the character walks away from the shopkeeper or stops playing. Purchase and sale sounds
  from Gatebound.
- A bag change now starts the save tick by itself (before, a purchase with nothing dropped was only saved when hero
  mode ended).
- **Tested in play mode:** talking to him, the button opening the shop, every kind of purchase and its effect
  (damage, potions 4/4, roll 2.5 s), refusals when too poor, selling, upgrades still there after a restart, the save.
- Village smoke, flames and glows fade out near the camera now (`_NearFade` 4 m): rising to hero mode's low camera, a
  puff right at the lens filled the screen.
