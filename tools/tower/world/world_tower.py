# The tower that stands in the main world (docs/world-tower.md; concepts docs/mockups/tower/world/a-islet.jpg and
# c-entrance-plaza.jpg), made in Blender from the command line:
#   blender -b --python tools/tower/world/world_tower.py -- <repo root> [preview] [out=<folder>]
# Reads tools/tower/world/stack.json: one section per floor, bottom first, then the cloud cap that hides the rest.
# Writes one FBX per section into Assets/World/Tower/World/Models (WorldTower_<n>_<style>.fbx, the cap
# WorldTower_Cap.fbx, the islet with the plaza WorldTower_Islet.fbx) and tools/tower/world/stack-built.json (each
# section's base height, radii and triangle counts), which the Unity builder reads. With "preview" it also renders
# pictures into Temp/Blender/world-tower.
#
# Every mesh is flat shaded and carries all it needs in its vertices, for the one shader Ravenfall/WorldTower:
#   color rgb   the face's color (sRGB); for a glowing face the color it glows in
#   color a     how much the face glows when its floor is lit (windows, runes, flames); 0 for stone
#   uv.x        the kind: 0 solid, 1 falling water, 2 cloud, 3 flame, 4 pane (a window or a rune: dark glass until its
#               floor is lit), 5 lightning
#   uv.y        along the flow for water, a phase for clouds, flames and lightning
# Each FBX holds <Part>_Near and <Part>_Far objects (Body, and for some sections Ring, Rocks, Clouds; the islet has
# Islet, Plaza, Egg, and the Flame and Flock that Unity copies about). Far is the low detail model for far away and
# for potato mode. (Not named _LOD0 and _LOD1: Unity would make a LOD group of them, and WorldTower.cs picks itself.)
# Units are metres, z up here (y up in Unity); the gate faces -y here. z = 0 is the sea for the islet and the first
# section, the section's own foot for the others.
import bpy, math, json, os, sys, random
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
PREVIEWS = "preview" in argv
HERE = os.path.join(ROOT, "tools", "tower", "world")
# "out=<folder>" writes the models somewhere else (to look at them before Unity imports them)
OUT = next((x[4:] for x in argv if x.startswith("out=")), os.path.join(ROOT, "Assets", "World", "Tower", "World", "Models"))
PREVIEW = os.path.join(ROOT, "Temp", "Blender", "world-tower")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)
with open(os.path.join(HERE, "stack.json")) as f:
    STACK = json.load(f)

TAU = math.pi * 2
FRONT = -math.pi / 2          # the gate's side
SOLID, WATER, CLOUD, FLAME, PANE, BOLT = 0.0, 1.0, 2.0, 3.0, 4.0, 5.0


# ---- small helpers -----------------------------------------------------------------------------------------------
def lerp(a, b, t): return a + (b - a) * t
def clamp01(t): return 0.0 if t < 0.0 else 1.0 if t > 1.0 else t
def smooth(t):
    t = clamp01(t)
    return t * t * (3 - 2 * t)
def mixc(a, b, t): return (lerp(a[0], b[0], t), lerp(a[1], b[1], t), lerp(a[2], b[2], t))
def mul(c, k): return (min(1.0, max(0.0, c[0] * k)), min(1.0, max(0.0, c[1] * k)), min(1.0, max(0.0, c[2] * k)))
def n3(x, y, z): return noise.noise(Vector((x, y, z)))
def P(r, a, z): return Vector((r * math.cos(a), r * math.sin(a), z))
def adiff(a, b):
    d = (a - b) % TAU
    return min(d, TAU - d)
def jit(c, amount, rnd): return mul(c, 1.0 + rnd.uniform(-amount, amount))


def poly_normal(pts):
    n = Vector((0.0, 0.0, 0.0))
    for i in range(len(pts)):
        a, b = pts[i], pts[(i + 1) % len(pts)]
        n.x += (a.y - b.y) * (a.z + b.z)
        n.y += (a.z - b.z) * (a.x + b.x)
        n.z += (a.x - b.x) * (a.y + b.y)
    return n


class Geo:
    """Faces with their own vertices (flat shading), a color, a glow and a kind each."""
    def __init__(self):
        self.v, self.f, self.c, self.uv = [], [], [], []

    def add(self, pts, col, glow=0.0, kind=SOLID, flow=None, away=None, toward=None, up=None):
        pts = [Vector(p) for p in pts]
        flow = list(flow) if flow is not None else [0.0] * len(pts)
        n = poly_normal(pts)
        if n.length < 1e-9:
            return
        flip = False
        if up is not None:
            flip = (n.z < 0) != (up < 0)
        elif away is not None or toward is not None:
            c = Vector((0.0, 0.0, 0.0))
            for p in pts: c += p
            c /= len(pts)
            d = n.dot(c - Vector(away)) if away is not None else -n.dot(c - Vector(toward))
            flip = d < 0
        if flip:
            pts.reverse()
            flow.reverse()
        i = len(self.v)
        self.v.extend(pts)
        self.f.append(tuple(range(i, i + len(pts))))
        self.c.append((col[0], col[1], col[2], glow))
        self.uv.append([(kind, fl) for fl in flow])

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def to_object(self, name):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata([tuple(p) for p in self.v], [], self.f)
        attr = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        cols, uvs = [], []
        for fi, f in enumerate(self.f):
            for k in range(len(f)):
                cols.extend(self.c[fi])
                uvs.extend(self.uv[fi][k])
        attr.data.foreach_set("color_srgb", cols)
        uvl = mesh.uv_layers.new(name="UVMap")
        uvl.data.foreach_set("uv", uvs)
        mesh.color_attributes.active_color = attr
        mesh.color_attributes.render_color_index = 0
        mesh.materials.append(MATERIAL())
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        return obj


def MATERIAL():
    m = bpy.data.materials.get("Tower")
    if not m:
        m = bpy.data.materials.new("Tower")
        m.diffuse_color = (0.6, 0.6, 0.6, 1.0)
    return m


_ICO = {}
def ico(sub):
    """A unit icosphere: (vertices, triangles)."""
    if sub in _ICO: return _ICO[sub]
    t = (1 + 5 ** 0.5) / 2
    v = [Vector(p).normalized() for p in [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    for _ in range(sub):
        cache, nf = {}, []
        def mid(a, b):
            key = (min(a, b), max(a, b))
            if key not in cache:
                v.append(((v[a] + v[b]) / 2).normalized())
                cache[key] = len(v) - 1
            return cache[key]
        for a, b, c in f:
            ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
            nf += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        f = nf
    _ICO[sub] = (v, f)
    return _ICO[sub]


def blob(g, c, rx, ry, rz, sub, amp, top, bot, seed, kind=SOLID, glow=0.0, phase=0.0, freq=1.6, rnd=None, flat_bottom=None):
    """A lumpy ball: canopies, boulders, cloud puffs, flames. Lighter on top, darker underneath."""
    c = Vector(c)
    verts, faces = ico(sub)
    pts = []
    for n in verts:
        d = 1.0 + amp * n3(n.x * freq + seed, n.y * freq + seed * 0.7, n.z * freq - seed * 0.3)
        p = Vector((c.x + n.x * rx * d, c.y + n.y * ry * d, c.z + n.z * rz * d))
        if flat_bottom is not None and p.z < flat_bottom: p.z = flat_bottom
        pts.append(p)
    for a, b, cc in faces:
        tri = [pts[a], pts[b], pts[cc]]
        # how far up the ball the face sits, in the ball's own proportions
        m = (tri[0] + tri[1] + tri[2]) / 3 - c
        mz, mh = m.z / max(rz, 0.001), math.hypot(m.x / max(rx, 0.001), m.y / max(ry, 0.001))
        t = clamp01(0.5 + 0.5 * mz / max(1e-6, math.hypot(mz, mh)))
        col = mixc(bot, top, t)
        if rnd: col = jit(col, 0.05, rnd)
        g.add(tri, col, glow=glow, kind=kind, flow=[phase] * 3, away=c)


def crag(g, c, size, rnd, top=(0.6, 0.6, 0.59), bot=(0.3, 0.31, 0.33), moss=None, sub=1, stretch=(1.0, 1.0, 1.0)):
    """A boulder: a ball with flat slabs cut off it at random, stretched and turned, so no two look alike."""
    from mathutils import Euler
    c = Vector(c)
    verts, faces = ico(sub)
    pts = [v.copy() for v in verts]
    for _ in range(6):
        u = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-0.6, 1))).normalized()
        k = rnd.uniform(0.45, 0.85)
        for q in pts:
            d = q.dot(u)
            if d > k: q -= u * (d - k)
    turn = Euler((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), rnd.uniform(0, TAU))).to_matrix()
    sx, sy, sz = size * stretch[0] * rnd.uniform(0.8, 1.25), size * stretch[1] * rnd.uniform(0.7, 1.1), size * stretch[2] * rnd.uniform(0.75, 1.15)
    world = [c + turn @ Vector((q.x * sx, q.y * sy, q.z * sz)) for q in pts]
    for a, b, cc in faces:
        tri = [world[a], world[b], world[cc]]
        n = poly_normal(tri)
        if n.length < 1e-9: continue
        n.normalize()
        if n.dot((tri[0] + tri[1] + tri[2]) / 3 - c) < 0: n = -n
        col = mixc(bot, top, clamp01(0.5 + 0.55 * n.z))
        if moss is not None and n.z > 0.72: col = mixc(col, moss, 0.75)
        g.add(tri, jit(col, 0.06, rnd), away=c)


def basis(axis):
    axis = axis.normalized()
    u = axis.orthogonal().normalized()
    return axis, u, axis.cross(u)


def cyl(g, p0, p1, r0, r1, segs, col, cap0=False, cap1=True, glow=0.0, kind=SOLID, phase=0.0, shade=True, turn=0.0):
    """A tapered column from p0 to p1; r1 = 0 makes a cone or a spike."""
    p0, p1 = Vector(p0), Vector(p1)
    axis, u, v = basis(p1 - p0)
    mid = (p0 + p1) / 2
    ring0 = [p0 + (u * math.cos(turn + k * TAU / segs) + v * math.sin(turn + k * TAU / segs)) * r0 for k in range(segs)]
    ring1 = [p1 + (u * math.cos(turn + k * TAU / segs) + v * math.sin(turn + k * TAU / segs)) * r1 for k in range(segs)]
    for k in range(segs):
        a, b = ring0[k], ring0[(k + 1) % segs]
        c2 = mul(col, 0.86 + 0.24 * (0.5 + 0.5 * math.cos(k * TAU / segs))) if shade else col
        if r1 <= 1e-6: g.add([a, b, p1], c2, glow=glow, kind=kind, flow=[phase] * 3, away=mid)
        else: g.add([a, b, ring1[(k + 1) % segs], ring1[k]], c2, glow=glow, kind=kind, flow=[phase] * 4, away=mid)
    if cap0 and r0 > 1e-6: g.add(ring0, mul(col, 0.7), glow=glow, kind=kind, flow=[phase] * segs, away=mid)
    if cap1 and r1 > 1e-6: g.add(ring1, mul(col, 1.1), glow=glow, kind=kind, flow=[phase] * segs, away=mid)


def box(g, lo, hi, col, shade=True, glow=0.0, faces="all"):
    """An axis aligned box."""
    (x0, y0, z0), (x1, y1, z1) = lo, hi
    c = ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2)
    sides = {
        "top": ([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], 1.08),
        "bottom": ([(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0)], 0.7),
        "front": ([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], 0.97),
        "back": ([(x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)], 0.85),
        "left": ([(x0, y0, z0), (x0, y1, z0), (x0, y1, z1), (x0, y0, z1)], 0.88),
        "right": ([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], 0.92),
    }
    for name, (pts, k) in sides.items():
        if faces != "all" and name not in faces: continue
        g.add(pts, mul(col, k) if shade else col, glow=glow, away=c)


class Wall:
    """Things placed on a round wall whose radius at height z is R(z): s runs along the wall (m), d out from it."""
    def __init__(self, g, R):
        self.g, self.R = g, R

    def p(self, a, s, z, d):
        r = self.R(z)
        return P(r + d, a + s / r, z)

    def block(self, a, s0, s1, z0, z1, din, d0, d1=None, col=(0.5, 0.5, 0.5), glow=0.0, back=False, shade=True):
        if d1 is None: d1 = d0
        p = self.p
        ib0, ib1, it0, it1 = p(a, s0, z0, din), p(a, s1, z0, din), p(a, s0, z1, din), p(a, s1, z1, din)
        ob0, ob1, ot0, ot1 = p(a, s0, z0, d0), p(a, s1, z0, d0), p(a, s0, z1, d1), p(a, s1, z1, d1)
        c = (ib0 + ib1 + it0 + it1 + ob0 + ob1 + ot0 + ot1) / 8
        k = (lambda x: mul(col, x)) if shade else (lambda x: col)
        self.g.add([ob0, ob1, ot1, ot0], k(1.0), glow=glow, away=c)
        self.g.add([ib0, ob0, ot0, it0], k(0.86), glow=glow, away=c)
        self.g.add([ib1, ob1, ot1, it1], k(0.92), glow=glow, away=c)
        self.g.add([it0, it1, ot1, ot0], k(1.1), glow=glow, away=c)
        self.g.add([ib0, ib1, ob1, ob0], k(0.68), glow=glow, away=c)
        if back: self.g.add([ib0, ib1, it1, it0], k(0.8), glow=glow, away=c)

    def strokes(self, a, zc, gw, gh, lines, th, tint, bright, d):
        """A rune: lines in a unit square, drawn as flat glowing strips on the wall."""
        for x0, y0, x1, y1 in lines:
            s0, z0, s1, z1 = (x0 - 0.5) * gw, zc + (y0 - 0.5) * gh, (x1 - 0.5) * gw, zc + (y1 - 0.5) * gh
            length = math.hypot(s1 - s0, z1 - z0)
            if length < 1e-6: continue
            ns, nz = -(z1 - z0) / length * th / 2, (s1 - s0) / length * th / 2
            pts = [self.p(a, s0 - ns, z0 - nz, d), self.p(a, s1 - ns, z1 - nz, d), self.p(a, s1 + ns, z1 + nz, d), self.p(a, s0 + ns, z0 + nz, d)]
            self.g.add(pts, tint, glow=bright, kind=PANE, away=(0, 0, zc))

    def window(self, a, zb, w, h, tint, frame, bright, lod):
        """A pointed window: a glowing pane and, close up, its stone frame."""
        d = 0.16 if lod == 0 else 0.5
        hs = h - w * 0.8
        pane = [self.p(a, -w / 2, zb, d), self.p(a, w / 2, zb, d), self.p(a, w / 2, zb + hs, d), self.p(a, 0, zb + h, d), self.p(a, -w / 2, zb + hs, d)]
        self.g.add(pane, tint, glow=bright, kind=PANE, away=(0, 0, zb))
        if lod != 0: return
        fw = max(0.4, w * 0.18)
        self.block(a, -w / 2 - fw, -w / 2, zb, zb + hs, -0.3, 0.6, col=frame)
        self.block(a, w / 2, w / 2 + fw, zb, zb + hs, -0.3, 0.6, col=frame)
        self.block(a, -w / 2 - fw * 1.7, w / 2 + fw * 1.7, zb - fw * 1.3, zb, -0.3, 0.9, col=frame)
        for side in (-1, 1):
            a0, a1 = self.p(a, side * w / 2, zb + hs, 0.6), self.p(a, 0, zb + h, 0.6)
            b0, b1 = self.p(a, side * (w / 2 + fw), zb + hs, 0.6), self.p(a, 0, zb + h + fw * 1.4, 0.6)
            i0, i1 = self.p(a, side * w / 2, zb + hs, -0.3), self.p(a, 0, zb + h, -0.3)
            j0, j1 = self.p(a, side * (w / 2 + fw), zb + hs, -0.3), self.p(a, 0, zb + h + fw * 1.4, -0.3)
            mid = (a0 + a1 + j0 + j1) / 4
            self.g.add([a0, a1, b1, b0], frame, away=(0, 0, zb + h))
            self.g.add([a0, a1, i1, i0], mul(frame, 0.7), away=mid)
            self.g.add([b0, b1, j1, j0], mul(frame, 1.1), away=mid)


def courses(g, R, z0, z1, rows, segs, colfn, rnd, skip=None, d=0.0, cut=None):
    """The wall itself: courses of huge stone blocks, every other course shifted half a block. cut = (half width in
    metres, height) leaves the gate's opening out at the front: the blocks beside it are trimmed to its edge."""
    for j in range(rows):
        za, zb = lerp(z0, z1, j / rows), lerp(z0, z1, (j + 1) / rows)
        off = (0.5 if j % 2 else 0.0) * TAU / segs
        for i in range(segs):
            a0 = off + i * TAU / segs
            a1 = a0 + TAU / segs
            am, zm = (a0 + a1) / 2, (za + zb) / 2
            if skip and skip(am, zm): continue
            pieces = [(a0, a1)]
            if cut and zm < cut[1]:
                ah = cut[0] / R(zm)
                r0 = (a0 - FRONT + math.pi) % TAU - math.pi
                r1 = r0 + (a1 - a0)
                pieces = []
                if r0 < -ah: pieces.append((FRONT + r0, FRONT + min(r1, -ah)))
                if r1 > ah: pieces.append((FRONT + max(r0, ah), FRONT + r1))
            col = colfn(am, zm, rnd)
            for b0, b1 in pieces:
                if b1 - b0 < 1e-4: continue
                g.add([P(R(za) + d, b0, za), P(R(za) + d, b1, za), P(R(zb) + d, b1, zb), P(R(zb) + d, b0, zb)], col, away=(0, 0, zm))


def ledge(g, R, z, out, th, segs, side, top, inn=1.0, skip=None, under=None):
    """A ring standing out from the wall: a terrace, a cornice."""
    r_in, r_out = R(z) - inn, R(z) + out
    for i in range(segs):
        a0, a1 = i * TAU / segs, (i + 1) * TAU / segs
        if skip and skip((a0 + a1) / 2): continue
        g.add([P(r_in, a0, z + th), P(r_out, a0, z + th), P(r_out, a1, z + th), P(r_in, a1, z + th)], top, up=1)
        g.add([P(r_out, a0, z), P(r_out, a1, z), P(r_out, a1, z + th), P(r_out, a0, z + th)], side, away=(0, 0, z))
        g.add([P(r_in, a0, z), P(r_out, a0, z), P(r_out, a1, z), P(r_in, a1, z)], under or mul(side, 0.6), up=-1)


def disc(g, r, z, segs, col, up=1, r_in=0.0):
    for i in range(segs):
        a0, a1 = i * TAU / segs, (i + 1) * TAU / segs
        if r_in <= 0: g.add([(0, 0, z), P(r, a0, z), P(r, a1, z)], col, up=up)
        else: g.add([P(r_in, a0, z), P(r, a0, z), P(r, a1, z), P(r_in, a1, z)], col, up=up)


def ribbon(g, pts, widths, rights, colfn, kind=SOLID, glow=0.0, away=None, v0=0.0, vscale=0.1):
    """A strip along pts (a waterfall, a vine, a bolt); colfn(t) gives the color along it."""
    v = v0
    for i in range(len(pts) - 1):
        a, b = Vector(pts[i]), Vector(pts[i + 1])
        ra, rb = Vector(rights[i]) * widths[i] / 2, Vector(rights[i + 1]) * widths[i + 1] / 2
        v1 = v + (b - a).length * vscale
        g.add([a - ra, a + ra, b + rb, b - rb], colfn((i + 0.5) / (len(pts) - 1)), glow=glow, kind=kind, flow=[v, v, v1, v1],
              away=away if away is not None else (0, 0, (a.z + b.z) / 2))
        v = v1


# ---- runes ---------------------------------------------------------------------------------------------------------
GLYPHS = [
    [(0.3, 0, 0.3, 1), (0.3, 0.62, 0.8, 0.92), (0.3, 0.3, 0.72, 0.56)],
    [(0.2, 0, 0.2, 1), (0.2, 1, 0.8, 0.6), (0.8, 0.6, 0.8, 0)],
    [(0.3, 0, 0.3, 1), (0.3, 0.78, 0.76, 0.52), (0.76, 0.52, 0.3, 0.26)],
    [(0.25, 0, 0.25, 1), (0.25, 1, 0.75, 0.76), (0.75, 0.76, 0.25, 0.5), (0.25, 0.5, 0.8, 0)],
    [(0.75, 1, 0.25, 0.5), (0.25, 0.5, 0.75, 0)],
    [(0.2, 0, 0.8, 1), (0.2, 1, 0.8, 0)],
    [(0.5, 0, 0.5, 1), (0.5, 1, 0.15, 0.64), (0.5, 1, 0.85, 0.64)],
    [(0.5, 1, 0.15, 0.6), (0.15, 0.6, 0.5, 0.2), (0.5, 0.2, 0.85, 0.6), (0.85, 0.6, 0.5, 1), (0.5, 0.2, 0.2, 0), (0.5, 0.2, 0.8, 0)],
    [(0.5, 0, 0.5, 1), (0.5, 0.55, 0.15, 1), (0.5, 0.55, 0.85, 1)],
    [(0.2, 0, 0.2, 1), (0.8, 0, 0.8, 1), (0.2, 1, 0.8, 0), (0.2, 0, 0.8, 1)],
    [(0.2, 0.5, 0.5, 1), (0.5, 1, 0.8, 0.5), (0.8, 0.5, 0.5, 0), (0.5, 0, 0.2, 0.5)],
    [(0.2, 0, 0.2, 1), (0.2, 0.5, 0.8, 1), (0.2, 0.5, 0.8, 0)],
]
def _ring(n, r, cx=0.5, cy=0.5, start=0.0):
    pts = [(cx + r * math.cos(start + k * TAU / n), cy + r * math.sin(start + k * TAU / n)) for k in range(n)]
    return [(pts[k][0], pts[k][1], pts[(k + 1) % n][0], pts[(k + 1) % n][1]) for k in range(n)]
SIGILS = {
    # the sun of the Overgrown Halls: a diamond in a diamond with rays
    "sun": _ring(4, 0.5) + _ring(4, 0.24) + [(0.5 + 0.34 * math.cos(k * TAU / 8 + TAU / 16), 0.5 + 0.34 * math.sin(k * TAU / 8 + TAU / 16), 0.5 + 0.5 * math.cos(k * TAU / 8 + TAU / 16), 0.5 + 0.5 * math.sin(k * TAU / 8 + TAU / 16)) for k in range(8)],
    # the Frozen Vault's snowflake
    "snow": [(0.5 - 0.5 * math.cos(k * TAU / 6), 0.5 - 0.5 * math.sin(k * TAU / 6), 0.5 + 0.5 * math.cos(k * TAU / 6), 0.5 + 0.5 * math.sin(k * TAU / 6)) for k in range(3)]
            + [(0.5 + 0.32 * math.cos(k * TAU / 6), 0.5 + 0.32 * math.sin(k * TAU / 6), 0.5 + 0.46 * math.cos(k * TAU / 6 + s * 0.32), 0.5 + 0.46 * math.sin(k * TAU / 6 + s * 0.32)) for k in range(6) for s in (-1, 1)],
    # the Storm Spire's gale: a square spiral
    "gale": [(0.5, 0.5, 0.5, 0.66), (0.5, 0.66, 0.3, 0.66), (0.3, 0.66, 0.3, 0.3), (0.3, 0.3, 0.74, 0.3), (0.74, 0.3, 0.74, 0.84), (0.74, 0.84, 0.12, 0.84), (0.12, 0.84, 0.12, 0.1), (0.12, 0.1, 0.94, 0.1), (0.94, 0.1, 0.94, 1.0)],
    # the Dark Fortress's trident
    "trident": [(0.5, 0, 0.5, 1), (0.14, 1, 0.14, 0.5), (0.14, 0.5, 0.86, 0.5), (0.86, 0.5, 0.86, 1), (0.3, 0.16, 0.7, 0.16), (0.04, 0.86, 0.14, 1), (0.24, 0.86, 0.14, 1), (0.76, 0.86, 0.86, 1), (0.96, 0.86, 0.86, 1), (0.4, 0.86, 0.5, 1), (0.6, 0.86, 0.5, 1)],
    "crown": [(0.05, 0.1, 0.95, 0.1), (0.05, 0.1, 0.0, 0.8), (0.0, 0.8, 0.27, 0.45), (0.27, 0.45, 0.5, 0.95), (0.5, 0.95, 0.73, 0.45), (0.73, 0.45, 1.0, 0.8), (1.0, 0.8, 0.95, 0.1)],
}


def rune_band(g, R, z0, z1, segs, stone, tint, sigil, glyphs, rnd, lod, out=1.0):
    """A band standing out from the wall with the floor's sigil at the four winds and runes between them."""
    Rb = lambda z: R(z) + out
    for i in range(segs):
        a0, a1 = i * TAU / segs, (i + 1) * TAU / segs
        col = jit(stone, 0.05, rnd)
        g.add([P(Rb(z0), a0, z0), P(Rb(z0), a1, z0), P(Rb(z1), a1, z1), P(Rb(z1), a0, z1)], col, away=(0, 0, z0))
        g.add([P(R(z1) - 0.3, a0, z1 + out * 0.6), P(Rb(z1), a0, z1), P(Rb(z1), a1, z1), P(R(z1) - 0.3, a1, z1 + out * 0.6)], mul(col, 1.15), up=1)
        g.add([P(R(z0) - 0.3, a0, z0 - out * 0.6), P(Rb(z0), a0, z0), P(Rb(z0), a1, z0), P(R(z0) - 0.3, a1, z0 - out * 0.6)], mul(col, 0.6), up=-1)
    W = Wall(g, Rb)
    d = 0.14 if lod == 0 else 0.5
    h = z1 - z0
    zc = (z0 + z1) / 2
    for q in range(4):
        aq = FRONT + q * TAU / 4
        W.strokes(aq, zc, h * 0.84, h * 0.84, SIGILS[sigil], h * 0.075, tint, 1.0, d)
        for k in range(glyphs):
            a = aq + (k + 1.5) / (glyphs + 2) * TAU / 4
            W.strokes(a, zc, h * 0.34, h * 0.56, GLYPHS[rnd.randrange(len(GLYPHS))], h * 0.06, tint, 0.85, d)


# ---- dressing ------------------------------------------------------------------------------------------------------
LEAF_TOP = [(0.42, 0.62, 0.22), (0.36, 0.56, 0.2), (0.48, 0.64, 0.24), (0.33, 0.52, 0.22)]
LEAF_BOT = (0.16, 0.3, 0.14)
TRUNK = (0.36, 0.26, 0.17)

def jungle_tree(g, base, h, rnd, lod):
    base = Vector(base)
    lean = Vector((rnd.uniform(-0.14, 0.14), rnd.uniform(-0.14, 0.14), 1.0)).normalized()
    top = base + lean * h * 0.58
    cyl(g, base - Vector((0, 0, 1)), top, h * 0.055, h * 0.03, 5 if lod == 0 else 3, TRUNK, cap1=False)
    leaf = LEAF_TOP[rnd.randrange(len(LEAF_TOP))]
    for k in range(3 if lod == 0 else 1):
        rr = h * rnd.uniform(0.34, 0.46) * (1 - 0.2 * k)
        c = top + Vector((rnd.uniform(-1, 1) * h * 0.16, rnd.uniform(-1, 1) * h * 0.16, h * 0.04 + k * h * 0.1)) if lod == 0 else top + Vector((0, 0, h * 0.06))
        blob(g, c, rr, rr, rr * (0.46 if lod == 0 else 0.6), 1, 0.25, leaf, LEAF_BOT, rnd.uniform(0, 50), rnd=rnd)


def pine(g, base, h, rnd, lod, snow=True):
    base = Vector(base)
    cyl(g, base - Vector((0, 0, 1)), base + Vector((0, 0, h * 0.3)), h * 0.045, h * 0.035, 5 if lod == 0 else 3, (0.3, 0.22, 0.16), cap1=False)
    tiers = 3 if lod == 0 else 2
    for k in range(tiers):
        z0 = h * (0.18 + 0.26 * k)
        r = h * (0.26 - 0.06 * k)
        green = (0.2, 0.36, 0.26) if not snow else mixc((0.2, 0.36, 0.26), (0.9, 0.95, 1.0), 0.35 + 0.2 * k)
        cyl(g, base + Vector((0, 0, z0)), base + Vector((0, 0, z0 + h * 0.4)), r, 0.0, 6, green, cap0=True, turn=rnd.uniform(0, 1))


def crystal(g, base, r, h, tilt, rnd, col=(0.6, 0.84, 0.96), deep=(0.36, 0.62, 0.86)):
    base = Vector(base)
    axis = (Vector((0, 0, 1)) + Vector(tilt)).normalized()
    p1 = base + axis * h * 0.78
    turn = rnd.uniform(0, 1)
    # the shaft darker at its foot, the point pale
    cyl(g, base - axis * 2.0, p1, r, r * 0.82, 6, mixc(deep, col, 0.6), cap1=False, turn=turn)
    cyl(g, p1, base + axis * h, r * 0.82, 0.0, 6, mixc(col, (1, 1, 1), 0.35), turn=turn)


def vines(g, W, zt, count, lmin, lmax, rnd, lod, skip=None, d=0.4):
    if lod != 0: count //= 3
    for _ in range(count):
        a = rnd.uniform(0, TAU)
        if skip and skip(a): continue
        length = rnd.uniform(lmin, lmax)
        w = rnd.uniform(1.6, 4.2) * (1.8 if lod else 1.0)
        n = 3 if lod == 0 else 1
        col = LEAF_TOP[rnd.randrange(len(LEAF_TOP))]
        drift = rnd.uniform(-1.5, 1.5)
        for k in range(n):
            t0, t1 = k / n, (k + 1) / n
            w0, w1 = w * (1 - 0.8 * t0), w * (1 - 0.8 * t1)
            s0, s1 = drift * t0, drift * t1
            z0, z1 = zt - length * t0, zt - length * t1
            dd = d + (0.25 if lod else 0.0)
            g.add([W.p(a, s0 - w0 / 2, z0, dd), W.p(a, s0 + w0 / 2, z0, dd), W.p(a, s1 + w1 / 2, z1, dd), W.p(a, s1 - w1 / 2, z1, dd)],
                  mixc(col, LEAF_BOT, 0.15 + 0.5 * t0), away=(0, 0, z0))


def waterfall(g, R, a, z_top, z_bot, w, out, rnd, lod, foam=True, ice=False):
    """Water pouring from an arched outlet in the wall, out and down."""
    W = Wall(g, R)
    # the outlet: a dark arch with a stone lip
    W.block(a, -w * 0.62, w * 0.62, z_top - 1.2, z_top, -0.3, 1.6, col=(0.36, 0.36, 0.36))
    g.add([W.p(a, -w / 2, z_top, 0.2), W.p(a, w / 2, z_top, 0.2), W.p(a, w / 2, z_top + w * 0.55, 0.2), W.p(a, 0, z_top + w * 0.95, 0.2), W.p(a, -w / 2, z_top + w * 0.55, 0.2)], (0.07, 0.09, 0.1), away=(0, 0, z_top))
    steps = 7 if lod == 0 else 3
    pts, widths, rights = [], [], []
    tangent = Vector((-math.sin(a), math.cos(a), 0))
    for k in range(steps + 1):
        t = k / steps
        z = lerp(z_top + 0.4, z_bot, t ** 1.25)
        o = 0.5 + out * (1 - (1 - min(1.0, t * 3.0)) ** 2)
        pts.append(P(R(z_top) + o, a, z))
        widths.append(w * (0.9 + 0.5 * t))
        rights.append(tangent)
    if ice:
        ribbon(g, pts, widths, rights, lambda t: mixc((0.66, 0.86, 0.97), (0.86, 0.95, 1.0), t), away=(0, 0, 0))
    else:
        ribbon(g, pts, widths, rights, lambda t: mixc((0.42, 0.72, 0.9), (0.78, 0.92, 0.98), t ** 0.7), kind=WATER, away=(0, 0, 0), v0=rnd.uniform(0, 3))
        if lod == 0:
            pts2 = [p + Vector((math.cos(a), math.sin(a), 0)) * 0.35 for p in pts]
            ribbon(g, pts2, [x * 0.45 for x in widths], rights, lambda t: mixc((0.7, 0.9, 0.98), (0.95, 0.99, 1.0), t), kind=WATER, away=(0, 0, 0), v0=rnd.uniform(0, 3))
    if foam:
        base = pts[-1]
        for k in range(4 if lod == 0 else 2):
            c = base + tangent * rnd.uniform(-w * 0.7, w * 0.7) + Vector((math.cos(a), math.sin(a), 0)) * rnd.uniform(-1, 3)
            rr = rnd.uniform(1.6, 3.2) if not ice else rnd.uniform(2.5, 4.5)
            blob(g, c, rr, rr, rr * 0.6, 1 if lod == 0 else 0, 0.3, (0.97, 0.99, 1.0), (0.75, 0.88, 0.95), rnd.uniform(0, 30))


def icicles(g, R, z, out, count, rnd, lod, lmin=2.5, lmax=9.0):
    if lod != 0: count //= 3
    for _ in range(count):
        a = rnd.uniform(0, TAU)
        length = rnd.uniform(lmin, lmax) * (1.4 if lod else 1.0)
        r = length * rnd.uniform(0.09, 0.14) * (1.5 if lod else 1.0)
        p0 = P(R(z) + out - rnd.uniform(0.2, 1.4), a, z + 0.2)
        cyl(g, p0, p0 - Vector((0, 0, length)), r, 0.0, 4, (0.8, 0.93, 1.0), turn=rnd.uniform(0, 1))


def battlements(g, R, z, out, count, w, h, col, rnd, thick=1.8, jag=0.0, skip=None):
    W = Wall(g, R)
    for i in range(count):
        a = i * TAU / count
        if skip and skip(a): continue
        hh = h * (1 + rnd.uniform(0, jag))
        W.block(a, -w / 2, w / 2, z, z + hh, out - thick, out, col=jit(col, 0.05, rnd), back=True)


def parapet(g, R, z, out, h, segs, col, thick=1.4):
    """A low wall along a ledge's rim."""
    r0, r1 = R(z) + out - thick, R(z) + out
    for i in range(segs):
        a0, a1 = i * TAU / segs, (i + 1) * TAU / segs
        g.add([P(r1, a0, z), P(r1, a1, z), P(r1, a1, z + h), P(r1, a0, z + h)], col, away=(0, 0, z))
        g.add([P(r0, a0, z), P(r0, a1, z), P(r0, a1, z + h), P(r0, a0, z + h)], mul(col, 0.8), toward=(0, 0, z))
        g.add([P(r0, a0, z + h), P(r1, a0, z + h), P(r1, a1, z + h), P(r0, a1, z + h)], mul(col, 1.12), up=1)


def windows_ring(W, z, count, w, h, tint, frame, rnd, lod, skip=None, dark=0.2):
    for i in range(count):
        a = FRONT + (i + 0.5) * TAU / count
        if skip and skip(a): continue
        # some rooms are dim, a few dark: nobody lights every window of a tower
        bright = 0.0 if rnd.random() < dark * 0.4 else rnd.uniform(0.45, 1.0)
        W.window(a, z, w, h, jit(tint, 0.08, rnd), frame, bright, lod)


# ---- the sections ----------------------------------------------------------------------------------------------------
GATE_HALF = 0.2     # radians either side of the front kept clear of windows, buttresses and falls at the foot
WINDOW = (1.0, 0.82, 0.5)

def overgrown(r_seat, h, lod, seed):
    """Floor 1, The Overgrown Halls: mossy stone, vines, trees on the terraces, waterfalls into the sea."""
    rnd = random.Random(seed)
    g = Geo()
    rb, rt = r_seat, r_seat - 6.0
    R = lambda z: rb + (rt - rb) * clamp01(z / h)
    W = Wall(g, R)
    segs = 72 if lod == 0 else 36
    stone, stone2, moss, moss2 = (0.58, 0.57, 0.52), (0.5, 0.49, 0.46), (0.34, 0.5, 0.22), (0.25, 0.4, 0.18)
    def col(a, z, rnd):
        c = mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3, math.sin(a) * 3, z * 0.05))
        m = smooth((n3(math.cos(a) * 2.2 + 5, math.sin(a) * 2.2, z * 0.03) + 0.25 - z / h * 0.45) * 2.2)
        c = mixc(c, moss if rnd.random() < 0.6 else moss2, m * 0.85)
        return jit(c, 0.07, rnd)
    near_gate = lambda a: adiff(a, FRONT) < GATE_HALF
    T = 34.0        # the terrace's height
    # the plinth leans out; the gate's opening is left out of the wall
    gate = (6.7, 25.0)
    Rp = lambda z: R(z) + 4.0 * (1 - clamp01(z / 14.0)) ** 1.2
    courses(g, Rp, -6.0, 14.0, 5 if lod == 0 else 2, segs + (32 if lod == 0 else 0), col, rnd, cut=gate)
    courses(g, R, 14.0, h - 7.0, 14 if lod == 0 else 6, segs, col, rnd, cut=gate)
    # buttresses up to the terrace
    for i in range(16):
        a = FRONT + (i + 0.5) * TAU / 16
        if near_gate(a): continue
        W.block(a, -2.6, 2.6, 2.0, T, -0.5, 6.0, 1.6, col=jit(stone2, 0.05, rnd))
    # the terrace: a wide ledge on corbels, grass on top
    grass = (0.36, 0.54, 0.22)
    ledge(g, R, T, 8.0, 3.0, segs, mixc(stone2, moss2, 0.35), grass)
    if lod == 0:
        for i in range(48):
            W.block(i * TAU / 48, -0.9, 0.9, T - 3.5, T, -0.3, 0.6, 6.0, col=stone2)
    # windows: small ones near the foot say how big the rest is
    frame = mul(stone, 0.8)
    if lod == 0:
        windows_ring(W, 10.0, 150, 1.0, 2.3, WINDOW, frame, rnd, lod, skip=lambda a: adiff(a, FRONT) < 0.22 or adiff(a, FRONT) > 1.5)
    windows_ring(W, 19.0, 52 if lod == 0 else 26, 2.2, 4.8, WINDOW, frame, rnd, lod, skip=lambda a: adiff(a, FRONT) < 0.26)
    windows_ring(W, 43.0, 46 if lod == 0 else 23, 2.6, 6.0, WINDOW, frame, rnd, lod)
    windows_ring(W, 72.0, 42 if lod == 0 else 21, 2.6, 6.0, WINDOW, frame, rnd, lod)
    rune_band(g, R, 54.0, 66.0, segs, (0.3, 0.27, 0.23), (1.0, 0.8, 0.36), "sun", 6, rnd, lod)
    # the top: a cornice, a parapet with merlons, and the roof terrace the next section stands on
    ledge(g, R, h - 7.0, 3.6, 2.6, segs, stone2, mixc(stone, moss, 0.4))
    parapet(g, R, h - 4.4, 3.6, 2.2, segs, stone)
    battlements(g, R, h - 2.2, 3.6, 44 if lod == 0 else 22, 3.4 if lod == 0 else 7.0, 2.4, stone, rnd, thick=1.4)
    disc(g, rt + 2.4, h - 4.4, segs, grass)
    # trees on the terrace and on the roof
    for i in range(20 if lod == 0 else 11):
        a = rnd.uniform(0, TAU)
        jungle_tree(g, P(R(T) + rnd.uniform(2.0, 6.5), a, T + 3.0), rnd.uniform(12, 20), rnd, lod)
    for i in range(30 if lod == 0 else 16):
        a = rnd.uniform(0, TAU)
        jungle_tree(g, P(rt - rnd.uniform(0.5, 9.0), a, h - 4.4), rnd.uniform(13, 23), rnd, lod)
    vines(g, W, T + 0.5, 64, 6, 24, rnd, lod, skip=lambda a: adiff(a, FRONT) < 0.12, d=8.3)
    vines(g, W, T, 44, 8, 26, rnd, lod, skip=near_gate)
    vines(g, W, h - 6.0, 70, 6, 22, rnd, lod, d=3.9)
    vines(g, W, h - 7.5, 44, 8, 24, rnd, lod)
    # waterfalls: from the upper halls onto the terrace, and from the terrace into the sea
    for k, a in enumerate([FRONT + 0.62, FRONT + 2.5, FRONT - 1.9]):
        waterfall(g, R, a, 69.0, T + 3.6, 5.0, 3.5, rnd, lod)
        waterfall(g, lambda z: R(T) + 7.0, a + 0.08, T + 0.2, -1.0, 6.5, 4.0, rnd, lod)
    waterfall(g, R, FRONT - 0.5, 27.0, 4.0, 4.0, 3.0, rnd, lod)
    return {"Body": g}, rt + 2.4


def frost(r_seat, h, lod, seed):
    """Floor 2, The Frozen Vault: blue stone under snow, ice crystals, icicles, snowed-in pines."""
    rnd = random.Random(seed)
    g = Geo()
    rb, rt = r_seat - 15.0, r_seat - 20.0
    R = lambda z: rb + (rt - rb) * clamp01(z / h)
    W = Wall(g, R)
    segs = 56 if lod == 0 else 28
    stone, stone2, ice, snow = (0.5, 0.57, 0.67), (0.42, 0.5, 0.62), (0.68, 0.86, 0.96), (0.94, 0.97, 1.0)
    def col(a, z, rnd):
        c = mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3, math.sin(a) * 3, z * 0.05))
        f = smooth((n3(math.cos(a) * 2.5 + 9, math.sin(a) * 2.5, z * 0.035) + 0.05) * 2.0)
        return jit(mixc(c, ice if rnd.random() < 0.7 else snow, f * 0.75), 0.06, rnd)
    courses(g, R, -2.0, h - 6.0, 12 if lod == 0 else 5, segs, col, rnd)
    for i in range(12):
        a = FRONT + (i + 0.5) * TAU / 12
        W.block(a, -2.2, 2.2, 0.0, 26.0, -0.5, 4.5, 1.2, col=jit(stone2, 0.05, rnd))
    ledge(g, R, 26.0, 5.0, 2.6, segs, stone2, snow)
    frame = mul(stone2, 0.85)
    windows_ring(W, 11.0, 40 if lod == 0 else 20, 2.4, 5.6, WINDOW, frame, rnd, lod)
    windows_ring(W, 32.0, 38 if lod == 0 else 19, 2.4, 5.6, WINDOW, frame, rnd, lod)
    windows_ring(W, 55.0, 36 if lod == 0 else 18, 2.4, 5.6, WINDOW, frame, rnd, lod)
    rune_band(g, R, 41.0, 51.5, segs, (0.2, 0.3, 0.46), (0.55, 0.88, 1.0), "snow", 5, rnd, lod)
    ledge(g, R, h - 6.0, 3.4, 2.4, segs, stone2, snow)
    parapet(g, R, h - 3.6, 3.4, 2.0, segs, mixc(stone, snow, 0.3))
    battlements(g, R, h - 1.6, 3.4, 40 if lod == 0 else 20, 3.0 if lod == 0 else 6.0, 2.2, mixc(stone, snow, 0.5), rnd, thick=1.4)
    disc(g, rt + 2.2, h - 3.6, segs, snow)
    icicles(g, R, 26.0, 5.0, 90, rnd, lod)
    icicles(g, R, h - 6.0, 3.4, 80, rnd, lod)
    icicles(g, lambda z: R(z) + 1.0, 41.0, 0.6, 44, rnd, lod, 2.0, 6.0)
    # the terrace it stands on (the roof of the section below): crystals, drifts and pines
    for i in range(13 if lod == 0 else 8):
        a = i * TAU / (13 if lod == 0 else 8) + rnd.uniform(-0.15, 0.15)
        for k in range(5 if lod == 0 else 2):
            hh = rnd.uniform(10, 34) * (1.0 if k else 1.25)
            out = Vector((math.cos(a), math.sin(a), 0))
            crystal(g, P(rb + rnd.uniform(1.0, 11.0), a + rnd.uniform(-0.09, 0.09), -1.0), hh * rnd.uniform(0.1, 0.15), hh,
                    out * rnd.uniform(0.0, 0.5) + Vector((rnd.uniform(-0.2, 0.2), rnd.uniform(-0.2, 0.2), 0)), rnd)
    for i in range(22 if lod == 0 else 10):
        a = rnd.uniform(0, TAU)
        rr = rnd.uniform(4.0, 8.5)
        blob(g, P(rb + rnd.uniform(1.5, 12.0), a, 0.0), rr, rr, rr * 0.45, 1 if lod == 0 else 0, 0.25, snow, (0.74, 0.84, 0.95), rnd.uniform(0, 30))
    for i in range(16 if lod == 0 else 8):
        pine(g, P(rb + rnd.uniform(3.0, 12.0), rnd.uniform(0, TAU), 0.0), rnd.uniform(11, 19), rnd, lod)
    for i in range(14 if lod == 0 else 7):
        pine(g, P(rt - rnd.uniform(0.5, 9.0), rnd.uniform(0, TAU), h - 3.6), rnd.uniform(10, 17), rnd, lod)
    # frozen falls
    for a in (FRONT - 0.7, FRONT + 1.4, FRONT + 3.3):
        waterfall(g, R, a, 40.0, 1.0, 5.5, 2.5, rnd, lod, ice=True)
    return {"Body": g}, rt + 2.2


def storm(r_seat, h, lod, seed):
    """Floor 3, The Storm Spire: weathered dark stone in a ring of thundercloud, rocks adrift round it."""
    rnd = random.Random(seed)
    g = Geo()
    rb, rt = r_seat - 13.0, r_seat - 17.0
    R = lambda z: rb + (rt - rb) * clamp01(z / h)
    W = Wall(g, R)
    segs = 48 if lod == 0 else 24
    stone, stone2 = (0.42, 0.43, 0.5), (0.33, 0.34, 0.42)
    def col(a, z, rnd):
        c = mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3.4, math.sin(a) * 3.4, z * 0.06))
        return jit(c, 0.08, rnd)
    courses(g, R, -2.0, h - 6.0, 11 if lod == 0 else 5, segs, col, rnd)
    broken = lambda a: n3(math.cos(a) * 2.0, math.sin(a) * 2.0, 4.2) > 0.28
    ledge(g, R, 20.0, 4.0, 2.4, segs, stone2, stone, skip=broken)
    frame = mul(stone2, 0.8)
    tint = (0.92, 0.86, 1.0)
    windows_ring(W, 8.0, 32 if lod == 0 else 16, 2.3, 5.4, WINDOW, frame, rnd, lod)
    windows_ring(W, 27.0, 30 if lod == 0 else 15, 2.3, 5.4, WINDOW, frame, rnd, lod)
    windows_ring(W, 48.5, 28 if lod == 0 else 14, 2.3, 5.4, WINDOW, frame, rnd, lod)
    rune_band(g, R, 36.0, 45.5, segs, (0.22, 0.2, 0.32), tint, "gale", 4, rnd, lod)
    ledge(g, R, h - 6.0, 3.2, 2.4, segs, stone2, stone)
    battlements(g, R, h - 3.6, 3.2, 32 if lod == 0 else 16, 3.2 if lod == 0 else 6.0, 3.4, stone, rnd, thick=1.6, jag=0.5, skip=broken)
    disc(g, rt + 2.0, h - 3.6, segs, stone2)
    # lightning rods: long iron spikes reaching out and up
    for i in range(8):
        a = FRONT + (i + 0.5) * TAU / 8
        p0 = P(R(50) - 1.0, a, 50.0)
        d = (Vector((math.cos(a), math.sin(a), 0)) * 0.8 + Vector((0, 0, 0.6))).normalized()
        cyl(g, p0, p0 + d * 24.0, 1.1, 0.0, 5, (0.2, 0.2, 0.25))
        if lod == 0: cyl(g, p0 + d * 3, p0 + d * 4.2, 2.0, 2.0, 6, (0.3, 0.28, 0.24), cap0=True)
    # the cloud ring and its lightning, turning slowly (its own object)
    ring = Geo()
    n = 20 if lod == 0 else 12
    for i in range(n):
        a = i * TAU / n + rnd.uniform(-0.1, 0.1)
        r = rnd.uniform(rb + 13, rb + 36)
        z = rnd.uniform(14, 34)
        rr = rnd.uniform(14, 23)
        blob(ring, P(r, a, z), rr, rr, rr * 0.55, 2 if lod == 0 else 1, 0.3, (0.5, 0.5, 0.6), (0.2, 0.2, 0.27), rnd.uniform(0, 50), kind=CLOUD, phase=rnd.uniform(0, 1))
        if i % 2 == 0:
            rr2 = rr * 0.7
            blob(ring, P(r - rr * 0.7, a + 0.12, z + rr * 0.3), rr2, rr2, rr2 * 0.6, 1, 0.3, (0.58, 0.58, 0.68), (0.26, 0.26, 0.34), rnd.uniform(0, 50), kind=CLOUD, phase=rnd.uniform(0, 1))
    for i in range(7 if lod == 0 else 4):
        a = rnd.uniform(0, TAU)
        r0 = rb + rnd.uniform(16, 28)
        pts = [P(r0, a, 22.0)]
        for k in range(5):
            last = pts[-1]
            pts.append(last + Vector((rnd.uniform(-3.5, 3.5), rnd.uniform(-3.5, 3.5), -rnd.uniform(5, 9))))
        tangent = Vector((-math.sin(a), math.cos(a), 0))
        ph = rnd.uniform(0, 1)
        widths = [1.5, 1.3, 1.1, 0.9, 0.7, 0.3]
        for sideways in (tangent, Vector((math.cos(a), math.sin(a), 0))):
            v = 0.0
            for k in range(len(pts) - 1):
                a0, b0 = pts[k], pts[k + 1]
                ra, rbv = sideways * widths[k] / 2, sideways * widths[k + 1] / 2
                ring.add([a0 - ra, a0 + ra, b0 + rbv, b0 - rbv], (0.86, 0.8, 1.0), glow=1.0, kind=BOLT, flow=[ph] * 4, away=(0, 0, a0.z))
                ring.add([a0 - ra, b0 - rbv, b0 + rbv, a0 + ra], (0.86, 0.8, 1.0), glow=1.0, kind=BOLT, flow=[ph] * 4, toward=(0, 0, a0.z))
    # rocks adrift (their own object: they bob)
    rocks = Geo()
    for i in range(12 if lod == 0 else 8):
        a = rnd.uniform(0, TAU)
        r = rnd.uniform(rb + 12, rb + 56)
        z = rnd.uniform(-10, 58)
        s = rnd.uniform(3.0, 8.5)
        c = P(r, a, z)
        crag(rocks, c, s, rnd, top=(0.52, 0.51, 0.55), bot=(0.22, 0.21, 0.26), stretch=(1.0, 1.0, rnd.uniform(1.1, 1.7)))
    return {"Body": g, "Ring": ring, "Rocks": rocks}, rt + 2.0


def fortress(r_seat, h, lod, seed):
    """Floor 4, the Dark Fortress: black stone flaring out over the storm, spikes, turrets and green fire."""
    rnd = random.Random(seed)
    g = Geo()
    r0 = r_seat - 3.0                 # where it grows out of the roof below
    rw = r_seat + 8.0                 # the fortress wall, wider than what carries it
    def R(z):
        if z < 16.0: return lerp(r0, rw, smooth(z / 16.0))
        return rw - 2.0 * clamp01((z - 16.0) / (h - 16.0))
    W = Wall(g, R)
    segs = 48 if lod == 0 else 24
    stone, stone2, edge = (0.15, 0.15, 0.19), (0.1, 0.1, 0.13), (0.24, 0.24, 0.3)
    green = (0.5, 1.0, 0.36)
    def col(a, z, rnd):
        return jit(mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3.4, math.sin(a) * 3.4, z * 0.06)), 0.1, rnd)
    courses(g, R, -2.0, 16.0, 4 if lod == 0 else 2, segs, col, rnd)
    courses(g, R, 16.0, 64.0, 9 if lod == 0 else 4, segs, col, rnd)
    # machicolations under the flare, and spikes thrusting out of it
    if lod == 0:
        for i in range(36):
            W.block(i * TAU / 36, -1.0, 1.0, 9.0, 16.0, -0.4, 0.2, 2.2, col=edge)
    for i in range(18 if lod == 0 else 9):
        a = (i + 0.5) * TAU / (18 if lod == 0 else 9)
        p0 = P(R(14) - 1.5, a, 14.0)
        d = (Vector((math.cos(a), math.sin(a), 0)) + Vector((0, 0, -0.25 + 0.5 * (i % 2)))).normalized()
        cyl(g, p0, p0 + d * rnd.uniform(12, 20), 1.8, 0.0, 4, stone2, turn=0.5)
    frame = edge
    windows_ring(W, 19.0, 36 if lod == 0 else 18, 1.8, 9.0, green, frame, rnd, lod, dark=0.1)
    windows_ring(W, 35.0, 36 if lod == 0 else 18, 1.8, 7.0, green, frame, rnd, lod, dark=0.1)
    rune_band(g, R, 48.0, 59.0, segs, (0.07, 0.08, 0.09), green, "trident", 4, rnd, lod)
    # the crown: a ledge of jagged teeth
    ledge(g, R, 64.0, 4.0, 2.6, segs, edge, stone)
    battlements(g, R, 66.6, 4.0, 44 if lod == 0 else 22, 2.6 if lod == 0 else 5.0, 5.0, stone, rnd, thick=1.6, jag=1.2)
    disc(g, R(66) + 2.6, 66.6, segs, stone2)
    # turrets round the crown, each with a spire; green fire between them
    turrets = 8
    for i in range(turrets):
        a = FRONT + (i + 0.5) * TAU / turrets
        c = P(R(50) + 2.5, a, 0)
        cyl(g, c + Vector((0, 0, 42)), c + Vector((0, 0, 80)), 5.2, 5.6, 8, stone, cap1=True)
        cyl(g, c + Vector((0, 0, 34)), c + Vector((0, 0, 42)), 1.0, 5.2, 8, stone2, cap1=False)
        cyl(g, c + Vector((0, 0, 80)), c + Vector((0, 0, 83)), 6.6, 6.6, 8, edge, cap0=True)
        cyl(g, c + Vector((0, 0, 83)), c + Vector((0, 0, 83 + rnd.uniform(20, 30))), 6.0, 0.0, 8, stone2)
        # the turret's own slit windows, drawn round its own middle
        for k in range(3):
            aa = a + (k - 1) * 0.9
            base = c + Vector((math.cos(aa), math.sin(aa), 0)) * 5.75
            t = Vector((-math.sin(aa), math.cos(aa), 0))
            pane = [base + t * -0.7 + Vector((0, 0, 66)), base + t * 0.7 + Vector((0, 0, 66)), base + t * 0.7 + Vector((0, 0, 73)), base + Vector((0, 0, 75)), base + t * -0.7 + Vector((0, 0, 73))]
            g.add(pane, green, glow=rnd.uniform(0.6, 1.0), kind=PANE, away=c + Vector((0, 0, 70)))
        # fire in an iron bowl between this turret and the next
        af = a + TAU / turrets / 2
        b = P(R(66) + 1.0, af, 69.2)
        if i % 2 == 0:
            cyl(g, b - Vector((0, 0, 2.6)), b, 1.2, 3.6, 6, edge, cap0=True)
            blob(g, b + Vector((0, 0, 4.5)), 3.2, 3.2, 6.5, 1, 0.35, (0.75, 1.0, 0.5), (0.3, 0.95, 0.25), rnd.uniform(0, 50), kind=FLAME, glow=1.0, phase=rnd.uniform(0, 1))
            if lod == 0: blob(g, b + Vector((0, 0, 3.5)), 1.8, 1.8, 3.8, 1, 0.3, (0.92, 1.0, 0.8), (0.6, 1.0, 0.45), rnd.uniform(0, 50), kind=FLAME, glow=1.0, phase=rnd.uniform(0, 1))
    # the keep inside the crown, rising into the cloud
    Rk = lambda z: R(66) - 14.0 - 2.0 * clamp01((z - 66) / 30)
    courses(g, Rk, 66.6, h + 6.0, 4 if lod == 0 else 2, segs // 2, col, rnd)
    Wk = Wall(g, Rk)
    windows_ring(Wk, 71.0, 16 if lod == 0 else 8, 2.0, 9.0, green, frame, rnd, lod, dark=0.1)
    for i in range(12 if lod == 0 else 6):
        a = (i + 0.5) * TAU / (12 if lod == 0 else 6)
        p0 = P(Rk(82) - 1.0, a, 82.0)
        d = (Vector((math.cos(a), math.sin(a), 0)) * 0.55 + Vector((0, 0, 1.0))).normalized()
        cyl(g, p0, p0 + d * rnd.uniform(14, 24), 2.0, 0.0, 4, stone2)
    return {"Body": g}, Rk(h) + 4.0


def unbuilt(r_seat, h, lod, seed):
    """A floor nobody has built a look for yet: plain dark stone, a rune band, windows that stay dark until lit."""
    rnd = random.Random(seed)
    g = Geo()
    rb, rt = max(STACK["minRadius"], r_seat - 6.0), max(STACK["minRadius"] - 2.0, r_seat - 9.0)
    R = lambda z: rb + (rt - rb) * clamp01(z / h)
    W = Wall(g, R)
    segs = 40 if lod == 0 else 20
    stone, stone2 = (0.32, 0.32, 0.36), (0.26, 0.26, 0.3)
    col = lambda a, z, rnd: jit(mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3, math.sin(a) * 3, z * 0.06)), 0.07, rnd)
    courses(g, R, -2.0, h, max(3, int(h / 6)) if lod == 0 else max(2, int(h / 14)), segs, col, rnd)
    windows_ring(W, h * 0.2, 24 if lod == 0 else 12, 2.4, 6.0, WINDOW, stone2, rnd, lod)
    windows_ring(W, h * 0.75, 24 if lod == 0 else 12, 2.4, 6.0, WINDOW, stone2, rnd, lod)
    rune_band(g, R, h * 0.48, h * 0.48 + 10.0, segs, (0.16, 0.16, 0.2), (0.9, 0.9, 1.0), "sun", 4, rnd, lod)
    ledge(g, R, h - 4.0, 3.0, 2.2, segs, stone2, stone)
    disc(g, rt + 1.8, h - 1.8, segs, stone2)
    return {"Body": g}, rt + 1.8


def cap(r_seat, h, lod, seed):
    """Above the last built section: the tower goes on into cloud, so it has no top."""
    rnd = random.Random(seed)
    g = Geo()
    rb = max(24.0, r_seat - 5.0)
    R = lambda z: rb - 4.0 * clamp01(z / h)
    stone, stone2 = (0.3, 0.3, 0.35), (0.24, 0.24, 0.29)
    col = lambda a, z, rnd: jit(mixc(stone, stone2, 0.5 + 0.5 * n3(math.cos(a) * 3, math.sin(a) * 3, z * 0.06)), 0.07, rnd)
    courses(g, R, -2.0, 60.0, 6 if lod == 0 else 3, 32 if lod == 0 else 16, col, rnd)
    clouds = Geo()
    white, under, grey, grey_under = (1.0, 1.0, 1.0), (0.74, 0.81, 0.94), (0.86, 0.87, 0.93), (0.56, 0.59, 0.72)
    # one wide cumulus with a flat floor, heaped over the tower: wide enough to be a cloud bank and not a mushroom, and
    # uneven round its rim so no two sides look the same
    def reach(a): return 1.0 + 0.22 * n3(math.cos(a) * 1.2, math.sin(a) * 1.2, 5.5) + 0.12 * n3(math.cos(a) * 2.7, math.sin(a) * 2.7, 1.5)
    def puff(r, a, z, rr, t, squash=0.5):
        blob(clouds, P(r, a, z), rr * rnd.uniform(1.0, 1.3), rr * rnd.uniform(1.0, 1.3), rr * squash, 2 if lod == 0 else 1, 0.3, mixc(grey, white, t), mixc(grey_under, under, t), rnd.uniform(0, 80), kind=CLOUD, phase=rnd.uniform(0, 1), freq=1.3, flat_bottom=z0)
    z0 = 34.0      # the cloud's floor: the crown below stays in the clear
    # the floor: two rings of flat puffs, their undersides level
    n = 14 if lod == 0 else 9
    for i in range(n):
        a = i * TAU / n + rnd.uniform(-0.12, 0.12)
        puff(rnd.uniform(140, 185) * reach(a), a, z0 + rnd.uniform(4, 10), rnd.uniform(36, 50), rnd.uniform(0.45, 0.8), 0.46)
    n = 10 if lod == 0 else 7
    for i in range(n):
        a = i * TAU / n + rnd.uniform(-0.15, 0.15)
        puff(rnd.uniform(74, 112) * reach(a), a, z0 + rnd.uniform(4, 12), rnd.uniform(40, 54), rnd.uniform(0.5, 0.85), 0.5)
    # round the tower itself, so the shaft goes into cloud whichever way it is seen
    n = 6 if lod == 0 else 5
    for i in range(n):
        a = i * TAU / n + rnd.uniform(-0.2, 0.2)
        puff(rb + 14, a, z0 + rnd.uniform(8, 16), rnd.uniform(30, 38), rnd.uniform(0.5, 0.85), 0.45)
    # the heaped top, whiter
    n = 9 if lod == 0 else 6
    for i in range(n):
        a = i * TAU / n + rnd.uniform(-0.2, 0.2)
        puff(rnd.uniform(40, 120) * reach(a), a, z0 + rnd.uniform(26, 40), rnd.uniform(38, 52), rnd.uniform(0.9, 1.0), 0.62)
    for i in range(4):
        a = i * TAU / 4 + rnd.uniform(-0.3, 0.3)
        puff(rnd.uniform(10, 50), a, z0 + rnd.uniform(52, 66), rnd.uniform(40, 50), 1.0, 0.62)
    return {"Body": g, "Clouds": clouds}, 0.0


STYLES = {"overgrown": overgrown, "frost": frost, "storm": storm, "fortress": fortress, "unbuilt": unbuilt, "cloud": cap}


# ---- the islet and the entrance plaza --------------------------------------------------------------------------------
R0 = float(STACK["baseRadius"])
PLAZA_Z = 5.0
SILL_Z = 8.0
PLAZA_R = 21.0
PLAZA_OUT = R0 + 15.0 + PLAZA_R      # the plaza's middle, out from the tower's middle toward the front
BASTION = PLAZA_R + 4.0              # the round wall that carries the plaza
def G(x, out, z): return Vector((x, -out, z))       # x to the right seen from the plaza, out toward the sea


def shore(a):
    """How far out the sea begins, by direction."""
    s = R0 + 32.0 + 9.0 * n3(math.cos(a) * 1.3, math.sin(a) * 1.3, 3.1) + 4.0 * n3(math.cos(a) * 3.1, math.sin(a) * 3.1, 7.7)
    return s + 26.0 * smooth(1.0 - adiff(a, FRONT) / 0.5)


def ground(a, r):
    sh = shore(a)
    x, y = r * math.cos(a), r * math.sin(a)
    if r >= sh:
        z = -0.4 - (r - sh) * 0.42 - 1.5 * (0.5 + 0.5 * n3(x * 0.05, y * 0.05, 2.0)) * min(1.0, (r - sh) / 10.0)
        z = max(z, -30.0)
    else:
        t = (sh - r) / max(1.0, sh - (R0 - 2.0))
        rough = 0.55 + 0.6 * (0.5 + 0.5 * n3(math.cos(a) * 4.0, math.sin(a) * 4.0, r * 0.06))
        z = 0.2 + 30.0 * (t ** 1.6) * rough + 2.5 * n3(x * 0.09, y * 0.09, 1.7) * min(1.0, t * 4.0)
        z = max(0.15, z)
    # the plaza's flat: round the plaza and along the way to the gate
    t = clamp01((-y - R0) / (PLAZA_OUT - R0))
    dist = math.hypot(x, -y - lerp(R0, PLAZA_OUT, t)) if -y > 0 else 1e9
    flat = smooth(1.0 - (dist - (BASTION - 1.0)) / 5.0)
    return lerp(z, PLAZA_Z - 0.15, flat), flat


def islet(lod, seed):
    rnd = random.Random(seed)
    g = Geo()
    segs = 144 if lod == 0 else 72
    radii = [R0 + k for k in ((-4, 2, 7, 13, 19, 25, 31, 37, 43, 49, 56, 64, 72, 82, 96, 116) if lod == 0 else (-4, 3, 10, 19, 28, 37, 47, 58, 70, 86, 116))]
    grid = []
    for ri, r in enumerate(radii):
        row = []
        for i in range(segs):
            a = i * TAU / segs
            z, flat = ground(a, r)
            # a little disorder off the plaza, so the rock is not a lathe's work
            k = (1 - flat) * (0.0 if ri == 0 else 1.0)
            aj = a + k * rnd.uniform(-0.3, 0.3) * TAU / segs
            rj = r + k * rnd.uniform(-1.6, 1.6)
            row.append((P(rj, aj, z + k * rnd.uniform(-0.5, 0.9) * (1.0 if z > 0.5 else 0.3)), flat))
        grid.append(row)
    rock, rock2, grass, grass2, sand, wet, deep = (0.52, 0.52, 0.52), (0.42, 0.42, 0.44), (0.38, 0.55, 0.24), (0.3, 0.47, 0.2), (0.76, 0.7, 0.5), (0.3, 0.32, 0.33), (0.16, 0.25, 0.3)
    def color(tri, flat):
        n = poly_normal(tri).normalized()
        zc = (tri[0].z + tri[1].z + tri[2].z) / 3
        nz = abs(n.z)
        if zc < -0.7: return mixc(wet, deep, clamp01(-zc / 8.0))
        if zc < 0.7: return jit(sand if nz > 0.93 else wet, 0.05, rnd)
        if flat > 0.6: return jit(mixc(grass, (0.5, 0.48, 0.34), 0.35), 0.05, rnd)
        if nz > 0.82: return jit(grass if rnd.random() < 0.6 else grass2, 0.06, rnd)
        strata = 0.5 + 0.5 * n3(zc * 0.35, tri[0].x * 0.02, tri[0].y * 0.02)
        c = mixc(rock, rock2, strata)
        if nz > 0.62: c = mixc(c, grass2, 0.45)
        return jit(c, 0.07, rnd)
    for ri in range(len(radii) - 1):
        for i in range(segs):
            (a, fa), (b, fb) = grid[ri][i], grid[ri][(i + 1) % segs]
            (c, fc), (d, fd) = grid[ri + 1][(i + 1) % segs], grid[ri + 1][i]
            flat = (fa + fb + fc + fd) / 4
            tris = ([a, d, c], [a, c, b]) if (i + ri) % 2 else ([a, d, b], [b, d, c])
            for tri in tris:
                g.add(tri, color(tri, flat), up=1)
    # boulders along the shore and against the wall, stacks standing off in the sea
    def boulder(c, s, tall=1.0, sub=1):
        crag(g, c, s, rnd, moss=(0.36, 0.5, 0.24) if Vector(c).z > 2.0 and rnd.random() < 0.6 else None, stretch=(1.0, 1.0, tall))
    for i in range(60 if lod == 0 else 26):
        a = rnd.uniform(0, TAU)
        if adiff(a, FRONT) < 0.34: continue
        s = rnd.uniform(2.5, 8.0)
        boulder(P(shore(a) + rnd.uniform(-9, 9), a, rnd.uniform(-1.5, 1.0)), s)
    for i in range(22 if lod == 0 else 12):
        a = rnd.uniform(0, TAU)
        if adiff(a, FRONT) < 0.36: continue
        s = rnd.uniform(7, 14)
        boulder(P(R0 + rnd.uniform(2, 14), a, rnd.uniform(4, 16)), s, tall=1.5)
    for i in range(9 if lod == 0 else 6):
        a = rnd.uniform(0, TAU)
        if adiff(a, FRONT) < 0.5: continue
        s = rnd.uniform(5, 9)
        boulder(P(shore(a) + rnd.uniform(14, 36), a, rnd.uniform(1, 4)), s, tall=rnd.uniform(1.8, 3.2))
    # rocks at the bastion's foot, clear of the quay
    n = 22 if lod == 0 else 12
    for k in range(n):
        t = -math.pi / 2 + (k + 0.5) / n * TAU
        if adiff(t, -math.pi / 2) < 0.42 or adiff(t, math.pi / 2) < 0.75: continue
        rr = BASTION + rnd.uniform(2.0, 7.0)
        boulder(G(rr * math.cos(t), PLAZA_OUT - rr * math.sin(t), rnd.uniform(-0.5, 2.5)), rnd.uniform(3.5, 6.5), tall=rnd.uniform(0.9, 1.5))
    return g


def plaza(lod, seed):
    """The entrance: the gate and its tunnel of light, the stair, the round paved plaza with its compass, the low wall,
    the quay's stone steps, the leaderboard stone and the statue's pedestal. Its glow is always on."""
    rnd = random.Random(seed)
    g = Geo()
    stone, stone2, dark, pale = (0.62, 0.6, 0.55), (0.52, 0.5, 0.47), (0.36, 0.34, 0.32), (0.78, 0.75, 0.68)
    gold = (1.0, 0.8, 0.36)
    FACE = R0 + 5.0             # how far out the gate's face stands
    BACK = R0 - 6.0
    HALF, SPRING, RIN, ROUT = 6.5, 15.5, 6.5, 10.0
    # pilasters with a foot and a head
    for side in (-1, 1):
        x0, x1 = side * HALF, side * (HALF + 5.0)
        lo, hi = min(x0, x1), max(x0, x1)
        box(g, (lo, -FACE, PLAZA_Z - 6), (hi, -BACK, 27.0), jit(stone, 0.03, rnd))
        box(g, (lo - 0.6, -FACE - 0.7, PLAZA_Z - 6), (hi + 0.6, -BACK, SILL_Z + 1.0), stone2)
        box(g, (lo - 0.5, -FACE - 0.6, 24.6), (hi + 0.5, -BACK, 27.0), stone2)
        if lod == 0:
            for k in range(4):
                box(g, (lo + 0.5, -FACE - 0.25, 10.0 + k * 3.2), (hi - 0.5, -FACE + 0.1, 12.4 + k * 3.2), mul(stone, 0.9 + 0.05 * (k % 2)))
    # the arch's stones
    n = 11
    for k in range(n):
        t0, t1 = math.pi * k / n, math.pi * (k + 1) / n
        key = k == n // 2
        ro = ROUT + (1.4 if key else 0.0)
        face = FACE + (0.7 if key else 0.0)
        c = jit(pale if k % 2 == 0 else stone, 0.03, rnd)
        q = lambda r, t, out: G(r * math.cos(t), out, SPRING + r * math.sin(t))
        a, b, c2, d = q(RIN, t0, face), q(RIN, t1, face), q(ro, t1, face), q(ro, t0, face)
        mid = (q(RIN, t0, BACK) + q(ro, t1, face)) / 2
        g.add([a, b, c2, d], c, away=mid)
        g.add([q(RIN, t0, face), q(RIN, t1, face), q(RIN, t1, BACK), q(RIN, t0, BACK)], mul(c, 0.7), away=mid)
        g.add([q(ro, t0, face), q(ro, t1, face), q(ro, t1, BACK), q(ro, t0, BACK)], mul(c, 1.1), away=mid)
        if key:
            g.add([q(RIN, t0, face), q(ro, t0, face), q(ro, t0, FACE), q(RIN, t0, FACE)], mul(c, 0.85), away=mid)
            g.add([q(RIN, t1, face), q(ro, t1, face), q(ro, t1, FACE), q(RIN, t1, FACE)], mul(c, 0.85), away=mid)
    # the wall between the arch and the lintel
    steps = 12
    for k in range(steps):
        t0, t1 = math.pi * k / steps, math.pi * (k + 1) / steps
        r = ROUT - 1.0
        x0, x1 = r * math.cos(t0), r * math.cos(t1)
        g.add([G(x0, FACE - 0.6, SPRING + r * math.sin(t0)), G(x1, FACE - 0.6, SPRING + r * math.sin(t1)), G(x1, FACE - 0.6, 27.0), G(x0, FACE - 0.6, 27.0)], stone2, away=(0, 0, 20))
    # the lintel with the tower's sun, and a cornice over it
    box(g, (-HALF - 5.6, -FACE - 0.5, 27.0), (HALF + 5.6, -BACK, 33.0), stone)
    box(g, (-HALF - 6.6, -FACE - 1.4, 33.0), (HALF + 6.6, -BACK, 34.6), stone2)
    for x0, z0, x1, z1 in SIGILS["sun"]:
        s = 5.0
        ax, az, bx, bz = (x0 - 0.5) * s, 30.0 + (z0 - 0.5) * s, (x1 - 0.5) * s, 30.0 + (z1 - 0.5) * s
        length = math.hypot(bx - ax, bz - az)
        nx, nz = -(bz - az) / length * 0.22, (bx - ax) / length * 0.22
        g.add([G(ax - nx, FACE + 0.62, az - nz), G(bx - nx, FACE + 0.62, bz - nz), G(bx + nx, FACE + 0.62, bz + nz), G(ax + nx, FACE + 0.62, az + nz)], gold, glow=1.0, kind=PANE, away=(0, 0, 30))
    # the tunnel: light at its end, warmer the deeper it goes
    prof = [(-HALF, SILL_Z), (HALF, SILL_Z)] + [(RIN * math.cos(t), SPRING + RIN * math.sin(t)) for t in [math.pi * k / 10 for k in range(11)]]
    depth0, depth1, slices = FACE - 0.5, R0 - 24.0, 5
    axis_mid = lambda out: G(0, out, 13.0)
    warm = (1.0, 0.8, 0.46)
    for s in range(slices):
        o0, o1 = lerp(depth0, depth1, s / slices), lerp(depth0, depth1, (s + 1) / slices)
        t = (s + 0.5) / slices
        for k in range(len(prof)):
            (x0, z0), (x1, z1) = prof[k], prof[(k + 1) % len(prof)]
            floor = k == 0
            c = mixc(stone2 if not floor else stone, warm, 0.25 + 0.6 * t)
            g.add([G(x0, o0, z0), G(x1, o0, z1), G(x1, o1, z1), G(x0, o1, z0)], c, glow=0.06 + 0.5 * t ** 1.5, toward=axis_mid((o0 + o1) / 2))
    g.add([G(x, depth1, z) for x, z in prof], (1.0, 0.88, 0.6), glow=1.0, kind=PANE, toward=axis_mid(depth0))
    # the stair down to the plaza, between two cheeks
    n_steps, tread = 6, 1.3
    rise = (SILL_Z - PLAZA_Z) / n_steps
    for k in range(n_steps):
        box(g, (-9.0, -(FACE + tread * (n_steps - k)), PLAZA_Z - 1.0), (9.0, -(FACE - 1.0), PLAZA_Z + rise * (k + 1)), jit(stone, 0.03, rnd), faces=("top", "front", "left", "right"))
    for side in (-1, 1):
        lo, hi = (9.0, 11.2) if side > 0 else (-11.2, -9.0)
        box(g, (lo, -(FACE + tread * n_steps + 0.8), PLAZA_Z - 1.0), (hi, -(FACE - 0.5), SILL_Z + 1.3), stone2)
    # the paving: a road from the stair, then rings of wedge stones round a compass
    C = G(0, PLAZA_OUT, PLAZA_Z + 0.12)
    def Q(r, t): return C + Vector((r * math.cos(t), r * math.sin(t), 0))
    road0 = FACE + tread * n_steps
    for k in range(4):
        o0, o1 = lerp(road0, PLAZA_OUT - PLAZA_R + 1.0, k / 4), lerp(road0, PLAZA_OUT - PLAZA_R + 1.0, (k + 1) / 4)
        for j in range(4):
            x0, x1 = -9.0 + j * 4.5, -9.0 + (j + 1) * 4.5
            g.add([G(x0, o0, PLAZA_Z + 0.12), G(x1, o0, PLAZA_Z + 0.12), G(x1, o1, PLAZA_Z + 0.12), G(x0, o1, PLAZA_Z + 0.12)], jit(stone, 0.06, rnd), up=1)
    def ring(r0, r1, count, ca, cb, turn=0.0):
        for k in range(count):
            t0, t1 = turn + k * TAU / count, turn + (k + 1) * TAU / count
            c = jit(ca if k % 2 == 0 else cb, 0.05, rnd)
            g.add([Q(r0, t0), Q(r1, t0), Q(r1, t1), Q(r0, t1)], c, up=1)
    ring(10.0, 11.0, 32, dark, dark)
    ring(11.0, 15.5, 24, stone, stone2)
    ring(15.5, 16.3, 48, dark, dark)
    ring(16.3, PLAZA_R - 1.0, 36, stone2, stone, turn=0.05)
    # the compass: eight points, each half pale and half dark, on a field of mid stone
    inner = 2.8
    for k in range(8):
        tk, tn = k * TAU / 8 + math.pi / 2, (k + 1) * TAU / 8 + math.pi / 2
        L, Ln = (9.6 if k % 2 == 0 else 6.2), (9.6 if (k + 1) % 2 == 0 else 6.2)
        tip, tipn = Q(L, tk), Q(Ln, tn)
        i_prev, i_next = Q(inner, tk - TAU / 16), Q(inner, tk + TAU / 16)
        g.add([C, i_prev, tip], pale, up=1)
        g.add([C, tip, i_next], dark, up=1)
        field = jit(mixc(stone, pale, 0.3), 0.03, rnd)
        m = Q(10.0, tk + TAU / 16)
        g.add([i_next, tip, m], field, up=1)
        g.add([i_next, m, tipn], field, up=1)
        g.add([tip, Q(10.0, tk), m], field, up=1)
        g.add([tipn, m, Q(10.0, tn)], field, up=1)
    # the kerb and the low wall on the sea's side, open toward the quay and toward the stair
    def arc_box(r0, r1, t0, t1, z0, z1, col, n):
        for k in range(n):
            a0, a1 = lerp(t0, t1, k / n), lerp(t0, t1, (k + 1) / n)
            p = [Q(r0, a0), Q(r1, a0), Q(r1, a1), Q(r0, a1)]
            lo = [Vector((q.x, q.y, z0)) for q in p]
            hi = [Vector((q.x, q.y, z1)) for q in p]
            mid = (lo[0] + hi[2]) / 2
            g.add(hi, mul(col, 1.1), up=1)
            g.add([lo[1], lo[2], hi[2], hi[1]], col, away=mid)
            g.add([lo[0], lo[3], hi[3], hi[0]], mul(col, 0.85), away=mid)
            if k == 0: g.add([lo[0], lo[1], hi[1], hi[0]], mul(col, 0.9), away=mid)
            if k == n - 1: g.add([lo[3], lo[2], hi[2], hi[3]], mul(col, 0.9), away=mid)
    arc_box(PLAZA_R - 1.0, PLAZA_R, 0, TAU, PLAZA_Z - 0.4, PLAZA_Z + 0.3, stone2, 48)
    # the bastion: the round wall the plaza stands on, down into the sea
    def at(q, zz): return Vector((q.x, q.y, zz))
    for row in range(3):
        z0, z1 = (-4.0, 0.2, 2.6)[row], (0.2, 2.6, PLAZA_Z + 0.05)[row]
        count = 40
        for k in range(count):
            t0 = (k + 0.5 * (row % 2)) * TAU / count
            t1 = t0 + TAU / count
            if adiff((t0 + t1) / 2, math.pi / 2) < 0.8: continue      # the tower's side: the ground is level with it there
            c = jit(stone2 if row else mul(stone2, 0.7), 0.07, rnd)
            g.add([at(Q(BASTION, t0), z0), at(Q(BASTION, t1), z0), at(Q(BASTION, t1), z1), at(Q(BASTION, t0), z1)], c, away=(C.x, C.y, z0))
    for k in range(40):
        t0, t1 = k * TAU / 40, (k + 1) * TAU / 40
        if adiff((t0 + t1) / 2, math.pi / 2) < 0.8: continue
        g.add([Q(PLAZA_R, t0), Q(BASTION, t0), Q(BASTION, t1), Q(PLAZA_R, t1)], jit(mixc(stone2, (0.42, 0.5, 0.3), 0.35), 0.05, rnd), up=1)
    # angles round the plaza: -90 degrees points out to sea (the quay), +90 back to the stair
    quay_gap, stair_gap = 0.34, 0.52
    for t0, t1 in ((-math.pi / 2 + quay_gap, math.pi / 2 - stair_gap), (math.pi / 2 + stair_gap, 1.5 * math.pi - quay_gap)):
        arc_box(PLAZA_R - 0.9, PLAZA_R - 0.1, t0, t1, PLAZA_Z + 0.3, PLAZA_Z + 1.5, stone, 14)
        posts = 6
        for k in range(posts + 1):
            t = lerp(t0, t1, k / posts)
            c = Q(PLAZA_R - 0.5, t)
            box(g, (c.x - 0.8, c.y - 0.8, PLAZA_Z - 0.3), (c.x + 0.8, c.y + 0.8, PLAZA_Z + 2.2), stone2)
            box(g, (c.x - 1.0, c.y - 1.0, PLAZA_Z + 2.2), (c.x + 1.0, c.y + 1.0, PLAZA_Z + 2.6), pale)
    # the quay: stone steps down to the water, where the wooden pier begins (Unity lays the pier)
    qo = PLAZA_OUT + BASTION - 0.3
    n_q = 7
    # level paving from the plaza's rim out over the bastion to the head of the steps
    g.add([G(-4.5, PLAZA_OUT + PLAZA_R - 0.2, PLAZA_Z + 0.12), G(4.5, PLAZA_OUT + PLAZA_R - 0.2, PLAZA_Z + 0.12), G(4.5, qo + 0.1, PLAZA_Z + 0.12), G(-4.5, qo + 0.1, PLAZA_Z + 0.12)], stone, up=1)
    for k in range(n_q):
        box(g, (-4.5, -(qo + 1.3 * (k + 1)), -4.0), (4.5, -(qo + 1.3 * k), PLAZA_Z - 0.5 * k), jit(stone, 0.04, rnd), faces=("top", "front", "left", "right"))
    box(g, (-5.2, -(qo + 1.3 * n_q + 5.0), -4.0), (5.2, -(qo + 1.3 * n_q), PLAZA_Z - 0.5 * n_q), stone2, faces=("top", "front", "left", "right"))
    # the leaderboard stone, to the right of the stair: a standing stone with a smooth dark face for the words
    def standing_stone(c, facing, w, h, th):
        fwd = Vector((math.cos(facing), math.sin(facing), 0))
        right = Vector((-math.sin(facing), math.cos(facing), 0))
        outline = [(-0.5, 0.0), (0.5, 0.0), (0.52, 0.55), (0.44, 0.8), (0.26, 0.95), (0.0, 1.0), (-0.3, 0.94), (-0.46, 0.78), (-0.52, 0.5)]
        front = [c + right * (x * w) + Vector((0, 0, y * h)) + fwd * (th / 2) for x, y in outline]
        back = [c + right * (x * w * 0.96) + Vector((0, 0, y * h * 0.98)) - fwd * (th / 2) for x, y in outline]
        mid = c + Vector((0, 0, h / 2))
        g.add(front, stone2, away=mid)
        g.add(back, mul(stone2, 0.8), away=mid)
        for k in range(len(outline)):
            kn = (k + 1) % len(outline)
            g.add([front[k], front[kn], back[kn], back[k]], jit(stone, 0.05, rnd), away=mid)
        # the smooth face, a hand's width proud of the rough stone
        face = [(-0.36, 0.1), (0.36, 0.1), (0.36, 0.7), (0.0, 0.78), (-0.36, 0.7)]
        fp = [c + right * (x * w) + Vector((0, 0, y * h)) + fwd * (th / 2 + 0.25) for x, y in face]
        g.add(fp, (0.2, 0.2, 0.23), away=mid)
        for k in range(len(face)):
            kn = (k + 1) % len(face)
            a, b = fp[k], fp[kn]
            g.add([a, b, b - fwd * 0.25, a - fwd * 0.25], dark, away=mid)
        # carved rows under the words: the climbers' names nobody can read yet (the game has no list of them at hand)
        for row in range(2):
            for k in range(7):
                gx, gy = (k - 3) * 0.085 * w, h * (0.235 - 0.08 * row)
                gw, gh = 0.05 * w, 0.055 * h
                for x0, y0, x1, y1 in GLYPHS[rnd.randrange(len(GLYPHS))]:
                    a = c + right * (gx + (x0 - 0.5) * gw) + Vector((0, 0, gy + (y0 - 0.5) * gh)) + fwd * (th / 2 + 0.3)
                    b = c + right * (gx + (x1 - 0.5) * gw) + Vector((0, 0, gy + (y1 - 0.5) * gh)) + fwd * (th / 2 + 0.3)
                    if (b - a).length < 1e-6: continue
                    nrm = (b - a).normalized().cross(fwd) * 0.05
                    g.add([a - nrm, b - nrm, b + nrm, a + nrm], (0.46, 0.46, 0.52), away=mid)
        # the crown over the words
        for x0, y0, x1, y1 in SIGILS["crown"]:
            s = w * 0.2
            a = c + right * ((x0 - 0.5) * s) + Vector((0, 0, h * 0.8 + y0 * s * 0.7)) + fwd * (th / 2 + 0.34)
            b = c + right * ((x1 - 0.5) * s) + Vector((0, 0, h * 0.8 + y1 * s * 0.7)) + fwd * (th / 2 + 0.34)
            d = (b - a).normalized()
            nrm = d.cross(fwd) * 0.14
            g.add([a - nrm, b - nrm, b + nrm, a + nrm], gold, glow=1.0, kind=PANE, away=mid)
        return fp
    slab_c = G(20.5, R0 + 15.0, PLAZA_Z - 0.4)
    slab_face = math.atan2(-(PLAZA_OUT - 6.0) - slab_c.y, 2.0 - slab_c.x)
    standing_stone(slab_c, slab_face, 9.0, 12.5, 2.0)
    box(g, (slab_c.x - 5.6, slab_c.y - 2.4, PLAZA_Z - 1.0), (slab_c.x + 5.6, slab_c.y + 2.4, PLAZA_Z + 0.5), stone2)
    # the statue's pedestal, to the left of the stair (Unity stands the hero on it)
    ped = G(-20.0, R0 + 14.5, PLAZA_Z)
    box(g, (ped.x - 3.6, ped.y - 3.6, PLAZA_Z - 1.0), (ped.x + 3.6, ped.y + 3.6, PLAZA_Z + 0.7), stone2)
    box(g, (ped.x - 2.8, ped.y - 2.8, PLAZA_Z + 0.7), (ped.x + 2.8, ped.y + 2.8, PLAZA_Z + 4.4), stone)
    box(g, (ped.x - 3.2, ped.y - 3.2, PLAZA_Z + 4.4), (ped.x + 3.2, ped.y + 3.2, PLAZA_Z + 5.0), pale)
    for x0, z0, x1, z1 in SIGILS["sun"]:
        s = 2.6
        ax, az, bx, bz = ped.x + (x0 - 0.5) * s, PLAZA_Z + 2.6 + (z0 - 0.5) * s, ped.x + (x1 - 0.5) * s, PLAZA_Z + 2.6 + (z1 - 0.5) * s
        length = math.hypot(bx - ax, bz - az)
        nx, nz = -(bz - az) / length * 0.1, (bx - ax) / length * 0.1
        g.add([Vector((ax - nx, ped.y - 2.92, az - nz)), Vector((bx - nx, ped.y - 2.92, bz - nz)), Vector((bx + nx, ped.y - 2.92, bz + nz)), Vector((ax + nx, ped.y - 2.92, az + nz))], gold, glow=0.9, kind=PANE, away=(ped.x, ped.y, PLAZA_Z + 2.6))
    marks = {
        "gate": [0, -(FACE), SILL_Z], "tunnelEnd": [0, -depth1, SILL_Z], "stairFoot": [0, -(road0 + 1.0), PLAZA_Z + 0.12],
        "plaza": [C.x, C.y, C.z], "plazaRadius": PLAZA_R, "quayHead": [0, -(qo + 1.3 * n_q + 5.0), PLAZA_Z - 0.5 * n_q],
        "slab": [slab_c.x, slab_c.y, slab_c.z], "slabFacing": math.degrees(slab_face), "slabSize": [9.0, 12.5, 2.0],
        "pedestal": [ped.x, ped.y, PLAZA_Z + 5.0], "plazaLevel": PLAZA_Z,
    }
    return g, marks


def egg(seed):
    """The easter egg (docs/easter-eggs.md): a little brass elevator door in the foot of a tower of a hundred floors.
    Unity hangs the OUT OF ORDER sign beside it."""
    g = Geo()
    brass, brass2, dark, stone = (0.78, 0.6, 0.24), (0.62, 0.46, 0.18), (0.14, 0.13, 0.12), (0.5, 0.48, 0.45)
    x, out = 16.5, R0 + 6.2
    # a stone surround standing out of the plinth
    box(g, (x - 2.0, -(out + 0.5), PLAZA_Z - 1.0), (x + 2.0, -(out - 4.0), PLAZA_Z + 4.2), stone)
    box(g, (x - 2.3, -(out + 0.8), PLAZA_Z + 4.2), (x + 2.3, -(out - 4.0), PLAZA_Z + 4.8), mul(stone, 0.85))
    # two brass door leaves, shut, with a dark seam
    for side in (-1, 1):
        lo, hi = (x + 0.04, x + 1.25) if side > 0 else (x - 1.25, x - 0.04)
        box(g, (lo, -(out + 0.62), PLAZA_Z + 0.15), (hi, -(out + 0.45), PLAZA_Z + 3.0), brass, faces=("front", "left", "right", "top"))
        box(g, (lo + 0.2, -(out + 0.66), PLAZA_Z + 0.5), (hi - 0.2, -(out + 0.6), PLAZA_Z + 2.6), brass2, faces=("front",))
    box(g, (x - 0.04, -(out + 0.56), PLAZA_Z + 0.15), (x + 0.04, -(out + 0.45), PLAZA_Z + 3.0), dark, faces=("front",))
    # the floor dial over the door: a half circle with a needle stuck at the bottom floor
    for k in range(6):
        t0, t1 = math.pi * k / 6, math.pi * (k + 1) / 6
        g.add([Vector((x, -(out + 0.56), PLAZA_Z + 3.25)), Vector((x + 0.9 * math.cos(t0), -(out + 0.56), PLAZA_Z + 3.25 + 0.75 * math.sin(t0))), Vector((x + 0.9 * math.cos(t1), -(out + 0.56), PLAZA_Z + 3.25 + 0.75 * math.sin(t1)))], brass2 if k % 2 else brass, away=(x, 0, PLAZA_Z + 3.4))
    g.add([Vector((x - 0.05, -(out + 0.6), PLAZA_Z + 3.25)), Vector((x - 0.8, -(out + 0.6), PLAZA_Z + 3.42)), Vector((x - 0.05, -(out + 0.6), PLAZA_Z + 3.36))], dark, away=(x, 0, PLAZA_Z + 3.4))
    # the call button, glowing faintly: someone keeps pressing it
    box(g, (x + 1.45, -(out + 0.6), PLAZA_Z + 1.2), (x + 1.8, -(out + 0.5), PLAZA_Z + 1.8), dark, faces=("front", "left", "right", "top"))
    g.add([Vector((x + 1.55, -(out + 0.62), PLAZA_Z + 1.4)), Vector((x + 1.7, -(out + 0.62), PLAZA_Z + 1.4)), Vector((x + 1.7, -(out + 0.62), PLAZA_Z + 1.6)), Vector((x + 1.55, -(out + 0.62), PLAZA_Z + 1.6))], (1.0, 0.5, 0.2), glow=1.0, kind=PANE, away=(x, 0, PLAZA_Z + 1.5))
    return g, {"egg": [x, -(out + 0.8), PLAZA_Z], "eggSign": [x + 3.4, -(out + 1.6), PLAZA_Z]}


def flame(seed):
    """A fire for a brazier's bowl, a metre and a half high, its foot at the origin. A phase of 1 or more tells the
    shader it is a small flame (it wavers less)."""
    rnd = random.Random(seed)
    g = Geo()
    blob(g, (0, 0, 0.75), 0.5, 0.5, 0.95, 1, 0.35, (1.0, 0.72, 0.2), (1.0, 0.36, 0.08), 3.0, kind=FLAME, glow=1.0, phase=1.3)
    blob(g, (0, 0, 0.5), 0.3, 0.3, 0.55, 1, 0.3, (1.0, 0.95, 0.6), (1.0, 0.7, 0.25), 7.0, kind=FLAME, glow=1.0, phase=1.7)
    return g


def flock(seed):
    """Seven gulls in loose formation, wings up; Unity sends a few flocks round the tower. Seen from both sides."""
    rnd = random.Random(seed)
    g = Geo()
    white, grey = (0.96, 0.96, 0.95), (0.62, 0.64, 0.68)
    for k in range(7):
        c = Vector((rnd.uniform(-14, 14), rnd.uniform(-9, 9), rnd.uniform(-4, 4)))
        span, lift = rnd.uniform(1.5, 2.0), rnd.uniform(0.25, 0.7)
        nose, tail = c + Vector((0, 0.7, 0)), c + Vector((0, -0.6, 0))
        for side in (-1, 1):
            elbow = c + Vector((side * span * 0.5, 0.1, lift))
            tip = c + Vector((side * span, -0.25, lift * 0.4))
            for pts, col in (([nose, elbow, tail], white), ([elbow, tip, tail], grey)):
                g.add(pts, col, up=1)
                g.add(pts, mul(col, 0.8), up=-1)
    return g


# ---- making the files ------------------------------------------------------------------------------------------------
def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export(name):
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", use_mesh_modifiers=False, add_leaf_bones=False,
                             colors_type="SRGB", object_types={"MESH"})
    return path


def render(name, cam_loc, look, lens=35, size=(1280, 1600), ortho=None):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    sh = scene.display.shading
    sh.light = "STUDIO"
    sh.color_type = "VERTEX"
    sh.show_cavity = False
    sh.show_shadows = False
    sh.show_backface_culling = True
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.world = scene.world or bpy.data.worlds.new("World")
    scene.world.color = (0.35, 0.6, 0.9)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = cam_loc
    cam.rotation_euler = (Vector(look) - Vector(cam_loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.clip_start, cam_data.clip_end = 1.0, 6000
    cam_data.lens = lens
    if ortho:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


built = {"note": "Made by tools/tower/world/world_tower.py from stack.json; read by tools/tower/world/world-tower-build.cs. Heights in metres above the sea, radii in metres.",
         "position": STACK["position"], "seaLevel": STACK["seaLevel"], "faces": STACK["faces"], "sections": []}
everything = []   # (name, near geos by part, base z) for the whole tower's preview
everything_far = []

r_top = R0
z = 0.0
entries = [dict(s, cap=False) for s in STACK["sections"]] + [dict(STACK["cap"], cap=True, floor=0)]
for index, s in enumerate(entries):
    clear()
    style = STYLES[s["style"]]
    name = "WorldTower_Cap" if s["cap"] else "WorldTower_%d_%s" % (s["floor"], s["style"].capitalize())
    info = {"floor": s["floor"], "style": s["style"], "model": name, "base": z, "height": s["height"], "seat": r_top, "glow": s.get("glow", [1, 1, 1]), "cap": s["cap"], "parts": {}}
    for lod in (0, 1):
        parts, top = style(r_top, s["height"], lod, 100 + index)
        for part, geo in parts.items():
            geo.to_object("%s_%s" % (part, "Near" if lod == 0 else "Far"))
            info["parts"].setdefault(part, {})["lod%d" % lod] = geo.tris()
            (everything if lod == 0 else everything_far).append((name + "_" + part, geo, z))
    info["top"] = top
    export(name)
    built["sections"].append(info)
    print("SECTION", name, json.dumps(info["parts"]))
    r_top = top
    z += s["height"]
built["height"] = z

clear()
marks = {}
info = {"model": "WorldTower_Islet", "parts": {}}
for lod in (0, 1):
    gi = islet(lod, 7)
    gi.to_object("Islet_%s" % ("Near" if lod == 0 else "Far"))
    info["parts"].setdefault("Islet", {})["lod%d" % lod] = gi.tris()
    gp, m = plaza(lod, 11)
    marks.update(m)
    gp.to_object("Plaza_%s" % ("Near" if lod == 0 else "Far"))
    info["parts"].setdefault("Plaza", {})["lod%d" % lod] = gp.tris()
    (everything if lod == 0 else everything_far).append(("Islet", gi, 0.0))
    (everything if lod == 0 else everything_far).append(("Plaza", gp, 0.0))
ge, m = egg(3)
marks.update(m)
gl = flame(9)
gl.to_object("Flame_Near")
info["parts"]["Flame"] = {"lod0": gl.tris()}
gf = flock(5)
gf.to_object("Flock_Near")
info["parts"]["Flock"] = {"lod0": gf.tris()}
ge.to_object("Egg_Near")
info["parts"]["Egg"] = {"lod0": ge.tris()}
everything.append(("Egg", ge, 0.0))
export("WorldTower_Islet")
built["islet"] = info
# the marks are in Unity's terms once the builder has turned the model back: x the same, z = Blender's y, y up
built["marks"] = {k: ([v[0], v[2], v[1]] if isinstance(v, list) and len(v) == 3 and k not in ("slabSize",) else v) for k, v in marks.items()}
built["baseRadius"] = R0
total0 = sum(p.get("lod0", 0) for s in built["sections"] for p in s["parts"].values()) + sum(p.get("lod0", 0) for p in info["parts"].values())
total1 = sum(p.get("lod1", 0) for s in built["sections"] for p in s["parts"].values()) + sum(p.get("lod1", 0) for p in info["parts"].values())
built["triangles"] = {"lod0": total0, "lod1": total1}
with open(os.path.join(HERE, "stack-built.json"), "w") as f:
    json.dump(built, f, indent=1)
print("WORLD TOWER DONE height", z, "triangles", total0, total1)

if PREVIEWS:
    clear()
    for name, geo, base in everything:
        o = geo.to_object(name)
        o.location.z = base
    H = z
    render("tower-front", (0, -900, 140), (0, 0, H * 0.48), lens=30)
    render("tower-quarter", (520, -620, 60), (0, 0, H * 0.45), lens=28)
    render("tower-low", (-260, -330, 12), (0, 0, 190), lens=20)
    render("foot-front", (0, -260, 40), (0, -40, 30), lens=28, size=(1600, 1000))
    render("plaza", (46, -176, 46), (0, -98, 10), lens=30, size=(1600, 1000))
    render("gate", (-10, -128, 12), (2, -70, 16), lens=24, size=(1600, 1000))
    render("fortress", (300, -360, 420), (0, 0, 345), lens=40, size=(1600, 1000))
    render("frost", (260, -300, 170), (0, 0, 150), lens=35, size=(1600, 1000))
    render("islet-top", (0, -40, 700), (0, -40, 0), lens=50, size=(1400, 1400))
    # the far models, as the world sees them from most islands
    clear()
    for name, geo, base in everything_far:
        o = geo.to_object(name)
        o.location.z = base
    render("far-quarter", (520, -620, 60), (0, 0, H * 0.45), lens=28)
    render("far-foot", (0, -330, 40), (0, -40, 60), lens=28, size=(1600, 1000))
    print("PREVIEWS DONE")
