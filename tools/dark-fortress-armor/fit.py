# Fitting cut pieces to the MFH female body (headless Blender 5.x).
#
# The Dark Fortress bodies are the MFH male body (same skeleton, same proportions), so the male pieces need no
# reshaping. For the female body a displacement field is built from the MFH male base parts to the female ones:
# every male part (subdivided for denser samples) is projected onto its female counterpart (nearest surface point),
# giving samples "here on the male body the female surface is this far away". Each armor vertex moves by the
# inverse-distance weighted mean of its nearest samples, so a plate 3 cm off the male chest ends up 3 cm off the
# female chest. A last pass pushes any vertex that ended up inside the female body back out to a small clearance.
# Skin weights are kept: both bodies share the skeleton.
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

PAIRS = [("Chr_Torso_Male_00", "Chr_Torso_Female_00"),
         ("Chr_ArmUpperLeft_Male_00", "Chr_ArmUpperLeft_Female_00"),
         ("Chr_ArmUpperRight_Male_00", "Chr_ArmUpperRight_Female_00"),
         ("Chr_ArmLowerLeft_Male_00", "Chr_ArmLowerLeft_Female_00"),
         ("Chr_ArmLowerRight_Male_00", "Chr_ArmLowerRight_Female_00"),
         ("Chr_HandLeft_Male_00", "Chr_HandLeft_Female_00"),
         ("Chr_HandRight_Male_00", "Chr_HandRight_Female_00"),
         ("Chr_Hips_Male_00", "Chr_Hips_Female_00"),
         ("Chr_LegLeft_Male_00", "Chr_LegLeft_Female_00"),
         ("Chr_LegRight_Male_00", "Chr_LegRight_Female_00"),
         ("Chr_Head_Male_00", "Chr_Head_Female_00")]


def _world_bm(o):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.transform(o.matrix_world)  # MFH sources are already in world meters (identity), kept general
    return bm


def _center(o):
    vs = o.data.vertices
    return o.matrix_world @ (sum((v.co for v in vs), Vector()) / len(vs))


def matched_pairs(lib):
    """PAIRS, with each male part matched to the female part of its kind on the same side of the body: the MFH
    file does not name the sides the same way for both sexes (a male "LegLeft" can sit where the female "LegRight"
    does)."""
    out = []
    females = [f for _, f in PAIRS]
    for m, f in PAIRS:
        kind = f.replace("Left", "").replace("Right", "")
        cands = [x for x in females if x.replace("Left", "").replace("Right", "") == kind]
        cm = _center(lib.mfh[m])
        out.append((m, min(cands, key=lambda x: (_center(lib.mfh[x]) - cm).length)))
    return out


def displacement_samples(lib, subdiv=2):
    pts, disp = [], []
    for m, f in matched_pairs(lib):
        bm_f = _world_bm(lib.mfh[f])
        tree = BVHTree.FromBMesh(bm_f)
        bm_m = _world_bm(lib.mfh[m])
        if subdiv:
            bmesh.ops.subdivide_edges(bm_m, edges=bm_m.edges[:], cuts=subdiv, use_grid_fill=True)
        for v in bm_m.verts:
            hit = tree.find_nearest(v.co)
            if hit[0] is None:
                continue
            pts.append(v.co.copy())
            disp.append(hit[0] - v.co)
        bm_f.free(); bm_m.free()
    kd = KDTree(len(pts))
    for i, p in enumerate(pts):
        kd.insert(p, i)
    kd.balance()
    return kd, disp


def female_body_tree(lib, side=1):
    """One BVH of the female (side 1) or male (side 0) base body (no head), for the push-out pass."""
    bm = bmesh.new()
    for pair in PAIRS:
        f = pair[side]
        if "Head" in f:
            continue
        tmp = _world_bm(lib.mfh[f])
        vmap = {v: bm.verts.new(v.co) for v in tmp.verts}
        for fc in tmp.faces:
            try:
                bm.faces.new([vmap[v] for v in fc.verts])
            except ValueError:
                pass
        tmp.free()
    bm.normal_update()
    return BVHTree.FromBMesh(bm), bm


def add_female_key(lib, o, k=10, smooth=8):
    """Gives the whole (uncut) world-space character mesh a "female" shape key: the field at each vertex, then
    smoothed along the mesh so a plate moves as one (a hanging flap is not squeezed by the thighs beside it). Done
    before cutting, so the pieces of one armor still meet exactly where they were cut apart."""
    kd, disp = displacement_samples(lib)
    me = o.data
    n = len(me.vertices)
    d = [_field(kd, disp, v.co, k) for v in me.vertices]
    nbrs = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        nbrs[a].append(b); nbrs[b].append(a)
    for _ in range(smooth):
        d = [(d[i] + sum((d[j] for j in nbrs[i]), Vector()) / len(nbrs[i])) * 0.5 if nbrs[i] else d[i]
             for i in range(n)]
    if not me.shape_keys:
        o.shape_key_add(name="Basis")
    key = o.shape_key_add(name="female")
    for i, v in enumerate(me.vertices):
        key.data[i].co = v.co + d[i]
    return key


def morph_female(lib, pieces, coll, clearance=0.004):
    """Female copies of the pieces: {region: object}. Each piece carries its female vertex positions from the cut
    (piece["female"]); a last pass pushes back out any vertex that went deeper into the female body than it was
    in the male one (a pure function of the position, so pieces still meet). Pads stay as they are: the game's
    shoulder and knee pad arrays are shared by both sexes."""
    tree, body_bm = female_body_tree(lib, 1)
    mtree, mbody_bm = female_body_tree(lib, 0)
    # the neck: the collar sits on the player's own head mesh, which is thinner in the female; a collar vertex keeps
    # the depth it had against the male neck (in or out), so the collar neither pokes through nor floats
    heads = {}
    for side, nm in ((0, "Chr_Head_Male_00"), (1, "Chr_Head_Female_00")):
        hb = _world_bm(lib.mfh[nm]); hb.normal_update()
        heads[side] = (BVHTree.FromBMesh(hb), hb)
    out, stats = {}, {}
    for r, p in pieces.items():
        o = p.copy()
        o.data = p.data.copy()
        o.name = p.name + "_F"
        o.data.name = o.name
        coll.objects.link(o)
        o.parent = p.parent
        o.matrix_parent_inverse = p.matrix_parent_inverse.copy()
        mw = p.matrix_world
        inv = mw.inverted()
        pushed = 0
        fem = p.get("female")
        if r.startswith(("shoulder", "knee")) or fem is None:
            if fem is not None:
                move = max(((Vector(fem[i * 3:i * 3 + 3]) - v.co).length for i, v in enumerate(p.data.vertices)))
                stats[r + "_would_move_cm"] = round(move * 100, 2)
        else:
            for i, v in enumerate(o.data.vertices):
                w = mw @ v.co
                nco = mw @ Vector(fem[i * 3:i * 3 + 3])
                mh = mtree.find_nearest(w)
                allowed = clearance
                if mh[0] is not None and (w - mh[0]).length < 0.05:
                    allowed = min(clearance, (w - mh[0]).dot(mh[1]))
                if w.z > 1.36:
                    mh2 = heads[0][0].find_nearest(w)
                    if mh2[0] is not None and (w - mh2[0]).length < 0.04:
                        want = (w - mh2[0]).dot(mh2[1])
                        fh = heads[1][0].find_nearest(nco)
                        if fh[0] is not None:
                            have = (nco - fh[0]).dot(fh[1])
                            # hidden in the male neck: as deep in the female one. Near it outside: as close.
                            if want < 0:
                                want -= 0.002
                            if (want < 0 and have > want) or (0 <= want < 0.012 and abs(have - want) > 0.001):
                                nco = nco + fh[1] * (want - have)
                                pushed += 1
                                v.co = inv @ nco
                                continue
                hit = tree.find_nearest(nco)
                if hit[0] is not None:
                    loc, nor = hit[0], hit[1]
                    depth = (nco - loc).dot(nor)
                    if depth < allowed - 0.002 and (nco - loc).length < 0.05:
                        nco = nco + nor * (allowed - depth)
                        pushed += 1
                v.co = inv @ nco
        stats[r] = pushed
        out[r] = o
    body_bm.free(); mbody_bm.free(); heads[0][1].free(); heads[1][1].free()
    return out, stats


def _field(kd, disp, p, k):
    acc = Vector(); wsum = 0.0
    for co, i, dist in kd.find_n(p, k):
        w = 1.0 / (dist * dist + 1e-4)
        acc += disp[i] * w
        wsum += w
    return acc / wsum if wsum else Vector()
