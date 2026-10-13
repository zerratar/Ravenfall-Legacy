# Sailor's log: a boathouse with a sailboat up on a cradle, an anchor, rope coils, oars, a lifebuoy and a ship's
# wheel on the walls, and a gull on the ridge. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Sailing")
f = Frame()
HULL, SAIL = "396b7c", "f5f5f5"

def anchor_emblem(b, ef, z):
    emb_bar(b, ef, z, (0, -0.2), (0, 0.17), 0.05, DARK, d=0.02)
    emb_bar(b, ef, z, (-0.13, 0.1), (0.13, 0.1), 0.045, DARK, d=0.02)
    emb_bar(b, ef, z, (0, -0.2), (-0.19, -0.06), 0.045, DARK, d=0.02)
    emb_bar(b, ef, z, (0, -0.2), (0.19, -0.06), 0.045, DARK, d=0.02)
    emb_circle(b, ef, z, 0, 0.21, 0.045, DARK, d=0.02, n=8)

yard(b, f, 4.6, 4.4, DIRT, seed=5.0)
# a plank deck under the boat
for k in range(9):
    b.box(f.p(-1.4, -0.4 + k * 0.42, 0.06), (5.6, 0.38, 0.1), "PAL", TIMBER_L if k % 2 else "8e5a30", rot_z=f.rot)

# the boathouse
hf = f.sub(1.5, -2.2, 0.0)
ridge = cottage(b, hf, 5.0, 3.6, 3.2, roof="ROOF", pitch=40, overhang=0.45, plinth=0.45, shutters="blue",
        front_windows=(-1.5,), side_windows=(0.0,), back_windows=(0.0,), door_x=0.9, chimney=None)
fw = hf.sub(0, 1.8, 0)
sign(b, fw, 2.45, 2.6, 0.0, anchor_emblem)
torus(b, fw.p(-0.2, 0.12, 2.0), fw.R @ Vector((0, 1, 0)), 0.3, 0.08, None, seg=12, tube=6, cols=[RED, RED, RED, WHITE, WHITE, WHITE])
# a ship's wheel on the east gable
gw = hf.sub(-2.5, 0, math.pi / 2)
c = Vector(gw.p(0, 0.12, 3.75)); n = gw.R @ Vector((0, 1, 0))
torus(b, c, n, 0.42, 0.05, TIMBER_L, seg=12, tube=5)
side = gw.R @ Vector((1, 0, 0))
for k in range(8):
    a = math.pi * k / 4; d = side * math.cos(a) + Vector((0, 0, math.sin(a)))
    bar(b, tuple(c), tuple(c + d * 0.62), 0.05, 0.05, "PAL", TIMBER_L, normal=n)
b.sphere(tuple(c + n * 0.04), 0.09, "PAL", GOLD, seg=6, rings=3)
# a gull sitting on the ridge (it has been there for years)
gl = hf.sub(-1.2, 0.0, 0.4)
GZ = ridge + 0.36
b.sphere(gl.p(0, 0, GZ), 0.16, "PAL", WHITE, seg=6, rings=4, sz=0.8)
b.sphere(gl.p(0, 0.15, GZ + 0.1), 0.09, "PAL", WHITE, seg=6, rings=3)
b.prism(gl.p(0, 0.24, GZ + 0.08), 0.03, 0.12, 4, "PAL", "e4aa60", r_top=0.0)
for s in (-1, 1): b.box(gl.p(s * 0.13, -0.03, GZ + 0.02), (0.04, 0.26, 0.08), "PAL", "8b8b8b", rot_z=gl.rot)

# the boat on its cradle, mast up, sail furled
bf = f.sub(-1.5, 1.2, 0.25)
for u in (-1.2, 1.2):
    for s in (-1, 1):
        bar(b, bf.p(u, s * 0.8, 0.0), bf.p(u, s * 0.25, 0.75), 0.12, 0.12, "PAL", TIMBER)
    b.box(bf.p(u, 0, 0.62), (0.2, 0.7, 0.14), "PAL", TIMBER, rot_z=bf.rot)
hull(b, f, -1.5, 1.2, length=4.6, beam=1.6, depth=0.75, z=0.6, yaw=0.25, col=HULL)
b.box(bf.p(0, 0, 0.75), (3.2, 0.9, 0.06), "PAL", TIMBER_L, rot_z=bf.rot)                  # floorboards
for u in (-0.9, 0.7):
    b.box(bf.p(u, 0, 1.05), (0.3, 1.3, 0.08), "PAL", TIMBER_L, rot_z=bf.rot)               # thwarts
b.prism(bf.p(0.35, 0, 0.75), 0.08, 4.6, 6, "PAL", TIMBER_L, r_top=0.05)
bar(b, bf.p(0.35, 0, 1.55), bf.p(-1.9, 0, 1.5), 0.07, 0.07, "PAL", TIMBER_L)
# the sail furled along the boom and lashed down, as it would be with the boat out of the water; set, it hid the
# side of the boathouse and the gable window
bar(b, bf.p(0.25, 0, 1.68), bf.p(-0.8, 0, 1.67), 0.26, 0.22, "PAL", SAIL)
bar(b, bf.p(-0.8, 0, 1.67), bf.p(-1.8, 0, 1.63), 0.18, 0.16, "PAL", SAIL)
for u in (0.0, -0.55, -1.1, -1.55):
    w = 0.29 if u > -0.8 else 0.21
    bar(b, bf.p(u + 0.04, 0, 1.68), bf.p(u - 0.04, 0, 1.67), w, w - 0.03, "PAL", "c6a177")
for s in (1, -1):
    b.face([bf.p(0.4, s * 0.01, 5.3), bf.p(1.0, s * 0.01, 5.18), bf.p(0.4, s * 0.01, 5.05)], bf.R @ Vector((0, s, 0)), "PAL", RED)

# the anchor, rope coils, oars, crates and barrels
af = f.sub(3.6, 1.6, -0.4)
bar(b, af.p(0, 0.0, 0.1), af.p(0, 0.25, 1.5), 0.1, 0.1, "PAL", DARK)
bar(b, af.p(-0.35, 0.22, 1.32), af.p(0.35, 0.22, 1.32), 0.08, 0.08, "PAL", DARK)
for s in (-1, 1):
    bar(b, af.p(0, 0.0, 0.12), af.p(s * 0.42, -0.02, 0.45), 0.09, 0.09, "PAL", DARK)
    b.prism(af.p(s * 0.42, -0.02, 0.45), 0.08, 0.16, 4, "PAL", DARK, r_top=0.0)
torus(b, af.p(0, 0.28, 1.62), af.R @ Vector((1, 0, 0)), 0.12, 0.035, DARK, seg=8, tube=4)
for k in range(3):
    torus(b, f.p(1.0, 2.9, 0.08 + k * 0.1), (0, 0, 1), 0.34 - k * 0.04, 0.06, "c6a177", seg=10, tube=4)
for s in (0.0, 0.35):
    of = hf.sub(-0.6 + s, 1.85, 0.0)
    bar(b, of.p(0, 0.25, 0.05), of.p(0.15, 0.05, 2.3), 0.06, 0.06, "PAL", TIMBER_L)
    b.box(of.p(0.02, 0.25, 0.35), (0.16, 0.04, 0.6), "PAL", TIMBER_L, rot_z=of.rot)
crate(b, f, 3.4, -0.1, s=0.6, yaw=0.2)
barrel(b, f, 3.8, 3.2)
barrel(b, f, 3.2, 3.5, r=0.28, h=0.7)
# a post with a lamp at the end of the deck
lantern_post(b, f, -4.1, 3.4, 2.2)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Sailing")
preview([house], "house_sailing", views=((200, 26), (150, 30)), res=(1100, 950), dist_mul=1.05)
