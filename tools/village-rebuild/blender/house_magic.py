# Magic House: a stone wizard's tower with a tall blue spire, a moon and a star, a small annex, a crystal hovering
# over a plinth with runestones round it, a lectern with a spellbook, a broom by the door and a black cat.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Magic")
f = Frame()
ARCANE, ARCANE_L, PAGE, CAT = "564888", "5f8191", "f5f5f5", "2a2241"

def star_emblem(b, ef, z):
    pts = []
    for k in range(10):
        a = math.pi / 2 + math.pi * k / 5; r = 0.2 if k % 2 == 0 else 0.08
        pts.append((math.cos(a) * r, math.sin(a) * r))
    emb(b, ef, z, pts, GOLD, d=0.025)

# the tower: a plinth, stone body, timber bands, lit windows, then a wide brimmed spire
tc = Vector(f.p(-1.2, -1.4, 0)); TR = 1.8; TH = 7.2; N = 12
b.lathe(tuple(tc), [(TR + 0.3, -0.8), (TR + 0.3, 0.6), (TR, 0.9), (TR, TH)], N, "WALL", cyl_radius=TR)
for z in (2.6, 5.0):
    b.lathe(tuple(tc), [(TR, z - 0.01), (TR + 0.12, z), (TR + 0.12, z + 0.22), (TR, z + 0.23)], N, "PAL", STONE)
b.lathe(tuple(tc), [(TR, TH), (TR + 0.2, TH), (TR + 0.2, TH + 0.25), (TR, TH + 0.25)], N, "PAL", TIMBER)
b.lathe(tuple(tc), [(TR, TH + 0.25), (TR + 0.75, TH + 0.25)], N, "PAL", TIMBER)                          # brim underside
b.lathe(tuple(tc), [(TR + 0.75, TH + 0.25), (TR * 0.7, TH + 1.1), (TR * 0.32, TH + 3.2), (0.0, TH + 5.0)], N, "ROOF", cyl_radius=TR)
b.prism((tc.x, tc.y, TH + 4.9), 0.05, 0.7, 6, "PAL", GOLD)
# a crescent moon on the tip and a star over it
mc = Vector((tc.x, tc.y, TH + 5.75)); nY = Vector((0, 1, 0))
for s in (1, -1):
    pts = []
    for k in range(9):
        a = math.pi * 0.25 + math.pi * 1.5 * k / 8
        pts.append(tuple(mc + Vector((math.cos(a) * 0.32, s * 0.03, math.sin(a) * 0.32))))
    for k in range(8, -1, -1):
        a = math.pi * 0.25 + math.pi * 1.5 * k / 8
        pts.append(tuple(mc + Vector((math.cos(a) * 0.2 + 0.1, s * 0.03, math.sin(a) * 0.22))))
    for i in range(8):
        q = [pts[i], pts[i + 1], pts[17 - i - 1], pts[17 - i]]
        b.face(q, nY * s, "PAL", GOLD)
ap = TR * math.cos(math.pi / N)
for k, z in ((3, 1.6), (4, 3.5), (1, 3.5), (2, 6.0), (5, 6.0), (9, 4.2), (7, 1.7)):
    a = (k + 0.5) * 2 * math.pi / N
    wf = Frame(tc.x + math.cos(a) * ap, tc.y + math.sin(a) * ap, a - math.pi / 2)
    window(b, wf, 0.0, z, 0.0, w=0.5, h=0.8, shutters=None)
# the tower door faces the front
a = (2 + 0.5) * 2 * math.pi / N
df = Frame(tc.x + math.cos(a) * ap, tc.y + math.sin(a) * ap, a - math.pi / 2)
door(b, df, 0.0, 0.0, w=0.95, h=2.0, col=ARCANE)
sign(b, df, 0.85, 2.4, 0.0, star_emblem)

# the annex
af = f.sub(2.3, -2.0, 0.0)
cottage(b, af, 3.6, 3.0, 2.8, roof="SLAT_BLUE", pitch=42, overhang=0.4, plinth=0.4, shutters=None,
        front_windows=(-0.8, 0.8), side_windows=(0.0,), back_windows=(), door_x=None, chimney=(0.9, -0.5))

# the crystal: a stone plinth, a hovering crystal and three runestones round it
pc = f.sub(1.6, 1.9, 0.0)
b.lathe(pc.p(0, 0, 0), [(0.55, -0.2), (0.55, 0.2), (0.35, 0.3), (0.3, 0.9), (0.45, 1.0), (0.45, 1.12), (0.0, 1.12)], 8, "WALL", cyl_radius=0.5)
b.lathe(pc.p(0, 0, 1.55), [(0.0, -0.32), (0.26, 0.0), (0.0, 0.62)], 6, "GLASS", ARCANE)
b.lathe(pc.p(0, 0, 1.55), [(0.0, -0.18), (0.13, 0.0), (0.0, 0.36)], 6, "GLASS", ARCANE_L, rot=0.5)
for k in range(3):
    a = 2 * math.pi * k / 3 + 0.3
    rf = pc.sub(math.cos(a) * 1.05, math.sin(a) * 1.05, a)
    b.box(rf.p(0, 0, 0.45 + 0.15 * k), (0.32, 0.18, 0.5), "PAL", STONE, rot_z=rf.rot)
    b.box(rf.p(0, 0.1, 0.48 + 0.15 * k), (0.12, 0.02, 0.26), "GLASS", ARCANE_L, rot_z=rf.rot)
# lectern with the spellbook open on it
lf = f.sub(-1.2, 2.2, 0.2)
b.box(lf.p(0, 0, 0.5), (0.16, 0.16, 1.0), "PAL", TIMBER, rot_z=lf.rot)
b.box(lf.p(0, 0, 0.04), (0.6, 0.6, 0.08), "PAL", TIMBER, rot_z=lf.rot)
bk = [Vector(lf.p(-0.36, -0.2, 1.0)), Vector(lf.p(0.36, -0.2, 1.0)), Vector(lf.p(0.36, 0.2, 1.18)), Vector(lf.p(-0.36, 0.2, 1.18))]
if (bk[1] - bk[0]).cross(bk[2] - bk[1]).z < 0: bk = bk[::-1]
fs = b.quad_box(bk, [v + Vector((0, 0, 0.06)) for v in bk], "PAL", ARCANE)
pg = [Vector(lf.p(-0.32, -0.17, 1.065)), Vector(lf.p(0.32, -0.17, 1.065)), Vector(lf.p(0.32, 0.17, 1.245)), Vector(lf.p(-0.32, 0.17, 1.245))]
b.face([tuple(v) for v in pg], (0, -0.4, 1), "PAL", PAGE)
# a broom by the tower door and the black cat on its step
bw = df.sub(-0.75, 0.25, 0.0)
bar(b, bw.p(0, 0, 0.35), bw.p(0.15, 0.05, 1.8), 0.05, 0.05, "PAL", TIMBER_L)
b.lathe(bw.p(-0.02, 0, 0.0), [(0.0, 0.0), (0.2, 0.05), (0.08, 0.42), (0.0, 0.45)], 7, "PAL", HAY)
cf = df.sub(0.25, 0.75, 0.3)
b.sphere(cf.p(0, 0, 0.2), 0.17, "PAL", CAT, seg=6, rings=4, sz=1.1)
b.sphere(cf.p(0, 0.04, 0.45), 0.12, "PAL", CAT, seg=6, rings=4)
for s in (-1, 1):
    b.prism(cf.p(s * 0.07, 0.04, 0.53), 0.04, 0.09, 3, "PAL", CAT, r_top=0.0, rot=cf.rot)
    b.sphere(cf.p(s * 0.045, 0.14, 0.47), 0.022, "GLASS", "e4aa60", seg=4, rings=3)
bar(b, cf.p(0, -0.15, 0.05), cf.p(0.28, -0.2, 0.06), 0.05, 0.05, "PAL", CAT)
bar(b, cf.p(0.28, -0.2, 0.06), cf.p(0.32, -0.05, 0.3), 0.05, 0.05, "PAL", CAT)

yard(b, f, 4.6, 4.4, DIRT, seed=6.0)
# a ring of little glowing mushrooms and a few stones
for k in range(7):
    a = 2 * math.pi * k / 7
    x, y = 3.3 + math.cos(a) * 0.55, 3.2 + math.sin(a) * 0.55
    b.prism(f.p(x, y, 0.0), 0.03, 0.12, 5, "PAL", "e6e6e6")
    b.lathe(f.p(x, y, 0.12), [(0.08, 0.0), (0.0, 0.07)], 6, "GLASS", ARCANE_L)
pine(b, f, -3.7, 2.0, h=4.0, r=0.95)
rock(b, f, -3.6, 3.4, 0.45, col=ROCK, seg=6, seed=2.0)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Magic")
preview([house], "house_magic", views=((200, 22), (150, 30)), res=(1100, 1050), dist_mul=1.1)
