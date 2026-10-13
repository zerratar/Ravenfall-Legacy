# Forager's Hut (gathering): a round thatched hut, a rack of drying herbs, baskets of berries and mushrooms,
# berry bushes, a giant spotted mushroom, and a garden gnome who thinks nobody has noticed him.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Gathering")
f = Frame()
CAP, SPOT, STEM, SKIN, BERRY_B = "c04531", "f5f5f5", "e6e6e6", "d9af87", "564888"

def mushroom_emblem(b, ef, z):
    emb(b, ef, z, [(-0.04, -0.2), (0.04, -0.2), (0.05, 0.02), (-0.05, 0.02)], STEM, d=0.02)
    emb(b, ef, z, [(math.cos(math.pi * k / 8) * 0.2, 0.0 + math.sin(math.pi * k / 8) * 0.17) for k in range(9)], CAP, d=0.025)
    for (u, v) in ((-0.08, 0.06), (0.07, 0.1), (0.0, 0.03)):
        emb_circle(b, ef, z, u, v, 0.025, SPOT, d=0.03, n=6)

def mushroom(b, f, x, y, h, r, cap=CAP):
    c = f.p(x, y, 0)
    b.lathe(c, [(r * 0.28, 0.0), (r * 0.22, h * 0.6), (r * 0.25, h * 0.85)], 8, "PAL", STEM)
    b.lathe(c, [(r * 0.25, h * 0.82), (r, h * 0.78)], 9, "PAL", "d6ceca")                     # gills, face down
    b.lathe(c, [(r, h * 0.78), (r * 0.95, h * 0.92), (r * 0.6, h * 1.08), (0.0, h * 1.14)], 9, "PAL", cap)
    for k in range(6 if r > 0.4 else 3):
        a = k * 2.1; t = 0.55 + 0.3 * (k % 2)
        b.sphere((c[0] + math.cos(a) * r * t, c[1] + math.sin(a) * r * t, h * (1.1 - 0.25 * t)), r * 0.12, "PAL", SPOT, seg=5, rings=3, sz=0.5)

yard(b, f, 4.6, 4.4, DIRT, seed=9.0)

# the hut, its door facing the front
round_hut(b, f, -0.9, -1.0, r=2.1, h=2.3, roof="SLAT_BROWN", door_angle=math.pi / 2)
signpost(b, f, 1.9, 1.5, mushroom_emblem, yaw=-0.5, h=2.0)

# drying rack with bundles of herbs
rf = f.sub(2.6, -1.9, -0.3)
for s in (-1, 1):
    b.box(rf.p(s * 1.0, 0, 0.9), (0.12, 0.12, 1.8), "PAL", TIMBER, rot_z=rf.rot)
bar(b, rf.p(-1.15, 0, 1.75), rf.p(1.15, 0, 1.75), 0.08, 0.08, "PAL", TIMBER_L)
for k in range(5):
    x = -0.75 + k * 0.38
    b.box(rf.p(x, 0, 1.62), (0.02, 0.02, 0.22), "PAL", TIMBER, rot_z=rf.rot)
    b.prism(rf.p(x, 0, 1.0), 0.13, 0.52, 5, "PAL", LEAF if k % 2 else "666f4a", r_top=0.04, rot=k)

# baskets of the day's finds
basket(b, f, -2.0, 1.8, fill=[RED, BERRY_B, RED])
basket(b, f, -1.3, 2.4, fill=["c6a177", STEM, "c6a177"])
basket(b, f, 2.2, 0.4, r=0.42, fill=[LEAF, "666f4a"])
sack(b, f, -2.6, 2.5)
# berry bushes, flowers and little mushrooms
bush(b, f, -3.9, -1.3, r=0.6, berries=RED)
bush(b, f, -3.2, 2.9, r=0.5, berries=BERRY_B)
bush(b, f, 3.7, 2.0, r=0.55, berries=RED)
for k, (x, y, col) in enumerate(((0.0, 3.3, "e4aa60"), (0.5, 3.6, "564888"), (-0.5, 3.6, RED), (2.6, 3.4, "e4aa60"), (-3.8, -2.0, "564888"))):
    b.prism(f.p(x, y, 0.0), 0.03, 0.3, 4, "PAL", LEAF)
    b.sphere(f.p(x, y, 0.33), 0.09, "PAL", col, seg=5, rings=3)
for (x, y, h, r) in ((2.0, 2.6, 0.35, 0.18), (2.3, 2.85, 0.25, 0.13), (3.4, -3.3, 0.3, 0.15)):
    mushroom(b, f, x, y, h, r)
# the giant mushroom
mushroom(b, f, -3.1, 0.4, 1.9, 1.05)
pine(b, f, -3.6, -3.6, h=4.6, r=1.1)

# the gnome, half hidden behind the blue berry bush
gf = f.sub(-2.75, 3.25, -0.5)
b.lathe(gf.p(0, 0, 0), [(0.0, 0.0), (0.16, 0.02), (0.13, 0.32), (0.0, 0.36)], 7, "PAL", BLUE_C)
b.sphere(gf.p(0, 0, 0.42), 0.09, "PAL", SKIN, seg=6, rings=4)
b.lathe(gf.p(0, 0.05, 0.42), [(0.0, -0.2), (0.1, 0.0), (0.0, 0.02)], 6, "PAL", WHITE)        # beard
b.lathe(gf.p(0, 0, 0.47), [(0.11, 0.0), (0.0, 0.3)], 7, "PAL", RED)                         # hat
b.sphere(gf.p(0, 0.09, 0.44), 0.025, "PAL", "c04531", seg=4, rings=3)                        # nose

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Gathering")
preview([house], "house_gathering", views=((200, 26), (150, 30)), res=(1100, 950), dist_mul=1.05)
