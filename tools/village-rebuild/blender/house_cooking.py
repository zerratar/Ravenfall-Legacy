# Cooking House: a bakery with a smoking chimney, a domed brick oven glowing inside, a table of bread, a pie and a
# cheese wheel, flour sacks, firewood, a string of sausages under the eaves, and a pie cooling on the windowsill.
# The table, benches, bread, pies, cheese, bottles, sausages, sacks, firewood, barrels and crate are Synty props;
# the bakery, the oven and the smoke are ours. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Cooking")
f = Frame()
CRUST, CRUST_D, CHEESE, SMOKE, SAUSAGE, FLOUR = "c6a177", "8e5a30", "e4aa60", "d6ceca", "714242", "e6e6e6"

def bread_emblem(b, ef, z):
    emb(b, ef, z, [(math.cos(math.pi * k / 10) * 0.22, -0.08 + math.sin(math.pi * k / 10) * 0.18) for k in range(11)], CRUST, d=0.02)
    for u in (-0.09, 0.0, 0.09):
        emb_bar(b, ef, z, (u - 0.03, -0.0), (u + 0.03, 0.08), 0.02, CRUST_D, d=0.025)

def loaf(b, f, x, y, z, yaw=0.0, s=1.0):
    anchor(f, "bread", x, y, z, yaw=yaw, s=s)

def pie(b, f, x, y, z, r=0.2):
    anchor(f, "pie", x, y, z, s=r / 0.15)

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
for x in (-0.8, 0.8):                      # either side of the door, clear of it
    anchor(fw, "sausages", x, 0.3, 2.45)

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

# the table of the day's baking, a bench either side; the Synty table is long along its depth, so it turns across
tf = f.sub(0.6, 1.9, 0.0)
anchor(tf, "table", 0, 0, yaw=math.pi / 2)
TOP = 0.86
for k, (x, y) in enumerate(((-0.75, 0.1), (-0.4, -0.2), (-0.1, 0.18))):
    loaf(b, tf, x, y, TOP, yaw=k * 0.6)
pie(b, tf, 0.3, 0.05, TOP, r=0.2)
anchor(tf, "cheese", 0.75, -0.15, TOP)
for x, y in ((0.75, 0.28), (0.9, 0.18)):
    anchor(tf, "bottle", x, y, TOP)
for s in (-1, 1):
    anchor(tf, "bench", 0, s * 0.95, yaw=0.0 if s > 0 else math.pi)
# sacks of flour, barrels and a crate of apples
anchor(f, "sack_stack", 3.5, 0.9, yaw=0.4)
barrel(b, f, 3.6, 2.6)
barrel(b, f, 3.0, 3.2, r=0.28, h=0.7)
crate(b, f, -1.6, 3.2, s=0.6, yaw=0.3)
for k in range(5):
    b.sphere(f.p(-1.75 + 0.14 * (k % 3), 3.1 + 0.12 * (k // 3), 0.66), 0.08, "PAL", RED, seg=5, rings=3)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Cooking")
preview([house], "house_cooking", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
