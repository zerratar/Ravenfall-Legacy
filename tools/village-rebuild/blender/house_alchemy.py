# Alchemy (the Elixir Emporium): a steep roofed shop with a glowing green chimney, a round turret with a purple
# spire, a bubbling cauldron over a fire, a shelf of coloured bottles, herb planters and a flask sign.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Alchemy")
f = Frame()
GREEN, LIME, PURPLE, RED, BLUE = "5c8f51", "666f4a", "564888", "c04531", "396b7c"

def flask(b, ef, z):
    n = ef.R @ Vector((0, 1, 0))
    pts = [ef.p(math.cos(2 * math.pi * i / 10) * 0.15, 0.02, z - 0.07 + math.sin(2 * math.pi * i / 10) * 0.15) for i in range(10)]
    b.face(pts, n, "GLASS", GREEN)
    b.box(ef.p(0, 0.02, z + 0.14), (0.08, 0.02, 0.16), "GLASS", GREEN, rot_z=ef.rot)
    b.box(ef.p(0, 0.03, z + 0.23), (0.12, 0.02, 0.04), "PAL", TIMBER_L, rot_z=ef.rot)

def bottle(b, f, x, y, z, col, s=1.0):
    b.lathe(f.p(x, y, z), [(0.07 * s, 0), (0.09 * s, 0.1 * s), (0.07 * s, 0.2 * s), (0.03 * s, 0.24 * s), (0.03 * s, 0.32 * s), (0.0, 0.32 * s)], 8, "GLASS", col)
    b.prism(f.p(x, y, z + 0.31 * s), 0.035 * s, 0.05 * s, 6, "PAL", TIMBER_L)

yard(b, f, 4.6, 4.4, DIRT, seed=4.0)

# the shop, steep roofed, set back and to the right
sf = f.sub(0.6, -1.2, 0.0)
ridge = cottage(b, sf, 5.0, 3.8, 3.3, roof="SLAT_TEAL", pitch=50, overhang=0.45, plinth=0.45, shutters="green",
                front_windows=(1.4,), side_windows=(0.0,), back_windows=(-1.2, 1.2), door_x=-0.6, chimney=(1.7, -0.7))
# the chimney glows green at the top: something is always brewing
b.box(sf.p(1.7, -0.7, ridge + 1.2), (0.6, 0.6, 0.18), "GLASS", GREEN, rot_z=sf.rot)
for i, (dx, dy, dz, r) in enumerate([(0.1, 0.0, 1.6, 0.22), (-0.15, 0.1, 2.0, 0.17), (0.12, -0.05, 2.35, 0.12)]):
    b.sphere(sf.p(1.7 + dx, -0.7 + dy, ridge + dz), r, "GLASS", GREEN, seg=6, rings=4)
fw = sf.sub(0, 1.9, 0)
sign(b, fw, -2.05, 2.5, 0.0, flask)
# a shelf of bottles against the front wall
for z in (0.75, 1.35):
    b.box(fw.p(1.4, 0.32, z), (1.5, 0.4, 0.06), "PAL", TIMBER_L, rot_z=fw.rot)
for s in (-1, 1):
    b.box(fw.p(1.4 + s * 0.72, 0.32, 0.75), (0.06, 0.4, 1.5), "PAL", TIMBER, rot_z=fw.rot)
cols = [RED, BLUE, GREEN, PURPLE, "e4aa60", GREEN, RED, BLUE]
for i in range(8):
    z = 0.78 if i < 4 else 1.38
    bottle(b, fw, 0.85 + (i % 4) * 0.36, 0.34, z, cols[i], s=1.0 + 0.25 * ((i * 7) % 3 == 0))

# the turret at the front left: plaster, small lit windows, a purple spire with a gold star
tc = Vector(f.p(-3.0, 0.2, 0)); TR = 1.15; TH = 4.9
b.lathe(tuple(tc), [(TR + 0.25, -0.8), (TR + 0.25, 0.45), (TR, 0.45)], 12, "WALL", cyl_radius=TR)
b.lathe(tuple(tc), [(TR, 0.45), (TR, TH)], 12, "STUCCO", cyl_radius=TR, tile=(4.0, 4.0))
b.lathe(tuple(tc), [(TR, TH), (TR + 0.18, TH + 0.12), (TR + 0.18, TH + 0.3), (TR, TH + 0.3)], 12, "PAL", TIMBER)
b.lathe(tuple(tc), [(TR + 0.32, TH + 0.3), (TR * 0.55, TH + 1.4), (0.0, TH + 3.1)], 12, "PAL", PURPLE)
b.lathe(tuple(tc), [(TR, TH + 0.3), (TR + 0.32, TH + 0.3)], 12, "PAL", TIMBER)     # eave underside
b.prism((tc.x, tc.y, TH + 3.05), 0.04, 0.5, 6, "PAL", GOLD)
for k in range(5):
    a = 2 * math.pi * k / 5 + math.pi / 2
    bar(b, (tc.x, tc.y, TH + 3.62), (tc.x + math.cos(a) * 0.22, tc.y, TH + 3.62 + math.sin(a) * 0.22), 0.07, 0.04, "PAL", GOLD, normal=(0, 1, 0))
ap = TR * math.cos(math.pi / 12)
for k, z in ((4, 1.6), (2, 3.3), (5, 3.3), (0, 2.4)):
    a = (k + 0.5) * 2 * math.pi / 12
    wf = Frame(tc.x + math.cos(a) * ap, tc.y + math.sin(a) * ap, a - math.pi / 2)
    window(b, wf, 0.0, z, 0.0, w=0.45, h=0.65, shutters=None)
# timber bands round the turret at each floor
for z in (0.55, 2.5):
    b.lathe(tuple(tc), [(TR, z - 0.08), (TR + 0.06, z - 0.08), (TR + 0.06, z + 0.08), (TR, z + 0.08)], 12, "PAL", TIMBER)

# the cauldron over a fire, bubbling green
cc = f.sub(2.1, 2.3, 0.0)
for k in range(3):
    a = 2 * math.pi * k / 3 + 0.4
    bar(b, cc.p(math.cos(a) * 0.75, math.sin(a) * 0.75, 0.0), cc.p(math.cos(a) * 0.35, math.sin(a) * 0.35, 0.55), 0.07, 0.07, "PAL", DARK)
for k in range(4):
    a = math.pi / 4 * k * 2 + 0.3
    bar(b, cc.p(math.cos(a) * 0.55, math.sin(a) * 0.55, 0.08), cc.p(-math.cos(a) * 0.55, -math.sin(a) * 0.55, 0.08), 0.13, 0.13, "PAL", TIMBER_L)
b.sphere(cc.p(0, 0, 0.1), 0.3, "GLASS", "e4aa60", seg=6, rings=3, sz=0.5)
b.lathe(cc.p(0, 0, 0.0), [(0.0, 0.32), (0.35, 0.36), (0.62, 0.62), (0.66, 0.9), (0.6, 1.08), (0.66, 1.14), (0.56, 1.14)], 12, "PAL", DARK)
b.lathe(cc.p(0, 0, 0.0), [(0.56, 1.14), (0.56, 1.0)], 12, "PAL", DARK)                    # inner lip, faces in
b.lathe(cc.p(0, 0, 0.0), [(0.56, 1.02), (0.0, 1.02)], 12, "GLASS", GREEN)                 # the brew
for (dx, dy, r) in ((0.15, 0.1, 0.12), (-0.2, -0.05, 0.09), (0.0, -0.25, 0.07), (-0.1, 0.25, 0.06)):
    b.sphere(cc.p(dx, dy, 1.06), r, "GLASS", GREEN, seg=6, rings=3)
b.sphere(cc.p(0.05, 0.0, 1.55), 0.08, "GLASS", GREEN, seg=6, rings=3)

# herb planters and a crate of supplies
for x, y in ((-1.4, 2.6), (0.2, 3.3)):
    b.box(f.p(x, y, 0.25), (1.2, 0.5, 0.5), "PAL", TIMBER_L, rot_z=f.rot)
    for k in range(4):
        b.prism(f.p(x - 0.45 + k * 0.3, y, 0.48), 0.12, 0.35 + 0.1 * (k % 2), 5, "PAL", GREEN if k % 2 else LIME, r_top=0.0, rot=k)
crate(b, f, 3.5, 0.4, s=0.6, yaw=0.2)
for i, col in enumerate((PURPLE, BLUE)):
    bottle(b, f, 3.35 + i * 0.3, 0.4, 0.6, col)
barrel(b, f, 3.6, -0.6)
# a few purple crystals by the turret
for i, (dx, dy, h, t) in enumerate(((-1.6, 1.5, 0.7, 0.2), (-1.9, 1.75, 0.5, -0.3), (-1.35, 1.8, 0.4, 0.5))):
    b.prism(f.p(-3.0 + dx + 1.6, 0.2 + dy, 0.0), 0.12, h, 5, "GLASS", PURPLE, r_top=0.0, rot=t)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Alchemy")
preview([house], "house_alchemy", views=((200, 26), (150, 30)), res=(1100, 950), dist_mul=1.05)
