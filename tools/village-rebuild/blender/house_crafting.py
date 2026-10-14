# Crafting House: a workshop with an open smithy beside it: a glowing forge under a brick hood, an anvil on its
# stump, bellows, a quench barrel, a grindstone, a tool rack, a workbench with a new sword on it, and a lucky
# horseshoe over the door. The bellows, anvil, stump, hammer, grindstone, workbench, sword, chest, barrel and crate
# are Synty props; the workshop, the smithy and its forge, the tool rack and the horseshoe are ours.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Crafting")
f = Frame()
STEEL, IRON_D, EMBER, LEATHER = "b9b9b9", "3a2e2c", "f27250", "6f4f3a"

def hammer_emblem(b, ef, z):
    emb_bar(b, ef, z, (-0.12, -0.2), (0.08, 0.12), 0.05, TIMBER_L, d=0.02)
    emb(b, ef, z, [(-0.06, 0.1), (0.14, 0.22), (0.2, 0.12), (0.0, 0.0)], STEEL, d=0.025)
    emb(b, ef, z, [(-0.22, -0.2), (0.06, -0.2), (0.0, -0.12), (-0.16, -0.12)], IRON_D, d=0.03)

yard(b, f, 4.6, 4.4, DIRT, seed=12.0)

# the workshop
hf = f.sub(2.0, -2.2, 0.0)
cottage(b, hf, 4.4, 3.6, 3.0, roof="SLAT_RED", pitch=40, overhang=0.45, plinth=0.4, shutters="brown",
        front_windows=(-1.2,), side_windows=(0.0,), back_windows=(0.0,), door_x=0.8, chimney=None)
fw = hf.sub(0, 1.8, 0)
sign(b, fw, 2.25, 2.4, 0.0, hammer_emblem)
# the horseshoe over the door, points up so the luck stays in
hc = Vector(fw.p(0.8, 0.14, 2.45)); n = fw.R @ Vector((0, 1, 0)); side = fw.R @ Vector((1, 0, 0))
pts =[hc + (side * math.cos(-math.pi * (0.1 + 0.8 * k / 7)) + Vector((0, 0, math.sin(-math.pi * (0.1 + 0.8 * k / 7))))) * 0.16 for k in range(8)]
for k in range(7):
    bar(b, tuple(pts[k]), tuple(pts[k + 1]), 0.05, 0.03, "PAL", STEEL, normal=n)

# the smithy: four posts and a slat roof over the forge
sx0, sx1, sy0, sy1 = -4.0, -0.6, -3.5, -0.4
for x in (sx0, sx1):
    for y in (sy0, sy1):
        b.box(f.p(x, y, 1.4), (0.24, 0.24, 2.8), "PAL", TIMBER, rot_z=f.rot)
for y in (sy0, sy1):
    bar(b, f.p(sx0 - 0.2, y, 2.8), f.p(sx1 + 0.2, y, 2.8), 0.22, 0.22, "PAL", TIMBER)
lo = [Vector(f.p(sx0 - 0.4, sy1 + 0.45, 2.72)), Vector(f.p(sx1 + 0.4, sy1 + 0.45, 2.72)), Vector(f.p(sx1 + 0.4, sy0 - 0.45, 3.3)), Vector(f.p(sx0 - 0.4, sy0 - 0.45, 3.3))]
if (lo[1] - lo[0]).cross(lo[2] - lo[1]).z < 0: lo = lo[::-1]
fs = b.quad_box(lo, [v + Vector((0, 0, 0.16)) for v in lo], "SLAT_RED"); b.paint([fs[0]], "PAL", TIMBER)
# the forge: a stone hearth with glowing coals, a brick hood and its chimney through the roof
gf = f.sub(-3.1, -2.7, 0.0)
b.box(gf.p(0, 0, 0.42), (1.5, 1.1, 0.84), "WALL", rot_z=gf.rot)
b.box(gf.p(0, 0, 0.86), (1.1, 0.75, 0.06), "GLASS", EMBER, rot_z=gf.rot)
for k in range(5):
    b.sphere(gf.p(-0.35 + 0.17 * k, 0.1 * math.sin(k * 2), 0.9), 0.1, "PAL", IRON_D if k % 2 else "5d252c", seg=5, rings=3, sz=0.6)
hood_lo = [Vector(gf.p(-0.8, -0.6, 1.7)), Vector(gf.p(0.8, -0.6, 1.7)), Vector(gf.p(0.8, 0.6, 1.7)), Vector(gf.p(-0.8, 0.6, 1.7))]
hood_hi = [Vector(gf.p(-0.35, -0.35, 2.7)), Vector(gf.p(0.35, -0.35, 2.7)), Vector(gf.p(0.35, 0.25, 2.7)), Vector(gf.p(-0.35, 0.25, 2.7))]
b.quad_box(hood_lo, hood_hi, "WALL")
b.box(gf.p(0, -0.05, 3.5), (0.7, 0.6, 1.7), "WALL", rot_z=gf.rot)
b.box(gf.p(0, -0.05, 4.4), (0.85, 0.75, 0.14), "PAL", STONE, rot_z=gf.rot)
for s in (-1, 1):
    b.box(gf.p(s * 0.62, 0.4, 1.25), (0.12, 0.12, 0.85), "PAL", STONE_D, rot_z=gf.rot)
# bellows beside the forge
bw = gf.sub(1.15, 0.1, 0.0)
anchor(bw, "bellows", 0, 0, yaw=-math.pi / 2)
# the anvil on its stump, a hammer on it and a bar still glowing
af = f.sub(-1.8, -0.9, 0.4)
anchor(af, "stump", 0, 0, s=1.1)
anchor(af, "anvil", 0, 0, 0.52, yaw=math.pi / 2)
b.box(af.p(-0.05, 0.0, 0.99), (0.32, 0.06, 0.03), "GLASS", EMBER, rot_z=af.rot + 0.3)
anchor(af, "hammer", 0.2, 0.12, 0.98, tilt=math.pi / 2)
# the quench barrel and the grindstone
barrel(b, f, -0.9, -2.6, r=0.34, h=0.75)
anchor(f, "grindstone", -1.2, 0.6, yaw=0.6)

# tool rack on the workshop's east wall: hammers and tongs
tw = hf.sub(-2.2, 0.2, math.pi / 2)
b.box(tw.p(0, 0.06, 1.7), (1.4, 0.06, 0.12), "PAL", TIMBER_L, rot_z=tw.rot)
for k, x in enumerate((-0.45, -0.15, 0.15, 0.45)):
    bar(b, tw.p(x, 0.12, 1.65), tw.p(x, 0.12, 1.05), 0.035, 0.035, "PAL", TIMBER_L if k % 2 == 0 else IRON_D)
    if k % 2 == 0: b.box(tw.p(x, 0.12, 1.66), (0.18, 0.07, 0.08), "PAL", STEEL, rot_z=tw.rot)
# the workbench with a new sword on it, and a chest waiting for its lock
wb = f.sub(1.4, 1.5, 0.0)
anchor(wb, "workbench", 0, 0)
anchor(wb, "sword", 0.0, 0.05, 0.86, yaw=math.pi / 2, tilt=math.pi / 2, s=0.9)
anchor(f, "chest", 3.3, 0.8, yaw=-0.3)
# a stack of bars and a crate of ore
for k in range(3):
    for j in range(3 - k):
        b.box(f.p(3.3 + j * 0.2 + k * 0.1, 2.6, 0.06 + k * 0.1), (0.16, 0.5, 0.09), "PAL", STEEL if k % 2 else "8b8b8b", rot_z=f.rot)
crate(b, f, 3.5, 3.4, s=0.55)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Crafting")
preview([house], "house_crafting", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
