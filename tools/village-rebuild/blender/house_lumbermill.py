# Lumbermill (woodcutting): an office cottage, an open saw shed with a big circular saw biting into a log, log
# stacks, stacked planks, an axe in a chopping stump, and a beaver who has clearly moved in.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Lumbermill")
f = Frame()
STEEL, FUR, FUR_D = "b9b9b9", "6f4f3a", "4d3828"

def axe_emblem(b, ef, z):
    emb_bar(b, ef, z, (-0.16, -0.2), (0.14, 0.2), 0.05, TIMBER_L, d=0.02)
    emb(b, ef, z, [(0.04, 0.1), (0.2, 0.2), (0.24, 0.02), (0.1, 0.0)], STEEL, d=0.025)

def stump(b, f, x, y, r=0.38, h=0.5):
    c = f.p(x, y, 0)
    b.lathe(c, [(r * 1.25, 0.0), (r, 0.12), (r, h)], 9, "PAL", TIMBER)
    b.lathe(c, [(r, h), (0.0, h)], 9, "PAL", "c6a177")

yard(b, f, 4.6, 4.4, DIRT, seed=3.0)

# the office
of = f.sub(1.9, -2.2, 0.0)
cottage(b, of, 4.2, 3.4, 3.0, roof="ROOF_RED", pitch=40, overhang=0.45, plinth=0.4, shutters="green",
        front_windows=(-1.1,), side_windows=(0.0,), back_windows=(0.0,), door_x=0.8, chimney=(-1.2, -0.6))
sign(b, of.sub(0, 1.7, 0), 2.15, 2.4, 0.0, axe_emblem)

# the saw shed: four posts and a lean-to slat roof
sx0, sx1, sy0, sy1 = -4.0, -0.7, -3.4, -0.6
for x in (sx0, sx1):
    for y, h in ((sy0, 3.0), (sy1, 2.5)):
        b.box(f.p(x, y, h / 2), (0.24, 0.24, h), "PAL", TIMBER, rot_z=f.rot)
for y, h in ((sy0, 3.0), (sy1, 2.5)):
    bar(b, f.p(sx0 - 0.2, y, h), f.p(sx1 + 0.2, y, h), 0.22, 0.22, "PAL", TIMBER)
lo = [Vector(f.p(sx0 - 0.4, sy1 + 0.5, 2.38)), Vector(f.p(sx1 + 0.4, sy1 + 0.5, 2.38)), Vector(f.p(sx1 + 0.4, sy0 - 0.4, 3.1)), Vector(f.p(sx0 - 0.4, sy0 - 0.4, 3.1))]
if (lo[1] - lo[0]).cross(lo[2] - lo[1]).z < 0: lo = lo[::-1]
fs = b.quad_box(lo, [v + Vector((0, 0, 0.16)) for v in lo], "SLAT_BROWN"); b.paint([fs[0]], "PAL", TIMBER)
# the saw bench, a log on it, and the blade
bf = f.sub((sx0 + sx1) / 2, (sy0 + sy1) / 2, 0.0)
b.box(bf.p(0, 0, 0.85), (3.0, 0.9, 0.12), "PAL", TIMBER_L, rot_z=bf.rot)
for x in (-1.3, 1.3):
    for y in (-0.35, 0.35):
        b.box(bf.p(x, y, 0.42), (0.14, 0.14, 0.84), "PAL", TIMBER, rot_z=bf.rot)
anchor(bf, "log", -0.45, 0.0, 0.92, yaw=math.pi / 2, s=0.8)          # the log being cut, on the bench
c = Vector(bf.p(0.75, 0.0, 1.2)); n = bf.R @ Vector((0, 1, 0)); side = bf.R @ Vector((1, 0, 0)); up = Vector((0, 0, 1))
for s in (1, -1):
    pts = []
    for i in range(24):
        a = 2 * math.pi * i / 24
        rr = 0.8 if i % 2 == 0 else 0.72                      # teeth
        pts.append(tuple(c + n * (0.02 * s) + (side * math.cos(a) + up * math.sin(a)) * rr))
    b.face(pts, n * s, "PAL", STEEL)
b.sphere(tuple(c + n * 0.03), 0.08, "PAL", DARK, seg=6, rings=3)
for k in range(5):
    b.sphere(bf.p(0.9 + 0.1 * k, 0.15 * math.sin(k), 0.93), 0.06, "PAL", "c6a177", seg=4, rings=3, sz=0.4)   # sawdust

# log stacks, plank stacks, the chopping stump with its axe
log_stack(b, f, -2.5, 1.6, length=2.8, rows=3, yaw=0.15)
log_stack(b, f, -2.3, 3.4, length=2.2, rows=2, yaw=-0.1, r=0.18)
# stacked planks, each a Synty plank laid from its end (they run towards -x), on two bearers
for k in range(7):
    anchor(f, "plank", 3.8 + 0.03 * math.sin(k * 2.0), 1.35 + 0.18 * (k % 3) + 0.05 * k, 0.1 + 0.05 * k, yaw=math.pi / 2 + 0.02 * math.sin(k))
for x in (1.6, 3.6):
    b.box(f.p(x, 1.6, 0.05), (0.15, 1.0, 0.1), "PAL", TIMBER, rot_z=f.rot)
anchor(f, "stump", 0.6, 2.4, s=1.1)
anchor(f, "axe", 0.62, 2.4, 0.42, yaw=0.5, tilt=math.radians(-25))     # stuck in the stump
anchor(f, "stump", 3.6, 3.4, s=0.8)
for k in range(4):
    b.box(f.p(1.2 + 0.25 * k, 3.0 + 0.1 * k, 0.06), (0.35, 0.12, 0.12), "PAL", "c6a177", rot_z=f.rot + k)     # split firewood

# the beaver, sitting by the logs and looking pleased with itself
vf = f.sub(-0.9, 2.7, 0.6)
b.sphere(vf.p(0, 0, 0.26), 0.26, "PAL", FUR, seg=7, rings=4, sz=0.9)
b.sphere(vf.p(0, 0.24, 0.42), 0.15, "PAL", FUR, seg=6, rings=4)
b.box(vf.p(0, 0.38, 0.36), (0.07, 0.03, 0.08), "PAL", WHITE, rot_z=vf.rot)
b.sphere(vf.p(0, 0.37, 0.45), 0.03, "PAL", DARK, seg=4, rings=3)
for s in (-1, 1):
    b.sphere(vf.p(s * 0.07, 0.33, 0.5), 0.02, "PAL", DARK, seg=4, rings=3)
    b.sphere(vf.p(s * 0.1, 0.22, 0.56), 0.04, "PAL", FUR_D, seg=4, rings=3)
b.box(vf.p(0, -0.36, 0.04), (0.26, 0.4, 0.06), "PAL", FUR_D, rot_z=vf.rot)

pine(b, f, 3.7, -0.2 - 3.4, h=4.4, r=1.0)
house = b.finish(recalc=False)
export_fbx([house], "Village_House_Lumbermill")
preview([house], "house_lumbermill", views=((200, 26), (150, 30)), res=(1100, 950), dist_mul=1.05)
