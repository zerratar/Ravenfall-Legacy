# Yard props for the village houses: fences, trees, bushes, hay, sacks, logs, training dummies, targets,
# boats, a well and a round hut. Loaded by house_kit.py; same conventions (local frames, +y is the front).
LEAF, LEAF_D, HAY, CLOTH, RED, BLUE_C = "5c8f51", "4d7451", "bda248", "c6a177", "c04531", "396b7c"

def fence(b, f, pts, h=0.9, col=TIMBER_L, post=TIMBER):
    """Posts and two rails along a polyline of local (x, y) points."""
    for i in range(len(pts) - 1):
        a, c = Vector(f.p(pts[i][0], pts[i][1], 0)), Vector(f.p(pts[i + 1][0], pts[i + 1][1], 0))
        L = (c - a).length; n = max(1, int(L / 1.6 + 0.5))
        for k in range(n + 1):
            p = a + (c - a) * (k / n)
            b.box((p.x, p.y, h / 2), (0.14, 0.14, h + 0.1), "PAL", post, rot_z=math.atan2((c - a).y, (c - a).x))
        for z in (h * 0.45, h * 0.85):
            bar(b, (a.x, a.y, z), (c.x, c.y, z), 0.08, 0.06, "PAL", col)

def tree(b, f, x, y, h=4.0, r=1.4, col=None, seed=0.0):
    """A low poly tree: a trunk and two stacked faceted crowns."""
    col = col or LEAF
    b.prism(f.p(x, y, 0), 0.16 * h / 4, h * 0.45, 6, "PAL", TIMBER, r_top=0.11 * h / 4)
    b.sphere(f.p(x, y, h * 0.55), r, "PAL", col, seg=7, rings=4, sz=0.85)
    b.sphere(f.p(x + 0.2 * math.sin(seed), y + 0.2 * math.cos(seed), h * 0.8), r * 0.7, "PAL", LEAF_D if col == LEAF else LEAF, seg=6, rings=4, sz=0.9)

def pine(b, f, x, y, h=4.5, r=1.1):
    b.prism(f.p(x, y, 0), 0.14, h * 0.3, 6, "PAL", TIMBER, r_top=0.1)
    for k, (z, rr) in enumerate(((0.25, 1.0), (0.5, 0.75), (0.72, 0.5))):
        b.prism(f.p(x, y, h * z), r * rr, h * 0.35, 7, "PAL", LEAF_D if k % 2 else LEAF, r_top=0.0, rot=k)

def bush(b, f, x, y, r=0.5, col=None, berries=None):
    b.sphere(f.p(x, y, r * 0.6), r, "PAL", col or LEAF, seg=6, rings=4, sz=0.8)
    if berries:
        for k in range(5):
            a = k * 2.4
            b.sphere(f.p(x + math.cos(a) * r * 0.8, y + math.sin(a) * r * 0.8, r * (0.6 + 0.25 * math.sin(k))), 0.07, "PAL", berries, seg=4, rings=3)

def hay_bale(b, f, x, y, z=0.0, yaw=0.0):
    hf = f.sub(x, y, yaw)
    b.box(hf.p(0, 0, z + 0.3), (1.0, 0.55, 0.6), "PAL", HAY, rot_z=hf.rot)
    for dx in (-0.25, 0.25):
        b.box(hf.p(dx, 0, z + 0.3), (0.05, 0.57, 0.62), "PAL", TIMBER_L, rot_z=hf.rot)

def sack(b, f, x, y, z=0.0, col=None):
    b.lathe(f.p(x, y, z), [(0.0, 0.0), (0.26, 0.04), (0.3, 0.3), (0.2, 0.52), (0.08, 0.6), (0.12, 0.68), (0.0, 0.7)], 7, "PAL", col or CLOTH)

def log(b, a, c, r, bark=None, end="c6a177", n=7):
    """One log from point a to point c."""
    a, c = Vector(a), Vector(c)
    axis = (c - a).normalized(); up = Vector((0, 0, 1))
    side = axis.cross(up)
    if side.length < 1e-3: side = Vector((1, 0, 0))
    side.normalize(); up = side.cross(axis).normalized()
    ring = lambda o: [o + (side * math.cos(2 * math.pi * i / n) + up * math.sin(2 * math.pi * i / n)) * r for i in range(n)]
    r0, r1 = ring(a), ring(c)
    b.face([tuple(v) for v in r1], axis, "PAL", end); b.face([tuple(v) for v in r0], -axis, "PAL", end)
    for i in range(n):
        j = (i + 1) % n; mid = (r0[i] + r0[j]) / 2 - a
        b.face([tuple(r0[i]), tuple(r0[j]), tuple(r1[j]), tuple(r1[i])], mid, "PAL", bark or TIMBER_L)

def log_stack(b, f, x, y, length=2.4, rows=3, yaw=0.0, r=0.2):
    lf = f.sub(x, y, yaw)
    for row in range(rows):
        n = rows - row + 1
        for k in range(n):
            px = (k - (n - 1) / 2) * r * 2.05
            z = r + row * r * 1.75
            log(b, lf.p(-length / 2, px, z), lf.p(length / 2, px, z), r)

def dummy(b, f, x, y, yaw=0.0):
    """A straw training dummy on a post, arms out, a bucket for a head."""
    df = f.sub(x, y, yaw)
    b.box(df.p(0, 0, 0.8), (0.12, 0.12, 1.6), "PAL", TIMBER, rot_z=df.rot)
    b.lathe(df.p(0, 0, 0.75), [(0.0, 0.0), (0.24, 0.05), (0.3, 0.4), (0.24, 0.75), (0.0, 0.8)], 7, "PAL", HAY)
    bar(b, df.p(-0.6, 0, 1.35), df.p(0.6, 0, 1.35), 0.1, 0.1, "PAL", TIMBER_L)
    b.lathe(df.p(0, 0, 1.55), [(0.14, 0.0), (0.18, 0.28), (0.0, 0.28)], 8, "PAL", "8b8b8b")
    b.box(df.p(0, 0, 1.15), (0.62, 0.62, 0.06), "PAL", RED, rot_z=df.rot)

def disc(b, c, n, side, up, r, col, segs=12):
    pts = [tuple(c + (side * math.cos(2 * math.pi * i / segs) + up * math.sin(2 * math.pi * i / segs)) * r) for i in range(segs)]
    b.face(pts, n, "PAL", col)
    return pts

def target(b, f, x, y, yaw=0.0, r=0.6):
    """An archery target on a stand: a straw boss with coloured rings, facing +y of its frame."""
    tf = f.sub(x, y, yaw)
    for s in (-1, 1):
        bar(b, tf.p(s * 0.45, 0.25, 0.0), tf.p(s * 0.2, 0.05, 1.3), 0.08, 0.08, "PAL", TIMBER)
    bar(b, tf.p(0, -0.6, 0.0), tf.p(0, -0.05, 1.3), 0.08, 0.08, "PAL", TIMBER)
    c = Vector(tf.p(0, 0.12, 1.2)); n = tf.R @ Vector((0, 1, 0)); side = tf.R @ Vector((1, 0, 0)); up = Vector((0, 0, 1))
    front = disc(b, c, n, side, up, r, HAY)
    back = disc(b, c - n * 0.18, -n, side, up, r, HAY)
    for i in range(12):
        j = (i + 1) % 12; mid = Vector(front[i]) + Vector(front[j]) - 2 * c
        b.face([back[i], back[j], front[j], front[i]], mid - n * mid.dot(n), "PAL", HAY)
    for k, (rr, col) in enumerate(((r * 0.82, WHITE), (r * 0.62, RED), (r * 0.42, BLUE_C), (r * 0.2, GOLD))):
        disc(b, c + n * (0.01 + 0.006 * k), n, side, up, rr, col)
    return tf, c, n

def hull(b, f, x, y, length=4.0, beam=1.4, depth=0.7, z=0.0, yaw=0.0, col=None, upturned=False):
    """A simple boat hull, pointed at both ends, along the frame's x. Upturned lays it keel up."""
    hf = f.sub(x, y, yaw)
    col = col or TIMBER_L
    st = [(-0.5, 0.0, 0.25), (-0.4, 0.55, 0.12), (-0.2, 0.92, 0.04), (0.1, 1.0, 0.0), (0.32, 0.85, 0.05), (0.45, 0.45, 0.15), (0.5, 0.0, 0.3)]
    sec = 5
    rings = []
    for u, w, lift in st:
        ring = []
        for k in range(sec):
            a = math.pi * k / (sec - 1)
            px = -math.cos(a) * w * beam / 2
            pz = (1 - math.sin(a) * 0.95) * depth + lift * depth
            if upturned: pz = depth * 1.3 - pz
            ring.append(Vector(hf.p(u * length, px, z + pz)))
        rings.append(ring)
    # a point inside the hull: outer planks face away from it, the inside faces towards it
    inner = Vector(hf.p(0, 0, z + (depth * 1.2 if not upturned else depth * 0.1)))
    for i in range(len(rings) - 1):
        for k in range(sec - 1):
            q = [rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]]
            out = sum(q, Vector()) / 4 - inner
            b.face([tuple(v) for v in q], out, "PAL", col)
            b.face([tuple(v) for v in q], -out, "PAL", TIMBER)
    for k in (0, sec - 1):
        for i in range(len(rings) - 1):
            bar(b, tuple(rings[i][k]), tuple(rings[i + 1][k]), 0.08, 0.08, "PAL", TIMBER)
    return hf

def well(b, f, x, y, roof="SLAT_BROWN"):
    wf = f.sub(x, y, 0)
    b.lathe(wf.p(0, 0, 0), [(0.75, -0.2), (0.75, 0.75), (0.55, 0.75), (0.55, 0.3)], 10, "WALL", cyl_radius=0.75)
    b.lathe(wf.p(0, 0, 0), [(0.55, 0.3), (0.0, 0.3)], 10, "GLASS", "396b7c")
    for s in (-1, 1):
        b.box(wf.p(s * 0.65, 0, 1.3), (0.14, 0.14, 1.8), "PAL", TIMBER, rot_z=wf.rot)
    bar(b, wf.p(-0.8, 0, 1.55), wf.p(0.8, 0, 1.55), 0.1, 0.1, "PAL", TIMBER_L)
    b.box(wf.p(0, 0, 1.0), (0.3, 0.3, 0.3), "PAL", TIMBER_L, rot_z=wf.rot)
    b.box(wf.p(0, 0, 1.3), (0.02, 0.02, 0.5), "PAL", DARK, rot_z=wf.rot)
    for s in (-1, 1):
        lo = [Vector(wf.p(-1.0, s * 0.9, 1.9)), Vector(wf.p(1.0, s * 0.9, 1.9)), Vector(wf.p(1.0, 0, 2.6)), Vector(wf.p(-1.0, 0, 2.6))]
        if (lo[1] - lo[0]).cross(lo[2] - lo[1]).z < 0: lo = lo[::-1]
        nv = (wf.R @ Vector((0, s * 0.6, 0.8))) * 0.12
        fs = b.quad_box(lo, [v + nv for v in lo], roof); b.paint([fs[0]], "PAL", TIMBER)

def round_hut(b, f, x, y, r=2.0, h=2.2, roof="SLAT_BROWN", door_angle=math.pi / 2):
    """A round hut: a plastered wattle wall on stakes and a conical roof with a deep eave."""
    c = f.p(x, y, 0)
    b.lathe(c, [(r + 0.15, -0.6), (r + 0.15, 0.3), (r, 0.3), (r, h)], 12, "STUCCO", cyl_radius=r, tile=(4.0, 4.0))
    for k in range(12):
        a = 2 * math.pi * k / 12
        b.box((c[0] + math.cos(a) * r, c[1] + math.sin(a) * r, h / 2), (0.16, 0.16, h), "PAL", TIMBER, rot_z=a)
    b.lathe(c, [(r, h), (r + 0.6, h)], 12, "PAL", TIMBER)
    b.lathe(c, [(r + 0.6, h), (r * 0.45, h + 1.6), (0.0, h + 2.4)], 12, roof, cyl_radius=r)
    ap = r * math.cos(math.pi / 12)
    k = int(round(door_angle / (2 * math.pi / 12) - 0.5)) % 12
    a = (k + 0.5) * 2 * math.pi / 12
    df = Frame(c[0] + math.cos(a) * ap, c[1] + math.sin(a) * ap, a - math.pi / 2)
    door(b, df, 0.0, 0.0, w=0.8, h=1.7)
    for kk in (k + 3, k - 3):
        a2 = (kk + 0.5) * 2 * math.pi / 12
        wf = Frame(c[0] + math.cos(a2) * ap, c[1] + math.sin(a2) * ap, a2 - math.pi / 2)
        window(b, wf, 0.0, 1.35, 0.0, w=0.45, h=0.55, shutters=None)
    return Vector(c)

def emb(b, ef, z, pts, col, d=0.02, key="PAL"):
    """A flat emblem shape on a signboard: (u, v) points on the board, v up from the board centre z."""
    n = ef.R @ Vector((0, 1, 0))
    b.face([ef.p(u, d, z + v) for u, v in pts], n, key, col)

def emb_circle(b, ef, z, cu, cv, r, col, d=0.02, n=10, key="PAL"):
    emb(b, ef, z, [(cu + math.cos(2 * math.pi * i / n) * r, cv + math.sin(2 * math.pi * i / n) * r) for i in range(n)], col, d, key)

def emb_bar(b, ef, z, a, c, w, col, d=0.02):
    a, c = Vector((a[0], a[1])), Vector((c[0], c[1]))
    t = (c - a).normalized(); s = Vector((-t.y, t.x)) * (w / 2)
    emb(b, ef, z, [tuple(a - s), tuple(c - s), tuple(c + s), tuple(a + s)], col, d)

def torus(b, c, axis, R, r, col, seg=12, tube=6, key="PAL", cols=None):
    """A ring of radius R and tube radius r round axis through c. cols colours alternate segments."""
    c = Vector(c); axis = Vector(axis).normalized()
    u = axis.cross(Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))).normalized(); v = axis.cross(u)
    for i in range(seg):
        for j in range(tube):
            q = []
            for di, dj in ((0, 0), (1, 0), (1, 1), (0, 1)):
                a = 2 * math.pi * (i + di) / seg; t = 2 * math.pi * (j + dj) / tube
                radial = u * math.cos(a) + v * math.sin(a)
                q.append(tuple(c + radial * (R + r * math.cos(t)) + axis * (r * math.sin(t))))
            a = 2 * math.pi * (i + 0.5) / seg; t = 2 * math.pi * (j + 0.5) / tube
            radial = u * math.cos(a) + v * math.sin(a)
            want = radial * math.cos(t) + axis * math.sin(t)
            b.face(q, want, key, (cols[i % len(cols)] if cols else col))

def signpost(b, f, x, y, emblem, yaw=0.0, h=2.0):
    """A sign on its own post, for houses with no flat wall to hang one from."""
    sf = f.sub(x, y, yaw)
    b.box(sf.p(0, 0, h / 2), (0.14, 0.14, h), "PAL", TIMBER, rot_z=sf.rot)
    b.box(sf.p(0, 0, h - 0.45), (0.8, 0.06, 0.52), "PAL", TIMBER_L, rot_z=sf.rot)
    b.box(sf.p(0, 0, h - 0.45), (0.88, 0.04, 0.6), "PAL", TIMBER, rot_z=sf.rot)
    for s in (1, -1):
        emblem(b, sf.sub(0, s * 0.045, 0 if s > 0 else math.pi), h - 0.45)

def basket(b, f, x, y, r=0.35, h=0.32, fill=None, z=0.0):
    c = f.p(x, y, z)
    b.lathe(c, [(r * 0.8, 0.0), (r, h)], 9, "PAL", TIMBER_L)
    b.lathe(c, [(r, h), (r * 0.9, h)], 9, "PAL", TIMBER)
    b.lathe(c, [(r * 0.9, h), (r * 0.72, 0.04)], 9, "PAL", TIMBER)
    b.lathe(c, [(r * 0.72, 0.04), (0.0, 0.04)], 9, "PAL", TIMBER)
    if fill:
        for k in range(7):
            a = k * 2.4; rr = r * 0.55 * (k > 0)
            b.sphere((c[0] + math.cos(a) * rr, c[1] + math.sin(a) * rr, z + h * 0.85), r * 0.28, "PAL", fill[k % len(fill)], seg=5, rings=3)
