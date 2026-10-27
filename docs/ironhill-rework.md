# Ironhill rework

Ironhill had no hill: a flat grass disc, a moss-green pile of cliff meshes in the north-west (the runestone cave on
top), cute dragons loose on the grass, a windmill, a scarecrow farm and two burned houses. Nothing said dwarves, and
the dragons had no home. The runestone story (docs/quests-design.md) says power stirs in the cave and "something below
stirs"; the island should look like the place where that happens.

Concept: docs/mockups/ironhill-concept.png (the target, not a blueprint: the island must stay playable).

## Direction

- **The iron hill (north-west).** The cliff pile becomes grey iron-stone with rusty veins: its own copy of the
  triplanar rock material, so other islands keep their moss. The terrain under it rises so the hill reads as one mass.
- **The dwarf hold (the hill's foot, facing the centre).** A carved gate and mine entrance (PolygonDungeonRealms dwarf
  kit), rails out to a forge yard (smelters, anvils, the lava wheel, braziers, chimney smoke), a few dwarf houses on
  terraces, dwarf NPCs (miner, worker, soldier at the gate).
- **Dragon country (east).** Scorched ground around the burned farm, bone piles and a giant dragon skeleton along the
  edges, an egg nest, sparse glowing cracks. The cute dragons (the level 100 fight spot) keep their open field.
- **The sleeper.** A big dragon (Malbers "Unka Poly Art", sleep animation) curled on a ledge on the hilltop, above
  the runestone cave.

## Rules

- Keep every skill spot working and reachable: fighting field, mining rocks, trees, farm, fishing dock. Rebake the
  island's navmesh after terrain changes and check the enemies and gather spots still stand on it.
- Decor along edges and behind spots, never across walking lines.
- Judge every step from the streamer's real views (overview, island camera, close-up) and iterate.

## Built (2026-10-26)

- **Mountain:** the world terrain rises to about 52 m along a ridge across the north (5 m height samples, so ~70 grey
  crags carry the detail); rock painted on steep faces, dark gravel on the shoulders. The cliff meshes use their own
  material, `Assets/World/Terrain/Ironhill Iron Stone.mat` (grey rock, mud gravel on top faces).
- **Cave:** untouched inside; the ground round it only climbs to its roof's height so the outcrop stays in sight. One
  roof piece over the runestone was removed so its glow and quest mark show from above again.
- **Dwarf hold:** a flat yard cut into the south face; the big dwarf gate at its back, obelisks, braziers, a paved way,
  smelter tower, lava wheel, crane, fire pit and anvil, ore carts, two houses, four dwarves on the island NPCs' idle.
- **Dragon country:** scorched ground east of the dragons' field, burnt trees, a dragon skeleton on the east shore, an
  egg nest at the mountain's foot, the burned house from the yard moved here; the green grove on that shore removed.
- **The sleeper:** Unka (poly art, red) asleep on the summit over the gate, seen from the island; its own animator in
  `Assets/World/Ironhill`.
- The woodcutting tree that stood in the yard moved into the tree field; every other spot was already on the ground.
  The fight field, trees, farm, dock and gathering spots did not move, so the navmesh there still holds.

The dragon is scenery only (no colliders, no enemy, Default layer): nobody can reach the summit, so it must never be a
target there. A future quest could wake it for a raid battle on the island itself, where it comes down to fight.

## Built (2026-10-27)

- **Lava cracks:** `LavaCrack` (Assets/Scripts/World) draws a jagged seam with branches, laid on the terrain, with
  `Ravenfall/Lava Crack` (Assets/World/Ironhill): a hot middle cooling to red lips, heat drifting along it, the whole
  crack breathing, and burnt ground round it that fades out raggedly. Seven of them (tools/ironhill/ih-cracks.cs): two
  from the mountain's foot to the dragon eggs, four sparse ones in the scorched east, one in the forge yard by the
  smelter. They glow with the bloom at night. The mesh is made when the crack wakes and never saved, so the scene keeps
  only the numbers (seed, length, widths, branches, heat).
- **The forge-master:** the dwarf worker at the anvil is an `IslandNpc` (key `ironhill_smith`) who gives the
  runestone's story: the gold ! to begin it is over them now (the runestone keeps its ? and the road west's marks), a
  grey ? while the stream brings the iron and coal for the stone's bands. A click says where the story stands and
  opens the quest window on it (StoryLog.SmithClicked). Now and then they talk on their own: their lines plus one for
  each step of the runestone and the road west (UI_IRONHILL_SMITH_*). Speech bubbles grow with the camera's distance
  now, so they can be read from the island camera.
- **The forge corner:** the big crag east of the yard had swallowed the anvil, fire pit, smelting pot, the forge-master,
  the lava wheel, the crane, a house and a cart; nobody had ever seen them. They moved into the open strip right of the
  paved way (the lava wheel half into the rock face, the house to the yard's south-west corner, the cart by the
  smelter); the crane, the second house and one cart were dropped. ih-hold.cs has the new places.

Ideas for later: a dwarf story of their own (the dragon's breakfast, the sheep on the anvil), and the quest that wakes
the sleeping dragon for a raid on the island.

## Phases

1. Hill shape and iron-stone material.
2. Dwarf hold: gate, rails, forge yard, houses, NPCs.
3. Dragon country: scorched ground, bones, skeleton, nest, cracks.
4. The sleeping dragon.
5. Navmesh, spots, polish passes.
