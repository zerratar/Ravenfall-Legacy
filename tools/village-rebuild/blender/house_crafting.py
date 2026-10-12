# Crafting House: a workshop with an open smithy beside it: a glowing forge under a brick hood, an anvil on its
# stump, bellows, a quench barrel, a grindstone, a tool rack, a workbench with a new sword on it, and a lucky
# horseshoe over the door. About 9 x 9 m, facing +y (Unity -z).
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
b.quad_box([bw.p(-0.2, -0.45, 0.55), bw.p(0.2, -0.45, 0.55), bw.p(0.2, 0.35, 0.55), bw.p(-0.2, 0.35, 0.55)],
           [bw.p(-0.2, -0.45, 0.8), bw.p(0.2, -0.45, 0.8), bw.p(0.05, 0.35, 0.62), bw.p(-0.05, 0.35, 0.62)], "PAL", LEATHER)
for z in (0.5, 0.82):
    b.box(bw.p(0, -0.05, z), (0.46, 0.85, 0.05), "PAL", TIMBER_L, rot_z=bw.rot)
b.box(bw.p(0, 0.25, 0.27), (0.4, 0.4, 0.54), "PAL", TIMBER, rot_z=bw.rot)
# the anvil on its stump
af = f.sub(-1.8, -0.9, 0.4)
b.lathe(af.p(0, 0, 0), [(0.36, 0.0), (0.3, 0.1), (0.3, 0.55), (0.0, 0.55)], 9, "PAL", TIMBER)
b.box(af.p(0, 0, 0.6), (0.5, 0.3, 0.1), "PAL", IRON_D, rot_z=af.rot)
b.box(af.p(0, 0, 0.72), (0.26, 0.18, 0.16), "PAL", IRON_D, rot_z=af.rot)
b.box(af.p(-0.05, 0, 0.87), (0.6, 0.24, 0.14), "PAL", IRON_D, rot_z=af.rot)
log(b, af.p(0.25, 0, 0.88), af.p(0.5, 0, 0.9), 0.08, bark=IRON_D, end=IRON_D, n=6)
b.sphere(af.p(0.53, 0, 0.9), 0.045, "PAL", IRON_D, seg=5, rings=3)
b.box(af.p(-0.1, 0.0, 0.96), (0.32, 0.06, 0.03), "GLASS", EMBER, rot_z=af.rot + 0.3)                     # a bar still glowing
hm = af.sub(0.1, 0.05, 0.9)
bar(b, hm.p(0, 0, 0.97), hm.p(0.3, 0, 0.97), 0.035, 0.035, "PAL", TIMBER_L)
b.box(hm.p(0, 0, 0.99), (0.08, 0.14, 0.08), "PAL", STEEL, rot_z=hm.rot)
# the quench barrel and the grindstone
barrel(b, f, -0.9, -2.6, r=0.34, h=0.75)
b.lathe(f.p(-0.9, -2.6, 0), [(0.28, 0.71), (0.0, 0.71)], 10, "GLASS", "396b7c")
gs = f.sub(-1.2, 0.6, 0.6)
for s in (-1, 1):
    b.box(gs.p(s * 0.25, 0, 0.4), (0.08, 0.5, 0.8), "PAL", TIMBER, rot_z=gs.rot)
c = Vector(gs.p(0, 0, 0.82)); ax = gs.R @ Vector((1, 0, 0)); fwd = gs.R @ Vector((0, 1, 0)); up = Vector((0, 0, 1))
r0 = [c - ax * 0.07 + (fwd * math.cos(2 * math.pi * i / 10) + up * math.sin(2 * math.pi * i / 10)) * 0.38 for i in range(10)]
r1 = [v + ax * 0.14 for v in r0]
b.face([tuple(v) for v in r1], ax, "PAL", STONE); b.face([tuple(v) for v in r0], -ax, "PAL", STONE)
for i in range(10):
    j = (i + 1) % 10
    b.face([tuple(r0[i]), tuple(r0[j]), tuple(r1[j]), tuple(r1[i])], (r0[i] + r0[j]) / 2 - (c - ax * 0.07), "PAL", STONE_D)
bar(b, gs.p(-0.32, 0, 0.82), gs.p(0.42, 0, 0.82), 0.04, 0.04, "PAL", IRON_D)
bar(b, gs.p(0.42, 0, 0.82), gs.p(0.42, 0.18, 0.62), 0.04, 0.04, "PAL", IRON_D)

# tool rack on the workshop's east wall: hammers and tongs
tw = hf.sub(-2.2, 0.2, math.pi / 2)
b.box(tw.p(0, 0.06, 1.7), (1.4, 0.06, 0.12), "PAL", TIMBER_L, rot_z=tw.rot)
for k, x in enumerate((-0.45, -0.15, 0.15, 0.45)):
    bar(b, tw.p(x, 0.12, 1.65), tw.p(x, 0.12, 1.05), 0.035, 0.035, "PAL", TIMBER_L if k % 2 == 0 else IRON_D)
    if k % 2 == 0: b.box(tw.p(x, 0.12, 1.66), (0.18, 0.07, 0.08), "PAL", STEEL, rot_z=tw.rot)
# the workbench with a new sword and a chest waiting for its lock
wb = f.sub(1.4, 1.5, 0.0)
b.box(wb.p(0, 0, 0.82), (1.8, 0.8, 0.1), "PAL", TIMBER_L, rot_z=wb.rot)
for x in (-0.8, 0.8):
    for y in (-0.32, 0.32):
        b.box(wb.p(x, y, 0.4), (0.12, 0.12, 0.8), "PAL", TIMBER, rot_z=wb.rot)
bar(b, wb.p(-0.6, 0.0, 0.89), wb.p(-0.38, 0.0, 0.89), 0.04, 0.04, "PAL", TIMBER)
bar(b, wb.p(-0.38, -0.12, 0.89), wb.p(-0.38, 0.12, 0.89), 0.04, 0.04, "PAL", GOLD)
bar(b, wb.p(-0.36, 0.0, 0.88), wb.p(0.55, 0.0, 0.88), 0.07, 0.02, "PAL", STEEL)
cf = f.sub(3.3, 0.8, -0.3)
b.box(cf.p(0, 0, 0.3), (0.9, 0.6, 0.6), "PAL", TIMBER_L, rot_z=cf.rot)
b.box(cf.p(0, 0, 0.66), (0.92, 0.62, 0.14), "PAL", TIMBER, rot_z=cf.rot)
for x in (-0.3, 0.3):
    b.box(cf.p(x, 0, 0.36), (0.07, 0.63, 0.74), "PAL", IRON_D, rot_z=cf.rot)
b.box(cf.p(0, 0.31, 0.5), (0.1, 0.03, 0.12), "PAL", GOLD, rot_z=cf.rot)
# a stack of bars and a crate of ore
for k in range(3):
    for j in range(3 - k):
        b.box(f.p(3.3 + j * 0.2 + k * 0.1, 2.6, 0.06 + k * 0.1), (0.16, 0.5, 0.09), "PAL", STEEL if k % 2 else "8b8b8b", rot_z=f.rot)
crate(b, f, 3.5, 3.4, s=0.55)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Crafting")
preview([house], "house_crafting", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
