# Farmer's House: a red barn with big doors and a loft full of hay, a fenced field of wheat and cabbages, a
# haystack with a pitchfork, hay bales, and a pumpkin-headed scarecrow in a straw hat.
# The fence, cabbages, bales and tree are Synty props; the barn, the wheat, the haystack, the pitchfork, the trough
# and the scarecrow are ours. About 9 x 9 m, facing +y (Unity -z).
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
# big double doors with white braces
b.box(fw.p(0.5, 0.06, 1.25), (2.2, 0.08, 2.3), "PAL", BARN, rot_z=fw.rot)
for x0, x1 in ((-0.6, 0.5), (0.5, 1.6)):
    for a, c in (((x0, 0.15), (x1, 2.35)), ((x0, 2.35), (x1, 0.15))):
        bar(b, fw.p(a[0], 0.12, a[1]), fw.p(c[0], 0.12, c[1]), 0.12, 0.05, "PAL", WHITE, normal=fw.R @ Vector((0, 1, 0)))
    for z in (0.15, 2.35):
        bar(b, fw.p(x0, 0.12, z), fw.p(x1, 0.12, z), 0.14, 0.05, "PAL", WHITE)
    for x in (x0, x1):
        bar(b, fw.p(x, 0.12, 0.1), fw.p(x, 0.12, 2.4), 0.14, 0.05, "PAL", WHITE)
# the hay loft door high on the gable end, hay spilling out, and a hoist beam under the ridge
lw = hf.sub(-2.5, 0, math.pi / 2)
b.box(lw.p(0, 0.12, 4.0), (1.0, 0.08, 0.9), "PAL", DARK, rot_z=lw.rot)
for x in (-0.5, 0.5):
    bar(b, lw.p(x, 0.17, 3.55), lw.p(x, 0.17, 4.45), 0.1, 0.05, "PAL", WHITE)
for z in (3.55, 4.45):
    bar(b, lw.p(-0.55, 0.17, z), lw.p(0.55, 0.17, z), 0.1, 0.05, "PAL", WHITE)
for k in range(6):
    b.prism(lw.p(-0.38 + k * 0.15, 0.18, 3.58), 0.09, 0.32 + 0.1 * (k % 2), 4, "PAL", HAY, r_top=0.0, rot=k)
bar(b, lw.p(0, -0.3, ridge - 0.35), lw.p(0, 1.0, ridge - 0.35), 0.14, 0.14, "PAL", TIMBER)
b.box(lw.p(0, 0.95, ridge - 0.75), (0.02, 0.02, 0.7), "PAL", "c6a177", rot_z=lw.rot)
b.lathe(lw.p(0, 0.95, ridge - 1.15), [(0.0, 0.0), (0.1, 0.03), (0.1, 0.08), (0.0, 0.1)], 6, "PAL", DARK)
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
            anchor(f, "cabbage", x, y, 0.15, yaw=j * 1.7)
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

# the haystack with a pitchfork, bales, a cart wheel and a water trough. The barn doors span x 1.0 to 3.2, so the
# haystack sits in the corner to their right and the bales stand lengthwise between them and the field fence.
hs = f.sub(3.9, 1.5, 0.0)
# our haystack: the Synty hay pile reads as a dark mound at this size
b.lathe(hs.p(0, 0, 0), [(0.75, -0.1), (0.79, 0.45), (0.64, 1.05), (0.3, 1.5), (0.0, 1.65)], 10, "PAL", HAY)
# the pitchfork stuck in the side of it, leaning out
bar(b, hs.p(-0.75, 0.75, 0.0), hs.p(-0.5, 0.55, 1.5), 0.05, 0.05, "PAL", TIMBER_L)
for k in (-1, 0, 1):
    bar(b, hs.p(-0.5 + k * 0.06, 0.55, 1.5), hs.p(-0.47 + k * 0.08, 0.5, 1.8), 0.025, 0.025, "PAL", "8b8b8b")
hay_bale(b, f, -0.05, 2.0, yaw=1.62)
hay_bale(b, f, 0.52, 2.1, yaw=1.5)
hay_bale(b, f, 0.25, 2.05, z=0.6, yaw=1.57)
b.box(f.p(3.4, 3.6, 0.25), (1.4, 0.5, 0.5), "PAL", TIMBER_L, rot_z=f.rot)
b.face([f.p(2.75, 3.4, 0.51), f.p(4.05, 3.4, 0.51), f.p(4.05, 3.8, 0.51), f.p(2.75, 3.8, 0.51)], (0, 0, 1), "GLASS", "396b7c")
tree(b, f, -3.6, -2.8, h=4.0, r=1.3)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Farming")
preview([house], "house_farming", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
