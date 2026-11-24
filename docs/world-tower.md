# The tower in the main world

The tower that stands in Ravenfall's sea (docs/tower-worlds-proposal.md section 6): a colossal landmark on its own
islet, a stack of sections that each look like a floor's world, lit by the stream's progress, with the entrance plaza
at its foot. The owner picked concept A (`docs/mockups/tower/world/a-islet.jpg`) with concept C's plaza
(`c-entrance-plaza.jpg`) on 2026-11-24, and asked above all for the feeling that it is massive: "it should after all
be the sense that it is massive given the big worlds inside".

Built 2026-11-24 for v0.9.7. Captures of it in the game, taken in play mode on the local server:
`docs/mockups/tower/world/built-*.jpg` (the list is at the end).

## Where it stands and how big it is

| | |
|---|---|
| Place | (405, -60) in the main world, sea level -3.31: the open water between Home, Away, Atria and Eldara |
| Base | 160 m across at the wall (radius 80), the rock round it about 230 m, its skirt under the sea 340 m |
| Height | 306 m of sections (90 + 70 + 62 + 84), the cloud cap from 340 m up to about 430 m |
| For scale | the islands are 200 to 300 m across; the highest ground in the world is Ironhill's peak, 56 m over the sea |
| Gate | 13 m wide and 22 m high, under a 34 m front; a character is 1.8 m |
| Faces | Home's middle (60, 40): the gate, the plaza and the pier look west-north-west |

Why there: it is the spot in the middle of the four eastern islands that lies furthest from every ferry line (184 m
from the nearest, Eldara to Home and Heim to Atria), so the islet's rocks stay about 55 m clear of the ferry and the
pier's end about 30 m from the Away to Ironhill line. From there it is 320 to 420 m to Home, Away, Atria and Eldara
(it fills the sky), 620 m to Ironhill, 780 to 900 m to Heim, Kyo and Mangrove, 1.5 km to War and 2.6 km to
Ravebridge. To move it, change `position` (and `faces`) in `tools/tower/world/stack.json` and run the three steps
under "Scripts" again.

Why that size: at 400 m a 306 m tower stands 37 degrees high, more than half the screen of a level camera, and its
foot is wider than Away island. Small things near the foot say how big the rest is: windows 1 m wide by the gate,
2 to 3 m ones in rings up the wall (lit dots from far away), the 0.5 m stair steps, the pier, gulls with a 5 m span
circling at four heights, palms that reach a fifth of the way to the first terrace.

## What it is made of

- **Models** (headless Blender, `tools/tower/world/world_tower.py`): one FBX per floor's section, the cloud cap, and
  the islet with the plaza, in `Assets/World/Tower/World/Models`. Flat shaded; everything a face needs is in its
  vertices (color, how much it glows, its kind), so the whole tower is drawn with **one material** and no texture.
  Each part exists twice: `_Near` and `_Far` (low detail, for far away and potato mode).
- **Shader** `Ravenfall/WorldTower` (`Assets/World/Tower/World/WorldTower.shader`): stone, the windows and runes
  that light up, falling water, cloud, green fire, lightning. It also solves the distance: see below.
- **Scene** `Assets/Scenes/Tower/WorldTower.unity`, made by `tools/tower/world/world-tower-build.cs`. The game loads
  it beside the main world at the start (`WorldTowerScene` in `Assets/Scripts/Game/Tower/WorldTower.cs`), in the
  editor by its path and in a build by its name. **MainWorld holds nothing of the tower.**
- **Synty props** dress the plaza (the builder places them): the hero's statue (Fantasy Kingdom, three times life
  size, given a pale stone material of its own), the banners and the bowls on the wall's posts (Dungeon), the big
  braziers and the ivy (Ancient Empire), palms, bushes, ferns and flowers (Nature Biomes, Tropical Jungle), the pier
  (Vikings), barrels and crates (Pirates), the sign board (Polygonal Fantasy Pack). Not Synty: the big masses on the
  tower itself (trees on the terraces, ice, cloud, spires: a prop 100 m up would vanish at the view distance, and
  they are the tower's outline), the islet's boulders (the jungle pack's rocks are brown and textured and sat on the
  grey islet like something dropped), and the flames in the braziers (the tower's own flame model, so they show from
  the other islands and need no particle system; the islands' brazier fire is found when MainWorld starts and would
  miss a scene loaded after it).

### The sections

| Floor | Section | Height | What shows |
|---|---|---|---|
| 1 | The Overgrown Halls | 0 to 90 m | mossy stone, a grassed terrace on corbels with trees, vines, six waterfalls (two tiers), the sun sigil in gold |
| 2 | The Frozen Vault | 90 to 160 m | blue stone under snow, ice crystals up to 40 m, icicles, snowed-in pines, frozen falls, the snowflake in blue |
| 3 | The Storm Spire | 160 to 222 m | weathered dark stone, lightning rods, a turning ring of thundercloud low round it with lightning, rocks adrift, the gale sigil |
| 4 | the Dark Fortress | 222 to 306 m | black stone flaring out over the storm, spikes, eight turrets with spires, green windows and fire, the trident (dark until v0.9.8 gives the floor) |
| - | the cap | from 306 m | the shaft goes on into a wide flat-floored cloud, so the tower has no top |

Every section has three or four rings of windows, a rune band with its sigil at the four winds, and steps in from the
one below, so the terraces show.

## Seen from every island

The game's camera sees 650 m (the View distance setting takes that from 32 m to 1300 m, and potato mode cuts it to
100 m); most of the world is further from the tower than that. The
shader's vertex stage pulls everything that lies beyond nine tenths of the view distance in toward the camera along
its own line of sight, into the last tenth of the view. On screen nothing moves (a point stays on its ray), only its
depth is squeezed, in the same order, so the tower hides itself correctly, an island between the camera and the tower
still hides its foot, and the far plane never cuts it. It follows the view
distance of whatever camera draws it, so it needs no setting and works in potato mode. With it:

- `WorldTower` gives the tower's renderers bounds that are never culled, and switches their shadows off beyond
  260 m (the game draws shadows 75 m out, so nothing would receive them);
- the game's haze reads the squeezed depth, so the shader adds the rest of the air between there and the tower's real
  place, with the Haze setting's own numbers (nothing is added when haze is off);
- what is under the sea is cut away when squeezed (the far sea writes no depth to hide it);
- the tower is not drawn beyond 3.6 km (the tower's own floors and towns lie 6 km out), nor from down in a dungeon.

Limits:

- The sky's volumetric clouds stop at the squeezed depth, so from further than the view distance the tower stands in
  front of all cloud, even on an overcast day, when by rights its top would be hidden. Up close the real clouds do
  drift in front of its upper sections.
- **The usual observe camera does not show it.** The camera that follows a player looks down at 45 degrees, so no
  horizon is on screen at all (`built-observe-camera-default.jpg`). The tower shows as soon as the streamer tilts
  that camera lower (`built-observe-camera-tilted.jpg`: 9 degrees, from Ironhill), and in the free camera, the
  island camera pulled back, and any cutscene that looks along the sea. An open call for the owner: a lower default
  tilt, or a camera beat that looks at the tower when a floor boss falls, would put it on stream by itself.

## Lit by the stream's progress

`WorldTower` reads `TowerManager.State` (what the server last said; the game invents nothing) twice a second:

- a floor the stream has **cleared**: its windows, runes and fire glow steadily;
- the floor the stream is **on**: it glows and breathes (down to 40% and back, every 4 seconds);
- a floor **not reached**: dark glass, no fire;
- before the server has answered: every floor dark;
- a floor boss falling while the game runs: the floor flares up for about 3 seconds and settles, the next begins to breathe.

Each section has its own copy of the material (`_Glow`, `_Pulse`); the gate, the plaza's sigils and the braziers are
always lit. `WorldTower.PreviewStep = 21` (editor only, from an eval) shows the light for a step without a server.

## Potato mode and cost

| | Normal | Potato mode, or further than 950 m |
|---|---|---|
| Tower triangles | 100,851 | 23,808 |
| Renderers | 11 for the tower, 12 small flames, 4 flocks of gulls | 10 |
| Light | steady, and breathing on the stream's floor | steady only |
| Moving | cloud ring and cap turn, rocks bob, clouds heave, fire wavers, lightning, gulls | none of it |
| Lamps | the gate's and four braziers' point lights | off |
| Shadows | cast within 260 m | cast within 260 m |

The Synty props on the plaza are another 86,168 triangles (the palms, the ferns and the statue are most of it),
drawn only within the view distance like any island's.

Measured in the editor's play mode from Home's shore, the tower and the plaza filling the view, view distance 650 m
(the editor's own statistics, every pass counted): with the tower 5,873 draw calls and 1,092,000 triangles, without it
5,658 and 857,000. So about 215 draw calls and 236,000 triangles more while all of it is on screen. The frame time
did not move outside the editor's noise (25 to 27 ms either way); it was not measured in a build or on a weak machine.

Nothing here has a setting of its own: the cost is triangles while it is in view, the moving parts are shader maths
and a handful of turning transforms, and all of it follows potato mode (`GameManager.PotatoMode`), as `TowerFx` does
for the floors. In potato mode the game's view distance is 100 m, so the plaza's props are not drawn at all from
anywhere but the islet, and the tower is its 23,808 triangles.

## The plaza

At the foot on the side facing Home: the gate with its tunnel of light and the tower's sun over it, a stair, the round
paved plaza with a compass (42 m across) on a round bastion, a low wall with fire bowls on its posts, stone steps down
to a landing and a wooden pier, the hero's statue on a pedestal to the left of the stair and the leaderboard stone to
the right.

- **The leaderboard stone** shows what the game has at hand: where the stream is ("FLOOR 2" over "PART 4", from
  `TowerWords.Place`, the letters about a metre high in bold Rodin) and the floor's name under it. The two carved
  rows under them are runes:
  the game has no list of a stream's best climbers (the server keeps each character's record and the world firsts,
  and the website shows them, but `api/tower` gives the game only the stream's own state). To fill them: an endpoint
  with the stream's top climbers (name, furthest floor and part), read once at the start and after each clear.
- **The statue** is a Synty hero. The concept's idea, the stream's best climber in stone, needs the same list.
- **The easter egg**: see docs/easter-eggs.md (`tower-elevator-out-of-order`).

## Visiting (not done: what it takes)

Built now: the islet's ground and the plaza have colliders, and the scene carries its own navmesh for the entrance
(the pier's landing, the steps, the plaza, the stair, the tunnel to the light). The builder checks that the landing
and the gate's mouth are joined by a walk. Markers `Arrival (quay)`, `Plaza` and `Gate` are in the scene. Own boats
sail round the islet (`tools/tower/world/sea-chart-islet.py` marks it as land on the sea chart; checked by asking the
chart for its routes: Eldara to Heim ran 11 m from the tower's middle before and passes 147 m off now, and no route
between two docks comes nearer than 144 m; no boat was watched sailing it).

Not built, because each step needs the server or the MainWorld scene saved:

1. **An island for it.** `Island` in RavenNest.Models has no value for the tower's islet, and where a player is gets
   saved by that enum. Needs `Island.Tower` on the server (sailing, saves, the island lists on the website) and an
   `IslandController` with a dock on the islet (it can live in WorldTower.unity, but the island list is gathered when
   MainWorld starts, so it has to be told about one that loads later).
2. **The ferry.** Its route is a ring of splines in MainWorld (`PathSelector`), like the stop at the Mangrove island:
   two splines (to and from the pier) and the stop's place in the ring. Needs MainWorld saved. The Away to Ironhill
   line passes 30 m from the pier's end, so the stop is short.
3. **Own boats.** Once the island exists: a dock entry for it on the sea chart (the chart's bake reads the islands).
4. **`!tower` from the plaza.** Today `!tower` takes a player into the tower from wherever they stand. From the plaza
   it should walk them up the stair into the light first: move to the `Gate` marker, on to the tunnel's end, then the
   join as now (`TowerManager.Join`), and the same way out when they leave.
5. **The camera.** Nothing needed for looking: the free camera and the island camera can go there now.

## Scripts

```
blender -b --python tools/tower/world/world_tower.py -- <repo root> [preview]
unity command eval_file "<repo>\tools\tower\world\world-tower-build.cs"
python tools/tower/world/sea-chart-islet.py <repo root>
```

1. Blender makes the models from `tools/tower/world/stack.json` and writes `stack-built.json` (heights, radii, the
   plaza's marks, triangle counts). `preview` renders pictures into `Temp/Blender/world-tower`.
2. The builder (editor not in play mode) makes the scene again from the models and `stack-built.json`: the stack, the
   foot, the props, the stone's words, the egg, the gulls, the navmesh. Its log: `%TEMP%/world-tower-build.txt`. It
   works in an additive scene of its own and closes it: the open scene is not touched.
3. The chart step only after the tower moved or the sea chart was baked again.

### Adding a floor's section

`stack.json` lists the sections bottom first; nothing assumes how many there are.

1. Add `{ "floor": 5, "style": "unbuilt", "height": 80, "glow": [r, g, b] }` to `sections`. `unbuilt` is a plain dark
   section with windows and a rune band; the cap moves up by itself.
2. For a look of its own, write a function in `world_tower.py` like `frost` or `storm` (it is given the radius it
   stands on, its height and the detail level, and returns its parts and the radius of its roof) and name it in
   `STYLES`.
3. Run the scripts again. The section lights up when the stream reaches that floor, with no change in the game's
   code: `WorldTower` lights by floor number, and the floors come from RavenNest.Combat's `Tower`.

A tower of very many floors cannot show them all under the cloud: lower the sections' heights in `stack.json`, or let
the cap hide the upper ones (the stream's floor then lights inside the cloud). That is a design call for when there
are more than six or so.

## What the build machine needs

- The Synty packs named above under "What it is made of" (they are not in git, like every pack), and the font
  `Assets/Fonts/FOT-NewRodin Pro B SDF.asset`.
- `Assets/Scenes/Tower/WorldTower.unity` **in the build's scene list** (File > Build Profiles), as the tower's floor
  and town scenes have to be. On 2026-11-24 the list on the development machine held only Update, MainWorld, Overlay
  and CodeOfConduct (`EditorBuildSettings.asset` is not in git). Without the entry the game plays as before, with no
  tower on the horizon, and says so once in the log.
- Nothing in `Assets/Resources`.

## Captures

`docs/mockups/tower/world/`, taken 2026-11-24 in play mode (local server, chat muted, the bot cut off), view distance
650 m unless said. The stream's real state on the dev server was floor 1, part 6.

| File | What |
|---|---|
| `built-from-home.jpg` | From Home's east shore, 300 m off: the stream's floor (1) lit, the rest dark |
| `built-from-away.jpg` | From Away's north shore, 280 m |
| `built-from-eldara.jpg` | From Eldara's west side, 300 m, over its roofs; lightning in the storm ring |
| `built-from-ironhill.jpg` | From Ironhill's south shore, 530 m |
| `built-from-kyo.jpg` | From Kyo, 770 m (past the view distance): Home lies in front of the tower's foot, as it should |
| `built-from-war.jpg` | From War, 1.5 km, zoomed in: the far model, hazed by the real distance |
| `built-from-ravebridge.jpg` | From Ravebridge, 2.6 km, zoomed in: the fortress and the cloud over Ravebridge's hills, which hide the rest |
| `built-wide-over-home.jpg` | From high over Home: the tower against Home's houses |
| `built-plaza.jpg` | The foot and the plaza from above the pier |
| `built-gate-and-stone.jpg` | The gate, the statue, the braziers and the leaderboard stone with the stream's real place |
| `built-egg.jpg` | The elevator and its sign |
| `built-light-floor-1-current.jpg` | Night, the server's state: floor 1 lit (it breathes), the others dark |
| `built-light-three-floors-cleared.jpg` | Night, the editor's preview of three floors cleared: 1 to 3 lit, the fortress dark |
| `built-storm-night.jpg` | A thunderstorm at night, from Away |
| `built-potato-mode.jpg` | Potato mode (view distance 100 m): the low detail tower, steady light, nothing moving |
| `built-observe-camera-default.jpg` | The usual camera on a player on Ironhill: the ground, no horizon, no tower |
| `built-observe-camera-tilted.jpg` | The same camera tilted low toward the tower |

Where it falls short of concept A: the cloud that hides the top is a low-poly cloud of its own and does not look like
the sky's volumetric clouds beside it (the small ones standing off it read a little like lenses); the sections carry
less growth and ice than the painting; the stone is flat blocks with no texture, which fits the low-poly look but is
plainer up close than the islands' textured ground; the sea round the islet shows the game's usual hard edge where
the sea floor ends at the view distance.
