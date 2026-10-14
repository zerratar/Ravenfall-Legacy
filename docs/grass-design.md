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

## Steps

1. **Blades from the terrain.** `GrassProfile`, `GrassField`, the cell generator, `GrassRenderer` and the grass
   shader, with generated blade clumps on the grass layers. Shots of every island, numbers for the frame time.
2. **Accents.** Synty tufts and flowers as further kinds, sparser.
3. **The brush.** The mask and its brush; village roads and buildings keep grass off without painting.
4. **Settings and numbers.** On/off, distance and density in the settings; the profiler with grass on and off.

Afterwards: grass bending around players and animals.
