# The tower in the main world

The tower that stands in Ravenfall's sea (docs/tower-worlds-proposal.md section 6): a colossal landmark on its own
islet, a stack of sections that each look like a floor's world, lit by the stream's progress, with the entrance plaza
at its foot. The owner picked concept A (`docs/mockups/tower/world/a-islet.jpg`) with concept C's plaza
(`c-entrance-plaza.jpg`) on 2026-11-24, and asked above all for the feeling that it is massive: "it should after all
be the sense that it is massive given the big worlds inside".

Built 2026-11-24 for v0.9.7, in two rounds the same day. Round 1 stood it at (405, -60) between the four eastern
islands; the owner then asked for it further off ("want it to feel like its a huge thing in the distance, well not too
far away") and took every open call. Round 2 moved it, made it bigger, took its top into the sky's own clouds, gave
it a look pass, the camera's looks at it, the islet as a place players sail to, the stream's best climbers on the
stone and as the statue, and a key that takes the camera to the tower and back.

Captures of it in the game, taken in play mode on the local server: `docs/mockups/tower/world/built2-*.jpg` (round 2;
the list is at the end) and `built-*.jpg` (round 1, the old place).

## Where it stands and how big it is

| | |
|---|---|
| Place | (60, -700) in the main world, sea level -3.31: out in the open sea south of Home, beyond Heim and Away |
| Scale | 1.2 (`scale` in `stack.json`; the models are made at 1 and everything below is as it stands in the world) |
| Base | 192 m across at the wall (radius 96), the rock round it about 280 m |
| Height | 367 m of sections (108 + 84 + 74 + 101), then the shaft goes on to 511 m and is gone into cloud between 415 and 500 m |
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
  `WorldTowerClimbers.cs` (the stone's names and the statue), and `WorldTowerCamera.cs` (the tower camera key).
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
| - | the shaft | from 367 | the tower goes on: ledges, rings of dark windows and ribs every 46 m, thinning out into the sky's cloud between 415 and 500 m |

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
Now the tower simply goes on (the shaft) and is lost in a real cloud:

- **The sky keeps a bank of cloud there.** `SkyClouds.Tower` (set by `WorldTower`, read by the cloud shader as
  `_CloudTower`) raises the cover to full and thickens the body within 260 m of the tower from about 380 m up,
  whatever the weather, with a ragged rim (the distance is pushed about by a slow noise) so it is a heap of cumulus
  and no disc. It is the game's own volumetric cloud, lit, moved and drawn like every other: on a clear day the
  tower wears its own cloud, on an overcast day it goes into the deck like everything else.
- **The shaft thins out inside it.** From 415 m to 500 m over the sea the shader drops more and more of the shaft's
  pixels (a dither that is even from pixel to pixel, so it reads as mist and needs no transparency) and turns what
  is left toward the cloud's own color in the light of the moment. At 500 m nothing is left; the model ends at
  511 m, inside the cloud layer's ceiling at 480 m and just over it.
- **What is seen**: the fortress's crown and turrets stand clear, the shaft rises some 40 m over them and goes into
  the white. From close by the cloud is really in front of the shaft and round it. From further than the view
  distance the tower is drawn in front of all cloud (the sky's clouds stop at the squeezed depth, as in round 1),
  and there the fade does the work alone: the shaft pales into the color of the bank behind it.
- **From above an island** (the free camera high up, an island's camera pulled back; `built2-above-home.jpg` is
  from 330 m over Home): the bank sits on the tower like a cap of cumulus among the other clouds at that height, and
  the shaft is gone inside it. No end shows.
- The storm ring stays: it sits at 200 to 260 m, well under the bank, and still reads as the Storm Spire's own
  weather.

**Potato mode, or Clouds off in the settings**: the game draws no sky clouds then, so there is nothing to fade into.
The shaft still thins out by the same dither (it costs nothing: a compare per pixel), and a low-poly cloud of 2,480
triangles (`Lid_Far`, part of the shaft's model) is shown round the fade so the end is hidden
(`built2-potato-mode.jpg`). It is drawn only then.

What was tried first and dropped: drawing the shaft after the opaque things and outside the depth the clouds read,
so the clouds lay over it wherever they were along the view. Every wisp behind the shaft then whitened it too, and
it read as smoke rising from the fortress.

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

One key takes the camera to the players in the tower and back (`WorldTowerCamera.cs`; the owner's ask of 2026-11-24:
"can you easily switch between the tower camera and game camera?"). **T** unless rebound (Settings > Keys > Camera,
"Tower camera: to the tower and back"; `Keybinds.Action.TowerCamera`), and a button for it on the tower panel, which
is on screen whenever anyone is in the tower, wherever the camera is: it reads "Tower (T)" while the camera is in the
world and "World (T)" while it is in the tower.

- **From the world**: to the player the camera last watched in the tower if they are still there, else someone on
  the climb's part, else someone in the town, else anyone in the tower. With nobody in the tower the key does
  nothing but say "Nobody is in the tower" in the status column for three seconds (the panel is not up then).
- **From the tower**: back to the player it was watching outside if they are still outside, or to that island's
  view, else the first player outside the tower, else the islands.
- Both ends are remembered while the game runs, so looking out and coming back lands on the same player.
- **It stays there.** After the key, the camera's own rotation (the ten second timer, Next player) goes round the
  players on that side only, in the tower or outside it: without that the rotation carried the camera out of the
  tower again ten seconds after the key took it in. That ends when the streamer picks someone on the other side (a
  click in the players list, the search, Next island), when the key is pressed again (the other side then), or when
  nobody is left on that side. Until the key is first used the rotation is as it always was, tower and world mixed.
- It leaves alone what has the camera for a reason: a cutscene (the Cinematic state takes no camera key), the race
  camera, build mode, a character played by hand, the hero's view.

The look (sky, light, weather, music) follows the camera as it does for Next player: `TowerLook` sets it after the
camera has moved.

## Potato mode and cost

| | Normal | Potato mode, or further than 1,150 m |
|---|---|---|
| Tower triangles | 120,389 | 24,443 (and the 2,480 of the cloud over the top while no sky clouds are drawn) |
| Light | steady, and breathing on the stream's floor | steady only |
| Moving | cloud ring turns, rocks bob, clouds heave, fire wavers, lightning, gulls | none of it |
| Lamps | the gate's and four braziers' point lights | off |
| Shadows | cast within 320 m | cast within 320 m |
| The top | fades into the sky's cloud bank | fades under the low-poly cloud |
| Camera looks | on (their settings) | the idle look and the two cutscene looks are off |

Round 1's models were 100,851 and 23,808 triangles: the look pass and the shaft added 19,538 near (the fortress's
shapes, the courses' joints and proud blocks, growth and ice) and 635 far, plus the 2,480 of the potato cloud. The budget kept: every block's backing and sides
exist only in the near models; the far models keep the silhouette (fins, turrets, crowns, crystals) and drop the
rest. The low detail models show from 1,150 m (round 1: 950 m), so every island that looks at the tower from within
a kilometre gets the near one.

The Synty props on the plaza are drawn only within the view distance like any island's.

Nothing here has a setting of its own except the idle look: the cost is triangles while it is in view, the moving
parts are shader maths and a handful of turning transforms, the fade is a compare per pixel, and all of it follows
potato mode (`GameManager.PotatoMode`), as `TowerFx` does for the floors.

## The plaza

At the foot on the side facing Home: the gate with its tunnel of light and the tower's sun over it, a stair, the round
paved plaza with a compass (50 m across) on a round bastion, a low wall with fire bowls on its posts, stone steps down
to a landing and a wooden pier, the statue on a pedestal to the left of the stair and the leaderboard stone to
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
- **The stone**: under the stream's place ("FLOOR 2" over "PART 4") and the floor's name, a line "BEST CLIMBERS"
  and five rows, "1. Name  12" (the place, the name, the clears), in bold Rodin a metre high, the first in gold. The
  game asks for the list when the tower's scene loads, three seconds after every change of the stream's step, and
  every ten minutes. Until a stream has climbers the rows are carved runes, as before.
- **The statue** on the pedestal is the first of them, in stone, 13 m tall (16 m at the tower's scale, as the
  nameless hero was): if they are in the game it is cast from their character as it stands that moment (the pose,
  what they wear and carry, not a pet), frozen, in the plaza's pale stone. If they are not in the game it is cast
  from the look the server sent, on a copy of the player model that never wakes up, posed from the first frame of
  its own idle animation, with what the bare model holds and not the player's gear (the look says nothing of what
  they wear); when they join later the statue is cast again from them. A cast that still stands with its arms out
  is thrown away. If nothing can be had, and for a
  stream nobody has climbed with, the nameless Synty hero stands there.

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

Play mode on the local server (chat muted, the bot cut off), 2026-11-24, with bots as players and the owner's own
character in the session.

Seen:

- The tower from Home's, Heim's, Away's and Atria's shores at eye level, and from Kyo, Ironhill and Eldara at the
  game's default view distance (650 m, further than which the tower is squeezed in); the top going into the cloud
  from each, from 330 m over Home, and in potato mode.
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
- The stone with a real row from the server ("1. zerratar  3": clears the other session's tests made on the dev
  server after the list existed) and with five rows; the statue cast from a player in the game and from a look
  alone.
- The tower camera key through its toggle (not the keyboard): to the tower onto the player last watched there, the
  rotation staying in the tower for two turns, back out onto the player watched before; "Nobody is in the tower";
  the button's two states on the tower panel.

Not seen, or not done:

- A real floor boss falling with its own cutscene before the look (the look waits for `TowerCutscene.Playing` to
  end, up to a minute; that wait was not played).
- The key pressed on a keyboard, rebinding it, and the settings rows (Looks at the tower, the key's row): they
  compile and use the same helpers as their neighbours, but the settings window was not opened.
- An own boat sailing to the islet and docking; a player logging off on the islet and coming back; a raid or a
  dungeon joined from the islet; the islet in the island tour camera.
- A v0.9.6.x game with a character saved on "Tower", and a v0.9.7 game against the live server: both are read from
  the code (see "Older games and the order of deploys"), neither was run.
- A build: everything here ran in the editor. Frame time with the new models was not measured again.
- The chat lines (the word on arriving at the islet, "You walk up the stair and in through the tower's gate"): chat
  was muted for the tests, so they were sent to nobody. The bot's help for `!sail tower` is changed in its source
  but the bot was not run.

## Captures

`docs/mockups/tower/world/`, round 2 (`built2-*`), taken 2026-11-24 in play mode (local server, chat muted, the bot
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

Where it still falls short: from further than the view distance the shaft's fade is a visible speckle against the
cloud when zoomed in; the low-poly cloud in potato mode sits behind the fortress's crown rather than round it; the
statue cast from a look alone wears what the bare model holds, not the player's gear; the sea round the islet
shows the game's usual hard edge where the sea floor ends at the view distance; the stone's rows are 0.9 m high,
which reads from the plaza and from the usual camera on a visitor, not from the pier.
