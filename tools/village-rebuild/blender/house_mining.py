# Mining Shelter: a timbered mine entrance in a rock mound, rails out to a cart full of ore, a tool shed with a
# pickaxe sign, an ore pile and a lantern. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Mining")
f = Frame()

def pickaxe(b, ef, z):
    n = ef.R @ Vector((0, 1, 0))
    bar(b, ef.p(-0.2, 0.01, z - 0.18), ef.p(0.16, 0.01, z + 0.16), 0.05, 0.02, "PAL", TIMBER_L, normal=n)
    bar(b, ef.p(-0.04, 0.02, z + 0.22), ef.p(0.24, 0.02, z + 0.08), 0.06, 0.02, "PAL", "b9b9b9", normal=n)
    bar(b, ef.p(-0.04, 0.02, z + 0.22), ef.p(0.0, 0.02, z - 0.02), 0.06, 0.02, "PAL", "b9b9b9", normal=n)

yard(b, f, 4.6, 4.4, DIRT, seed=2.0)

# the hill behind: a faceted heightfield, grassy where it is flat and rocky where it is steep, ending at the
# front in a rock face with the mine cut into it
XC, YC, AX, AY, HH = -2.0, -1.6, 2.8, 3.4, 3.6
Y0 = -0.6                      # the rock face
OX0, OX1, OZ = -2.8, -1.2, 2.2  # the tunnel mouth
def hill(x, y):
    r2 = ((x - XC) / AX) ** 2 + ((y - YC) / AY) ** 2
    if r2 >= 1: return -0.12
    h = HH * math.sqrt(1 - r2)
    h += 0.24 * math.sin(2.1 * x + 1.3 * y) * math.cos(1.7 * y - 0.8 * x) * min(1.0, h)
    return max(h, -0.12)
xs = sorted(set([round(-4.9 + 0.5 * i, 3) for i in range(13)] + [OX0, OX1, -2.0]))
ys = [round(-5.0 + (Y0 + 5.0) * j / 8, 3) for j in range(9)]
for i in range(len(xs) - 1):
    for j in range(len(ys) - 1):
        q = [(xs[i], ys[j]), (xs[i + 1], ys[j]), (xs[i + 1], ys[j + 1]), (xs[i], ys[j + 1])]
        zs = [hill(x, y) for x, y in q]
        if max(zs) <= -0.1: continue
        pts = [Vector(f.p(x, y, z)) for (x, y), z in zip(q, zs)]
        nz = abs((pts[1] - pts[0]).cross(pts[3] - pts[0]).normalized().z)
        b.face([tuple(v) for v in pts], (0, 0, 1), "PAL", GRASS if nz > 0.8 else (ROCK if nz > 0.5 else ROCK_D))
for i in range(len(xs) - 1):
    x0, x1 = xs[i], xs[i + 1]
    z0, z1 = hill(x0, Y0), hill(x1, Y0)
    if max(z0, z1) <= -0.1: continue
    base = OZ if (x0 >= OX0 - 1e-6 and x1 <= OX1 + 1e-6) else -0.3
    b.face([f.p(x0, Y0, base), f.p(x1, Y0, base), f.p(x1, Y0, z1), f.p(x0, Y0, z0)], (0, 1, 0), "PAL", ROCK_D)
# the tunnel: walls darken with depth, timber frames repeat into the dark
depth = [Y0, -1.6, -2.6, -3.6]
shade = [ROCK_D, "3a2e2c", "191a1b"]
for k in range(3):
    ya, yb = depth[k], depth[k + 1]
    b.face([f.p(OX0, ya, 0), f.p(OX0, yb, 0), f.p(OX0, yb, OZ), f.p(OX0, ya, OZ)], (1, 0, 0), "PAL", shade[k])
    b.face([f.p(OX1, ya, 0), f.p(OX1, yb, 0), f.p(OX1, yb, OZ), f.p(OX1, ya, OZ)], (-1, 0, 0), "PAL", shade[k])
    b.face([f.p(OX0, ya, OZ), f.p(OX1, ya, OZ), f.p(OX1, yb, OZ), f.p(OX0, yb, OZ)], (0, 0, -1), "PAL", shade[k])
    b.face([f.p(OX0, ya, 0.03), f.p(OX1, ya, 0.03), f.p(OX1, yb, 0.03), f.p(OX0, yb, 0.03)], (0, 0, 1), "PAL", shade[min(k + 1, 2)])
b.face([f.p(OX0, depth[-1], 0), f.p(OX1, depth[-1], 0), f.p(OX1, depth[-1], OZ), f.p(OX0, depth[-1], OZ)], (0, 1, 0), "PAL", DARK)
for k, y in enumerate((Y0 + 0.14, -1.55, -2.55)):
    big = k == 0
    pw = 0.28 if big else 0.22
    for x in (OX0 + pw / 2 + 0.02, OX1 - pw / 2 - 0.02):
        b.box(f.p(x, y, OZ / 2), (pw, pw, OZ), "PAL", TIMBER, rot_z=f.rot)
        b.box(f.p(x, y, OZ - 0.34), (0.5 if big else 0.36, pw, 0.16), "PAL", TIMBER, rot_z=f.rot)     # corbel under the cap
    b.box(f.p((OX0 + OX1) / 2, y, OZ - 0.13), ((OX1 - OX0) + (0.5 if big else 0.0), pw, 0.26), "PAL", TIMBER, rot_z=f.rot)
b.box(f.p((OX0 + OX1) / 2, Y0 + 0.05, OZ + 0.45), (1.2, 0.1, 0.36), "PAL", TIMBER_L, rot_z=f.rot)       # name plank
for x in (-0.4, 0.4):
    b.box(f.p((OX0 + OX1) / 2 + x, Y0 + 0.11, OZ + 0.45), (0.06, 0.04, 0.06), "PAL", DARK, rot_z=f.rot)
b.box(f.p(OX1 - 0.4, Y0 + 0.3, OZ - 0.62), (0.2, 0.2, 0.28), "GLASS", WARM, rot_z=f.rot)                 # lantern
b.prism(f.p(OX1 - 0.4, Y0 + 0.3, OZ - 0.48), 0.16, 0.12, 4, "PAL", DARK, r_top=0.0, rot=math.pi / 4)
b.box(f.p(OX1 - 0.4, Y0 + 0.3, OZ - 0.33), (0.03, 0.03, 0.2), "PAL", DARK, rot_z=f.rot)
# boulders at the foot of the face
rock(b, f, -3.75, -0.25, 0.75, sz=0.8, col=ROCK, seg=7, seed=1.1)
rock(b, f, -4.2, 0.35, 0.45, sz=0.8, col=ROCK_D, seg=6, seed=2.4)
rock(b, f, -0.55, -0.35, 0.6, sz=0.85, col=ROCK_D, seg=7, seed=3.7)
# rails from deep in the tunnel out into the yard, then the cart on them
rails = [f.p(-2.0, -3.2, 0), f.p(-2.0, -0.3, 0), f.p(-0.9, 1.7, 0), f.p(0.2, 3.9, 0)]
for i in range(len(rails) - 1):
    a, c = Vector(rails[i]), Vector(rails[i + 1]); d = (c - a); L = d.length; d.normalize(); s = Vector((-d.y, d.x, 0))
    for k in range(int(L / 0.6) + 1):
        p = a + d * (k * 0.6)
        bar(b, tuple(p - s * 0.55 + Vector((0, 0, 0.06))), tuple(p + s * 0.55 + Vector((0, 0, 0.06))), 0.16, 0.08, "PAL", TIMBER_L)
    for side in (-0.36, 0.36):
        bar(b, tuple(a + s * side + Vector((0, 0, 0.13))), tuple(c + s * side + Vector((0, 0, 0.13))), 0.06, 0.06, "PAL", IRON)
cart_c = (Vector(rails[2]) * 0.55 + Vector(rails[3]) * 0.45); d = (Vector(rails[3]) - Vector(rails[2])).normalized()
yaw = math.atan2(d.y, d.x) - math.pi / 2
cf = Frame(cart_c.x, cart_c.y, yaw)
b.quad_box([cf.p(-0.45, -0.6, 0.35), cf.p(0.45, -0.6, 0.35), cf.p(0.45, 0.6, 0.35), cf.p(-0.45, 0.6, 0.35)],
           [cf.p(-0.6, -0.78, 1.0), cf.p(0.6, -0.78, 1.0), cf.p(0.6, 0.78, 1.0), cf.p(-0.6, 0.78, 1.0)], "PAL", "6e6557")
for sx in (-1, 1):
    for sy in (-1, 1):
        # wheels: short cylinders on their side
        c = Vector(cf.p(sx * 0.5, sy * 0.4, 0.22))
        ax = cf.R @ Vector((sx, 0, 0))
        n = 8; rr = 0.2
        up = Vector((0, 0, 1)); fw = cf.R @ Vector((0, 1, 0))
        ring = lambda off: [c + ax * off + (fw * math.cos(2 * math.pi * i / n) + up * math.sin(2 * math.pi * i / n)) * rr for i in range(n)]
        r0, r1 = ring(-0.05), ring(0.05)
        b.face(r1, ax, "PAL", DARK); b.face(r0, -ax, "PAL", DARK)
        for i in range(n):
            j = (i + 1) % n; mid = ((r0[i] + r0[j]) / 2 - (c - ax * 0.05)); mid = mid - ax * mid.dot(ax)
            b.face([r0[i], r0[j], r1[j], r1[i]], mid, "PAL", DARK)
for i, (dx, dy, col) in enumerate([(-0.2, -0.25, ROCK), (0.2, 0.1, ROCK_D), (-0.1, 0.35, GOLD), (0.25, -0.35, "5f8191"), (0.0, 0.0, ROCK)]):
    b.sphere(cf.p(dx, dy, 1.0), 0.24 if col in (ROCK, ROCK_D) else 0.16, "PAL", col, seg=6, rings=4, sz=0.8)

# tool shed with the sign
sf = f.sub(2.5, -0.6, 0.0)
cottage(b, sf, 3.4, 3.0, 2.7, roof="SLAT_BROWN", pitch=36, overhang=0.4, plinth=0.35, shutters="brown",
        front_windows=(), side_windows=(0.0,), back_windows=(), door_x=0.55, chimney=None, braces=True)
window(b, sf.sub(0, 1.5, 0), -0.85, 1.6, 0.0, w=0.6, h=0.7, shutters="brown")
sign(b, sf.sub(0, 1.5, 0), -1.75, 2.0, 0.0, pickaxe)

# ore pile, a pickaxe leaning on a crate, crates, a lantern by the mine
for i, (dx, dy, r) in enumerate([(0, 0, 0.55), (0.45, 0.2, 0.4), (-0.35, 0.25, 0.42), (0.1, 0.45, 0.35), (0.05, 0.12, 0.38)]):
    rock(b, f, 3.3 + dx, 2.6 + dy, r, sz=0.8, col=ROCK_D if i % 2 else ROCK, seg=6, seed=i * 1.3)
b.sphere(f.p(3.3, 2.75, 0.62), 0.14, "PAL", GOLD, seg=6, rings=4)
crate(b, f, 1.6, 2.9, s=0.65, yaw=0.3)
crate(b, f, 1.75, 3.5, s=0.5, yaw=-0.2)
pk = f.sub(1.25, 2.55, 0.3)
bar(b, pk.p(0, 0, 0.05), pk.p(0, 0.32, 0.95), 0.06, 0.06, "PAL", TIMBER_L)
bar(b, pk.p(-0.3, 0.37, 0.98), pk.p(0.3, 0.3, 0.92), 0.08, 0.06, "PAL", "b9b9b9")
barrel(b, f, -3.0, 1.2)
barrel(b, f, -3.5, 1.8, r=0.28, h=0.7)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Mining")
preview([house], "house_mining", views=((200, 28), (150, 35)), res=(1100, 900), dist_mul=1.05)
