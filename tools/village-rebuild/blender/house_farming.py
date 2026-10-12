# Farmer's House: a red barn with big doors and a loft full of hay, a fenced field of wheat and cabbages, a
# haystack with a pitchfork, hay bales, and a pumpkin-headed scarecrow in a straw hat.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Farming")
f = Frame()
BARN, SOIL, WHEAT, CABBAGE, PUMPKIN = "714242", "4d3828", "bda248", "5c8f51", "e8862a"

def wheat_emblem(b, ef, z):
    emb_bar(b, ef, z, (0.0, -0.22), (0.0, 0.12), 0.03, TIMBER_L, d=0.02)
    for k in range(4):
        v = -0.04 + k * 0.07
        for s in (-1, 1):
            emb(b, ef, z, [(0.0, v), (s * 0.09, v + 0.05), (s * 0.04, v + 0.1)], WHEAT, d=0.025)
    emb(b, ef, z, [(-0.03, 0.12), (0.03, 0.12), (0.0, 0.22)], WHEAT, d=0.025)

yard(b, f, 4.6, 4.4, DIRT, seed=13.0)

# the barn: painted boards, white corners, a steep slat roof
hf = f.sub(1.6, -2.1, 0.0)
ridge = cottage(b, hf, 5.0, 4.0, 3.4, roof="SLAT_BROWN", pitch=48, overhang=0.45, plinth=0.3, shutters=None,
                front_windows=(), side_windows=(0.0,), back_windows=(), door_x=None, chimney=None, braces=False,
                wall="PAL", wall_col=BARN, post_col=WHITE, beams=False)
fw = hf.sub(0, 2.0, 0)
for k in range(9):
    x = -2.1 + k * 0.52
    b.box(fw.p(x, 0.02, 1.85), (0.04, 0.04, 3.0), "PAL", "5d252c", rot_z=fw.rot)
# big double doors with white braces, a hay loft door above
b.box(fw.p(0.5, 0.06, 1.25), (2.2, 0.08, 2.3), "PAL", BARN, rot_z=fw.rot)
for x0, x1 in ((-0.6, 0.5), (0.5, 1.6)):
    for a, c in (((x0, 0.15), (x1, 2.35)), ((x0, 2.35), (x1, 0.15))):
        bar(b, fw.p(a[0], 0.12, a[1]), fw.p(c[0], 0.12, c[1]), 0.12, 0.05, "PAL", WHITE, normal=fw.R @ Vector((0, 1, 0)))
    for z in (0.15, 2.35):
        bar(b, fw.p(x0, 0.12, z), fw.p(x1, 0.12, z), 0.14, 0.05, "PAL", WHITE)
    for x in (x0, x1):
        bar(b, fw.p(x, 0.12, 0.1), fw.p(x, 0.12, 2.4), 0.14, 0.05, "PAL", WHITE)
b.box(fw.p(0.5, 0.05, 3.75), (1.0, 0.08, 0.9), "PAL", DARK, rot_z=fw.rot)
for x in (0.0, 1.0):
    bar(b, fw.p(x, 0.1, 3.3), fw.p(x, 0.1, 4.2), 0.1, 0.05, "PAL", WHITE)
bar(b, fw.p(0.0, 0.1, 4.2), fw.p(1.0, 0.1, 4.2), 0.1, 0.05, "PAL", WHITE)
for k in range(6):
    b.prism(fw.p(0.12 + k * 0.15, 0.1, 3.32), 0.09, 0.32 + 0.1 * (k % 2), 4, "PAL", HAY, r_top=0.0, rot=k)
bar(b, fw.p(0.5, 0.0, ridge - 0.3), fw.p(0.5, 0.9, ridge - 0.3), 0.12, 0.12, "PAL", TIMBER)              # hay hoist
b.box(fw.p(0.5, 0.88, ridge - 0.65), (0.02, 0.02, 0.6), "PAL", "c6a177", rot_z=fw.rot)
b.lathe(fw.p(0.5, 0.88, ridge - 1.0), [(0.0, 0.0), (0.1, 0.03), (0.1, 0.08), (0.0, 0.1)], 6, "PAL", DARK)
sign(b, fw, -2.55, 2.6, 0.0, wheat_emblem)

# the field: soil rows, wheat and cabbages, a fence round it
fx0, fx1, fy0, fy1 = -4.1, -0.6, -0.4, 3.9
fence(b, f, [(fx0, fy0), (fx0, fy1), (fx1, fy1), (fx1, fy0 + 1.4)], h=0.85)
for k in range(5):
    x = fx0 + 0.45 + k * 0.62
    b.box(f.p(x, (fy0 + fy1) / 2, 0.1), (0.42, fy1 - fy0 - 0.5, 0.12), "PAL", SOIL, rot_z=f.rot)
    n = 7
    for j in range(n):
        y = fy0 + 0.45 + j * (fy1 - fy0 - 0.9) / (n - 1)
        if k < 3:
            for t in range(3):
                a = t * 2.1 + j
                px, py = x + math.cos(a) * 0.08, y + math.sin(a) * 0.08
                b.prism(f.p(px, py, 0.15), 0.025, 0.55, 4, "PAL", "a37f48")
                b.prism(f.p(px, py, 0.62), 0.05, 0.2, 4, "PAL", WHEAT, r_top=0.0, rot=a)
        else:
            b.sphere(f.p(x, y, 0.3), 0.19, "PAL", CABBAGE, seg=6, rings=4, sz=0.85)
            b.sphere(f.p(x, y, 0.36), 0.12, "PAL", "6b8f4f" if j % 2 else LEAF_D, seg=6, rings=3)
# the scarecrow, pumpkin head and straw hat
sc = f.sub(-2.35, 1.9, 0.2)
b.box(sc.p(0, 0, 1.0), (0.1, 0.1, 2.0), "PAL", TIMBER, rot_z=sc.rot)
bar(b, sc.p(-0.7, 0, 1.55), sc.p(0.7, 0, 1.55), 0.08, 0.08, "PAL", TIMBER)
b.quad_box([sc.p(-0.28, -0.14, 1.0), sc.p(0.28, -0.14, 1.0), sc.p(0.28, 0.14, 1.0), sc.p(-0.28, 0.14, 1.0)],
           [sc.p(-0.24, -0.13, 1.68), sc.p(0.24, -0.13, 1.68), sc.p(0.24, 0.13, 1.68), sc.p(-0.24, 0.13, 1.68)], "PAL", BLUE_C)
for s in (-1, 1):
    b.box(sc.p(s * 0.48, 0, 1.55), (0.42, 0.16, 0.18), "PAL", BLUE_C, rot_z=sc.rot)
    b.prism(sc.p(s * 0.72, 0, 1.47), 0.06, 0.2, 4, "PAL", HAY, r_top=0.0, rot=s)
b.sphere(sc.p(0, 0, 1.95), 0.24, "PAL", PUMPKIN, seg=8, rings=5, sz=0.85)
for s in (-1, 1):
    b.face([sc.p(s * 0.1 - 0.04, 0.215, 2.0), sc.p(s * 0.1 + 0.04, 0.215, 2.0), sc.p(s * 0.1, 0.215, 2.07)], sc.R @ Vector((0, 1, 0)), "PAL", DARK)
b.face([sc.p(-0.1, 0.21, 1.88), sc.p(0.1, 0.21, 1.88), sc.p(0.0, 0.21, 1.84)], sc.R @ Vector((0, 1, 0)), "PAL", DARK)
b.lathe(sc.p(0, 0, 2.12), [(0.42, 0.0), (0.42, 0.03), (0.2, 0.05), (0.18, 0.22), (0.0, 0.24)], 9, "PAL", HAY)
b.lathe(sc.p(0, 0, 2.12), [(0.0, -0.005), (0.42, -0.005)], 9, "PAL", HAY)

# the haystack with a pitchfork, bales, a cart wheel and a water trough
hs = f.sub(3.0, 1.8, 0.0)
b.lathe(hs.p(0, 0, 0), [(1.0, -0.1), (1.05, 0.5), (0.85, 1.2), (0.4, 1.75), (0.0, 1.9)], 10, "PAL", HAY)
bar(b, hs.p(-0.5, 0.75, 0.0), hs.p(-0.2, 0.55, 1.75), 0.05, 0.05, "PAL", TIMBER_L)
for k in (-1, 0, 1):
    bar(b, hs.p(-0.2 + k * 0.06, 0.55, 1.75), hs.p(-0.17 + k * 0.08, 0.5, 2.05), 0.025, 0.025, "PAL", "8b8b8b")
hay_bale(b, f, 1.2, 1.6, yaw=0.2)
hay_bale(b, f, 1.4, 2.3, yaw=-0.3)
hay_bale(b, f, 1.3, 1.95, z=0.6, yaw=0.0)
b.box(f.p(3.4, 3.6, 0.25), (1.4, 0.5, 0.5), "PAL", TIMBER_L, rot_z=f.rot)
b.face([f.p(2.75, 3.4, 0.51), f.p(4.05, 3.4, 0.51), f.p(4.05, 3.8, 0.51), f.p(2.75, 3.8, 0.51)], (0, 0, 1), "GLASS", "396b7c")
tree(b, f, -3.6, -2.8, h=4.0, r=1.3)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Farming")
preview([house], "house_farming", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
