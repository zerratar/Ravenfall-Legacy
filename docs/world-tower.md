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
the sea floor on south of the world's terrain so the sea round the islet has no straight edge behind it. Round 4
(2026-11-25, the owner's two approved fixes) gave the tower a lasting deck of cloud in layers to rise into, so its top
no longer reads as a cloud sitting on it, and laid the sea floor strip level and far out, which took away a pale band
with a straight end that the haze drew across the sea behind the islet (see "No top" and "The sea round the islet").

Captures of it in the game, taken in play mode on the local server: `docs/mockups/tower/world/built4-*.jpg` (round 4),
`built3-*.jpg` (round 3),
`built2-*.jpg` (round 2; the lists are at the end) and `built-*.jpg` (round 1, the old place).

## Where it stands and how big it is

| | |
|---|---|
| Place | (60, -700) in the main world, sea level -3.31: out in the open sea south of Home, beyond Heim and Away |
| Scale | 1.2 (`scale` in `stack.json`; the models are made at 1 and everything below is as it stands in the world) |
| Base | 192 m across at the wall (radius 96), the rock round it about 280 m |
| Height | 367 m of sections (108 + 84 + 74 + 101), then the shaft goes on to 559 m and fades out between 402 and 488 m, inside the sky's deck of cloud |
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
| - | the shaft | 367 to 559 | the tower goes on: ledges, rings of dark windows and ribs every 46 m, fading out into the sky's cloud between 402 and 488 m |

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
speckle against the sky and from the islands, and the bank sat on the tower like a hat. Round 3 took the dither out for
a smooth fade, but from the overview over Home the bank still read as one cloud on the crown
(`built3-top-above-home.jpg`). Round 4 (2026-11-25, the direction the owner approved: a lasting bank of cloud to the
south that the tower rises into):

- **The deck.** The sky's clouds (`SkyClouds`, the same volumetric march as the rest of the sky, nothing modelled)
  keep a wide deck of cloud in the tower's part of the sky in every weather: four thin layers, 30 m apart from 375 m
  over the sea (just over the crown at 367 m) to about 470 m, each heaving up and down a little and torn with holes
  of its own so the sky shows between and through them. Its ground plan is an oval drawn out behind the tower (away
  from the gate, so away from Home): 2.6 km behind it, 400 m in front, 2.2 km to each side and wider the further
  back, its rim ragged (`SkyClouds.TowerDeck`, `TowerDeckWay`; `_CloudDeck`, `_CloudDeckWay` and `Deck()` in
  `SkyClouds.shader`; the numbers are `Deck*` on `WorldTower`, set by the builder from the stack's heights). From
  the islands it is a lasting bank of cloud on the southern horizon; from Home's shore it fills the sky behind and
  over the fortress; from 330 m over Home the shaft goes up into a field of layered cloud that spans the view, with
  sky between the layers, not under a lid (`built4-top-above-home.jpg`, `built4-top-before-after.jpg`).
- **The column.** Round 3's bank stays, narrower (170 m round the shaft, from about 390 m up, ragged at its foot and
  rim, `SkyClouds.Tower`): the shaft goes into a column of cloud where it meets the deck, so from every side
  something stands in front of it.
- **The shaft fades out by 488 m** (was 548 m), inside the deck's top layer, so nothing of it stands over the cloud.
  From 402 m it blends toward the cloud's color, from 463 m on toward the sky. The cloud's color is now the one the
  sky's clouds are lit with in this light and weather (`SkyClouds.LitColor`, from the same sun, ambient and storm
  darkness as the march): the old fixed color was brighter than the cloud at night and under a storm, and stood as a
  pale blob over the crown. The sky's color is read from the live sky probe at its sharpest level (mip 0); the
  blurred level used before carried the horizon's white up the sky, so the faded shaft stood paler than the sky
  round it with a visible end (round 3's `built3-top-clouds-off-above.jpg`, a pale cap; gone now).
- **At the default view distance (650 m)** the tower is drawn squeezed in front of the far plane (see "Seen from
  every island"), which put it in front of all its cloud from the overview. The cloud march now reads such a pixel
  back to where the tower really stands (only where that lands within 260 m of the tower's axis, so nothing else
  changes), so the deck goes in front of the shaft at any view distance (`built4-top-view-650.jpg`).
- **Weather**: the deck is there in every weather. Under a settled overcast, rain or a storm the weather's own cloud
  (230 to 480 m) covers the fortress too, so the tower goes up into the storm (`built4-top-overcast.jpg`, `-rain.jpg`,
  `-storm.jpg`); at night the deck and the column are the night's cloud and the fade matches them
  (`built4-top-night.jpg`).
- **Potato mode, or Clouds off**: no sky clouds and no deck; the same fade runs straight into the sky from 402 m up
  (`WorldTower.Fade`) and, with the sharp sky lookup, the shaft melts into the sky with no end to see
  (`built4-top-potato.jpg`, `-clouds-off.jpg`).
- The storm ring stays: it sits at 200 to 260 m, well under the deck, and still reads as the Storm Spire's own
  weather.

What was tried and dropped: in round 3, a shaft going on to 991 m that turned to the sky's color over several hundred
metres (from the overview it stood over the bank as a long pale column with an end); in round 4, the deck without the
column round the shaft (the shaft then stood in front of the deck as a pale ghost column from the overview, since the
deck lies mostly behind it). The deck's sheets are left out of the march toward the sun and out of the cloud shadows
(they are thin, so they shade little; that saves about a tenth of a millisecond): they are lit as if nothing of the
deck stood between them and the sun, and they cast no shadow on the sea.

## The sea round the islet

From Home's shore at eye level a lighter patch of sea surrounded the islet with straight vertical sides
(`built3-top-from-home.jpg`), and from above and from the overview a pale band with a straight end lay across the sea
behind it (`built3-top-above-home.jpg`). Round 4 (2026-11-25) looked at each candidate on its own (the tower's
renderers hidden, the sea floor strip hidden, the haze off, the terrain, the chart, the foam) and found two causes:

- **The band from above: the haze reading the sea floor strip. Fixed.** The sea writes no depth, so the game's haze
  pass (`Haze.shader`) hazes the water by the distance to the bed seen through it. Round 3's strip sloped 150 m down
  over 600 m and stopped at z -1453; the height haze is thicker low down, so its deep part came out paler and its end
  was a straight line across the sea. With the strip hidden, or the haze off, the band was gone
  (`built4-sea-above-before-after.jpg`). The strip is now level: it starts at the terrain's own edge height (-30 to
  -29.3 m all along), eases to the world's usual 26 m of water within 250 m, and reaches 3.2 km south and 1 km past
  the terrain's ends east and west, so its end lies past the view from anywhere a camera goes (the builder's sea
  floor step; the mesh asset was made again in place). What remains from the overview is a faint line where the far
  plane cuts the sea floor, as everywhere in the world at that height.
- **The straight-sided shallows at eye level: the ocean shader's grazing look. Not changed.** Each water pixel reads
  the depth behind it along the line of sight and takes the water's depth from the point that line meets
  (`Ocean.shader`, `vd`). From a camera at the waterline the line runs on under the surface for hundreds of metres,
  and every pixel of a column on screen looks the same way across the sea, so the whole column meets the same thing:
  an island's shelf or the islet's foot far off (shallow, so turquoise), or nothing before the far plane (the open
  deep). The edge between the two is a compass direction, so it is a vertical line on screen, whatever the shore's
  shape. It is not the islet: with the tower's whole scene hidden Away and Heim show the same straight-sided pale
  shallows in the same picture, and Eldara seen from Home's east shore at the same distance shows the same patch
  under it (`built4-sea-proof-other-islands.jpg`). Not the terrain's tiles (no tile edge there), the sea chart (the
  sea does not read it) or the shore foam (only where the water is under 1.6 m deep). From the usual camera, which
  looks down at 45 degrees, the line meets the bed just under the water, and nothing of this shows
  (`built4-sea-gamecam.jpg`).

**Tried and taken back, 2026-11-25: the owner prefers the sea as it was** ("i dont mind the edges we could see before"; the change touched the sea everywhere for one view). Kept here so it is not proposed again. What was tried (it changes the sea everywhere): trust the bed only near the water it lies under,
and count a bed met far off as the open sea's, as when no bed is met at all. In `Ocean.shader`, after the `open` case:

```hlsl
// at a grazing angle the line of sight runs on under the surface for hundreds of metres, and what it meets there
// (an island's shelf far off) tells nothing of the water under P
float across = length(bedWS.xz - P.xz);
float trust = 1.0 - smoothstep(60.0, 200.0, across);
vd = lerp(1000.0, vd, trust);
```

Tried on the live sea in play mode (a test copy of the shader on the sea's material, not committed): at eye level from
Home, Heim, Away and toward Eldara the straight-sided patches are gone and the shallows follow the shores
(`built4-sea-from-home-eye-level.jpg`, `built4-sea-heim-away.jpg`); from the usual camera and from above nothing
changes. It costs a length and a smoothstep per pixel. What it costs in looks: from the waterline an island more
than a couple of hundred metres off loses its turquoise rim (Eldara at 520 m is deep blue to its beach), and the
islet's near shallows are thinner. A wider trust (150 to 300 m) keeps more of the rim but leaves Away's shelf with a
straight side at eye level.

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
| The top | fades into the sky's deck and column of cloud | fades into the sky (no sky clouds in potato mode) |
| Camera looks | on (their settings) | the idle look and the two cutscene looks are off |

Round 1's models were 100,851 and 23,808 triangles: the look pass and the shaft added 19,538 near (the fortress's
shapes, the courses' joints and proud blocks, growth and ice) and 635 far, plus the 2,480 of the potato cloud. Round 3
added about 1,600 near for the stone (the stele's body, plinth, rune ring, plaques, laurels and crown 450, its twelve
carved tier looks 660, the rows' runes 248) and about 1,670 for the taller shaft, and took the potato cloud out. The
sea floor strip south of the terrain is 3,916 more (round 4: level and further out; it was 2,304), drawn with a plain lit material, no shadows. The budget kept: every block's backing and sides
exist only in the near models; the far models keep the silhouette (fins, turrets, crowns, crystals) and drop the
rest. The low detail models show from 1,150 m (round 1: 950 m), so every island that looks at the tower from within
a kilometre gets the near one.

The Synty props on the plaza are drawn only within the view distance like any island's.

**The deck of cloud** (round 4) is part of the sky's cloud march, so it follows the Clouds setting and potato mode
(neither draws it). Measured on the development PC (GPU time of the march at 2560 by 1440, the march at half that,
fair weather, the profiler's GPU recorder over 120 frames): from 330 m over Home 1.49 ms with round 3's bank, 1.77 ms
with the deck and column (1.03 ms with no tower cloud at all); from Home's shore 0.74 ms against 0.99 ms; from the
usual camera that looks down at the islands 0.003 ms either way (a ray that never reaches the cloud layer stops at
once). So about 0.25 to 0.3 ms more, and only while the tower's sky is in view. The deck is skipped wherever the
weather's own cloud is already full, and in the march toward the sun and the shadows.

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

Play mode on the local server (chat muted, the bot cut off), 2026-11-24 and (rounds 3 and 4) 2026-11-25, with bots
as players and the owner's own character in the session.

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
- Round 4's top from Home's shore, Heim and 330 m over Home in fair weather; in a settled overcast, rain and
  thunderstorm (the weather brought in over a second and ticked past it, so no front was still drifting in) from
  Home, Heim and 200 m over Home; at night; at the 650 m view distance; with clouds off and in potato mode. The
  deck's GPU time from the overview, from Home's shore and from the usual camera.
- Round 4's sea: the band from above with the strip hidden and with the haze off (both took it away), then gone with
  the level strip; the eye-level patch with the tower's whole scene hidden, toward Eldara, from Heim and Away, and
  with the proposed ocean change on a test copy of the shader.

Not seen, or not done:

- A real floor boss falling with its own cutscene before the look (the look waits for `TowerCutscene.Playing` to
  end, up to a minute; that wait was not played).
- The key pressed on a real keyboard, and a rebind through "Press a key" with a real key: the window's capture
  reads the keyboard itself.
- The deck from other islands far off (Kyo, Ironhill, Eldara: read as a bank on the southern horizon, not
  captured), and its look as the weather changes over while watched (only settled weathers were seen).
- The deck's frame cost on a weak machine and in a build (measured in the editor on the development PC only).
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

`docs/mockups/tower/world/`, round 4 (`built4-*`), taken 2026-11-25 in play mode (local server, chat muted, the bot cut
off), view distance 1300 m unless said, weather settled:

| File | What |
|---|---|
| `built4-top-above-home.jpg` | From 330 m over Home, fair: the shaft goes into the deck, sky between its layers |
| `built4-top-before-after.jpg` | The same view, round 3 (one cloud on the crown) beside round 4 |
| `built4-top-from-home.jpg`, `-from-heim.jpg` | From Home's south shore at eye level and from Heim's east point, fair |
| `built4-top-view-650.jpg` | From 330 m over Home at the default view distance, 650 m |
| `built4-top-overcast.jpg`, `-rain.jpg`, `-storm.jpg` | Settled overcast (Home's shore, 200 m over Home), rain and a thunderstorm (Home's shore, Heim) |
| `built4-top-night.jpg` | Night, Home's shore and Heim |
| `built4-top-clouds-off.jpg`, `-potato.jpg` | Volumetric clouds off, and potato mode: Home's shore and 330 m over Home |
| `built4-sea-from-home-eye-level.jpg` | From Home's shore at eye level: round 3, round 4, and the proposed ocean change |
| `built4-sea-gamecam.jpg` | The usual camera over Home's south shore: before, after, proposed |
| `built4-sea-above-before-after.jpg` | From 160 m up: the haze band before, gone after, and gone before with the haze off or the strip hidden |
| `built4-sea-heim-away.jpg` | From Heim and Away at eye level: before, after, proposed |
| `built4-sea-proof-other-islands.jpg` | Home at eye level with the tower hidden (Away and Heim show the same patches), Eldara at 520 m, and Eldara with the proposed change |

Round 3 (`built3-*`), taken 2026-11-25 in play mode (local server, chat muted, the bot cut
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

- **The sea's straight-sided shallows at eye level** round the islet stay until the owner says yes to the ocean
  change proposed under "The sea round the islet" (they show round every island from the waterline).
- **The deck's layers are flat sheets**: from the overview they read as a stack of thin, even layers (an
  altostratus deck) more than billowing cumulus; puffier sheets would cost more noise per sample.
- **Under a settled overcast, rain or storm** the weather's own cloud (from 230 m) hides the fortress too, so the
  tower's crown is lost in grey; that is the weather's slab, not the deck, and reads as the tower going up into the
  storm, but the fortress cannot be seen then.
- From Heim and Away, close under it, a faint pale dome stands where the shaft meets the cloud over the crown (the
  half-faded shaft against the shaded underside of the column).
- Further than about 3.6 km the tower is not drawn; between the view distance and that the cloud march puts its cloud
  in front of the squeezed tower (round 4), so it no longer only melts into the sky there.
- The statue cast from a look alone wears what the server's list sends; a climber whose gear the server does not
  know (an older server) wears what the bare model holds.
