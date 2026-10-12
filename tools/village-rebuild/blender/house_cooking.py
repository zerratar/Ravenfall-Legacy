# Cooking House: a bakery with a smoking chimney, a domed brick oven glowing inside, a table of bread, a pie and a
# cheese wheel, flour sacks, firewood, a string of sausages under the eaves, and a pie cooling on the windowsill.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Cooking")
f = Frame()
CRUST, CRUST_D, CHEESE, SMOKE, SAUSAGE, FLOUR = "c6a177", "8e5a30", "e4aa60", "d6ceca", "714242", "e6e6e6"

def bread_emblem(b, ef, z):
    emb(b, ef, z, [(math.cos(math.pi * k / 10) * 0.22, -0.08 + math.sin(math.pi * k / 10) * 0.18) for k in range(11)], CRUST, d=0.02)
    for u in (-0.09, 0.0, 0.09):
        emb_bar(b, ef, z, (u - 0.03, -0.0), (u + 0.03, 0.08), 0.02, CRUST_D, d=0.025)

def loaf(b, f, x, y, z, yaw=0.0, s=1.0):
    lf = f.sub(x, y, yaw)
    b.sphere(lf.p(0, 0, z + 0.06 * s), 0.16 * s, "PAL", CRUST, seg=7, rings=4, sz=0.55)
    for k in (-1, 0, 1):
        b.box(lf.p(k * 0.07 * s, 0, z + 0.14 * s), (0.02, 0.12 * s, 0.015), "PAL", CRUST_D, rot_z=lf.rot + 0.4)

def pie(b, f, x, y, z, r=0.2):
    c = f.p(x, y, z)
    b.lathe(c, [(r * 0.8, 0.0), (r, 0.07), (r, 0.09), (r * 0.6, 0.12), (0.0, 0.13)], 10, "PAL", CRUST)
    for k in range(4):
        a = math.pi / 4 * k
        bar(b, (c[0] - math.cos(a) * r * 0.7, c[1] - math.sin(a) * r * 0.7, z + 0.125), (c[0] + math.cos(a) * r * 0.7, c[1] + math.sin(a) * r * 0.7, z + 0.125), 0.025, 0.01, "PAL", CRUST_D)

yard(b, f, 4.6, 4.4, DIRT, seed=11.0)

# the bakery
hf = f.sub(1.4, -2.2, 0.0)
ridge = cottage(b, hf, 5.2, 3.6, 3.1, roof="ROOF_BROWN", pitch=40, overhang=0.45, plinth=0.45, shutters="blue",
                front_windows=(-1.5, 1.5), side_windows=(0.0,), back_windows=(0.0,), door_x=0.0, chimney=(1.8, -0.6))
for k, (dz, r, dx) in enumerate(((1.45, 0.28, 0.0), (1.95, 0.36, 0.25), (2.55, 0.42, 0.6))):
    b.sphere(hf.p(1.8 + dx, -0.6, ridge + dz), r, "PAL", SMOKE, seg=6, rings=4)
fw = hf.sub(0, 1.8, 0)
sign(b, fw, -2.6, 2.6, 0.0, bread_emblem)
pie(b, fw.sub(1.5, 0.12, 0), 0.0, 0.05, 1.53, r=0.17)                      # cooling on the sill
# sausages hanging under the eaves
for k in range(7):
    x = -0.9 + k * 0.26
    b.lathe(fw.p(x, 0.35, 2.55 - 0.18 * math.sin(math.pi * k / 6)), [(0.0, -0.18), (0.05, -0.15), (0.05, 0.0), (0.0, 0.03)], 6, "PAL", SAUSAGE)
bar(b, fw.p(-1.0, 0.35, 2.62), fw.p(0.75, 0.35, 2.62), 0.015, 0.015, "PAL", "c6a177")

# the dome oven with its glowing mouth and a chimney stub
oc = f.sub(-2.6, -1.0, 0.0)
b.box(oc.p(0, 0, 0.35), (2.0, 2.0, 0.7), "WALL", rot_z=oc.rot)
b.lathe(oc.p(0, 0, 0.7), [(0.92, 0.0), (0.85, 0.45), (0.6, 0.85), (0.3, 1.05), (0.0, 1.1)], 10, "WALL", cyl_radius=0.9)
mf = oc.sub(0, 0.86, 0)
b.solid_xz([(-0.32, 0.72), (0.32, 0.72), (0.32, 1.05), (0.22, 1.2), (0.0, 1.26), (-0.22, 1.2), (-0.32, 1.05)], -0.1, 0.06, "PAL", DARK, rot_z=mf.rot, origin=mf.o)
b.face([mf.p(-0.24, 0.07, 0.74), mf.p(0.24, 0.07, 0.74), mf.p(0.24, 0.07, 0.98), mf.p(-0.24, 0.07, 0.98)], mf.R @ Vector((0, 1, 0)), "GLASS", "f27250")
b.prism(oc.p(-0.45, -0.35, 1.4), 0.2, 0.7, 6, "WALL", r_top=0.17)
b.sphere(oc.p(-0.45, -0.35, 2.4), 0.24, "PAL", SMOKE, seg=6, rings=4)
log_stack(b, f, -2.8, 0.7, length=1.6, rows=2, yaw=0.0, r=0.15)

# the table of the day's baking
tf = f.sub(0.6, 1.9, 0.1)
b.box(tf.p(0, 0, 0.8), (2.0, 0.9, 0.08), "PAL", TIMBER_L, rot_z=tf.rot)
for x in (-0.85, 0.85):
    for y in (-0.35, 0.35):
        b.box(tf.p(x, y, 0.4), (0.1, 0.1, 0.8), "PAL", TIMBER, rot_z=tf.rot)
for k, (x, y) in enumerate(((-0.65, 0.1), (-0.35, -0.2), (-0.1, 0.18))):
    loaf(b, tf, x, y, 0.84, yaw=k * 0.6)
pie(b, tf, 0.35, 0.05, 0.84, r=0.2)
cc = tf.p(0.75, -0.15, 0.84)
b.lathe(cc, [(0.18, 0.0), (0.18, 0.12), (0.0, 0.12)], 10, "PAL", CHEESE)
for x, y in ((0.7, 0.28), (0.85, 0.22)):
    b.lathe(tf.p(x, y, 0.84), [(0.05, 0.0), (0.055, 0.14), (0.0, 0.14)], 7, "PAL", TIMBER_L)
for s in (-1, 1):
    b.box(tf.p(0, s * 0.85, 0.45), (1.8, 0.35, 0.07), "PAL", TIMBER_L, rot_z=tf.rot)
    for x in (-0.7, 0.7):
        b.box(tf.p(x, s * 0.85, 0.22), (0.08, 0.3, 0.44), "PAL", TIMBER, rot_z=tf.rot)
# sacks of flour, barrels and a crate of apples
for k, (x, y) in enumerate(((3.3, 0.5), (3.7, 0.9), (3.5, 1.4))):
    sack(b, f, x, y, col=FLOUR)
barrel(b, f, 3.6, 2.6)
barrel(b, f, 3.0, 3.2, r=0.28, h=0.7)
crate(b, f, -1.6, 3.2, s=0.6, yaw=0.3)
for k in range(5):
    b.sphere(f.p(-1.75 + 0.14 * (k % 3), 3.1 + 0.12 * (k // 3), 0.66), 0.08, "PAL", RED, seg=5, rings=3)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Cooking")
preview([house], "house_cooking", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
