# The tower in the main world

The tower that stands in Ravenfall's sea (docs/tower-worlds-proposal.md section 6): a colossal landmark on its own
islet, a stack of sections that each look like a floor's world, lit by the stream's progress, with the entrance plaza
at its foot. The owner picked concept A (`docs/mockups/tower/world/a-islet.jpg`) with concept C's plaza
(`c-entrance-plaza.jpg`) on 2026-11-24, and asked above all for the feeling that it is massive: "it should after all
be the sense that it is massive given the big worlds inside".

Built 2026-11-24 for v0.9.7, in two rounds the same day, and a third on 2026-11-25. Round 1 stood it at (405, -60) between the four eastern
islands; the owner then asked for it further off ("want it to feel like its a huge thing in the distance, well not too
far away") and took every open call. Round 2 moved it, made it bigger, took its top into the sky's own clouds, gave
it a look pass, the camera's looks at it, the islet as a place players sail to, the stream's best climbers on the
stone and as the statue, and a key that takes the camera to the tower and back. Round 3 finished the statue (the
climber's gear in a heroic pose for every kind of weapon, a name plaque), rebuilt the stone as the tower stele the
owner picked (design C), took the top's dithered fade out for a smooth one into the cloud and the sky, and carried
the sea floor on south of the world's terrain so the sea round the islet has no straight edge behind it.

Captures of it in the game, taken in play mode on the local server: `docs/mockups/tower/world/built3-*.jpg` (round 3),
`built2-*.jpg` (round 2; the lists are at the end) and `built-*.jpg` (round 1, the old place).

## Where it stands and how big it is

| | |
|---|---|
| Place | (60, -700) in the main world, sea level -3.31: out in the open sea south of Home, beyond Heim and Away |
| Scale | 1.2 (`scale` in `stack.json`; the models are made at 1 and everything below is as it stands in the world) |
| Base | 192 m across at the wall (radius 96), the rock round it about 280 m |
| Height | 367 m of sections (108 + 84 + 74 + 101), then the shaft goes on to 559 m and fades out between 402 and 548 m, inside the sky's bank of cloud and just over it |
| For scale | the islands are 200 to 300 m across; the highest ground in the world is Ironhill's peak, 56 m over the sea |
| Gate | 16 m wide and 26 m high, under a 41 m front; a character is 1.8 m |
| Faces | north, to (0, -100): the gate, the plaza and the pier look at Home's south shore |

How far it is (from the shore nearest to it, at eye level; round 1 in brackets):

| From | Now | Round 1 |
|---|---|---|
| Home's south shore, below the town | 516 m | (309 m from the east shore) |
| Home's middle | 712 m | (386 m) |
| Heim's east point | 255 m | (780 m) |
| Away's south shore | 330 m | (270 m) |
| Atria's west cliffs | 480 m | (210 m) |
| Kyo | 765 m | (770 m) |
| Eldara | 950 m | (270 m) |
| Ironhill | 990 m | (515 m) |
| War | 1.9 km | (1.5 km) |

Why there. The owner's words rule out the old place: it stood inside the ring of islands, 300 m off Home's beach,
and from the beach it filled the screen (45 degrees high). The new place is the open sea to the south: nothing lies
behind it, so from Home it stands alone against the horizon with Heim's white hills to its right and Away's to its
left, 1.85 times as far from Home's middle as before. At 516 m its sections stand 35 degrees high and 21 degrees
wide from Home's shore: still more than half the height of a level camera's view, with sky round it. It is
scaled up by a fifth so the distance costs it less: the same model at the old size would stand 31 degrees. The
shape is unchanged (as wide for its height as before), so it is no needle.

It is not twice as far from everything: Heim and Away are nearer than Home (255 and 330 m), and from their south
shores it is as big as it was from Home in round 1. That is the price of standing clear of the ring without going
out past the edge of the world's sea: the sea chart, the ocean's mesh and the far terrain end 700 m south of Home's
latitude, and the tower's middle stands at that edge with its back to the open ocean, where no boat goes.

**The other candidate**: (-350, 650), in the wide water north-west between Kyo, Ironhill and War, 760 m from Home's
middle. It would be far from every island that matters (640 m from Kyo and Ironhill) and reads as "the far north".
Against it: from Home it stands behind Ironhill's peak and Kyo's hills for most of the shore, the ferry never comes
near it (a stop would add a kilometre to the ring), and it sits on the line to War and Ravebridge. To try it, set
`position` to `[-350, 650]` and `faces` to `[0, 100]` in `tools/tower/world/stack.json` and run the steps under
"Scripts" again; the ferry's stop then needs `FerryAfterPath` and `FerryNext` set for that side of the ring
(`world-tower-build.cs`).

The ferry's ring passes 280 m north of the tower's middle on its way from Heim to Atria, 70 m from the pier's end:
the stop at the pier is a short turn off the ring, taken only when someone is bound for the islet or waiting on it.

## What it is made of

- **Models** (headless Blender, `tools/tower/world/world_tower.py`): one FBX per floor's section, the shaft over
  them, and the islet with the plaza, in `Assets/World/Tower/World/Models`. Flat shaded; everything a face needs is
  in its vertices (color, how much it glows, its kind), so the whole tower is drawn with **one material** and no
  texture. Each part exists twice: `_Near` and `_Far` (low detail, for far away and potato mode).
- **Shader** `Ravenfall/WorldTower` (`Assets/World/Tower/World/WorldTower.shader`): stone, the windows and runes
  that light up, falling water, cloud, green fire, embers, lightning. It also solves the distance and the top: see
  below.
- **Scene** `Assets/Scenes/Tower/WorldTower.unity`, made by `tools/tower/world/world-tower-build.cs`. The game loads
  it beside the main world at the start (`WorldTowerScene` in `Assets/Scripts/Game/Tower/WorldTower.cs`), in the
  editor by its path and in a build by its name. **MainWorld holds nothing of the tower**: the islet's island, its
  dock and the ferry's way to it live in this scene too and are made known to the game when it loads.
- **Code**, all `partial class WorldTower` in `Assets/Scripts/Game/Tower/`: `WorldTower.cs` (light, distance, the top,
  potato mode), `WorldTowerVisit.cs` (the islet as a place), `WorldTowerMoments.cs` (the camera's looks),
  `WorldTowerClimbers.cs` (the stone's names and the statue), `WorldTowerStone.cs` (the stone's carved tower) and
  `WorldTowerCamera.cs` (the tower camera key).
- **Synty props** dress the plaza (the builder places them): the hero's statue (Fantasy Kingdom, given a pale stone
  material of its own), the banners and the bowls on the wall's posts (Dungeon), the big braziers and the ivy
  (Ancient Empire), palms, bushes, ferns and flowers (Nature Biomes, Tropical Jungle), the pier (Vikings), barrels
  and crates (Pirates), the sign boards (Polygonal Fantasy Pack). Not Synty: the big masses on the tower itself
  (trees on the terraces, ice, cloud, spires: a prop 100 m up would vanish at the view distance, and they are the
  tower's outline), the islet's boulders, and the flames in the braziers (the tower's own flame model, so they show
  from the other islands and need no particle system).

### The sections

| Floor | Section | Height (m over the sea) | What shows |
|---|---|---|---|
| 1 | The Overgrown Halls | 0 to 108 | mossy stone in courses with dark joints and blocks standing proud, a grassed terrace on corbels with trees and a hedge, bushes growing out of the wall, vines, blossoms, six waterfalls (two tiers), the sun sigil in gold |
| 2 | The Frozen Vault | 108 to 192 | blue stone under snow, sheets of ice grown over the wall, ice crystals up to 48 m in 17 clusters, snow on every buttress head, icicles, snowed-in pines, frozen falls, the snowflake in blue |
| 3 | The Storm Spire | 192 to 266 | weathered dark stone, lightning rods, a turning ring of thundercloud low round it with lightning, rocks adrift, the gale sigil |
| 4 | the Dark Fortress | 266 to 367 | slate-blue stone flaring out over the storm: 16 fins that end in points, a lip with long and short spikes, tiers of window slits, two crowns of battlements, eight turrets at two heights with collars, teeth and spires, iron fire bowls, a keep with horns (dark until v0.9.8 gives the floor: see "Lit by the stream's progress") |
| - | the shaft | 367 to 559 | the tower goes on: ledges, rings of dark windows and ribs every 46 m, fading out into the sky's cloud and the sky between 402 and 548 m |

Every section has three or four rings of windows, a rune band with its sigil at the four winds, and steps in from the
one below, so the terraces show.

### The look pass (round 2)

Against concept A, round 1 fell short in three places; what was done:

- **The fortress was a dark blob.** Its stone is now slate (0.27, 0.28, 0.35 and two neighbours) rather than near
  black, so its shapes take the sun and read against the sky; the shapes themselves were rebuilt (the list in the
  table above). While the floor is not reached its windows are dark glass and its fires are out, but each iron bowl
  holds **embers**: a dull green coal that glows at about a fifth of a lit fire's strength, with no flame. From the
  islands that is eight faint green points on a dark section: the sign of what waits there. "Dark means not reached"
  stays clear because a lit section is another thing altogether: every window, the rune band and the sigil shine, and
  the fires stand tall.
- **The stone was flat.** Every wall is laid in courses now: each block has a dark backing 16 cm behind its face (the
  joint), one block in a few stands proud with sides of its own, and every block's color is weathered by height, by
  its row and by chance (darker and greener low down and under ledges, paler where the sun and wind reach).
- **Too little growth and ice.** The Overgrown Halls got bushes on the wall, a hedge round the terrace and the roof,
  blossoms and more vines; the Frozen Vault got ice sheets, more frozen falls, snow on the buttress heads, crystals
  on the ledges and five more clusters.

What it costs is under "Potato mode and cost".

## Seen from every island

The game's camera sees 650 m (the View distance setting takes that from 32 m to 1300 m, and potato mode cuts it to
100 m); most of the world is further from the tower than that. The
shader's vertex stage pulls everything that lies beyond nine tenths of the view distance in toward the camera along
its own line of sight, into the last tenth of the view. On screen nothing moves (a point stays on its ray), only its
depth is squeezed, in the same order, so the tower hides itself correctly, an island between the camera and the tower
still hides its foot, and the far plane never cuts it. It follows the view
distance of whatever camera draws it, so it needs no setting and works in potato mode. With it:

- `WorldTower` gives the tower's renderers bounds that are never culled, and switches their shadows off beyond
  320 m (the game draws shadows 75 m out, so nothing would receive them);
- the game's haze reads the squeezed depth, so the shader adds the rest of the air between there and the tower's real
  place, with the Haze setting's own numbers (nothing is added when haze is off): at half a kilometre the tower is
  a shade paler and bluer than the islands in front of it, which is what says it is far and large;
- what is under the sea is cut away when squeezed (the far sea writes no depth to hide it);
- the tower is not drawn beyond 3.6 km (the tower's own floors and towns lie 6 km out), nor from down in a dungeon.

## No top: into the sky's own clouds

Round 1 hid the top under a low-poly cloud of its own, which read as a flat lid next to the sky's volumetric clouds.
Round 2 let the shaft go on into a bank of the sky's own cloud and thinned it out by a dither, which showed as dotted
speckle against the sky and from the islands, and the bank sat on the tower like a hat. Round 3 (2026-11-25):

- **The sky keeps a bank of cloud there.** `SkyClouds.Tower` (set by `WorldTower`, read by the cloud shader as
  `_CloudTower`) raises the cover to full and thickens the body within 340 m of the tower from about 360 m up,
  whatever the weather. Its rim is torn by a slow noise, and so is its foot: the underside rises toward the rim
  (70 m higher at the edge than at the shaft) and is pushed up and down by a second noise, so from below it is a
  mass of cloud the shaft goes into, thick at the shaft and thinning out round it, not a flat-bottomed disc.
- **The shaft fades out smoothly, with no dither.** From 402 m to 548 m over the sea the shader blends the shaft
  toward what is behind it, eased so most of it is gone by the top of the cloud layer (480 m): low down, inside
  the bank, toward the cloud's own color in the light of the moment; from 463 m up toward the sky itself, read
  from the live sky (`_RavenfallSky`, the probe `SkyReflection` keeps) in the very direction it is seen in, with
  the game's haze worked in (the haze pass hazes the shaft by its depth but the sky only near the horizon, so the
  shader asks for the color that comes out as the sky's once the haze is on it). What is wholly faded is not drawn,
  so it hides no cloud behind it. Under a full overcast (a deck of cloud over everything) it fades into the cloud's
  color all the way. The model ends at 559 m.
- **What is seen**: the fortress's crown and turrets stand clear, the shaft rises some 35 m over them and goes into
  the white (`built3-top-from-home.jpg`, `-from-heim.jpg`, `-night.jpg`). From 330 m over Home it goes into the
  bank and nothing stands over it (`built3-top-above-home.jpg`).
- **Potato mode, or Clouds off**: no sky clouds, so the same fade runs straight into the sky from 402 m up
  (`WorldTower.Fade`): the shaft rises out of the crown and melts into the sky, with no cloud of its own any more
  (`built3-top-potato.jpg`, `-clouds-off.jpg`). The low-poly cloud of round 2 (`Lid_Far`) is gone.
- The storm ring stays: it sits at 200 to 260 m, well under the bank, and still reads as the Storm Spire's own
  weather.

What was tried and dropped in round 3: a shaft going on to 991 m that turned to the sky's color over several hundred
metres. From the islands it worked, but from the overview camera over Home it stood over the bank as a long pale
column with an end to it (the sky probe and the rendered sky do not match closely enough over that length). The
short shaft hides its end inside the bank and the last 70 m over it.

## Lit by the stream's progress

`WorldTower` reads `TowerManager.State` (what the server last said; the game invents nothing) twice a second:

- a floor the stream has **cleared**: its windows, runes and fire glow steadily;
- the floor the stream is **on**: it glows and breathes (down to 40% and back, every 4 seconds);
- a floor **not reached**: dark glass, no fire (the fortress keeps its embers);
- before the server has answered: every floor dark;
- a floor boss falling while the game runs: the camera looks at the tower as the floor flares up (see "The camera's
  looks"), or with cutscenes off it flares at once; the next floor begins to breathe.

Each section has its own copy of the material (`_Glow`, `_Pulse`); the gate, the plaza's sigils and the braziers are
always lit. `WorldTower.PreviewStep = 21` (editor only, from an eval) shows the light for a step without a server.

## The camera's looks at the tower

The camera that follows a player looks down at 45 degrees and never shows the horizon, so on a normal stream the
tower was not on screen at all. Three short looks put it there (`WorldTowerMoments.cs`):

| Look | When | How long | What it shows |
|---|---|---|---|
| A floor falls | the stream clears a floor's last part, after the floor boss's own cutscene | 4.6 s | from the sea in front, rising toward that floor's section, which flares up as the camera settles |
| The stream enters | the tower was empty and someone goes in (to the climb or a town), at most once in 15 minutes and not within a minute of the tower's own opening cutscene | 5 s | from the gate, pulling back and up until the whole tower stands in the picture |
| Now and then | nothing is going on and nobody has touched mouse or keys for 45 s; every 12 to 20 minutes (picked anew each time) | 9 s | from wherever the camera is, it lifts a little, turns to the tower and drifts toward it |

Rules they all keep:

- Never over something else: not during a cutscene (the tower's or Ravebridge's), a raid, a dungeon, the arena, a
  war, the regatta's race camera, build mode, hero mode or a character played by hand, a tower boss countdown or
  fight, nor with the settings, the shop, What's new or a Ravebridge window open, nor while the streamer types.
- Never against the streamer: the free camera is left alone, and any key or mouse movement ends a look at once and
  gives the camera back to who or what it was on. After the tower camera key was used the looks keep off for 30 s.
- Not queued: a look that cannot start within its window (14 s after the floor fell, longer while the boss's
  cutscene still plays; 6 s after someone went in) is dropped, and the floor flares without the camera. The idle
  look tries again 30 s later.
- The camera goes back to who or what it was on. `WorldTower.Instance.PlayNow(kind, floor)` plays one by hand (the
  tests); a look played that way runs its time whatever the mouse does.
- No window is hidden and no title is put up: a look is the camera only.
- The first two follow **Settings > Game > Observer camera > Cutscenes**. The idle look has its own switch under
  it, **Looks at the tower** (on by default, `PlayerSettings.TowerLooks`), and is off in potato mode whatever the switch says. An
  idle raid, dungeon, regatta, wave run, pirate raid or treasure hunt counts as "something going on".

The interval is long on purpose: four looks an hour at most, nine seconds each, only on a stream nobody is steering.

## The tower camera key

While anyone is in the tower the camera has two sides, the tower and the world, and one key goes between them
(`WorldTowerCamera.cs`; all of it, with the order of the rules, is in docs/tower-design.md section 23). **T** unless
rebound (Settings > Keys > Camera, "Tower camera: to the tower and back"; `Keybinds.Action.TowerCamera`), a button on
the tower panel, which is on screen whenever anyone is in the tower ("World (T)", lit, while the tower's group camera
is on, "Tower (T)" otherwise), and one in the shortcut bar.

- **The tower side** is a group camera: no single player is watched, the camera stands over the middle of the group
  on the place the climb is on (a boss fight with the guardian in the picture, the climb's part, else the town or
  the busiest open part). It is where the camera goes by itself when the climb starts, after the entering look.
- **The world side** is the camera as it always was, among the players outside the tower, and it stays outside
  until the key is pressed again.
- A player the streamer picks is followed as always (in the tower or outside); T goes back to the group camera.
- With nobody in the tower the key does nothing but say "Nobody is in the tower" for three seconds.
- Whatever takes the camera for a while (a raid, a dungeon, a cutscene, the looks at this tower) gives it back to
  the side the streamer was on; when the last player leaves the tower it goes back outside.

## Potato mode and cost

| | Normal | Potato mode, or further than 1,150 m |
|---|---|---|
| Tower triangles | 123,176 | 24,779 |
| Light | steady, and breathing on the stream's floor | steady only |
| Moving | cloud ring turns, rocks bob, clouds heave, fire wavers, lightning, gulls | none of it |
| Lamps | the gate's and four braziers' point lights | off |
| Shadows | cast within 320 m | cast within 320 m |
| The top | fades into the sky's cloud bank and the sky over it | fades into the sky (no sky clouds in potato mode) |
| Camera looks | on (their settings) | the idle look and the two cutscene looks are off |

Round 1's models were 100,851 and 23,808 triangles: the look pass and the shaft added 19,538 near (the fortress's
shapes, the courses' joints and proud blocks, growth and ice) and 635 far, plus the 2,480 of the potato cloud. Round 3
added about 1,600 near for the stone (the stele's body, plinth, rune ring, plaques, laurels and crown 450, its twelve
carved tier looks 660, the rows' runes 248) and about 1,670 for the taller shaft, and took the potato cloud out. The
sea floor strip south of the terrain is 2,304 more, drawn with a plain lit material, no shadows. The budget kept: every block's backing and sides
exist only in the near models; the far models keep the silhouette (fins, turrets, crowns, crystals) and drop the
rest. The low detail models show from 1,150 m (round 1: 950 m), so every island that looks at the tower from within
a kilometre gets the near one.

The Synty props on the plaza are drawn only within the view distance like any island's.

Nothing here has a setting of its own except the idle look: the cost is triangles while it is in view, the moving
parts are shader maths and a handful of turning transforms, the fade is a blend and one sky lookup per pixel of the shaft over 402 m, and all of it follows
potato mode (`GameManager.PotatoMode`), as `TowerFx` does for the floors.

## The plaza

At the foot on the side facing Home: the gate with its tunnel of light and the tower's sun over it, a stair, the round
paved plaza with a compass (50 m across) on a round bastion, a low wall with fire bowls on its posts, stone steps down
to a landing and a wooden pier, the statue on a pedestal to the left of the stair and the best climbers' stone to
the right.

### The stream's best climbers: the stone and the statue

- **The measure**: how many of this stream's clears a character fought in. A clear is a part's guardian beaten on
  the stream's climb (each part falls once per stream, so with today's 30 parts the count runs to 30), counted from
  v0.9.7 on. Ties go to the higher part reached, then to more last blows on floor bosses, then to whose last clear
  is the older. It is the stream's own list: a character who climbs on two streams has a count on each. Why clears
  and not "furthest floor": the furthest floor is the same for everyone who was there on the day, so it ranks
  nobody; the count says who actually turns up for the climb.
- **The server** keeps it (`TowerClimber`: the stream's user, the character, clears, highest step, last blows; one
  small row per character per stream, written when a boss is won) and gives the game the first five with
  `GET api/tower/climbers`: for each a name, the three numbers, and the character's look (`TowerClimberInfo`).
  Nothing else of a player leaves the server: names and looks are what the game shows of every player already.
  The table is made by the server's own schema step at its start, like every other.
- **The stone** is the tower stele the owner picked on 2026-11-25 (design C,
  `docs/mockups/tower/world/stone-c-tower-stele.jpg`; made by `stele()` in `world_tower.py`, its words by the builder,
  its light by `WorldTowerStone.cs`): a tall dark obelisk, 9 m wide and 13.6 m high (11 by 16 m at the tower's
  scale), its front edges chamfered, on a round two-step plinth with 26 gold runes glowing round it, a small gold
  crown on a step at its tip. The left third of its face is a carved relief of the tower, six tiers from the foot
  up with battlements and an arched window each; the right two thirds hold the words in the game's bold Rodin:
  "FLOOR 1 - PART 6" in gold (the stream's real place), the floor's name, a gold divider with a diamond, "BEST
  CLIMBERS", and five rows on proud dark plaques, "1  Name" on the left and the clears at the right end, the first
  on a larger plaque in gold with a gold laurel each side. The game asks for the list when the tower's scene loads,
  three seconds after every change of the stream's step, and every ten minutes. A row with no name shows carved
  runes on its plaque (each row's runes are their own object), so an empty stone is five rows of runes and a stone
  with one climber is one name over four rows of runes. The rows under the first share one size, so the list is
  even: the smallest any of them needs, but never under seven tenths of the largest, so one very long name is made
  smaller on its own row rather than shrinking every row (`built3-stone-close.jpg`); the first stands a quarter
  over them. The letters are 0.6 to 0.7 m high at the tower's scale: read from the plaza and from the usual camera
  on a visitor (`built3-stone-plaza.jpg`).
- **The carved tower** lights as the world tower does, from the same progress (`TowerManager.State`): the floors
  the stream has reached are gold (each tier has a lit look, gold and glowing with the plaza's light, and a dark
  one of grey stone; the game shows one), the floor it is on breathes (its own material with the floors' pulse,
  which stops in potato mode with the tower's other moving light), the ones above stay dark; it follows every
  change of the stream's step, so a floor that falls lights its tier. The tower can have any number of floors:
  while it has no more than the six tiers, tier n is floor n and the tiers over its last floor stay dark (the tower
  goes on); with more, the tiers show a window of six round the stream's floor (two below it, three above; at the
  top, the last six; with all cleared, the last six), and then each tier's window carries its floor's number in
  gold, so "37, 38, 39..." reads as part of a taller tower. Six was chosen because seven tiers of 1.6 m are the most
  the face holds at a size that still reads as a tower of floors from the plaza; squeezing a hundred floors in would
  make them lines. (`WorldTower.StoneTierFloors` does the mapping; checked for 3 and for 100 floors.)
- **The statue** on the pedestal is the first of them, in stone, about 13 m tall (16 m at the tower's scale, as
  the nameless hero was), with a plaque on the pedestal's front that says who it is (gold Rodin on a dark plate,
  hidden while the nameless hero stands there; `built3-statue-plaque.jpg`). It is made the same way whether the
  climber is in the game or not: their look and what they wear (from their character when it is here, else from
  the gear the server sends with the list) are put on a copy of the player model that never wakes up, through the
  game's own appearance and equipment code, so every slot shows (helmet, armor, cape, shield, the weapon); of
  several weapons the strongest is held. The copy is stood in a pose for its kind of weapon, a frame of one of the
  game's own clips, then every mesh on it is frozen, given the one stone material and stood on the pedestal; the
  work is spread over frames (the longest step about 50 ms in the editor) and the old statue stands until the new
  one is whole. The poses (`WorldTower.Poses`, picked by looking at them in stone on the pedestal,
  `built3-statue-pose-candidates.jpg` has the spear's and the bare fist's candidates):

  | Weapon | Pose |
  |---|---|
  | two-handed sword or axe, one-handed weapon | the blade raised over the shoulder, the top of a great swing (`2Hand-Sword-Attack5` at 0.2) |
  | sword (or axe) and shield | on guard behind the shield, the blade out at the side (`Shield-Idle`) |
  | staff | raised to the sky in one fist (`Armed-Boost1` at 0.5) |
  | bow | drawn (`2Hand-Bow-Attack1` at 0.3) |
  | spear | lifted over the head in both hands (`2Hand-Spear-Unsheath-Back-Relax` at 0.6) |
  | none | a fist raised to the sky (`Unarmed-Boost1` at 0.55) |

  It is made again when the best climber changes, when what they wear changes, and when they walk into the game
  with gear the server did not know of; for a stream nobody has climbed with, or if it cannot be made, the nameless
  Synty hero stands there (`built3-statue-gear-sets.jpg`: both bodies, capes, every kind of weapon).

### Easter eggs

See docs/easter-eggs.md: the elevator that is out of order (`tower-elevator-out-of-order`) and the ferry's timetable
on the quay (`tower-ferry-timetable`).

## Visiting: the islet is a place

Players can go there (`WorldTowerVisit.cs`). The islet is an island like the others, `Island.Tower` (11) on the
server and "Tower" in chat, open to every level.

- **`!sail tower`** (or `!travel tower`): the ferry. Its ring is unchanged; after Heim it turns in to a stop off the
  pier's end and goes on to Atria, but only when a passenger is bound for the islet or someone on the islet waits for
  it, so nobody else's crossing is longer. The two legs ("Heim => Tower", "Tower => Atria") are splines in the
  tower's scene, built from the ring's own Heim to Atria line, and `PathSelector.RegisterStop` adds them when the
  scene loads. **MainWorld is not changed and nothing has to be saved by hand.**
- **Own boats** sail there too: the sea chart has a "Tower" dock off the pier's end, and the islet is land on the
  chart (`tools/tower/world/sea-chart-islet.py`, which also took the round 1 patch out again).
- **What a player does there**: nothing to train on the islet, and nobody loses training time (the owner's rule).
  Whoever arrives takes a place on the plaza and keeps learning the skill they were training at the rate they learned
  on their island (`PlayerProgression.TrainAway`, as in the tower's towns and Ravebridge), and the server goes on
  giving what they were gathering (its resource ticks treat the islet as level 1 ground for every skill). A word in
  chat says so when they arrive, at most every five minutes each: "You are at the foot of the tower. There is
  nothing to train here, but you keep learning as on your island. !tower to go in, !sail and an island's name to
  leave."
- **`!tower` from the plaza** walks the player up the stair and in through the gate first; the command itself runs
  when they are inside the tunnel (or after 45 s if they cannot get there). From anywhere else `!tower` works as
  before. Commands that take nobody in (`!tower info`, `map`, `boss`, `climb`...) are not held up.
- **Leaving the tower** puts a player back where they came from, as before: the plaza, if they walked in from it.
- **Saves**: a player who logs off on the islet is saved on "Tower" and comes back to the plaza.
- **The player details** show the skill and its time to the next level as usual there (without the islet's own
  case they said "Level too low for this island", as they do for any place with nothing to train).
- **The camera** can watch the islet like any island (Next island), and the island's own view looks at the plaza.
- **Raids, dungeons, wars**: joined from the islet as from anywhere; no war is fought on it.
- **The ground**: the islet, the plaza, the stair, the tunnel and the pier have colliders of their own under the
  scene's `Place` (they stay when the tower is not drawn) and a navmesh baked into the scene. The steps have unseen
  ramps over them and the pier a flat deck, because the navmesh would not cross the pier pieces' own end beams.

### Older games and the order of deploys

- **A v0.9.6.x game with a character saved on "Tower"**: it has no such island, so the character is put at its saved
  coordinates in the open sea, found to be nowhere, and unstuck onto the ferry; the server's island for it is then
  cleared. No crash, no lost progress: they simply start on the ferry.
- **A v0.9.7 game against a server without `Island.Tower`** (the live one today): a player on the islet is stored as
  island "11", and the old resource tick looks its level table up without a check and throws, which stops that
  session's tick for everyone on the stream. **So the server has to be deployed before a game with the islet is
  released** (it is on the checklist). The new server no longer throws for any island it does not know.

## Scripts

```
blender -b --python tools/tower/world/world_tower.py -- <repo root> [preview] [out=<folder>]
python tools/tower/world/sea-chart-islet.py <repo root>
unity command eval_file "<repo>\tools\tower\world\world-tower-build.cs"
```

1. Blender makes the models from `tools/tower/world/stack.json` and writes `stack-built.json` (heights, radii, the
   plaza's marks, triangle counts). `preview` renders pictures into `Temp/Blender/world-tower`; `out=` writes the
   models somewhere else to look at them first.
2. The chart step puts the islet (as land) and its dock on the sea chart, and takes out what an earlier run put
   there: run it after the tower moved or was scaled, and after every bake of the chart.
3. The builder (editor not in play mode, MainWorld open: it reads the ferry's ring and Home's island from it and
   changes nothing there) makes the scene again from the models, `stack.json` and `stack-built.json`: the stack, the
   foot, the props, the stone's words, the eggs, the gulls, the ground and its navmesh, the island with its dock and
   places, the ferry's two legs. Its log: `%TEMP%/world-tower-build.txt`; it says whether the walks join up, whether
   the ferry's legs lie over water and how far the chart's dock is from the ferry's stop. It works in an additive
   scene of its own and closes it: the open scene is not touched.

### Moving or scaling it

`position`, `faces` (the point the gate looks at) and `scale` in `stack.json`, then steps 2 and 3. Blender is only
needed when a section changes. The ferry's stop follows by itself as long as the tower stays south of the Heim to
Atria line; elsewhere set which path of the ring it comes after (`FerryAfterPath`, `FerryNext` in the builder).

### Adding a floor's section

`stack.json` lists the sections bottom first; nothing assumes how many there are.

1. Add `{ "floor": 5, "style": "unbuilt", "height": 80, "glow": [r, g, b] }` to `sections`. `unbuilt` is a plain dark
   section with windows and a rune band; the shaft moves up by itself, and so do the fade and the cloud bank (the
   builder sets them from the stack's heights).
2. For a look of its own, write a function in `world_tower.py` like `frost` or `storm` (it is given the radius it
   stands on, its height and the detail level, and returns its parts and the radius of its roof) and name it in
   `STYLES`.
3. Run the scripts again. The section lights up when the stream reaches that floor, with no change in the game's
   code: `WorldTower` lights by floor number, and the floors come from RavenNest.Combat's `Tower`.

A tower of very many floors cannot show them all under the cloud: lower the sections' heights in `stack.json`, or let
the cloud hide the upper ones (the stream's floor then lights inside it). That is a design call for when there
are more than six or so.

## What the build machine needs

- The Synty packs named above under "What it is made of" (they are not in git, like every pack), and the font
  `Assets/Fonts/FOT-NewRodin Pro B SDF.asset`.
- `Assets/Scenes/Tower/WorldTower.unity` in the build: the release build adds every scene in `Assets/Scenes/Tower`
  itself since d413b74d. Without it the game plays as before, with no tower on the horizon and no islet to sail to,
  and says so once in the log.
- Nothing in `Assets/Resources`.

## Seen in play, and not

Play mode on the local server (chat muted, the bot cut off), 2026-11-24 and (round 3) 2026-11-25, with bots as
players and the owner's own character in the session.

Seen:

- The tower from Home's, Heim's, Away's and Atria's shores at eye level, and from Kyo, Ironhill and Eldara at the
  game's default view distance (650 m, further than which the tower is squeezed in); the top going into the cloud
  from each, from 330 m over Home, and in potato mode.
- Round 3's top from Home's shore (eye level and 40 m up), from Heim and from 330 m over Home in fair weather, at
  night, with clouds off and in potato mode: no dither, no lid, no end of the shaft to see.
- Each camera look by hand, and each by its own trigger: the stream's step going from a floor's last part to the
  next (through the editor's preview step, not a real boss) started the floor look 0.6 s later, it ran 4.7 s and
  the camera went back to the player it was on; two visitors walking in from the plaza started the entering look
  as the first got inside; the idle look started by its timer after 2 s of quiet (set short for the test), ran 9 s
  and went back.
- Visiting: the islet in the game's island list and found by name; bots sailing there from Home and Kyo by the
  ferry, which turned in after Heim, put them on the pier and went on to Atria; three more the same way with the
  camera on the pier; the ferry turning in empty for three who waited on the islet to leave, and taking them; a
  place on the plaza for each; experience still coming in on the islet (TrainAway); `!tower` from the plaza walking
  two of them up the stair and through the gate before the join went through; the player details' training card
  there.
- The sea chart: every island has a route to the Tower dock and back, and no route between two other docks comes
  nearer than 416 m to the tower's middle (asked of the chart; no own boat was sailed).
- The stone (round 3): with the server's one real row ("1  zerratar  5") over four rows of runes, with five rows put
  in by hand (one a 22 letter name), empty, and on floor 2 part 4 through the preview step (two tiers lit, the
  second breathing). The tier mapping for 3 and for 100 floors.
- The statue (round 3): cast from the server's answer for every kind of weapon (two-handed sword and axe,
  one-handed sword, sword and shield, staff, bow, spear, none), male and female, with and without capes, and from a
  bot in the game; the plaque with each name; the nameless hero back when the list is empty.
- The tower camera key: T through the camera's own key check (`Keybinds.Simulate`, which the editor's evals use
  because a key cannot be pressed while the editor is not the focused window) from the tower's group camera to the
  world side and back, with two bots in the tower; the settings window's Game tab with "Looks at the tower" turned
  off and on by a click on its row, and the Keys tab's "Tower camera: to the tower and back" row as T, waiting for a
  key, rebound to Y (with its reset) and reset to T (`built3-settings.jpg`, `built3-tower-key.jpg`).
- The sea floor strip from above (the straight edge behind the tower gone), from Home at eye level and from Heim.

Not seen, or not done:

- A real floor boss falling with its own cutscene before the look (the look waits for `TowerCutscene.Playing` to
  end, up to a minute; that wait was not played).
- The key pressed on a real keyboard, and a rebind through "Press a key" with a real key: the window's capture
  reads the keyboard itself.
- The top in a settled overcast, rain or a storm: the weather was still drifting in from the last one in those
  captures. Under a full overcast the fade goes to the cloud's color (read from the shader, not seen).
- An own boat sailing to the islet and docking; a player logging off on the islet and coming back; a raid or a
  dungeon joined from the islet; the islet in the island tour camera.
- A v0.9.6.x game with a character saved on "Tower", and a v0.9.7 game against the live server: both are read from
  the code (see "Older games and the order of deploys"), neither was run.
- A build: everything here ran in the editor. Frame time with the new models was not measured again.
- The chat lines (the word on arriving at the islet, "You walk up the stair and in through the tower's gate"): chat
  was muted for the tests, so they were sent to nobody. The bot's help for `!sail tower` is changed in its source
  but the bot was not run.
- The statue's sword and shield: the shield hangs as the game's own shields do, and some of those sit wrong on the
  forearm (the owner's close-up of 2026-11-25); the main session is fixing the shields' mounts with an editor tool,
  which fixes the statue with them (it uses the game's own equipment path).

## Captures

`docs/mockups/tower/world/`, round 3 (`built3-*`), taken 2026-11-25 in play mode (local server, chat muted, the bot cut
off), view distance 1300 m:

| File | What |
|---|---|
| `built3-statue-gear-sets.jpg` | The statue for six gear sets: two-handed sword (male, cape), one-handed sword (female, cape), staff (female, cape), bow (male, no cape), spear (female, cape), no weapon (male, cape) |
| `built3-statue-pose-candidates.jpg` | The candidates for the spear and for no weapon |
| `built3-statue-from-plaza.jpg` | The plaza from its far side at a normal distance: the statue on the left, the stone on the right |
| `built3-statue-plaque.jpg` | The plaque on the pedestal |
| `built3-statue-from-the-game.jpg` | Cast from a bot in the game |
| `built3-stone-close.jpg` | The stone with five rows (one a long name), floor 1 lit and breathing |
| `built3-stone-one-climber.jpg` | The stone with the server's one real row and runes on the other four |
| `built3-stone-plaza.jpg` | The same as `built3-statue-from-plaza.jpg` |
| `built3-top-from-home.jpg`, `-from-heim.jpg`, `-above-home.jpg`, `-night.jpg` | The top in fair weather and at night |
| `built3-top-clouds-off.jpg`, `-clouds-off-above.jpg`, `-potato.jpg` | The top with no sky clouds, and in potato mode |
| `built3-sea-before-after.jpg` | From above: the terrain's straight southern edge behind the tower, and the sea floor strip carrying on |
| `built3-sea-from-home-eye-level.jpg` | From Home's shore at eye level after the fix: the lighter band near the horizon that is left |
| `built3-settings.jpg` | The settings window: "Looks at the tower" in the Game tab, the tower camera key in the Keys tab |
| `built3-tower-key.jpg` | T from the tower side to the world side and back |
| `built3-towercam-*.jpg` | The tower camera mode (docs/tower-design.md section 23) |

Round 2 (`built2-*`), taken 2026-11-24 in play mode (local server, chat muted, the bot
cut off), view distance 1300 m (the development machine's setting) unless said. The stream's real state on the dev
server was floor 1, part 6. Round 1's (`built-*`) show the tower at its first place, (405, -60).

| File | What |
|---|---|
| `built2-from-home.jpg` | From Home's south shore at eye level, 516 m: Away to the left, Heim to the right |
| `built2-from-heim.jpg` | From Heim's east point, 255 m |
| `built2-from-away.jpg` | From Away's south shore, 330 m |
| `built2-from-atria.jpg` | From Atria's west cliffs, 480 m; a cloud drifting in front of the fortress |
| `built2-from-kyo-default-view.jpg` | From Kyo, 765 m, view distance 650 m, zoomed in: squeezed in from beyond the view, Heim in front of its foot |
| `built2-from-ironhill-default-view.jpg` | From Ironhill, 990 m, view distance 650 m, zoomed in, over Home's town |
| `built2-from-eldara-default-view.jpg` | From Eldara, 950 m, view distance 650 m, zoomed in |
| `built2-compare-home.jpg` | Round 1 from Home's east shore (309 m, taken at dusk under an overcast sky) beside round 2 from its south shore (516 m, by day) |
| `built2-compare-away.jpg`, `built2-compare-atria.jpg` | The same for Away and Atria |
| `built2-above-home.jpg` | From 330 m over Home, level with the cloud layer: the top inside its own bank of cloud |
| `built2-top-into-cloud.jpg` | The fortress's crown and the shaft going into the cloud, zoomed in from Home's shore |
| `built2-fortress-close.jpg`, `built2-fortress-close-2.jpg` | The Dark Fortress section from 200 m, unreached: fins, slits, crowns, turrets, the trident |
| `built2-potato-mode.jpg` | Potato mode from Home's shore: the low detail tower and the low-poly cloud |
| `built2-moment-floor-lit.jpg` | The look when a floor falls, 2.4 s in |
| `built2-moment-entering.jpg` | The look when the stream enters, 3.4 s in |
| `built2-moment-idle.jpg` | The idle look, 6.5 s in, from a camera that was on a player on Home |
| `built2-ferry-at-pier.jpg` | The ferry at the pier's end, three passengers just ashore, Home across the water |
| `built2-plaza-visitor.jpg` | A visitor on the plaza after the ferry, the game's interface on; the stone shows the server's real list |
| `built2-walk-in.jpg` | Two visitors walking up the stair after `!tower`, from the usual camera |
| `built2-stone-rows.jpg` | The stone with five rows (bots put in as the list) |
| `built2-statue-climber.jpg` | The statue cast from the best climber in the game |
| `built2-statue-from-look.jpg` | The statue cast from a look alone |
| `built2-camera-button.jpg` | The tower panel's camera button in both states, and the word with nobody in the tower |
| `built2-details-on-islet.jpg` | The player details of a visitor on the islet |

Where it still falls short:

- **From the overview camera over Home** (330 m up) in fair weather the bank still reads as one big cloud over the
  crown, broader and ragged now, but a cloud on top (`built3-top-above-home.jpg`). Making it part of a wider field
  (a bigger radius) made a bigger hat; what would really answer it is a lasting deck of cloud toward the south, or
  the bank higher than the overview camera ever goes. The owner's call.
- **At eye level from Home's shore** a lighter band of sea near the horizon is still darker at its sides than in
  front of the islet (`built3-sea-from-home-eye-level.jpg`). It is the sea's look at a grazing angle, not the islet:
  the water reads its depth along the line of sight, a line that skims the surface from a camera at the waterline
  reaches the bed only where something stands under the water (the islet's foot, an island's shelf) and runs out
  past the far plane elsewhere, which counts as the open deep; Away's shelf in the same picture shows the same band.
  The game's camera is never that low. A fix belongs in the ocean shader (count "beyond the far plane" as the
  world's usual 26 m of sea rather than the open deep), which changes the far sea everywhere: not done without the
  owner.
- From further than the view distance the tower is drawn in front of all cloud (the squeezed depth), so there the
  fade does the work alone; it reads as the shaft melting into the sky rather than into a cloud.
- The statue cast from a look alone wears what the server's list sends; a climber whose gear the server does not
  know (an older server) wears what the bare model holds.
