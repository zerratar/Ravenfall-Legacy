# Slayers Guild: a dark lodge with a horned monster skull over the door, a spiked palisade, a wheeled cage with
# something in it (glowing eyes), a weapon rack and torches. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Slayer")
f = Frame()
BONE, BLOOD = "d6ceca", "714242"

def skull_emblem(b, ef, z):
    emb_bar(b, ef, z, (-0.26, -0.2), (0.26, 0.2), 0.05, "b9b9b9", d=0.015)
    emb_bar(b, ef, z, (-0.26, 0.2), (0.26, -0.2), 0.05, "b9b9b9", d=0.015)
    emb_circle(b, ef, z, 0, 0.03, 0.14, BONE, d=0.025)
    emb(b, ef, z, [(-0.08, -0.08), (0.08, -0.08), (0.06, -0.17), (-0.06, -0.17)], BONE, d=0.025)
    emb_circle(b, ef, z, -0.055, 0.04, 0.035, DARK, d=0.03, n=6)
    emb_circle(b, ef, z, 0.055, 0.04, 0.035, DARK, d=0.03, n=6)

def torch(b, f, x, y, h=1.9):
    b.box(f.p(x, y, h / 2), (0.12, 0.12, h), "PAL", TIMBER, rot_z=f.rot)
    b.lathe(f.p(x, y, h), [(0.06, 0.0), (0.16, 0.18), (0.16, 0.24), (0.0, 0.24)], 6, "PAL", DARK)
    b.prism(f.p(x, y, h + 0.22), 0.13, 0.42, 5, "GLASS", WARM, r_top=0.0)
    b.prism(f.p(x, y, h + 0.22), 0.08, 0.28, 5, "GLASS", "f27250", r_top=0.0, rot=0.6)

yard(b, f, 4.6, 4.4, "6e6557", seed=7.0)

# the lodge
lf = f.sub(0.5, -1.3, 0.0)
cottage(b, lf, 5.6, 4.2, 3.4, roof="SLAT_BLUE", pitch=46, overhang=0.5, plinth=0.5, shutters="red",
        front_windows=(-1.8, 1.8), side_windows=(0.0,), back_windows=(0.0,), door_x=0.0, chimney=(-1.9, -0.8))
fw = lf.sub(0, 2.1, 0)
sign(b, fw, 2.75, 2.6, 0.0, skull_emblem)
# the trophy over the door: a big horned skull on a shield board
b.box(fw.p(0, 0.06, 2.85), (1.3, 0.1, 0.9), "PAL", TIMBER, rot_z=fw.rot)
b.sphere(fw.p(0, 0.42, 2.98), 0.44, "PAL", BONE, seg=8, rings=5, sz=0.85)
b.box(fw.p(0, 0.68, 2.66), (0.5, 0.46, 0.26), "PAL", BONE, rot_z=fw.rot)                       # snout
for s in (-1, 1):
    b.sphere(fw.p(s * 0.18, 0.78, 3.02), 0.1, "PAL", DARK, seg=6, rings=3)                     # eye sockets
    b.sphere(fw.p(s * 0.18, 0.84, 3.02), 0.04, "GLASS", "c04531", seg=4, rings=3)               # a faint red glint
    # a horn growing out of the skull: one tapering curve, out, up and back in
    pts = [fw.p(s * 0.2, 0.42, 3.2), fw.p(s * 0.5, 0.48, 3.3), fw.p(s * 0.78, 0.52, 3.5), fw.p(s * 0.88, 0.56, 3.8), fw.p(s * 0.8, 0.58, 4.05)]
    for k in range(4):
        log(b, pts[k], pts[k + 1], 0.15 - k * 0.032, bark="c6a177", end="c6a177", n=6)
        b.sphere(pts[k + 1], 0.15 - (k + 1) * 0.032, "PAL", "c6a177", seg=6, rings=3)
for k in range(4):
    b.prism(fw.p(-0.12 + k * 0.08, 0.9, 2.55), 0.03, 0.09, 4, "PAL", WHITE, r_top=0.0, rot=math.pi)   # teeth, pointing down
    b.box(fw.p(-0.12 + k * 0.08, 0.9, 2.58), (0.04, 0.04, 0.05), "PAL", WHITE, rot_z=fw.rot)

# a spiked palisade down the left side and along the back
stakes = [(-4.2, -3.9 + i * 0.36) for i in range(17)] + [(-4.2 + (i + 1) * 0.36, -3.9) for i in range(5)]
for i, (x, y) in enumerate(stakes):
    h = 1.9 + 0.25 * math.sin(i * 1.7)
    b.prism(f.p(x, y, 0), 0.17, h, 6, "PAL", TIMBER if i % 2 else TIMBER_L, rot=i)
    b.prism(f.p(x, y, h), 0.17, 0.4, 6, "PAL", "c6a177", r_top=0.0, rot=i)
bar(b, f.p(-4.05, -3.9, 1.2), f.p(-4.05, 1.9, 1.2), 0.12, 0.08, "PAL", TIMBER)

# the cage on wheels: bars, a roof, and two glowing eyes in the dark inside
cf = f.sub(-2.4, 2.2, 0.25)
b.box(cf.p(0, 0, 0.45), (1.6, 1.1, 0.2), "PAL", TIMBER, rot_z=cf.rot)
b.box(cf.p(0, 0, 1.15), (1.4, 0.9, 1.2), "PAL", DARK, rot_z=cf.rot)
b.box(cf.p(0, 0, 1.85), (1.7, 1.2, 0.18), "PAL", TIMBER, rot_z=cf.rot)
for k in range(8):
    x = -0.75 + k * (1.5 / 7)
    for s in (-1, 1):
        b.box(cf.p(x, s * 0.52, 1.15), (0.05, 0.05, 1.3), "PAL", "8b8b8b", rot_z=cf.rot)
for k in range(5):
    y = -0.52 + k * (1.04 / 4)
    for s in (-1, 1):
        b.box(cf.p(s * 0.77, y, 1.15), (0.05, 0.05, 1.3), "PAL", "8b8b8b", rot_z=cf.rot)
for s in (-1, 1):
    b.box(cf.p(s * 0.18, 0.46, 1.32), (0.12, 0.02, 0.07), "GLASS", "e8a33d", rot_z=cf.rot)
    for sy in (-1, 1):
        c = Vector(cf.p(s * 0.6, sy * 0.58, 0.3)); ax = cf.R @ Vector((0, sy, 0)); fwd = cf.R @ Vector((1, 0, 0)); up = Vector((0, 0, 1))
        r0 = [c + (fwd * math.cos(2 * math.pi * i / 8) + up * math.sin(2 * math.pi * i / 8)) * 0.28 for i in range(8)]
        r1 = [v + ax * 0.1 for v in r0]
        b.face([tuple(v) for v in r1], ax, "PAL", TIMBER_L); b.face([tuple(v) for v in r0], -ax, "PAL", TIMBER_L)
        for i in range(8):
            j = (i + 1) % 8; mid = (r0[i] + r0[j]) / 2 - c
            b.face([tuple(r0[i]), tuple(r0[j]), tuple(r1[j]), tuple(r1[i])], mid, "PAL", TIMBER)
b.box(cf.p(0.0, 0.0, 2.05), (0.45, 0.3, 0.2), "PAL", BLOOD, rot_z=cf.rot)          # a warning plank on the roof

# weapon rack: a frame with two axes and a spear
rf = f.sub(3.4, 1.4, -math.pi / 2)
for s in (-1, 1):
    b.box(rf.p(s * 0.7, 0, 0.7), (0.12, 0.12, 1.4), "PAL", TIMBER, rot_z=rf.rot)
for z in (0.35, 1.3):
    b.box(rf.p(0, 0, z), (1.5, 0.12, 0.1), "PAL", TIMBER, rot_z=rf.rot)
for k, x in enumerate((-0.4, 0.0, 0.4)):
    bar(b, rf.p(x, 0.1, 0.2), rf.p(x, 0.1, 1.55 + (0.35 if k == 1 else 0)), 0.06, 0.06, "PAL", TIMBER_L)
    if k == 1:
        b.prism(rf.p(x, 0.1, 1.9), 0.07, 0.3, 4, "PAL", "b9b9b9", r_top=0.0)
    else:
        b.quad_box([rf.p(x, 0.07, 1.25), rf.p(x + 0.3, 0.07, 1.15), rf.p(x + 0.3, 0.13, 1.15), rf.p(x, 0.13, 1.25)],
                   [rf.p(x, 0.07, 1.5), rf.p(x + 0.3, 0.07, 1.6), rf.p(x + 0.3, 0.13, 1.6), rf.p(x, 0.13, 1.5)], "PAL", "b9b9b9")

torch(b, f, -0.9, 1.5)
torch(b, f, 1.9, 1.5)
crate(b, f, 3.2, -0.2, s=0.6)
barrel(b, f, 3.6, 3.0)
pine(b, f, -3.4, -2.6, h=4.2, r=1.0)
rock(b, f, 2.7, 3.4, 0.5, col=ROCK_D, seg=6, seed=1.0)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Slayer")
preview([house], "house_slayer", views=((200, 26), (150, 30)), res=(1100, 950), dist_mul=1.05)
