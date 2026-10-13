# Barracks (melee): a long hall with a training yard of sand, straw dummies (one knocked flat), a weapon rack with
# swords and spears, shields on the wall and a banner. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Barracks")
f = Frame()
STEEL, SAND = "b9b9b9", "c6a177"

def swords_emblem(b, ef, z):
    emb(b, ef, z, [(-0.17, 0.17), (0.17, 0.17), (0.17, -0.02), (0.0, -0.2), (-0.17, -0.02)], RED, d=0.015)
    for s in (-1, 1):
        emb_bar(b, ef, z, (-0.2 * s, -0.17), (0.2 * s, 0.2), 0.035, STEEL, d=0.025)
        emb_bar(b, ef, z, (-0.13 * s, -0.04), (-0.05 * s, -0.12), 0.03, GOLD, d=0.03)

def sword(b, a, c, w=0.07):
    a, c = Vector(a), Vector(c); d = (c - a).normalized()
    bar(b, tuple(a), tuple(a + d * 0.22), 0.04, 0.04, "PAL", TIMBER)                 # grip
    s = d.cross(Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))).normalized()
    bar(b, tuple(a + d * 0.24 - s * 0.14), tuple(a + d * 0.24 + s * 0.14), 0.05, 0.05, "PAL", GOLD)
    bar(b, tuple(a + d * 0.26), tuple(c), w, 0.02, "PAL", STEEL)

def shield(b, c, n, r, col, rim=TIMBER):
    c = Vector(c); n = Vector(n).normalized()
    side = n.cross(Vector((0, 0, 1))).normalized(); up = side.cross(n)
    disc(b, c, n, side, up, r, rim, segs=10)
    disc(b, c + n * 0.02, n, side, up, r * 0.85, col, segs=10)
    b.sphere(tuple(c + n * 0.04), r * 0.18, "PAL", STEEL, seg=6, rings=3)

yard(b, f, 4.6, 4.4, DIRT, seed=10.0)
for k in range(2):
    b.face([f.p(-4.0, 0.0 + k * 1.9, 0.07), f.p(1.6, 0.0 + k * 1.9, 0.07), f.p(1.6, 1.9 + k * 1.9, 0.07), f.p(-4.0, 1.9 + k * 1.9, 0.07)], (0, 0, 1), "PAL", SAND)

# the long hall
hf = f.sub(0.6, -2.5, 0.0)
cottage(b, hf, 6.6, 3.6, 3.2, roof="ROOF_RED", pitch=40, overhang=0.45, plinth=0.5, shutters="red",
        front_windows=(-2.3, 2.3), side_windows=(0.0,), back_windows=(-1.6, 1.6), door_x=0.0, chimney=(2.4, -0.5))
fw = hf.sub(0, 1.8, 0)
sign(b, fw, 3.35, 2.6, 0.0, swords_emblem)
for x, col in ((-1.15, RED), (1.15, BLUE_C)):
    shield(b, fw.p(x, 0.06, 2.3), fw.R @ Vector((0, 1, 0)), 0.3, col)
    for s in (-1, 1):
        sword(b, fw.p(x - s * 0.3, 0.1, 1.95), fw.p(x + s * 0.32, 0.1, 2.68))

# the training yard: three dummies standing out at the far edge of the sand, beside and between the openings
# rather than in front of them, one knocked flat nearer the hall where it hides nothing
dummy(b, f, -3.5, 1.1, yaw=0.2)
dummy(b, f, -2.6, 3.0, yaw=-0.1)
dummy(b, f, -0.6, 3.1, yaw=0.3)
# the fallen one, flat on its back with its bucket rolled away
ff = f.sub(-1.8, 1.4, 0.4)
b.box(ff.p(0, 0, 0.1), (1.6, 0.12, 0.12), "PAL", TIMBER, rot_z=ff.rot)
b.lathe(ff.p(-0.1, 0, 0.0), [(0.0, 0.0), (0.3, 0.05), (0.24, 0.3), (0.0, 0.32)], 7, "PAL", HAY)
bar(b, ff.p(0.3, -0.6, 0.12), ff.p(0.3, 0.6, 0.12), 0.1, 0.1, "PAL", TIMBER_L)
b.lathe(ff.p(1.15, 0.35, 0.0), [(0.0, 0.0), (0.16, 0.0), (0.16, 0.24), (0.12, 0.24)], 8, "PAL", "8b8b8b")
# the weapon rack: swords and spears, past the end of the hall so it stays clear of the window
rf = f.sub(4.15, 1.3, -math.pi / 2)
for s in (-1, 1):
    b.box(rf.p(s * 0.8, 0, 0.75), (0.14, 0.14, 1.5), "PAL", TIMBER, rot_z=rf.rot)
for z in (0.25, 1.35):
    b.box(rf.p(0, 0, z), (1.75, 0.14, 0.1), "PAL", TIMBER, rot_z=rf.rot)
for k, x in enumerate((-0.55, -0.2, 0.2, 0.55)):
    if k % 2 == 0:
        sword(b, rf.p(x, 0.12, 0.2), rf.p(x, 0.16, 1.55))
    else:
        bar(b, rf.p(x, 0.12, 0.1), rf.p(x, 0.14, 2.0), 0.05, 0.05, "PAL", TIMBER_L)
        b.prism(rf.p(x, 0.14, 2.0), 0.06, 0.28, 4, "PAL", STEEL, r_top=0.0)
# a banner pole (out at the yard's corner, past the end of the hall), a water barrel and a bench
BX = 4.2
b.box(f.p(BX, 3.5, 1.6), (0.12, 0.12, 3.2), "PAL", TIMBER, rot_z=f.rot)
bar(b, f.p(BX, 3.5, 3.1), f.p(BX, 2.7, 3.1), 0.08, 0.08, "PAL", TIMBER)
for s in (1, -1):
    b.face([f.p(BX, 2.75, 3.05), f.p(BX, 3.45, 3.05), f.p(BX, 3.45, 1.8), f.p(BX, 3.1, 2.05), f.p(BX, 2.75, 1.8)], (s, 0, 0), "PAL", RED)
b.sphere(f.p(BX, 3.5, 3.25), 0.08, "PAL", GOLD, seg=6, rings=3)
barrel(b, f, 2.2, 0.0)
b.lathe(f.p(2.2, 0.0, 0.0), [(0.26, 0.76), (0.0, 0.76)], 10, "GLASS", "396b7c")
b.box(f.p(2.0, 3.3, 0.45), (1.6, 0.45, 0.08), "PAL", TIMBER_L, rot_z=f.rot)
for x in (1.4, 2.6):
    b.box(f.p(x, 3.3, 0.22), (0.1, 0.4, 0.44), "PAL", TIMBER, rot_z=f.rot)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Barracks")
preview([house], "house_barracks", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
