# Grass

Dense grass on the islands, drawn the same way as the trees and gather spots (one `BatchRendererGroup`, culled in a
Burst job), but generated instead of placed: nothing per blade is saved, it grows around the camera from the
terrain and a painted mask.

## Decisions

- **Where it grows:** by itself wherever the terrain is painted with a grass layer, plus a brush to add or remove
  grass (around buildings, on paths, for meadows).
- **The look:** dense generated blade clumps as the base, coloured from the terrain beneath so they blend into it,
  with Synty tufts and flowers scattered among them as accents.
- **Bending around characters:** later. First wind sway and a soft fade at the edge of the grass distance.
- **Settings:** grass on or off, its distance and its density, so slower machines can turn it down. The renderer
  takes them as values; wiring them into the settings screen comes with the last step.

## What the terrain gives

MainWorld has one 2560 m terrain under every island. Which of its layers carry grass, sampled on each island:

| Layer | Name | Where |
|---|---|---|
| 0 | Meadow Dirt | Small Island (Away); also the sea floor |
| 1 | Footpath tiles | paths, so no grass |
| 2 | Meadow Grass 01 | Home, Ironhill, Kyojin, Eldara |
| 3 | Meadow Grass 02 | Atria, edges elsewhere |
| 4 | Mud | no grass |
| 5 | Snow grass | Iceheim |
| 6 | Rock | no grass |

The water is at -3.3 m, so nothing grows below it (the sea floor is layer 0 too). The layer textures are not
readable at runtime, so the editor bakes each layer's average colour into the grass profile once.

## Data

- **`GrassProfile`** (asset): the terrain layers and how dense grass is on each, the colour of each layer, and the
  grass kinds: a kind is a mesh (generated blades, or a Synty tuft or flower), its material, how many per square
  metre at full density, its size range and how far it shows. The blades are many and short range; the accents are
  few and show a bit further.
- **`GrassField`** (component in the scene, one per terrain): the terrain, the profile, and the painted mask, a
  small grid of bytes over the terrain (one per metre: neutral, none, or up to twice as dense) kept in an asset next
  to the scene. The brush in its inspector paints that mask, like the resource brush.

## Runtime

- The world is split into 8 m cells. Around each camera, every cell within the grass distance is generated once in
  a Burst job: a jittered grid of points at the profile's density, each kept by the terrain layers' density under
  it, the mask, the slope and the water line; height and normal from the heightmap; a colour from the layers'
  colours; a random turn, size and rank. The same cell always grows the same grass.
- Cells get a slot in one GPU buffer (transforms and colours) and keep it while they are in range; a cell that
  leaves frees its slot, so moving the camera only generates the cells at the edge.
- Culling, per camera and per shadow pass, is a Burst job like the resource renderer's: cells against the view,
  then per instance a thinning by distance (instances with a high rank drop out further away, so density falls off
  smoothly), and the draw commands. Grass receives shadows and casts none.
- **The shader** (URP, with the instancing the renderer needs): the colour per instance, wind as a travelling wave
  bending blades more at the tip, and a shrink to nothing at the end of the grass distance so cells never pop.
- When the terrain changes (village build mode reshapes it), the cells over the change are generated again.

## How it came out

- **The blades** are generated clumps of ten blades fanning out from the middle, so from the game camera above
  their faces show instead of thin edges; past half the grass distance a clump is five single-segment blades
  (5 triangles instead of 50). The profile has 14 clumps per square metre at full density.
- **The accents** are two Fantasy Kingdom grass tufts and five small flowers (Adventure and Fantasy Kingdom), the
  flowers only where the ground is lush: Home, Kyojin and Atria get flowery meadows, dry Small Island and snowy
  Iceheim none.
- **Blending:** a blade's root has the ground's colour and is lit as if it faced up, as the terrain is; the tip is
  lighter. Far grass melts into the ground colour. The profile no longer tints the ground green, so the snow grass
  is white. Broad noise patches make parts of a meadow lighter or drier.
- **Tricks that cost almost nothing:** blades bending in the wind catch more light, so gusts roll across a meadow;
  tips glow a little with the sun behind them; no alpha test, so the GPU keeps early depth rejection; no shadows
  cast.
- **Roads:** village roads and the roads drawn in the Scene view (`WorldRoad`) tell the grass their middle and
  width, and nothing grows on the solid road and 0.3 m beyond. **Village plots** (`TownHouseSlot`) keep grass off
  the box round their plot, house and yard. Building, moving or removing either grows the cells under it again.
- **The mask** (`GrassMask`) is a byte per square metre in 32 m tiles, kept only where painted; the brush in the
  field's inspector has More, Less and Reset.
- **Settings:** Graphics > Quality: Grass, Grass distance (20 to 120 m, 60 by default) and Grass density. Potato
  mode keeps grass off without changing the setting.
- Not done: grass on islands whose ground is a mesh rather than the terrain (the village island from the rebuild):
  only a few tufts poke through there. Growing on meshes would need a height pass over them.

## Numbers

Measured in the editor on 2026-10-14 (the editor adds its own cost; a build will be faster):

- Around the camera: about 215 cells and 100,000 to 175,000 instances grown, depending on the island; growing all of
  them takes about 40 ms once (the ground is read in a Burst job), and moving the camera grows only the edge.
- The Home view from the game camera draws 6,500 near and 15,600 far clumps, about 405,000 triangles (250,000 in the
  play mode view).
- Play mode, the game camera on Home: 33.8 ms a frame with grass, 30.7 ms without (about 3 ms; editor, not in
  focus).
- GPU memory: about 35 MB at 60 m (the slots for every cell that can be in range); the CPU keeps the points (5 MB)
  and the ground maps (12 MB).

## Steps

1. **Blades from the terrain.** Done.
2. **Accents.** Done.
3. **The brush, roads and plots.** Done.
4. **Settings and numbers.** Done; profiling a build is still to do.

Afterwards: grass bending around players and animals; grass on mesh ground.
