# Healing House: an infirmary with a little bell on its ridge, herb beds, a glowing healing spring, a well, and a
# bandaged training dummy on a bench, recovering from the barracks. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Healing")
f = Frame()
HEART, HERB, SPRING, BANDAGE, BELL = "c04531", "5c8f51", "5f8191", "f5f5f5", "bda248"

def heart_emblem(b, ef, z):
    emb_circle(b, ef, z, -0.07, 0.05, 0.08, HEART, d=0.02, n=10)
    emb_circle(b, ef, z, 0.07, 0.05, 0.08, HEART, d=0.02, n=10)
    emb(b, ef, z, [(-0.15, 0.03), (0.15, 0.03), (0.0, -0.17)], HEART, d=0.02)
    emb(b, ef, z, [(0.1, 0.12), (0.2, 0.22), (0.24, 0.14), (0.16, 0.08)], HERB, d=0.025)

yard(b, f, 4.6, 4.4, DIRT, seed=15.0)

# the infirmary, with a bell cote on the ridge
hf = f.sub(1.0, -2.3, 0.0)
ridge = cottage(b, hf, 5.8, 3.8, 3.3, roof="ROOF", pitch=40, overhang=0.45, plinth=0.45, shutters="green",
                front_windows=(-2.0, 2.0), side_windows=(0.0,), back_windows=(-1.4, 1.4), door_x=0.0, chimney=(-2.0, -0.6))
bc = hf.sub(1.2, 0.0, 0.0)
for sx in (-1, 1):
    for sy in (-1, 1):
        b.box(bc.p(sx * 0.32, sy * 0.32, ridge + 0.55), (0.1, 0.1, 0.8), "PAL", WHITE, rot_z=bc.rot)
b.prism(bc.p(0, 0, ridge + 0.95), 0.62, 0.55, 4, "ROOF", r_top=0.0, rot=math.pi / 4)
b.lathe(bc.p(0, 0, ridge + 0.38), [(0.17, 0.0), (0.15, 0.08), (0.1, 0.32), (0.0, 0.36)], 8, "PAL", BELL)
b.sphere(bc.p(0, 0, ridge + 1.55), 0.06, "PAL", GOLD, seg=5, rings=3)
fw = hf.sub(0, 1.9, 0)
sign(b, fw, 2.9, 2.6, 0.0, heart_emblem)
# window boxes of flowers under the front windows
for x in (-2.0, 2.0):
    b.box(fw.p(x, 0.3, 1.38), (1.1, 0.3, 0.22), "PAL", TIMBER_L, rot_z=fw.rot)
    for k in range(5):
        b.sphere(fw.p(x - 0.4 + k * 0.2, 0.3, 1.55), 0.08, "PAL", ("c04531", "f5f5f5", "e4aa60", "564888", "f5f5f5")[k], seg=5, rings=3)

# herb beds: raised boxes with plants and flowers
for k, (x, y) in enumerate(((-3.0, 0.8), (-3.0, 2.4), (-1.2, 3.4))):
    bf = f.sub(x, y, 0.0 if k < 2 else math.pi / 2)
    b.box(bf.p(0, 0, 0.2), (1.6, 0.8, 0.4), "PAL", TIMBER_L, rot_z=bf.rot)
    b.box(bf.p(0, 0, 0.4), (1.45, 0.65, 0.04), "PAL", "4d3828", rot_z=bf.rot)
    for j in range(6):
        px, py = -0.55 + (j % 3) * 0.55, -0.15 + (j // 3) * 0.3
        b.prism(bf.p(px, py, 0.42), 0.12, 0.3 + 0.1 * ((j + k) % 2), 5, "PAL", HERB if j % 2 else LEAF_D, r_top=0.0, rot=j)
        if (j + k) % 3 == 0: b.sphere(bf.p(px, py, 0.78), 0.06, "PAL", ("f5f5f5", "564888", "e4aa60")[k], seg=5, rings=3)
# the healing spring: a stone basin of glowing water with a little spout
sp = f.sub(1.6, 1.4, 0.0)
b.lathe(sp.p(0, 0, 0), [(0.95, -0.2), (0.95, 0.5), (0.8, 0.55), (0.75, 0.35)], 12, "WALL", cyl_radius=0.9)
b.lathe(sp.p(0, 0, 0), [(0.75, 0.38), (0.0, 0.38)], 12, "GLASS", SPRING)
b.lathe(sp.p(0, 0, 0), [(0.18, 0.38), (0.15, 0.95), (0.25, 1.05), (0.0, 1.12)], 8, "WALL", cyl_radius=0.2)
b.lathe(sp.p(0, 0, 1.12), [(0.0, 0.0), (0.09, 0.08), (0.0, 0.3)], 6, "GLASS", SPRING)
for k in range(5):
    a = k * 1.3
    b.sphere(sp.p(math.cos(a) * 0.45, math.sin(a) * 0.45, 0.6 + 0.25 * (k % 3)), 0.04, "GLASS", "e6e6e6", seg=4, rings=3)
well(b, f, 3.6, 3.2)

# the patient: a training dummy, bandaged, on a bench, arm in a sling
pb = f.sub(-0.4, 1.0, 0.0)
b.box(pb.p(0, 0, 0.45), (1.6, 0.45, 0.08), "PAL", TIMBER_L, rot_z=pb.rot)
for x in (-0.65, 0.65):
    b.box(pb.p(x, 0, 0.22), (0.1, 0.4, 0.44), "PAL", TIMBER, rot_z=pb.rot)
b.lathe(pb.p(0.2, 0, 0.49), [(0.0, 0.0), (0.24, 0.04), (0.28, 0.35), (0.22, 0.65), (0.0, 0.7)], 7, "PAL", HAY)
for z in (0.62, 0.8, 0.98):
    b.lathe(pb.p(0.2, 0, 0.0), [(0.29, z - 0.04), (0.3, z + 0.04)], 7, "PAL", BANDAGE)
b.lathe(pb.p(0.2, 0, 1.2), [(0.14, 0.0), (0.18, 0.28), (0.0, 0.28)], 8, "PAL", "8b8b8b")
b.lathe(pb.p(0.2, 0, 1.2), [(0.165, 0.12), (0.175, 0.2)], 8, "PAL", BANDAGE)
bar(b, pb.p(-0.2, 0.0, 1.0), pb.p(-0.55, 0.0, 0.85), 0.09, 0.09, "PAL", TIMBER_L)
bar(b, pb.p(0.55, 0.12, 0.95), pb.p(0.35, 0.22, 0.75), 0.1, 0.1, "PAL", TIMBER_L)
b.box(pb.p(0.42, 0.2, 0.82), (0.28, 0.14, 0.18), "PAL", BANDAGE, rot_z=pb.rot + 0.4)
bar(b, pb.p(0.32, 0.08, 1.15), pb.p(0.55, 0.2, 0.86), 0.04, 0.02, "PAL", BANDAGE)
tree(b, f, -3.8, -2.6, h=3.8, r=1.2)
bush(b, f, 3.9, 0.6, r=0.5)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Healing")
preview([house], "house_healing", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
