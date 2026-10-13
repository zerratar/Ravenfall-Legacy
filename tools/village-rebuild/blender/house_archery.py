# Archer Range (ranged): a fletcher's hut, a shooting lane of three targets on hay bales with arrows in them, a
# shooting line, a bow rack and quivers. The middle target has an apple on top with an arrow through it.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Archery")
f = Frame()
FLETCH, APPLE = "f5f5f5", "c04531"

def bow_emblem(b, ef, z):
    pts = [(math.sin(math.pi * k / 8 - math.pi / 2) * 0.1 - 0.05, math.cos(math.pi * k / 8 - math.pi / 2) * -0.22) for k in range(9)]
    for i in range(8):
        emb_bar(b, ef, z, pts[i], pts[i + 1], 0.04, TIMBER, d=0.02)
    emb_bar(b, ef, z, (-0.05, -0.22), (-0.05, 0.22), 0.012, WHITE, d=0.015)
    emb_bar(b, ef, z, (-0.2, 0.0), (0.22, 0.0), 0.025, TIMBER_L, d=0.025)
    emb(b, ef, z, [(0.22, 0.05), (0.3, 0.0), (0.22, -0.05)], "b9b9b9", d=0.025)

def arrow(b, a, c):
    """An arrow from its nock (a) to where its head sits (c)."""
    a, c = Vector(a), Vector(c)
    bar(b, tuple(a), tuple(c), 0.025, 0.025, "PAL", TIMBER_L)
    d = (c - a).normalized(); s = d.cross(Vector((0, 0, 1))).normalized()
    for sg in (1, -1):
        b.face([tuple(a + d * 0.02), tuple(a + d * 0.16), tuple(a + d * 0.04 + s * 0.06 * sg)], s.cross(d) * sg, "PAL", FLETCH)
        b.face([tuple(a + d * 0.02), tuple(a + d * 0.04 + s * 0.06 * sg), tuple(a + d * 0.16)], -s.cross(d) * sg, "PAL", FLETCH)

def bow(b, f, x, y, z0, h=1.3):
    """A bow standing on end against a rack: a curved stave and its string."""
    pts = [f.p(x, y + math.sin(math.pi * k / 6) * 0.18, z0 + h * k / 6) for k in range(7)]
    for i in range(6): bar(b, pts[i], pts[i + 1], 0.045, 0.045, "PAL", TIMBER)
    bar(b, pts[0], pts[-1], 0.01, 0.01, "PAL", WHITE)

yard(b, f, 4.6, 4.4, DIRT, seed=8.0)
# grass in the lane
for k in range(2):
    b.box(f.p(-1.4, -0.4 + k * 1.6, 0.06), (5.0, 1.55, 0.06), "PAL", "666f4a" if k % 2 else "5c7a46", rot_z=f.rot)

# the fletcher's hut
hf = f.sub(2.5, -2.4, 0.0)
cottage(b, hf, 3.8, 3.2, 2.9, roof="ROOF_TEAL", pitch=42, overhang=0.42, plinth=0.4, shutters="red",
        front_windows=(-0.9,), side_windows=(0.0,), back_windows=(), door_x=0.75, chimney=None)
fw = hf.sub(0, 1.6, 0)
sign(b, fw, -1.75, 2.35, 0.0, bow_emblem)

# the targets on hay bales at the far end of the lane, arrows in them
for k, x in enumerate((-3.4, -1.6, 0.2)):
    for dz in (0.0, 0.6):
        hay_bale(b, f, x, -3.5, z=dz, yaw=0.0)
    tf, c, n = target(b, f, x, -2.7, yaw=0.0, r=0.62)
    side = tf.R @ Vector((1, 0, 0)); up = Vector((0, 0, 1))
    for j, (u, v) in enumerate(((0.05, 0.02), (-0.22, 0.18), (0.3, -0.25))[:k + 1]):
        tip = c + side * u + up * v
        arrow(b, tuple(tip + n * 0.65 + up * 0.05), tuple(tip + n * 0.02))
# the apple on the middle target, and the arrow that got it
ac = Vector(f.p(-1.6, -2.7, 1.98))
b.sphere(tuple(ac), 0.12, "PAL", APPLE, seg=7, rings=4)
b.box(tuple(ac + Vector((0, 0, 0.14))), (0.02, 0.02, 0.07), "PAL", TIMBER, rot_z=0)
b.box(tuple(ac + Vector((0.04, 0, 0.15))), (0.07, 0.02, 0.04), "PAL", LEAF, rot_z=0.4)
arrow(b, tuple(ac + Vector((0.55, 0.3, 0.02))), tuple(ac + Vector((-0.25, -0.13, -0.01))))
# the shooting line, a bow rack and two quivers
bar(b, f.p(-4.0, 2.75, 0.1), f.p(0.6, 2.75, 0.1), 0.18, 0.16, "PAL", TIMBER_L)            # the shooting line
rf = f.sub(1.2, 2.2, 0.0)
for s in (-1, 1):
    b.box(rf.p(s * 0.8, 0, 0.6), (0.12, 0.12, 1.2), "PAL", TIMBER, rot_z=rf.rot)
bar(b, rf.p(-0.9, 0, 1.15), rf.p(0.9, 0, 1.15), 0.1, 0.1, "PAL", TIMBER_L)
bar(b, rf.p(-0.9, 0.2, 0.1), rf.p(0.9, 0.2, 0.1), 0.1, 0.1, "PAL", TIMBER_L)
for x in (-0.5, 0.0, 0.5):
    bow(b, rf, x, 0.12, 0.08)
for qx in (2.6, 3.0):
    qc = f.p(qx, 2.6, 0.0)
    b.lathe(qc, [(0.12, 0.0), (0.14, 0.75), (0.0, 0.75)], 8, "PAL", "6f4f3a")
    for k in range(4):
        a = k * 1.6
        arrow(b, (qc[0] + math.cos(a) * 0.06, qc[1] + math.sin(a) * 0.06, 1.12), (qc[0] + math.cos(a) * 0.05, qc[1] + math.sin(a) * 0.05, 0.6))
barrel(b, f, 3.8, 0.6)
tree(b, f, -3.6, -3.7, h=3.6, r=1.1)                      # behind the targets, clear of the range

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Archery")
preview([house], "house_archery", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
