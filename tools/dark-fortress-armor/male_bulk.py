# The male bulk pass (headless Blender 5.x): makes the male pieces read as a broad armored man.
#
# The pack's characters are dressed on a slim base body: on the game's male the Knight's torso sat about 1 cm
# outside the bare body (the game's plate stands 2 to 4 cm out), its waist was cinched and its chest plates were two
# sculpted mounds, which read as a woman's breastplate (the owner: "the chests seem to be more to the female side").
# This is a "male" shape key on the uncut character mesh, like the female fit, so the cuts carry it and the pieces of
# one set still meet exactly; the cut lines stay the same, so the pieces still mix with the game's parts. The female
# pieces keep the female fit (fit.py) and are not touched.
#
# What the field does, by the bones a vertex is weighted to (smooth blends, never a hard edge):
#   torso   wider (shoulders and chest +10 %, the waist +14 %: less cinch, a straighter taper), the front pushed out
#           to the plate's chest depth, and the chest flattened: in the bust band every front vertex near the middle
#           is pulled forward toward the slice's most forward point, which fills the valley between the mounds and
#           makes one broad barrel front (the plate edges and details move with it, so they stay)
#   pauldrons (loose parts on the clavicle and shoulder) 15 % bigger about their own center and 2 cm further out
#   arms    thicker about the arm's own line (upper arm +14 %, forearm +10 %), like the game's plate arms
#   legs    +4 % about the leg's line
# Then no vertex may lie inside the game's male body: any that does is pushed out to 4 mm.
import bpy, bmesh, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

MALE_BODY = ["Chr_Torso_Male_00", "Chr_ArmUpperLeft_Male_00", "Chr_ArmUpperRight_Male_00", "Chr_ArmLowerLeft_Male_00",
             "Chr_ArmLowerRight_Male_00", "Chr_HandLeft_Male_00", "Chr_HandRight_Male_00", "Chr_Hips_Male_00",
             "Chr_LegLeft_Male_00", "Chr_LegRight_Male_00"]

TORSO = ("Hips", "Spine_01", "Spine_02", "Spine_03", "Neck", "Clavicle_L", "Clavicle_R")
UPPER = {"L": ("Shoulder_L",), "R": ("Shoulder_R",)}
LOWER = {"L": ("Elbow_L",), "R": ("Elbow_R",)}
LEG = {"L": ("UpperLeg_L", "LowerLeg_L"), "R": ("UpperLeg_R", "LowerLeg_R")}

# the strength of each part of the field; tuned by eye against the game's plate (docs/dark-fortress-armor-test.md)
K = {"chest_w": 1.19, "waist_w": 1.20, "hips_w": 1.06, "front": 1.20, "back": 1.08, "flatten": 1.0, "chest_half": 0.17,
     "pad_scale": 1.45, "pad_out": 0.045, "upper": 1.24, "lower": 1.16, "leg": 1.08, "clearance": 0.004}


def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def _share(v, dl, names, bones):
    return sum(w for g, w in v[dl].items() if names.get(g) in bones)


def _body_tree(lib):
    bm = bmesh.new()
    for n in MALE_BODY:
        o = lib.mfh[n]
        tmp = bmesh.new(); tmp.from_mesh(o.data); tmp.transform(o.matrix_world)
        vm = {v: bm.verts.new(v.co) for v in tmp.verts}
        for f in tmp.faces:
            try: bm.faces.new([vm[v] for v in f.verts])
            except ValueError: pass
        tmp.free()
    bm.normal_update()
    return BVHTree.FromBMesh(bm), bm


def _bone_line(lib, a, b):
    A = lib.rig.matrix_world
    return A @ lib.rig.data.bones[a].head_local, A @ lib.rig.data.bones[b].head_local


def _radial(p, line, k):
    a, b = line
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    q = a + ab * t
    return q + (p - q) * k


def add_male_key(lib, o, k=None):
    """A "male" shape key on the uncut world-space character mesh (see the file's comment)."""
    k = dict(K, **(k or {}))
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    dl = bm.verts.layers.deform.active
    names = {g.index: g.name for g in o.vertex_groups}
    bm.verts.ensure_lookup_table()
    # the spine's line in y per height (the body's middle front to back), from the bare body
    tree, body = _body_tree(lib)
    lines = {s: (_bone_line(lib, "Shoulder_" + s, "Elbow_" + s), _bone_line(lib, "Elbow_" + s, "Hand_" + s),
                 _bone_line(lib, "UpperLeg_" + s, "Ankle_" + s)) for s in ("L", "R")}

    # loose islands that are pauldrons: scaled as a whole
    islands = []
    seen = set()
    for v in bm.verts:
        if v.index in seen: continue
        comp = []; stack = [v]; seen.add(v.index)
        while stack:
            x = stack.pop(); comp.append(x)
            for e in x.link_edges:
                y = e.other_vert(x)
                if y.index not in seen: seen.add(y.index); stack.append(y)
        islands.append(comp)
    pad_of = {}
    for comp in islands:
        if len(comp) > 0.5 * len(bm.verts): continue
        cen = sum((v.co for v in comp), Vector()) / len(comp)
        side = "L" if cen.x > 0 else "R"
        cs = sum(_share(v, dl, names, ("Clavicle_" + side, "Shoulder_" + side)) for v in comp) / len(comp)
        if cs > 0.9 and cen.z > 1.3:
            for v in comp: pad_of[v.index] = (cen, side)

    # the chest's front line: the front-most point of each 2 cm slice near the middle, then made straight from the
    # waist up to the chest's peak and flat above it. A bust is two mounds with a valley between and an undercut
    # below; pulling the middle's front out to this line fills both, leaving one broad barrel front.
    front = {}
    for v in bm.verts:
        if abs(v.co.x) < 0.10 and 0.98 < v.co.z < 1.48 and _share(v, dl, names, TORSO) > 0.5:
            key = round(v.co.z / 0.02)
            front[key] = min(front.get(key, 0.0), v.co.y)
    peak = min((kk for kk in front if 1.22 <= kk * 0.02 <= 1.42), key=lambda kk: front[kk], default=None)
    waist = min(front, key=lambda kk: abs(kk * 0.02 - 1.02), default=None)

    def front_line(z):
        if peak is None or waist is None: return None
        zp, zw = peak * 0.02, waist * 0.02
        if z >= zp: return front[peak]
        if z <= zw: return None
        t = (z - zw) / (zp - zw)
        return front[waist] + (front[peak] - front[waist]) * t

    new = []
    for v in bm.verts:
        p = v.co.copy()
        if v.index in pad_of:
            cen, side = pad_of[v.index]
            q = cen + (p - cen) * k["pad_scale"]
            q.x += k["pad_out"] * (1 if side == "L" else -1)
            new.append(q); continue
        side = "L" if p.x > 0 else "R"
        wt = _share(v, dl, names, TORSO)
        wu = _share(v, dl, names, UPPER[side])
        wl = _share(v, dl, names, LOWER[side])
        wg = _share(v, dl, names, LEG[side])
        tot = wt + wu + wl + wg
        q = p.copy()
        if tot > 0:
            # torso
            z = p.z
            sx = 1.0 + (k["hips_w"] - 1.0) * smooth(0.80, 0.92, z)
            sx += (k["waist_w"] - k["hips_w"]) * smooth(0.90, 1.00, z) * (1 - smooth(1.12, 1.22, z))
            sx += (k["chest_w"] - 1.0) * smooth(1.12, 1.22, z) * (1 - smooth(1.50, 1.58, z))
            cy = 0.02
            fy = k["front"] if p.y < cy else k["back"]
            band = smooth(0.92, 1.05, z) * (1 - smooth(1.48, 1.56, z))
            t = Vector((p.x * sx, cy + (p.y - cy) * (1 + (fy - 1) * band), z))
            # the flat chest: the middle's front pulled out to the front line (then pushed out with the rest)
            line = front_line(z)
            if p.y < cy - 0.02 and line is not None:
                g = math.exp(-(p.x / k["chest_half"]) ** 2) * smooth(1.04, 1.14, z) * (1 - smooth(1.44, 1.50, z))
                if p.y > line:
                    py = p.y + (line - p.y) * k["flatten"] * g
                    t.y = cy + (py - cy) * (1 + (fy - 1) * band)
            # arms and legs about their own lines
            ua = _radial(p, lines[side][0], k["upper"])
            la = _radial(p, lines[side][1], k["lower"])
            lg = _radial(p, lines[side][2], k["leg"])
            q = (t * wt + ua * wu + la * wl + lg * wg) / tot
        # nothing inside the game's male body
        loc, nor, _, dist = tree.find_nearest(q)
        if loc is not None and dist < 0.06 and (q - loc).dot(nor) < k["clearance"]:
            q = loc + nor * k["clearance"]
        new.append(q)
    bm.free(); body.free()
    if not me.shape_keys:
        o.shape_key_add(name="Basis")
    key = o.shape_key_add(name="male")
    for i, q in enumerate(new):
        key.data[i].co = q
    return key


def apply_male(pieces):
    """The male pieces take the "male" positions the cut carried (piece["male"])."""
    for p in pieces.values():
        m = p.get("male")
        if m is None: continue
        for i, v in enumerate(p.data.vertices):
            v.co = p.matrix_world.inverted() @ Vector(m[i * 3:i * 3 + 3])
        p.data.update()
